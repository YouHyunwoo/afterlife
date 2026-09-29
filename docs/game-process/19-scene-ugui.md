# 19 씬에 저장하는 uGUI 전환

> 현재 조작(2026-09-24): 영력을 써서 위치에 8초 유지되는 필드를 설치한다. 필드는 커서와 무관하게 범위 안 대상을 주기적으로 처리한다. 대상 클릭·빈 타일 Shift+클릭이 설치 입력이며 R8은 작업 간격을 줄인다. 아래의 과거 입력·QA 기록은 당시 버전 설명이다. [현재 구현·검증](47-divine-interaction-fields.md).


M5 구현·플레이 검증 반복. 사용자가 Inspector에서 UI를 직접 편집하기 위한 구조 전환.

## 작업 목록
- [x] Canvas / EventSystem / 고정 UI를 씬에 저장하는 편집기 설치 도구
- [x] HUD·건설·모달·전리품·툴팁·결과·기여 수치 uGUI 전환
- [x] 기존 모델 명령·ESC·카메라 UI 보호 연결
- [x] Mona12TextKR Font 직접 참조, 사용자 편집 유지
- [x] 컴파일·씬 저장·Windows 빌드·실행 화면 검증

기존은 빈 씬에 런타임 조립과 IMGUI 직접 그리기. 새 구조는 씬에 UI와 EventSystem을 저장하고 Runtime에서는 데이터와 상태만 갱신한다. 반복적인 목록/기여 텍스트는 편집기에 미리 슬롯을 만든다. 빌드할 때 기존 UI 배치와 스타일을 덮어쓰지 않는다.

## 변경 구조

```text
Afterlife_PixelPrototype
├─ GameRoot (PrototypeScene / PrototypeView)
├─ Canvas (CanvasScaler / GraphicRaycaster / PrototypeCanvas)
│  ├─ WorldDisplay (RawImage: 픽셀 월드 화면)
│  ├─ WorldFeedback (기여 수치 96개 슬롯)
│  ├─ HUD
│  │  ├─ Resources / Relics / DayTimeline
│  │  ├─ HUD.Build / HUD.Loot / HUD.Menu / HUD.Camera
│  │  └─ BuildPanel / Notice
│  ├─ ModalLayer
│  │  ├─ DimBackground
│  │  ├─ Market / Loot / Menu / Barracks / Research
│  │  ├─ Tower / Altar / RebirthShop / Settlement
│  │  └─ Result
│  ├─ Tooltip
│  └─ Selection
└─ EventSystem (InputSystemUIInputModule)
```

기존 OnGUI 및 IMGUI 그리기 코드를 제거했다. 게임 월드의 동적 모델과 카메라는 기존대로 게임 시작 시 구성하되 UI는 씬에 저장된 참조만 이용한다. WorldDisplay는 카메라의 픽셀 RenderTexture를 표시한다. 모델의 환전·생산·연구·환생 명령은 그대로 유지하고 uGUI 이벤트를 연결했다.

## Inspector에서 수정하기

1. `Assets/_AfterlifePrototype/Scenes/Afterlife_PixelPrototype.unity`를 연다.
2. Canvas 아래에서 수정할 UI를 선택한다. 위치/크기는 RectTransform, 글꼴/크기/색은 Text, 프레임/아이콘은 Image에서 바꾼다.
3. 기본 비활성 창은 ModalLayer와 원하는 창만 활성화해 편집한다. DimBackground는 필요하면 잠시 끈다. 편집을 마치면 원래처럼 비활성화해 저장한다. 플레이할 때 상태에 맞춰 표시 여부가 갱신된다.
4. 모달 본문은 `Body/Viewport/Content` 아래에 있다. ScrollRect와 Content의 높이로 스크롤 범위를 조정한다. 전리품과 유물은 LayoutGroup이 정렬한다.
5. `PrototypeCanvas.Bindings`의 ID는 모델 연결 키다. GameObject 이름은 변경해도 되지만 ID와 연결된 컴포넌트 참조는 유지한다. 고정 제목은 Text에서 편집하며 동적인 금액/설명/버튼 상태는 컨트롤러가 갱신한다.
6. 카메라 UI 접근 보호 범위는 `PrototypeCanvas.CameraGuards`의 RectTransform을 기준으로 계산한다. 창을 이동해도 보호 범위가 함께 이동한다. 정보 HUD와 월드 화면의 Raycast Target은 꺼져 있다.
7. `PrototypeCanvas`에서 기여 수치의 일반/치명타 크기와 일반/치명타/불/화상 색상을 수정할 수 있다. HUD 편집 기준은 1200×675이고 1920×1080에서 1.6배로 표시된다. CanvasScaler의 기준 해상도도 시작 시 값을 읽어 사용자 UI 배율에 적용한다.

## 폰트와 스프라이트

- `Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR.ttf`: Unity가 임포트한 **Font 에셋**을 각 Text에 직접 참조한다. TMP_FontAsset은 사용하지 않는다.
- 모든 Text에 같은 Font가 연결됐는지 실행 검증한다. 폰트를 전체 변경하려면 Text들을 다중 선택해 Font를 바꾸고 Canvas의 Font 참조도 함께 바꾼다.
- `Resources/UI/SceneSprites`: 기존 프레임/아이콘 텍스처를 참조하는 Sprite 에셋. 프레임은 9-slice이며 PNG를 다시 생성하거나 변경하지 않았다.
- 일반 빌드는 이미 저장된 UI를 재생성하지 않는다. `Tools/Afterlife/Install Scene UI`도 기존 Canvas가 있으면 보존한다. 이번 최초 전환에서 생성된 슬라이더·스크롤 앵커만 버전 1 보정 후 다시 건드리지 않는다.

## 검증 구분

독립 Windows 플레이어에서 화면과 uGUI 이벤트를 확인한다. Unity 에디터 PlayMode·Test Runner는 실행하지 않는다. 씬 저장은 에디터가 닫힌 상태에서 CLI로 수행했다. 최종 증거와 제한은 별도 결과 문서에 기록한다.

결과: [검증 기록](19-scene-ugui-qa.md).
