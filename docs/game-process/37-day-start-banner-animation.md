# 37 · 하루 시작 배너 애니메이션

## 작업 목록
- [x] 기존 Day Start Banner와 하루 시작 신호 확인
- [x] 조절 가능한 PrototypeDayStartBanner 컴포넌트
- [x] 가로 → 세로 → 텍스트 → 유지 → 역재생
- [x] 기존/신규 씬의 컴포넌트 추가 경로
- [x] C# 컴파일 검증
- [ ] Unity 컴포넌트 추가·씬 저장·실제 화면 확인

기존 1초 활성/비활성 전환을 전용 컴포넌트 재생으로 교체했다. 시작은 화면 중앙에서 너비 0/높이 3, 텍스트 투명. 가로 0.2초 동안 부모 Canvas 전체 너비로 확장 → 높이 88까지 0.2초 확장 → 텍스트 0.2초 페이드 인 → 1초 유지 → 같은 타임라인 역재생 후 비활성화. 총 기본 시간 2.2초. 세로 및 페이드 0.2초는 사용자 미지정 부분의 기본값이다.

Day Start Banner의 PrototypeDayStartBanner에서 LineHeight, ExpandedHeight, ScreenWidthRatio, HorizontalSeconds, VerticalSeconds, TextFadeSeconds, HoldSeconds, ExpansionCurve를 조절한다. 크기는 Canvas 단위이며 변경되는 부모 너비를 매 프레임 반영한다. 시간을 0 이하로 입력해도 단계 시간은 최소 0.01초로 보정한다. Text와 TMP 모두 지원하고 텍스트 CanvasGroup으로 기존 글자 색/폰트를 유지하며 페이드한다. 시간은 unscaledDeltaTime을 사용한다.

새 하루가 시작되면 처음부터 재생, 낮 종료 시 즉시 숨김. 중간 비활성화 시 크기/알파를 정리한다. 이전 PrototypeCanvas.DayBannerSeconds는 직렬화 호환만 남기고 Inspector에서 숨겼다.

에디터 스크립트 임포트/편집 모드 복귀 시 컴포넌트가 없는 기존 배너에만 Undo 가능한 추가를 수행한다. 이미 존재하는 컴포넌트의 값은 덮어쓰지 않는다. 자동 씬 저장 없음. 필요 시 Tools > Afterlife > Add Day Start Banner Animation. 신규 Install Scene UI 경로에도 추가. 런타임에도 누락 컴포넌트를 보완해 기존 열린 씬에서 동작한다.

검증: C# dotnet build --no-restore 오류·경고 0. Unity 플레이/테스트 및 Windows 빌드는 하지 않았다. 직접 확인: 하루 시작 시 가로 확장이 끝난 후 세로 확장/텍스트 순서인지, 1초 유지 후 정확히 역순인지, 다음 날 재생되는지, 창 크기 변경 후 화면 너비에 맞는지 확인한다.
