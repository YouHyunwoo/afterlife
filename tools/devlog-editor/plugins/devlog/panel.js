'use strict';
(()=>{
 const dialog=document.createElement('dialog');dialog.id='devlogPlugin';
 dialog.innerHTML=`<h2>개발일지 제작</h2><p class="hint">결과는 일반 트랙으로 추가됩니다. 기존 편집은 유지하며 한 번에 되돌릴 수 있습니다.</p>
 <label>작업<select id="devlogAction"><option value="script">대본 → 자막 초안</option><option value="narration">대본 → 음성 + 자막</option><option value="srt">SRT → 자막</option><option value="sequence">녹화본 순서대로 배치</option><option value="comparison">이전/현재 비교 장면</option><option value="credits">엔딩 크레딧 자막</option></select></label>
 <div id="devlogTextArea"><label>대본 / 자막<textarea id="devlogText" rows="9" placeholder="한 줄에 한 문장씩 입력하세요."></textarea></label><button id="devlogRead" type="button">TXT · Markdown · SRT 파일 가져오기</button><input id="devlogFile" type="file" accept=".txt,.md,.srt" hidden></div>
 <div id="devlogVideos" hidden><p class="hint">프로젝트에 가져온 영상을 선택하세요. 아래 목록 순서대로 원본 전체 길이를 배치합니다.</p><div id="devlogVideoList"></div></div>
 <div class="row"><label>타임라인 시작 (초)<input id="devlogStart" type="number" min="0" max="3599" step=".1"></label><label id="devlogSecondsLabel">문장 / 크레딧 길이 (초)<input id="devlogSeconds" type="number" min=".1" max="120" step=".1" value="4"></label></div>
 <div id="devlogVoiceOptions" hidden><p class="hint">앱에 포함된 Supertonic 모델로 한국어 음성을 생성합니다. 생성한 음성을 듣고 발음과 타이밍을 확인하세요.</p><label>음성<select id="devlogVoice"><option value="M1">M1</option><option value="M2">M2</option><option value="M3">M3</option><option value="M4">M4</option><option value="M5">M5</option></select></label><label>말하기 속도 (0.5~1.5)<input id="devlogSpeed" type="number" min="0.5" max="1.5" step="0.05" value="0.95"></label></div>
 <p id="devlogError" role="alert"></p><div class="row modal-actions"><button id="devlogClose">닫기</button><button id="devlogApply" class="primary">타임라인에 추가</button></div>`;
 document.body.append(dialog);
 const el=id=>dialog.querySelector('#'+id);
 function change(){const action=el('devlogAction').value;el('devlogVideos').hidden=!['sequence','comparison'].includes(action);el('devlogTextArea').hidden=['sequence','comparison'].includes(action);el('devlogVoiceOptions').hidden=action!=='narration';el('devlogSecondsLabel').hidden=!['script','credits','comparison'].includes(action);el('devlogSecondsLabel').firstChild.textContent=action==='comparison'?'비교 장면 길이 (초)':'문장 / 크레딧 길이 (초)';el('devlogVideos').querySelector('p').textContent=action==='comparison'?'이전 영상과 현재 영상을 순서대로 2개 선택하세요.':'프로젝트에 가져온 영상을 선택하세요. 아래 목록 순서대로 원본 전체 길이를 배치합니다.'}
 el('devlogAction').onchange=change;el('devlogClose').onclick=()=>dialog.close();
 el('devlogRead').onclick=()=>el('devlogFile').click();
 el('devlogFile').onchange=async()=>{const file=el('devlogFile').files[0];if(file){if(file.size>400000){el('devlogError').textContent='파일은 400KB 이하로 선택해주세요.';return}el('devlogText').value=await file.text();if(file.name.toLowerCase().endsWith('.srt')){el('devlogAction').value='srt';change()}}};
 let running=false;
 dialog.addEventListener('cancel',e=>{if(running)e.preventDefault()});
 el('devlogApply').onclick=async()=>{
  running=true;el('devlogError').textContent='';dialog.querySelectorAll('button,input,select,textarea').forEach(e=>e.disabled=true);
  try{
   const assets=[...el('devlogVideoList').querySelectorAll('input:checked')].map(e=>e.value);
   await window.EditorPlugins.run('devlog',el('devlogAction').value,{text:el('devlogText').value,start:Number(el('devlogStart').value),seconds:Number(el('devlogSeconds').value),voice:el('devlogVoice').value,speed:Number(el('devlogSpeed').value),assets});dialog.close();
  }catch(e){el('devlogError').textContent=e.message}
  finally{running=false;dialog.querySelectorAll('button,input,select,textarea').forEach(e=>e.disabled=false)}
 };
 window.EditorPlugins.register('devlog',{open(action){
  if(action)el('devlogAction').value=action;
  el('devlogStart').value=window.EditorPlugins.getPosition().toFixed(2);el('devlogError').textContent='';el('devlogVideoList').replaceChildren();
  for(const asset of window.EditorPlugins.getProject().assets||[]){if(asset.kind!=='video')continue;const label=document.createElement('label'),check=document.createElement('input');check.type='checkbox';check.value=asset.id;label.append(check,' '+asset.name);el('devlogVideoList').append(label)}
  change();dialog.showModal();
 }});
})();
