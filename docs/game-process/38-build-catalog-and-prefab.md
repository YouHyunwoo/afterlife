# 38 · 건설 데이터와 런타임 프리팹 목록

## 사용자 정정 반영 · 현재 구현
사용자 의도는 에디터 배치가 아닌 게임 초기화 중 1회 동적 생성이다. 이전 편집 단계 구성은 폐기했다. PrototypeBuildMenuItems.InitializeBuildItems가 InitializeSceneUi의 Bind 호출 전에 데이터셋으로 Content 항목을 Instantiate하고 버튼/가격 바인딩을 등록한다. 메뉴 재개방/재도전/환생에서는 같은 항목을 재사용하며 가격/해금 상태만 갱신한다. 저장된 기존 건설 항목은 런타임에서 비활성화·제거 후 교체한다. 게임 오브젝트 종료 시 동적 항목을 정리한다.

에디터 ConfigureScene/Configure/InstantiatePrefab 및 IProcessSceneWithReport, 플레이 직전 씬 구성과 BuildCatalogStamp는 제거했다. 에디터 도구는 데이터 에셋 최초 생성과 요청된 원본 프리팹 TMP 변환/Label 제거만 수행한다. 수동 메뉴는 Prepare Build Catalog Assets로 변경. BuildPanel 표시 시 자식 배치를 유지하는 변경과 데이터 기반 모델 가격/해금 연결은 유지한다. C# 오류·경고 0, 실제 플레이 미실시.

확인 절차: 플레이 시작 시 Content에 데이터 수만큼 런타임 항목 생성 → 메뉴를 여러 번 열어도 중복 없음 → 항목 클릭/가격 차감/잠금 상태 확인. 아래 편집 단계 구성 기록은 최초 구현 이력이며 현재 동작은 이 정정 내용을 우선한다.

## 작업 목록
- [x] Build.Item 프리팹, TMP 대상, 기존 레이아웃 변경 경로 확인
- [x] 건설 항목별 ScriptableObject와 목록 데이터
- [x] 프리팹 Name Text/Cost Text TMP 변환, 미사용 Label 제거 코드
- [x] 편집 단계 Buildings/Content 구성과 바인딩
- [x] 데이터 가격/해금 조건을 모델에 전달
- [x] 메뉴 열기/갱신 중 자식 배치 강제 변경 제거
- [x] C# 컴파일 검증
- [ ] Unity 실제 에셋 생성/프리팹 변환/씬 구성 확인 및 저장

## 데이터와 편집
`Resources/Data/BuildCatalog.asset`에 순서대로 `PrototypeBuildingData` 참조와 Build.Item 프리팹을 저장한다. 개별 에셋은 `Resources/Data/Buildings/House.asset` 등 8개. Id, DisplayName, Icon, GoldCost, RequiresBuilding, RequiredBuilding을 편집한다. 최초 기본값만 생성하며 기존 데이터는 덮어쓰지 않는다. 순서/추가/삭제는 Catalog.Buildings에서 관리한다. 새 ID 자체는 기존 게임의 건물 Kind 및 동작 구현이 필요하다.

기존 `Prefabs/Build.Item.prefab`의 Name Text/Cost Text를 동일 오브젝트의 TextMeshProUGUI로 변환하고 위치·크기·색·정렬·줄바꿈 설정을 복사한다. 사용하지 않는 빈 Label은 프리팹에서 삭제한다. PrototypeBuildItem이 TMP/Button/Icon/데이터 참조를 저장한다. 이미 변환한 TMP의 사용자 설정은 덮어쓰지 않는다.

스크립트 임포트, 씬 열기, 플레이 진입 직전에 편집 단계 구성을 실행한다. 데이터/프리팹 해시 및 기존 항목/바인딩 상태가 일치하면 생략한다. 기존 Build.Item/Build.Cost 바인딩이 가리키는 항목만 정리하고 Content에 PrefabUtility로 인스턴스를 생성한다. 기존 자식의 무관한 오브젝트는 삭제하지 않는다. Content에 LayoutGroup이 있으면 해당 레이아웃 사용, 없으면 프리팹 크기로 8 간격 세로 배치 및 필요한 Content 높이 증가를 편집 단계에서 수행한다. 게임 중에는 목록 생성/삭제를 하지 않는다. 빌드 시 씬 처리에도 구성을 연결했다.

수동 갱신: Tools > Afterlife > Configure Build Catalog Items. 씬 변경은 Undo 가능하며 자동 저장하지 않는다. 프리팹 TMP 변환과 데이터 에셋 생성은 Unity API로 저장하고 .meta는 직접 만들지 않는다. 생성 후 씬을 저장해야 한다.

## 실제 게임 연결
PrototypeScene이 데이터의 가격/필요 건물을 순수 ConstructionRule로 복사하여 Moonstone 모델 조립 전에 설정한다. HUD 가격·잠금 상태, 배치 가능 검사, 골드 차감 및 PaidGold가 같은 모델 규칙을 사용한다. 기본 정적 BuildPrice는 에셋 최초 생성/구형 데이터 폴백용으로 남긴다. 환생/재도전에도 건설 규칙은 유지된다.

## 레이아웃 변경 전후
이전 Open/UpdateTileBuildMenu는 Buildings, Viewport, 스크롤바, 프레임, 배경, 버튼, 아이콘, 텍스트의 위치/크기와 표시 여부를 강제로 수정했다. 이제 BuildPanel 루트만 타일 위로 이동하고 화면 경계에 맞춰 크기를 제한한다. 표시 기준 크기는 씬의 패널 크기이며, 자식 계층·앵커·위치·크기·표시 여부와 스크롤 축 설정은 변경하지 않는다. 스크롤 시작 위치 초기화와 기존 등장/역재생·배경 어둡게 연출은 유지한다.

## 검증
C# dotnet build --no-restore 오류·경고 0. Unity 플레이/테스트 및 Windows 빌드는 실행하지 않았다. 실제 에디터 적용은 확인 필요.

직접 확인: Unity 임포트 완료 후 Build.Item의 두 TMP와 Label 삭제, 개별 건설 에셋 8개, Content의 프리팹 인스턴스/바인딩 확인 → 씬 저장 → 플레이 후 타일 클릭 시 편집한 Buildings 배치 유지 확인 → 항목 클릭 시 해당 타일 건설/비용 차감 확인 → MagicTower 해금 전후 확인 → 데이터 가격/목록 순서 수정 후 다음 플레이에서 표시/차감 동시 반영 및 중복 항목 없음 확인.
