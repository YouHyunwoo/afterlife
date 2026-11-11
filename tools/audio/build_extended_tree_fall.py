"""합성 착지음을 제외하고 원본 사용 구간을 두 배로 확장한다."""
from pathlib import Path
import json
import numpy as np
from scipy import signal
from scipy.io import wavfile
root=Path(__file__).resolve().parents[2]
folder=root/'res/audio/tree-fall-analysis'
rate,pcm=wavfile.read(folder/'02_tree_fall_short.wav')
# 피치 유지 상태에서 정확히 1초가 되도록 원본 구간을 확장한다.
source_end=.24+1/1.091
x=pcm[round(.24*rate):round(source_end*rate)+1].astype(np.float64).mean(axis=1)/32768
x=signal.resample_poly(x,1091,1000)
x=x[:rate]
x=signal.sosfiltfilt(signal.butter(2,[120,2200],btype='bandpass',fs=rate,output='sos'),x)
attack=round(.008*rate); release=round(.12*rate)
x[:attack]*=np.linspace(0,1,attack)
x[-release:]*=np.linspace(1,0,release)**1.5
x*=.46/max(np.max(abs(x)),1e-9)
out=np.round(np.column_stack([x,x])*32768).astype(np.int16)
name='04_tree_fall_extended_no_thud.wav'
wavfile.write(folder/name,rate,out)
assert np.max(abs(out.astype(np.int32)))<32767 and np.all(out[0]==0) and np.all(out[-1]==0)
metrics={'file':name,'duration':len(out)/rate,'original_source_range':[.72,.48+source_end],'pitch_semitones':float(-12*np.log2(1.091)),'synthetic_thud':False}
(folder/'extended_edit.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print(json.dumps(metrics))
