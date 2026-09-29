# 45 독립 화면 흐름 모듈

> 2026-09-28 프로토타입 적용: [62 ThresholdLab 화면 통합](62-threshold-prototype-integration.md). 아래 기존 `Game Flow Screens` 설명은 적용 전 기록이다.

> 독립 화면 샘플 · 2026-09-26: [54 사후세계 화면 흐름 테스트 씬](54-threshold-screen-lab.md)에 동일 공간의 타이틀→로비 이동, 실제 마을을 보는 균열과 모달/승패 전환을 작성했다. 기존 프로토타입 적용은 아직 하지 않았다.

> 후속 설계 · 2026-09-26: [53 사후세계 설정과 화면 연출](53-afterlife-world-and-screen-direction.md)에서 사후세계 로비와 테마 전환 방향을 기록했다. 아래는 기존 구현 기록이며 새 로비·수중 전환이 구현됐다는 뜻이 아니다.

> 현재 조작(2026-09-24): 영력을 써서 위치에 8초 유지되는 필드를 설치한다. 필드는 커서와 무관하게 범위 안 대상을 주기적으로 처리한다. 대상 클릭·빈 타일 Shift+클릭이 설치 입력이며 R8은 작업 간격을 줄인다. 아래의 과거 입력·QA 기록은 당시 버전 설명이다. [현재 구현·검증](47-divine-interaction-fields.md).


작성일: 2026-09-19. 구현과 독립 어셈블리 컴파일 완료. Unity 화면 확인은 미실시.

2026-09-27 후속: 고정된 화면 구조를 `Game Flow Screens.prefab`과 씬 인스턴스로 옮겼다. 아래 흐름 코어와 화면 전환 규칙은 유지한다.

## 작업 목록

1. 기존 상태 머신, 프로토타입 초기화, 승패 및 UI 입력 경로 조사.
2. Unity 비의존 흐름 코어와 독립 asmdef 구현.
3. 게임 전환 규칙, 세션 어댑터, 기본 화면 연결.
4. 모듈별 C# 컴파일과 재사용/수동 검증 안내 작성.

## 변경 전후

기존에는 씬 시작 시 게임을 바로 진행했고 승패는 인게임 결과 모달로 표시했다. 이제 기본 진입은 타이틀이며 `Title → Intro → Playing → Clear / GameOver → Title`을 따른다. 게임 메뉴의 포기도 같은 씬의 타이틀로 돌아간다. 외부 TitleSceneName은 흐름에 사용하지 않지만 기존 씬 직렬화 호환을 위해 필드는 보존한다. 구형 Result.Restart/Menu.Restart 바인딩도 타이틀 복귀로 연결된다.

인트로는 현재 설명을 읽고 ‘마을로 들어가기’를 누르는 정적 화면이다. 44번 문서의 단계형 플레이 튜토리얼을 구현한 것은 아니다. 타이틀·인트로·승리·패배는 씬의 `Game Flow Screens` Canvas 하나를 공유하며, 런타임에는 문구와 표시 상태만 바꾼다.

## 계층

| 계층 | 구현 | 책임 |
| --- | --- | --- |
| 재사용 코어 | Assets/Afterlife/Modules/Flow | 임의의 상태/이벤트, 전환 표, Guard, Enter/Exit, 변경 이벤트, 해제 |
| 게임 흐름 | PrototypeGameFlow.cs | 다섯 화면 규칙, 결과 스냅샷, 세션 Begin/Stop |
| 조립 | PrototypeScene.cs | Moonstone에 모델·뷰·흐름 등록 |
| 세션/표현 연결 | PrototypeFlow.cs | 기존 모델 리셋, 입력·시뮬레이션 경계, Canvas 표시, 종료 정리 |
| 기본 화면 | Game Flow Screens.prefab / PrototypeFlowScreen.cs | Canvas·텍스트·버튼 배치와 타이틀·인트로·승리·패배 표시 및 버튼 의도 이벤트 |

`Afterlife_PixelPrototype` 씬의 루트 `Game Flow Screens`는 `Assets/_AfterlifePrototype/Prefabs/Game Flow Screens.prefab` 인스턴스다. 게임 HUD의 `Canvas`와 별도 Canvas이므로 HUD를 숨겨도 흐름 화면을 표시할 수 있다. `GameRoot`의 `PrototypeView.Flow Screen` 참조가 이 인스턴스의 `PrototypeFlowScreen`을 가리키고, 기존 HUD `Canvas`에는 `CanvasGroup`을 편집 모드에서 저장한다. 제목·본문·버튼 라벨·배경·버튼 스타일은 프리팹에서 편집하며, 글꼴은 초기화 시 기존 `PrototypeCanvas.Font`를 적용한다.

레거시 `Afterlife.Dev.State`의 Enter/Exit 방식은 참고하되 어셈블리 의존을 가져오지 않았다. 코어 네임스페이스는 `Afterlife.Modules.Flow`이고 게임 이름과 Unity를 모른다. Afterlife의 낮·황혼·밤·정산·환생은 기존 `VillageModel.Phase`가 계속 담당한다.

## 수명과 입력

- 씬 수명: 모델, 지형, 설정, 카메라, Moonstone 컨테이너와 흐름 인스턴스를 한 번 조립한다. 현재 프로토타입은 모델 객체를 재사용하며 완전한 세션 DI 스코프 교체는 하지 않는다.
- Playing 진입: 모델 Reset, 월드 뷰 재생성, 입력/카메라/시간 누적/일차 배너 초기화. 영구 해금 및 사용자 설정은 유지한다.
- Playing 종료: 입력 중단, 시간 누적 제거, 모달 스택/블러/스킬·채집 연출 정리, 동적 월드 뷰 해제. 게임 이외 화면에서는 월드 카메라와 게임 Canvas 입력을 끈다.
- 포기 후 모델 데이터는 숨겨진 단일 모델에 남지만 더 이상 갱신되지 않는다. 다음 Playing 진입의 Reset으로 판 데이터와 강화/스킬이 초기화된다. 승패 종료의 모델 정리는 기존 End가 수행한다.
- 결과는 변경 불가능한 GameFlowResult로 복사한다. RunEnded 콜백은 플래그만 설정하고, 모델 Tick과 그 프레임 동기화가 끝난 뒤 화면을 전환한다. 결과 처리 도중 모델을 초기화하지 않는다.
- 타이틀 복귀 시 결과 스냅샷 제거. 타이틀/인트로에서 음원 큐 정리 및 게임 효과음 중지. 승패 결과음은 결과 화면에서 재생할 수 있도록 유지한다.
- 씬 해제: 흐름 Dispose, 화면 이벤트 구독과 버튼 리스너 해제, 원래 게임 CanvasGroup 설정 복구. 씬이 소유한 화면 프리팹 인스턴스는 코드에서 파괴하지 않는다.
- 화면 버튼은 표시된 같은 프레임의 재입력을 거절한다. 머신은 전환 중 재진입을 거절하고 규칙에 없는 이벤트를 무시한다.

## 재사용과 확장

다른 프로젝트에서는 Flow 폴더만 복사하고 자체 enum/상태/전환과 콜백을 정의한다. Unity 소비 어셈블리에는 Afterlife.Flow를 명시적으로 참조한다. 구체적인 예제와 오류 계약은 모듈 README.md에 있다.

시각 디자인을 교체할 때는 PrototypeFlowScreen의 Show/Requested 경계를 대체한다. 씬별 화면을 원하면 프레젠터와 세션 어댑터가 씬 로딩을 소유하고 코어는 유지한다. 비동기 로딩 자체는 이번 동기 모듈에 포함하지 않았다.

## 검증

기존 Build-Modules.ps1 검증에 이어 후속 화면 프리팹 변경은 `Afterlife.Prototype.csproj`와 `Afterlife.Prototype.Editor.csproj`에 `dotnet build --no-restore`를 수행한다. Unity CLI로 편집 모드 프리팹·씬 참조를 확인하며, Unity 테스트와 플레이 모드 검증은 저장소 지침에 따라 실행하지 않는다. `.meta`는 Unity 에디터가 생성한다.

Unity 수동 확인 절차:

1. 임포트 완료 후 Afterlife_PixelPrototype 씬을 재생한다. 타이틀만 표시되고 월드 시간이 진행되지 않아야 한다.
2. 게임 시작 → 인트로 → 마을로 들어가기를 누른다. Day 1, 정상 HUD와 새 마을이 표시되어야 한다. 빠른 중복 클릭이 인트로를 건너뛰지 않아야 한다.
3. 게임 메뉴에서 포기한다. 타이틀로 돌아오고 모달·블러·월드 조작이 남지 않아야 한다. 재시작 시 기존 돈/강화/선택/스킬 상태가 초기화되어야 한다.
4. 마지막 집을 잃는다. GameOver 화면과 해당 판 통계가 한 번 표시되어야 한다. 타이틀 복귀 뒤 다시 시작할 수 있어야 한다.
5. 설정된 최종 밤을 방어하고 결산 보상을 완료한다. Clear 화면과 통계가 표시되어야 한다. 영구 해금은 다음 판에도 유지되어야 한다.
6. 타이틀/인트로/결과에서 ESC·마우스·스킬 키를 눌러도 게임이 진행되지 않아야 한다. 각 화면에서 해상도를 바꿔 텍스트와 버튼을 확인한다.
7. 여러 판 반복과 각 상태에서 플레이 종료를 확인한다. 중복 Canvas/리스너와 MissingReferenceException이 없어야 한다.
8. 재생 전 씬 하이어라키에 `Game Flow Screens`가 하나 있고 `PrototypeView` 참조가 연결되어 있는지 확인한다. 재생 중에도 두 번째 인스턴스가 생기지 않아야 한다. 프리팹에서 제목 색상이나 버튼 스타일을 바꾸면 화면에 반영되어야 한다.
