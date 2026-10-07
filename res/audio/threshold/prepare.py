from pathlib import Path
import sys,json,hashlib,shutil
import numpy as np
from scipy.signal import butter,sosfiltfilt,resample_poly
HERE=Path(__file__).resolve().parent; ROOT=HERE.parents[2]
sys.path.insert(0,str(ROOT/'impls/unity/Afterlife/Temp/threshold-audio-deps'))
import soundfile as sf
RATE=44100
TARGET=ROOT/'impls/unity/Afterlife/Assets/Samples/ThresholdLab/Audio'
TARGET.mkdir(parents=True,exist_ok=True)
OUT=HERE/'edited'; OUT.mkdir(exist_ok=True)
report=[]
def read(key):
 a,sr=sf.read(HERE/(key+'_source.mp3'),dtype='float32',always_2d=True)
 if sr!=RATE:
  from math import gcd
  g=gcd(sr,RATE);a=resample_poly(a,RATE//g,sr//g,axis=0)
 return a-a.mean(axis=0)
def level(a,rms=-22,peak=-4):
 return a*min(10**(rms/20)/max(1e-9,np.sqrt(np.mean(a*a))),10**(peak/20)/max(1e-9,np.max(np.abs(a))))
def fade(a,start=.015,end=.12):
 a=a.copy();n=min(int(start*RATE),len(a)//2);m=min(int(end*RATE),len(a)//2)
 a[:n]*=np.linspace(0,1,n)[:,None];a[-m:]*=np.linspace(1,0,m)[:,None];return a
def loop(a,seconds):
 n=int(seconds*RATE);w=np.linspace(0,1,n)[:,None]
 seam=a[-n:]*(1-w)+a[:n]*w
 return np.concatenate([a[n:-n],seam])
def write(name,a,looped,details):
 a=level(a,-23 if name.startswith('music') else -20,-4)
 ext='.wav'
 out=OUT/(name+ext)
 sf.write(out,a,RATE,**({'format':'OGG','subtype':'VORBIS'} if ext=='.ogg' else {'subtype':'PCM_16'}))
 shutil.copy2(out,TARGET/out.name)
 decoded,sr=sf.read(out,always_2d=True)
 report.append(dict(file=out.name,seconds=len(decoded)/sr,peak_dbfs=float(20*np.log10(max(np.max(np.abs(decoded)),1e-9))),rms_dbfs=float(20*np.log10(max(np.sqrt(np.mean(decoded**2)),1e-9))),loop=looped,seam_delta=float(np.max(np.abs(decoded[0]-decoded[-1]))) if looped else None,edits=details,sha256=hashlib.sha256(out.read_bytes()).hexdigest()))
water=read('water')
# Slow the actual bubbles, rather than layering a synthetic bass hum.
water=resample_poly(water,5,4,axis=0)
water=sosfiltfilt(butter(2,[65,2400],btype='bandpass',fs=RATE,output='sos'),water,axis=0)
write('water_loop',loop(water,1.2),True,'Playback slowed to 80%; 65-2400 Hz; DC removed; 1.2 s overlap seam; level matched')
enter=water[int(2.0*RATE):int(3.15*RATE)]
write('water_enter',fade(enter,.025,.23),False,'Slowed/filtered source 2.0-3.15 s; fades 25/230 ms')
write('water_exit',fade(enter[::-1],.08,.25),False,'Reverse of edited water entry; fades 80/250 ms')
whoosh=read('whoosh');whoosh=resample_poly(whoosh,4,3,axis=0)
write('camera_whoosh',fade(whoosh,.025,.10),False,'Original whoosh slowed to 75%; DC removal; fades 25/100 ms')
for key,start,duration in [('afterlife',20,120),('night',12,120),('title',8,120),('playing',4,112),('over',12,120),('clear',35,112)]:
 a=read(key);a=a[int(start*RATE):int((start+duration)*RATE)]
 name={'afterlife':'title','title':'day'}.get(key,key)
 write('music_'+name,loop(a,5),True,f'{key} source excerpt {start}-{start+duration} s; DC removed; 5 s crossfade loop; level matched; PCM16 WAV (Unity importer compresses music)')
(HERE/'analysis.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for x in report: print(x['file'],round(x['seconds'],2),'peak',round(x['peak_dbfs'],2),'seam',x['seam_delta'])
