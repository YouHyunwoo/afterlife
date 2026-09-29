"""실제 로컬 API로 프로젝트 분리·브라우저 재열기·저장 경로를 검증한다."""
import copy
import io
import json
import tempfile
import threading
import unittest
import wave
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.error import HTTPError
from unittest.mock import patch

import server
import storage
from project import ROOT, load, resolve


class StorageTests(unittest.TestCase):
    def setUp(self):
        server.CACHE.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=server.CACHE)
        self.folder = Path(self.temp.name)
        self.state = copy.deepcopy(server.STATE)
        server.STATE.update(project=None, projectPath=str(self.folder/'empty.json'), job={'busy':False})
        self.browser = patch.object(storage, 'BROWSER_PROJECTS', self.folder/'browser')
        self.browser.start()
        self.cache = patch.object(server, 'CACHE', self.folder/'cache')
        self.cache.start()
        self.http = server.ThreadingHTTPServer(('127.0.0.1', 0), server.Handler)
        self.thread = threading.Thread(target=self.http.serve_forever, daemon=True)
        self.thread.start()
        self.url = 'http://127.0.0.1:'+str(self.http.server_port)

    def tearDown(self):
        self.http.shutdown(); self.http.server_close(); self.thread.join()
        server.STATE.clear(); server.STATE.update(self.state)
        server.MEDIA.clear()
        self.cache.stop(); self.browser.stop(); self.temp.cleanup()

    def api(self, route, data=None, raw=None):
        headers={'X-Editor-Token':server.TOKEN, 'Content-Type':'application/json'}
        if raw is not None:
            headers.update({'X-File-Name':'voice.wav','X-Media-Kind':'audio'})
        request=Request(self.url+'/api/'+route,
                        data=raw if raw is not None else json.dumps(data or {}).encode(),headers=headers)
        with urlopen(request) as response:
            return json.load(response)

    def audio(self):
        out=io.BytesIO()
        with wave.open(out,'wb') as wav:
            wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(48000)
            wav.writeframes(b'\0\0'*4800)
        return out.getvalue()

    def test_native_projects_isolate_imports_exports_and_preserve_references(self):
        first=self.api('create',{'folder':str(self.folder),'name':'Day01'})
        a=self.api('upload',raw=self.audio())
        again=self.api('upload',raw=self.audio())
        self.assertEqual(a['path'],again['path'])
        second=self.api('create',{'folder':str(self.folder),'name':'Day02'})
        b=self.api('upload',raw=self.audio())
        source_a=resolve(first['projectPath'],a['path'])
        source_b=resolve(second['projectPath'],b['path'])
        self.assertNotEqual(source_a,source_b)
        self.assertTrue(source_a.is_file() and source_b.is_file())
        self.assertEqual(Path(second['mediaRoot']),self.folder/'Day02.media')
        self.assertEqual(Path(second['exportFolder']),self.folder/'Day02.media/video/exports')
        p=second['project'];p['assets']=[dict(id='voice',kind='audio',name='voice',path=b['path'])]
        moved=self.folder/'elsewhere'/'renamed.devfilm.json'
        saved=self.api('save',{'project':p,'path':str(moved)})
        self.assertEqual(resolve(moved,saved['project']['assets'][0]['path']),source_b)
        self.assertEqual(Path(saved['mediaRoot']),moved.parent/'renamed.media')
        self.assertTrue(source_a.is_file())

    def test_browser_storage_survives_save_reopen_and_new_projects_are_separate(self):
        blank=self.api('blank',{'name':'영상'})['project']
        first=self.api('load-document',{'project':blank})
        asset=self.api('upload',raw=self.audio())
        p=first['project'];p['assets']=[dict(id='voice',kind='audio',name='voice',path=asset['path'])]
        saved=self.api('save',{'project':p})
        opened=self.api('load-document',{'project':saved['portableProject']})
        self.assertEqual(first['projectPath'],opened['projectPath'])
        self.assertEqual(first['mediaRoot'],opened['mediaRoot'])
        self.assertTrue(resolve(opened['projectPath'],opened['project']['assets'][0]['path']).is_file())
        other=self.api('blank',{'name':'영상'})['project']
        other=self.api('load-document',{'project':other})
        self.assertNotEqual(first['mediaRoot'],other['mediaRoot'])
        # 이전 브라우저 JSON은 ID가 없어도 열리고 저장 결과에 ID가 기록된다.
        legacy=copy.deepcopy(saved['portableProject']);legacy.pop('browserStorageId')
        restored=self.api('load-document',{'project':legacy})
        self.assertIn('browserStorageId',restored['project'])
        self.assertTrue(resolve(restored['projectPath'],restored['project']['assets'][0]['path']).is_file())

    def test_default_export_and_invalid_path_do_not_leave_busy_state(self):
        data=self.api('create',{'folder':str(self.folder),'name':'Day02'})
        with self.assertRaises(HTTPError):
            self.api('export',{'project':data['project'],'output':str(ROOT.parent/'outside.mp4')})
        self.assertFalse(server.STATE['job']['busy'])
        complete=threading.Event();calls=[]
        def fake_export(project,project_file,output,work,progress,cancel):
            calls.append(Path(output))
        # 실행 스레드의 사이드카 저장이 끝난 뒤 정리한다.
        real_save=server.save
        def save_and_signal(path,p):
            real_save(path,p);complete.set()
        with patch.object(server,'export',fake_export),patch.object(server,'save',save_and_signal):
            self.api('export',{'project':data['project']})
            self.assertTrue(complete.wait(5))
        self.assertEqual(calls[0].parent,Path(data['exportFolder']))
        self.assertTrue(calls[0].name.startswith('Day02-'))
        self.assertEqual(load(calls[0].with_suffix('.devfilm.json'))['title'],'Day02')

    def test_browser_id_rejects_path_traversal(self):
        p=self.api('blank',{'name':'검증'})['project']
        p['browserStorageId']='../../escape'
        with self.assertRaises(HTTPError):self.api('load-document',{'project':p})


if __name__=='__main__':unittest.main()
