"""개발일지 전용 제작 명령. 결과는 표준 영상/음성/자막 트랙이다."""
import math
import re
import uuid
from project import relative, resolve
from storage import media_root, writable
from tts import synthesize


def uid():return uuid.uuid4().hex


def number(value, low, high):
    n=float(value)
    if not math.isfinite(n) or not low<=n<=high:raise ValueError(f'시간은 {low}~{high} 범위로 입력해주세요.')
    return n


def track(kind,name):return dict(id=uid(),kind=kind,name=name,enabled=True,clips=[])


def subtitle(text,start,duration):
    return dict(id=uid(),title=text[:60],text=text,start=start,duration=duration)


def lines(text):
    if not isinstance(text,str) or len(text)>100_000:raise ValueError('대본은 10만 자 이내로 입력해주세요.')
    result=[re.sub(r'^#{1,6}\s+','',line.strip()) for line in text.splitlines() if line.strip()]
    if not result or len(result)>200:raise ValueError('대본을 1~200줄 입력해주세요.')
    return result


def parse_srt(text, offset):
    rows=[]
    def seconds(parts):
        if int(parts[1])>=60 or int(parts[2])>=60:raise ValueError('SRT 분/초는 0~59 범위여야 합니다.')
        return int(parts[0])*3600+int(parts[1])*60+int(parts[2])+int(parts[3])/1000
    for block in re.split(r'\n\s*\n',text.strip().replace('\r','').lstrip('\ufeff')):
        pieces=block.splitlines()
        if pieces and pieces[0].isdigit():pieces=pieces[1:]
        if len(pieces)<2:raise ValueError('SRT 시간과 자막 본문을 확인해주세요.')
        match=re.fullmatch(r'(\d+):(\d{2}):(\d{2})[,.](\d{3})\s*-->\s*(\d+):(\d{2}):(\d{2})[,.](\d{3})',pieces[0])
        if not match:raise ValueError('SRT 시간 형식이 올바르지 않습니다.')
        start=seconds(match.groups()[:4]);end=seconds(match.groups()[4:])
        if end<=start:raise ValueError('SRT 끝 시간은 시작보다 커야 합니다.')
        rows.append(subtitle('\n'.join(pieces[1:]),start+offset,end-start))
    return rows


def run(action,project,project_file,options):
    start=number(options.get('start',0),0,3599)
    added=[];text=options.get('text','')
    if action=='srt':
        if not isinstance(text,str) or len(text)>100_000:raise ValueError('SRT는 10만 자 이하로 입력해주세요.')
        item=track('subtitle','개발일지 · SRT');item['clips']=parse_srt(text,start);added.append(item)
    elif action in ('script','credits','narration'):
        rows=lines(text);length=number(options.get('seconds',4),.1,120)
        item=track('subtitle','개발일지 · 크레딧' if action=='credits' else '개발일지 · 자막')
        if action=='credits':item['clips']=[subtitle('\n'.join(rows),start,length)]
        elif action=='script':item['clips']=[subtitle(t,start+i*length,length) for i,t in enumerate(rows)]
        else:
            speed=number(options.get('speed',.95),.5,1.5)
            voice=str(options.get('voice','M1')).strip() or 'M1'
            outputs=synthesize(rows,writable(media_root(project_file)/'audio/narration'/uid()),voice=voice,speed=speed)
            audio=track('audio','개발일지 · 나레이션')
            for t,(path,duration) in zip(rows,outputs):
                media=relative(project_file,path)
                group=uid()
                audio['clips'].append(dict(id=uid(),title=t[:60],start=start,duration=duration,path=media,sourceIn=0,volume=1,role='narration',loop=False,linkGroup=group))
                caption=subtitle(t,start,duration);caption['linkGroup']=group;item['clips'].append(caption)
                project.setdefault('assets',[]).append(dict(id=uid(),kind='audio',name=t[:60],path=media,duration=duration,
                                                             aiGenerated=True,generatedWith='Supertonic 3',voice=voice))
                start+=duration+.3
            added.append(audio)
        added.append(item)
    elif action in ('sequence','comparison'):
        selected=options.get('assets',[])
        if not selected or len(selected)>200 or len(selected)!=len(set(selected)) or action=='comparison' and len(selected)!=2:
            raise ValueError('비교 장면은 영상 2개, 녹화본 배치는 1~200개를 선택해주세요.')
        available={a['id']:a for a in project.get('assets',[]) if a['kind']=='video'}
        from render import probe
        item=track('video','개발일지 · 이전/현재' if action=='comparison' else '개발일지 · 녹화본')
        captions=track('subtitle','개발일지 · 비교 제목') if action=='comparison' else None
        scene_length=number(options.get('seconds',4),.1,120) if captions else None
        for index,key in enumerate(selected):
            if key not in available:raise ValueError('선택한 영상 에셋을 찾을 수 없습니다.')
            asset=available[key];length=probe(resolve(project_file,asset['path']))['duration']
            if scene_length is not None:length=min(length,scene_length)
            if length<.04:raise ValueError('비교 영상 길이가 너무 짧습니다.')
            item['clips'].append(dict(id=uid(),title=asset['name'],start=start,duration=length,path=asset['path'],sourceIn=0,volume=1,padEnd=False))
            if captions:captions['clips'].append(subtitle('이전' if index==0 else '현재',start,length))
            start+=length
        added.append(item)
        if captions:added.append(captions)
    else:raise ValueError('지원하지 않는 작업입니다.')
    project['tracks']=added+project['tracks']
    if any(t['kind']=='subtitle' for t in added):project['export']['subtitles']=True
    return dict(project=project,summary=f'{len(added)}개 트랙을 추가했습니다. 되돌리기로 취소할 수 있습니다.')
