import copy
import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch, MagicMock

import plugin_registry as registry
import storage
from desktop import DesktopBridge
from project import blank_project, save, load, ROOT


class PluginTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(dir=ROOT/'.tooling/devlog-editor')
        self.folder=Path(self.temp.name);self.file=self.folder/'test.devfilm.json'
        self.settings=patch.object(registry,'SETTINGS',self.folder/'settings.json');self.settings.start()
        self.p=blank_project('플러그인 검증')

    def tearDown(self):self.settings.stop();self.temp.cleanup()

    def run_plugin(self,action,**options):return registry.invoke('devlog',action,self.p,self.file,options)['project']

    def test_script_creates_standard_subtitles_without_mutating_input(self):
        original=copy.deepcopy(self.p)
        result=self.run_plugin('script',text='첫 문장\n둘째 문장',start=2,seconds=3)
        self.assertEqual(self.p,original)
        clips=result['tracks'][0]['clips']
        self.assertEqual([c['start'] for c in clips],[2,5])
        self.assertTrue(result['export']['subtitles'])
        save(self.file,result)
        registry.set_enabled('devlog',False)
        self.assertEqual(load(self.file),result)
        with self.assertRaises(ValueError):self.run_plugin('script',text='꺼진 플러그인')

    def test_srt_offsets_and_multiline_text(self):
        result=self.run_plugin('srt',text='1\n00:00:01,200 --> 00:00:02,500\n첫째\n둘째\n\n2\n00:00:03,000 --> 00:00:04,000\n셋째',start=5)
        clips=result['tracks'][0]['clips']
        self.assertEqual(clips[0]['start'],6.2)
        self.assertAlmostEqual(clips[0]['duration'],1.3)
        self.assertEqual(clips[0]['text'],'첫째\n둘째')
        self.assertEqual(clips[1]['start'],8)

    def test_supertonic_narration_creates_reopenable_audio_and_subtitles(self):
        result=self.run_plugin('narration',text='안녕하세요.',voice='M1',speed=.95,start=2)
        audio,subtitles=result['tracks'][:2]
        clip=audio['clips'][0]
        self.assertEqual(audio['kind'],'audio')
        self.assertEqual(subtitles['clips'][0]['start'],2)
        self.assertAlmostEqual(subtitles['clips'][0]['duration'],clip['duration'])
        self.assertEqual(subtitles['clips'][0]['linkGroup'],clip['linkGroup'])
        self.assertTrue((self.file.parent/clip['path']).is_file())
        self.assertTrue((self.file.parent/clip['path']).parent.joinpath('provenance.json').is_file())
        self.assertEqual(result['assets'][0]['generatedWith'],'Supertonic 3')
        save(self.file,result)
        self.assertEqual(load(self.file)['tracks'][0]['clips'][0]['path'],clip['path'])

    def test_invalid_script_srt_and_overrun_rejected(self):
        for action,options in [('script',{'text':''}),('script',{'text':'a','start':3599,'seconds':4}),
                               ('srt',{'text':'bad'}),('srt',{'text':'1\n00:00:02,000 --> 00:00:01,000\nbad'}),
                               ('sequence',{'assets':[]})]:
            with self.subTest(action=action,options=options),self.assertRaises(ValueError):self.run_plugin(action,**options)
        self.assertEqual(self.p['tracks'],[])

    def test_credits_preserve_existing_tracks(self):
        self.p=self.run_plugin('script',text='원본')
        old=copy.deepcopy(self.p['tracks'])
        result=self.run_plugin('credits',text='제작: 나\n음악: 작곡가',start=20,seconds=10)
        self.assertEqual(result['tracks'][1:],old)
        self.assertEqual(result['tracks'][0]['clips'][0]['duration'],10)

    def test_comparison_arranges_before_after_with_titles(self):
        for name in ('before.mp4','after.mp4'):(self.folder/name).write_bytes(b'fixture')
        self.p['assets']=[dict(id='before',kind='video',name='이전',path='before.mp4'),
                          dict(id='after',kind='video',name='현재',path='after.mp4')]
        with patch('render.probe',return_value={'duration':8}):
            result=self.run_plugin('comparison',assets=['before','after'],start=3,seconds=2)
        video,captions=result['tracks'][:2]
        self.assertEqual([clip['start'] for clip in video['clips']],[3,5])
        self.assertEqual([clip['duration'] for clip in video['clips']],[2,2])
        self.assertEqual([clip['text'] for clip in captions['clips']],['이전','현재'])
        self.assertTrue(result['export']['subtitles'])

    def test_manifest_resources_and_ids_are_confined(self):
        self.assertEqual(registry.manifests()[0]['id'],'devlog')
        with self.assertRaises(ValueError):registry.resource('devlog','../../editor.js')
        with self.assertRaises(ValueError):registry.resource('devlog','plugin.py')
        with self.assertRaises(ValueError):registry.invoke('devlog','unknown',self.p,self.file,{})

    def test_publish_output_preserves_existing_and_cleans_failed_copy(self):
        from render import publish_output
        source=self.folder/'source.mp4';target=self.folder/'output.mp4'
        source.write_bytes(b'new');target.write_bytes(b'original')
        with self.assertRaises(FileExistsError):publish_output(source,target)
        self.assertEqual(target.read_bytes(),b'original')
        target.unlink()
        with patch('render.shutil.copyfileobj',side_effect=OSError('disk full')):
            with self.assertRaises(OSError):publish_output(source,target)
        self.assertFalse(target.exists());self.assertEqual(source.read_bytes(),b'new')
        publish_output(source,target)
        self.assertEqual(target.read_bytes(),b'new');self.assertFalse(source.exists())

    def test_desktop_pickers_grant_selected_parent_and_cancel_is_noop(self):
        bridge=DesktopBridge();bridge._window=MagicMock()
        roots=storage.WRITE_ROOTS.copy()
        try:
            with patch('webview.FileDialog'):
                bridge._window.create_file_dialog.return_value=None
                self.assertIsNone(bridge.choose_folder())
                self.assertEqual(roots,storage.WRITE_ROOTS)
                bridge._window.create_file_dialog.return_value=(str(self.folder),)
                self.assertEqual(bridge.choose_folder(),str(self.folder))
                self.assertIn(self.folder,storage.WRITE_ROOTS)
                bridge._window.create_file_dialog.return_value=(str(self.file),)
                self.assertEqual(bridge.open_project(),str(self.file))
                self.assertEqual(bridge.choose_media('audio'),str(self.file))
                with self.assertRaises(ValueError):bridge.choose_media('subtitle')
        finally:storage.WRITE_ROOTS.clear();storage.WRITE_ROOTS.update(roots)

    def test_custom_titlebar_controls_call_native_window(self):
        bridge=DesktopBridge();bridge._window=MagicMock()
        bridge.minimize_window()
        self.assertTrue(bridge.toggle_maximize())
        self.assertFalse(bridge.toggle_maximize())
        bridge.close_window();bridge._actions.join()
        bridge._window.minimize.assert_called_once()
        bridge._window.maximize.assert_called_once()
        bridge._window.restore.assert_called_once()
        bridge._window.destroy.assert_called_once()


if __name__=='__main__':unittest.main()
