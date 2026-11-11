# 유지 상호작용 충전 코어

`Afterlife.HoldInteraction`은 Unity 참조가 없는 독립 어셈블리다. 버튼을 놓아도 충전하고 완충에서 대기하는 게이지만 담당한다. 대상·작업 효과·HUD·씬·전역 서비스는 참조하지 않는다.

## API

- `new InteractionCharge(intervalSeconds)`: 양의 유한 주기를 지정한다.
- `Reset(fraction)`: 소비자가 판 시작 충전량을 0~1로 지정한다.
- `Advance(deltaSeconds)`: 게임 시간으로 충전한다. 완충을 넘긴 시간/작업을 쌓아두지 않는다.
- `TryConsume(held)`: 버튼 유지와 완충이 모두 맞으면 충전량을 0으로 초기화하고 `true`를 반환한다. 소비자는 유효한 작업 대상이 있을 때만 호출한다.
- `Fraction`, `RemainingSeconds`, `Ready`, `IntervalSeconds`: 읽기 전용 상태다. HUD 연결은 필수가 아니다.

## 적용·수명

게임의 입력 컴포넌트 하나가 코어 인스턴스를 소유하고, 판 시작 `Reset` → 게임 시간 `Advance` → 유효 대상의 유지 입력 `TryConsume` → 작업 실행 순으로 연결한다. UI/모달/포커스 차단과 비활성화에서는 유지 입력만 취소하고 충전량은 유지한다. 정지된 게임 시간에는 `Advance`를 호출하지 않는다. 판 초기화 때 다시 `Reset`하고 해제 시 인스턴스를 폐기한다. 이벤트 구독·필수 외부 참조·독립 GameObject는 없다.

본편은 기존 `PrototypeView` 컴포넌트와 `PrototypeManualWork` 어댑터가 기본 적용 경로다. 새 프리팹이나 직렬화 연결을 설치할 필요가 없다. 현재 설정은 1초·판 시작 100%이고 게이지는 HUD에 표시하지 않는다. [사용법·설계·컴파일 및 사용자 검증](../../../../../../docs/game-process/73-hold-interaction-gauge.md)을 따른다.
