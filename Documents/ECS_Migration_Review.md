# ECS Migration Review

## 구현된 내용

- 기존 `Assets/02_Scripts/Field` 기반 레거시 필드/오브젝트 구조를 제거하고 `Assets/02_Scripts/Game/ECS` 중심 구조로 전환했다.
- `Entity`, `EntityManager`, `Application`, `ISystem`, `IComponentManager` 등 ECS 실행 골격을 추가했다.
- `ComponentManager`를 Object, Agent, Citizen, Enemy, Building, Resource, Event, Global 영역으로 분리했다.
- `EntityFactory`가 시민, 적, 건물, 자원 프리팹을 생성하고 관련 컴포넌트를 등록하도록 구성했다.
- 이동, 배회, 탐지, 추적, 전투, 채취, 귀환, 건설, 체력, 선택, 안개, 이벤트, 웨이브 스케줄러 시스템을 신규 구조에 맞게 추가했다.
- `Bootstrapper`가 신규 ECS 애플리케이션을 구성하고 `Game.World` 프리팹 기반으로 필드를 생성하도록 변경했다.
- 기존 Dev.Object 프리팹/데이터 경로를 ECS.Object 및 평탄화된 데이터 경로로 이동했다.

## 불완전하거나 인지해야 할 부분

- `Bootstrapper.cs`에서 웨이브 스케줄 등록이 주석 처리되어 실제 웨이브가 시작되지 않는다.
- `Bootstrapper.cs`에서 초기 나무/바위 생성 루프가 주석 처리되어 `_initialTreeCount`, `_initialRockCount`, `_treeData`, `_rockData`가 사실상 미사용 상태다.
- `World.Despawn()`이 엔티티 위치가 아니라 `gameObject.transform.position`을 사용해 필드 점유 해제와 마을 영역 제거가 잘못될 가능성이 크다.
- `World.Despawn()`이 `_entities`에서 엔트리를 제거하지 않아 같은 id에 대한 stale entry가 남는다.
- `EntityFactory.TrySpawn()`이 `RouteSpawn()` 실패 여부를 무시하고 항상 성공 처리한다.
- `BuildingSystem`은 작업자 수 체크가 주석 처리되어 건물이 Preview 상태면 자동으로 완성된다.
- `HarvestSystem`, `ReturnSystem`에 TODO가 남아 있어 채취 도착 판정, 실패 처리, 채취 속도는 임시 구현이다.
- 선택/건설 모드에 개발용 `Debug.Log`가 남아 있어 플레이 중 로그가 출력된다.

## TODO

- [ ] 웨이브 스케줄 등록 복구 및 실제 스폰 흐름 확인
- [ ] 초기 나무/바위 생성 루프 복구 또는 의도적으로 제거
- [ ] `World.Despawn()`이 엔티티 위치를 기준으로 필드/마을 영역을 해제하도록 수정
- [ ] `World.Despawn()`에서 `_entities` 엔트리 제거
- [ ] `EntityFactory.TrySpawn()`이 `RouteSpawn()` 실패 시 생성 오브젝트/엔티티를 정리하고 `false` 반환
- [ ] `BuildingSystem`의 작업자 기반 건설 조건 재검토
- [ ] 채취 도착 판정, 실패 처리, 채취 속도 하드코딩 제거
- [ ] 개발용 `Debug.Log` 정리
- [ ] Field 씬에서 월드 생성, 집/시민/적 생성, 선택/건설 모드 전환 수동 확인

## 검증 메모

- 이번 검증은 빌드 실행보다 코드 리뷰성 확인에 초점을 두었다.
- 커밋 전에는 위 불완전 항목을 인지한 상태로 ECS 전환 중간 지점을 저장하는지 확인한다.
- 자동 빌드 검증은 이번 요청의 필수 기준이 아니었다.
