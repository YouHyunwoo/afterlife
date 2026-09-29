'use strict';
window.EditorPlugins=(()=>{
 const registered=new Map();let installed=[];
 const manager=document.createElement('dialog');manager.id='pluginManager';
 const heading=document.createElement('h2');heading.textContent='플러그인';
 const list=document.createElement('div');
 manager.append(heading,list,button('닫기',()=>manager.close()));document.body.append(manager);
 const toolbar=button('플러그인',async()=>{try{await refresh();manager.showModal()}catch(e){fail(e)}});
 document.querySelector('.menu-bar').append(toolbar);
 async function refresh(){
  installed=await(await fetch('/api/plugins')).json();list.replaceChildren();
  for(const entry of installed){
   if(entry.enabled&&!registered.has(entry.id))await new Promise((resolve,reject)=>{
    const script=document.createElement('script');script.src=`/plugins/${entry.id}/${entry.entry}`;
    script.onload=resolve;script.onerror=()=>reject(Error('플러그인 화면을 불러올 수 없습니다.'));document.head.append(script);
   });
   const row=document.createElement('section');row.className='plugin-card';
   const title=document.createElement('strong');title.textContent=entry.name+' · '+entry.version;
   const detail=document.createElement('p');detail.textContent=entry.description;
   const label=document.createElement('label'),toggle=document.createElement('input');toggle.type='checkbox';toggle.checked=entry.enabled;
   toggle.onchange=async()=>{try{await api('plugins/toggle',{id:entry.id,enabled:toggle.checked});await refresh()}catch(e){fail(e)}};
   label.append(toggle,' 사용');
   const open=button('열기',()=>{manager.close();registered.get(entry.id)?.open()});open.disabled=!entry.enabled||!project||busy;
   row.append(title,detail,label,open);list.append(row);
  }
 }
 async function run(id,action,options){
  if(!project||busy)throw Error('프로젝트를 열고 진행 중인 작업을 마쳐주세요.');
  const snapshot=clone(project);stop();busy=true;lock(true);$('cancelJob').hidden=true;
  message('플러그인 작업 중…');
  try{
   const result=await api('plugins/run',{id,action,options,project:snapshot});
   remember();project=result.project;selectedTrack=project.tracks[0]?.id;selectedClip=null;
   changed();renderSettings();seedAssets();renderAssets();message(result.summary);return result;
  }finally{busy=false;lock(false);$('undo').disabled=!history.length}
 }
 function register(id,plugin){registered.set(id,plugin)}
 async function open(id,action){if(!project||busy)throw Error('프로젝트를 열고 진행 중인 작업을 마쳐주세요.');await refresh();const entry=installed.find(item=>item.id===id);if(!entry?.enabled)throw Error('플러그인 메뉴에서 개발일지를 켜주세요.');registered.get(id)?.open(action)}
 return {register,run,open,getProject:()=>project?clone(project):null,getPosition:()=>position,refresh};
})();
