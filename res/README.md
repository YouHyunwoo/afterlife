# 리소스 보관소

게임에서 사용할 엔진 비의존 파일과 공용 제작 자료를 보관한다. 게임 제작 원본은 res/art-source, 유튜브 전용 원본·음원·영상·이미지는 로컬 devlog/youtube, Unity 임포트본은 impls/unity/Afterlife/Assets에 둔다. 폴더를 복사했다고 에셋이 자동 갱신되지는 않는다.

- audio/stone-analysis: 두 돌 음원의 구간 분석·후보·선정 파일
- audio/tree-fall-analysis: 이전 나무 쓰러짐 음원의 분석/후처리 후보, 현재 비활성
- audio/gongu-tree-fall: 현재 사용하는 공유마당 나무 쓰러짐 원본과 출처
- textures/vegetation: 사용자 제공 풀 PNG 4종
- models/world: 프로젝트 기존 공유 FBX 18종의 보관본

리소스 목록과 SHA-256은 manifest.json에 기록한다. 로컬 도구 `python tools/resources/update_manifest.py`로 갱신한다. 이 도구가 있는 `devlog/`는 Git에서 제외한다. 라이선스가 확인되지 않은 파일은 임의로 CC0/자체 제작으로 표시하지 않는다. 공유마당의 공개 크레딧은 [지침](../docs/audio-credits.md)을 따른다.

## 폴더 구분

- art-source/: Blender·Aseprite·이미지 제작 원본과 제작별 후보·검수 자료
- audio/kenney-review/: 외부 음원 원본·출처·선정 자료와 전용 검토 도구
- models/, textures/, images/: 엔진 비의존 사용 리소스
- concepts/, previews/: 제작 시안과 렌더 미리보기
- art-source/pixel-art/: 기존 Unity Workspace의 Aseprite 편집 원본 58개와 추가 편집본 frame-edited.aseprite
- images/pixel-art/: 해당 픽셀 아트 PNG 출력본 68개
- audio/legacy-effects/: 기존 효과음 원본 및 잘라낸 MP3 4개. 출처·라이선스 미확인
- analysis/ui-audit/: UI 구조 과거 UI 분석 Graphify 결과; 실행 캐시는 Git·리소스 목록에서 제외

Unity 프로젝트의 Workspace 자료는 위 경로로 분리했다. 문자열 정리 스크립트는 `tools/localization/arrange-strings.py`에 있다. 스크립트는 `--order`로 기준 언어 JSON을 받아 입력을 보존하고 별도 파일로 출력한다. 예전 기본 기준 `Localization/en.json`은 현재 없으므로 기준을 명시한다. [사용법](../tools/localization/README.md).

원본과 같은 폴더를 기준으로 동작하는 전용 검토 도구는 자료와 함께 둔다. 공통 제작·가공 도구는 루트 tools/에서 관리한다. manifest.json은 하위 제작 manifest도 포함한다. 자기 자신, Python 캐시·로컬 분석 도구 설치본, Blender 자동 백업, 운영체제의 폴더 설정 파일은 목록에서 제외한다.

## 2026-10-07 구조 정리

- references/kenney/: 기존 docs/game-process/reference/kenney 자료를 내용 변경 없이 이전.
- art-source/pixel-art/frame-edited.aseprite: 출력 이미지 폴더에 있던 내용이 다른 편집 원본. 기존 frame.aseprite를 덮어쓰지 않았다.
- 공용 목록 도구는 tools/resources/update_manifest.py이며 새 복제본에서도 실행할 수 있다.
- 원본·임포트본 중복은 역할 구분에 따른 보관이다. 검증·출처 없이 동일 파일을 일괄 삭제하지 않는다.
