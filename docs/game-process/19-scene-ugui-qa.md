# 19 uGUI 전환 검증 결과

M5 — 이번 구조 전환 범위 완료. 전체 게임의 밸런스 검증이나 M6 출시 후보 완료 선언은 아니다.

- Windows 최종 빌드: 20260917-135310, Development Player, Unity 6000.0.69f1.
- 컴파일 및 Windows 빌드: 오류 0, 경고 0.
- Afterlife.Prototype.dll SHA-256: `40c6e9551f022d78560616763d1051cd2df611993c33f4dd94f7ef9364faf179`.
- [실제 화면 24장](qa/ugui/index.html), [자동 확인 23/23](qa/ugui/checks.txt).
- 씬에 Canvas / EventSystem / GameRoot 저장 확인. 모든 Text의 Font 직접 참조, 중복 없는 바인딩 ID, UI 전환 전후 RectTransform 수 동일 확인.
- 메뉴 열기/닫기, 슬라이더 환전, 모집/취소, 연구 및 탭, 타워 강화는 씬 Button/Slider 이벤트를 통해 검증했다.
- 적 시야/보상 기존 규칙, UI 주변 카메라 정지와 영역 밖 이동, 정보 HUD 월드 클릭 통과 확인.
- 빌드 전 백업한 Graphics/Quality/Project/Build/URP 설정 5개가 복원된 것을 해시로 확인했다.

## 수정·검증 과정

Unity 실제 빌드에서 편집기 Input System 어셈블리 참조 누락을 발견해 수정했다. 첫 uGUI 화면에서 Slider fill/handle과 Scrollbar handle의 앵커·sizeDelta가 중복 적용되는 것을 발견해 보정했다. 초기 월드 클릭 통과 확인은 종료 화면 비활성화가 반영되기 전에 조회해서 실패했다. 프레임 종료 후 조회하도록 검증 시점을 바로잡았고 최종 통과했다. 생산 코드의 광범위 입력 차단을 추가하는 방식으로 우회하지 않았다.

IMGUI OnGUI/GUI 그리기 경로는 제거됐다. 씬 설치 도구는 기존 Canvas가 있으면 보존하며 최초 전환 컨트롤의 앵커만 일회성 버전 보정한다. 사용자 RectTransform 편집은 런타임에서 고정 좌표로 재설정하지 않는다. 진행 마커·동적 텍스트·슬라이더 수치 등 상태에 따른 변화만 갱신한다.

## 제한

실제 Win32 마우스/ESC 입력도 시도했지만 `SetForegroundWindow=false`, 다른 프로세스가 전경이라는 결과를 확인했다. 따라서 이 입력 시도는 UI 검증 성공으로 세지 않는다. 자동 이벤트 확인은 하드웨어 클릭의 대체 증거로 주장하지 않는다. 일반 플레이어를 직접 열어 건설/전리품 버튼, 교역소 슬라이더와 환전, ESC 닫기, 스크롤을 확인하면 된다.

이번 최종 로그에서 실행 예외/오류는 검출되지 않았다. 게임 전체 판의 난이도·경제·환생 효용 검증은 별도이며, UI 미술의 세부 조정은 [씬 편집 안내](19-scene-ugui.md)에 따라 Inspector에서 진행할 수 있다.
