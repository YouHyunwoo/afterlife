# 프로토타입 ScriptableObject 데이터 구조

> 확정 기획 · 2026-10-03: 채집·건설·수리·공격은 버튼을 누르고 있는 동안 작업 주기에 맞춰 반복하고, 강화는 작업 1회당 작업량을 높인다. 자동 수입은 농장 건설·생산 강화로 확장한다. 본편 상호작용 필드·슬롯·충전·호버 자동 작업은 폐기한다. [새 규칙·사용법·검증](../game-process/71-click-work-and-farm-automation.md). 새 규칙은 게임 코드·에셋에 아직 적용하지 않았다. 이전 규칙·수치·QA는 당시 기록으로 보존한다.


## 새 규칙 데이터 계약 · 2026-10-03 / 미구현

본편 설계의 조절 대상은 작업 1회당 채집/공사/수리/피해·단가/비용, 농장 기본 생산량/주기와 생산 강화 효과·가격/선행이다. MvpSlots·초기/최대 슬롯·충전·필드 수명/펄스/반경·영력 설치 비용은 본편에서 사용하지 않을 계획이며 기존 시연/소비자를 위한 참조와 값은 보존한다. ‘슬롯’이라는 이름의 UI 예약 칸/일일 선택지 수는 필드 슬롯과 별개라 유지한다.

SO 저작→판 시작 스냅샷→순수 모델/서비스→게임 어댑터의 방향과 원본 SO 불변 원칙을 유지한다. 농장 수량/주기와 인접/비옥 보너스를 별도 효과로 표현하고 회당 작업량 강화·풍요·보상 카탈로그를 함께 연결한다. 새 필드명·효과 ID·이전 버전·단계별 수치는 아직 확정하지 않는다. 문서 작업에서 SO·enum·GUID를 재작성하지 않았다.

> 2026-09-29 건물 개편: [64 건물 자동 생산·마을·에테론](../game-process/64-building-economy.md). `PrototypeBuildingData.Stats`가 점유 깊이·시야·영역·통행·자동 생산·합체를 관리한다. 주민/인구 필드는 호환용으로만 남기며 본편 계산에서 제외한다. 마을/생산과 경제 지대 규칙은 각각 `Afterlife.Buildings`/`Afterlife.EconomicSites` 독립 코어다. 새 숫자는 프리팹에 저장하지 않는다.

> 2026-09-28 본편 MVP: `System.asset`의 `MvpSlots`로 슬롯·자동 스킬·환생 제외·일차별 낮 시간을 선택한다. 본편 강화는 별도 `MvpProgression.asset`/`ProgressionData/MvpResearch.asset`의 22노드 트리를 사용한다. 기존 Progression/Research/Altar 에셋은 영력 모드 소비자용으로 보존한다. 신규 슬롯·보상 설정과 설치·검증은 [63 MVP 구현](../game-process/63-mvp-slots-and-auto-skills.md)을 따른다.

2026-09-24. 기존 건물 가격 SO, 통합 강화 카탈로그, 통합 설정에 흩어진 콘텐츠와 조정 수치를 역할별 에셋으로 분리했다. 기존 씬·프리팹과 에셋 GUID는 보존한다.

## 작업 목록

- [x] 건물·유닛·웨이브·전리품·유물·환생·설정·환경 데이터 조사
- [x] 순수 C# 데이터 모듈과 Unity SO 분리
- [x] 기존 설정값을 보존하는 에디터 마이그레이션
- [x] 런타임 모델·UI·강화 편집기 연결
- [x] 관련 8개 어셈블리 `dotnet build --no-restore`
- [ ] 사용자 직접 플레이 확인

## 편집 위치

Unity 프로젝트 기준 `Assets/Prototype/Resources/AfterlifePrototype/Settings.asset`은 각 데이터 SO를 참조하는 조립 에셋이다. 아래 18개 에셋은 인접한 `SettingsData/`에 있다. 숫자와 목록은 해당 에셋의 Inspector에서 수정한다.

| 에셋 | 관리하는 데이터 |
|---|---|
| `System.asset` | 낮·황혼·밤 시간, 작업량, 기본 채집·수리·공격, 영력 필드, 시야, 병력 기본 생산 시간 |
| `World.asset` | 지도 시드·반경, 자원 생성 주기·기본 상한, 비옥한 땅/영맥 개수·크기·최소 거리 |
| `Camera.asset` | 카메라 거리·가장자리 이동 속도, 사용자 UI 크기·이동 설정의 최초 기본값 |
| `Presentation.asset` | 픽셀 해상도, 시야 블러, 나무·바위·풀 크기와 식생 색상 |
| `Audio.asset` | 효과음 파일·볼륨·재생 간격·피치, 환경음 파일·음량·새 울음 간격, 기존 WorldAudioBank 참조 |
| `Units.asset` | 아군·적·동물의 이름, 체력·피해·사거리·이동·공격 속도, 모집 가격·시간 배율·선행 건물, 적 처치 골드·전리품 |
| `Waves.asset` | 일차별 적 수·반복 구성·그룹 수·간격·침공 방향 수·설명, 체력·피해 성장률·생성 거리 |
| `Resources.asset` | 나무·바위 잔량·판매 단가·귀중품 확률·정산 가격, 자연 생성·군집 밀도·간격·상한 여유분, 자원 변종 이름 |
| `Economy.asset` | 골드당 수리량, 편입 제단·유적 효과. 인구·주민·수동 예약 필드는 레거시 호환용 |
| `Combat.asset` | 공격 선후딜·경직·동물 도주, 시야·치명타, 낙뢰·연쇄·감전·보호막·여운·쿨다운 환급, 화상·병력 표적 탐색 주기 |
| `Growth.asset` | 기존 일반 강화·타워 강화 비용·상한·단계별 효과, 발화·화상 기본 수치 |
| `Rebirth.asset` | 기억 강화 효과·비용·상한, 일차·처치·보유 골드의 환생 보상, 외형 해금 일차 |
| `Relics.asset` | 유물별 실제 효과 계수, 일일 선택지 수·자원 재생 최소 간격 |
| `Loot.asset` | 전리품별 이름·판매 가격 |
| `Chests.asset` | 등급별 이름·내구도·출현 가중치·보상량·허용 유물/전리품, 생성 주기·개수·최소 거리 |
| `Environment.asset` | 지형 팔레트·수면/해저 높이, 풀 밀도·잎 카드 수·바람 세기·유적 발광·눈 타격 표현 |
| `Destruction.asset` | 기존 독립 파괴 모듈의 쓰러짐·파편·제거 프로필 |
| `WorldVisuals.asset` | 건물·유닛·자원·도구·장식의 모델/프리팹 참조 목록 |

추가 콘텐츠 위치:

| 위치 | 역할 |
|---|---|
| `Resources/Data/Buildings/*.asset` | 건물별 가격·이름·아이콘·선행 건물, 체력·공사 시간·점유 폭/깊이·타워 성능, 시야·영역·자동 생산·합체. 연구소는 참조 호환용으로 보존 |
| `Resources/Data/BuildCatalog.asset` | 기존 건물 목록·건설 아이템 프리팹. 참조 보존 |
| `Resources/AfterlifePrototype/ProgressionData/Research.asset` | 연구소 강화 트리: 노드·선행 관계·단계별 비용/효과·아이콘·배치 |
| `Resources/AfterlifePrototype/ProgressionData/Altar.asset` | 제단 강화 트리. 연구소와 독립된 SO |
| `Resources/AfterlifePrototype/ProgressionData/Skills.asset` | 스킬별 쿨다운·지속 시간·펄스 간격 |
| `Resources/AfterlifePrototype/Progression.asset` | 위 트리·스킬 SO를 참조하는 기존 GUID의 카탈로그 |
| `Resources/Data/RelicCatalog.asset` | 기존 유물 표시 이름·아이콘·툴팁·목록 프리팹. 효과 문구는 현재 모델의 유물 수치로 생성 |

`Day02/Data/Day02Settings.asset`도 같은 구조의 `Day02SettingsData/`를 갖는다. 지도 반경 12·시드 1·카메라 거리 18 등 촬영용 값과 본편 값을 각각 보존했다. 이후 Day02 씬 생성 시에도 자식 데이터까지 복제하여 본편 설정을 변경하지 않는다.

## 데이터 경계와 수명

- `Assets/Modules/PrototypeData/Afterlife.Prototype.Data.asmdef`는 `noEngineReferences: true`인 독립 데이터 모듈이다. 기존 enum의 이름과 숫자를 보존하고 순수 데이터·기본값·검증을 이곳에서 정의한다.
- SO 타입은 Unity 어셈블리에 둔다. 순수 모델은 SO, 씬, UI, Resources, AssetDatabase를 참조하지 않는다.
- 씬 조립 시 설정과 자식 SO의 런타임 복사본을 만들고, 별도 `Balance`/`PrototypeRules` 스냅샷을 모델에 전달한다. 건물 데이터는 최초 집 생성 전에 주입한다.
- 강화 카탈로그도 트리·스킬 데이터를 깊게 복사한다. 구매 레벨·스킬 상태·체력·보유 자원은 모델/서비스가 소유하며 원본 SO에 쓰지 않는다.
- 판/환생 초기화는 기존 상태 초기화 규칙을 따른다. 설정 스냅샷은 해당 씬의 수명 동안 유지한다. 씬 종료 시 런타임 복제 SO를 해제한다.
- Inspector에서 저작 에셋의 값을 변경한 결과는 다음 플레이 시작에 적용된다. 실행 중 모델 수치만 따로 바꾸는 핫 리로드는 제공하지 않는다.
- 사용자 UI 크기·카메라 선호도는 기존 `PlayerPrefs` 키로 저장한다. `Camera.asset`의 기본값은 아직 저장된 선호도가 없을 때 사용한다.

## 기존 에셋 이전과 편집

`PrototypeDataMigration`은 에디터 재컴파일 후 플레이 중이 아닐 때 실행한다. `Assets > Afterlife > 데이터 분리 및 검사`에서도 실행할 수 있다. `Tools` 메뉴는 추가하지 않았다.

- 기존 설정 필드는 `FormerlySerializedAs`를 붙인 숨김 필드에 보존하고 최초 이전 시 자식 SO로 복사한다. 숨김 값은 호환용이며 이후 저작의 기준은 연결된 SO다.
- 기존 경로에 데이터가 있으면 덮어쓰지 않는다. 손상되거나 다른 타입인 파일이 있으면 오류를 표시하고 중단한다.
- 완료 버전이 기록된 설정은 자동으로 다시 초기화하지 않는다. 참조 누락·잘못된 ID·비용·필수 간격은 시작 시 검출한다.
- 신규 에셋과 `.meta`는 Unity `AssetDatabase`가 생성한다. `.meta`를 수작업 작성하지 않는다.
- 기존 `Window > Afterlife > 강화 에디터`는 선택한 트리 SO에 Undo/저장을 적용한다. 아이콘 가져오기도 실제 트리 에셋을 저장한다.
- 현재 프리팹은 예약 슬롯 10개, 일일 보상 슬롯 3개다. 설정은 예약 0~10개, 선택지 1~3개를 지원하며 초과값은 오류로 처리한다.

## 코드·프리팹에 유지한 항목

조사 대상은 프로토타입의 콘텐츠 정의와 운영·밸런스·표현 설정이다. 메시 정점·좌표 변환·격자 키·경로 탐색 표본·셰이더 배열 크기·프레임 적분 같은 알고리즘/구현 상수는 코드에 남긴다. 절차적 모델의 부품 배치와 씬 UI RectTransform·이벤트 바인딩·모달별 동작은 기존 프리팹/컴포넌트에서 저작한다. 현재 사용하지 않는 `FirstPreparation`/`Maintenance`는 호환용 숨김 값으로 유지한다. 초기 기본값과 에셋 누락 시의 레거시 호환 경로는 존재하지만, 정상 플레이의 콘텐츠 수치는 연결된 SO에서 읽는다.

## 검증과 직접 확인

`Tools/AfterlifePrototype/Build-ScriptableData.ps1`로 아래 8개를 `dotnet build --no-restore`했다. Unity가 생성한 csproj의 참조를 사용하고 별도 검증 csproj/출력을 Temp에 분리하며 패키지 복원을 실행하지 않는다.

- Prototype.Data, Progression.Unity, Prototype, Prototype.Editor
- Day01, Day02, Day01.Editor, Day02.Editor

전부 오류 0개. Day02.Editor에는 기존 JSON 로딩용 필드의 CS0649 경고 14개가 있다. 일반 Unity 생성 csproj의 누락된 `project.assets.json` 때문에 복원 없는 빌드가 불가능하여 기존 저장소 방식과 같은 참조 기반 검증 프로젝트를 사용했다.

Unity Play/Test Runner는 프로젝트 규칙에 따라 실행하지 않았다. 사용자는 다음을 확인한다.

1. 본편 씬에서 새 게임을 시작한다. 건설 가격·체력·모집 UI가 각 SO의 값과 같고 기존 첫 집·자원·영력 필드가 정상 동작해야 한다.
2. 연구소·제단 트리를 각각 열어 노드를 구매하고 스킬을 사용한다. 단계 비용·효과·쿨다운이 각 에셋과 같아야 한다. 강화 에디터에서 수정 후 다시 열어 변경이 유지되는지도 확인한다.
3. 낮밤·침공·상자·유물 선택·환생을 진행한다. 설정된 적 구성·보상·효과가 적용되고 툴팁/단계 상한과 일치해야 한다.
4. 카메라·식생·효과음·자원 파괴를 확인한다. `Day02Settings`로 시작한 장면이 본편 설정을 변경하지 않아야 한다.
5. 플레이를 종료하고 SO를 확인한다. 런타임 체력·강화 레벨·사용자 설정 변경이 원본 에셋에 기록되지 않아야 한다.
