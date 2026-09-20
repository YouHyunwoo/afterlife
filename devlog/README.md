# Devlog 제작 관리

게임의 매력을 짧은 영상으로 보여주고 관심 있는 사람들을 모으는 것이 목표다. Day01은 같은 작은 맵이 기본 도형에서 3D 픽셀 환경으로 바뀌는 과정과 직접 만지는 반응을 보여준다.

[관리 화면](index.html)을 열면 목표, 진행 현황, 기능, 모듈, 촬영 계획을 한 페이지에서 볼 수 있다. 내용 원본은 이 폴더의 Markdown이며 HTML은 생성 결과다.

## 읽는 순서

1. [현재 상태](status.md): 완료와 미검증, 다음 작업
2. [Day01 목표와 촬영](day-01/production.md): 확정 범위와 다음 단계 제안
3. [기능 목록](day-01/features.md): 지금 구현한 표현과 조작법
4. [모듈 안내](architecture/modules.md): 책임, 의존성, 재사용 방법
5. [에셋과 프리팹](architecture/assets.md): 편집 위치와 리소스 흐름
6. [검증 방법](records/validation.md): 빌드 증거와 사용자 확인 항목

## 갱신 방법

프로젝트 루트에서 `python devlog/tools/render_dashboard.py`를 실행한다. 변경한 기능 문서와 status.md를 먼저 고치고 HTML을 다시 만든다. HTML에서 직접 편집한 내용은 다음 생성 때 보존되지 않는다.

새 에피소드는 day-02처럼 추가한다. 재사용 코드는 Unity Modules에, 에피소드 배치와 입력 조합은 Day02에 둔다. 공통 모듈을 복사해서 따로 고치지 않는다.
