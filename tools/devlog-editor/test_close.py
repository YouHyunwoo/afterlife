import threading
import unittest
from unittest.mock import MagicMock
from close_controller import CloseController


class CloseTests(unittest.TestCase):
    def make_controller(self, dirty=False, confirm=True, busy=False):
        window=MagicMock()
        window.evaluate_js.return_value=dirty
        window.create_confirmation_dialog.return_value=confirm
        controller=CloseController(window,lambda:busy)
        return window,controller

    def check(self,controller):
        # 비동기 확인의 끝을 기다리되 UI 스레드는 차단하지 않는다.
        done=threading.Event();original=controller._check
        def checked():
            try:original()
            finally:done.set()
        controller._check=checked
        self.assertFalse(controller.request_close())
        self.assertTrue(done.wait(2))

    def test_close_event_releases_ui_before_waiting_for_javascript(self):
        window,controller=self.make_controller()
        ui_released=threading.Event();entered=threading.Event();destroyed=threading.Event()
        def evaluate(script):
            entered.set()
            if not ui_released.wait(2):raise AssertionError('UI 스레드 교착')
            return False
        window.evaluate_js.side_effect=evaluate
        window.destroy.side_effect=destroyed.set
        self.assertFalse(controller.request_close())
        self.assertTrue(entered.wait(1))
        self.assertFalse(controller.request_close())
        self.assertEqual(window.evaluate_js.call_count,1)
        ui_released.set()
        self.assertTrue(destroyed.wait(2))
        self.assertTrue(controller.request_close())

    def test_saved_project_closes_without_confirmation(self):
        window,controller=self.make_controller();self.check(controller)
        window.create_confirmation_dialog.assert_not_called()
        window.destroy.assert_called_once()

    def test_unsaved_cancel_keeps_window_and_allows_retry(self):
        window,controller=self.make_controller(dirty=True,confirm=False);self.check(controller)
        window.destroy.assert_not_called()
        window.create_confirmation_dialog.return_value=True
        self.check(controller);window.destroy.assert_called_once()

    def test_busy_job_keeps_window(self):
        window,controller=self.make_controller(busy=True);self.check(controller)
        window.evaluate_js.assert_not_called();window.destroy.assert_not_called()

    def test_failed_javascript_requires_confirmation(self):
        window,controller=self.make_controller(confirm=False)
        window.evaluate_js.side_effect=RuntimeError('webview unavailable')
        self.check(controller)
        window.create_confirmation_dialog.assert_called_once();window.destroy.assert_not_called()


if __name__=='__main__':unittest.main()
