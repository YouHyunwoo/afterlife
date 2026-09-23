"""로컬 전용 편집기 서버. 네트워크 공개·Unity 실행 없이 동작한다."""
import argparse
import copy
import json
import mimetypes
import re
import secrets
import threading
import time
import traceback
import hashlib
import webbrowser
import uuid
from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlparse, unquote
from project import ROOT, DEFAULT, load, save, from_day01, rebase, resolve, relative, duration, validate, create_project, browse_projects, blank_project, portable_project, has_relative_media
from render import mix, export, probe
from timeline import migrate
from media_library import describe, import_stream

HERE=Path(__file__).parent
CACHE=ROOT/'.tooling/devlog-editor'
TOKEN=secrets.token_urlsafe(24)
STATE={'projectPath':str(DEFAULT),'project':None,'job':{'busy':False},'preview':None}
LOCK=threading.Lock()
CANCEL=threading.Event()
MEDIA={}

def inside_write(path):
    p=Path(path).resolve()
    if not p.is_relative_to(ROOT): raise ValueError('저장 위치는 프로젝트 폴더 안으로 지정해주세요.')
    return p

class Handler(BaseHTTPRequestHandler):
    def log_message(self,*args): pass
    def json(self,data,status=200):
        raw=json.dumps(data,ensure_ascii=False).encode();self.send_response(status);self.send_header('Content-Type','application/json; charset=utf-8');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
    def do_GET(self):
        try:
            path=urlparse(self.path).path
            if path=='/':
                raw=(HERE/'ui.html').read_text(encoding='utf-8').replace('__TOKEN__',TOKEN).encode()
                self.send_response(200);self.send_header('Content-Type','text/html; charset=utf-8');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
            elif path=='/api/state':
                self.json(STATE)
            elif path in ('/editor.js','/editor.css'):
                self.file(HERE/path[1:])
            elif path.startswith('/media/source/') and path.split('/')[-1] in MEDIA:
                self.file(MEDIA[path.split('/')[-1]])
            elif path=='/api/job':self.json(STATE['job'])
            elif path=='/media/video':self.file(resolve(STATE['projectPath'],STATE['project']['video']['path']))
            elif path=='/media/preview' and STATE['preview']:self.file(Path(STATE['preview']))
            elif path=='/media/export' and STATE['job'].get('output'):self.file(Path(STATE['job']['output']))
            else:self.send_error(404)
        except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass
        except Exception as e:self.json({'error':str(e)},400)
    def file(self,path):
        size=path.stat().st_size;start=0;end=size-1
        header=self.headers.get('Range')
        if header:
            match=re.fullmatch(r'bytes=(\d+)-(\d*)',header)
            if not match:self.send_error(416);return
            start=int(match[1]);end=min(end,int(match[2])) if match[2] else end
            if start>end:self.send_error(416);return
        self.send_response(206 if header else 200)
        self.send_header('Content-Type',mimetypes.guess_type(path)[0] or 'application/octet-stream')
        self.send_header('Accept-Ranges','bytes');self.send_header('Cache-Control','no-store')
        if header:self.send_header('Content-Range',f'bytes {start}-{end}/{size}')
        self.send_header('Content-Length',str(end-start+1));self.end_headers()
        with path.open('rb') as f:
            f.seek(start);remaining=end-start+1
            while remaining:
                chunk=f.read(min(256*1024,remaining))
                if not chunk:break
                self.wfile.write(chunk);remaining-=len(chunk)
    def do_POST(self):
        try:
            if self.headers.get('X-Editor-Token')!=TOKEN:self.json({'error':'로컬 편집기 인증 오류'},403);return
            length=int(self.headers.get('Content-Length',0))
            if urlparse(self.path).path=='/api/upload':
                if STATE['job'].get('busy'):raise ValueError('진행 중인 작업이 끝난 뒤 불러오세요.')
                self.json(import_stream(self.rfile,length,unquote(self.headers.get('X-File-Name','')),
                    self.headers.get('X-Media-Kind','video'),STATE['projectPath'],MEDIA));return
            if not 0<length<2_000_000:raise ValueError('요청 크기가 올바르지 않습니다.')
            data=json.loads(self.rfile.read(length));route=urlparse(self.path).path
            if route=='/api/cancel':CANCEL.set();self.json({'ok':True});return
            if STATE['job'].get('busy'):raise ValueError('진행 중인 작업이 끝나거나 취소된 뒤 변경해주세요.')
            if route=='/api/browse':self.json(browse_projects(data.get('path') or DEFAULT.parent));return
            if route=='/api/pick':raise ValueError('편집기 화면을 새로고침해 새 경로 선택 창을 사용해주세요.')
            if route=='/api/create':
                path,p=create_project(data.get('folder',''),data.get('name',''))
                STATE.update(projectPath=str(path),project=p,preview=None,job={'busy':False})
                self.json(STATE);return
            if route=='/api/blank':
                self.json({'project':blank_project(data.get('name',''))});return
            if route=='/api/media':
                self.json(describe(data['path'],data.get('kind','video'),STATE['projectPath'],MEDIA));return
            if route=='/api/load':
                path=Path(data['path']).resolve();p=migrate(load(path),path)
                STATE.update(projectPath=str(path),project=p,preview=None)
                self.json(STATE);return
            if route=='/api/load-document':
                p=data['project']
                if has_relative_media(p):
                    raise ValueError('이 파일은 상대 경로 리소스를 사용합니다. 작업 폴더에서 열기를 이용해 원래 위치에서 열어주세요.')
                path=CACHE/'browser-projects'/(uuid.uuid4().hex+'.devfilm.json')
                p=migrate(p,path);validate(p,path);save(path,p)
                STATE.update(projectPath=str(path),project=p,preview=None,job={'busy':False})
                self.json(STATE);return
            if route=='/api/video':
                path=Path(data['path']).resolve();info=probe(path)
                p=data['project'];p['video']['path']=relative(STATE['projectPath'],path)
                validate(p,STATE['projectPath']);STATE.update(project=p,preview=None)
                self.json({'project':p,'info':info});return
            if route in ('/api/save','/api/preview','/api/export'):
                p=migrate(data['project'],STATE['projectPath']);validate(p,STATE['projectPath'],files=route!='/api/save')
                if route=='/api/save':
                    path=inside_write(data.get('path') or STATE['projectPath'])
                    if path.exists():
                        backup=path.with_name(path.stem+'.backup-'+str(time.time_ns())+'.json');backup.write_bytes(path.read_bytes())
                    p=rebase(p,STATE['projectPath'],path);save(path,p)
                    STATE.update(projectPath=str(path),project=p)
                    self.json({**STATE,'portableProject':portable_project(p,path)});return
                STATE['project']=p;CANCEL.clear()
                STATE['job']={'busy':True,'phase':'준비','percent':0,'kind':route.split('/')[-1]}
                snapshot=copy.deepcopy(p);project_path=STATE['projectPath']
                output=inside_write(data.get('output') or ROOT/'devlog/youtube/day-01/media/video/exports'/f'day01-{time.strftime("%Y%m%d-%H%M%S")}.mp4')
                work=CACHE/str(time.time_ns());work.mkdir(parents=True,exist_ok=True)
                def worker():
                    def progress(update):STATE['job'].update(update)
                    try:
                        if route=='/api/preview':
                            progress({'phase':'미리듣기 합성'})
                            report=mix(snapshot,project_path,work/'preview.wav',CANCEL)
                            if CANCEL.is_set():raise InterruptedError('취소했습니다.')
                            STATE['preview']=str(work/'preview.wav');progress({'phase':'미리보기 준비 완료','percent':100,'report':report})
                        else:
                            export(snapshot,project_path,output,work,progress,CANCEL)
                            save(output.with_suffix('.devfilm.json'),rebase(snapshot,project_path,output.with_suffix('.devfilm.json')))
                    except Exception as e:progress({'phase':'취소됨' if isinstance(e,InterruptedError) else '실패','error':str(e)})
                    finally:STATE['job']['busy']=False
                threading.Thread(target=worker,daemon=True).start();self.json(STATE['job']);return
            self.send_error(404)
        except Exception as e:self.json({'error':str(e)},400)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8767);parser.add_argument('--no-browser',action='store_true');parser.add_argument('--resume-state');args=parser.parse_args()
    CACHE.mkdir(parents=True,exist_ok=True)
    STATE['project']=migrate(load(DEFAULT) if DEFAULT.exists() else from_day01(),DEFAULT)
    if args.resume_state:
        snapshot=json.loads(Path(args.resume_state).read_text(encoding='utf-8'))
        validate(snapshot['project'],snapshot['projectPath'])
        STATE.update(snapshot);STATE['job']={'busy':False}
        global TOKEN
        TOKEN=STATE.pop('_token',TOKEN)
    was_legacy=STATE['project'].get('schemaVersion')!=2
    STATE['project']=migrate(STATE['project'],STATE['projectPath'])
    if was_legacy:STATE['preview']=None
    server=ThreadingHTTPServer(('127.0.0.1',args.port),Handler)
    url=f'http://127.0.0.1:{args.port}/';print(url,flush=True)
    if not args.no_browser:webbrowser.open(url)
    server.serve_forever()

if __name__=='__main__':main()
