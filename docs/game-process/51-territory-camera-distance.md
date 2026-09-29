# 51 · 집 영역에 따라 멀어지는 카메라

2026-09-26. [50의 시간 기반 거리 증가](50-camera-distance-progression.md)를 집 영역 기반으로 변경했다. 집 영역에 변화가 없으면 오래 기다려도 카메라 목표 거리는 증가하지 않는다. 기존 시작 거리 16과 최대 거리 22.7은 유지한다.

## 인스펙터 설정

Project에서 `Assets/_AfterlifePrototype/Resources/AfterlifePrototype/SettingsData/Camera.asset`을 선택한다. 프로토타입 `Settings.asset`이 이 에셋을 참조한다.

| 필드 | 기본값 | 의미 |
| --- | --- | --- |
| Camera Ground Distance | 16 | 새 판·새 생의 시작 거리 |
| Camera Maximum Ground Distance | 22.7 | 카메라가 멀어질 수 있는 최대 거리 |
| Camera Expansion Tiles To Maximum | 120 | 초기 집 영역보다 이만큼 타일이 늘어나면 목표 거리가 최대에 도달 |
| Camera Distance Follow Speed | 3 | 영역 변화 후 목표 거리로 이동하는 초당 거리. 0은 현재 거리 유지 |

거리는 카메라의 시선을 따라 기준 지면(y=0)까지 잰다. 모델의 `Territory`는 완공된 집마다 `VillageRadius` 안에 들어오는 타일의 합집합이다. `Territory.Count`는 겹치는 타일을 한 번만 세므로 새 집·영역 강화로 실제 영역이 넓어진 만큼 반응한다. 비율은 `max(0, 현재 타일 수 - 초기 타일 수) / 120`을 0~1로 제한한다. 목표 거리는 16에서 22.7까지 이 비율로 보간한다. 집이 파괴되어 영역이 작아지면 목표 거리도 낮아진다. 한 판에서는 시작 타일 수를 고정하고, 새 판·새 생에서 다시 기록한다.

`CameraDistanceProgression` 순수 C# 코어가 타일 수를 목표 거리로 바꾼다. `GroundCameraDistance` Unity 컴포넌트는 기존 화면 중심·회전을 유지하면서 카메라 위치와 직교 크기를 반영한다. `PrototypeView`는 영역 타일 수와 설정을 전달하는 조립 계층이다. 카메라 설정에서 이전 `CameraDistancePerSecond` 필드를 제거했으며, 새 따라가기 속도는 영역이 변했을 때 목표 거리에 접근하는 데만 쓰인다.

## 씬·프리팹 구성

`Afterlife_PixelPrototype` 씬에서 고정 오브젝트를 직접 선택하고 참조를 확인한다.

```text
Afterlife_PixelPrototype
├─ Display Camera                    화면 표시용 기존 카메라
├─ World Camera                      PrototypeCamera.prefab 인스턴스
│  ├─ Camera / URP Camera Data
│  ├─ GroundCameraDistance
│  └─ AudioListener
├─ AudioRoot                         PrototypeAudioRoot.prefab 인스턴스
│  ├─ PrototypeAudio
│  ├─ WorldVoices                     일반 효과음 8채널
│  └─ FeedbackVoices                  중요 피드백 4채널
├─ Game Flow Screens                 별도 Canvas 프리팹 인스턴스
├─ Canvas                            기존 PrototypeCanvas 및 화면 요소
└─ GameRoot
   ├─ PrototypeScene / PrototypeView
   └─ WorldMarkers                   PrototypeWorldMarkers.prefab 인스턴스
      ├─ 범위·호버·설치·명령용 LineRenderer 7개
      └─ BuildTileMarker              MeshFilter / MeshRenderer
```

`GameRoot`의 `PrototypeView` 인스펙터에서 `World Camera`, `AudioRoot`, 7개 선 표시와 `BuildTileMarker` 참조를 확인한다. 카메라의 기본 위치·각도·투영 및 URP 설정은 카메라 프리팹에서, 거리 임계값과 따라가기 속도는 `Camera.asset`에서 관리한다. 대체 카메라를 쓰려면 씬의 `World Camera` 인스턴스를 교체하고 `PrototypeView` 참조를 다시 연결한다. 더 이상 카메라나 오디오 컴포넌트를 런타임에 생성하지 않는다.

Canvas의 기존 Day 배너, 버튼 62개, 빌드 패널과 모달 패널 11개에도 필요한 애니메이션·효과음·`CanvasGroup` 컴포넌트를 씬에 저장했다. 새 UI 버튼이나 모달을 추가할 때는 해당 컴포넌트도 에디터에서 배치한다. 실제 타일·유닛·효과 및 선택 대상 수에 따라 늘어나는 선은 런타임 생성한다. `RenderTexture`는 화면 비율이나 픽셀 높이가 바뀌면 크기를 다시 맞춰야 하므로 런타임에 생성·재생성하고 종료 시 해제한다.

게임 플레이 중에만 따라가기 시간을 전달한다. 정산·환생 상태, 일시정지 모달, 포커스 상실, 촬영 중에는 이동을 보류한다. 설정에서 최대 거리를 낮추면 즉시 상한을 적용하고, 시작 거리를 바꾸면 현재 타일 수를 새 기준으로 잡고 시작 거리로 돌아간다. 최대 거리가 시작 거리보다 작으면 최대 거리를 우선한다. 타일 임계값의 최소 유효값은 1이다. 촬영 중 카메라 제어는 촬영 도구가 소유하며, 촬영 종료 시 저장된 구도를 복원하는 기존 흐름을 유지한다.

## 검증

`Afterlife.Prototype.csproj`에 대해 `dotnet build --no-restore --nologo -v quiet -p:SkipResolvePackageAssets=true -p:GenerateDependencyFile=false`를 실행해 프로토타입 런타임을 컴파일했다. 경고 0, 오류 0. Unity CLI가 연결된 편집 모드에서 씬·프리팹을 저장하고 카메라, 오디오 12채널, 마커 및 UI 참조를 확인했다. Unity 에디터 테스트와 플레이 모드 검증은 저장소 지침에 따라 실행하지 않았다.

Unity에서 직접 확인할 항목:

1. 프로토타입 씬에서 새 게임을 시작하고 집을 추가하지 않은 채 1분 이상 기다린다. 카메라 거리가 16 근처에 머물러야 한다.
2. 집을 완공하거나 영역 강화를 적용해 집 영역 타일이 늘어나게 한다. 영역 증가에 맞춰 카메라가 부드럽게 멀어져야 한다. 추가 120타일 이상에서 목표 거리는 22.7을 넘지 않아야 한다.
3. 집을 파괴해 영역을 줄이거나 최대 거리를 18로 낮춘다. 카메라는 새 목표를 따라 가까워지고, 변경 직후에도 18을 넘지 않아야 한다.
4. Follow Speed를 0으로 놓고 영역을 넓힌다. 현재 거리에서 멈춰야 한다. 다시 3으로 바꾸면 영역 크기에 맞는 목표로 이동해야 한다.
5. 오른쪽 가장자리 이동·가운데 드래그 회전 중 화면 중심이 튀지 않아야 한다. 새 판·새 생에서는 거리 16과 새 초기 영역으로 돌아와야 한다. 촬영 시작/종료 시 촬영 구도가 자동 거리 로직으로 덮어써지지 않아야 한다.
6. 타이틀→게임→타이틀을 반복해도 `World Camera`, `AudioRoot`, `WorldMarkers`가 중복 생성되지 않아야 한다. 효과음이 들리고 Day 배너·비활성 버튼음·빌드 메뉴·모달 입력 차단이 유지되어야 한다. 창 크기를 바꾸면 월드 `RenderTexture`가 새 비율에 맞게 갱신되어야 한다.

검증 후 기본 설정 16 / 22.7 / 120 / 3으로 복원한다.
