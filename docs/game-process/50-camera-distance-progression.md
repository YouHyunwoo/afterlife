# 50 · 가까운 시작 카메라와 시간에 따른 거리 증가

> 2026-09-26: 이 문서는 시간 기반 구현의 기록이다. 현재 카메라는 집 영역 크기로 거리를 정한다. [현재 설정·구현·검증](51-territory-camera-distance.md).

2026-09-25. 기존 22.7 고정 거리에서, 가까이 시작한 뒤 플레이 시간에 따라 최대 거리까지 천천히 멀어지는 카메라로 변경했다.

## 인스펙터 설정

Project에서 `Assets/Prototype/Resources/AfterlifePrototype/SettingsData/Camera.asset`을 선택한다. 프로토타입의 `Settings.asset`이 이 에셋을 참조한다.

| 필드 | 기본값 | 의미 |
| --- | --- | --- |
| Camera Ground Distance | 16 | 새 게임·새 생 시작 거리 |
| Camera Maximum Ground Distance | 22.7 | 더 멀어지지 않는 임계 거리 |
| Camera Distance Per Second | 0.15 | 플레이 1초당 거리 증가량. 0은 정지 |
| Camera Prefab | 비어 있음 | 비어 있으면 Resources의 기본 PrototypeCamera 프리팹 사용. 다른 프리팹으로 대체 가능 |

거리는 수직 높이가 아니라 카메라 시선을 따라 기준 지면(y=0)까지의 거리다. 기존 각도를 유지해 시작 높이도 약 14.9에서 10.5로 낮아진다. 기본값에서는 약 44.7초 후 최대 거리에 도달한다. 직교 투영 크기도 거리 비율로 바뀌므로 실제 화면이 서서히 넓어진다.

인스펙터에서 최대 거리를 낮추면 다음 플레이 갱신에서 즉시 제한하고, 시작 거리를 바꾸면 해당 거리로 다시 시작한다. 최대 거리가 시작 거리보다 작으면 최대 거리를 우선한다. 유효 거리 최솟값은 1이다. 초당 속도를 0으로 바꾸면 현재 거리에서 멈춘다. 실행 중 에셋 변경은 저장될 수 있으므로 검증 뒤 원래 설정으로 되돌린다.

## 구조와 수명

- `Afterlife.CameraDistance`: `CameraDistanceProgression` 순수 C# 코어. 거리 상태·증가·상한·유효성 처리. `noEngineReferences` 사용.
- `Afterlife.CameraDistance.Unity`: `GroundCameraDistance` 컴포넌트. 시선의 지면 교점을 보존하며 Transform·직교 크기·far clip 반영. 게임 모델과 독립적이다.
- `PrototypeView`: 설정과 진행 시간을 전달하는 조립 계층. 카메라 프리팹을 생성하고 기존 회전·가장자리 이동·RenderTexture·오디오를 연결한다. 프리팹이 없는 레거시 설정은 카메라 생성 경로를 유지한다.
- `PrototypeCameraSettings`: 기존 ScriptableObject/GUID/시작 거리 필드를 유지하고 임계 거리·속도·프리팹 참조를 추가한다. Camera 자식 설정이 없는 레거시 설정은 기존 고정 거리를 유지한다.

게임 플레이 중에만 `Time.deltaTime`으로 진행한다. 타이틀·인트로·게임 종료, 정산/환생 상태, 게임을 일시정지하는 모달, 포커스 상실, 촬영 중에는 진행하지 않는다. 단순 UI 호버는 자동 거리 증가를 멈추지 않는다. 새 게임·새 생의 기존 월드 초기화 경로에서 거리도 초기화한다. 촬영은 거리 모델을 변경하지 않고, 촬영 종료 시 저장한 위치·회전·화면 크기에서 이어간다. 컴포넌트는 카메라와 함께 파괴되며 이벤트 구독이나 외부 해제 자원이 없다.

`Assets/Prototype/Resources/AfterlifePrototype/PrototypeCamera.prefab`은 프로젝트 카메라 구성을 재사용한다. 설정 참조가 없으면 Resources에서 기본 프리팹을 읽고, 그것도 없는 경우 기존 런타임 생성 경로를 사용한다. 기존 씬/프리팹의 사용자 편집을 덮어쓰지 않는다. `.meta`는 직접 생성/수정하지 않으며 Unity가 임포트 시 생성한다. 새 C# 모듈의 자동 생성 메타데이터는 확인했고, 새 프리팹 임포트는 사용자 에디터 확인 대상이다. 추가 Tools 메뉴는 없다.

## 검증

`dotnet build Afterlife.Prototype.csproj --no-restore --nologo -v minimal -p:SkipResolvePackageAssets=true -p:GenerateDependencyFile=false`로 새 코어·Unity 어셈블리와 프로토타입을 함께 컴파일했다. 경고 0, 오류 0. 새 Unity 생성 csproj의 `project.assets.json`이 없어 기본 명령은 NETSDK1004로 실패했으며, Unity가 제공하는 명시적 참조로 컴파일하도록 패키지 자산 조회/의존성 파일 생성만 비활성화했다. 패키지 복원은 하지 않았다.

프로젝트 지침에 따라 Unity Test Runner·EditMode·PlayMode·플레이 스모크 테스트는 실행하지 않았다. 사용자 확인 절차:

최종 프리팹 로딩 연결 후 `Afterlife.Prototype.Editor.csproj`도 같은 옵션으로 빌드하여 코어·Unity 표현·프로토타입·에디터 소비자까지 경고 0, 오류 0을 확인했다.

1. Unity 컴파일 완료 후 프로토타입 씬에서 새 게임을 시작한다. 가까운 구도에서 시작하고 약 45초 동안 서서히 멀어지다가 22.7에서 멈춰야 한다.
2. 최대 거리를 18로 바꾸고 충분히 기다린다. 시선 기준 지면 거리가 18을 넘지 않아야 한다. 12로 낮추면 시작 거리 16보다 낮은 상한 12가 우선되어야 한다.
3. 속도 0에서 정지하고, 양수로 돌리면 현재 거리부터 재개해야 한다. 메뉴/정산/포커스 상실 동안 멈추고 복귀하면 이어져야 한다.
4. 오른쪽 가장자리 이동과 가운데 드래그 회전 중 화면 중심이 튀지 않아야 한다. 새 게임·새 생은 시작 거리와 초기 방향으로 돌아와야 한다.
5. 촬영 시작/재촬영/종료 시 촬영 구도를 자동 거리 로직이 덮어쓰지 않고, 종료 후 저장된 구도에서 이어져야 한다.
6. 기본 PrototypeCamera 프리팹 임포트와 런타임 카메라의 `GroundCameraDistance` 컴포넌트를 확인한다. Camera 에셋의 Camera Prefab을 비워도 기본 프리팹을 사용해야 한다. 검증 뒤 거리 16/22.7, 속도 0.15를 복원한다.

루트와 Unity `AGENTS.md`에 기능 변경 시 문서 갱신, 독립 모듈, 사람이 읽기 쉬운 구조, 프리팹 우선, 로직/컴포넌트 분리 지침을 반영했다. 두 지침 파일은 기존 저장소 정책상 Git 추적 제외다.
