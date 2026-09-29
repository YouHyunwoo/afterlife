"""WinForms 종료 이벤트에서는 WebView 응답을 동기적으로 기다리지 않는다."""
import threading


class CloseController:
    def __init__(self, window, is_busy):
        self.window = window
        self.is_busy = is_busy
        self._lock = threading.Lock()
        self._pending = False
        self._approved = False

    def request_close(self):
        # FormClosing은 UI 스레드에서 실행된다. 즉시 취소하고 UI 루프를 풀어준다.
        with self._lock:
            if self._approved:
                return True
            if self._pending:
                return False
            self._pending = True
        threading.Thread(target=self._check, daemon=True).start()
        return False

    def _check(self):
        try:
            if self.is_busy():
                self.window.create_confirmation_dialog('작업 진행 중', '진행 중인 작업이 끝난 뒤 종료해주세요.')
                return
            try:
                dirty = self.window.evaluate_js('typeof modified !== "undefined" && modified')
            except Exception:
                dirty = True
            if dirty and not self.window.create_confirmation_dialog(
                    '저장하지 않은 변경', '저장하지 않은 변경을 버리고 종료할까요?'):
                return
            # 확인창을 띄운 사이에 시작된 출력도 보호한다.
            if self.is_busy():
                return
            with self._lock:
                self._approved = True
            self.window.destroy()
        except Exception:
            with self._lock:
                self._approved = False
        finally:
            with self._lock:
                self._pending = False
