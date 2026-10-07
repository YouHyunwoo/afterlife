# Mona12

공식 출처: https://github.com/MonadABXY/mona-font
배포 버전: 2026.08.16, MonaFont-ttf.zip / Mona/01 Main/Mona12.ttf
저작자: Monad ABXY. SIL Open Font License 1.1. 라이선스 원문과 구성 폰트 고지를 함께 보관한다.

프로토타입 UI와 기여 수치에 Resources/Fonts/Mona12를 사용한다. 일반 수치 24px, 치명타 36px. 폰트가 로드되지 않으면 기존 시스템 폰트로 대체한다.

## 폰트 표시 복구

런타임 로그에서 No Font Asset has been assigned 경고를 확인했다. 현재 사용하는 파일은 같은 공식 배포의 Mona/02 Text Only/Mona12TextKR.ttf이며 컬러 이모지 테이블이 없는 한국어 텍스트 전용본이다. GUI 스킨 스타일을 통째로 복사하지 않고 새 스타일에 폰트를 지정한다. 파일/기본 한글·숫자 글리프가 없으면 시스템 폰트로 대체한다. 실제 실행 확인은 별도로 필요하다.

## uGUI 전환 (19)

현재 UI는 UnityEngine.UI.Text를 사용하며, 씬의 각 Text.font가 Mona12TextKR.ttf 임포트 Font 에셋을 직접 참조한다. TextMeshPro Font Asset은 사용하지 않는다. Canvas의 PrototypeCanvas.Font에도 공통 참조를 저장한다. 기여 수치는 미리 저장된 Text 슬롯 96개로 표시하며 크기/색은 PrototypeCanvas Inspector에서 변경할 수 있다.
