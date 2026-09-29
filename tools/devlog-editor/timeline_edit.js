'use strict';
// 타임라인의 시간 편집을 UI와 분리한다. 모든 함수는 입력 프로젝트를 보존한다.
window.EditorTimeline=(()=>{
 const copy=project=>structuredClone(project);
 const items=project=>project.tracks.flatMap(track=>track.clips.map(clip=>({track,clip})));
 const selected=(project,trackId,clipId)=>{
  const track=project.tracks.find(track=>track.id===trackId);
  const clip=track?.clips.find(clip=>clip.id===clipId);
  if(!clip)throw Error('편집할 클립을 선택하세요.');
  return {track,clip};
 };
 const members=(project,clip)=>clip.linkGroup?items(project).filter(item=>item.clip.linkGroup===clip.linkGroup):items(project).filter(item=>item.clip.id===clip.id);
 function verify(project){
  for(const track of project.tracks){
   const sorted=[...track.clips].sort((a,b)=>a.start-b.start);
   for(let i=0;i<sorted.length;i++){
    const clip=sorted[i];
    if(clip.start<0||clip.duration<.04||clip.start+clip.duration>3600+1e-6)throw Error('클립은 0~3600초 안에 있어야 합니다.');
    if(track.kind==='video'&&i&&sorted[i-1].start+sorted[i-1].duration>clip.start+1e-6)throw Error('같은 영상 트랙의 클립이 겹칩니다.');
   }
  }
 }
 function sourceLimit(project,clip){return project.assets?.find(asset=>asset.path===clip.path)?.duration}
 function checkSource(project,track,clip){
  if(track.kind==='subtitle')return;
  const limit=sourceLimit(project,clip);
  if(Number.isFinite(limit)&&clip.sourceIn>=limit)throw Error('원본 시작이 파일 끝을 넘습니다.');
  if(Number.isFinite(limit)&&!clip.loop&&!clip.padEnd&&clip.sourceIn+clip.duration>limit+.02)throw Error('조정한 구간이 원본 길이를 넘습니다.');
 }
 function move(project,trackId,clipId,toTrackId,start){
  const result=copy(project),{track,clip}=selected(result,trackId,clipId);
  const destination=result.tracks.find(item=>item.id===toTrackId);
  if(!destination||destination.kind!==track.kind)throw Error('같은 종류의 트랙으로만 옮길 수 있습니다.');
  const delta=start-clip.start;
  for(const item of members(result,clip))item.clip.start+=delta;
  if(destination!==track){track.clips=track.clips.filter(item=>item.id!==clipId);destination.clips.push(clip)}
  verify(result);return result;
 }
 function split(project,trackId,clipId,time,id){
  const result=copy(project),{clip}=selected(result,trackId,clipId),group=members(result,clip);
  if(!group.every(item=>time>item.clip.start+.04-1e-6&&time<item.clip.start+item.clip.duration-.04+1e-6))throw Error('연결된 클립 모두의 내부에서 분할 위치를 선택하세요.');
  if(group.some(item=>item.clip.loop))throw Error('반복 음원은 구간 편집창에서 먼저 반복을 해제한 뒤 분할하세요.');
  const rightGroup=clip.linkGroup?id():null;let rightId=null;
  for(const item of group){
   const source=item.clip,offset=time-source.start,right={...source,id:id(),start:time,duration:source.duration-offset};
   source.duration=offset;
   if(item.track.kind!=='subtitle')right.sourceIn+=offset;
   if(rightGroup)right.linkGroup=rightGroup;
   item.track.clips.push(right);
   if(item.clip.id===clipId)rightId=right.id;
  }
  verify(result);return {project:result,rightId};
 }
 function trim(project,trackId,clipId,side,time){
  if(!['start','end'].includes(side))throw Error('올바르지 않은 클립 손잡이입니다.');
  const result=copy(project),{clip}=selected(result,trackId,clipId),group=members(result,clip);
  if(group.some(item=>item.clip.loop&&side==='start'))throw Error('반복 음원의 시작 손잡이는 구간 편집창에서 조정하세요.');
  for(const {track,clip:item} of group){
   const end=item.start+item.duration;
   if(side==='start'){
    const delta=time-item.start;
    if(delta<0||time>end-.04)throw Error('시작 손잡이는 클립 내부에서 줄일 수 있습니다.');
    item.start=time;item.duration=end-time;
    if(track.kind!=='subtitle')item.sourceIn+=delta;
   }else{
    if(time<item.start+.04||time>end)throw Error('끝 손잡이는 클립 내부에서 줄일 수 있습니다.');
    item.duration=time-item.start;
   }
   checkSource(result,track,item);
  }
  verify(result);return result;
 }
 function resize(project,trackId,clipId,duration){
  const result=copy(project),{clip}=selected(result,trackId,clipId);
  for(const {track,clip:item} of members(result,clip)){item.duration=duration;checkSource(result,track,item)}
  verify(result);return result;
 }
 function rippleDelete(project,trackId,clipId,id){
  const result=copy(project),{clip}=selected(result,trackId,clipId),start=clip.start,end=start+clip.duration,delta=end-start;
  for(const track of result.tracks){
   const next=[];
   for(const item of track.clips){
    const itemEnd=item.start+item.duration;
    if(itemEnd<=start+1e-6){next.push(item);continue}
    if(item.start>=end-1e-6){item.start-=delta;next.push(item);continue}
    if(item.loop&&item.start<end&&itemEnd>end)throw Error('반복 음원이 삭제 구간을 가로지릅니다. 먼저 해당 음원을 편집하세요.');
    if(item.start<start&&itemEnd>end){
     const right={...item,id:id(),start,duration:itemEnd-end};
     if(track.kind!=='subtitle')right.sourceIn+=end-item.start;
     item.duration=start-item.start;delete item.linkGroup;delete right.linkGroup;
     checkSource(result,track,right);next.push(item,right);continue;
    }
    if(item.start<start&&itemEnd>start){item.duration=start-item.start;delete item.linkGroup;next.push(item);continue}
    if(item.start<end&&itemEnd>end){
     if(track.kind!=='subtitle')item.sourceIn+=end-item.start;
     item.start=start;item.duration=itemEnd-end;delete item.linkGroup;
     checkSource(result,track,item);next.push(item);
    }
   }
   track.clips=next;
  }
  if(result.markers)result.markers=result.markers.filter(marker=>marker.time<start||marker.time>=end).map(marker=>({...marker,time:marker.time>=end?marker.time-delta:marker.time}));
  verify(result);return result;
 }
 function linkAtStart(project,trackId,clipId,id){
  const result=copy(project),{track,clip}=selected(result,trackId,clipId);
  const matches=items(result).filter(item=>item.clip.id!==clip.id&&item.track.kind!==track.kind&&Math.abs(item.clip.start-clip.start)<.001&&Math.abs(item.clip.duration-clip.duration)<.001);
  if(!matches.length)throw Error('같은 시작과 길이의 다른 종류 클립이 없습니다.');
  const existing=new Set([clip.linkGroup,...matches.map(item=>item.clip.linkGroup)].filter(Boolean));
  const group=clip.linkGroup||matches.find(item=>item.clip.linkGroup)?.clip.linkGroup||id();
  const linkedIds=new Set([clip.id,...matches.map(item=>item.clip.id)]);
  for(const item of items(result))if(linkedIds.has(item.clip.id)||existing.has(item.clip.linkGroup))item.clip.linkGroup=group;
  return result;
 }
 function unlink(project,trackId,clipId){
  const result=copy(project),{clip}=selected(result,trackId,clipId);
  for(const item of members(result,clip))delete item.clip.linkGroup;
  return result;
 }
 return {move,split,trim,resize,rippleDelete,linkAtStart,unlink};
})();
