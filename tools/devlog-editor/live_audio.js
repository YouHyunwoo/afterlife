'use strict';

// 브라우저 재생 전용 믹서. 출력 파일의 PCM 합성은 render_tracks.py가 맡는다.
(function(root){
const clamp=(value,min=0,max=1)=>Math.max(min,Math.min(max,value));
const smooth=value=>{const t=clamp(value);return t*t*(3-2*t)};

function clipSegments(clip,sourceDuration,time){
 const elapsed=time-clip.start;
 if(elapsed<0||elapsed>=clip.duration)return[];
 const available=sourceDuration-clip.sourceIn;
 if(available<=0)return[];
 const span=Math.min(clip.duration,available);
 if(!clip.loop)return elapsed<span?[{cycle:0,sourceTime:clip.sourceIn+elapsed,weight:1}]:[];
 const overlap=Math.min(4,span/4),step=span-overlap;
 if(step<=0)return[];
 const cycle=Math.floor(elapsed/step),result=[];
 for(const index of [cycle-1,cycle]){
  if(index<0)continue;
  const age=elapsed-index*step;
  if(age<0||age>=span)continue;
  let weight=1;
  if(index>0&&overlap>0)weight*=clamp(age/overlap);
  if(index*step+span<clip.duration&&overlap>0)weight*=clamp((span-age)/overlap);
  result.push({cycle:index,sourceTime:clip.sourceIn+age,weight});
 }
 return result;
}

function clipGain(project,clip,time,visibleVideo){
 let gain=clip.volume??1;
 if(clip.role==='narration')gain*=project.narrationVolume;
 if(clip.role==='music'){
  const settings=project.music,elapsed=time-clip.start;
  gain*=settings.volume;
  gain*=clamp(elapsed/Math.max(.01,settings.fadeIn??2));
  gain*=clamp((clip.duration-elapsed)/Math.max(.01,settings.fadeOut??3));
  if(visibleVideo?.credits){
   const ramp=smooth((time-visibleVideo.start)/Math.max(.01,settings.creditsRamp??2));
   gain*=1+(settings.creditsMultiplier-1)*ramp;
  }
 }
 return Math.max(0,gain);
}

class LiveAudio{
 constructor({media,clips,project,visibleVideo,onError}){
  Object.assign(this,{media,clips,project,visibleVideo,onError});
  this.states=new Map();this.context=null;this.playing=false;this.generation=0;this.startSerial=0;this.time=0;
 }
 createPlayer(url){
  const element=new Audio(url);element.preload='auto';
  element.addEventListener('loadedmetadata',()=>{if(this.playing)this.update(this.time)});
  let gain=null;
  if(this.context){const source=this.context.createMediaElementSource(element);gain=this.context.createGain();source.connect(gain).connect(this.context.destination)}
  return{element,gain,cycle:null};
 }
 ensure(clip){
  const existing=this.states.get(clip.id);
  if(existing)return existing.promise;
  const generation=this.generation,state={clip,meta:null,players:[],promise:null};
  this.states.set(clip.id,state);
  state.promise=this.media(clip.path,'audio').then(meta=>{
   if(generation!==this.generation)return;
   state.meta=meta;
   state.players=Array.from({length:clip.loop?2:1},()=>this.createPlayer(meta.url));
   if(this.playing)this.syncState(state,this.time);
  }).catch(error=>{if(generation===this.generation){state.error=error;this.onError(error)}throw error});
  return state.promise;
 }
 async start(time){
  const serial=++this.startSerial;
  if(!this.context){const Context=root.AudioContext||root.webkitAudioContext;if(Context)this.context=new Context()}
  if(this.context)await this.context.resume();
  const current=this.clips().filter(({clip})=>time>=clip.start&&time<clip.start+clip.duration&&clip.path);
  await Promise.all(current.map(({clip})=>this.ensure(clip)));
  if(serial!==this.startSerial)return false;
  this.playing=true;this.update(time);return true;
 }
 pause(){
  this.startSerial++;this.playing=false;
  for(const state of this.states.values())for(const player of state.players)player.element.pause();
 }
 reset(){
  this.pause();this.generation++;
  for(const state of this.states.values())for(const player of state.players){player.element.pause();player.element.removeAttribute('src');player.gain?.disconnect()}
  this.states.clear();
 }
 syncState(state,time){
  const clip=state.clip,segments=clipSegments(clip,state.meta.duration,time),gain=clipGain(this.project(),clip,time,this.visibleVideo(time));
  const used=new Set();
  for(const segment of segments){
   const player=state.players[segment.cycle%state.players.length];used.add(player);
   const switching=player.cycle!==segment.cycle;
   player.cycle=segment.cycle;
   const element=player.element;
   if(element.readyState===0)continue;
   if(switching||Math.abs(element.currentTime-segment.sourceTime)>.25){
    try{element.currentTime=segment.sourceTime}catch{}
   }
   const volume=gain*segment.weight;
   if(player.gain)player.gain.gain.value=volume;
   else element.volume=clamp(volume);
   if(element.paused&&!player.failed)element.play().catch(error=>{player.failed=true;this.onError(error)});
  }
  for(const player of state.players)if(!used.has(player))player.element.pause();
 }
 update(time){
  if(!this.playing)return;
  this.time=time;
  const upcoming=this.clips().filter(({clip})=>clip.path&&time>=clip.start-3&&time<clip.start+clip.duration);
  const activeIds=new Set();
  for(const {clip} of upcoming){
   if(!this.states.has(clip.id))this.ensure(clip).catch(()=>{});
   if(time>=clip.start){activeIds.add(clip.id);const state=this.states.get(clip.id);if(state?.meta)this.syncState(state,time)}
  }
  for(const [id,state] of this.states){
   if(!activeIds.has(id))for(const player of state.players)player.element.pause();
   if(time<state.clip.start-10||time>=state.clip.start+state.clip.duration+10){
    for(const player of state.players){player.element.removeAttribute('src');player.gain?.disconnect()}
    this.states.delete(id);
   }
  }
 }
}

root.DevlogLiveAudio=LiveAudio;
if(typeof module!=='undefined'&&module.exports)module.exports={LiveAudio,clipSegments,clipGain};
})(typeof window==='undefined'?globalThis:window);
