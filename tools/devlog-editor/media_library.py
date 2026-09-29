"""브라우저 파일 선택에서 전달된 미디어를 로컬 리소스로 보관하고 등록한다."""
import hashlib
import re
import uuid
from pathlib import Path
from project import ROOT, relative, resolve
from storage import import_folder, writable

EXTENSIONS={'video':{'.mp4','.mov','.mkv','.webm','.m4v'},'audio':{'.wav','.mp3','.ogg','.flac','.m4a'}}


def describe(source,kind,project_file,registry):
    from render import run,probe
    source=resolve(project_file,source)
    if not source.is_file():raise ValueError('원본 파일을 찾을 수 없습니다: '+str(source))
    if kind=='video':info=probe(source)
    elif kind=='audio':
        result=run(['-i',str(source)],capture_output=True,text=True,encoding='utf-8',errors='replace')
        match=re.search(r'Duration: (\d+):(\d+):([\d.]+)',result.stderr)
        if not match or 'Audio:' not in result.stderr:raise ValueError('재생 가능한 오디오가 아닙니다.')
        info=dict(duration=int(match[1])*3600+int(match[2])*60+float(match[3]))
    else:raise ValueError('지원하지 않는 미디어 종류입니다.')
    key=hashlib.sha256(str(source).encode()).hexdigest();registry[key]=source
    return dict(path=relative(project_file,source),name=source.name,kind=kind,url='/media/source/'+key,**info)


def import_stream(stream,length,name,kind,project_file,registry):
    name=Path(name.replace('\\','/')).name
    suffix=Path(name).suffix.lower()
    if suffix not in EXTENSIONS.get(kind,set()):raise ValueError('지원하지 않는 미디어 확장자입니다.')
    if not 0<length<=20*1024**3:raise ValueError('빈 파일이거나 파일이 20GB를 넘습니다.')
    folder=writable(import_folder(project_file,kind))
    folder.mkdir(parents=True,exist_ok=True)
    temp=folder/(uuid.uuid4().hex+'.part'+suffix)
    digest=hashlib.sha256()
    try:
        with temp.open('wb') as f:
            remaining=length
            while remaining:
                chunk=stream.read(min(1024*1024,remaining))
                if not chunk:raise ValueError('파일 전송이 중단됐습니다. 다시 불러오세요.')
                f.write(chunk);digest.update(chunk);remaining-=len(chunk)
        # 검증 실패 파일은 영구 에셋으로 등록하지 않는다.
        describe(temp,kind,project_file,{})
        target=folder/(digest.hexdigest()+suffix)
        if target.exists():temp.unlink()
        else:temp.replace(target)
        result=describe(target,kind,project_file,registry);result['name']=name
        return result
    finally:
        if temp.exists():temp.unlink()
