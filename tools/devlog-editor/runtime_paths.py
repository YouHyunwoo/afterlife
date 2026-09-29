"""개발 체크아웃과 설치된 앱의 코드/사용자 데이터 위치를 분리한다."""
import os
import sys
from pathlib import Path

APP_DIR = Path(__file__).resolve().parent
FROZEN = bool(getattr(sys, 'frozen', False))
ROOT = Path.home() if FROZEN else APP_DIR.parents[1]
DATA_DIR = (Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'AfterlifeEditor'
            if FROZEN else ROOT / '.tooling/devlog-editor')
CACHE = DATA_DIR / 'cache'
