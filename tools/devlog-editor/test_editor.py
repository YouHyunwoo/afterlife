import copy
import json
import tempfile
import unittest
from pathlib import Path
import numpy as np
import soundfile as sf
from project import validate, save, load, rebase, resolve, ROOT, blank_project, create_project
from render import mix, export, probe, run, RATE

class EditorTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(dir=ROOT/'.tooling/devlog-editor')
        self.root=Path(self.temp.name);self.file=self.root/'test.devfilm.json'
        sf.write(self.root/'cue.wav',np.full((RATE//4,2),.2),RATE,subtype='PCM_16')
        sf.write(self.root/'music.wav',np.zeros((RATE*3,2)),RATE)
        self.p=dict(schemaVersion=1,title='검증',video=dict(path='source.mp4',offset=0,volume=0,padEnd=True),
            narrationVolume=1,music=dict(path='music.wav',volume=0,creditsMultiplier=2),export=dict(height=720,fps=30,subtitles=False),
            shots=[dict(id='a',title='검증',duration=2,sourceIn=0,credits=False,cues=[dict(start=.5,duration=.25,text='자막 검증',volume=1,path='cue.wav')])])
        r=run(['-v','error','-y','-f','lavfi','-i','color=c=green:s=128x72:r=30:d=2','-c:v','libx264','-pix_fmt','yuv420p',str(self.root/'source.mp4')],capture_output=True)
        self.assertEqual(r.returncode,0)
    def tearDown(self):self.temp.cleanup()
    def test_roundtrip_rebase(self):
        save(self.file,self.p);self.assertEqual(load(self.file),self.p)
        other=self.root/'sub'/'moved.json';p=rebase(self.p,self.file,other)
        self.assertEqual(resolve(other,p['shots'][0]['cues'][0]['path']),self.root/'cue.wav')
    def test_reject_nonfinite_and_overrun(self):
        p=copy.deepcopy(self.p);p['video']['offset']=float('nan')
        with self.assertRaises(ValueError):validate(p,self.file)
        p=copy.deepcopy(self.p);p['shots'][0]['cues'][0]['start']=1.9
        with self.assertRaises(ValueError):validate(p,self.file)
    def test_audio_position_and_silence(self):
        mix(self.p,self.file,self.root/'mix.wav');a,sr=sf.read(self.root/'mix.wav')
        self.assertEqual(sr,RATE);self.assertEqual(len(a),RATE*2)
        self.assertLess(np.max(np.abs(a[:RATE//2])),1e-5)
        self.assertAlmostEqual(float(a[RATE//2+100,0]),.2,places=3)
        self.assertLess(np.max(np.abs(a[RATE:])),1e-5)
    def test_export_and_no_overwrite(self):
        self.p['export']['subtitles']=True
        out=self.root/'result.mp4';export(self.p,self.file,out,self.root/'work')
        info=probe(out);self.assertTrue(info['audio']);self.assertAlmostEqual(info['duration'],2,delta=.06)
        with self.assertRaises(ValueError):export(self.p,self.file,out,self.root/'work2')
    def test_explicit_start_gap_audio_and_export(self):
        self.p['shots'][0]['timelineStart']=1
        mix(self.p,self.file,self.root/'gap.wav');audio,_=sf.read(self.root/'gap.wav')
        self.assertEqual(len(audio),RATE*3)
        self.assertLess(np.max(np.abs(audio[:RATE*3//2])),1e-5)
        self.assertGreater(audio[RATE*3//2+100,0],.19)
        out=self.root/'gap.mp4';export(self.p,self.file,out,self.root/'gap-work')
        self.assertAlmostEqual(probe(out)['duration'],3,delta=.06)
        def pixel(t):
            r=run(['-v','error','-ss',str(t),'-i',str(out),'-frames:v','1','-vf','scale=1:1','-f','rawvideo','-pix_fmt','rgb24','pipe:1'],capture_output=True)
            self.assertEqual(r.returncode,0);return list(r.stdout)
        self.assertLess(max(pixel(.4)),10)
        self.assertGreater(pixel(1.4)[1],80)
        other=copy.deepcopy(self.p['shots'][0]);other['id']='overlap';other['timelineStart']=2
        self.p['shots'].append(other)
        with self.assertRaises(ValueError):validate(self.p,self.file)
    def test_credits_gain_and_long_voice_rejection(self):
        sf.write(self.root/'music.wav',np.full((RATE*5,2),.1),RATE)
        self.p['music'].update(volume=.1,fadeIn=.01,fadeOut=.01)
        self.p['shots'][0]['cues']=[]
        self.p['shots'].append(dict(id='b',title='credits',duration=2,sourceIn=0,credits=True,cues=[]))
        mix(self.p,self.file,self.root/'gain.wav');audio,_=sf.read(self.root/'gain.wav')
        self.assertAlmostEqual(float(audio[RATE,0]),.01,places=3)
        self.assertGreater(float(audio[round(3.5*RATE),0]),.017)
        sf.write(self.root/'cue.wav',np.full((RATE*3,2),.1),RATE)
        self.p['shots'][0]['cues']=[dict(start=0,duration=1,text='too long',volume=1,path='cue.wav')]
        with self.assertRaises(ValueError):mix(self.p,self.file,self.root/'bad.wav')

    def test_tracks_migration_independent_audio_and_rebase(self):
        from timeline import migrate
        p=migrate(self.p,self.file)
        self.assertNotIn('shots',p);self.assertEqual(p['schemaVersion'],2)
        p['tracks'][0]['clips'][0]['start']=1
        # 영상 이동이 나레이션 및 자막을 이동시키지 않는다.
        self.assertEqual(p['tracks'][1]['clips'][0]['start'],.5)
        self.assertEqual(p['tracks'][2]['clips'][0]['start'],.5)
        validate(p,self.file,files=True);save(self.file,p);self.assertEqual(load(self.file),p)
        other=self.root/'sub'/'moved.json';rebased=rebase(p,self.file,other)
        self.assertEqual(resolve(other,rebased['tracks'][0]['clips'][0]['path']),self.root/'source.mp4')
        mix(p,self.file,self.root/'independent.wav');audio,_=sf.read(self.root/'independent.wav')
        self.assertGreater(audio[RATE//2+10,0],.19)
        self.assertLess(np.max(np.abs(audio[RATE:])),1e-5)

    def test_track_layers_sources_and_black_export(self):
        from timeline import migrate,video_segments
        p=migrate(self.p,self.file);p['tracks'][1]['enabled']=False
        p['tracks'][0]['clips'][0].update(start=1,duration=1.5)
        r=run(['-v','error','-y','-f','lavfi','-i','color=c=red:s=128x72:r=30:d=2','-c:v','libx264','-pix_fmt','yuv420p',str(self.root/'red.mp4')],capture_output=True)
        self.assertEqual(r.returncode,0)
        p['tracks'].insert(0,dict(id='top',name='위 영상',kind='video',enabled=True,clips=[dict(id='red',title='red',start=1.5,duration=.5,sourceIn=.3,path='red.mp4',volume=0)]))
        seg=video_segments(p);self.assertEqual([(a,b,c['id'] if c else None) for a,b,c in seg],[(0,1,None),(1,1.5,'video-0'),(1.5,2,'red'),(2,2.5,'video-0')])
        out=self.root/'tracks.mp4';export(p,self.file,out,self.root/'tracks-work')
        def rgb(t):
            r=run(['-v','error','-ss',str(t),'-i',str(out),'-frames:v','1','-vf','scale=1:1','-f','rawvideo','-pix_fmt','rgb24','pipe:1'],capture_output=True)
            self.assertEqual(r.returncode,0);return list(r.stdout)
        self.assertLess(max(rgb(.3)),10);self.assertGreater(rgb(1.2)[1],80);self.assertGreater(rgb(1.7)[0],200);self.assertGreater(rgb(2.2)[1],80)

    def test_empty_tracks_and_overlap_validation(self):
        from timeline import migrate,video_segments
        p=migrate(self.p,self.file);p['tracks']=[];p['minimumDuration']=.5
        validate(p,self.file,files=True);self.assertEqual(video_segments(p),[(0,.5,None)])
        out=self.root/'empty.mp4';export(p,self.file,out,self.root/'empty-work')
        self.assertAlmostEqual(probe(out)['duration'],.5,delta=.06)
        p=migrate(self.p,self.file);c=copy.deepcopy(p['tracks'][0]['clips'][0]);c['id']='overlap';c['start']=1;p['tracks'][0]['clips'].append(c)
        with self.assertRaises(ValueError):validate(p,self.file)

    def test_browser_media_import_dedup_and_assets_rebase(self):
        import io
        from unittest.mock import patch
        import media_library
        from timeline import migrate
        library=self.root/'imports';registry={}
        with patch.object(media_library,'IMPORTS',library):
            raw=(self.root/'source.mp4').read_bytes()
            a=media_library.import_stream(io.BytesIO(raw),len(raw),'한글 영상.mp4','video',self.file,registry)
            b=media_library.import_stream(io.BytesIO(raw),len(raw),'다른 이름.mp4','video',self.file,registry)
            self.assertEqual(a['path'],b['path']);self.assertEqual(a['name'],'한글 영상.mp4')
            self.assertEqual(len(list(library.iterdir())),1);self.assertAlmostEqual(a['duration'],2,delta=.06)
            with self.assertRaises(ValueError):media_library.import_stream(io.BytesIO(b'bad'),3,'bad.mp4','video',self.file,registry)
            self.assertEqual(len(list(library.iterdir())),1)
        p=migrate(self.p,self.file);p['assets']=[dict(id='asset',path=a['path'],kind='video',name=a['name'])]
        dest=self.root/'sub'/'project.json';q=rebase(p,self.file,dest)
        self.assertEqual(resolve(dest,q['assets'][0]['path']),resolve(self.file,a['path']))

    def test_create_empty_project_and_reopen(self):
        path,p=create_project(self.root,'새 개발일지',root=self.root)
        self.assertEqual(path.name,'새 개발일지.devfilm.json')
        self.assertEqual(load(path),p)
        self.assertEqual(p['tracks'],[])
        self.assertEqual(p['assets'],[])
        other=self.root/'sub'/'moved.devfilm.json'
        rebased=rebase(p,path,other)
        self.assertEqual(rebased['video']['path'],'')
        self.assertEqual(rebased['music']['path'],'')
        save(other,rebased)
        self.assertEqual(load(other)['title'],'새 개발일지')
        with self.assertRaises(FileExistsError):create_project(self.root,'새 개발일지',root=self.root)
        for name in ('', '../escape', 'CON', 'bad:name'):
            with self.assertRaises(ValueError):blank_project(name)

    def test_windows_folder_picker_does_not_use_tk_runtime(self):
        import base64
        import subprocess
        from unittest.mock import patch
        import server
        selected=str(self.root)
        output=base64.b64encode(selected.encode('utf-16-le'))+b'\r\n'
        with patch.object(server.subprocess,'run',return_value=subprocess.CompletedProcess([],0,output,b'')) as run_picker:
            self.assertEqual(server.dialog('folder'),selected)
            command=run_picker.call_args.args[0]
            self.assertEqual(command[:3],['powershell.exe','-NoProfile','-Sta'])
            self.assertEqual(run_picker.call_args.kwargs['env']['DEVLOG_PICKER_KIND'],'folder')
        with self.assertRaises(ValueError):server.dialog('invalid')

if __name__=='__main__':unittest.main()
