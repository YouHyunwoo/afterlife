"""트랙 기반 렌더링. 영상 레이어, 독립 오디오/자막, 빈 구간을 동일 시간축에 배치."""
import math
from pathlib import Path
import numpy as np
import soundfile as sf
from project import resolve, validate
from timeline import clips, duration, video_segments


def mix(project, project_file, out, cancel=None):
    from render import RATE, decode, probe, smooth
    validate(project,project_file,files=True)
    total=duration(project);n=round(total*RATE);audio=np.zeros((n,2),np.float32)
    def cancelled():
        if cancel and cancel.is_set():raise InterruptedError('취소했습니다.')
    def add(data,start,gain=1):
        first=round(start*RATE);count=min(len(data),n-first)
        if count>0:audio[first:first+count]+=data[:count]*gain
    # 위에서 실제로 보이는 영상의 원음만 사용한다.
    info={}
    for start,end,c in video_segments(project):
        cancelled()
        if c is None or not project['video']['volume'] or not c.get('volume',1):continue
        path=resolve(project_file,c['path'])
        if str(path) not in info:info[str(path)]=probe(path)
        if info[str(path)]['audio']:
            part=decode(path,end-start,c['sourceIn']+start-c['start'])
            add(part,start,project['video']['volume']*c.get('volume',1))
    for _,c in clips(project,'audio'):
        cancelled();length=round(c['duration']*RATE)
        part=decode(resolve(project_file,c['path']),c['duration'],c['sourceIn'])
        if not len(part):raise ValueError(c['title']+': 선택한 구간에 음성이 없습니다.')
        if c.get('loop'):
            overlap=min(4*RATE,len(part)//4);step=max(1,len(part)-overlap)
            data=np.zeros((length,2),np.float32)
            for first in range(0,length,step):
                repeat=part.copy()
                if first and overlap:repeat[:overlap]*=np.linspace(0,1,overlap)[:,None]
                if first+len(repeat)<length and overlap:repeat[-overlap:]*=np.linspace(1,0,overlap)[:,None]
                count=min(len(repeat),length-first);data[first:first+count]+=repeat[:count]
        else:
            data=np.zeros((length,2),np.float32);count=min(length,len(part));data[:count]=part[:count]
        gain=c.get('volume',1)
        if c.get('role')=='narration':gain*=project['narrationVolume']
        if c.get('role')=='music':
            settings=project['music'];t=np.arange(length,dtype=np.float32)/RATE
            envelope=np.full(length,settings['volume'],np.float32)
            for start,end,v in video_segments(project):
                if v and v.get('credits'):
                    mask=(t+c['start']>=start)&(t+c['start']<end)
                    envelope[mask]*=1+(settings['creditsMultiplier']-1)*smooth((t[mask]+c['start']-v['start'])/max(.01,settings.get('creditsRamp',2)))
            envelope*=np.clip(t/max(.01,settings.get('fadeIn',2)),0,1)*np.clip((c['duration']-t)/max(.01,settings.get('fadeOut',3)),0,1)
            data*=envelope[:,None]
        add(data,c['start'],gain)
    peak=float(np.max(np.abs(audio)));notes=[]
    if peak>.98:
        audio*=.98/peak;notes.append(f'합산 피크 보호: {20*math.log10(.98/peak):.1f} dB')
    sf.write(out,audio,RATE,subtype='PCM_16')
    return dict(duration=total,samples=n,peakBeforeLimit=peak,notes=notes)


def subtitles(project,out):
    # 기존 자막 작성기의 포맷을 사용하되 독립 자막 클립을 입력한다.
    from render import subtitles as legacy_subtitles
    rows=[]
    for _,c in clips(project,'subtitle'):
        rows.append(dict(timelineStart=c['start'],duration=c['duration'],cues=[dict(start=0,duration=c['duration'],text=c['text'])]))
    from aspect_ratio import dimensions
    legacy_subtitles(dict(shots=rows),out,*dimensions(project['export']))


def export(project,project_file,output,work,progress=lambda x:None,cancel=None):
    from render import execute,probe,RATE,publish_output
    validate(project,project_file,files=True)
    output=Path(output);work=Path(work);work.mkdir(parents=True,exist_ok=True)
    if output.exists():raise ValueError('같은 이름의 파일이 있습니다. 다른 이름으로 내보내세요.')
    from aspect_ratio import dimensions
    total=duration(project);width,height=dimensions(project['export']);fps=project['export']['fps']
    info={}
    for _,c in clips(project,'video'):
        source=resolve(project_file,c['path']);key=str(source)
        if key not in info:info[key]=probe(source)
        limit=info[key]['duration']
        if c['sourceIn']>=limit:raise ValueError(c['title']+': 원본 시작이 파일 끝을 넘습니다.')
        if c['sourceIn']+c['duration']>limit+.05 and not c.get('padEnd'):
            raise ValueError(c['title']+': 원본 구간이 파일 길이를 넘습니다.')
    progress(dict(phase='독립 오디오 트랙 합성',percent=0))
    report=mix(project,project_file,work/'mix.wav',cancel)
    parts=[]
    for i,(start,end,c) in enumerate(video_segments(project)):
        if cancel and cancel.is_set():raise InterruptedError('취소했습니다.')
        frames=round(end*fps)-round(start*fps)
        if frames<=0:continue
        part=work/f'part-{i:04}.mp4';parts.append(part)
        args=['-v','error','-y']
        if c is None:
            args+=['-f','lavfi','-i',f'color=c=black:s={width}x{height}:r={fps}']
        else:
            src=resolve(project_file,c['path']);source_time=c['sourceIn']+start-c['start']
            # 이전 레이어가 끝난 뒤 파일 끝을 넘긴 아래 레이어는 끝 프레임을 유지한다.
            source_time=min(source_time,max(0,info[str(src)]['duration']-1/fps))
            args+=['-ss',str(source_time),'-i',str(src),'-vf',f'scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2,setsar=1,fps={fps},tpad=stop_mode=clone:stop_duration={end-start}']
        args+=['-frames:v',str(frames),'-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4',str(part)]
        execute(args,cancel,work/'ffmpeg.log')
        progress(dict(phase=f'영상 구간 {i+1}',percent=round(end/total*90,1)))
    (work/'concat.txt').write_text('\n'.join(f"file '{p.name}'" for p in parts),encoding='utf-8')
    args=['-v','error','-y','-f','concat','-safe','0','-i','concat.txt','-i','mix.wav','-map','0:v:0','-map','1:a:0','-t',str(total)]
    if project['export']['subtitles']:
        subtitles(project,work/'subtitles.ass')
        args+=['-vf','subtitles=subtitles.ass','-c:v','libx264','-preset','fast','-crf','18','-threads','4']
    else:args+=['-c:v','copy']
    args+=['-c:a','aac','-b:a','192k','-ar',str(RATE),'-movflags','+faststart','export.mp4']
    execute(args,cancel,work/'ffmpeg.log',work)
    publish_output(work/'export.mp4',output)
    progress(dict(phase='완료',percent=100,output=str(output)));return report
