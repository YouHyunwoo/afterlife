# Git 운영 규칙

## 저장소와 프로젝트 경로

- Git 루트: `Afterlife/`
- Unity 프로젝트: `impls/unity/Afterlife/`
- 문서와 제작 기록: `docs/`, `devlog/`
- 제작 원본과 엔진 비의존 리소스: `res/`
- 공통 제작 도구: `tools/`

Unity Hub에는 기존 Unity 프로젝트 경로를 그대로 사용한다. Git 작업은 루트에서 수행하며 Unity 하위 폴더에서도 같은 저장소가 인식된다.

## 브랜치

|브랜치|역할|분기와 병합|
|---|---|---|
|main|검증된 게임의 기준|dev 검증 완료 후 병합|
|dev|공용 코드, 프로토타입, Devlog의 통합 개발|모든 신규 작업의 시작점|
|feat/core/기능명|독립 공용 모듈|dev에서 분기하고 dev로 병합|
|feat/prototype/기능명|게임 프로토타입|dev에서 분기하고 dev로 병합|
|feat/devlog/기능명|Unity 데모와 촬영 장면|dev에서 분기하고 dev로 병합|
|docs/주제|기획, 설계, 제작 기록|dev에서 분기하고 dev로 병합|
|fix/문제명|버그 수정|dev에서 분기하고 dev로 병합|

`prototype`, `devlog`라는 영구 분기 대신 작업 단위 브랜치를 사용한다. 공용 기능 변경은 dev에 통합한 뒤 다른 작업에서 받아 쓴다. 촬영 또는 배포 기준점은 `devlog-day01-v1`, `prototype-v0.1.0` 같은 태그로 보존하며, 실제 검증이 끝났을 때 만든다.

## 기본 작업 흐름

아래 명령은 작업 트리가 깨끗한 상태에서 실행한다. 마지막 이름은 실제 구현할 기능명으로 바꾼다.

```powershell
git switch dev
git switch -c feat/prototype/기능명
# 구현, 관련 .csproj 빌드, 기능별 한국어 커밋
git switch dev
git merge --no-ff feat/prototype/기능명
git branch -d feat/prototype/기능명
```

독립 작업을 동시에 진행할 때는 기능별 Git worktree를 쓸 수 있다. 각 worktree의 Unity 프로젝트는 별개이므로 Unity Library 생성 비용이 추가된다. 열린 Unity 프로젝트에서 브랜치를 바꿀 때는 Play를 종료하고 에디터를 닫아 에셋 재임포트와 저장 충돌을 피한다.

dev의 C# 컴파일과 사용자 플레이 확인을 마친 뒤 main으로 병합한다. 2026-09-20 마이그레이션에서는 main에 경로 변경만 반영했으며, 신규 프로토타입과 Devlog 기능은 dev에 있다.

## 커밋과 검증

- 기능 또는 목적별로 커밋하고 메시지는 한국어로 쓴다. 예: `feat(devlog): 물 호버 효과음 변형 추가`.
- 스크립트와 에셋에는 Unity가 만든 `.meta`를 함께 포함한다. `.meta`를 직접 만들거나 수정하지 않는다.
- 상호 의존하는 코드, 직렬화 참조, 프리팹과 필요한 설정은 함께 커밋한다.
- 구현 설명, 출처, 검증 기록은 해당 기능 변경과 함께 갱신한다.
- C# 검증은 관련 `.csproj`에 `dotnet build --no-restore`를 실행한다. 에이전트는 Unity 테스트나 Play를 실행하지 않는다.
- Devlog Markdown을 바꾸면 `python devlog/tools/render_dashboard.py`로 HTML을 갱신한다.
- 리소스를 바꾸면 `python devlog/tools/resource_manifest.py`로 목록을 갱신한다.
- 파일 크기와 제작 원본 필요성을 확인하고, 향후 큰 바이너리가 늘면 Git LFS 도입을 별도 변경으로 검토한다. 이번 이전은 LFS 이력 변환을 하지 않았다.

## 추적에서 제외하는 파일

루트 `.gitignore`는 백업, 실행 로그, Python 캐시, 로컬 분석 도구 설치본, Blender 자동 백업과 과거 배포 ZIP을 제외한다. Unity의 Library, Temp, 빌드 산출물과 자동 생성 프로젝트 파일은 `impls/unity/Afterlife/.gitignore`에서 제외한다. Unity의 기존 `Afterlife.slnx`는 기존 정책대로 계속 추적한다.

`AGENTS.md`, `CLAUDE.md`는 기존 Unity 저장소의 제외 정책을 유지한다. 로컬 지침 파일은 그대로 보존하며, 공유할 작업 규칙은 이 문서에 기록한다.

`impls/unity/Afterlife 3d Test/`는 기존 저장소 밖에 있던 별도 실험 프로젝트이므로 추적하지 않는다. 제외한 파일은 디스크에서 삭제하지 않았다.

## 마이그레이션 전 이력과 복구

과거 커밋은 해시를 재작성하지 않았다. 이전 커밋에서는 `Assets/`가 Git 루트였고, 마이그레이션 이후에는 `impls/unity/Afterlife/Assets/`로 나타난다. `git log --follow -- 경로`로 개별 파일의 이전 이력을 확인할 수 있다.

기존 `feat/citizen`, `feat/component`, `feat/grid`, `game-feature` 및 원격 추적 참조는 그대로 보존했다. 이 브랜치들은 이전 루트 구조다. 현재 Unity 작업 폴더에서 직접 전환하지 말고 별도 worktree에서 참고하거나 필요한 커밋을 경로 차이를 검토하며 이식한다.

기존 stash도 이전 루트 경로를 사용한다. 새 루트에서 바로 적용하지 말고 이전 기준 커밋의 별도 작업 폴더에서 복원한 후 필요한 파일을 현재 경로로 옮긴다.

로컬 복구 자료는 `local-backups/git-root-migration-20260920/`에 있다.

- `before.bundle`: 마이그레이션 전 브랜치와 stash 참조를 포함한 Git 이력
- `uncommitted-files.zip`, `working-changes.patch`: 기능별 커밋 전 변경 파일
- `unity-before.json`: 원래 HEAD, 참조와 Unity 파일 SHA-256
- `unity-commits.json`, `migration.json`: 커밋 분류와 이전/이후 기준점
- `build-logs/`: 이번 C# 빌드 결과

원격 URL과 추적 참조는 유지했으며 이번 작업에서는 fetch, push 또는 force-push를 실행하지 않았다. 첫 공유 전에는 `git fetch origin`으로 실제 원격 변경을 확인하고 비교한 뒤 main/dev를 정상 push한다. 다른 복제본에서 변경이 없었다는 가정으로 강제 푸시하지 않는다.
