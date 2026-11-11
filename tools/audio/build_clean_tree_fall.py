"""짧은 원본 파열 후보와 잔향 없는 합성 착지음을 조합한다."""
from pathlib import Path
import json
import numpy as np
from scipy import signal
from scipy.io import wavfile

root=Path(__file__).resolve().parents[2]
folder=root/'res/audio/tree-fall-analysis'
rate,pcm=wavfile.read(folder/'02_tree_fall_short.wav')
x=pcm.astype(np.float64)/32768
# 원본 전체 기준 0.72~1.04초. 잔향을 추정 제거하지 않고 짧게 편집한다.
crack=x[round(.24*rate):round(.56*rate)].mean(axis=1)
crack=signal.resample_poly(crack,1091,1000)
crack=signal.sosfiltfilt(signal.butter(2,[120,2200],btype='bandpass',fs=rate,output='sos'),crack)
attack=round(.008*rate); release=round(.12*rate)
crack[:attack]*=np.linspace(0,1,attack)
crack[-release:]*=np.linspace(1,0,release)**1.5
crack*=.46/max(np.max(np.abs(crack)),1e-9)
# 1.1초의 넘어짐 모션 끝에 맞춘 짧은 저역 충격. 에코/리버브 없음.
t=np.arange(round(.38*rate))/rate
rng=np.random.default_rng(139952)
noise=signal.sosfiltfilt(signal.butter(2,[70,700],btype='bandpass',fs=rate,output='sos'),rng.normal(size=len(t)))
noise/=max(np.max(np.abs(noise)),1e-9)
body=np.sin(2*np.pi*83*t)*np.exp(-t*20)+.28*np.sin(2*np.pi*137*t)*np.exp(-t*29)
thud=(body*.48+noise*.52)*np.exp(-t*9)*(1-np.exp(-t*400))
thud[-round(rate*.06):]*=np.linspace(1,0,round(rate*.06))
thud*=.68/max(np.max(np.abs(thud)),1e-9)
y=np.zeros(round(1.50*rate))
y[:len(crack)]+=crack
start=round(1.1*rate)
y[start:start+len(thud)]+=thud
y=np.column_stack([y,y])
out=np.round(y*32768).astype(np.int16)
name='03_tree_fall_clean_crack_thud.wav'
wavfile.write(folder/name,rate,out)
_,previous=wavfile.read(folder/'02_tree_fall_short_processed.wav')
wavfile.write(folder/'preview_processed_then_clean.wav',rate,np.concatenate([previous,np.zeros((round(.7*rate),2),np.int16),out]))
assert np.max(np.abs(out.astype(np.int32)))<32767 and np.all(out[0]==0) and np.all(out[-1]==0)
metrics={'output':name,'duration':len(out)/rate,'crack_source_original_seconds':[.72,1.04],'crack_duration':len(crack)/rate,'thud_start':1.1,'thud_duration':.38,'peak_dbfs':float(20*np.log10(np.max(abs(y)))),'reverb':False,'spectral_denoising':False}
(folder/'clean_edit.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print(json.dumps(metrics,indent=2))
