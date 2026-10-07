"""돌 녹음의 강한 충돌 구간과 잔해 구간을 원본 보존 방식으로 추출한다."""
import csv
import json
import wave
from pathlib import Path
import numpy as np
from scipy.signal import find_peaks

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'res/audio/stone-analysis'
SOURCES = [
    ('stones_falling', ROOT / 'res/audio/stone-analysis/stones_falling/567251__iwanplays__stones-falling.wav'),
    ('dropping_rocks', ROOT / 'res/audio/stone-analysis/dropping_rocks/567250__iwanplays__dropping-rocks.wav'),
]

def read(path):
    with wave.open(str(path), 'rb') as stream:
        assert stream.getsampwidth() == 2
        rate = stream.getframerate()
        data = np.frombuffer(stream.readframes(stream.getnframes()), dtype='<i2').reshape(-1, stream.getnchannels())
    return rate, data.astype(np.float64) / 32768

def write(path, data, rate):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.round(np.clip(data, -1, 32767/32768) * 32768).astype('<i2')
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(data.shape[1]); stream.setsampwidth(2); stream.setframerate(rate)
        stream.writeframes(pcm.tobytes())

def extract(data, rate, start, end):
    clip = data[round(start*rate):round(end*rate)].copy()
    # 공격부를 보존하는 0.7ms 페이드인, 절단 노이즈를 줄이는 12ms 페이드아웃.
    attack = min(round(rate*.0007), len(clip)//8)
    release = min(round(rate*.012), len(clip)//5)
    clip[:attack] *= np.linspace(0, 1, attack)[:, None]
    clip[-release:] *= np.linspace(1, 0, release)[:, None]
    # 개별 피크 정규화 없이 원본의 강약을 보존한다.
    return clip * .7943282347

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rows, recommended = [], []
    for label, path in SOURCES:
        rate, data = read(path)
        hop = round(rate*.004)
        count = len(data)//hop
        rms = np.sqrt(np.mean(data[:count*hop].reshape(count, hop, -1)**2, axis=(1, 2)))
        peaks, info = find_peaks(rms, prominence=.065, height=.13, distance=round(.115*rate/hop))
        if np.max(rms[:5]) > .18:
            early = int(np.argmax(rms[:5]))
            peaks = np.unique(np.r_[early, peaks[peaks > 20]])
        starts = []
        for peak in peaks:
            lo = max(0, peak-round(.045*rate/hop))
            valley = lo+int(np.argmin(rms[lo:peak+1]))
            starts.append(max(0, valley*hop/rate-.001))
        per_source = []
        for index, (peak, start) in enumerate(zip(peaks, starts)):
            next_start = starts[index+1] if index+1<len(starts) else len(data)/rate
            end = min(start+.24, next_start, len(data)/rate)
            if end-start < .065:
                continue
            clip = extract(data, rate, start, end)
            name = f'{label}_impact_{len(per_source)+1:02d}.wav'
            rel = Path(label)/'impacts'/name
            write(OUT/rel, clip, rate)
            mono = clip.mean(axis=1)
            spectrum = np.abs(np.fft.rfft(mono*np.hanning(len(mono))))**2
            frequencies = np.fft.rfftfreq(len(mono), 1/rate)
            centroid = float(np.sum(spectrum*frequencies)/max(np.sum(spectrum), 1e-12))
            peak_level = float(np.max(np.abs(clip)))
            tail = np.sqrt(np.mean(clip[-max(1, round(.02*rate)):]**2))
            score = float(rms[peak]/max(tail, .015))
            row = dict(file=rel.as_posix(), source=path.name, start=round(start,4), end=round(end,4),
                duration=round(len(clip)/rate,4), peak_dbfs=round(20*np.log10(max(peak_level,1e-10)),2),
                spectral_centroid_hz=round(centroid), purpose='짧은 돌 충돌 후보', score=round(score,3))
            rows.append(row); per_source.append((row, clip, rate))
        # 단발음과 별도로 연속 충돌 질감을 보존한다.
        for index, (start,end) in enumerate([(0,.75),(1.45,2.35),(2.65,3.72)],1):
            clip=extract(data,rate,start,end)
            rel=Path(label)/'debris'/f'{label}_debris_{index:02d}.wav'
            write(OUT/rel,clip,rate)
            rows.append(dict(file=rel.as_posix(),source=path.name,start=start,end=end,duration=round(len(clip)/rate,4),
                peak_dbfs=round(20*np.log10(np.max(np.abs(clip))),2),spectral_centroid_hz='',purpose='연속 충돌·잔해 후보',score=''))
        recommended += sorted(per_source,key=lambda entry:entry[0]['score'],reverse=True)[:6]
    preview, timeline, cursor = [], [], 0
    for index,(row,clip,rate) in enumerate(recommended,1):
        name=f'{index:02d}_'+Path(row['file']).name
        write(OUT/'recommended'/name,clip,rate)
        timeline.append(f"{index:02d}. {cursor:.2f}초 — {name} (원본 {row['start']:.3f}–{row['end']:.3f}초)")
        preview += [clip,np.zeros((round(rate*.35),2))]
        cursor += len(clip)/rate+.35
    write(OUT/'preview_recommended.wav',np.concatenate(preview),rate)
    with (OUT/'segments.csv').open('w',encoding='utf-8-sig',newline='') as file:
        writer=csv.DictWriter(file,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
    (OUT/'segments.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    (OUT/'README.md').write_text('# 돌 효과음 분할\n\n'
        '원본은 수정하지 않았다. 두 파일 모두 44.1kHz / 16bit / stereo / 약 4.202초다. '
        'RMS 피크와 앞쪽 에너지 골짜기를 기준으로 분할했으며 충돌이 겹친 부분은 그대로 남아 있다. '
        '단발음은 0.065~0.24초이며 0.7ms/12ms 페이드와 공통 -2dB 게인을 적용했다. 피치·EQ·개별 정규화는 적용하지 않았다.\n\n'
        f'- 추출 구간: {len(rows)}개 (추천 폴더의 파일은 이 중 복사본)\n'
        '- recommended: 어택 대비 꼬리 에너지로 고른 12개 후보. 청음 평가가 아닌 파형 지표 기반이다.\n'
        '- impacts: 원본별 짧은 충돌 구간\n- debris: 원본별 연속 충돌/잔해 구간 3개\n'
        '- segments.csv / json: 원본 시작·종료 시간, 길이, 피크, 주파수 지표\n'
        '- 게임에는 아직 연결하지 않았다.\n\n## 추천 미리듣기 순서\n\n'+'\n'.join(timeline)+'\n\n'
        '출처: iwanPlays — https://freesound.org/people/iwanPlays/sounds/567251/ 및 https://freesound.org/people/iwanPlays/sounds/567250/ . '
        '567251의 CC0는 앞서 페이지에서 확인했다. 567250의 라이선스는 이 분할 작업에서는 별도로 확인하지 않았다.\n',encoding='utf-8')
    print(json.dumps({'segments':len(rows),'impacts':sum('impact' in row['file'] for row in rows),'debris':6,'recommended':len(recommended),'output':str(OUT)},ensure_ascii=False))

if __name__ == '__main__':
    main()
