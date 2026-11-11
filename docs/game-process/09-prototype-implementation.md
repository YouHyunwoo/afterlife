# M4 구현 작업 목록

> 확정 기획 · 2026-10-03: 채집·건설·수리·공격은 버튼을 누르고 있는 동안 작업 주기에 맞춰 반복하고, 강화는 작업 1회당 작업량을 높인다. 자동 수입은 농장 건설·생산 강화로 확장한다. 본편 상호작용 필드·슬롯·충전·호버 자동 작업은 폐기한다. [새 규칙·사용법·검증](71-click-work-and-farm-automation.md). 새 규칙은 게임 코드·에셋에 아직 적용하지 않았다. 이전 규칙·수치·QA는 당시 기록으로 보존한다.




사용자 구현 요청으로 M3 첫 범위를 개발 기준으로 채택하고 M4 시작. M2 상세는 후속 검증으로 남긴다.

- [x] Unity6 본 프로젝트 안에 격리된 런타임/설정/새 씬 구성
- [x] 순수 모델: 자원·건설·영역·주민·커서·전투·2웨이브·결과/재시작
- [x] 3D 픽셀 표시·사각 격자 배치·자유 이동·외곽 공격·호버
- [x] 교역소1개 자동 환전·병영 선불 생산/취소·집결/순찰
- [x] 관련 C# 프로젝트 dotnet build --no-restore 및 결함 수정
- [x] 사용 방법·직접 플레이 검증 절차·현재 한계 기록

개발 대상: impls/unity/Afterlife (Unity6000.0.69f1/URP17.0.4). 3d Test는 Unity2022로 확인되어 그대로 확장하지 않는다. 기존 수정된 Afterlife.slnx/EditorBuildSettings 및 _EmberVillage는 보존한다.

Unity 에디터 테스트/플레이모드 스모크는 AGENTS.md에 따라 실행하지 않는다. .meta는 작성하지 않고 Unity가 생성한다. 새 씬은 독립 부트스트랩 방식으로 연결한다.

## 결과 · 코드 구현/C# 컴파일 완료, Unity 플레이 검증 대기

새 파일은 Assets/Prototype 및 Tools/AfterlifePrototype 아래에 격리했다. 새 씬은 Assets/Prototype/Scenes/Afterlife_PixelPrototype.unity. .meta는 직접 작성하지 않았으며 Unity 임포트 시 생성한다. 기존 씬/프로젝트 설정/기존 수정 파일은 변경하지 않았다.

### 실행 방법 · 사용자 직접 확인

1. Unity6000.0.69f1에서 impls/unity/Afterlife 프로젝트를 연다. 스크립트 임포트/컴파일 완료를 기다린다.
2. Tools → Afterlife → Open Pixel Prototype Scene 메뉴를 실행한다. 전용 설정·3D 렌더 자산 생성 후 새 씬을 연다. 편집 중인 씬이 있으면 Unity의 저장 안내가 나타난다.
3. Game 창을1280×720 이상으로 두고 Play를 누른다. 씬은 편집 시 비어 있으며 Play 시 독립 부트스트랩이 집·지도·UI를 생성한다. 에디터에서 씬 파일을 직접 열어도 임시 렌더 자산으로 실행할 수 있지만 메뉴 사용을 권장한다.
4. 좌클릭으로 자원/적 주변에 원형 작업 범위를 놓는다. 건설 버튼 후 지면 클릭으로 배치. 우클릭은 건설/집결 모드 해제, 기본 모드에서는 작업 해제다.
5. 교역소 완공 후 환전ON으로 돈을 확보하고, 건설 재료를 모을 때OFF로 돌린다. 병영을 완공하고 전사 생산을2회 주문한다. 병영 클릭 → 집결점 지정 버튼 → 빈 지면 클릭으로 기존/새 전사의 위치를 지정한다.
6. 모든 집 파괴로 실패 또는2웨이브 전멸 방어로 성공한 뒤 새로 시작 버튼을 확인한다.

### 구현 구조와 임시 수치

- PrototypeModel.cs: Unity 오브젝트 없는 순수 판 데이터·경제·건설·커서·생산·전투/웨이브 모델. 자유 좌표 이동에 내부0.25타일 길 찾기 표본을 사용한다. 유닛을1×1 배치 타일 중심으로 제한하지 않는다.
- PrototypeSettings.cs: ScriptableObject 설정을 순수 Balance로 복사. 준비60초/정비45초, 대상별 목재2·석재1/초, 커서 피해8/초, 전사돈10/10초 등 가설.
- 주민1 이후1명당 대상별 작업/공격 효율+9%, 반경+4%. 집 영역 반경3.8, 커서 기본2.1. 근접 집 반경 보너스는 M3 축소대로 미포함.
- PrototypeView.cs: 3D 임시 모델·격자 표시·호버 밝기/외곽·배치 미리보기·한국어UI·입력. 360픽셀 내부 높이의 Point 렌더 텍스처를 확대한다.
- PixelSurface.shader: URP 단순3단계 명암·제한된 색 표현. 정식3D 아트/그림자/애니메이션 제작이나 화면 검증 완료를 의미하지 않는다.
- PrototypeScene.cs: 새 씬 이름에 한정된 부트스트랩, Moonstone.Ore.Container로 모델/설정/뷰를 조립. 런타임 전용 URP를 사용하고 종료 시 이전 파이프라인으로 복귀. 에디터 메뉴는 전용 자산만 생성한다.
- 기존 Afterlife.Dev.State/Mode는 레거시 MonoBehaviour 및 기존 어셈블리 의존을 확인했다. 이를 확장해 기존 씬 상태에 결합하지 않고 새 씬의 순수 Phase/InputMode로 최소 상태 전환을 분리했다. Moonstone DI는 유지한다.
- 수리: 체력10당 목재1, 재료 부족 대기. 취소 환급은 미진행 비율에서 내림. 마지막 집 파괴와 마지막 적 처치 동시는 패배 우선. 환전은1초 간격으로 정수 잔량 전부 판매, 초기OFF, 교역소 공사 중도1개 제한. 기존 제안 보존량 설정은 첫 범위에서 제외했다.
- 2×1 검증 건물 버튼은 무료 개발용 대상이며 본편 콘텐츠/경제가 아니다. 큰 건물 외곽 공격 확인에만 사용한다.

### 실제 검증 증거

명령: 프로젝트 폴더에서 dotnet build Afterlife.Prototype.Validation.csproj --no-restore --nologo -v minimal

결과: 종료코드0, 경고0, 오류0. 새 Runtime/Editor C# 파일을 모두 포함하고 설치된 Unity 및 Library DLL을 참조하는 별도 검증 프로젝트다. Unity 생성 프로젝트는 수정하지 않았다. NuGet 의존성 없이 명시적 DLL만 검사하므로 패키지 자산/의존성 파일 생성을 생략한다. 재현 도구는 Tools/AfterlifePrototype/Build-Prototype.ps1.

초기 검증 프로젝트에서 project.assets.json 부재로 빌드 단계가 실패했으나, 외부 복원 없이 DLL 전용 검증 설정을 수정하여 빌드 통과했다. 이 기록은 Unity 에셋 임포트·셰이더 컴파일·씬 직렬화·플레이·프레임률 검증 증거가 아니다.

### 직접 검증해야 하는 항목과 기대 결과

- 새 씬 실행: 집1/주민1/재화0과 픽셀 화면·UI가 표시됨. Console의 셰이더/URP/부트스트랩 오류가 없는지 확인.
- 동시 작업: 나무2개면 각각 채집되고 다른 작업/공격 때문에 속도가 나뉘지 않음. 원 종료 후 새 적/피해에는 재클릭 필요.
- 농장 완공: 상한/주민·반경·작업/공격 효율 증가. 집 파괴 시 즉시 재계산.
- 건물 배치/호버: 격자 유효성 표시와 비용 차감 일치. 자원·아군·적·공사·완공 건물에 호버 표시, 호버만으로 작업 위치 변경 없음.
- 2×1 건물: 적이 긴 변/짧은 변/모서리 외곽에서 공격하고 내부에 들어가지 않음. 여러 적의 겹침/정체는 후속 개선 가능성이 있음.
- 교역소: 공사 중 중복 금지,ON 판매/OFF 중단, 파괴 후 돈 유지·재건 가능. 병영 예약·취소 환급·생산/집결과 두 번째 웨이브 연결.
- 승패/재시작: 마지막 완공 집 소멸 시 실패, 잔여 적이 있으면 클리어하지 않음. 재시작 후 이전 판 재화/병력/건물 제거.

프로젝트 AGENTS.md의 “검증은 관련 .csproj에 대한 dotnet build --no-restore만 수행” 지침 때문에 Unity 에디터 테스트·PlayMode 스모크는 실행하지 않았다. M4는 코드/컴파일 완료, 실제 플레이 확인 대기 상태다. M5 인간 경험 검증은 아직 시작하지 않았다.

## Unity CLI 빌드 · 진행 중

사용자가 Unity CLI 빌드를 명시적으로 요청하여 기존 dotnet만 검증하던 제한의 이번 작업 예외로 진행한다. Unity6000.0.69f1 실행 중 프로세스/잠금 없음 확인 후 배치모드 시작. Windows 개발 빌드에 새 씬만 지정하며 실행/플레이 테스트는 별도다.

- [x] 빌드 진입점 추가·C# 컴파일 확인
- [x] 기존 프로젝트 설정 백업·배치모드 실행
- [x] Unity 임포트·셰이더·Player 빌드 결과 확인 및 필요한 수정
- [x] 기존 설정 복원 확인·실행 파일 경로/결과 기록

## Unity CLI 결과 · Windows 개발 빌드 성공

Unity6000.0.69f1, StandaloneWindows64, Mono Development. 새 Afterlife_PixelPrototype 씬만 포함했다.

- BuildReport: Succeeded, 오류0, 경고0, 크기145,566,601바이트, Player 빌드 소요30.907초(에디터 시작/임포트 시간 별도).
- 실행 파일: impls/unity/Afterlife/Builds/PixelPrototypeWindows/AfterlifePrototype.exe
- 실행에 필요한 *_Data·UnityPlayer.dll·MonoBleedingEdge 등은 같은 빌드 폴더에 있다. exe만 따로 옮기지 않는다.
- 최종 로그: impls/unity/Afterlife/Logs/PrototypeBuild/unity-build.log. 빌드 결과: Builds/PixelPrototypeWindows/build-summary.txt.
- EXE SHA256: a676dcdbe72ce11da17b53cb53cf93d139af9b371606572caffd6ae189b05223. 실행 파일만의 해시이며 전체 데이터 번들의 해시는 아니다.
- 재빌드 진입점: Afterlife.Prototype.Editor.PrototypeTools.BuildWindows. Tools/AfterlifePrototype/Build-Windows.ps1은 에디터 실행 여부 검사·로그·원래 설정 복원을 묶는 실행 도구다. 이번 실제 실행은 같은 진입점의 직접 CLI 호출로 수행했다.

첫 시도에서는 Moonstone0.10.1의 Arc/D3 런타임 코드가UnityEditor.AssetDatabase를 직접 참조해Player 컴파일6건이 실패했다. Unity Package Manager Embed로 Packages/com.materialstone.moonstone에 포함하고 해당 검색을UNITY_EDITOR와Player Resources 조회로 분리했다. 설정이 없는 선택적Arc/D3 자동 부트스트랩은Player에서 생략한다. 프로토타입의Ore.Container는 그대로 유지한다. 수정 내역은 패키지AFTERLIFE-PATCH.md. 직접PackageCache나.meta를 편집하지 않았다.

이후Unity Player 컴파일·셰이더/에셋 빌드·실행 파일 패키징 통과. 빌드용렌더/품질/백엔드 설정은 복원하고 프로젝트 설정4개를 작업 전 백업과 바이트 비교해 일치 확인했다. 최초 변경 없던URP 전역 설정의 빌드 자동 갱신도 원본으로 복원했다. 기존사용자 Afterlife.slnx/EditorBuildSettings 변경과_EmberVillage는 보존했다.

이번 사용자 요청으로Unity CLI 빌드를 허용받아 검증 범위를 확장했다. 이전의“Unity 에셋/셰이더 미검증”은이 빌드 대상으로해소되었으나실행 파일을실제로플레이하거나화면/조작/성능/2웨이브완주를확인한것은아니다. M4는Unity빌드완료·플레이확인대기, M5인간경험검증은미시작.
