# 27 삭제된 UI 참조 예외 수정

## 작업 목록
- [x] 제공된 스택과 바인딩 접근 경로 확인
- [x] Show/Contains/Guards의 파괴된 Unity 참조 검사
- [x] 바인딩의 빈 항목과 Raycast 결과의 삭제된 오브젝트 검사
- [x] dotnet build --no-restore 검증

## 변경 전후
이전에는 PrototypeCanvas.Show가 바인딩 객체만 검사하고 Rect.gameObject에 접근했다. 바인딩은 일반 C# 객체이므로 연결된 RectTransform이 삭제되어도 남아 있을 수 있다. UpdateUiTimeline의 이전 HUD.Sun/HUD.Moon/HUD.Day 숨김 요청이 이 상태의 참조에 접근하면서 MissingReferenceException이 발생했다.

이제 Show와 Contains는 Rect를 Unity의 null 비교로 검사한 뒤 접근한다. CameraGuards에 삭제된 RectTransform이 남아 있는 경우도 건너뛴다. Canvas가 없으면 카메라 보호 영역 판정은 false를 반환한다. Bind는 빈 항목/ID를 건너뛰고 Blocks는 삭제된 Raycast 대상에 접근하지 않는다. 기존 유효한 UI 표시·숨김·영역 판정 동작과 씬 배치는 유지한다.

## 검증
Build-Prototype.ps1에서 dotnet build --no-restore 성공, 오류 0/경고 0. AGENTS.md에 따라 Unity 에디터 테스트와 플레이 실행은 하지 않았다.

사용자 확인: Unity 컴파일 완료 후 Console을 비우고 오류가 발생했던 씬을 다시 실행한다. 낮/밤 HUD 갱신과 UI 주변 마우스 이동 시 해당 MissingReferenceException이 발생하지 않고 현재 DayNight 표시가 계속 갱신되어야 한다.
