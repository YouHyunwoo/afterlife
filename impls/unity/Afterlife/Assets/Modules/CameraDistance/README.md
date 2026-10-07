# 카메라 거리 모듈

`Core`는 Unity 참조가 없는 `Afterlife.CameraDistance` 어셈블리다. `CameraDistanceProgression`이 마을 영역의 타일 증가량을 목표 거리로 바꾸고 최대 거리를 지킨다. `Reset`은 새 판/새 생의 시작 거리와 초기 영역 타일 수를 기록한다. `FollowTerritory`는 현재 타일 수에서 목표 거리를 구하고 초당 따라가는 속도로 부드럽게 이동한다. 영역이 변하지 않으면 목표 거리도 변하지 않으며, 영역이 줄면 목표 거리도 낮아진다. 상한이 시작 거리보다 작으면 상한을 우선한다. 잘못된 거리(NaN/무한대)는 기본값, 1 미만 거리는 1, 잘못된 시간/속도는 진행 없음으로 처리한다.

`Runtime`의 `Afterlife.CameraDistance.Unity`는 코어만 참조한다. `GroundCameraDistance`는 현재 시선과 기준 지면의 교점을 유지하며 위치와 직교 카메라 크기를 반영한다. 자체 `Update`나 게임 전역 참조, 이벤트 구독은 없다. 수명은 카메라 오브젝트와 같으며 별도 해제 자원은 없다.

프로토타입 조립 계층은 `VillageModel.Territory.Count`와 `PrototypeCameraSettings`의 시작 거리·최대 거리·최대 거리 도달에 필요한 추가 타일 수·따라가는 속도를 전달한다. 진행 시간은 플레이/일시정지/촬영 상태에 따라 허용한다. 기본 카메라 프리팹은 `Assets/Prototype/Resources/AfterlifePrototype/PrototypeCamera.prefab`이고 `Camera`와 `GroundCameraDistance`를 조합한다. 설정의 `CameraPrefab`으로 대체할 수 있다. 새 씬에서도 같은 컴포넌트를 재사용할 수 있다. 게임 규칙과 입력 처리를 이 모듈에 넣지 않는다.

현재 사용법과 수동 검증: 저장소 루트 `docs/game-process/51-territory-camera-distance.md`. 이전 시간 기반 구현 기록은 `50-camera-distance-progression.md`.
