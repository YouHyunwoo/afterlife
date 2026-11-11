"""Collect real standalone-player evidence without altering captured pixels."""
from pathlib import Path
import json, shutil, html, hashlib
ROOT=Path(__file__).resolve().parents[2]
PROJECT=ROOT/'impls/unity/Afterlife'
RUN=PROJECT/'Logs/WorldQA/pass05'
OUT=ROOT/'docs/game-process/qa/world-art'
OUT.mkdir(parents=True,exist_ok=True)
assert (RUN/'complete.txt').exists(), 'Wait for the real player QA to finish.'
checks=(RUN/'checks.txt').read_text(encoding='utf8').splitlines()
assert checks and all(c.startswith('PASS ') for c in checks), checks
for source in RUN.glob('*.png'):shutil.copy2(source,OUT/source.name)
for name in ['checks.txt','chest-times.txt','complete.txt']:shutil.copy2(RUN/name,OUT/name)
live=OUT/'input';live.mkdir(exist_ok=True)
for source in (PROJECT/'Logs/WorldQA/live01').iterdir():
 if source.suffix in ['.png','.txt'] and source.name!='capture.txt':shutil.copy2(source,live/source.name)
shutil.copy2(PROJECT/'Builds/PixelPrototypeWindows/build-summary.txt',OUT/'build-summary.txt')
manifest=json.loads((ROOT/'res/art-source/world/manifest.json').read_text())
shutil.copy2(ROOT/'res/art-source/world/manifest.json',OUT/'assets.json')
snapshots=sorted(p for p in (PROJECT/'Logs/PrototypeBuild').iterdir() if p.is_dir() and len(p.name)==15 and p.name[8]=='-')
snapshot=snapshots[-1]
hash_file=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
settings=['ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','ProjectSettings/ProjectSettings.asset','ProjectSettings/EditorBuildSettings.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset']
restored=[dict(path=p,restored=hash_file(PROJECT/p)==hash_file(snapshot/Path(p).name)) for p in settings]
assert all(p['restored'] for p in restored)
(OUT/'settings-restored.json').write_text(json.dumps(restored,indent=2),encoding='utf8')
dll=PROJECT/'Builds/PixelPrototypeWindows/AfterlifePrototype_Data/Managed/Assembly-CSharp.dll'
evidence=dict(run=str(RUN),build_snapshot=snapshot.name,game_dll_sha256=hash_file(dll),screenshots=len(list(RUN.glob('*.png'))),checks=checks,assets=len(manifest),total_source_triangles=sum(m['triangles'] for m in manifest))
(OUT/'evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf8')
groups=[('마을과 건축',['01-village-day.png','02-village-night.png','04-house-market-detail.png','05-tower-detail.png','05-wall-connections.png']),('병력·마물·자원',['06-unit-silhouettes.png','13-resource-variants.png','13-animals.png','13-work-impact-01.png']),('지형',['16-biome--12--9.png','16-biome-10--6.png','16-biome-29-0.png','16-biome-0-30.png']),('실제 입력',['input/02-market-click.png','input/04-tree-click.png','input/05-troop-selection.png'])]
gallery=''.join('<section><h2>'+label+'</h2><div class="grid">'+''.join('<a href="'+f+'" target="_blank"><img loading="lazy" src="'+f+'" alt="'+html.escape(f)+'"><span>'+html.escape(f)+'</span></a>' for f in files)+'</div></section>' for label,files in groups)
sequences={
 '걷기·슬라임 도약':[p.name for p in sorted(RUN.glob('07-walk-*.png'))],
 '공격 준비·타격·회복':['08-attack-ready.png','09-attack-windup.png','10-attack-impact.png','11-attack-recovery.png'],
 '상자 개봉':[p.name for p in sorted(RUN.glob('15-chest-opening-*.png'))],
 '실제 전투':[p.name for p in sorted(RUN.glob('17-live-combat-*.png'))]}
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Afterlife · 월드 아트 QA</title>
<style>body{margin:0;background:#0d151a;color:#e2e2d8;font:16px/1.65 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:40px 32px}h1{font-size:36px;line-height:1.2}h2{margin-top:42px;color:#dbc38a}.tag{color:#71cdb9;font-weight:700}.sub{max-width:950px;color:#9eaeb7}button,select{font:inherit;color:#e2e2d8;background:#1c3039;border:1px solid #557579;padding:8px 14px;border-radius:5px;cursor:pointer}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:18px}.grid img{width:100%;display:block}.grid a{color:#adb8bc;text-decoration:none;background:#15232b;padding:8px;border:1px solid #30414a}.grid span{font-size:12px}#motion{width:100%;display:block;border:1px solid #526164;margin-top:16px}.controls{display:flex;gap:14px;align-items:center;flex-wrap:wrap}input{flex:1;min-width:200px}footer{margin:44px 0;color:#9eaeb7}a{color:#87d7c6}</style>
<main><p class="tag">M5 · 17 월드 모델링·지형·애니메이션</p><h1>실제 게임 화면으로 확인한 월드 아트</h1>
<p class="sub">Blender 원본에서 만든 49종과 관절을 보완한 동물 4종. 아래 화면은 Windows 개발 빌드의 원본 캡처이며, 사진 보정이나 이미지 생성으로 대체하지 않았습니다. 조형·시야·공격·상자·입력 검증이며 게임 전체 밸런스와 출시 인증은 포함하지 않습니다.</p>
<p>검증 화면 <b>SCREENCOUNT장</b> · 자동 확인 <b>CHECKCOUNT개 통과</b> · 빌드 오류/경고 <b>0/0</b></p>
<h2>애니메이션 프레임 보기</h2><p class="sub">연속 캡처와 단계별 포즈를 편하게 비교하는 보기입니다. 화면 간 간격이 다르므로 영상의 실제 재생 속도를 뜻하지 않습니다. 상자 캡처 시간은 <a href="chest-times.txt">원본 시간 기록</a>을 참고하세요.</p>
<div class="controls"><select id="sequence"></select><button id="play">재생</button><input id="frame" type="range" min="0" value="0"><span id="counter"></span></div><img id="motion" alt="실제 게임 애니메이션 캡처">
GALLERY
<footer><a href="../../17-world-art-qa.md">상세 QA 보고서</a> · <a href="evidence.json">빌드 식별·검증 데이터</a> · <a href="build-summary.txt">빌드 결과</a><br>기존 종료 시 ComputeBuffer 정리 경고는 별도 후속 항목으로 남습니다.</footer></main>
<script>const sequences=SEQUENCES;const select=document.getElementById('sequence'),slider=document.getElementById('frame'),picture=document.getElementById('motion'),counter=document.getElementById('counter'),play=document.getElementById('play');let timer=null;
for(const key of Object.keys(sequences)){let opt=document.createElement('option');opt.textContent=key;select.append(opt)}
function show(){let list=sequences[select.value],i=Number(slider.value);picture.src=list[i];counter.textContent=(i+1)+' / '+list.length}
function stop(){clearInterval(timer);timer=null;play.textContent='재생'}
select.onchange=()=>{stop();slider.value=0;slider.max=sequences[select.value].length-1;show()};slider.oninput=()=>{stop();show()};play.onclick=()=>{if(timer){stop();return}play.textContent='일시정지';timer=setInterval(()=>{slider.value=(Number(slider.value)+1)%sequences[select.value].length;show()},180)};select.onchange();</script></html>'''
page=page.replace('SCREENCOUNT',str(evidence['screenshots'])).replace('CHECKCOUNT',str(len(checks))).replace('GALLERY',gallery).replace('SEQUENCES',json.dumps(sequences,ensure_ascii=False))
(OUT/'index.html').write_text(page,encoding='utf8')
print(json.dumps(evidence,ensure_ascii=False,indent=2))
