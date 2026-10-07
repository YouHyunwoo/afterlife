# 개별 충전 슬롯

Unity 비의존 `RechargeSlotService`에 용량·충전 시간을 전달한다. 행동이 성공한 뒤 `TryConsume()`으로 왼쪽 준비 슬롯을 사용하고 게임 시간에만 `Tick(dt)`을 호출한다. 슬롯 수는 활성 오브젝트 수의 제한이 아니다.

`Configure(capacity, seconds)`는 새 슬롯을 준비 상태로 추가하고 기존 충전 완료 비율을 보존한다. `Reset()`은 현재 용량을 준비 상태로 되돌린다. 새 판 시작에는 초기 용량으로 Configure 후 Reset한다. `Changed`는 상태 변화, `Recharged(index)`는 충전 완료 알림이다. 슬롯 목록은 읽기 전용이다.

표현은 별도 `Afterlife.RechargeSlots.Unity` 어셈블리다. `Assets/Prototype/Resources/AfterlifePrototype/FieldSlotsHud.prefab`을 Canvas 아래 한 번 배치하고 게임 어댑터에서 `RechargeSlotsHud.Bind(service)`를 호출한다. 자식 참조 자동 탐색·구독 해제는 컴포넌트가 담당한다. 프리팹은 최대 3슬롯, 48×6 바·10 간격이다. 제거는 Bind(null) 후 프리팹 인스턴스를 해제한다.

검증은 `Tools/AfterlifePrototype/Build-ScriptableData.ps1`의 슬롯 코어·Unity 어셈블리 `dotnet build --no-restore`로 수행한다. 슬롯 사용·개별 충전·변경 중 비율 보존·정지·초기화·점멸은 `docs/game-process/63-mvp-slots-and-auto-skills.md`의 직접 확인 절차를 따른다.
