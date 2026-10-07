# Threshold 화면 모듈

타이틀에서는 `ThresholdBackdropRig`이 자식 `ThresholdPortal`의 표면과 반사를 숨긴다. 인트로(`Lobby`) 진입 시 0.95초 카메라 이동의 SmoothStep 진행률에 맞춰 표시한다. `SetVisibility`로 표시 강도를 전달하며 원본 Lab의 Host 기반 처리는 유지한다. 기존 프리팹 연결로 적용되므로 재설치나 추가 참조가 필요 없다. 타이틀 복귀 시 강도를 0으로 초기화한다.

`Prefabs/ThresholdFlowPack.prefab`을 씬에 추가하면 타이틀·인트로·게임 오버·클리어 Canvas와 전용 3D 배경이 함께 배치된다. 게임별 어댑터에서 `ThresholdScreenDeck.Show(screen, resultSummary)`와 `ThresholdBackdropRig.Show(screen)`를 호출하고 `ThresholdScreenDeck.ActionRequested`를 게임 흐름에 연결한다. 배경 출력용 `RawImage`는 `ThresholdBackdropRig.Bind`로 한 번 지정한다.

배경 프리팹의 균열 표면은 `ThresholdBackdrop` 레이어로 촬영하고, 균열 안 마을은 원본 30번 레이어와 `VillageCamera`의 마스크를 유지한다. 배경 전체의 레이어나 모든 카메라 마스크를 일괄 변경하면 마을이 균열 밖으로 직접 그려질 수 있다.

화면별로 선택하려면 `ThresholdTitlePage`, `ThresholdLobbyPage`, `ThresholdGameOverPage`, `ThresholdClearPage` 프리팹을 Canvas에 추가한다. 각 루트의 `ThresholdScreenView`가 표시와 버튼 의도를 제공한다. 인트로는 `ThresholdIntroSequenceView`가 진행과 완료 이벤트를 처리한다. 이 프리팹은 원본 ThresholdLab 화면의 변형이므로 원본 샘플은 독립 실행을 유지한다.

게임 준비에 시간이 걸리면 `ThresholdBackdropRig.BeginPortalEntry()`를 호출한 뒤 `PortalEntryComplete`를 기다린다. 배경 리그와 ThresholdLab 원본은 같은 `ThresholdPresentationSystem.PortalApproach` 이동 계산을 사용하고, 배경 프리팹은 `ThresholdProfile.WorldTransitionSeconds`를 참조한다. 균열 앞 도착 시 원본처럼 암전한 다음 `ThresholdLoadingPage` 프리팹의 `ThresholdLoadingView.SetVisible(true)`를 먼저 렌더링하고 게임별 세션 시작을 호출한다. 준비가 끝나면 로딩 표시를 숨긴다. 이 순서는 프로토타입 어댑터가 구현하며 모듈은 게임 규칙과 맵 생성 방법을 알지 못한다.

기존 모달에는 `ThresholdModalFrame`을 추가해 프레임 스타일을 적용한다. 캡처 화면에 외곽 연출이 필요하면 `RawImage`에 `ThresholdScreenEdge`를 추가하고 WorldVeil 머티리얼을 지정한다. 화면 소유자에 `ThresholdModalEffect`를 추가해 원본 `ThresholdProfile`과 `ThresholdAudioBank`를 지정한 뒤, 모달 열림·닫힘을 `SetOpen(bool)`로 전달한다. 캡처 소유자는 매 프레임 `Present(texture, darkness, modalEffect.Amount)`를 호출하고 닫힘 애니메이션이 끝난 뒤 `Clear()`를 호출한다. 원본처럼 약 0.45초간 `_Modal`과 `_Blur`가 변하고 `WaterEnter`·`WaterExit` 및 반복 물소리가 재생된다. 효과 컴포넌트는 카메라나 게임 흐름을 변경하지 않는다.

설계, 프로토타입 연결, 수동 검증 순서는 저장소의 `docs/game-process/62-threshold-prototype-integration.md`에 기록한다. 관련 `.csproj`의 `dotnet build --no-restore`로 컴파일을 확인하며 Unity 플레이 모드 검증은 사용자가 수행한다.
