'use strict';
if(window.EDITOR_DESKTOP){
 document.body.classList.add('desktop');
 const bridgeReady=new Promise(resolve=>{
  if(window.pywebview?.api)resolve(window.pywebview.api);
  else window.addEventListener('pywebviewready',()=>resolve(window.pywebview.api),{once:true});
 });
 for(const id of ['createWorkspaceFolder','openWorkspaceProject'])$(id).hidden=true;
 $('savePickerHint').textContent='프로젝트 폴더를 선택하세요. 프로젝트 파일과 미디어가 함께 보관됩니다.';
 $('openPickerHint').textContent='기존 프로젝트 JSON을 선택하세요. 이전 편집본도 열 수 있습니다.';
 $('newProjectFolder').placeholder='클릭하여 프로젝트 폴더 선택';
 $('newProjectFolder').parentElement.firstChild.textContent='프로젝트 폴더';
 $('newProjectFolder').title='클릭하여 프로젝트 폴더 선택';
 $('openProjectPath').title='클릭하여 프로젝트 파일 선택';
 $('newProjectFolder').onclick=async()=>{
  try{const folder=await(await bridgeReady).choose_folder();if(folder){$('newProjectFolder').value=folder;pendingSaveHandle=null}}
  catch(e){$('projectError').textContent=e.message}
 };
 $('openProjectPath').onclick=async()=>{
  try{const path=await(await bridgeReady).open_project();if(path){$('openProjectPath').value=path;pendingOpenHandle=null}}
  catch(e){$('openError').textContent=e.message}
 };
 const openDesktopProject=async(error)=>{
  if(busy||saving)return;
  closeMenus();
  if(error)error.textContent='';
  try{
   const path=await(await bridgeReady).open_project();
   if(!path)return;
   if(modified&&!confirm('저장하지 않은 편집을 버리고 다른 프로젝트를 열까요?'))return;
   const data=await api('load',{path});
   showProject(data);
  }catch(e){if(error)error.textContent=e.message;else fail(e)}
 };
 $('welcomeOpen').onclick=()=>openDesktopProject($('welcomeError'));
 $('open').onclick=()=>openDesktopProject();
 $('windowMinimize').onclick=async()=>{try{await(await bridgeReady).minimize_window()}catch(e){fail(e)}};
 $('windowMaximize').onclick=async()=>{try{const maximized=await(await bridgeReady).toggle_maximize();$('windowMaximize').textContent=maximized?'❐':'□';$('windowMaximize').title=maximized?'복원':'최대화';$('windowMaximize').setAttribute('aria-label',$('windowMaximize').title)}catch(e){fail(e)}};
 $('windowClose').onclick=async()=>{try{await(await bridgeReady).close_window()}catch(e){fail(e)}};
 $('titleDrag').ondblclick=()=>$('windowMaximize').click();
 const outputButton=button('출력 위치 선택',async()=>{
  try{const path=await(await bridgeReady).choose_output();if(path)$('output').value=path}catch(e){fail(e)}
 });
 $('output').parentElement.append(outputButton);
 // 기본 생성/열기 명령은 실제 경로를 받아 동일한 저장 경로를 사용한다.
 bridgeReady.then(()=>{window.desktopReady=true});
}
