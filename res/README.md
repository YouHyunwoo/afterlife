# 리소스 보관소

게임/Devlog에서 사용할 엔진 비의존 파일을 보관한다. 제작 원본은 res/art-source, Unity 임포트본은 impls/unity/Afterlife/Assets에 둔다. 폴더를 복사했다고 에셋이 자동 갱신되지는 않는다.

- audio/stone-analysis: 두 돌 음원의 구간 분석·후보·선정 파일
- audio/tree-fall-analysis: 이전 나무 쓰러짐 음원의 분석/후처리 후보, 현재 비활성
- audio/gongu-tree-fall: 현재 사용하는 공유마당 나무 쓰러짐 원본과 출처
- textures/vegetation: 사용자 제공 풀 PNG 4종
- models/world: 프로젝트 기존 공유 FBX 17종의 보관본

리소스 목록과 SHA-256은 manifest.json에 기록한다. `python devlog/tools/resource_manifest.py`로 갱신한다. 라이선스가 확인되지 않은 파일은 임의로 CC0/자체 제작으로 표시하지 않는다. 공유마당의 공개 크레딧은 [지침](../docs/audio-credits.md)을 따른다.

## 폴더 구분

- art-source/: Blender·Aseprite·이미지 제작 원본과 제작별 후보·검수 자료
- audio/kenney-review/: 외부 음원 원본·출처·선정 자료와 전용 검토 도구
- models/, textures/, images/: 엔진 비의존 사용 리소스
- concepts/, previews/: 제작 시안과 렌더 미리보기

원본과 같은 폴더를 기준으로 동작하는 전용 검토 도구는 자료와 함께 둔다. 공통 제작·가공 도구는 루트 tools/에서 관리한다. manifest.json은 하위 제작 manifest도 포함한다. 자기 자신, Python 캐시·로컬 분석 도구 설치본, Blender 자동 백업, 운영체제의 폴더 설정 파일은 목록에서 제외한다.
