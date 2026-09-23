# Afterlife

Git 저장소 루트는 이 폴더이며 Unity 프로젝트는 `impls/unity/Afterlife/`에서 연다.
현재 통합 개발 브랜치는 `dev`, 검증 기준 브랜치는 `main`이다.
작업별 브랜치 생성과 병합 방법은 [Git 운영 규칙](docs/git-workflow.md)을 따른다.

## 폴더 안내

|경로|역할|
|---|---|
|res/|제작 원본, 엔진 비의존 리소스, 출처와 검토 자료|
|tools/|공통 아트·음원 제작 도구|
|impls/unity/Afterlife/|Unity 구현과 임포트 에셋|
|devlog/|Git에서 제외하는 로컬 유튜브·DC 개발일지|
|docs/|게임 기획·설계, 과거 기록, 배포·홍보 자료|
|logs/|과거 제작 도구 실행 로그|
|local-backups/|복구용 로컬 백업|

`devlog/`, `logs/`, `local-backups/`, 과거 배포 ZIP(`docs/steam/builds/`), 로컬 분석 도구 설치본은 Git에서 제외한다. 로컬 개발일지는 `devlog/youtube/`와 `devlog/dc/`로 나눈다. Unity 구현은 `impls/unity/Afterlife/Assets/Devlog/Youtube/`와 `impls/unity/Afterlife/Assets/Devlog/DC/`로 나누어 추적한다.
별도 실험 프로젝트 `impls/unity/Afterlife 3d Test/`도 이번 저장소 통합 대상에서 제외했다.

게임 제작 원본은 `res/art-source/`, 내보낸 리소스는 `res/models/`·`res/textures/` 등에 둔다. 유튜브 개발일지의 원본과 음성·영상·이미지는 로컬 `devlog/youtube/`에서 관리한다. Unity는 `Assets/`의 임포트본을 사용한다.

- [리소스 관리](res/README.md)
