# Afterlife

Git 루트는 이 폴더이며 Unity 프로젝트는 `impls/unity/Afterlife/`에서 연다. 통합 개발 브랜치는 `dev`, 검증 기준은 `main`이다. [Git 운영 규칙](docs/git-workflow.md)을 따른다.

## 현재 게임 상태 · 2026-10-07

채집·건설·수리·공격은 버튼 유지 반복 작업과 회당 작업량 강화, 자동 수입은 농장 건설·생산 효율 강화로 확장한다. 본편 필드·슬롯·충전은 폐기·재설계 대상이다. [확정 규칙](docs/game-process/71-click-work-and-farm-automation.md)과 [1차 경제 계산](docs/game-process/72-hold-economy-balance.md)을 문서화했다. 경제 수치는 미채택이며 새 본편 규칙은 코드·씬·SO에 아직 적용하지 않았다. [현재 진행](docs/game-process/status.md).

Day01은 공개 완료, Day02는97.29초 이야기 필름·음성, Day03은13샷/118초 무음 시연을 준비했다. Day02·Day03 실제 화면·조작 확인과 촬영은 사용자 작업으로 남는다. 로컬 제작 상태는 `devlog/youtube/status.md`를 확인한다.

## 폴더 안내

|경로|역할|
|---|---|
|res/|게임 제작 원본·엔진 비의존 리소스·출처·참고 자료|
|tools/|공용 아트·음원·TTS·리소스 목록·문자열 도구|
|impls/unity/Afterlife/|Unity 구현·임포트 에셋·설정|
|devlog/|Git 제외하는 로컬 유튜브·DC 문서와 제작 미디어|
|.tooling/youtube-tts/|여러 회차가 공유하는 로컬 음성 모델·Python 환경, Git 제외|
|D:/area/archives/Afterlife/backups/|외부 복구 자료; 프로젝트 local-backups는 제거 완료|
|Temp/ · logs/|로컬 임시 작업·과거 도구 로그, Git 제외|

Unity 에셋은 최상위 `Legacy/`, `Prototype/`, `Modules/`, `Samples/`, `Devlog/`로 구분한다. `Plugins/`, `Resources/`, `StreamingAssets/` 등 Unity·패키지 전용 경로는 유지한다. [구조·사용법·검증](docs/architecture/project-organization.md).

영상 편집기 **EVE는 외부 독립 프로젝트로 이전 완료**했다. 이 루트에 EVE 폴더는 없다. 현재 회차 편집 JSON과 같은 이름의 `.media` 폴더는 Afterlife에 보존하며 EVE에서 연다. 이전·검증 연혁은 `devlog/youtube/shared/records/eve-extraction.md`에 있다.

별도 실험 프로젝트 `impls/unity/Afterlife 3d Test/`는 현재 루트와 하위 폴더에 없다. 과거 저장소 통합 제외 기록은 남아 있으나 이동·삭제 경위와 외부 위치는 확인되지 않았다.

## 제작과 보관

게임 원본은 `res/art-source/`, 출력은 `res/models/`·`res/textures/`·`res/images/`, 참고 자료는 `res/references/`에 둔다. Unity는 Assets 임포트본을 사용한다. [리소스 관리](res/README.md) · [공용 도구](tools/README.md) · [공용 TTS](tools/tts/README.md).

리소스 변경 후 `python tools/resources/update_manifest.py`로 목록을 갱신한다. 유튜브 Markdown 변경 후 로컬 `python devlog/youtube/tools/render_dashboard.py`로 대시보드를 재생성한다. 배포 ZIP은 `D:/area/archives/Afterlife/releases/<플랫폼>/<버전>/`에 보관한다. 과거 Windows64 ZIP4개는 이전·해시 검증을 완료했다. [배포 보관 절차](tools/releases/README.md)와 [보관 색인](docs/steam/archive-index.json)을 따른다.

Unity Library와 Externals 내용은 유지한다. Builds 폴더는 유지하고 기존 산출물을 비웠다. 앞으로 프로토타입 Windows 빌드는 지정 출력 하위 폴더를 먼저 비워 과거 실행파일·데이터가 새 결과에 섞이지 않게 한다. 공개본은 임시 출력 폴더 밖에 보관한다.
