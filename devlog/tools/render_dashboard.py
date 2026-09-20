"""Markdown 원본으로 오프라인 Devlog 관리 화면 생성. 외부 패키지 설치 불필요."""
from pathlib import Path
from urllib.parse import urlsplit
import sys, html, re, os
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).parent/'vendor'))
import mistune
ROOT=Path(__file__).resolve().parents[1]
DOCS=[('status.md','현재 상태'),('day-01/production.md','목표 · 촬영'),('day-01/features.md','기능 · 조작'),('architecture/modules.md','모듈 안내'),('architecture/assets.md','에셋 · 프리팹'),('architecture/organization.md','구조 개편'),('records/validation.md','검증'),('records/hover-feedback.md','호버 피드백'),('records/leaf-card-comparison.md','나뭇잎 비교'),('records/organization-change.md','변경 기록'),('records/day-01-history.md','누적 기록'),('README.md','관리 방법')]
DOCS.insert(9,('records/abandoned-altar-v2.md','제단 모델 제작'))
DOCS.insert(10,('records/visual-rhythm.md','밀도 · 타격 리듬'))
DOCS.insert(11,('records/ground-wind-night.md','접지 · 바람 · 밤'))
DOCS.insert(12,('records/leaf-dissolve.md','잎 디졸브'))
DOCS.insert(13,('records/damage-marks.md','타격 자국 비교'))
DOCS.insert(14,('records/nature-ambience.md','자연 환경음'))
DOCS.insert(2,('records/stage-progression.md','6단계 촬영 구성'))
DOCS.insert(3,('records/movie-sequence.md','영상 연출 · 자막'))
DOCS.insert(4,('records/continuous-terrain.md','연속 지형'))
DOCS.insert(5,('records/runtime-optimization.md','런타임 최적화'))
DOCS.insert(6,('records/prototype-terrain.md','프로토타입 지형'))
IDS={str((ROOT/p).resolve()):'doc-'+str(i) for i,(p,_) in enumerate(DOCS)}
class Renderer(mistune.HTMLRenderer):
    def __init__(self,source):super().__init__(escape=True);self.source=source
    def image(self,text,url,title=None):
        if not urlsplit(url).scheme:
            target=(self.source.parent/url).resolve()
            url=Path(os.path.relpath(target,ROOT)).as_posix()
        return super().image(text,url,title)
    def link(self,text,url,title=None):
        parts=urlsplit(url)
        if not parts.scheme and not url.startswith('#'):
            target=(self.source.parent/parts.path).resolve()
            url='#'+IDS[str(target)] if str(target) in IDS else Path(os.path.relpath(target,ROOT)).as_posix()+('#'+parts.fragment if parts.fragment else '')
        return super().link(text,url,title)
def main():
    nav=[]; sections=[]
    for i,(path,label) in enumerate(DOCS):
        source=ROOT/path
        if not source.exists():raise FileNotFoundError(source)
        parser=mistune.create_markdown(renderer=Renderer(source),plugins=['table','task_lists','strikethrough'])
        body=parser(source.read_text(encoding='utf-8-sig'))
        nav.append(f'<a href="#doc-{i}"><span>{i+1:02}</span>{html.escape(label)}</a>')
        sections.append(f'<section id="doc-{i}" class="document"><div class="section-label">{html.escape(path)}</div>{body}</section>')
    page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Afterlife · Devlog 제작 관리</title><style>
:root{color-scheme:dark;--bg:#101a20;--panel:#19262e;--line:#33454e;--ink:#eef3ee;--muted:#acbec4;--accent:#b7dfa5}*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:24px}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.8 'Segoe UI','Malgun Gothic',sans-serif}aside{position:fixed;inset:0 auto 0 0;width:238px;padding:32px 22px;background:#152129;border-right:1px solid var(--line);overflow:auto}.brand{font-size:22px;letter-spacing:3px;font-weight:700}.subtitle{font-size:12px;color:var(--muted);margin-bottom:28px}nav a{display:flex;gap:14px;padding:10px 8px;color:var(--muted);text-decoration:none;border-radius:6px}nav a:hover,nav a:focus{background:#2a3b42;color:white}nav span{font-size:11px;color:var(--accent);padding-top:4px}main{margin-left:238px;max-width:1330px;padding:45px 5vw}.eyebrow{color:var(--accent);letter-spacing:3px;font-size:12px}header h1{font-size:clamp(30px,4vw,50px);line-height:1.25;margin:18px 0}.lead{color:var(--muted);max-width:650px}header{padding:0 0 34px}.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:24px 0}.card{padding:17px;background:var(--panel);border-top:2px solid var(--accent)}.card small{display:block;color:var(--muted)}.card strong{font-size:19px}input{width:100%;padding:13px 16px;background:#0e171c;color:white;border:1px solid var(--line);border-radius:7px;font:inherit}.document{background:var(--panel);padding:30px;margin:24px 0;border:1px solid var(--line);border-radius:10px;overflow-wrap:anywhere}.section-label{color:var(--accent);font-size:11px;letter-spacing:1px}.document h1{font-size:27px;margin:8px 0 20px}.document h2{font-size:20px;margin-top:30px}p,li{max-width:88ch}a{color:#b8e4d2}table{border-collapse:collapse;width:100%;font-size:14px;margin:18px 0;display:block;overflow:auto}th,td{text-align:left;vertical-align:top;padding:12px;border-bottom:1px solid var(--line);min-width:95px}th{color:var(--accent);background:#21313a}pre{overflow:auto;background:#101a20;padding:18px}code{font-size:13px;color:#c6e4c2}blockquote{border-left:3px solid var(--accent);margin-left:0;padding-left:20px;color:var(--muted)}footer{font-size:12px;color:var(--muted)}[hidden]{display:none!important}@media(max-width:800px){aside{position:static;width:auto;padding:20px}nav{display:flex;gap:5px;overflow:auto}nav a{white-space:nowrap}nav span{display:none}.subtitle{margin-bottom:8px}main{margin:0;padding:24px 16px}.document{padding:20px 16px}.cards{grid-template-columns:1fr}.card{padding:10px 16px}}@media print{aside,input,.lead{display:none}main{margin:0;padding:0;max-width:none}body{background:white;color:black}.document{display:block!important;background:white;border:0;padding:8px;break-before:page}th{background:#eee;color:black}a,code{color:black}.cards{display:none}table{display:table;font-size:11px}.section-label{color:#333}}
</style><aside><div class="brand">AFTERLIFE</div><div class="subtitle">DEVLOG / PRODUCTION NOTES</div><nav>'''+''.join(nav)+'''</nav></aside><main><header><div class="eyebrow">DAY 01 · 3D PIXEL WORLD</div><h1>작은 세계를,<br>만지고 싶게.</h1><p class="lead">기본 도형에서 생동감 있는 중세 판타지 환경까지.<br>목표, 표현 기능, 재사용 모듈과 촬영 준비를 한곳에서 관리합니다.</p><div class="cards"><div class="card"><small>현재 제작</small><strong>6단계 구성 완료</strong></div><div class="card"><small>공간</small><strong>Before → 4단계 → After</strong></div><div class="card"><small>다음 결정</small><strong>단계별 화면 확인</strong></div></div><label for="search">문서 찾기</label><input id="search" type="search" placeholder="예: 눈, 프리팹, 크레딧" autocomplete="off"><p id="count" role="status"></p></header>'''+''.join(sections)+'''<footer>Markdown 원본 → HTML 생성 · 오프라인 문서 · 공개/배포되지 않은 내부 제작 기록</footer></main><script>
const input=document.querySelector('#search'), docs=[...document.querySelectorAll('.document')];input.addEventListener('input',()=>{const q=input.value.trim().toLowerCase();docs.forEach(d=>d.hidden=q&&!d.textContent.toLowerCase().includes(q));document.querySelector('#count').textContent=q?`${docs.filter(d=>!d.hidden).length}개 문서` : '';});document.querySelectorAll('a[href^="#doc-"]').forEach(a=>a.addEventListener('click',()=>{input.value='';docs.forEach(d=>d.hidden=false);document.querySelector('#count').textContent='';}));
</script></html>'''
    page=page.replace('</style>','.document img{display:block;max-width:100%;height:auto;border-radius:8px;margin:20px 0}</style>')
    (ROOT/'index.html').write_text(page,encoding='utf-8')
    print(f'Generated {ROOT / "index.html"}: {len(DOCS)} documents')
if __name__=='__main__':main()
