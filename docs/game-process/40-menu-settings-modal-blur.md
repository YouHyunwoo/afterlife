# 40 · 게임 메뉴/설정 화면 분리와 모달 블러

## 작업 목록
- [x] 게임 메뉴를 계속하기/다시하기/포기하기 버튼 목록으로 변경하는 마이그레이션
- [x] Header 설정 진입 버튼과 전체 화면 설정, 화면/조작 탭
- [x] 기존 UI 크기/카메라 구간/속도 슬라이더와 저장 로직 연결
- [x] 모달창/모달화면 분류 컴포넌트
- [x] HUD 포함 배경 캡처와 셰이더 블러, 조절 파라미터
- [x] 모달 중 시뮬레이션 일시정지 및 리소스 정리
- [x] C# 컴파일 검증
- [ ] Unity 실제 씬 적용/셰이더 임포트/화면 확인

## 사용자 요청과 구현
ModalLayer/Menu의 설정을 분리한다. 게임 메뉴에는 계속하기, 다시하기, 포기하기 세 버튼만 둔다. 계속하기는 닫기, 다시하기는 기존 Reset 경로. 포기하기는 타이틀 씬이 없으므로 기본 비활성이고, PrototypeCanvas.TitleSceneName에 Build Settings에 등록한 타이틀 씬 이름을 지정하면 해당 씬으로 이동한다. Application.Quit으로 연결하지 않는다.

설정은 Header/Menu의 별도 설정 버튼으로 연다. ModalLayer/Settings는 전체 화면이며 Header에 화면/조작 탭과 닫기 버튼, 아래 Tab Pages에 해당 탭 내용을 표시한다. 화면 탭에는 UI 크기, 조작 탭에는 카메라 이동 구간/속도 및 조작 안내를 넣었다. 기존 슬라이더를 옮기고 UI 표시 문구는 TMP로 구성한다. Menu.Size/Edge/Speed 바인딩 ID와 PlayerPrefs 저장은 호환성을 위해 유지한다. ESC 또는 닫기로 게임에 복귀한다.

PrototypeModalSurface.Presentation: Window는 기존 창 배치 사용, Screen은 모달 레이어 전체로 stretch. 기존 모달은 Window, Settings는 Screen. UI 크기를 바꿔도 Settings 루트는 전체 화면을 채운다.

## 배경 블러
Canvas의 PrototypeModalBackdrop에 Mode(Dark/DarkBlur), Darkness(0~1, 기본 .6), BlurRadius(0~24, 기본 8), Downsample(1~4, 기본 2)가 있다. ModalLayer의 가장 아래 DimBackground를 RawImage로 바꾸고 공통 배경으로 사용한다.

모달 최초 열기 시 레이어를 한 프레임 투명하게 유지하고 프레임 끝에서 ScreenCapture.CaptureScreenshotIntoRenderTexture로 월드와 HUD 전체를 캡처한다. 그 후 `Resources/UI/ModalBackdropBlur.shader`로 가로/세로 분리 9탭 가우시안 블러 및 어두움 처리를 하고, 팝업은 선명하게 그 위에 표시한다. 모달 자체를 캡처하지 않는다. 모달 간 전환/탭 전환은 같은 캡처를 재사용하고, 강도 변경 때만 다시 블러 처리한다. 닫기/비활성화/종료 시 코루틴과 RenderTexture를 정리하고 material은 종료 시 해제한다. 셰이더 미지원/누락 시 어두운 반투명으로 폴백한다.

배경은 정지 화면이므로 메뉴뿐 아니라 모달이 열린 동안 시뮬레이션을 멈추도록 통일했다. 수동 업그레이드/선택 버튼은 계속 동작한다. 캡처 방식상 최초 표시는 한 렌더 프레임 뒤에 나타난다. 실제 게임 화면의 색공간/상하 방향/해상도 변경은 플레이에서 확인해야 한다.

## 에디터 적용과 보존
PrototypeSettingsModalInstaller가 스크립트 임포트/편집 모드 복귀/씬 열기 시 아직 적용되지 않은 씬만 변경한다. 기존 슬라이더 이동 및 바인딩 갱신 후 기존 게임 메뉴를 새 세 버튼 메뉴로 교체한다. 전체 작업 Undo 가능, 실패 시 해당 작업 롤백. ModalSettingsVersion=1 이후 자동 덮어쓰기 없음. 씬 자동 저장하지 않는다. 수동 Tools > Afterlife > Separate Settings Modal. 신규 Install Scene UI 경로에도 연결.

## 검증과 직접 확인
관련 C# dotnet build --no-restore 오류·경고 0. Unity 플레이/테스트/셰이더 실제 실행/Windows 빌드는 미실시.

임포트 후 씬 저장 전 확인: 메뉴 세 버튼, Header 설정 버튼, Settings 전체 화면/두 탭, Canvas 배경 컴포넌트 참조. 플레이 확인: ESC 메뉴와 계속하기/다시하기, 타이틀 미설정 시 포기하기 비활성, 설정 탭/슬라이더 값 저장, UI 크기 변경 후 전체 화면 유지, 월드+HUD만 블러되고 팝업 텍스트는 선명함, 모달 중 낮밤/적 진행 정지, 닫기 즉시 재개, 여러 번 열고 닫아도 검은 화면/잔상 없음.
