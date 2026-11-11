# 다음 세션 인계 · 2026-10-09

> 최신 밸런스 제안: [74 완충 게이지 10일 재설계](74-gauge-balance-redesign.md). 27:15·시작30G·농장35G/5→4.5→4초·나무 작업당1, 기본/채집20% 감소 후 순서 변경/농장 복구 경로의 예산·준비 목표 산술 확인. 필드 전용 강화/보상·풍요 정리안을 함께 기록했다. **제안은 미채택이고 게임 코드·SO 미변경**이다. 실제 지도 공급·전투·사람 검증 전이며 적용 전 새 수치/효과를 사용자와 확정한다.

> 2026-10-09 본편 입력 구현: 현재 커서 단일 대상에 좌버튼 유지 작업, 주기 1초·판 시작 100%·해제 후 완충 대기·대상 전환 충전 유지. 좌버튼 드래그와 필드 설치 입력/슬롯 HUD 제거. UI/포커스 차단 후 재입력 필요. 농장 독립 생산과 기존 회당 효과를 사용한다. [현재 구현·사용법·설계·검증](73-hold-interaction-gauge.md). 새 경제/농장 강화·필드 전용 효과/보상 이관은 별도이며 이전 QA는 현행 검증으로 사용하지 않는다.

## 현재 확인 대상

독립 충전 코어와 프로토타입은 `dotnet build --no-restore`에서 모두 경고/오류 0개로 통과했다. Unity 테스트/Play는 에이전트가 실행하지 않았다. 사용자 플레이 확인은 73의 시작 즉시 작업·1초 반복·해제 후 무작업·재입력/대상 전환·UI/포커스 복귀·농장 독립 생산 절차를 따른다. 신규 메타데이터는 열린 Unity 에디터가 자동 생성하며 기존 씬/프리팹 참조는 보존했다.

아래 내용은 작성 당시의 이전 구현 기록이다.


## 2026-09-27 · 남은 고정 오브젝트의 프리팹 연결

[61 구성·검증 기록](61-prototype-static-composition.md): 환경음 채널, 영력 HUD, 환생 기록, 강화 트리 고정 틀, 스킬 바, 모달 캡처 카메라, 흙먼지를 편집 모드 프리팹/씬 참조로 전환했다. 카탈로그 노드·연결선과 화면 크기에 종속된 RenderTexture는 런타임에 유지한다. C# 빌드·직렬화 참조 확인 완료, 실제 플레이 확인 대기.

## 2026-09-27 · Game Flow Screens 고정 구성

[45 화면 흐름 기록](45-modular-screen-flow.md): 런타임에 조립하던 타이틀·인트로·결과 공용 Canvas를 프리팹으로 만들고 프로토타입 씬에 배치했다. `PrototypeView` 직렬화 참조와 기존 HUD Canvas의 `CanvasGroup`을 저장했으며, 런타임에는 화면 상태·문구·버튼 이벤트만 처리한다. C# 빌드 및 씬 참조 확인 완료, 실제 플레이 확인 대기.

## 2026-09-27 · 프로토타입 고정 오브젝트의 씬 연결

[51 카메라·씬 구성](51-territory-camera-distance.md): `World Camera`, `AudioRoot`, `WorldMarkers`를 프리팹 인스턴스로 하이어라키에 배치하고 `PrototypeView` 직렬화 참조를 연결했다. 오디오 8+4채널과 고정 선 7개, 빌드 마커, Day 배너·버튼·패널 보조 컴포넌트도 에디터에서 관리한다. 화면 크기에 맞춰야 하는 `RenderTexture`와 개수가 변하는 월드 오브젝트는 런타임에 생성한다. 관련 C# 빌드 통과, Unity 플레이 확인 대기.

## 2026-09-26 · 집 영역 기반 카메라

[51 구현 기록](51-territory-camera-distance.md): 시간 경과만으로 멀어지는 동작을 제거했다. `Territory.Count`의 초기 대비 증가량을 목표 거리로 매핑하고, 120타일 증가 시 최대 22.7이다. 시작 거리 16과 프리팹·독립 모듈 구조는 유지한다. 따라가기 속도 3으로 영역 변화에만 부드럽게 반응한다. 관련 C# 빌드 경고/오류 0, Unity 실제 플레이 확인 대기.

## 2026-09-25 · 카메라 거리 증가

[50 이전 구현 기록](50-camera-distance-progression.md): 시간 기반 거리 증가를 처음 추가했다. 현재 동작은 위의 51 집 영역 기반 구현을 따른다. 독립 CameraDistance 코어와 Unity 표현 컴포넌트를 PrototypeCamera 프리팹에 조합했고, 루트/Unity AGENTS.md에 문서 갱신·독립 모듈·프리팹·로직 분리 지침을 추가했다.

## 2026-09-24 · 대륙 전체 랜덤화

[49 구현 기록](49-random-continent.md): 사용자 지정 Astra low 계획·Sol high 구현. `WorldMapLayout`에서 대륙 방향/종횡비/반도·만, 기후 방향, 호수·강 배치를 시드로 생성한다. 시작 11×11 초원, 초기 자원, 연결된 육지, 물 회피 탐험 길을 보호한다. 수계는 바다 연결 및 육지 연결을 확인해 채택한다. Terrain 독립 재빌드와 Point 임시 타입을 사용한 실제 PrototypeTerrain 어댑터 컴파일은 경고·오류 0. 전체 Prototype은 기존 PrototypeRelicData.cs/PrototypeRelicCatalog.cs 중복 정의 CS0101로 실패. Unity 직접 확인은 미실시이며 49의 시드/반경 비교 절차를 따른다.


## 현재 재개 위치 · 영력 필드

기본 조작은 위치에 영력 30을 투자해 고정 필드를 8초 설치하는 방식이다. 최대 영력 100·초당 회복 3, 기본 작업 간격 1초, 기존 기여량 계산과 R8 주기 강화가 연결된다. 대상 클릭 또는 빈 타일 `Shift+클릭`으로 설치하고, 빈 타일 일반 클릭의 건설 메뉴는 유지한다. [47 구현 기록](47-divine-interaction-fields.md)에 코드 경계와 수동 확인 절차가 있다. 관련 C# 임시 프로젝트 컴파일은 경고·오류 0이며 Unity 임포트·직접 플레이는 아직 확인하지 않았다.


## 최신 UI · 강화 트리 아이콘·툴팁·전체 보기 (2026-09-19)

UpgradeTreeScreen 우측 패널/노드 텍스트 제거, UpgradeHoverTooltip·UpgradeNodeVisual 분리. viewport 전체 stretch, 전체 노드 bounds로 초기/전체 보기/리사이즈 자동 맞춤, 부모 가시성은 유지. imagegen 20개 → 8색 48px/투명 0·255/여백4px → 48·32px 시각 검사 → 승인 해시 일치본만 Resources/UI/Upgrades에 적용. PrototypeUpgradeIconImporter가 Point/무압축/Sprite Single 설정 및 비어 있는 Catalog Icon만 연결. 정확한 프롬프트와 검수표는 res/art-source/ui/upgrades-v1. 관련 독립 빌드 오류·경고0, Unity 플레이 미실시. 상세: upgrade-tree-icon-tooltip.md

## 최신 추가 · 45 독립 화면 흐름 모듈 (2026-09-19)

Assets/Modules/Flow에 Unity 비의존 Afterlife.Flow 추가(noEngineReferences, 외부 참조 없음). 상태/전환 정의, Guard, Enter/Exit, 재진입 거절, 예외 시 Faulted, Dispose 계약 제공. PrototypeGameFlow가 게임별 5화면 규칙과 IPrototypeGameSession을 조합하고 Moonstone으로 주입한다. PrototypeFlowScreen은 런타임 기본 화면이며 인트로는 정적 안내(44번 튜토리얼 구현 아님). 게임 외 상태에서는 입력·Tick·월드 카메라/HUD를 끈다. RunEnded 후 Tick/Sync가 끝나면 결과 스냅샷으로 전환. 포기와 결과 화면에서 타이틀 복귀, 다음 Playing 진입 때 모델 Reset. 모델/지형은 씬 수명으로 재사용하고 동적 월드 뷰/모달/연출은 Playing 종료 시 정리. Build-Modules.ps1에 독립 코어 빌드 추가. 컴파일 오류·경고 0. Unity 플레이/테스트·Windows 빌드 미실시. 상세: 45-modular-screen-flow.md, 코어 재사용: Flow/README.md.

## 최신 기획 · 44 단계형 튜토리얼 (2026-09-19)

[44 기획](44-guided-tutorial.md)이 튜토리얼의 채택 명세다. 9×9 맵에서 10원 채집 + 40원 지원 → 농장 30원 → 집 20원 → 낮 5초·밤 방어 → 정산·유물 → 외곽 확장 → 본 게임 Day 1. 모든 집 파괴 시 전투 직전 상태 복원, 수리 무료. 같은 판에서는 성과를 유지하지만 이후 튜토리얼 없는 새 게임은 집 1채로 시작한다. 이번 작업은 문서만 변경했으며 구현·빌드·플레이 검증을 하지 않았다. 진입 UI·중도 저장 정책과 연출/적 세부 수치는 후속 결정 사항이다.

## 최신 추가 · 43 독립 스킬·강화 모듈과 격자 트리 (2026-09-19)

강화/스킬 순수 C# asmdef, 공통 Unity 데이터/트리 UI, 게임 어댑터 분리. 연구소/제단 각 10노드, 복수 부모 표시 OR·해금 AND, 격자 배치. Window/Afterlife/강화 에디터에서 노드 드래그·자동 저장·Undo/Redo. 기본 Progression.asset은 Unity 임포트 후 없을 때만 생성한다. 전체 화면 모달과 연구소 기포/제단 성소 배경, Q/W/E 스킬, 기존 환생 기록 별도 모달 보존. PauseGameplay Inspector 값 유지. 향후 독립 모듈 규칙을 AGENTS.md에 추가. Build-Modules.ps1로 5개 asmdef 경계별 dotnet build --no-restore 성공(오류·경고 0). Unity 테스트/플레이/Windows 빌드 미실시. 상세/수동 확인은 43-modular-skills-upgrade-trees.md.

## 최신 추가 · 42 선택한 Kenney 효과음 (2026-09-18)

사용자 선택 JSON을 기준으로 21개 항목과 추가 나무 변형 01C, 총 22개 WAV 적용. 나무는 01B/01C 각각 50% 랜덤 재생, 보류 02/08/10/11/13 전용음 없음. 승리/패배는 25B 공통. PrototypeAudio가 모델 SoundRequested 이벤트를 구독하고 씬 초기화 때 자동 연결하며 Tools 메뉴/씬 설치는 추가하지 않았다. Resources/AfterlifePrototype/Audio에 음원·출처·CC0 라이선스 보존. Settings.SoundVolume 기본 .7. 프레임 병합/쿨다운/8+4채널 풀/화면·시야 필터. C# 오류·경고 0, Unity 테스트/플레이/Windows 빌드는 미실시. 상세 확인 절차: 42-selected-sound-effects.md.

## 최신 추가 · 41 프로토타입 도구 제거

사용자 요청으로 PrototypeTools.Open/CreateAssets/EnsureFolder 삭제, PrototypeUiInstaller.cs 전체 삭제. BuildWindows의 생성/설치 호출 제거 및 저장 씬/Settings/Pipeline 검증 추가. PrototypeScene/PrototypeUgui 오류 메시지 수정. 기존 씬/에셋 유지. 컴파일 오류·경고 0, Windows 빌드 미실시. 과거 문서의 생성/설치 메뉴 안내 사용 금지. AGENTS에 Tools 신규 메뉴는 사전 명시 승인 필요 규칙 있음. 상세 41-remove-prototype-tools.md.

## 최신 수정 · 실시간 모달 블러

작업 목록: URP/Canvas 렌더 경로 확인 → 전용 UI 카메라 캡처 → 실시간 블러/정리 → 컴파일. 이전 ScreenCapture 정지 이미지를 폐기하고 매 프레임 끝에 Canvas를 전용 카메라에 임시 연결하여 URP SingleCameraRequest로 월드 RawImage·HUD·하위 모달을 RT에 렌더한다. 최상위/블러 배경만 캡처 동안 숨기고 finally에서 Canvas 모드/카메라/거리/alpha를 즉시 복구한다. 기존 화면 렌더 이후 캡처라 팝업을 숨긴 프레임을 화면에 표시하지 않으며 결과는 다음 프레임에 표시된다. RT 재사용, 해상도 변경 시 재생성, 닫기 시 해제, 종료 시 카메라/재질 제거. 일반 카메라 RT에 과거 스크린샷 Y 반전 보정은 적용하지 않는다. BlurRadius는 화면 픽셀 샘플 반경. 실제 Unity GPU/플레이 검증은 미실시: 메뉴/Loot 뒤 실시간 월드·HUD, 설정 스택, 방향·해상도 변경·반복 열기 확인 필요.

## 최신 수정 · 모달별 게임 일시정지 정책

작업 목록: 기존 정지 조건 확인 → 모달 Inspector 정책 추가 → 스택 통합 → 컴파일. 모든 모달에서 simulationTime을 멈추던 조건을 제거하고 PrototypeModalSurface.PauseGameplay로 제어한다. 기본 false라 메뉴/Loot 등은 게임이 계속 진행한다. 스택의 어느 모달이라도 true면 정지하고 마지막 요청을 닫으면 재개. 프리팹별 Inspector 설정 가능, 런타임 덮어쓰기 없음. 월드 입력 차단 및 모델의 결산/환생 단계 동결은 유지. 블러는 여전히 모달 전환 때 캡처하는 정지 이미지이므로 실시간 배경 영상은 별도 작업 필요. Unity 플레이 미실시: 메뉴/Loot에서 날짜/전투 진행, Pause Gameplay 켠 모달에서 정지, 스택 닫기 후 재개 확인 필요.

## 최신 수정 · 블러 캡처 상하 반전

작업 목록: 캡처/블러 경로 확인 → 첫 패스 UV 보정 → 컴파일. 모달 뒤 상단 HUD가 하단에 표시되는 캡처 반전 증상에 대해 PrototypeModalBackdrop의 첫 Blit만 graphicsUVStartsAtTop에 따라 _FlipY를 설정하고 두 번째 Blit은 0으로 초기화한다. 셰이더의 모든 샘플은 보정된 UV를 사용한다. HUD RectTransform/RawImage UV는 변경하지 않는다. 실제 Unity GPU 화면 검증은 미실시: Loot 열기 전후 HUD 상단 위치 유지, 메뉴→설정 스택/닫기, 블러 강도 변경 후 방향 유지 확인 필요.

## 최신 수정 · 모달 CanvasGroup MissingComponentException

작업 목록: 오류 지점/동일 패턴 확인 → Unity null 비교로 수정 → C# 컴파일. 최근 모달 스택 코드에서 GetComponent 결과에 C# ??를 사용하여 Unity의 가짜 null을 통과시킨 것이 원인. PrototypeUgui의 모달 입력 제어와 PrototypeModalBackdrop의 레이어/최상위 캡처 총 세 곳을 명시적 == null 검사 후 AddComponent로 교체했다. Barracks에 수동 컴포넌트 추가 불필요. Unity 플레이 검증은 실행하지 않음. 재생 후 오류가 사라지고 게임 메뉴→설정→ESC 복귀와 블러가 동작하는지 확인 필요.

## 최신 수정 · 모달 스택과 블러 누락 복구

작업 목록: 모달 전환/블러 참조 조사 → 스택과 입력 차단 → 캡처 변경 → 컴파일. 씬의 PrototypeModalBackdrop.ModalLayer가 fileID 0이라 SetVisible이 조기 반환하는 원인 확인. 런타임에 ModalLayer 바인딩에서 CanvasGroup을 복구한다. PushModal과 이전 모달/본문 ID 스택 추가, 메뉴→설정 및 결산→제단 누적, 닫기/ESC 한 단계 복귀, 하위 CanvasGroup 입력 차단, 재시작/종료 스택 초기화. 배경 RawImage를 하위 모달 위/최상위 아래로 정렬하고 최상위만 숨겨 캡처하여 아래 모달도 블러 포함. 스택 전환 때 RT/캡처 재생성, 취소 시 alpha 복구. C# 컴파일 오류·경고 0. 실제 GPU/Unity 플레이 검증 미실시: 메뉴→설정→ESC 시 메뉴 복귀, 아래 메뉴 클릭 차단, 블러 반경/어두움 변경, 모두 닫으면 게임 재개 확인 필요.

## 최신 수정 · 메뉴 버튼 테두리와 디자인 규칙

Build.Item과 기존 PrototypeUiInstaller 버튼을 기준으로 메뉴 네 동작 버튼에 금색 Outline(1,-1), 민트 호버/청록 누름/회색 비활성 ColorTint와 0.1초 전환 적용. PrototypeMenuPrefabInstaller가 기존 Menu Variant도 ButtonStyleVersion 1로 최초 한 번 갱신하고, 이후 사용자 편집은 보존한다. 닫기 버튼은 원본 스타일 유지. 규칙은 [UI 디자인 규칙](ui-design-rules.md)에 기록. Unity 적용 후 메뉴 네 버튼의 테두리·호버·누름·비활성 포기하기 확인 필요.

## 최신 수정 · 메뉴 Variant와 설정 버튼

작업 목록: Titled View/기존 메뉴 구조 확인 → Menu Variant 생성·교체 유틸리티 → 설정 진입점 이동 → C# 컴파일.
별도 HUD.Settings 대신 Menu.Settings로 설정 화면을 연다. 기존 설정 설치 코드의 HUD 아이콘 생성 제거. PrototypeMenuPrefabInstaller는 Common/Titled View의 연결된 인스턴스를 Modal/Menu.prefab으로 저장하여 Variant를 최초 생성한다. Body 아래 기존 Content에 계속하기/다시하기/설정/포기하기 순으로 TMP 버튼 추가, 제목 게임 메뉴와 닫기 연결. PrototypeMenuPanel에 참조 저장, 런타임 Bind에도 연결. 기존 메뉴를 Variant 인스턴스로 교체하고 HUD.Settings 오브젝트/바인딩/가드 제거, Undo 지원·씬 자동 저장 없음. 기존 Menu.prefab은 덮어쓰지 않는다. 스크립트 임포트/씬 열기/편집 모드 복귀 시 적용, 수동 Tools > Afterlife > Apply Menu Prefab. 컴파일 오류·경고 0. Unity 실제 Variant 생성과 화면 검증 미확인: 적용 로그 확인 후 씬 저장, 게임 메뉴의 순서·설정 진입·계속하기·다시하기 확인 필요. 타이틀 씬 미등록 시 포기하기 비활성 유지. Windows 빌드/Unity 테스트 미실시.

## 최신 수정 · 새 Tooltip 프리팹 연결

작업 목록: 새 프리팹과 씬 참조 확인 → TMP 참조 교체/레거시 제거 → 위치 계산 수정 → C# 컴파일.
기존 Canvas.Tooltip은 Tooltip(Legacy)를 가리켰고 새 Common/Tooltip 프리팹의 Message Text는 미연결이었다. PrototypeTooltipMigration이 이미 배치된 프리팹을 GUID로 찾아 Tooltip/TooltipTmpText에 연결하고 레거시를 Undo 삭제한다. 배경과 텍스트 RaycastTarget 해제, 초기 비활성화, 씬 자동 저장 없음. 연결 완료 후 재실행은 아무것도 변경하지 않는다. 스크립트 임포트/씬 열기/편집 모드 복귀 시 적용하며 수동 메뉴는 Tools > Afterlife > Connect Tooltip Prefab. 런타임 위치 계산은 프리팹 앵커/피벗을 보존하고 실제 경계를 기준으로 화면 안에 제한한다. dotnet 컴파일 오류·경고 0. 실제 Unity 적용은 미확인: 에디터 임포트 후 레거시 제거 로그와 참조를 확인하고 씬 저장, 플레이에서 Loot/유물/건설 툴팁의 내용과 화면 가장자리 위치 확인 필요. Unity 테스트/Windows 빌드는 실행하지 않았다.

## 최신 추가 · 40 메뉴·설정 모달 분리와 블러

게임 메뉴 세 버튼(계속하기/다시하기/포기하기), 별도 Header 설정 버튼, 전체 화면 Settings와 화면/조작 탭 마이그레이션 구현. 포기하기는 TitleSceneName이 등록된 씬일 때만 활성화. PrototypeModalSurface Window/Screen 분류. Canvas PrototypeModalBackdrop의 어두움/블러 반경/다운샘플 파라미터, 화면+HUD 캡처 후 2패스 셰이더 블러. 모든 모달 중 시뮬레이션 정지, 닫기 시 RT 정리. ModalSettingsVersion 1 이후 설정 덮어쓰기 없음. Undo/자동 씬 저장 없음, Separate Settings Modal 메뉴. C# 오류·경고 0, 실제 씬 적용/셰이더/화면은 확인 필요. 상세 40-menu-settings-modal-blur.md.

## 최신 수정 · Loot.Items 이름 변경 오류

사용자 프리팹의 목록 이름이 Loot.Items로 변경됐지만 설치 코드가 Items만 검색하여 예외 발생. 이미 유효한 TMP 바인딩이 있으면 이름 검색 없이 반환하도록 수정. 최초/미완료 연결은 저장된 Loot.Row 참조의 부모 → Loot.Items/Items 이름 → 공통 전리품 행 부모 순으로 탐색한다. 프리팹 이름/레이아웃은 변경하지 않는다. C# 오류·경고 0, 실제 Unity 재적용 확인 필요.

## 최신 추가 · 39 Loot 프리팹 TMP/팝업 교체

Modal/Loot.prefab(공통 Titled View 기반)의 Items 하위 legacy Text를 TMP로 변환하고 PrototypeLootPanel에 참조 저장. PrototypeLootPrefabInstaller가 기존 Modal.Loot를 같은 부모의 프리팹 인스턴스로 교체하고 모든 Loot 바인딩/가드를 재연결(Undo, 씬 자동 저장 없음). Header Loot 버튼은 새 팝업을 연다. 이미 교체된 인스턴스는 재생성하지 않는다. 런타임 Bind에서도 내부 참조 재연결. Tools > Afterlife > Apply Loot Prefab. C# 오류·경고 0, 실제 에디터 적용 확인 필요. 상세 39-loot-prefab-tmp.md.

## 최신 정정 · 건설 항목은 런타임 초기화에서 생성

사용자가 에디터 구성이 아니라 런타임 동적 생성을 요구한다고 정정했다. PrototypeBuildMenuItems.InitializeBuildItems를 InitializeSceneUi의 Bind 이전에 실행하여 Content에 1회 Instantiate/바인딩, 메뉴 재개방·재도전에서는 재사용. 기존 저장 항목은 런타임에서만 교체. 에디터 씬 구성/플레이 직전/빌드 씬 처리/BuildCatalogStamp 전부 제거. 에디터는 SO 에셋과 원본 프리팹 TMP 변환만 준비한다(Prepare Build Catalog Assets). BuildPanel 자식 배치 유지, 모델 가격·해금 연동 유지. 컴파일 오류·경고 0. 38의 이전 편집 단계 구성 안내는 폐기.

## 최신 추가 · 38 건설 데이터·프리팹 목록

BuildCatalog 및 개별 PrototypeBuildingData 8개 최초 생성, Build.Item의 Name Text/Cost Text TMP 변환과 Label 제거, 편집 단계 Content 구성 코드를 추가했다. 데이터/프리팹 변경 시에만 재구성하며 플레이 중 생성하지 않는다. Configure Build Catalog Items 메뉴/플레이 직전/빌드 씬 처리 경로. 순수 ConstructionRule로 실제 가격/해금/차감/환불 기록 연동. 메뉴 열기/갱신 시 자식 UI 배치 수정 제거, BuildPanel 루트만 이동/크기 제한. C# 오류·경고 0, 실제 Unity 에셋/프리팹/씬 적용은 확인 필요. 상세 38-build-catalog-and-prefab.md.

## 최신 수정 · HUD.Performance TMP 변환

PrototypeUiInstaller의 기존 Resources TMP 변환을 공통 하위 트리 변환으로 추출하고, HUD.Performance만 변환하는 진입점 추가. PrototypePerformanceTmpMigration은 스크립트 임포트/편집 모드 복귀 시 기존 Text를 같은 오브젝트의 TextMeshProUGUI로 교체하고 바인딩 TmpText를 갱신한다. Undo, RectTransform/폰트 크기/색/정렬 보존, 기존 TMP는 재변환하지 않음. 수동 Tools > Afterlife > Convert Performance to TMP. 자동 씬 저장 없음. 신규 Install에도 적용. F3 표시 로직 그대로, C# 오류·경고 0, 실제 Unity 변환/플레이 확인 필요.

## 최신 수정 · Tooltip TMP 연결

Tooltip 아래 기존 TMP(fileID 1477058338)를 확인. PrototypeCanvas.TooltipTmpText 명시적 참조 우선, 미지정 시 Tooltip 하위 TMP를 컴포넌트 타입으로 찾아 런타임 연결한다. 구형 Text는 숨기고 SetTooltipText를 통해 전체 툴팁을 TMP에 출력한다. 폰트/배치 유지, TMP RaycastTarget 해제. 구형 씬만 Text 폴백 지원. C# 오류·경고 0, 실제 플레이 미실시.

## 최신 추가 · 37 하루 시작 배너 애니메이션

PrototypeDayStartBanner 컴포넌트: 중앙 0×3 → 가로 0.2초 → 높이 88까지 0.2초 → 텍스트 페이드 0.2초 → 1초 유지 → 역재생. 크기/시간/커브 Inspector 조절, TMP/Text 지원. 기존 배너 활성 타이머를 Play로 교체. 컴포넌트 없는 배너에만 에디터 자동 추가(Undo, 씬 자동 저장 없음), 런타임 누락 보완 포함. 기존 조절 값은 덮어쓰지 않는다. 이전 DayBannerSeconds는 숨김. 컴파일 오류·경고 0, 실제 에디터 추가/화면은 확인 필요. 상세 37-day-start-banner-animation.md.

## 최신 수정 · Notice Message Text 연결

HUD.Notice가 비활성 구형 Message(Text)를 참조하고 새 Message Text(TMP)는 미연결인 것을 확인했다. PrototypeCanvas.Bind에서 명시적 TmpText를 우선하고, 없으면 Notice/Message Text를 찾아 알림 바인딩에 연결한다. 구형 Text는 숨기며 기존 4.5초 표시 로직, 사용자 폰트/배치를 유지한다. 열린 씬 파일 변경 없이 런타임 연결. C# 오류·경고 0, 실제 플레이 미실시.

## 최신 수정 · 빈 유물 목록 연결 오류

기존 유물 아이콘 삭제 후 Relic.* 바인딩의 Rect가 모두 null이 되어 부모 컨테이너 추론이 실패했다. 데이터셋과 프리팹 참조는 정상임을 파일에서 확인. PrototypeRelicHud는 명시적 RelicRoot 우선, Header 아래 Relics/Relic 컨테이너 검색, 이전 아이콘 부모 순으로 연결한다. 빈 목록도 정상 지원하고 여러 후보가 있으면 명시적 연결을 요구한다. 오류 메시지를 데이터셋/프리팹/루트별로 분리했다. 씬/프리팹은 수정하지 않았으며 실제 플레이 검증은 미실시.

## 최신 추가 · 36 유물 데이터셋과 Loot 툴팁

Header Menu Loot 툴팁 추가. PrototypeRelicCatalog(ScriptableObject)의 ID/이름/아이콘/툴팁과 기존 Relic List Item.prefab을 사용해 획득한 유물만 HUD 생성. 모델 RelicsChanged 이벤트 및 기존 HUD 갱신에서 동기화, 중복 ID는 단계만 갱신, 환생/리셋 시 제거. 기존 씬 아이콘은 런타임 숨김. 하루 보상 선택지도 같은 데이터 사용. Resources/Data/RelicCatalog.asset은 Unity 임포트 후 최초 생성하며 기존 에셋은 덮어쓰지 않는다. 컴파일 오류·경고 0. 실제 에셋 생성/플레이 미확인, Windows 빌드 미실시. 상세 36-relic-catalog-and-loot-tooltip.md.

## 최신 추가 · 35 인게임 건설 메뉴만 사용

HUD.Build 생성/입력 제거. BuildPanel은 유지하고 _tileBuildMenu일 때만 표시. 기존 버튼은 런타임 숨김, 편집기 PrototypeWorldBuildOnlyMigration이 리로드/플레이 종료 후 바인딩/가드/오브젝트 제거(Undo, 자동 저장 없음). 수동 Remove HUD Build Button 메뉴 제공. 컴파일 오류·경고 0, 씬 적용/실제 플레이 미확인. 상세 35-world-build-menu-only.md.

## 최신 추가 · 34 건설 메뉴 배경 밝기

PrototypeTileBuildMenu에서 WorldDisplay 원래 색을 저장하고 전체 연출 진행도(0.25초)에 따라 기본 45% 어둡게 한다. 메뉴/HUD 밝기 유지, 닫기 역재생에 동기화. 즉시 정리 시 원래 색/알파 복원. Build Background Dim 직렬화 필드로 조절. 새 UI/씬 설치 없음. 컴파일 오류·경고 0, 실제 플레이 미실시. 상세 34-build-background-dim.md.

## 최신 Tools 정리

사용자 요청으로 Convert HUD Resources to TMP 메뉴와 ConvertOpenResourcesToTmp 진입 함수 제거. 기존 TMP 에셋 및 Install Scene UI 내부 TMP 처리는 유지. C# 오류·경고 0.

## 최신 추가 · 33 화면 출력 카메라

PrototypeDisplayCameraInstaller 추가. 열린 프로토타입 씬에 리로드/플레이 종료 후 Display Camera 설치(기존 활성 Display 1 카메라 있으면 건너뜀). cullingMask=0, 검정 배경, HDR/MSAA/후처리/그림자/컬러·깊이 텍스처 요청 해제. 기존 RT 월드 카메라 유지. Undo 지원/자동 저장 없음. Tools > Afterlife > Add Display Camera 수동 메뉴. 컴파일 0오류/0경고. 설치 로그/씬 저장/실제 성능은 미확인. 상세 33-display-camera.md.

## 최신 추가 · 32 선택 타일 표시 유지

후속 요청으로 BuildMarkerSeconds=0.13, BuildPanelSeconds=0.12로 변경. 총 등장/닫기 0.25초(이전 0.42초). 컴파일 오류·경고 0, 플레이 체감 미검증.

건설 메뉴가 열려 있으면 PrototypeInteraction.Sync가 _buildTile에 기존 _hoverRing 사각 테두리를 표시. 마우스/UI 위치와 무관하게 유지하며 CloseTileBuildMenu 즉시 정리 단계에서 해제. 등장/닫기 역재생 중 유지. 컴파일 오류·경고 0, 플레이/Windows 빌드 미실시. 상세 32-selected-tile-highlight.md.

## 최신 추가 · 31 건설 가능한 타일 메뉴

VillageModel.BuildTileError로 영역/물/겹침 조건을 추출해 PlacementError와 메뉴 열기에서 공유. OpenTileBuildMenu는 조건 통과 후에만 UI/사각뿔 준비. 골드/해금은 기존 건설 검사 유지. 불가 타일의 호버와 선택 해제 유지. C# 오류·경고 0, 실제 플레이 및 Windows 빌드 미실시. 상세 31-buildable-tile-menu.md.

## 최신 추가 · 30 빈 타일 호버 표시

PrototypeInteraction에서 월드 포인터 입력 허용 시 _tileHoverValid 갱신. 오브젝트 미호버 시 기존 _hoverRing으로 반올림한 타일 중앙 0.98 사각형 표시. UI/모달/건설 메뉴/카메라 조작/드래그 시 숨김. 배치 모드의 기존 미리보기 유지. C# 오류·경고 0, 플레이/Windows 빌드 미실시. 상세 30-tile-hover.md.

## 최신 추가 · 29 타일 선택·건설 메뉴 연출

PrototypeTileBuildMenu에 타일 중앙 회전 사각뿔 메시/알파, 0.22초 표시자 후 0.20초 메뉴 상승·페이드 인 추가. 사용자 “메뉴 페이드 아웃”은 열기 흐름상 페이드 인으로 해석했다고 알림. 기본 CloseTileBuildMenu는 현재 시간부터 역재생하며 완료까지 _tileBuildMenu 유지/입력 차단. 모달/종료/리셋/사이드 메뉴 전환은 true 인자로 즉시 정리. CanvasGroup 상태와 기존 RectTransform 레이아웃 복원, 메시 종료 해제. 컴파일 오류·경고 0, 실제 플레이/Windows 빌드 미실시. 수동 확인은 29-build-menu-animation.md.

## 최신 추가 · 28 건설 메뉴 크기·닫기

27의 가로 목록을 300×300 세로 팝업/세로 스크롤로 변경. 프레임·배경 크기를 함께 변경해 기존 세로 프레임 잔류 문제 수정. 팝업이 열려 있으면 월드 입력과 커서 기여를 막고 배경 클릭은 닫기로만 소비한다. 컴파일 오류·경고 0, 실제 플레이/Windows 빌드 미실시. 상세 28-build-menu-dismiss.md.

## 최신 추가 · 27 빈 타일 건설 메뉴

빈 타일 클릭은 선택 해제와 클릭 위치 위 가로 건설 목록 표시. 건물 선택 시 저장한 최초 타일에 Place 호출. PrototypeTileBuildMenu.cs에서 기존 씬 BuildPanel RectTransform을 일시 변경하고 닫을 때 복원한다. 신규 UI 생성 없음. 사이드 건설 메뉴는 유지. ESC/카메라 조작/다른 대상/모달 시 닫기. 컴파일 오류·경고 0, 실제 플레이와 Windows 빌드 미실시. 상세 및 수동 확인은 27-tile-build-menu.md.

## 최신 추가 · 26 마우스 조작 변경

이번 변경은 25 이후 사용자 골드/TMP/UI 편집을 보존한다. 아래 카메라 상태 텍스트 기록은 과거 기록이며 이제 고정 기능 자체를 제거했다.

- 빈 지면 왼쪽 클릭은 선택 해제. 건설 배치 모드에서는 기존 배치 유지, ESC 취소.
- 오른쪽 누름 중에만 가장자리 이동하고 해제 즉시 정지. 오른쪽 선택 취소 제거.
- 가운데 수평 드래그는 시작 순간 화면 중앙이 바라보는 지면을 축으로 회전. 기존 고정 토글 제거. 현재 카메라 방향에 맞춰 이동하며 재시작/환생 시 방향 복원.
- PrototypeInteraction/PrototypeUgui/PrototypeCanvas/PrototypeUiInstaller와 기존 QA 호출 호환 수정. 신규 PrototypeCameraControlsMigration은 열린 프로토타입 씬에서 고정 UI/바인딩/가드와 도움말만 일회 보정한다. PointerControlsVersion으로 중복 방지, Undo 지원, 자동 저장 없음. 플레이 중이면 종료 후 컴파일이 끝나야 적용. 수동 메뉴 Tools > Afterlife > Apply Mouse Camera Controls.
- 최종 C# 컴파일 오류·경고 0. 실제 플레이 및 Windows 빌드 미실시. 보정 로그 미확인: 씬 적용/저장 완료로 간주하지 말 것. 사용자가 편집 중인 씬을 덮어쓰거나 Unity를 종료하지 않는다.
- 상세 및 수동 확인 목록: 26-mouse-camera-controls.md. 현재 단계 M5, M5 전체 완료 아님.

## 최신 수정 · 카메라 상태 텍스트 바인딩

카메라 잠금/이동 상태 갱신 대상을 `HUD.Camera.State`에서 사용자가 편집하는 `Camera State Text` TMP로 변경했다. 현재 프로토타입 씬의 바인딩은 해당 TMP 오브젝트(fileID 2050930112/2050930113)를 직접 가리키며, 기존 비활성 HUD.Camera.State 오브젝트는 레거시로 남겨 런타임에서 갱신하지 않는다. 새 UI 설치 경로도 같은 ID를 사용한다. C# 검증 오류·경고 0, Windows 빌드는 하지 않았다.

## 최신 수정 · DayNight 연결 도구 제거

사용자 커스텀 Sprite/임포트 설정을 덮어쓰던 Connect DayNight Icons and Text 메뉴, 리로드/편집 모드 복귀 자동 훅, Install 호출 및 이미지 강제 임포트/연결 함수를 모두 제거했다. 기존 씬 참조와 이미지 에셋은 유지한다. 런타임 낮밤 전환은 PrototypeCanvas의 DaySunSprite/NightMoonSprite를 계속 사용하므로 커스텀 이미지는 이 필드에서 지정한다. 아래 25의 자동 연결 안내는 과거 기록이며 더 이상 적용되지 않는다. Windows 빌드는 하지 않는다.

## 최신 추가 · 25 DayNight 이미지/텍스트

사용자가 직접 편집한 DayTimeline/Group/DayNight/Icon Image와 Text(TMP)에 연결 요청. imagegen으로 해/달 투명 PNG를 생성하여 Resources/UI/DaySun-v1.png, NightMoon-v1.png에 복사했다.

- PrototypeCanvas.DayNightIcon/DaySunSprite/NightMoonSprite 추가. UpdateUiTimeline이 DayNight.Text를 갱신하고 같은 Image에서 해/달 교체. 기존 HUD.Day/Sun/Moon 비활성 유지.
- PrototypeUiInstaller의 Connect DayNight Icons and Text 메뉴/자동 연결 훅: 에디터 스크립트 reload 및 편집 모드 복귀 시 누락 참조만 연결. 사용자가 만든 UI 위치/폰트 유지, 입력 통과, Undo 지원, 씬 자동 저장 없음. 이미지의 Sprite 임포트 설정은 Unity API 사용.
- C# 오류·경고 0. PNG 알파 확인 완료. 최종 확인에서 PNG .meta 두 개와 Editor.log의 DayNight 적용 완료 로그로 실제 임포트/참조 연결을 확인했다. 사용자가 에디터로 복귀하여 확인하고 씬을 저장할 차례다. 누락 시 위 메뉴 실행.
- Windows 빌드/플레이/에디터 테스트 미실시. 이전 사용자 선호대로 빌드하지 않음.
- 24에서 생성 대기였던 Mona12TextKR SDF.asset은 지금 존재하고 DayNight/Text에 TMP가 사용 중이다. 사용자 변경을 유지한다.
- 상세 25-daynight-generated-icons.md. 새 UI도 연결 후에는 이름이 아닌 직렬화 참조/Binding으로 동작한다.

## 최신 추가 · 24 Resources TMP 전환 준비

사용자 요청: HUD Resources부터 Text → TMP, TMP Font Asset 생성. 이어서 “아직 빌드하진 않을거야 수정된거 보고”라고 답했다. **Windows 빌드나 열린 Unity 종료를 자동 진행하지 않는다.**

- PrototypeUiBinding.TmpText와 Label 혼용 지원, Canvas.ResourcesTmpFont, TMP 어셈블리 참조 추가.
- PrototypeUiInstaller에 Tools > Afterlife > Convert HUD Resources to TMP 메뉴 구현. 현재 열린 프로토타입 씬의 Resources만 변환, Undo 지원, 씬 자동 저장 없음. 일반 Install에도 연결됨.
- Mona12TextKR SDF.asset 생성 코드는 Dynamic SDFAA/64/8/1024/MultiAtlas, 숫자/영문/필요 한글 미리 추가, Material/Atlas를 하위 에셋으로 저장. 원본 ttf 사용.
- 기존 컴포넌트의 RectTransform과 스타일 보존, 이미 TMP인 것은 재설정하지 않음.
- C# 컴파일 오류·경고 0. 마지막 확인 시 실제 SDF.asset은 아직 없고 씬 변환도 미실행. 에디터 직접 조작 도구가 연결되지 않아 사용자가 메뉴 실행/확인할 차례다. 완료를 과장하지 않는다.
- 상세: 24-hud-resources-tmp.md. 최신 Windows 파일은 23 빌드 그대로.

## 최신 추가 · 23 타격·지형·적 이동·메뉴

최신 사용자 요청 범위 구현·씬 저장·Windows 빌드 완료. `23-combat-navigation-and-editing.md`에 변경과 UI 편집 안내를 저장했다.

- 도끼는 병합 FBX 메시 경계를 도구 피벗 좌표로 변환하여 실제 날이 돌출된 쪽을 찾고 회전 부호/접촉점을 보정. 원본 Blender export가 메시를 합치므로 Honed edge 이름 탐색은 쓰지 않는다.
- 적 근접/원거리 명중 → EnemyHit → 실제 피해 숫자(빨간색). FeedbackKind.EnemyDamage, PrototypeCanvas.EnemyDamageColor 추가.
- Pathfinder.Find에 유닛 점유 혼잡 비용과 직선 혼잡 검사를 추가. 실충돌 유지, 0.4초 진행 부족 시 재탐색, ±149도 측후방 회피. 실제 군집 성능/옆길 해결 여부는 플레이 확인 필요.
- PixelSurface.shader의 반복 사인파/격자 점을 저대비 다중 스케일 노이즈로 변경. 풀/자갈 장식 군집 분포 추가. 물/지형 판정 및 마을 경계는 유지.
- Menu.Restart 버튼은 게임 종료 위. 기존 Reset/RebuildVillageView 경로로 새 판. 씬 저장 완료. UI 편집은 Afterlife_PixelPrototype.unity/Canvas 하위, Bindings ID/참조 유지.
- 처음 Unity가 열려 있어 대기했다. 사용자가 저장 후 닫겠다고 답했고 종료 확인 후 CLI 빌드. 에디터를 임의 종료하거나 중복 실행하지 않았다.
- 최신 빌드 20260917-191104, C#/Windows 오류·경고 0, 셰이더 오류 없음. 설정 5개 복원 해시 일치. DLL 007619FB9FAFF9DC130EA8CC3E1453DF048E9F69852A6C59866405D2E26B3FCE.
- 실제 실행/에디터 테스트는 미실시. 요청 구현·빌드 완료와 M5 전체 경험 검증 완료를 구분한다. 실행 중인 Unity/게임 없음.

## 최신 추가 · 22 초반 성장·일차 배너

최신 사용자 피드백: 초반 커서/기여가 너무 커 골드와 성장이 빠름. 하루를 줄이고 N일차 배너 요청. 22 문서를 최신으로 읽는다.

- 커서 기본 반경 1.4 → 0.65, 고정 기여 3배 → ContributionPower 1. 채집/건설/수리/상자/공격에 적용. 집 20골드, 기본 주기 2초 유지. 기본 단일 자원 골드 6 → 2/주기. 시야 반경은 유지.
- 낮 35 / 황혼 5 / 밤 35 = 기본 75초. 적이 남으면 밤은 연장. 기존 출현 간격 유지.
- DaySequence와 PrototypeUgui.UpdateDayBanner로 새 판/다음 날/환생 후 실제 낮 시작을 감지. 중앙 전체 폭 검정 65% 배너에 N일차 1초, 입력 통과. 씬 저장, Inspector 편집 가능.
- PrototypeTools.CreateAssets에서 매 빌드 기존 커서 반경 1.4를 덮어쓰던 코드 제거. 기존 밸런스 설정 보존.
- C#/Windows 빌드 오류·경고 0, 씬/설정 정적 확인 7항목, 설정 5개 복원 해시 일치. 최신 빌드 20260917-154404, DLL 66B541855582AE9165DE241153EBA0062C85A8F5B2D7289967B7B105B8685109.
- 실제 플레이/배너 표시 관찰은 미실시. 준비시간·공격 기여도 줄어 첫날 난이도는 확인 필요. 22 문서에 직접 확인 절차. 실행 중인 게임/Unity 없음.

## 최신 추가 · 21 골드 경제·채집 연출

20 아래의 골드 통일 미적용 표기는 과거 기록이다. 사용자가 후속으로 채택하여 21 구현·씬 저장·Windows 빌드까지 완료했다. 자세한 최신 규칙/수치/확인 절차는 `21-gold-economy-and-collection.md`.

- 목재/석재 재고 제거, 채집 즉시 골드 지급(나무 1/바위 2, 소수 이월). 건설/수리/취소 환급/상자/환생 재화 집계 모두 골드. 상인의 저울은 채집 골드 +25%/단계. 특수 재화 에테론은 유지.
- 교역소 건설/관리 UI·환전 함수 제거. Kind.Market 번호는 직렬화 안정성을 위해 예약 상태로 남겼고 모델에서 건설 거부.
- 모델 HarvestGold 이벤트 → PrototypeUgui의 OnHarvestGold/UpdateGoldFeedback. 목재/석재 Sprite가 튀어나와 HUD 골드에 곡선 이동. 실제 골드는 즉시 지급, HUD 금액은 아이콘 도착 후 누적. 지출은 즉시 표시. 풀 초과/화면 밖 금액은 연출 없이 표시 목표에 합류.
- Canvas Collection Feedback에 96개 Image를 미리 저장. PrototypeCanvas Inspector의 채집 골드 연출에서 속도/퍼짐/곡률/카운트 속도 편집 가능. 런타임 UI 생성 없음.
- Install Scene UI의 InstallGoldEconomy는 최초 한 번만 기존 HUD/교역소를 마이그레이션한다. 이후 기존 연출 설정 보존.
- 빌드 `20260917-144557`, C# 및 Windows 오류·경고 0. 씬 정적 확인 7항목 통과, 설정 5개 복원 해시 일치. DLL `3AB6AC39F66B5CF62FF8251F478052C6D160B1B745C065CD2E2646564FD28C65`.
- 에디터/플레이어 실행 테스트는 하지 않았다. 실제 아이콘 궤적/조작성/경제 밸런스는 직접 확인 필요. 실행 중인 게임/Unity 없음. M5 전체와 M6는 아직 미완료.

## 최신 추가 · 20 하루 보상·채집 수정

현재 요청 기능 구현·씬 저장·Windows 빌드 완료. 아래 19 내용은 이전 기록이며 충돌 시 이 절과 `20-daily-rewards-and-resources.md`를 우선한다.

- 유적/버려진 제단의 편입 효과 및 적용 여부를 툴팁에 복구. HUD 유물 효과가 월드 설명에 덮이는 문제 수정.
- 하루 결산 → 생존 보상 3택1 → 다음 날/환생. 미보유 우선 추첨, 부족한 후보는 기존 유물 강화. 마지막 날도 선택 후 결과. 선택 전 다음 날/환생은 모델에서 차단. ResetExploration에서 단계·보상 상태 초기화.
- Health=자원 잔량, MaxHealth=총량. Stock은 같은 값의 호환 접근자. 일차 전환 때 기존 자원 체력만 증가하던 ScaleResourcesForWave 삭제. 채집 기여를 잔량에서 직접 차감. 새 생성 자원만 일차에 비례한 총량. 월드 숫자는 실제 채집 기여, 인벤토리 소수 이월 유지.
- PrototypeUiInstaller.Install은 기존 씬에 누락된 Settlement.Rewards/Choice.0~2/Help만 추가. 새 UI도 씬 저장 완료, 런타임 생성 없음.
- 골드 통일은 추천안만 문서화, 아직 적용하지 않음. 사용자는 환전 번거로움과 채집 즉시 돈/모든 비용 골드 방향에 관심을 보임.
- 최신 빌드 `20260917-143120`, C# 및 Windows 오류·경고 0. DLL SHA-256 `C5A8165CFAD26502B559F53C53BE88CC937118FC0FF6135C1BE45740283F10CF`. 설정 5개 복원 해시 일치.
- Unity 라이선스 제한으로 첫 설치 실패 후 로컬 라이선스 접근 실행으로 성공. 에디터/플레이어 테스트는 실행하지 않았음. 기존 19의 자동 QA는 새 보상 규칙 증거가 아님. 20 문서에 직접 확인 절차 있음.
- 다음: 최신 플레이어에서 보상 흐름·유적 설명·자원 표시/기여량 확인, 골드 통일 채택 여부 결정. M5 전체/출시 준비는 미완료.

## 먼저 읽을 문서

1. 이 문서와 `status.md` — 현재 상태와 사용자 선호.
2. `D:/area/dev/alone/Afterlife/impls/unity/Afterlife/AGENTS.md` — Unity 작업 규칙.
3. `19-scene-ugui.md`, `19-scene-ugui-qa.md` — 마지막 구현과 확인 범위.
4. `18-combat-and-ui.md` — 최신 전투/보상/표시 규칙.
5. 필요한 작업에 해당하는 10~17 문서. 초기 01~09 내용과 충돌하면 최신 사용자 결정과 후속 문서를 우선한다.

## 현재 상태

**M5 — 플레이 검증·수정. 19 씬 uGUI 전환 요청 범위 완료.** MVP 핵심 흐름을 구현한 확장 프로토타입이며 M5 전체/출시 후보 검증 완료는 아니다. M6 업데이트 준비, M7 배포는 미착수. 현재 진행 중인 빌드·QA 플레이어는 종료했다. 이번 사용자의 요청은 세션 인계 문서 저장이며, 새 기능 추가 요청은 아니다.

사용자는 기존 Steam 게임 Afterlife를 다시 기획하여 업데이트하려 한다. 처음에는 기획만 요청했지만 이후 프로토타입 구현·피드백 반영·CLI 빌드·화면 확인을 명시적으로 허용했다. 공개 배포/스토어 게시 권한은 없음. 새 세션은 사용자 다음 요청을 받아 이어가며 전체 기획을 다시 시작하지 않는다.

## 사용자와 소통하는 방식

- 한국어, 짧고 쉬운 설명. 연관된 결정은 묶어서 제안한다.
- 브리핑에 **M1/M2 등의 마일스톤과 현재 문서 주제**를 명시한다. 완료한 범위와 미완료인 전체 단계를 구분한다.
- 커리큘럼이 아니라 실전 제작 협업이다. game-design-to-release 스킬을 사용한다.
- 기획/결정/구현 결과를 `docs/game-process`에 저장하고 status 및 대시보드를 갱신한다.
- 이미 승인받은 작업을 다시 확인하지 않는다. 질문은 필요한 경우 초기에 모아서 한다.
- **빌드 완료 시 빌드 폴더의 클릭 가능한 링크를 반드시 제공한다.**
- 사용자는 직접 Inspector에서 UI 디테일을 수정하고 싶어 한다. 런타임 UI 생성이나 빌드 때 씬 UI를 덮어쓰는 방식으로 되돌리지 않는다.

## 경로와 도구

- 작업 루트: `D:/area/dev/alone/Afterlife`
- 실제 Unity/Git 프로젝트: `D:/area/dev/alone/Afterlife/impls/unity/Afterlife` (상위 작업 루트는 Git 아님).
- Unity: 6000.0.69f1, URP 17, Windows.
- 씬: `Assets/Prototype/Scenes/Afterlife_PixelPrototype.unity`.
- 코드: `Assets/Prototype/Runtime`, 편집기 도구: `Assets/Prototype/Editor`.
- 빌드 폴더: `D:/area/dev/alone/Afterlife/impls/unity/Afterlife/Builds/PixelPrototypeWindows`
- 실행 파일: 위 폴더의 `AfterlifePrototype.exe`.
- Blender: `C:/Program Files/Blender Foundation/Blender 5.0/blender.exe`.
- 모델 소스와 생성기: `res/art-source/world/AfterlifeWorld.blend`, `tools/art/build_world.py` 등. 17 문서 참고.
- 진행 대시보드: `docs/game-process/index.html`.
- 대시보드 재생성: `python -X utf8 C:/Users/User/.codex/skills/game-design-to-release/scripts/render_dashboard.py --docs-dir docs/game-process`

## 현재 게임 방향과 최신 결정

- 중세 판타지 생존 디펜스. 집 하나/주민 1명/목재 0/석재 0에서 시작한다.
- 주민은 개별 조작하는 유닛이 아닌 인구 수다. 집으로만 늘며 마을 영역 넓이에 따른 주민 보너스는 폐기했다. 기본 주민 상한 1, 농장당 +3. 집당 인원은 연구로 강화한다.
- 커서를 따라다니는 원형 범위 안의 대상들에 기본 2초마다 동시 기여한다. 주민은 범위/효율에 영향을 주고 주기 단축은 연구로 한다. 클릭 위치에 범위를 고정하는 옛 설계는 폐기했다.
- 커서는 채집·건설·수리·적 공격·상자/아이템에 기여한다. 공전 에너지 볼은 악용 가능성 때문에 삭제했다.
- 건설비는 배치 시 선불, 자동 건설하며 커서로 도울 수 있다. 건물은 바로 드래그해 이전. 모달이 있는 건물은 한 번 클릭하면 열린다.
- 병력은 별도 생산하며 순찰/방어한다. 클릭·드래그 선택, 선택한 병력을 끌어 공격 이동 명령.
- 낮/황혼 준비 후 밤에 해당 일의 적 웨이브. 새벽에 전리품을 돈으로 정산한다. 최종 웨이브 방어가 목표다.
- 마지막 집이 파괴되면 패배한다. 일반 강화는 판 내부용, 돈이 있으면 연구소에서 수시 구매한다.
- 환생은 제단이 있고 밤 방어 후 정비 때 가능. 웨이브/마을/일반 강화를 초기화하고 환생 시 에테론을 정산한다. 환생 강화와 미사용 에테론만 같은 판 안에서 유지한다. 최종 승리/패배 후 판 내부 데이터는 초기화하며 영구 해금만 남는다.
- 에테론은 웨이브 클리어 때 직접 지급하지 않는다. 환생 시 진행도 중심 + 처치/남은 재화 보너스로 정산한다.
- 교역소는 하나. 클릭 모달에서 목재/석재 비율을 정하고 버튼으로 한 번 환전한다. 자동 환전은 폐기했다.
- 유물은 미보유 목록에서 추첨, 자원 HUD 아래 표시. 상자 등급별 내구도/보상 차이. 나무/바위는 주기 생성하되 물 타일 제외.
- 적 시야 안에서 아군 병력·타워·성벽을 같은 높은 우선순위로, 가까운 대상을 선택. 나머지 건물은 차순위. 적 시야는 플레이어 시야를 밝히지 않는다. 초기 반경은 근접 8/원거리 10타일.
- 적 처치 시 종별 정수 돈 즉시 지급과 아이템을 모두 획득. 아이템은 새벽 정산용. 현재 즉시 돈은 슬라임 2, 고블린 3, 고블린 전사 5, 주술사/리자드 4, 오크 8. 밸런스 가설이다.
- 시야 경계는 투명도이며 디더링은 사용자가 피로를 느껴 제거했다. 병력 이동 시 시야 갱신, 밤 집 라이트는 좁게. 마을 영역은 타일 테두리/약한 청록 채색으로 표시한다.
- 카메라는 화면 가장자리의 직사각형 구간에서 가속 이동. 가운데 버튼은 고정 토글. UI 주변 여유 영역에서는 자동 정지한다.

## 가장 최근 변경: 씬 uGUI 전환

이전 씬은 비어 있었고 PrototypeScene이 런타임 루트를 생성, OnGUI가 UI를 직접 그렸다. 지금은 씬에 아래 구조가 저장돼 있으며 **OnGUI/GUI 그리기 경로를 제거했다.** 월드의 카메라/건물/적 등은 기존대로 런타임 구성하지만 UI 오브젝트는 미리 저장한다.

```text
Afterlife_PixelPrototype
├─ GameRoot (PrototypeScene / PrototypeView)
├─ Canvas (CanvasScaler / GraphicRaycaster / PrototypeCanvas)
│  ├─ WorldDisplay (RawImage: 픽셀 렌더 텍스처)
│  ├─ WorldFeedback (기여 텍스트 96개 사전 슬롯)
│  ├─ HUD (자원/유물/일차/메뉴/건설/전리품 등)
│  ├─ ModalLayer (DimBackground + 각 모달/결과창)
│  ├─ Tooltip
│  └─ Selection
└─ EventSystem (InputSystemUIInputModule)
```

핵심 파일:

- `PrototypeCanvas.cs`: 씬에 직렬화된 Bindings, Font/Canvas 참조, UI Raycast 및 CameraGuards. ID와 참조를 유지하면 오브젝트 이름은 바꿔도 된다.
- `PrototypeUgui.cs`: 기존 VillageModel 명령과 버튼/슬라이더 연결, LateUpdate에서 데이터와 활성 상태 갱신. 고정 UI 위치는 다시 생성하지 않는다.
- `PrototypeUiSymbol.cs`: 삼각형/태양/달용 uGUI Graphic.
- `Editor/PrototypeUiInstaller.cs`: `Tools/Afterlife/Install Scene UI`. Canvas가 이미 있으면 기존 배치/스타일 보존. 최초 생성 컨트롤의 앵커 보정은 LayoutVersion 1에서 한 번만 한다.
- `PrototypeScene.cs`: 저장된 PrototypeView를 사용한다. 빈 씬을 실행해 런타임 UI 루트를 새로 생성하는 fallback을 제거했다.
- `PrototypeInteraction.cs`: 실제 uGUI Raycast로 월드 입력 차단, 실제 RectTransform으로 카메라 UI 보호 범위 계산.
- `PrototypeHud.cs`, `PrototypeUiTheme.cs`: 예전 그리기 구현은 제거됐다. 일부 이전 QA용 상태/좌표와 설정 저장만 남아 있다.
- Runtime 및 Editor asmdef에 UnityEngine.UI / Unity.InputSystem 참조가 있다.

UI 편집:
- 비활성 ModalLayer와 수정할 창을 활성화해 편집한 뒤 다시 비활성으로 저장한다.
- 모달 본문은 `Body/Viewport/Content`; ScrollRect와 Content 크기로 스크롤 범위 조정.
- Font는 **Mona12TextKR.ttf의 Unity Font 에셋**. TMP_FontAsset이 아니다. 각 Text.font에 직접 참조한다.
- 폰트 경로: `Assets/Prototype/Resources/Fonts/Mona12TextKR.ttf`.
- 프레임/아이콘 Sprite 에셋: `Resources/UI/SceneSprites`. 기존 텍스처 참조/9-slice이며 PNG를 새로 편집하지 않았다.
- 편집 기준 1200×675, 1920×1080에서 1.6배. CanvasScaler 기준은 시작 시 값을 읽어 사용자 UI 배율과 함께 적용한다.
- 기여 텍스트 일반/치명타 크기와 일반/치명타/불/화상 색상은 PrototypeCanvas Inspector에서 수정 가능.
- 창 제목은 환생/전리품/메뉴/환생 강화/연구소/정산. AFTERLIFE / VILLAGE 등의 장식 문구는 제거했다.
- 일차 옆에는 태양/달, 진행 바 위에는 간격을 둔 아래 삼각형. 미정산 전리품은 항상 HUD에 나열하지 않고 왼쪽 버튼으로 모달을 연다.

## 마지막 빌드·검증 증거

- 최종 빌드 ID: **20260917-135310**.
- `Afterlife.Prototype.dll` SHA-256: `40c6e9551f022d78560616763d1051cd2df611993c33f4dd94f7ef9364faf179`.
- C# 컴파일 및 Windows 빌드: 오류 0, 경고 0.
- `Logs/UguiQA/pass03`: 최종 실행 스크린샷 24장, 확인 23/23.
- 보관한 화면: `docs/game-process/qa/ugui/index.html`, 결과 `19-scene-ugui-qa.md`.
- 확인: Canvas/EventSystem, 전체 Text 폰트 참조, Binding ID 중복 없음, UI 개수 유지, 환전·생산/취소·연구/탭·타워 강화, 전투/보상 회귀, 카메라 보호, HUD 월드 클릭 통과.
- 버튼 검증은 실제 씬 Button.onClick / Slider 이벤트를 호출하는 통제된 자동 확인이다. 하드웨어 마우스 클릭 성공으로 과장하지 않는다.
- Win32 실제 클릭/ESC 추가 시도는 실패: `SetForegroundWindow=false`, 전경 프로세스가 QA 게임과 다름. `Logs/UguiQA/live`는 이 미확인 시도이며 성공 증거가 아니다. 게임 창은 종료했다.
- 전체 한 판 난이도/경제/환생 동기나 사람의 편집/조작감은 이번 자동 확인으로 입증되지 않았다.
- 16/17/18의 과거 스크린샷은 최신 uGUI 증거로 재사용하지 않는다.

## 빌드와 작업 주의

- AGENTS.md를 먼저 읽는다. 작업 목록 선작성, 변경 전후 요약, 한국어 문서.
- `.meta` 직접 생성/수정 금지. Unity가 생성하도록 한다.
- 모델은 순수 C#, 뷰는 MonoBehaviour, Moonstone 주입을 유지한다.
- 에디터 TestRunner/EditMode/PlayMode 금지. 일반 컴파일은 `Tools/AfterlifePrototype/Build-Prototype.ps1`로 `dotnet build --no-restore`.
- Windows 빌드는 Unity 프로젝트에서 `Tools/AfterlifePrototype/Build-Windows.ps1`. 실행 전에 Unity 에디터가 닫혔는지 확인하고 사용자에게 알린다. 이미 열린 프로젝트를 batchmode로 중복 실행하지 않는다.
- 빌드 스크립트는 Graphics/Quality/Project/Build/URP 설정 5개를 백업/복원한다. 마지막 빌드 후 해시 일치를 확인했다.
- 이번에 Start-Process -Wait가 Unity 종료 후에도 대기하는 현상이 있어, Unity 프로세스 자체의 WaitForExit로 변경했다. 실패한 해당 스크립트만 식별해 종료하고 해당 백업을 복원했다.
- UI 씬 설치는 사용자 편집을 덮어쓰는 매 빌드 재생성이 아니다. 이미 있는 Canvas는 보존한다.
- 실제 Git 위치에서만 상태 확인. 기존 사용자/앞선 작업 변경이 많고 프로토타입 파일 상당수가 untracked였다. 임의로 되돌리거나 전체 정리/커밋하지 않는다. 이 세션은 커밋하지 않았다.
- Windows 파일 조작은 PowerShell 한 셸에서 LiteralPath로 처리한다. Python은 `-X utf8` 사용.
- 현재 별도 에이전트나 계속 실행해야 할 작업은 없다. 사용자 요청 없이 새 persistent goal/자동화/하위 작업을 생성하지 않는다.

## 새 세션에서 이어갈 순서

1. 이 문서·AGENTS·status와 마지막 결과를 읽고 현재 파일 상태 확인.
2. 사용자 다음 요청에 맞춰 진행. 사용자에게 이미 답한 기획 결정을 다시 질문하지 않는다.
3. 우선 검증 후보는 에디터에서 UI 편집 편의성과 일반 실행의 실제 마우스/ESC/스크롤 입력. 포커스가 확보된 환경에서 확인한다.
4. 이후 M5 전체 판의 난이도·자원 경제·방어 선택·환생 효용을 검증하고, 안정되면 M6 업데이트 범위를 고정한다. 이 목록은 후속 후보이며 지금 자동으로 모두 구현하라는 지시가 아니다.

## 새 세션 시작 메시지 예시

`D:/area/dev/alone/Afterlife/docs/game-process/session-handoff.md를 먼저 읽고 현재 상태를 이어받아줘. M5의 uGUI 전환까지 완료한 프로젝트야. 다음 요청은 ...`
