'use strict';
const assert=require('node:assert/strict');
const {clipSegments,clipGain}=require('./live_audio.js');

const clip={start:10,duration:20,sourceIn:2,volume:.8,role:'music',loop:true};
assert.deepEqual(clipSegments({...clip,loop:false},10,10.5),[{cycle:0,sourceTime:2.5,weight:1}]);
assert.deepEqual(clipSegments({...clip,loop:false},10,19),[]);
const seam=clipSegments(clip,10,16.5);
assert.equal(seam.length,2);
assert.equal(seam[0].cycle,0);
assert.equal(seam[1].cycle,1);
assert.ok(Math.abs(seam[0].weight-.75)<1e-9);
assert.ok(Math.abs(seam[1].weight-.25)<1e-9);
assert.ok(Math.abs(seam[0].weight+seam[1].weight-1)<1e-9);
assert.deepEqual(clipSegments(clip,10,30),[]);

const project={narrationVolume:.7,music:{volume:.4,fadeIn:2,fadeOut:2,creditsMultiplier:2,creditsRamp:2}};
assert.ok(Math.abs(clipGain(project,{...clip,role:'narration'},11,null)-.56)<1e-9);
assert.ok(Math.abs(clipGain(project,clip,11,null)-.16)<1e-9);
assert.ok(Math.abs(clipGain(project,clip,13,{credits:true,start:11})-.64)<1e-9);
assert.equal(clipGain(project,clip,30,null),0);
console.log('live audio timing and gain: OK');
