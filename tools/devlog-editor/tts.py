"""편집기 공용 로컬 Supertonic 3 음성 합성. Day01 스크립트와 독립."""
import json
import time
from pathlib import Path
import numpy as np
import soundfile as sf
import onnxruntime as ort
from runtime_paths import APP_DIR, DATA_DIR, FROZEN
from third_party.supertonic_helper import (
    load_cfgs, load_onnx_all, load_text_processor, TextToSpeech, load_voice_style)

MODEL_REVISION='aafc6e32416a594460b32413efc49d7fe4ce6d46'
VOICES=tuple(f'M{i}' for i in range(1,6))
ASSETS=APP_DIR/'tts_assets' if FROZEN else DATA_DIR/'tts_assets'


def synthesize(texts, output_folder, voice='M1', speed=.95, steps=12, assets=None):
    if voice not in VOICES:raise ValueError('음성은 M1~M5 중에서 선택해주세요.')
    speed=float(speed)
    if not .5<=speed<=1.5:raise ValueError('음성 속도는 0.5~1.5 범위로 입력해주세요.')
    if not 1<=len(texts)<=200 or any(not text or len(text)>300 for text in texts):
        raise ValueError('대본은 1~200문장, 문장당 300자 이내여야 합니다.')
    folder=Path(assets or ASSETS)
    required=[folder/'onnx/tts.json',folder/'onnx/vocoder.onnx',folder/'LICENSE',folder/'voice_styles'/f'{voice}.json']
    if any(not p.is_file() for p in required):
        raise FileNotFoundError('Supertonic 모델이 없습니다. 편집기 setup.cmd를 실행해 모델을 준비해주세요.')
    opts=ort.SessionOptions();opts.intra_op_num_threads=4;opts.inter_op_num_threads=1
    onnx_dir=str(folder/'onnx')
    engine=TextToSpeech(load_cfgs(onnx_dir),load_text_processor(onnx_dir),
                        *load_onnx_all(onnx_dir,opts,['CPUExecutionProvider']))
    style=load_voice_style([str(folder/'voice_styles'/f'{voice}.json')])
    destination=Path(output_folder);destination.mkdir(parents=True,exist_ok=False)
    outputs=[];started=time.monotonic()
    for index,text in enumerate(texts):
        if time.monotonic()-started>600:raise TimeoutError('음성 생성 시간이 10분을 넘었습니다.')
        np.random.seed(1200+index)
        wave,duration=engine(text,'ko',style,steps,speed)
        wave=np.asarray(wave,dtype=np.float32).reshape(-1)
        wave=wave[:int(float(np.asarray(duration).reshape(-1)[0])*engine.sample_rate)]
        if not len(wave) or not np.isfinite(wave).all():raise ValueError('음성 생성 결과가 올바르지 않습니다.')
        wave=wave-wave.mean()
        peak=float(np.max(np.abs(wave)))
        if peak>0:wave*=min(2.0,.707/peak)
        fade=min(len(wave)//2,int(engine.sample_rate*.008))
        if fade:
            wave[:fade]*=np.linspace(0,1,fade)
            wave[-fade:]*=np.linspace(1,0,fade)
        path=destination/f'{index+1:03}.wav'
        sf.write(path,wave,engine.sample_rate,subtype='PCM_16')
        outputs.append((path,len(wave)/engine.sample_rate))
    (destination/'provenance.json').write_text(json.dumps({
        'engine':'Supertonic 3','modelRevision':MODEL_REVISION,'voice':voice,
        'language':'ko','speed':speed,'steps':steps,'aiGenerated':True,'texts':texts,
    },ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return outputs
