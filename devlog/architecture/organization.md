# Devlog 구조 개편 계약

## 이번 작업 목록
- [x] 공용 모듈과 Day01 구성 코드 분리
- [x] 클릭/눈 효과의 구체 타깃 의존성 제거
- [x] 음원 설정과 파괴 효과 설정 에셋화
- [x] GUID 보존 에셋 이동 및 프리팹 생성
- [x] res 리소스 원본/임포트 관계와 라이선스 기록
- [x] HTML 관리 화면 및 기록 정리
- [x] 모듈 빌드와 실제 에디터 반영 증거 기록

## 경로와 소유권
|경로|역할|
|---|---|
|devlog/|목표, 상태, 촬영 계획, 기술 문서, 변경 기록과 HTML|
|res/|사용할 음원·PNG·모델 등 엔진 비의존 리소스와 출처|
|res/art-source/|Blender 등 편집 원본과 제작 작업물|
|impls/unity/Afterlife/Assets/Devlog/Modules/|공용 표현 모듈별 코드·셰이더·필수 리소스|
|impls/unity/Afterlife/Assets/Devlog/Day01/|씬, 배치 데이터, 재질, 생성 메시, 프리팹, Day01 조립 코드|

Unity는 Assets 밖의 파일을 직접 임포트하지 않으므로 res는 배포/선정 리소스 보관소, Assets의 파일은 Unity 임포트본이다. GUID가 필요한 에셋은 AssetDatabase.MoveAsset으로 이동하고 .meta를 수작업 생성/수정하지 않는다. 공유 프로토타입의 FBX/URP 에셋은 기존 소비자가 있으므로 이동하지 않는다.

## 모듈 경계
- Contracts: 클릭/눈 타깃의 최소 인터페이스. 에피소드를 참조하지 않는다.
- Terrain: 순수 C# 바이옴/변형/혼합 규칙. 현재 Day01 지도는 샘플 규칙으로 남긴다.
- Interaction: 정확한 삼각형 레이캐스트와 표면색 조회. Contracts만 참조한다.
- Snow: 눈 덮개 메시/침식. Contracts와 Interaction을 참조한다.
- Vegetation, Lighting, Destruction, Hover, Shoreline: 기존 독립 어셈블리를 기능별 폴더로 이동한다.
- Audio: 설정 에셋 기반 타격/파괴음 선택과 재생, 순환 상태 초기화.
- Rendering: 공통 URP 셰이더 및 include. 렌더링은 현재 전역 셰이더 상태를 사용하므로 동시에 여러 쇼케이스를 실행하는 것은 아직 지원하지 않는다.
- Day01 Runtime: 입력, 카메라, 촬영 UI, 모듈 생성/해제와 씬 어댑터만 소유한다.
- Day01 Editor: 기존 씬/메시 생성과 버전 마이그레이션. 재실행으로 사용자 씬을 덮어쓰지 않는다.

## 보존 조건
기존 장면 위치, 오브젝트 이름, 직렬화된 MonoBehaviour GUID, 음원 선택, 효과 기본값, 게임 프로토타입을 유지한다. 프리팹은 Unity PrefabUtility로 생성한다. GUI 키와 효과 전환은 그대로 유지한다. Play/Unity TestRunner는 실행하지 않고 dotnet build --no-restore를 수행한다. 에셋 이동/프리팹 저장은 실행 테스트가 아닌 에디터 저작 작업이다.

진행 상황: Unity 이동 및 프리팹 30개 저장 완료. 최종 구성 12개 어셈블리 빌드 통과. Play/GPU 확인은 별도 대기.

## 루트 폴더 검증 (2026-09-20)

- res/: 제작 원본부터 엔진 비의존 리소스까지 통합 보관한다.
- tools/: 공통 아트·음원 제작 도구. 자료 전용 도구는 해당 res 하위 폴더와 함께 유지한다.
- docs/: 게임 기획·설계·출처와 과거 제작 기록. qa/reference 및 steam 배포·홍보 작업 묶음은 문서 부속 자료로 유지한다.
- devlog/: Day01 제작 현황·아키텍처·검증 기록 및 문서 생성 도구.
- impls/: 엔진별 구현과 임포트 에셋.
- logs/: 제작 도구의 과거 실행 로그. 과거 경로는 실행 당시 증거로 보존한다.
- local-backups/: 기존 복구용 백업. 활성 리소스와 구분한다.

[이동 및 검증 기록](../records/resource-layout.md).
