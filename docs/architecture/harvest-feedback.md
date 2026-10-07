# 채집 결과·입력 표시 피드백 모듈

2026-10-04 · Day02 적용 완료, 사용자 Play 확인 대기.

## 책임과 의존성

`Assets/Afterlife/Modules/HarvestFeedback/`는 채집 결과와 전달받은 입력 상태의 표시를 담당한다. 골드 지급·자원 잔량·마우스 입력 수집은 소비자가 소유한다.

|어셈블리|책임|참조|
|---|---|---|
|Afterlife.HarvestFeedback.Core|FeedbackMotion의 바운스·페이드·아이콘 궤적, PointerInputPulse의 입력 표시 상태|없음, noEngineReferences|
|Afterlife.HarvestFeedback.Unity|HarvestFeedbackPresenter의 결과 표시, PointerInputPresenter의 입력 링·텍스트·좌표·수명|Core, TMP, uGUI|
|Afterlife.Devlog.Day02|작업량·자원 종류·작업 시작 화면 좌표 주입, 골드 표시 숫자|HarvestFeedback.Unity, 기존 SceneFeatures|

공용 모듈은 Day01·Day02·Prototype을 참조하지 않는다. Day02의 목재·석재 Sprite는 프로토타입에서 쓰는 기존 Icon0/1 에셋을 Inspector 참조로 주입한다. 본편 입력·경제는 변경하지 않았다.

Day02 골드 HUD Sprite는 프로토타입 HUD.GoldIcon과 동일한 `Assets/_AfterlifePrototype/Resources/UI/Icons16.png`의 `Icons16_5`를 사용한다. Sprite 에셋 참조만 공유하며 런타임은 프로토타입 클래스나 씬을 참조하지 않는다.

## 컴포넌트 하나로 적용

1. 기존 Canvas 또는 그 자식 RectTransform에 `HarvestFeedbackPresenter` 하나를 추가한다. 내부 효과 레이어와 TMP/Image는 자동 생성·재사용한다. 새 Canvas나 EventSystem은 필요하지 않다.
2. 초기화 때 `Initialize(font, goldTarget, woodSprite, stoneSprite)`를 호출한다. 텍스트에는 TMP 폰트가 필요하고, 아이콘 연출에는 같은 Canvas의 골드 RectTransform과 두 Sprite가 필요하다.
3. 완료된 작업마다 `ShowText(screenPoint, message, color)` 또는 `ShowCollection(screenPoint, stone, actualAmount)`를 호출한다. 화면 좌표는 좌하단 원점의 실제 포인터 픽셀 좌표다. CanvasScaler 변환은 모듈이 수행한다.
4. 소비자의 시간 흐름에서 `Tick(delta)`를 한 번 호출한다. 일시정지 중에는 0을 전달한다. 별도 Update에서 시간이 중복 진행되지 않는다.
5. `IconArrived`를 구독하면 아이콘 하나의 도착을 알 수 있다. 실제 재화 지급 대신 표시 숫자나 HUD 반응에만 사용한다.

농장처럼 목재/석재 이외의 결과는 `ShowCollection(screenPoint, sprite, actualAmount)` 오버로드로 원하는 Sprite를 주입한다. 기존 bool 오버로드와 직렬화 필드는 유지한다. 이 경로에서는 `Initialize(font, goldTarget, null, null)`로 연결한 뒤 호출마다 Sprite를 넘길 수 있다. 모듈은 자원 종류나 생산/재화 규칙을 소유하지 않는다.

Day03은 기존 Day03Village 프리팹 하나로 적용한다. Showcase가 현재 실제/비교/맛보기 세션의 GoldProduced를 구독하고 농장 보리의 월드 위치를 픽셀/레터박스 화면 좌표로 바꾼다. Hud가 기존 Canvas에 Presenter 하나를 자동 추가해 보리 `Icons16_10`·골드 `Icons16_5`·폰트를 주입한다. 보리 +N 텍스트는 기본0.85초 바운스/페이드, 아이콘은 Day02의48크기·Spread96·Arc120·0.25초 튀어나옴/0.55초 비행을 사용한다. 도착은 골드 아이콘 펄스와 HUD +N만 표시하며 실제 골드는 생산 시 Core에서 한 번 지급한다. 정지는 delta0, 샷 이동/초기화/숨김/종료는 Clear, 스킵 빠른 계산은 효과 생성을 보류한다. 해제 시 생산/도착 이벤트를 구독 해제하고 소유 HUD를 제거한다. 추가 수동 UI 설치는 없다.

2026-10-06 Sprite 오버로드 검증: HarvestFeedback.Core/Unity를 포함한 Day03 관련8개 어셈블리와 새 DLL을 참조하는 Day02 소비자 소스의 dotnet build --no-restore 통과. 기존 bool 호출 경로를 보존했다. 실제 모션·정지·목표 좌표·중복 지급 여부는 사용자 Play에서 확인한다. [절차·빌드 기록](../../devlog/youtube/day-03/records/showcase.md).

Day02는 기존 런타임 Day02Hud에 컴포넌트를 자동 추가한다. 씬의 Day02Showcase에는 폰트·목재/석재 Sprite와 HUD용 `GoldHudSprite`를 연결한다. HUD는 코인 Image·수치 TMP를 자동 구성하고 코인의 RectTransform을 Presenter에 주입한다. 기존 섬은 SmallHarvestIsland 프리팹을 재사용한다. 새 Tools 메뉴나 수동 씬 재설치는 필요하지 않다.

## 표현

### 입력 표시 컴포넌트 하나로 적용

1. Canvas에 `PointerInputPresenter`를 추가하고 `Initialize(font)`를 호출한다. 내부 링 Sprite/Texture와 TMP 자식은 자동 구성한다. 외부 이미지 에셋·InputSystem·EventSystem은 필요하지 않다.
2. 외부에서 `Tick(delta, visible, screenPoint, clickMode, pressed, held)`를 호출한다. 클릭 모드는 누름 시작마다 0.22초 흰 링, 유지 모드는 누르는 동안 금색 링을 표시한다. 이 컴포넌트는 직접 입력을 읽거나 작업을 실행하지 않는다. 화면 좌표는 실제 포인터 픽셀 좌표이며 CanvasScaler 변환을 수행한다.
3. 일시정지에는 마지막 표시를 유지하도록 Tick을 보류한다. 샷/공간/세션 초기화에는 Clear를 호출한다. OnDisable은 표시를 숨기고 OnDestroy는 소유한 자식·Sprite·Texture만 해제한다. 폰트는 해제하지 않는다.

Day02Showcase는 HUD에 컴포넌트를 자동 추가하고 사용자 입력 또는 시연 입력을 전달한다. 순수 PointerInputPulse는 Unity 컴포넌트 없이도 에디터 미리보기의 외부 시간으로 사용할 수 있다. 신규 스크립트는 기존 Core/Unity 독립 어셈블리에 포함된다.

### 채집 결과

- 1~3번 공간: 작업 시작 때의 클릭/현재 커서 위치에서 `목재 +N` 또는 `석재 +N`이 올라온다. 감쇠 바운스로 튀어 오르고 후반에 페이드 아웃한다. 기본 0.85초, 상승 58 Canvas 단위, TMP 30 크기. 연속 작업은 각각 독립적으로 표시한다.
- 4번 공간: 실제 획득량 N만큼 아이콘 N개를 만든다. Day02는 48×48 크기, 흩어짐 96·곡선 높이 120 Canvas 단위를 적용한다. 아이콘 간 0.025초 시차, 0.25초 흩어짐 이후 0.55초 곡선 비행이다. 프로토타입의 아이콘 수 요약 방식 대신 수량을 그대로 표시한다.
- 공용 Presenter의 기본값은 `IconSize=32`, `Spread=48`, `Arc=70`이다. `IconSize`는 풀에서 재사용하는 아이콘에도 매 생성 시 반영한다. Day02Showcase Inspector의 `Collection feedback`에서 `CollectionIconSize`, `CollectionSpread`, `CollectionArc`를 바꾸면 초기화 때 Presenter에 주입된다.
- 아이콘 도착점은 주입된 GoldTarget RectTransform의 중심이다. Day02는 2026-10-05부터 HUD 코인 아이콘을 주입한다. 도착 때 표시 골드가 1씩 증가하고 코인이 짧게 커졌다 돌아온다. HUD 수치는 천 단위 구분으로 표시하며 4번 상호작용 구간에만 아이콘·수치가 함께 나타난다. 모델 지급은 기존 HarvestSession에서 이미 처리하며 도착 이벤트로 다시 지급하지 않는다.
- TMP/Image의 raycastTarget은 false이므로 채집 입력을 가로막지 않는다.

## Day01 도구 모션 연결

Day02는 자체 TickTool 회전 대신 Day01 After가 사용하는 `ImpactPresentation`을 소비한다. 같은 FBX 자식 위치·회전·크기와 날 끝의 실제 ImpactPoint를 사용한다. 원점에 있던 임시 접촉 마커는 바인딩에서 제외했다.

`SceneFeatureSession.BeginSurfaceImpact(target, point, contacted)`는 기존 표면 샘플링과 `ImpactPresentation.TryBeginImpact`를 연결한다. 0.12초 접근·접촉 정지·0.285초 스윙/회복 궤적을 공유하며, 접촉 콜백에서 채집 표시를 생성한다. 기존 `BeginImpact`와 즉시 표현용 `PresentSurfaceImpact` API는 유지한다. Day02는 마지막 작업의 접촉까지 고갈 파괴를 기다린다. 작업 시작 화면 좌표와 수량을 캡처하므로 접촉 전에 커서를 움직여도 표시 원점은 이동하지 않는다.

## 초기화와 해제

Day02의 R·공간 변경·필름 샷 변경은 작업 접촉 콜백·진행 중 텍스트/아이콘·표시 골드를 초기화한다. 공간을 나갈 때 SceneFeatureHost.Clear로 스윙을 취소한다. 필름 일시정지는 도구와 UI 효과 시간을 함께 멈춘다. 버튼 해제는 새 작업만 중단하고 이미 수락한 작업의 타격/표시는 마친다.

Presenter.Clear는 활성 표시를 풀로 반환하고 HUD 크기를 복원한다. OnDisable도 Clear를 호출한다. OnDestroy는 이벤트와 소유한 효과 레이어를 정리한다. 소비자는 자신의 IconArrived 구독을 해제한다. 공유 Sprite·폰트는 파괴하지 않는다.

## 검증

관련 csproj에 `dotnet build --no-restore -p:SkipResolvePackageAssets=true -p:GenerateDependencyFile=false`를 적용한다. Day02와 SceneFeatureMigration.Editor 빌드는 경고 0·오류 0으로 통과했다. 열린 Unity 에디터에서 씬·프리팹 저장과 날 끝/Sprite 직렬화 연결을 확인했다. .meta는 Unity가 생성했다. Unity Play·Test Runner는 실행하지 않았다.

사용자가 `Devlog_Day02.unity`를 열고 Play하여 아래 항목을 확인한다.

1. 1~3 키로 공간을 바꿔 나무/바위의 서로 다른 위치를 채집한다. 텍스트 원점이 클릭 위치와 일치하고 위로 바운스한 뒤 사라져야 한다. 1번에는 기존 구성대로 나무만 있다.
2. 2~3번에서 버튼 유지·커서 이동·해제를 확인한다. 매 작업의 도구 날이 현재 표면에 닿으며 접촉 순간 표시가 나온다. Day01 After와 접근 방향·정지·회복 모션을 비교한다.
3. 3번 U로 작업량 1/3을 비교한다. 4번은 코인 아이콘·수치가 보이고 작업량 3이면 종류에 맞는 아이콘 3개가 흩어졌다 코인 중심에 도착하고 표시 골드가 3 증가해야 한다. 마지막 잔량이 적으면 그 수량만 표시해야 한다. 1~3번·설명·마지막 크레딧에서는 골드 HUD가 숨겨져야 한다.
4. 텍스트/아이콘이 움직이는 동안 R·1~4·필름 샷 이동을 실행한다. 이전 공간의 효과·숫자 증가가 새 공간에 남으면 안 된다. F8/F9의 Space 정지 중 도구와 UI 효과도 멈추고 재개 시 이어져야 한다.
5. 1920×1080과 다른 Game 뷰 크기에서 클릭 위치·골드 도착점이 맞는지, 연속 채집/반복 Play 종료 뒤 예외·중복 표시가 없는지 확인한다.

Devlog의 상세 변경 기록은 [Day02 채집 피드백](../../devlog/youtube/day-02/records/harvest-feedback.md)에 둔다. Devlog는 Git 제외이며 본 문서와 Unity 구현은 추적한다.
