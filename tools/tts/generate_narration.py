"""Supertonic 3 로컬 남성 TTS 생성. 모델은 .tooling, 결과 WAV만 Unity에 복사한다."""
from pathlib import Path
import argparse, hashlib, json, math, shutil, sys, time

ROOT = Path(__file__).resolve().parents[2]
CACHE = ROOT / '.tooling/youtube-tts'
SOURCE = ROOT / 'devlog/youtube/day-01/source/narration'
OUTPUT = ROOT / 'devlog/youtube/day-01/media/audio/narration'
UNITY = ROOT / 'impls/unity/Afterlife/Assets/Devlog/Youtube/Day01/Audio/Narration'

def write_text_retry(path, text):
    # 열린 에디터의 임포트가 잠깐 잡고 있는 파일은 재시도한다.
    for attempt in range(8):
        try:
            path.write_text(text,encoding='utf-8');return
        except OSError as error:
            if attempt==7 or error.errno not in (13,22):raise
            time.sleep(.2*(attempt+1))

def episode_name(value):
    import re
    if not re.fullmatch(r'day-\d{2}', value):
        raise argparse.ArgumentTypeError('회차는 day-01 형식이어야 합니다.')
    return value

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--sample', action='store_true')
    parser.add_argument('--timing-only', action='store_true', help='기존 WAV를 검증하여 시간표만 갱신한다. 음성을 새로 합성하지 않는다.')
    parser.add_argument('--day', default='day-01', type=episode_name)
    args = parser.parse_args()
    global SOURCE, OUTPUT, UNITY
    episode = 'Day' + args.day[-2:]
    SOURCE = ROOT / 'devlog/youtube' / args.day / 'source/narration'
    OUTPUT = ROOT / 'devlog/youtube' / args.day / 'media/audio/narration'
    UNITY = ROOT / 'impls/unity/Afterlife/Assets/Devlog/Youtube' / episode / 'Audio/Narration'
    import numpy as np
    import soundfile as sf
    script = json.loads((SOURCE / 'script.json').read_text(encoding='utf-8'))
    if args.timing_only:
        if args.sample: raise ValueError('--timing-only와 --sample은 함께 사용할 수 없습니다.')
        old_manifest = json.loads((OUTPUT/'manifest.json').read_text(encoding='utf-8'))
        sample_rate = old_manifest['sampleRate']
    else:
        import onnxruntime as ort
        sys.path.insert(0, str(ROOT/'tools/tts/vendor'))
        from supertonic_helper import load_cfgs, load_onnx_all, load_text_processor, TextToSpeech, load_voice_style
        opts = ort.SessionOptions(); opts.intra_op_num_threads = 4; opts.inter_op_num_threads = 1
        model = str(CACHE / 'assets/onnx')
        tts = TextToSpeech(load_cfgs(model), load_text_processor(model), *load_onnx_all(model, opts, ['CPUExecutionProvider']))
        style = load_voice_style([str(CACHE / ('assets/voice_styles/' + script['voice'] + '.json'))])
        sample_rate = tts.sample_rate
    OUTPUT.mkdir(parents=True, exist_ok=True); UNITY.mkdir(parents=True, exist_ok=True)
    manifest = {'version':script['version'], 'engine':'Supertonic 3', 'modelRevision':'aafc6e32416a594460b32413efc49d7fe4ce6d46', 'voice':script['voice'], 'language':'ko', 'speed':script['speed'], 'steps':script['steps'], 'sampleRate':sample_rate, 'shots':[]}
    previous = {}
    if (OUTPUT/'manifest.json').exists():
        previous = {c['file']:c for s in json.loads((OUTPUT/'manifest.json').read_text(encoding='utf-8'))['shots'] for c in s['cues']}
    timeline = 0
    for index, shot in enumerate(script['shots']):
        if args.sample and not shot['lines']: continue
        record = dict(shot); record.pop('lines'); record['cues'] = []
        local = .75 if index == 0 else .55
        for line_index, line in enumerate(shot['lines']):
            filename = f"{shot['id']}-{line_index+1:02d}.wav"
            path = OUTPUT / filename
            signature = hashlib.sha256((line+str(script['speed'])+str(script['steps'])+script['voice']+manifest['modelRevision']).encode()).hexdigest()
            if not path.exists() or previous.get(filename,{}).get('signature') != signature:
                if args.timing_only: raise ValueError(f'재합성이 필요한 문장입니다: {filename}')
                np.random.seed(1200+index*10+line_index)
                wav, duration = tts(line, 'ko', style, script['steps'], script['speed'])
                wav = np.asarray(wav).reshape(-1)[:int(float(np.asarray(duration).reshape(-1)[0])*tts.sample_rate)]
                # DC 제거, 피크 -3dBFS 이하. 말소리 양끝을 자르지 않고 8ms 클릭 방지만 적용.
                wav = wav - wav.mean()
                peak = float(np.max(np.abs(wav)))
                if peak > 0: wav *= min(2.0, .707/peak)
                fade = min(len(wav)//2, int(tts.sample_rate*.008))
                wav[:fade] *= np.linspace(0,1,fade); wav[-fade:] *= np.linspace(1,0,fade)
                sf.write(path, wav, tts.sample_rate, subtype='PCM_16')
            wav, sr = sf.read(path); length = len(wav)/sr
            if sr != sample_rate: raise ValueError(f'Sample rate mismatch: {path}')
            if args.timing_only and hashlib.sha256(path.read_bytes()).hexdigest() != previous[filename]['sha256']:
                raise ValueError(f'Audio hash mismatch: {path}')
            if not np.isfinite(wav).all() or length < .3 or np.max(np.abs(wav)) < .01: raise ValueError(f'Invalid audio: {path}')
            cue = {'start':round(local,3),'duration':round(length+.35,3),'text':line,'file':filename,'audioSeconds':round(length,3),'signature':signature,'peak':round(float(np.max(np.abs(wav))),5),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
            record['cues'].append(cue); local += length+.55
            if not args.timing_only: shutil.copy2(path, UNITY/filename)
            print(filename, round(length,2), 'seconds', flush=True)
        end = local+(.9 if index==len(script['shots'])-1 else .55) if shot['lines'] else 0
        if 'postNarrationSeconds' in shot:
            tail = shot['postNarrationSeconds']
            if not shot['lines'] or not isinstance(tail,(int,float)) or not math.isfinite(tail) or tail < .35:
                raise ValueError('postNarrationSeconds는 음성이 있는 샷에 0.35초 이상이어야 합니다.')
            end = local-.55+tail
        record['duration'] = round(max(shot['minimumDuration'],end),2)
        record['start'] = round(timeline,2); timeline += record['duration']; manifest['shots'].append(record)
        if args.sample: break
    # 동일 조건 비교 컷은 더 긴 음성에 맞춰 두 화면의 재생 길이도 같게 한다.
    groups={}
    for shot in manifest['shots']:
        if shot.get('comparisonGroup'):
            key=shot['comparisonGroup'];groups[key]=max(groups.get(key,0),shot['duration'])
    timeline=0
    for shot in manifest['shots']:
        if shot.get('comparisonGroup'):shot['duration']=groups[shot['comparisonGroup']]
        shot['start']=round(timeline,2);timeline+=shot['duration']
    manifest['duration'] = round(timeline,2)
    name = 'sample-manifest.json' if args.sample else 'manifest.json'
    (OUTPUT/name).write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    if not args.sample:
        # JsonUtility용 필드 이름은 제작 manifest와 동일하다.
        (UNITY.parent.parent/'Data').mkdir(parents=True,exist_ok=True)
        write_text_retry(UNITY.parent.parent/'Data'/f'{episode}Narration.json',json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
        joined = np.zeros(round(timeline*sample_rate),dtype=np.float32)
        for shot in manifest['shots']:
            for cue in shot['cues']:
                data,_ = sf.read(OUTPUT/cue['file']); start=round((shot['start']+cue['start'])*sample_rate)
                joined[start:start+len(data)] = data
        sf.write(OUTPUT/f'{episode.lower()}-narration-preview.wav',joined,sample_rate,subtype='PCM_16')
        def stamp(seconds):
            ms=round(seconds*1000); h,ms=divmod(ms,3600000); m,ms=divmod(ms,60000); s,ms=divmod(ms,1000)
            return f'{h:02}:{m:02}:{s:02},{ms:03}'
        captions=[]
        for shot in manifest['shots']:
            for cue in shot['cues']:
                start=shot['start']+cue['start']
                captions.append(f'{len(captions)+1}\n{stamp(start)} --> {stamp(start+cue["duration"])}\n{cue["text"]}\n')
        (SOURCE/f'{episode.lower()}.ko.srt').write_text('\n'.join(captions),encoding='utf-8')
    print('Total seconds:',round(timeline,2),flush=True)

if __name__ == '__main__': main()
