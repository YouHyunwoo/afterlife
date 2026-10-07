"""나무 쓰러짐 녹음의 파형 기반 편집 후보를 원본 보존 방식으로 생성한다."""
from pathlib import Path
import argparse
import csv
import zipfile
import numpy as np
from scipy.io import wavfile

ROOT = Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--input', type=Path, required=True, help='ecfike 원본 WAV 경로')
args=parser.parse_args()
if not args.input.is_file():parser.error('입력 WAV를 찾을 수 없습니다: '+str(args.input))
SOURCE = args.input
OUT = ROOT / 'res/audio/tree-fall-analysis'

def main():
    rate, pcm = wavfile.read(SOURCE)
    assert pcm.dtype == np.int16
    data = pcm.astype(np.float64) / 32768
    OUT.mkdir(parents=True, exist_ok=True)
    # 시작부를 보존하고 절단점만 페이드 처리한다. 개별 정규화/피치/EQ는 적용하지 않는다.
    cuts = [
        ('01_tree_fall_full', 0, 4.46, .002, .08, '전체 흐름 보존'),
        ('02_tree_fall_short', .48, 3.42, .008, .20, '짧은 게임용 후보'),
        ('03_tree_fall_strong', .70, 1.98, .006, .08, '강한 소리 구간 후보'),
        ('04_tree_fall_tail', 1.94, 4.46, .025, .10, '후반 잔향 구간 후보'),
        ('05_tree_fall_start', 0, .74, .002, .035, '초반 구간 후보'),
    ]
    rows=[]
    clips=[]
    for name,start,end,fadein,fadeout,description in cuts:
        clip=data[round(start*rate):round(end*rate)].copy()
        ni,no=round(fadein*rate),round(fadeout*rate)
        clip[:ni]*=np.linspace(0,1,ni)[:,None]
        clip[-no:]*=np.linspace(1,0,no)[:,None]
        result=np.round(np.clip(clip,-1,32767/32768)*32768).astype(np.int16)
        wavfile.write(OUT/(name+'.wav'),rate,result)
        clips.append(result)
        rows.append([name+'.wav',start,end,len(result)/rate,description])
    silence=np.zeros((round(rate*.6),data.shape[1]),dtype=np.int16)
    wavfile.write(OUT/'preview_all.wav',rate,np.concatenate([part for clip in clips for part in (clip,silence)]))
    with (OUT/'segments.csv').open('w',newline='',encoding='utf-8-sig') as stream:
        writer=csv.writer(stream); writer.writerow(['파일','원본 시작 초','원본 끝 초','길이 초','용도 후보']); writer.writerows(rows)
    (OUT/'README.md').write_text('''# 나무 쓰러짐 효과음 편집 후보

원본: 139952__ecfike__a-tree-falling-down.wav (ecfike)
출처: https://freesound.org/people/ecfike/sounds/139952/
원본 길이 약 4.52초, 44.1kHz / 16bit / 스테레오. 원본 파일은 수정하지 않았다.

|파일|원본 구간|길이|용도 후보|
|---|---|---|---|
|01_tree_fall_full.wav|0.00–4.46초|4.46초|전체 흐름 보존|
|02_tree_fall_short.wav|0.48–3.42초|2.94초|짧은 게임용 후보|
|03_tree_fall_strong.wav|0.70–1.98초|1.28초|강한 소리 구간|
|04_tree_fall_tail.wav|1.94–4.46초|2.52초|후반 잔향|
|05_tree_fall_start.wav|0.00–0.74초|0.74초|초반 구간|

`preview_all.wav`는 위 순서대로 0.6초 간격을 두고 재생한다.
먼저 full과 short를 비교하고 애니메이션 길이에 맞는 것을 고르면 된다.

파형과 0.1초 RMS 분석으로 자른 후보이며 직접 청취로 줄기/가지/착지 소리를 식별한 결과는 아니다. 초반부터 소리가 있어서 full의 시작은 보존했다. 큰 에너지는 약 0.7–1.9초에 집중되고 후반은 점차 감쇠한다. 구간 파일은 일부 겹치므로 이어 붙이면 원본이 중복된다. 각 파일의 절단점에 짧은 페이드를 적용했고 short 끝에는 0.2초 페이드를 적용했다. 개별 볼륨 정규화, 피치 변경, EQ, 잡음 제거는 하지 않았다. Unity에는 아직 연결하지 않았다.
''',encoding='utf-8')
    with zipfile.ZipFile(OUT/'tree_fall_slices.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(OUT.iterdir()):
            if path.suffix in ('.wav','.csv','.md'): archive.write(path,path.name)
    print('Created',len(rows),'clips and preview in',OUT)
    for row in rows: print(row[0],round(row[3],3),'seconds')

if __name__=='__main__': main()
