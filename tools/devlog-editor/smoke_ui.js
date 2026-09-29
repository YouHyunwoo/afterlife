// 실제 WebView2에서 생성 → 플러그인 패널 → 되돌리기 → 저장 → MP4를 검증한다.
(async()=>{
 const pause=ms=>new Promise(resolve=>setTimeout(resolve,ms));
 const until=async predicate=>{for(let i=0;i<600;i++){if(predicate())return;await pause(100)}throw Error('화면 검증 시간 초과')};
 const check=(value,label)=>{if(!value)throw Error(label)};
 try{
  await until(()=>window.desktopReady&&window.EditorPlugins);
  const realOpen=window.pywebview.api.open_project;
  try{
   window.pywebview.api.open_project=async()=>null;
   $('welcomeOpen').click();await pause(100);
   check(!$('welcome').hidden&&!$('openDialog').open,'파일 선택 취소 시 첫 화면 유지');
   window.pywebview.api.open_project=async()=>window.SMOKE_OPEN_PATH;
   $('welcomeOpen').click();
   await until(()=>$('welcome').hidden);
   check(projectPath===window.SMOKE_OPEN_PATH&&!$('openDialog').open,'첫 화면에서 모달 없이 프로젝트 열기');
  }finally{window.pywebview.api.open_project=realOpen}
  const data=await api('create',{name:'Desktop QA',folder:window.SMOKE_FOLDER});showProject(data);
  check(!document.querySelector('.project-header')&&$('headerProjectName').textContent==='Desktop QA','프로젝트 이름 제목표시줄 표시');
  check($('windowClose').offsetWidth>0&&document.querySelector('.asset-sidebar').getBoundingClientRect().right<=document.querySelector('.preview').getBoundingClientRect().left,'제목표시줄과 왼쪽 사이드바');
  const titlebar=document.querySelector('.app-titlebar'),scrollArea=$('timelineScroll');
  const scrollProbe=document.createElement('div');scrollProbe.style.height='1400px';$('timeline').append(scrollProbe);
  scrollArea.scrollTop=500;
  check(scrollArea.scrollTop>0&&scrollArea.getBoundingClientRect().top>=titlebar.getBoundingClientRect().bottom-1&&titlebar.getBoundingClientRect().top===0&&document.documentElement.scrollTop===0,'스크롤바가 제목표시줄 아래 작업 영역에만 표시');
  scrollProbe.remove();scrollArea.scrollTop=0;
  const previewBox=document.querySelector('.preview').getBoundingClientRect(),workBox=document.querySelector('.work-area').getBoundingClientRect(),videoBox=$('video').parentElement.getBoundingClientRect();
  check(previewBox.width>(workBox.width-310)*.85&&Math.abs((videoBox.left+videoBox.right-previewBox.left-previewBox.right)/2)<3,'작업 영역 중앙 프리뷰');
  check(document.querySelector('.timeline-panel').getBoundingClientRect().top>=previewBox.bottom-1,'하단 타임라인 배치');
  $('windowMaximize').click();await until(()=>$('windowMaximize').textContent==='❐');
  $('windowMaximize').click();await until(()=>$('windowMaximize').textContent==='□');
  $('height').value='720';$('height').dispatchEvent(new Event('change'));
  $('aspectOpen').click();check($('aspectDialog').open,'화면 비율 모달');
  document.querySelector('.aspect-option[data-ratio="9:16"]').click();
  const screen=$('video').parentElement.getBoundingClientRect();
  check(Math.abs(screen.width/screen.height-9/16)<.03,'세로 프리뷰 비율');
  $('aspectApply').click();check(project.export.aspectRatio==='9:16'&&!$('aspectDialog').open,'화면 비율 적용');
  $('aspectOpen').click();document.querySelector('.aspect-option[data-ratio="1:1"]').click();$('aspectCancel').click();
  await until(()=>$('previewRatioLabel').textContent==='9:16');
  check(project.export.aspectRatio==='9:16','화면 비율 취소');
  $('devlogTab').click();check(!$('devlogPane').hidden&&$('mediaPane').hidden,'개발일지 사이드바');
  $('devlogScriptShortcut').click();await until(()=>$('devlogPlugin')?.open);
  check($('devlogAction').value==='script','사이드바 대본 바로가기');$('devlogClose').click();
  $('mediaTab').click();check(!$('mediaPane').hidden,'미디어 사이드바 복귀');
  await window.EditorPlugins.refresh();
  document.querySelector('.menu-bar>button').click();
  await until(()=>$('pluginManager').open);
  [...$('pluginManager').querySelectorAll('button')].find(b=>b.textContent==='열기').click();
  check($('devlogPlugin').open,'플러그인 패널 열림');
  $('devlogText').value='첫 번째 개발일지\n두 번째 장면';$('devlogSeconds').value='1';$('devlogApply').click();
  await until(()=>!$('devlogPlugin').open&&!busy);
  check(project.tracks[0].clips.length===2,'자막 2개 생성');
  $('undo').click();check(project.tracks.length===0,'플러그인 작업 되돌리기');
  await window.EditorPlugins.run('devlog','script',{text:'독립 편집기 검증',start:0,seconds:1});
  $('devlogTab').click();$('markerTitle').value='이전 화면 촬영';seek(.75);$('markerAdd').click();
  check(project.markers.length===1&&$('markerList').textContent.includes('이전 화면 촬영'),'촬영 메모 추가');
  let clip=project.tracks[0].clips[0];select(project.tracks[0],clip);seek(.5);$('splitClip').click();
  check(project.tracks[0].clips.length===2&&selectedClip!==clip.id,'재생 위치에서 분할');
  $('rippleDelete').click();check(project.tracks[0].clips.length===1&&project.tracks[0].clips[0].duration===.5&&project.markers.length===0,'구간 당겨 삭제');
  $('undo').click();$('undo').click();check(project.tracks[0].clips.length===1,'타임라인 작업 되돌리기');
  await saveProject(true);check(!modified&&$('message').textContent.includes('자동 저장'),'프로젝트 자동 저장');
  mutate(()=>project.minimumDuration=1.1);await saveProject();check(!modified,'프로젝트 저장');
  const saved=await api('load',{path:projectPath});showProject(saved);
  check(project.tracks[0].clips[0].text==='독립 편집기 검증','재열기');
  check(project.export.aspectRatio==='9:16','화면 비율 재열기');
  check(project.markers?.[0]?.title==='이전 화면 촬영','촬영 메모 재열기');
  const menuOpen=window.pywebview.api.open_project;
  try{
   window.pywebview.api.open_project=async()=>window.SMOKE_OPEN_PATH;
   $('open').click();await until(()=>projectPath===window.SMOKE_OPEN_PATH);
   check(!$('openDialog').open,'편집 화면 메뉴에서 모달 없이 프로젝트 열기');
  }finally{window.pywebview.api.open_project=menuOpen}
  showProject(saved);
  await api('plugins/toggle',{id:'devlog',enabled:false});
  let rejected=false;try{await api('plugins/run',{id:'devlog',action:'script',options:{text:'차단'},project})}catch{rejected=true}
  check(rejected,'비활성 플러그인 실행 차단');
  await api('plugins/toggle',{id:'devlog',enabled:true});
  await window.EditorPlugins.run('devlog','narration',{text:'개발일지 편집기를 테스트합니다.',start:1});
  check(project.tracks.some(t=>t.kind==='audio'&&t.clips.length),'나레이션 생성');
  const voice=project.tracks.find(t=>t.kind==='audio').clips[0],caption=project.tracks.find(t=>t.kind==='subtitle'&&t.clips.some(c=>c.linkGroup===voice.linkGroup)).clips.find(c=>c.linkGroup===voice.linkGroup);
  check(!!voice.linkGroup&&voice.linkGroup===caption.linkGroup,'문장별 음성·자막 연결');
  const narrationAsset=project.assets.find(asset=>asset.path===voice.path);selectAsset(narrationAsset);
  const relink=[...$('inspector').querySelectorAll('details')].find(item=>item.querySelector('summary')?.textContent==='원본 파일 다시 연결');
  check(!!relink,'원본 다시 연결 도구');relink.querySelector('input').value=narrationAsset.path;
  [...relink.querySelectorAll('button')].find(item=>item.textContent==='선택한 원본으로 연결').click();
  await until(()=>$('message').textContent==='원본 파일을 다시 연결했습니다.');
  const originalVoicePath=voice.path;voice.path='missing-smoke.wav';$('export').click();
  await until(()=>$('exportCheckList').textContent.includes('원본을 열 수 없음'));
  check($('exportCheckStart').disabled,'누락 원본은 내보내기를 차단');
  $('exportCheckCancel').click();voice.path=originalVoicePath;
  $('export').click();check($('exportCheckDialog').open,'MP4 내보내기 사전 확인');
  await until(()=>$('exportCheckStart').disabled===false);
  check($('exportCheckList').textContent.includes('완료하지 않은 촬영 메모')&&$('exportCheckList').textContent.includes('원본'),'원본과 촬영 메모 확인');
  $('exportCheckStart').click();await until(()=>busy);
  let jobState;for(let i=0;i<600;i++){jobState=await(await fetch('/api/job')).json();if(!jobState.busy)break;await pause(100)}
  check(!jobState.busy&&!jobState.error&&jobState.output,'MP4 내보내기: '+JSON.stringify(jobState));
  window.SMOKE_RESULT={ok:true,desktop:window.desktopReady,tracks:project.tracks.length,output:jobState.output,aspectRatio:project.export.aspectRatio,plugins:true,title:document.title};
 }catch(e){window.SMOKE_RESULT={ok:false,error:e.stack||e.message,debug:{windowButton:$('windowMaximize').textContent,bridgeMethod:typeof window.pywebview?.api?.toggle_maximize,message:$('message').textContent}}}
})();
