# 25 DayNight 해·달 이미지와 텍스트 연결

현재: M5 UI 편집 개선 / 이미지·연결 코드·컴파일 완료, 열린 Unity 임포트/씬 참조 연결 확인 완료, 사용자 표시 확인·씬 저장 대기. Windows 빌드는 하지 않는다.

## 작업 목록
- [x] imagegen으로 투명 배경 해/달 픽셀 아트 PNG 생성
- [x] 생성 이미지 프로젝트에 복사, Sprite 임포트 설정 코드
- [x] 열린 Unity 실제 임포트·참조 연결 확인
- [x] 사용자 DayNight/Icon Image 해/달 교체 코드
- [x] 기존 HUD.Day 대신 DayNight/Text TMP 갱신
- [x] 기존 UI 배치 유지, 열린 씬 참조 연결/편집 메뉴 지원
- [x] 컴파일·현재 증거·진행 문서 갱신

사용자가 편집한 DayTimeline/Group/DayNight를 사용한다. Icon Image는 기존 Image, Text는 기존 TMP다. 새 UI를 생성하지 않고 지정된 참조만 연결한다. 기존 HUD.Day/HUD.Sun/HUD.Moon은 비활성 상태를 유지한다.


## 생성 이미지
- Assets/_AfterlifePrototype/Resources/UI/DaySun-v1.png: 금색 8방향 햇살, 짙은 갈색 외곽선.
- Assets/_AfterlifePrototype/Resources/UI/NightMoon-v1.png: 아이보리/푸른 회색 초승달, 남색 외곽선.
- imagegen 내장 도구로 각각 생성. 둘 다 1254×1254 RGBA, 알파 범위 0~255 확인. 원본 알파를 유지하여 프로젝트로 복사했으며 이미지 후처리는 하지 않았다.
- 기본 import: Sprite Single, Point, mipmap 끔, 무압축, Alpha Is Transparency. Sprite 설정은 Unity API로 적용하고 .meta를 직접 만들거나 수정하지 않는다.

## 연결 구현
- PrototypeCanvas.DayNightIcon, DaySunSprite, NightMoonSprite 직렬화 참조 추가.
- 낮에는 해, 황혼/밤/결산에는 달을 기존 Image.sprite에서 교체. 기존 HUD.Day/HUD.Sun/HUD.Moon을 다시 켜지 않는다.
- UpdateUiTimeline은 DayNight.Text Binding에 기존 일차/낮밤/남은 시간·적 수를 기록한다. 사용자 Text 오브젝트는 TMP였으며 그대로 연결한다.
- 사용자 편집 계층은 Canvas/HUD/DayTimeline/Group/DayNight/Icon Image 및 Text. 기존 RectTransform/폰트/스타일을 유지한다. 이미지는 종횡비 유지, 텍스트/이미지 Raycast Target은 꺼서 HUD 입력 통과를 유지한다.
- 스크립트 reload 후 에디터가 편집 상태이고 연결이 빠져 있으면 해당 프로토타입 씬의 DayNight만 자동 연결. 플레이 상태에는 수정하지 않으며 편집 상태 복귀 시 다시 연결한다. 연결된 컴포넌트는 이름을 바꿔도 참조로 유지한다.
- Tools > Afterlife > Connect DayNight Icons and Text 메뉴도 제공. 열린 씬을 다시 열거나 저장하지 않고 Undo와 dirty 표시를 남긴다. 사용자가 결과를 보고 저장한다. 자동 연결이 실행되지 않았을 때 이 메뉴를 사용한다.
- 최초 빈 씬 UI 생성 경로도 DayNight 구조를 사용하도록 변경. 현재 편집 중인 씬에는 새 UI 오브젝트를 생성하지 않는다.

## 검증과 적용 상태
- C# dotnet build --no-restore 오류 0, 경고 0.
- 소스 PNG 두 장의 RGBA/투명 알파 확인. 해당 이미지들은 도구 결과에서 시각적으로 확인했다.
- 최종 점검에서 Unity가 생성한 PNG .meta 두 개와 Editor.log의 DayNight 적용 완료 로그를 확인했다. 열린 씬의 참조 연결은 완료되었으며, 시각 확인과 씬 저장은 사용자가 진행한다.
- 현재 TMP 폰트 에셋 Mona12TextKR SDF.asset은 존재하며 사용자 DayNight/Text에도 TMP Font Asset이 연결되어 있음을 씬에서 확인했다. 24의 이전 미생성 기록 이후 사용자 편집이 있었다.
- Windows 빌드/에디터 테스트/플레이 실행을 하지 않았다. 기존 Windows 실행 파일에는 이번 변경이 없다.

## 직접 확인
Unity에서 플레이 중이면 종료한 뒤 임포트/컴파일 완료를 기다린다. DayNight/Icon Image에 해 Sprite, PrototypeCanvas에 해/달 두 참조와 DayNight.Text Binding이 연결됐는지 확인한다. 누락되면 위 연결 메뉴 실행. 낮/황혼/밤에서 텍스트가 새 TMP에 갱신되고 같은 Image가 해/달로 바뀌는지 확인한 뒤 씬을 저장한다.
