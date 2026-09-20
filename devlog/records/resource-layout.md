# 2026-09-20 폴더 구조 검증과 리소스 정리

## 작업 목록

- [x] 루트 폴더·제작 도구·리소스·문서 참조 점검
- [x] 파일 이동 전후 SHA-256 비교
- [x] 제작 원본과 음원 검토 자료 재배치
- [x] 도구 경로·문서·리소스 목록·대시보드 갱신

## 변경 전후

|변경 전|변경 후|이유|
|---|---|---|
|루트 art-source/|res/art-source/|리소스 하위로 제작 원본 통합|
|art-source/audio/kenney-review/|res/audio/kenney-review/|외부 음원 선정·분석 자료의 역할에 맞춤|
|res/images/tilesets/tileset.aseprite|res/art-source/tilesets/tileset.aseprite|편집 원본과 사용 PNG 분리|

공통 제작 도구의 입력 경로를 수정했다. 동물 생성기의 머신 고정 루트도 스크립트 위치 기준으로 변경했다. Kenney 전용 도구는 자신의 폴더를 기준으로 동작하므로 자료와 함께 유지한다.

리소스 목록 생성기의 manifest.json 제외 조건을 목록 파일 자체로 한정하여 하위 제작 manifest도 수록한다. 사라진 레거시 개발 계획 링크 1개는 파일 부재를 명시했다.

## 유지한 구조

문서의 qa/reference와 steam 배포·홍보 묶음은 문서 부속 자료로 유지한다. logs와 local-backups는 실행 증거·복구 자료다. 과거 이동 기록의 출발 경로와 실행 로그는 당시 기록으로 보존한다. Unity Assets, .meta 및 직렬화 참조는 수정하지 않았다.

## 검증

882개 파일의 이동 직후 SHA-256이 이동 전과 일치했다. [파일별 이동 경로와 원본 해시](resource-layout-moves.json)를 보존한다. 이후 문서·제작 도구의 경로 수정은 의도된 텍스트 변경이다.

Markdown 로컬 링크 256개, Python 파일 58개의 구문, 리소스 목록 999개 파일의 해시 검증을 통과했다. 하위 제작 manifest 2개가 목록에 포함됨을 확인했다. Devlog HTML을 재생성했다. Blender 재생성 및 Unity 실행은 수행하지 않는다. Blender 내부에 저장된 과거 출력 경로는 바이너리를 수정하지 않았으므로 수동 내보내기 시 현재 목적지를 확인해야 한다.
