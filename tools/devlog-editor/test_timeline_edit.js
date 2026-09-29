'use strict';
const assert=require('node:assert/strict');
global.window=global;
require('./timeline_edit.js');
let serial=0;const id=()=>`new-${++serial}`;
const sample=()=>({assets:[{id:'asset',kind:'audio',name:'voice',path:'voice.wav',duration:12}],tracks:[
 {id:'video',kind:'video',clips:[{id:'v1',start:0,duration:2,sourceIn:1,path:'movie.mp4'},{id:'v2',start:4,duration:2,sourceIn:3,path:'movie.mp4'}]},
 {id:'audio',kind:'audio',clips:[{id:'a1',start:0,duration:2,sourceIn:1,path:'voice.wav',linkGroup:'sentence'}]},
 {id:'subtitle',kind:'subtitle',clips:[{id:'s1',start:0,duration:2,text:'문장',linkGroup:'sentence'}]}
]});

{
 const original=sample(),moved=EditorTimeline.move(original,'audio','a1','audio',3);
 assert.equal(original.tracks[1].clips[0].start,0);
 assert.equal(moved.tracks[1].clips[0].start,3);
 assert.equal(moved.tracks[2].clips[0].start,3);
}
{
 const {project,rightId}=EditorTimeline.split(sample(),'audio','a1',1,id);
 const left=project.tracks[1].clips.find(c=>c.id==='a1'),right=project.tracks[1].clips.find(c=>c.id===rightId);
 assert.deepEqual([left.duration,right.start,right.duration,right.sourceIn],[1,1,1,2]);
 assert.equal(project.tracks[2].clips.length,2);
 assert.equal(project.tracks[2].clips[1].linkGroup,right.linkGroup);
 assert.notEqual(right.linkGroup,left.linkGroup);
}
{
 const trimmed=EditorTimeline.trim(sample(),'audio','a1','start',.5);
 assert.deepEqual([trimmed.tracks[1].clips[0].sourceIn,trimmed.tracks[1].clips[0].duration,trimmed.tracks[2].clips[0].start],[1.5,1.5,.5]);
}
{
 const resized=EditorTimeline.resize(sample(),'audio','a1',1.5);
 assert.equal(resized.tracks[1].clips[0].duration,1.5);
 assert.equal(resized.tracks[2].clips[0].duration,1.5);
}
{
 const project=sample();project.markers=[{id:'during',title:'삭제',time:1,done:false},{id:'after',title:'이동',time:4,done:false}];
 const deleted=EditorTimeline.rippleDelete(project,'video','v1',id);
 assert.deepEqual(deleted.tracks[0].clips.map(c=>c.start),[2]);
 assert.equal(deleted.tracks[1].clips.length,0);
 assert.equal(deleted.tracks[2].clips.length,0);
 assert.deepEqual(deleted.markers.map(marker=>marker.time),[2]);
}
{
 const project=sample();project.tracks[1].clips[0].start=2;project.tracks[1].clips[0].duration=4;delete project.tracks[1].clips[0].linkGroup;
 const deleted=EditorTimeline.rippleDelete(project,'video','v1',id);
 assert.deepEqual([deleted.tracks[1].clips[0].start,deleted.tracks[1].clips[0].sourceIn,deleted.tracks[1].clips[0].duration],[0,1,4]);
}
{
 const project=sample();project.tracks[1].clips[0].duration=7;delete project.tracks[1].clips[0].linkGroup;
 const deleted=EditorTimeline.rippleDelete(project,'video','v2',id);
 assert.deepEqual(deleted.tracks[1].clips.map(c=>[c.start,c.duration,c.sourceIn]),[[0,4,1],[4,1,7]]);
}
{
 const project=sample();project.tracks[0].clips[1].start=2;
 assert.throws(()=>EditorTimeline.move(project,'video','v1','video',1),/겹칩니다/);
 assert.equal(project.tracks[0].clips[0].start,0);
}
{
 const project=sample();delete project.tracks[1].clips[0].linkGroup;delete project.tracks[2].clips[0].linkGroup;
 const linked=EditorTimeline.linkAtStart(project,'audio','a1',id);
 assert.equal(linked.tracks[1].clips[0].linkGroup,linked.tracks[2].clips[0].linkGroup);
 assert.equal(EditorTimeline.unlink(linked,'audio','a1').tracks[2].clips[0].linkGroup,undefined);
}
{
 const project=sample();delete project.tracks[1].clips[0].linkGroup;delete project.tracks[2].clips[0].linkGroup;
 const linked=EditorTimeline.linkAtStart(project,'video','v1',id);
 assert.equal(linked.tracks[0].clips[0].linkGroup,linked.tracks[1].clips[0].linkGroup);
 assert.equal(linked.tracks[0].clips[0].linkGroup,linked.tracks[2].clips[0].linkGroup);
 const moved=EditorTimeline.move(linked,'video','v1','video',2);
 assert.deepEqual(moved.tracks.map(track=>track.clips[0].start),[2,2,2]);
}
console.log('timeline editing: 10 cases passed');
