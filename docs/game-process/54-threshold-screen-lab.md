# 54 사후세계 화면 흐름 테스트 씬

> 2026-09-28 프로토타입 적용: [62 ThresholdLab 화면 통합](62-threshold-prototype-integration.md). 아래의 미적용 상태와 연결 절차는 독립 샘플 당시 기록이다.

> 최신 수정: [55 웅덩이·현실의 창](55-threshold-surface-refinement.md). 내부 균열선 제거, 외곽 잉크 번짐과 Day01 After, 0.95초 왕복 전환, 현장 유지 결과 화면을 적용한다. 아래 초기 구현 기록보다 55를 우선한다.

- 작성일: 2026-09-26
- 핵심 요약: 타이틀의 검은 웅덩이에서 카메라 이동으로 로비의 균열까지 이어지는 독립 화면 샘플을 구현했다. 빈 인게임·실패·통과·모달·수중 전환을 프리팹으로 조합한다.
- 상태: 독립 코드와 에셋 작성. 관련 C# 어셈블리 컴파일 확인. Unity 플레이 모드·Test Runner·인간 경험 검증은 하지 않았다.
- 기준: [53 세계관과 연출 방향](53-afterlife-world-and-screen-direction.md). 이번 후속 지시로 시험 마을은 **실제 원래 세계의 마을**로 확정했다.

## 작업 목록

- [x] 기존 Unity 규칙·화면 흐름·프리팹 구성 확인
- [x] 순수 상태와 Unity 컴포넌트·표현을 독립 어셈블리로 분리
- [x] 타이틀/로비/빈 인게임/게임 오버/클리어/모달 및 전환 코드 작성
- [x] 열린 에디터에서 별도 테스트 씬·프리팹·머티리얼 생성
- [x] 관련 C# 어셈블리 `dotnet build --no-restore`
- [x] 사용법·설계·수동 검증 절차 기록
- [ ] 사용자 화면 확인과 연출 조정
- [ ] 승인 후 기존 프로토타입에 연결

## 변경 전후와 범위

이전에는 세계관과 화면 방향만 있었고, 기존 프로토타입은 자체 Title/Intro/Playing/Clear/GameOver 흐름을 사용했다. 이번에는 기존 씬을 변경하지 않고 `Afterlife_ThresholdLab`을 추가했다. 기존 게임 로직·밸런스·세이브·빌드 씬 목록은 변경하지 않는다.

로비에 보이는 마을은 실제 세계를 바라본다는 설정을 전달하기 위한 임시 3D 아트다. 현재 플레이 중인 프로토타입의 마을과 연결된 것은 아니다. 빈 인게임에는 바닥·격자와 화면 테스트 버튼만 있으며 건설·전투·결과 통계를 구현하지 않는다.

## 열기와 확인 순서

Unity 프로젝트 기준:

`Assets/Afterlife/Samples/ThresholdLab/Scenes/Afterlife_ThresholdLab.unity`

1. 작업 중인 기존 씬을 저장하고 위 씬을 연다. Game 뷰를 우선 16:9로 설정하고 사용자가 Play를 누른다.
2. 타이틀: 검은 웅덩이의 희미한 반사와 가끔 생기는 얇은 동심원, 읽을 수 있는 로고·메뉴를 확인한다.
3. `새 게임`: 메뉴가 먼저 페이드 아웃되고 같은 공간 안에서 카메라가 균열 앞으로 이동해야 한다. 도중에 검은 전체 덮개나 씬 교체로 끊지 않는다.
4. 로비: 균열 안의 마을이 물처럼 일렁이고 바닥에 약한 반사가 보이는지 확인한다. 현재 균열은 각진 유리 파손 표현이다.
5. `시험 시작`: 어둠이 모인 뒤 중앙부터 가장자리로 걷히며 흐림이 풀리고 빈 인게임이 나타난다. UI는 전환 끝에 나타난다.
6. `설정 / 모달 보기` 또는 ESC: 화면 외곽 어둠과 배경 흐림, 선명한 모달을 확인한다. 음량·일렁임 줄이기 옵션은 실행 중에만 적용한다. ESC 또는 계속하기로 닫는다.
7. `테스트: 게임 오버` → 실패 화면 → 다시 도전/로비/타이틀을 각각 확인한다.
8. `테스트: 게임 클리어` → 통과 화면 → 타이틀/로비를 확인한다.
9. 전환 중 연속 클릭이 추가 전환을 만들지 않는지, 모달 뒤 버튼이 클릭되지 않는지, 반복 진입 후 소리가 겹치지 않는지 확인한다.

`종료`는 빌드에서 Application.Quit를 호출한다. 에디터 Play Mode 종료는 사용자가 에디터의 정지 버튼으로 수행한다. 테스트 버튼은 샘플 PlayingPage에만 있다.

## 에셋 구성

`Assets/Afterlife/Samples/ThresholdLab/`

- `Scenes/Afterlife_ThresholdLab.unity`: 전체 경험 프리팹 인스턴스 하나로 구성한 샘플 씬.
- `Prefabs/ThresholdExperience.prefab`: 조립 루트, Host, 카메라, 포즈, 캔버스, 오디오, EventSystem.
- `Prefabs/VoidEnvironment.prefab`: 웅덩이·실루엣·균열·반사·마을 촬영 카메라·임시 마을·빈 시험 바닥.
- `Prefabs/TitlePage.prefab`, `LobbyPage.prefab`, `PlayingPage.prefab`, `GameOverPage.prefab`, `ClearPage.prefab`, `ModalPage.prefab`: 화면별 편집 가능한 저장 UI.
- `Settings/ThresholdProfile.asset`: 이동·전환·모달 시간, 기본 음량, 움직임 줄이기.
- `Materials/`: 웅덩이·유리 균열·화면 어둠과 임시 월드 재질.

기본 UI는 1920×1080 기준 CanvasScaler를 사용한다. 초광폭·4:3·저해상도의 최종 배치, 한글 가독성과 화면 명도는 사용자 확인 대상이다. 샘플의 카메라/AudioListener/EventSystem은 독립 실행용이므로 다른 씬에 통합할 때 중복 인스턴스를 유지하지 않는다.

## 모듈 구조와 ECS 적용 범위

`Assets/Afterlife/Modules/Threshold/`는 기존 Prototype/Devlog에 의존하지 않는다. 이 모듈의 ECS는 GameObject를 엔티티로 사용하는 조합 방식이며 Unity Entities/DOTS 패키지를 새로 설치한 구조가 아니다.

| 계층 | 파일·컴포넌트 | 책임 |
|---|---|---|
| 순수 코어 | `Core/ThresholdSession.cs` | 화면 상태·허용 경로·전환 시간·중복 입력 차단·모달 상태. Unity 의존 없음. 기존 Afterlife.Flow 사용 |
| 조립 | `ThresholdHost` | Moonstone 세션 등록·수명, 컴포넌트 등록, 명령 전달, 시스템 Tick, 외부 연결 이벤트 |
| 화면 데이터 | `ThresholdPage` | 화면 ID/모달 여부/CanvasGroup |
| 카메라 데이터 | `ThresholdCameraPose` | 화면 ID와 Transform·FOV |
| 버튼 데이터 | `ThresholdActionButton` | 버튼 의도 enum. 부모 Host를 통해 전달 |
| 표현 시스템 | `ThresholdPresentationSystem` | 등록된 Page와 Pose 데이터를 읽어 페이드·입력·카메라 보간 처리 |
| 화면 연출 | `ThresholdVeil` | 지정 월드 카메라 RenderTexture·블러·화면 외곽 어둠. UI는 별도 선명 계층 |
| 세계 창 | `ThresholdPortal` | 지정 소스 카메라의 실시간 마을 영상·수면 같은 왜곡·반사 |
| 음향/옵션 | `ThresholdAmbience`, `ThresholdOptions` | 자체 합성 저음·필터 변화, 실행 중 볼륨·움직임 옵션 |
| 에디터 제작 | `Editor/ThresholdLabInstaller`, `ThresholdLabWorld`, `ThresholdLabUi` | 저장 에셋 제작만 담당. 게임 실행 중 UI를 새로 조립하지 않음 |

기존 화면 ID의 추가 표현은 Host 아래 Page/CameraPose/ActionButton 프리팹·컴포넌트를 넣으면 자동 등록된다. 런타임 활성/비활성 때 등록/해제한다. **완전히 새로운 화면 ID와 전환 규칙**은 순수 코어의 enum/전환표 추가가 필요하다. 임의의 게임 상태까지 컴포넌트만으로 생성하는 도구는 아니다.

### 다른 씬에 연결

1. 별도 빈 씬에서 전체 경험을 재사용하려면 `ThresholdExperience.prefab`을 배치한다.
2. 기존 게임에 연결할 때 Host와 필요한 UI/연출 프리팹을 배치하고 `ViewCamera`·포즈를 연결한다. 샘플 환경과 테스트 PlayingPage는 교체한다.
3. 마을 창은 `ThresholdPortal.SourceCamera`를 실제 마을 촬영 카메라로 교체한다. `Surface`와 `Reflection`은 원하는 메시 Renderer로 바꿀 수 있다.
4. `TrialStarted`에서 실제 게임 시작, `TrialStopped`에서 실제 게임 입력/진행 중단을 연결한다. 시작 이벤트는 전환 완료 후에만 발생한다.
5. 실제 패배/통과는 `ReportFailure()` / `ReportCompletion()`으로 전달한다. 외부 코드는 Session.Move를 직접 호출하지 않고 Host를 사용한다.
6. `GameplayInteractionChanged`는 모달/전환에 따른 입력 허용 신호다. 기존 게임 시간 정책은 별도 어댑터에서 결정하고 다른 씬의 Time.timeScale을 모듈이 임의 변경하지 않는다.
7. `QuitRequested`는 외부 종료 처리로 바꿀 수 있다. 샘플은 Host.ExitApplication을 연결했다.

새 장면의 포즈는 Host 자식으로 두고 화면마다 하나만 둔다. 같은 화면 ID의 Page는 여러 개 사용할 수 있다. 여러 겹 모달 스택은 이번 샘플 범위 밖이며 기존 프로토타입 스택에 연결하는 어댑터는 후속이다.

## 시청각 구현과 수명

- 타이틀→로비는 동일 카메라의 위치·회전·FOV를 보간한다. 기본 약 4.8초이며 앞부분은 메뉴 페이드, 뒷부분은 로비 UI 등장이다.
- 그 외 전환은 약 2.4초이며 완전히 어두워지는 구간에서 카메라를 목적지 포즈로 바꾸고 중앙부터 다시 드러낸다.
- 모달은 별도 전체 화면 배경을 추가하지 않고 동일 Veil의 외곽 농도를 높인다. 텍스트·버튼은 이 화면 처리 위에서 그린다.
- Portal은 삼각 분기 선·각진 외곽·부분 UV 어긋남으로 깨진 유리를 암시하고, 촬영 영상에 느린 물결 왜곡을 준다.
- 마을 프리뷰는 전용 레이어 30에 있고 월드 카메라에서 제외된다. 실제 통합 시 레이어 충돌을 확인한다.
- 음향은 코드로 생성한 임시 수중 분위기이며 외부 녹음·음원은 추가하지 않았다. 실제 물소리 녹음의 최종 사운드 디자인은 아니다.
- 세션·컨테이너·이벤트·RenderTexture·복제 Material·합성 AudioClip을 비활성화 시 정리하고 카메라의 기존 렌더 타깃을 복원한다. 공유 Profile은 실행 옵션으로 수정하지 않는다.

## 덧댄 듯한 균열 · 조사와 후속 제안

이번 구현은 사용자 지정대로 유리 균열을 유지한다. 다음 후보는 디자인 참고이며 구현했다고 주장하지 않는다.

1. **종이·막을 겹쳐 덧댄 가장자리**: Spider-Punk의 콜라주 제작에서 겹친 층의 그림자까지 검토했다는 제작자 인터뷰를 참고한다. 포털 자체의 사례가 아니라 경계의 재질 표현 참고다. Afterlife에서는 어두운 얇은 막 2~3장을 약간 어긋나게 겹쳐 현실의 조각을 붙여 놓은 인상으로 적용할 수 있다. [제작자 인터뷰](https://www.motionpictures.org/2023/08/spider-man-across-the-spider-verse-look-of-picture-supervisor-bret-st-clair-on-spider-punk-mumbattan-more-2/)
2. **잉크가 번지는 외곽**: The Spot의 FX는 손으로 그린 형태와 수채 시뮬레이션을 조합한다. 이를 참고해 내부 마을은 유지하고 가장자리만 번지고 수축하는 안이다. 과한 촉수·빠른 변형은 피한다. [FX 제작자 인터뷰](https://www.toonboom.com/behind-the-amazing-2dfx-in-spider-man-across-the-spider-verse)
3. **공간의 표면을 벌린 창**: His Dark Materials의 세계 간 창은 공중에 열린 다른 세계라는 개념 참고다. 유리 파편이 아닌 얇은 세계의 막을 젖힌 형태로 발전시킬 수 있다. [작품의 창 개요](https://hisdarkmaterials.fandom.com/wiki/Window), [VFX 제작사 자료](https://www.framestore.com/work/his-dark-materials)

추천은 1번의 겹친 외곽과 2번의 미세한 번짐을 결합하고, 중심은 지금의 물결 마을 영상을 유지하는 방향이다. 외곽을 교체해도 화면 흐름과 마을 촬영 구조를 재사용한다.

## 검증 기록과 한계

실행 명령: `& impls/unity/Afterlife/Tools/Threshold/Build-Threshold.ps1`

이 스크립트는 현재 Unity 생성 csproj의 참조를 이용해 전용 검증 csproj를 생성하고 다음 어셈블리를 `dotnet build --no-restore`로 컴파일한다. Unity 실행·테스트·패키지 복원을 하지 않는다.

- Afterlife.Flow: 오류 0 / 경고 0.
- Afterlife.Threshold.Core: 오류 0 / 경고 0.
- Afterlife.Threshold.Unity: 오류 0 / 경고 0.
- Afterlife.Threshold.Editor: 오류 0 / 경고 0.

설치 과정에서 모달 CanvasGroup 참조 누락을 수정했고, 셰이더에서 예약어 `line`을 변수로 사용한 문제도 수정했다. 이 에셋 작성/임포트 로그 확인은 플레이 검증과 구분한다.

Unity 메타데이터는 현재 열린 에디터가 생성했다. 기존 프로젝트를 batchmode로 추가 실행하지 않았고 Tools 메뉴·키보드 자동 입력도 추가하지 않았다. 최초 설치는 명시적 `Temp/threshold-lab.request`의 `create` 명령으로 열린 에디터에서 실행했다. Profile Inspector에도 별도 씬 생성 버튼이 있다. 기존 씬이 있으면 설치는 덮어쓰기를 거절하며, 작업 중이던 씬은 저장/교체하지 않는다.

최종 시각·음향 품질과 모든 화면 입력은 위 절차로 사용자가 확인해야 한다. Unity Play Mode/Test Runner를 실행하지 않는 프로젝트 규칙 때문에 실제 화면을 검증 완료로 표시하지 않는다.
