# 62 · ThresholdLab 화면을 프로토타입에 적용

- 날짜: 2026-09-28
- 작업 목록: 기존 흐름·모달 조사 → 화면·배경·가장자리 공용화 → 프로토타입 어댑터·프리팹 연결 → C# 빌드 → 사용자 화면 확인.
- Unity 플레이 모드와 Test Runner는 저장소 규칙에 따라 실행하지 않는다.

## 변경 전후

프로토타입은 단일 `Game Flow Screens` 프리팹에서 타이틀·한 장짜리 인트로·승패 화면의 문구만 바꿨다. 이제 `ThresholdFlowPack`이 ThresholdLab의 타이틀, 다섯 줄 인트로, 로딩, 게임 오버, 클리어 UI와 3D 배경을 표시한다. 기존 `PrototypeGameFlow`와 모델의 승패 판정, 결과 수치, 영구 진행 데이터는 그대로 사용한다. 원래 화면 프리팹 인스턴스는 씬에 비활성 상태로 보존했다.

게임 오버의 `다시 도전`은 `GameOver → Playing` 전환으로 모델을 새 판으로 초기화한다. 두 결과 화면의 타이틀 버튼은 기존 복귀 경로를 사용한다. 타이틀의 설정은 프로토타입 설정 항목을 보여 주고, 종료는 빌드에서 `Application.Quit`을 호출한다.

## 재사용 모듈과 적용 방법

| 구성 | 책임 | 다른 씬에 적용 |
| --- | --- | --- |
| `ThresholdTitlePage`, `ThresholdLobbyPage`, `ThresholdGameOverPage`, `ThresholdClearPage` 프리팹 | 화면 구성과 버튼 의도. 원본 Lab 화면의 프리팹 변형 | Canvas에 필요한 프리팹을 추가하고 `ThresholdScreenView.Show` 및 `ActionRequested`를 연결 |
| `ThresholdIntroSequenceView` | 인트로 진행·음성·진행 표식·클릭/Enter/Esc 입력. `ThresholdIntroTimeline` 사용 | 인트로 프리팹을 추가하고 완료 이벤트를 게임 흐름에 연결 |
| `ThresholdBackdropRig` 프리팹 | Lab 3D 공간 전용 카메라·포즈·RenderTexture 수명 | 프리팹 추가 후 표시할 `RawImage`를 `Bind`하고 화면 상태를 `Show`로 전달 |
| `ThresholdLoadingPage` 프리팹 | 동기 게임 준비 중 화면을 가리는 재사용 가능한 로딩 표시 | Canvas의 최상단에 추가하고 `ThresholdLoadingView.SetVisible`로 표시 |
| `ThresholdFlowPack` 프리팹 | 위 화면·배경·Canvas를 묶은 기본 구성 | 씬에 프리팹 하나를 추가하고 게임별 어댑터만 연결 |
| `ThresholdModalFrame` | 기존 모달의 프레임·헤더 색 적용. 내용·버튼 동작에는 관여하지 않음 | 원하는 모달 루트에 컴포넌트 추가 |
| `ThresholdScreenEdge` | 이미 캡처한 화면에 Lab의 외곽 연출 적용. 카메라나 캡처를 소유하지 않음 | 캡처 `RawImage`에 컴포넌트를 추가하고 머티리얼을 지정 |
| `ThresholdModalEffect` | Lab의 0.45초 모달 강도 변화와 물 진입·퇴장·반복 효과음 | 화면 또는 모달 배경 소유 오브젝트에 컴포넌트를 추가하고 원본 프로필·오디오 뱅크를 지정 |

공용 컴포넌트는 프로토타입의 `GameScreen`, 모달 ID, 모델을 참조하지 않는다. `PrototypeFlowScreen`이 화면과 버튼 의도를 `PrototypeGameFlow`에 맞춰 변환한다. `ThresholdBackdropRig`은 `ThresholdBackdrop` 전용 레이어만 촬영하며 프로토타입 월드 카메라의 `targetTexture`를 바꾸지 않는다. 균열 안의 마을은 원본 프리팹의 30번 레이어와 `VillageCamera` 마스크를 유지해 균열 표면을 통해서만 보인다. 게임 화면에서는 Lab 배경을 끄고 기존 픽셀 월드 출력으로 돌아간다. RenderTexture와 복제 머티리얼은 해당 컴포넌트가 정리한다.

`Assets/Afterlife/Modules/Threshold/Prefabs/`의 개별 화면·배경·묶음 프리팹은 편집할 수 있다. 현재 프로토타입 씬에는 묶음 프리팹과 `PrototypeFlowScreen` 어댑터가 연결되어 있다. 재구성이 필요하면 저장된 프로토타입 씬을 연 뒤 `PrototypeThresholdAuthoring.Apply()`를 **현재 열린 에디터**에서 호출한다. 이 에디터 유틸리티는 프리팹과 씬 참조를 다시 연결하며 새 `Tools` 메뉴는 만들지 않는다. 재실행 전 씬의 다른 변경을 저장한다.

인트로가 끝나면 `PrototypeFlowScreen`이 로비 문구를 숨기고 `ThresholdBackdropRig.BeginPortalEntry()`를 호출한다. 배경 리그는 ThresholdLab 원본 `ThresholdPresentationSystem.PortalApproach`를 함께 사용하며 샘플 `ThresholdProfile.WorldTransitionSeconds`를 따른다. 현재 1.8초 설정에서 진행률 0.38까지 약 0.68초 동안 원본의 3제곱 가속, 안전 거리, 카메라 회전, 시야각 68도 이동을 적용한다. 표면 직전 도착 순간 원본과 같이 완전히 검게 전환하고, 진행률 0.48까지 약 0.18초 유지한 후 `ThresholdLoadingPage`를 한 프레임 표시하고 `PrototypeGameFlow`의 세션 시작을 호출한다. 맵 생성은 현재 메인 스레드에서 동기 실행되므로 로딩 중 진행률을 표시하지 않는다. 재도전은 균열 이동 없이 같은 로딩 화면을 거친다. `ModelReset`, `Ground`, `Decorations`, `Vegetation`, `Objects` 단계의 `[Prototype.Startup]` 로그로 실제 비용을 비교한다.

## 모달과 설정

전리품·설정·정산에만 `ThresholdModalFrame`을 붙였다. 정보, 탭, 선택, 닫기, 모달 스택은 기존 프로토타입 컴포넌트가 맡는다. 나머지 모달은 현재 외형을 유지한다.

`PrototypeModalSurface.EdgeBackground`는 모달별 Inspector 스위치다. 최상단 모달에서 켜져 있으면 `PrototypeModalBackdrop`이 기존 캡처를 `ThresholdScreenEdge`로 표시한다. `ThresholdModalEffect`는 원본 프로필의 0.45초 동안 모달 강도를 올리고 내리며, 베일의 `_Modal`과 `_Blur=강도×0.65`에 전달한다. 열림·닫힘에는 원본 오디오 뱅크의 `WaterEnter`·`WaterExit`을 재생하고 반복 물소리를 강도에 맞춰 조절한다. 닫힐 때는 가장자리 애니메이션이 끝날 때까지 모달 배경을 유지한다. 기본값은 **모든 창형 모달과 설정에서 켜짐**이다. 설정의 기존 배경 블러는 꺼져 있고, 다른 모달의 기존 암전 누적과 쌓임 순서는 유지한다. 다른 전체 화면 표면은 가장자리 연출을 자동 적용하지 않는다. 타이틀 설정도 배경 리그의 같은 강도·효과음을 사용한다. 같은 설정 항목의 별도 프리팹을 사용하며 UI 크기·카메라 이동 구간·속도를 같은 PlayerPrefs 키에 저장한다. 게임 시작 시 이를 다시 읽는다.

## 검증 절차

### 2026-09-29 균열 표시 보정

기존 프로토타입은 `ThresholdHost`가 없어 균열이 항상 보였다. `ThresholdBackdropRig`이 자식 균열을 자동 탐색하고 `ThresholdPortal.SetVisibility`로 표면과 반사 강도를 전달하도록 수정했다. 타이틀에서 즉시 숨기고 인트로에서는 기존 0.95초 카메라 이동에 맞춰 SmoothStep으로 표시한다. 원본 Lab의 상태 처리는 유지하며 기존 프리팹에 자동 적용된다.

사용자 확인: `Afterlife_PixelPrototype`에서 Play 후 타이틀과 타이틀 설정에 균열·반사가 없는지 확인한다. 새 게임을 누르면 약 0.95초 동안 서서히 나타나야 한다. 인트로 완료 후 균열 진입과 결과 화면→타이틀→새 게임 반복 시 숨김·등장이 유지되는지 확인한다. Unity 플레이 검증은 실행하지 않았다.

이번 기본 `Afterlife.Threshold.Unity.csproj`의 `dotnet build --no-restore`는 `project.assets.json` 누락(NETSDK1004)으로 실행되지 않았다.
기존 `ThresholdValidation.csproj`를 사용해 Flow → Threshold.Core → Threshold.Unity 순서로 `dotnet build --no-restore`를 수행했다. 모두 오류 0으로 성공했으며 Unity 프로젝트에는 `Mono.Cecil` 참조 누락 경고 1개가 남았다.

1. Unity 에디터에서 `Afterlife_PixelPrototype` 씬의 `ThresholdFlowPack`과 `GameRoot/PrototypeView.Flow Screen` 참조, 세 모달의 `ThresholdModalFrame`, 창형 모달과 설정의 `EdgeBackground=true` 및 설정의 `BlurBackground=false`를 확인한다. 균열 표면은 `ThresholdBackdrop` 레이어, 내부 마을은 30번 레이어, `VillageCamera` 마스크는 30번 레이어인지 확인한다.
2. 사용자가 Play를 실행해 타이틀 3D 배경과 새 게임·설정·종료 버튼을 확인한다. 설정에서 값을 바꾼 뒤 게임을 시작해 값이 반영되는지 확인한다. 에디터에서는 종료 버튼이 Play를 멈추지 않는다.
3. 인트로에서 균열과 그 안쪽 마을이 보이는지 확인한다. 다섯 줄을 클릭과 Enter로 진행하고 Esc 건너뛰기를 확인한다. 마지막 입력 뒤 ThresholdLab 원본처럼 가속하며 균열 표면 직전까지 이동하고, 도착 순간 암전된 뒤에만 `마을을 준비하고 있습니다`가 맵 생성 동안 표시되어야 한다. 카메라는 표면을 통과하지 않아야 한다. 진입 클릭이 첫 문장을 즉시 넘기지 않아야 한다.
4. 게임에서 전리품·설정·정산과 다른 창형 모달을 열어 화면 가장자리가 약 0.45초 동안 안쪽으로 들어오고 닫을 때 되돌아가는지 확인한다. 열림·닫힘 물 효과음과 반복 물소리, 내용과 기존 조작을 확인한다. 설정에서는 기존 배경 블러 대신 Lab 베일의 블러만 적용되어야 한다. 타이틀 설정에서도 같은 열림·닫힘 연출과 효과음을 확인한다. 다른 전체 화면 표면은 기존 배경 동작을 유지한다.
5. 패배 후 결과 수치와 재도전 초기화, 승리 후 결과 수치와 타이틀 복귀를 확인한다. 재도전은 균열 이동이나 인트로 없이 로딩 화면을 거쳐 새 판으로 진입해야 한다.
6. 16:9와 다른 화면 비율, 반복 진입·복귀에서 화면 늘어짐, UI 클릭 관통, 카메라 렌더 타깃 복원, 중복 이벤트와 누수 여부를 확인한다.

위 플레이 확인은 자동으로 실행하지 않았다. `Afterlife.Threshold.Core`, `Afterlife.Threshold.Unity`, `Afterlife.Threshold.Editor`, `Afterlife.Prototype`, `Afterlife.Prototype.Editor`의 `dotnet build --no-restore`가 모두 오류·경고 0으로 완료됐다. Unity 에디터의 저장된 씬에서 새 흐름 참조, 화면 네 개와 로딩 프리팹, 인트로 다섯 줄, 균열 안쪽 마을의 분리된 레이어와 카메라 마스크, 창형 모달과 설정의 가장자리 스위치, 두 화면 소유자의 원본 프로필·오디오 뱅크와 세 물소리 클립 참조를 읽기 전용으로 확인했다.
