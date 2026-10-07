"""선택한 나무 쓰러짐 효과음의 보수적인 잡음 억제/피치 다운/고음 완화."""
from pathlib import Path
import argparse
import json
import numpy as np
from scipy.io import wavfile
from scipy import signal, ndimage

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--input', type=Path, required=True, help='ecfike 원본 WAV 경로')
args=parser.parse_args()
if not args.input.is_file():parser.error('입력 WAV를 찾을 수 없습니다: '+str(args.input))
DIR=ROOT/'res/audio/tree-fall-analysis'
rate,pcm=wavfile.read(DIR/'02_tree_fall_short.wav')
x=pcm.astype(np.float64)/32768
original_rate,original_pcm=wavfile.read(args.input)
assert original_rate==rate and pcm.dtype==np.int16
original=original_pcm.astype(np.float64)/32768
# 약한 후반부에는 자연 잔향도 섞여 있으므로 최대 억제를 8dB로 제한한다.
noise=original[round(4.05*rate):round(4.40*rate)]
_,_,s=signal.stft(x,fs=rate,nperseg=1024,noverlap=768,axis=0)
_,_,n=signal.stft(noise,fs=rate,nperseg=1024,noverlap=768,axis=0)
power=np.mean(abs(s)**2,axis=1)
profile=np.median(np.mean(abs(n)**2,axis=1),axis=-1)[:,None]
gain=np.sqrt(np.maximum(1-1.2*profile/(power+1e-12),10**(-8/10)))
gain=ndimage.gaussian_filter(gain,sigma=(1.2,1.0))
_,clean=signal.istft(s*gain[:,None,:],fs=rate,nperseg=1024,noverlap=768,time_axis=-1,freq_axis=0)
clean=clean.T[:len(x)]
# 1091/1000倍の長さにすることで約-1.51半音。時間伸長の位相処理を使わず衝撃を保つ。
y=signal.resample_poly(clean,1091,1000,axis=0)
y=signal.sosfiltfilt(signal.butter(2,55,fs=rate,btype='highpass',output='sos'),y,axis=0)
y=signal.sosfiltfilt(signal.butter(2,3400,fs=rate,btype='lowpass',output='sos'),y,axis=0)
# 低域を控えめに補い、突出したピークを軽く丸める。
warm=signal.sosfiltfilt(signal.butter(2,280,fs=rate,btype='lowpass',output='sos'),y,axis=0)
y=np.tanh((y+.12*warm)*1.15)/1.15
rms=lambda a:float(np.sqrt(np.mean(a*a)))
gain_out=min(rms(x)/max(rms(y),1e-12),10**(-2/20)/np.max(abs(y)))
y*=gain_out
fadein=round(rate*.003); fadeout=round(rate*.10)
y[:fadein]*=np.linspace(0,1,fadein)[:,None]
y[-fadeout:]*=np.linspace(1,0,fadeout)[:,None]
out=np.round(np.clip(y,-1,32767/32768)*32768).astype(np.int16)
name='02_tree_fall_short_processed.wav'
wavfile.write(DIR/name,rate,out)
preview=np.concatenate([pcm,np.zeros((round(rate*.7),2),dtype=np.int16),out])
wavfile.write(DIR/'preview_original_then_processed.wav',rate,preview)
metrics={'source':'02_tree_fall_short.wav','output':name,'sample_rate':rate,'duration_seconds':len(out)/rate,'pitch_semitones':-12*np.log2(1.091),'noise_attenuation_limit_db':8,'highpass_hz':55,'lowpass_hz':3400,'original_rms':rms(x),'processed_rms':rms(y),'peak_dbfs':20*np.log10(float(np.max(abs(y))))}
(DIR/'processing.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
assert out.ndim==2 and out.shape[1]==2 and np.max(abs(out.astype(np.int32)))<32767
assert np.all(out[0]==0) and np.all(out[-1]==0)
print(json.dumps(metrics,indent=2))
