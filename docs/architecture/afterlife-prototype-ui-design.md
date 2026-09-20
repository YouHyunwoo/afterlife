# _AfterlifePrototype UI 분석 및 프리팹 중심 설계

2026-09-19 후속 분석. 범위는 `Assets/_AfterlifePrototype`과 이 코드가 직접 호출하는 UI 구현이다. EmberVillage와 `02_Scripts/UI`는 제외한다. 아래 경로는 Unity 프로젝트의 Assets 기준이다. 최신 소스를 다시 읽어 확인했으며 이전 Graphify 캐시에는 새 진행 UI 연결이 반영되어 있지 않아 해당 결론은 최신 소스에 근거한다.

작업 목록: (완료) 초기화·직접 의존 추적 → (완료) 동적 UI 분류 → (완료) Prototype 전용 모듈 설계. 실행 코드·씬·프리팹 변경은 없다.

## 1. 현재 상태와 정정

기존 화면 대부분은 이미 저장된 Canvas와 UGUI 프리팹을 사용한다. 그러나 현재 `PrototypeProgressionUi`는 연구소·제단 내부 화면과 스킬바를 런타임에 새로 조립한다. 이 경로가 프리팹 중심 작업을 가장 크게 방해한다.

이전 전체 분석에서 UpgradeTreeScreen의 호출처를 확인하지 못했다고 했으나, 현재 코드에서는 다음 경로가 명확히 존재한다.

```text
PrototypeScene.Awake
  → ProgressionCatalog 로드/기본값 생성, 모델 ConfigureProgression
  → PrototypeView.SetUp
    → InitializeSceneUi
      → InitializeBuildItems
      → PrepareProgressionUi: 제단 복제 → 환생 기록 창
      → PrototypeCanvas.Bind
      → InitializeProgressionUi
        → 연구소·제단의 기존 자식을 숨김
        → UpgradeTreeScreen 생성 및 Bind
        → 제단 스킬바 생성
```

`Afterlife.Prototype.asmdef`도 `Afterlife.Upgrades`, `Afterlife.Skills`, `Afterlife.Progression.Unity`를 참조한다. 따라서 폴더 밖 `UpgradeTreeScreen`은 Prototype이 사용하는 직접 의존으로 이번 범위에 포함한다. 실행 여부는 Unity 플레이 검증 없이 소스 연결로 확인했다.

## 2. 동적 UI 목록

| 우선순위 | 코드 | 실제 동작 | 프리팹 전환 |
|---|---|---|---|
| P1 | `Runtime/PrototypeProgressionUi.cs:50` | Research/Altar의 자식을 비활성화하고 root를 stretch한 뒤 UpgradeTreeScreen 추가 | 각 화면에 UpgradeTreeView 프리팹을 미리 배치. 기존 자식 숨김·새 화면 조립 제거 |
| P1 | 직접 의존 `Afterlife/Modules/Progression.Unity/UpgradeTreeScreen.cs:30` | 배경·viewport·마스크·노드·선·텍스트·버튼·상세 영역 생성. 배치·색·폰트 코드 지정 | 고정 shell 프리팹 + Node/Edge 항목 프리팹. 그래프 좌표 계산은 유지 |
| P1 | `Runtime/PrototypeProgressionUi.cs:68` | 하단 스킬바, lightning/abundance/ritual 버튼 3개, Image/Outline/TMP 생성 | SkillBar 프리팹에 슬롯 3개 배치하거나 SkillSlot 프리팹 목록 사용 |
| P1 | `Runtime/PrototypeProgressionUi.cs:21` | 현재 Altar 인스턴스를 복제하고 ID를 RebirthReview로 치환. fallback에서 경로로 자식 매핑 | RebirthReview 전용 프리팹/Variant로 저장하고 typed 참조 연결 |
| P2 | `Runtime/PrototypeBuildMenuItems.cs:12` | Build.Item 프리팹 생성, 비용/아이콘/이름 설정, 전역 binding 등록 | 생성 유지. 목록 소유권을 BuildMenuView로 이동 |
| P2 | `Runtime/PrototypeRelicHud.cs:42` | 소유 유물별 Relic.Item 프리팹 생성·제거 | 생성 유지. RelicItemView 참조와 독립 Presenter 도입 |
| P2 | `Runtime/PrototypeUgui.cs`, `PrototypeTileBuildMenu.cs`, `PrototypeDayStartBanner.cs` | 누락 시 CanvasGroup·배너·DisabledButtonSound 추가 | 프리팹에 미리 저장하고 누락 검증 |
| P3 | `Runtime/PrototypeModalBackdrop.cs:94` | 블러용 Camera 생성, 렌더 자원 관리 | 전용 Backdrop 컴포넌트의 인프라 책임으로 유지 가능 |
| P3 | `Runtime/PrototypeHud.cs:22` | 개발 빌드/에디터에서 OnGUI 디버그 패널 | 별도 Debug 모듈. 제품 화면 전환보다 후순위 |

건설 항목은 LayoutGroup이 없을 때 Y 위치·content 높이도 코드에서 계산한다. 프리팹 레이아웃을 필수로 하면 해당 fallback을 제거할 수 있다. 반면 건설 메뉴 루트를 타일 가까이 이동하고 화면 안으로 제한하는 `PrototypeTileBuildMenu` 동작은 유지해야 한다.

다음은 화면 계층을 동적으로 만드는 코드와 구분한다.

- Amounts, CollectionIcons, DayTick: 저장된 항목의 활성/텍스트/좌표를 갱신한다. 데미지 숫자·채집 연출·타임라인 모듈로 소유권을 옮기면 된다.
- PrototypeUiSymbol과 직접 사용하는 ProgressionBackdrop: 커스텀 UI 메시 렌더링. 프리팹에 붙일 수 있는 시각 컴포넌트로 유지 가능하다.
- 타일 건설 마커와 낙뢰 LineRenderer: 월드 표현. 화면 View가 아닌 월드 표현 모듈로 분리한다.
- Editor의 Migration 스크립트: 편집 시 프리팹을 작성하는 코드. 런타임 생성으로 집계하지 않는다.

## 3. 모듈화해야 하는 이유

- `PrototypeView`의 partial 파일들은 `_model`, `_sceneUi`, `_modal`, 월드 카메라·입력 상태를 공유하는 같은 클래스다. 파일을 나누는 것만으로는 독립 실행이나 의존성 제한이 되지 않는다.
- `PrototypeCanvas`는 문자열 Binding 저장소이면서 버튼 연결·텍스트 갱신·raycast 판정도 담당한다. 기능 패널의 Connect는 자신의 참조를 전역 목록으로 합친다.
- `PrototypeUgui.UiClick`은 건설·모집·설정·환생 등 대부분의 명령을 분기한다. LateUpdate는 여러 화면을 한꺼번에 갱신한다.
- `PrototypeProgressionUi`에는 강화 화면 생성, 환생 창 복제, 스킬 입력, 스킬 UI 갱신, 월드 낙뢰 연출까지 들어 있다.
- 화면을 프리팹에서 수정해도 InitializeProgressionUi가 기존 자식을 숨기고 별도 화면을 만들어 수정 사항이 실제 표시 화면에 반영되지 않을 수 있다.

## 4. Prototype 전용 목표 구조

```text
PrototypeScene / PrototypeUiComposition
  ├─ VillageModel + 기존 UpgradeService/SkillService
  ├─ Prototype 게임 Adapter
  ├─ UiShell: 레이어·모달·입력 차단·Backdrop
  └─ 기능별 Presenter ↔ 기능별 View + Prefab
```

| 독립 기능 | 옮길 책임 | 권장 View/Prefab |
|---|---|---|
| Progression | 연구소·제단 트리 선택, 설명, 구매 요청 | UpgradeTreeView, UpgradeNodeView, UpgradeEdgeView |
| Skills | 쿨다운·해금·버튼 표시, 발동 요청 | SkillBarView, SkillSlotView |
| Rebirth | 환생 기록 표시·실행 요청 | RebirthReviewView |
| Construction | 건설 목록, 가격·가능 여부, 선택 타일 문맥 | BuildMenuView, 기존 Build.Item |
| Relics | 유물 목록·설명 표시 | RelicHudView, 기존 Relic.Item |
| Barracks | 모집·예약·취소·진행 표시 | BarracksView, 기존 Barracks 프리팹 |
| Hud / Feedback | 재화·인구·일차 / 데미지·획득·배너 연출 | HudView, DayTimelineView, FloatingTextView, CollectionEffectView |
| 기타 모달 | Menu, Settings, Loot, Settlement, Result, Tower | 기존 프리팹에 각각 typed View |

재사용 경계:

1. View는 직렬화된 TMP/Button/Image 참조로 그리기만 하고 사용자의 의도를 이벤트로 보낸다. VillageModel이나 컨테이너를 직접 찾지 않는다.
2. Presenter는 순수 C#이며 화면 상태 DTO와 해당 기능의 작은 조회·명령 인터페이스에 의존한다. UI 컴포넌트나 다른 화면의 Presenter를 참조하지 않는다.
3. Prototype Adapter가 VillageModel의 Kind/Point/Body와 UI 계약 사이를 변환한다. 강화 구매의 건물 조건·재화 검증은 기존 모델/서비스에 남긴다.
4. Composition에서 Moonstone으로 기존 객체를 조립하고 Presenter에는 의존성을 명시적으로 전달한다. 새 공통 UI가 기존 Prototype assembly를 참조하지 않는다.
5. 기능 간 화면 열기는 명시적인 요청/결과로 연결한다. 예: 제단 화면의 환생 기록 요청 → 조립 계층 → RebirthReview 열기. Progression이 RebirthReviewView를 직접 소유하지 않는다.

asmdef는 우선 `Prototype.UI.Presentation`(Unity 비의존), `Prototype.UI.UGUI`(Presentation+UGUI/TMP), 기존 `Afterlife.Prototype` 내 Integration으로 시작할 수 있다. 기능별 폴더로 경계를 정하고 독립 재사용이 필요한 Progression/Skills부터 별도 assembly로 강화한다. 순환 참조를 피하려면 UI 계약에 기존 Prototype 타입을 노출하지 않는다.

## 5. 프리팹과 코드의 책임

프리팹에 저장: 고정 계층, 크기/앵커, 폰트·색·아이콘, 레이아웃, Button/CanvasGroup/Mask, typed 직렬화 참조.

코드에 유지: 목록 개수, 노드 데이터 좌표, 선 길이·각도, 실시간 숫자·게이지, 포인터 근처 패널 위치, 화면 밖 제한, 연출 진행, 게임 명령 요청.

권장 화면 구성:

```text
Research.prefab / Altar.prefab
  UpgradeTreeView
    Header / Balance / Close
    Viewport + RectMask2D
      Content
        EdgeRoot   # UpgradeEdge 프리팹 목록
        NodeRoot   # UpgradeNode 프리팹 목록
    Details
    ResetView / ZoomLabel
    ExtraAction    # 제단의 환생 기록 버튼
```

Research/Altar는 공통 화면 프리팹을 재사용하고 데이터·배경 설정으로 차이를 표현한다. RebirthReview는 별도 프리팹으로 관리해 Altar 자식 구조나 이름 변경의 영향을 받지 않게 한다. SkillBar는 현재 고정 3슬롯이라 프리팹에 슬롯을 미리 두는 구성이 가장 단순하다.

Presenter 수명은 초기화 → 구독/표시 → 해제 → 폐기로 명시한다. 재오픈·재바인딩 때 이벤트가 누적되지 않도록 하고 View가 자신이 등록한 리스너만 제거한다. 목록은 ID 기준으로 차이만 반영하며, 풀링은 빈번한 Feedback 항목부터 적용한다.

Prototype 고유 동작도 보존한다: 모달 중첩·ESC 우선순위, 관리 대상 건물 소멸 시 창 닫기, 건설 메뉴 바깥 클릭의 월드 입력 차단, 일시정지 정책, 스킬 Q/W/E와 조준 상태, RawImage로 표시되는 월드와 UI 사이 좌표 변환, 실제 재화 지급과 지연 표시의 분리.

## 6. 권장 순서

1. **연구소·제단 강화 화면 프리팹화**: 기존 프리팹 자식 숨김/새 화면 조립을 없애고 Inspector 편집 결과가 그대로 표시되게 한다.
2. **SkillBar·RebirthReview 프리팹화**: 스킬 컴포넌트 생성과 Altar 복제·ID 치환을 제거한다.
3. **Progression/Skills Presenter 분리**: UI 생성과 구매/발동 요청을 분리하고 낙뢰는 월드 연출 쪽으로 이동한다.
4. **Construction/Relics 분리**: 기존 항목 프리팹은 유지하고 전역 문자열 binding과 중앙 UiClick에서 분리한다.
5. **ModalHost·나머지 Presenter 이관**: 한 화면씩 전환하며 해당 중앙 분기와 갱신을 함께 제거한다.

완료 기준은 (a) 고정 UI 계층을 런타임에 조립하지 않음, (b) 화면 프리팹을 Fake Port로 단독 확인 가능, (c) 재오픈 시 명령이 한 번만 실행됨, (d) 기존 구매·모달·입력·연출 동작 보존이다.

이번 작업은 최신 코드 재분석과 문서 작성만 수행했다. 빌드와 Unity 에디터 테스트는 실행하지 않았다. 구현 후 자동 검증은 저장소 지침의 관련 csproj `dotnet build --no-restore`를 사용하고, 프리팹 외형 반영·반복 열기/닫기·중첩 모달·스킬 입력·UI 배율 변경은 사용자가 Unity에서 확인한다.
