# 편집기 플러그인 API v1

플러그인은 앱의 `plugins/<id>/`에 둔다. 데스크톱 배포본에서는 `_internal/plugins/<id>/`다. 이 폴더의 코드는 앱과 같은 권한으로 실행되므로 개발자가 신뢰하는 플러그인만 설치한다. 별도 샌드박스나 온라인 마켓은 제공하지 않는다.

```text
plugins/example/
  manifest.json
  panel.js
  plugin.py
```

`manifest.json` 예시:

```json
{
  "id": "example",
  "name": "예제",
  "version": "1.0.0",
  "apiVersion": 1,
  "description": "설명",
  "entry": "panel.js",
  "actions": ["create"]
}
```

ID는 폴더명과 같고 소문자로 시작하며 소문자·숫자·하이픈만 사용한다. 본체가 목록을 검색하고 사용 여부를 사용자 설정에 저장한다. 켜진 플러그인의 패널만 로드한다.

## 패널 계약

```js
EditorPlugins.register('example', {open() { /* 패널 표시 */ }});
const project = EditorPlugins.getProject(); // 복사본, null 가능
const time = EditorPlugins.getPosition();
await EditorPlugins.run('example', 'create', {start: time});
```

패널은 에디터의 전역 상태를 직접 수정하지 않는다. `run`은 프로젝트 복사본을 서버에 전달하고 성공 결과만 적용한다. 본체가 재생 정지·편집 잠금·실패 표시·되돌리기·변경 상태·화면 갱신을 소유한다. 사용자 파일 이름과 대본은 `textContent`로 표시한다.

## 제작 명령 계약

`plugin.py`는 `run(action, project, project_file, options)`를 제공하고 `{'project': project, 'summary': '추가 완료'}`를 반환한다. `project`는 깊은 복사본이고 `project_file`은 실제 JSON 위치다. 본체는 실행 전후 프로젝트를 검증한다. 실패 시 현재 편집본은 적용하지 않는다.

결과는 schemaVersion 2의 표준 트랙·클립·에셋으로 표현한다. 플러그인 전용 런타임을 요구하는 클립은 만들지 않는다. 플러그인 없이도 열기·저장·렌더링이 가능해야 한다. 파일 경로는 `project.relative/resolve`, 생성 파일 위치는 `storage.media_root/writable`을 사용한다. 원본 파일을 덮어쓰지 않는다.

플러그인 실행은 현재 동기 요청이며 UI가 진행 중 편집을 잠근다. 개발일지 음성 생성은 최대 200문장·600초 실행 제한을 둔다. 중간 취소는 아직 지원하지 않는다. 되돌리기는 타임라인 변경을 복원하며 생성 파일까지 삭제하지 않는다. 배포본에 없는 Python 패키지를 요구하는 플러그인은 빌드 의존성과 포함 설정도 추가해야 한다.

## 본체와 개발일지의 경계

- 본체: 프로젝트·경로·미디어·트랙 편집·재생·MP4·작업 수명·플러그인 실행 계약과 공용 `tts.py` 음성 합성.
- 개발일지: 대본 규칙·SRT 변환·공용 음성 합성 호출·녹화본 배치·크레딧 제작.
- Day01 및 Unity 자료는 실행·배포 시 본체와 플러그인의 필수 의존성이 아니다. 개발 환경에서는 기존 Day01 Supertonic 모델을 편집기 전용 자산 폴더에 복사한다.

창·파일 대화상자는 [pywebview API](https://pywebview.flowrl.com/api/), 실행파일 생성은 [PyInstaller](https://pyinstaller.org/en/stable/)를 사용한다.
