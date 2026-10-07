# 유튜브 공용 TTS

Day01 전용이던 이름과 위치를 정리했다. 기존 생성기는 Day01과 Day02에서 이미 사용했고, 이제 `--day day-XX` 형식으로 다음 회차에도 같은 환경을 사용한다.

## 구성

- `generate_narration.py`: 회차 대본에서 음성·시간표·자막·Unity 임포트본 생성
- `preview_narration_voices.py`: 기존 필름을 바꾸지 않는 목소리 비교
- `vendor/`: Supertonic 추론 코드와 코드·모델 라이선스
- `requirements.txt`: 재설치 의존성
- 루트 `.tooling/youtube-tts/assets/`: 공유 ONNX 모델·음색
- 루트 `.tooling/youtube-tts/venv/`: 공유 Python 환경, Git 제외
- `devlog/youtube/day-XX/source/narration/script.json`: 회차별 입력
- `devlog/youtube/day-XX/media/audio/narration/`: 회차별 출력

## 사용

저장소 루트에서 실행한다. 새 회차는 먼저 `script.json`을 준비한다. 회차별 음성·시간표를 덮어쓸 수 있으므로 준비된 회차만 지정한다.

```powershell
.tooling/youtube-tts/venv/Scripts/python.exe tools/tts/generate_narration.py --day day-02
.tooling/youtube-tts/venv/Scripts/python.exe tools/tts/generate_narration.py --day day-02 --timing-only
```

이전 `devlog/youtube/tools/generate_narration.py` 명령은 공용 구현으로 위임한다. 가상환경 이동 후 확인한 Python 실행파일을 사용한다. 패키지 관리는 경로가 내장된 `pip.exe` 대신 `python.exe -m pip`를 사용한다.

새 환경은 `setup.ps1 -PythonExecutable <Python 3.12 실행파일>`로 만든다. 모델은 이 도구가 자동 다운로드하지 않으며 기존 모델을 `assets/`에 복원한다. 코드를 재설치해도 모델과 회차 출력은 보존한다.

## 검증

`python.exe tools/tts/generate_narration.py --help`와 numpy·soundfile·onnxruntime·vendor 추론 모듈 import를 확인한다. 정리 작업은 새 음성을 생성하거나 기존 대본·음성·필름을 변경하지 않는다. 출처와 공개 표기는 [음원 크레딧](../../docs/audio-credits.md)을 따른다.
