"""기존 Film을 변경하지 않고 남성 프리셋별 느린 한국어 샘플 생성."""
from pathlib import Path
import argparse, sys, json
import numpy as np
import soundfile as sf
import onnxruntime as ort
ROOT=Path(__file__).resolve().parents[2]
CACHE=ROOT/'.tooling/youtube-tts/assets'
OUT=ROOT/'devlog/youtube/shared/media/audio/voice-comparison'
sys.path.insert(0,str(ROOT/'tools/tts/vendor'))
from supertonic_helper import load_cfgs,load_text_processor,load_onnx_all,TextToSpeech,load_voice_style

def main():
    global OUT
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=OUT,help='목소리 비교 파일 출력 폴더')
    OUT=parser.parse_args().output
    OUT.mkdir(parents=True,exist_ok=True)
    opts=ort.SessionOptions();opts.intra_op_num_threads=4;opts.inter_op_num_threads=1
    model=str(CACHE/'onnx')
    tts=TextToSpeech(load_cfgs(model),load_text_processor(model),*load_onnx_all(model,opts,['CPUExecutionProvider']))
    text='자원을 모아 마을을 키우고, 몰려오는 마물을 막는 게임을 만들고 있습니다.'
    records=[]
    for voice in ['M1','M2','M3','M4','M5']:
        np.random.seed(1200)
        style=load_voice_style([str(CACHE/'voice_styles'/f'{voice}.json')])
        wav,duration=tts(text,'ko',style,12,.95)
        wav=wav.reshape(-1)[:int(float(np.asarray(duration).reshape(-1)[0])*tts.sample_rate)]
        wav-=wav.mean()
        # 동일 RMS 목표와 피크 상한으로 음량 차이가 목소리 비교를 지배하지 않게 한다.
        rms=float(np.sqrt(np.mean(wav**2)));peak=float(np.max(np.abs(wav)))
        wav*=min(.09/max(rms,1e-8),.707/max(peak,1e-8))
        n=int(tts.sample_rate*.008);wav[:n]*=np.linspace(0,1,n);wav[-n:]*=np.linspace(1,0,n)
        path=OUT/f'{voice}-calm-095.wav';sf.write(path,wav,tts.sample_rate,subtype='PCM_16')
        assert np.isfinite(wav).all() and np.max(np.abs(wav))>.01
        records.append({'voice':voice,'file':path.name,'seconds':len(wav)/tts.sample_rate,'speed':.95,'steps':12,'pitchShift':0})
        print(voice,round(len(wav)/tts.sample_rate,2),flush=True)
    (OUT/'manifest.json').write_text(json.dumps({'text':text,'engine':'Supertonic 3','aiGenerated':True,'samples':records},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
if __name__=='__main__':main()
