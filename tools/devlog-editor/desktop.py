"""Windows 독립 창. 로컬 서버의 시작/종료와 OS 파일 선택을 소유한다."""
import argparse
import json
import queue
import threading
from pathlib import Path

import server
from storage import grant_folder
from runtime_paths import DATA_DIR
from close_controller import CloseController


class DesktopBridge:
    def __init__(self):
        self._window = None
        self._maximized = False
        self._window_error = None
        self._actions = queue.Queue()
        threading.Thread(target=self._window_worker,daemon=True).start()

    def _window_worker(self):
        while True:
            action=self._actions.get()
            try:
                action()
            except Exception as error:
                self._window_error = error
            finally:
                self._actions.task_done()

    def minimize_window(self):
        if self._window_error:raise RuntimeError(str(self._window_error))
        self._actions.put(self._window.minimize)

    def toggle_maximize(self):
        if self._window_error:raise RuntimeError(str(self._window_error))
        if self._maximized:
            self._actions.put(self._window.restore)
        else:
            self._actions.put(self._window.maximize)
        self._maximized = not self._maximized
        return self._maximized

    def close_window(self):
        if self._window_error:raise RuntimeError(str(self._window_error))
        self._actions.put(self._window.destroy)

    def choose_folder(self):
        import webview
        result = self._window.create_file_dialog(webview.FileDialog.FOLDER)
        if not result:
            return None
        folder = str(Path(result[0]).resolve())
        grant_folder(folder)
        return folder

    def open_project(self):
        import webview
        result = self._window.create_file_dialog(webview.FileDialog.OPEN,
                    file_types=('편집 프로젝트 (*.json)',))
        if not result:
            return None
        path = Path(result[0]).resolve()
        grant_folder(path.parent)
        return str(path)

    def choose_media(self, kind):
        import webview
        if kind not in ('video', 'audio'):
            raise ValueError('영상 또는 음성 파일을 선택하세요.')
        pattern = '영상 파일 (*.mp4;*.mov;*.mkv;*.webm;*.m4v)' if kind == 'video' else '음성 파일 (*.wav;*.mp3;*.ogg;*.flac;*.m4a)'
        result = self._window.create_file_dialog(webview.FileDialog.OPEN, file_types=(pattern,))
        if not result:
            return None
        path = Path(result[0]).resolve()
        grant_folder(path.parent)
        return str(path)

    def choose_output(self):
        import webview
        result = self._window.create_file_dialog(webview.FileDialog.SAVE,
                    save_filename='video.mp4',file_types=('MP4 영상 (*.mp4)',))
        if not result:
            return None
        path = Path(result[0]).resolve().with_suffix('.mp4')
        grant_folder(path.parent)
        return str(path)


def main():
    import webview
    parser=argparse.ArgumentParser()
    parser.add_argument('--smoke-test',help='자동 창 검증 결과 JSON 위치')
    args=parser.parse_args()
    server.DESKTOP=True
    DATA_DIR.mkdir(parents=True,exist_ok=True)
    server.CACHE.mkdir(parents=True,exist_ok=True)
    http=server.ThreadingHTTPServer(('127.0.0.1',0),server.Handler)
    thread=threading.Thread(target=http.serve_forever,daemon=True)
    thread.start()
    bridge=DesktopBridge()
    window=webview.create_window('Afterlife Editor',f'http://127.0.0.1:{http.server_port}/',
        js_api=bridge,width=1440,height=960,min_size=(1000,700),background_color='#101516',
        text_select=True,frameless=True,easy_drag=False)
    bridge._window=window
    window.events.maximized += lambda: setattr(bridge,'_maximized',True)
    window.events.restored += lambda: setattr(bridge,'_maximized',False)

    close_controller=CloseController(window,lambda:bool(server.STATE['job'].get('busy')))
    window.events.closing += close_controller.request_close

    def smoke():
        import time
        import tempfile
        import plugin_registry
        from runtime_paths import APP_DIR
        output=Path(args.smoke_test).resolve()
        old_settings=plugin_registry.SETTINGS
        try:
            with tempfile.TemporaryDirectory(dir=output.parent) as folder:
                grant_folder(folder)
                plugin_registry.SETTINGS=Path(folder)/'plugins.json'
                window.evaluate_js('window.SMOKE_FOLDER='+json.dumps(folder))
                from project import blank_project, save
                sample=Path(folder)/'open-sample.devfilm.json'
                save(sample,blank_project('열기 검증'))
                window.evaluate_js('window.SMOKE_OPEN_PATH='+json.dumps(str(sample)))
                window.evaluate_js((APP_DIR/'smoke_ui.js').read_text(encoding='utf-8'))
                result=None
                for _ in range(1200):
                    result=window.evaluate_js('window.SMOKE_RESULT || null')
                    if result:break
                    time.sleep(.1)
                if not result:result={'ok':False,'error':'WebView2 smoke test timeout'}
                if result.get('ok'):
                    result['media']=server.probe(Path(result['output']))
                output.write_text(json.dumps(result,ensure_ascii=False),encoding='utf-8')
        except Exception:
            import traceback
            output.write_text(json.dumps({'ok':False,'error':traceback.format_exc()}),encoding='utf-8')
        finally:
            plugin_registry.SETTINGS=old_settings
            # 검증에서만 만든 임시 프로젝트. 실제 종료 이벤트는 우회하지 않는다.
            window.evaluate_js('modified=false')
            window.destroy()
    try:
        webview.start(smoke if args.smoke_test else None,gui='edgechromium',
                      storage_path=str(DATA_DIR/('webview-smoke' if args.smoke_test else 'webview')),
                      private_mode=bool(args.smoke_test))
    finally:
        server.CANCEL.set()
        http.shutdown();http.server_close();thread.join(timeout=3)


if __name__=='__main__':
    try:
        main()
    except Exception:
        import traceback
        import ctypes
        DATA_DIR.mkdir(parents=True,exist_ok=True)
        log=DATA_DIR/'desktop-error.log'
        log.write_text(traceback.format_exc(),encoding='utf-8')
        ctypes.windll.user32.MessageBoxW(None,
            '편집기를 시작하지 못했습니다. Windows WebView2 Runtime 설치를 확인해주세요.\n오류 기록: '+str(log),
            'Afterlife Editor',0x10)
        raise
