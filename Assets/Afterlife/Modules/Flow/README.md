# 재사용 흐름 모듈

`Reusable.Flow`는 Unity, Afterlife, UI, 씬, DI 컨테이너에 의존하지 않는 동기 상태 머신이다. 화면 흐름, 작업 단계, 메뉴 흐름 등에 사용할 수 있다. `FlowMachine.cs`와 `Reusable.Flow.asmdef`가 모듈 전체이며 다른 Afterlife 모듈은 필요 없다.

## 다른 프로젝트에서 사용

Unity에서는 이 폴더를 복사하고 사용하는 어셈블리의 asmdef references에 `Reusable.Flow`를 추가한다. `.meta`는 대상 Unity 에디터가 생성한다. 일반 C# 프로젝트에서는 `FlowMachine.cs`를 포함한다. NuGet 패키지나 전역 싱글턴은 필요 없다. 같은 정의로 여러 머신 인스턴스를 만들 수 있으며 현재 상태는 각 인스턴스에 속한다. 상태 ID와 이벤트 키는 enum 또는 불변 문자열을 권장한다.

```csharp
using Reusable.Flow;

var flow = new FlowMachine<string, string>(
    new[] {
        new FlowState<string>("menu", enter: ShowMenu, exit: HideMenu),
        new FlowState<string>("editor", enter: OpenEditor, exit: CloseEditor)
    },
    new[] {
        new FlowTransition<string, string>("menu", "open", "editor", guard: CanOpen),
        new FlowTransition<string, string>("editor", "back", "menu")
    });

flow.StateChanged += UpdateNavigation;
flow.Start("menu");
bool accepted = flow.TrySend("open");
flow.Dispose();
```

예제의 콜백은 소비 프로젝트가 구현한다. 필요한 서비스는 해당 콜백을 소유하는 어댑터에 생성자 주입한다. 코어에 서비스 로케이터나 Unity 참조를 추가하지 않는다.

## 계약

- 생성 시 상태 목록과 전환 표를 복사한다. null, 빈 상태 목록, 중복 상태, 미등록 목적지, 동일 출발 상태/이벤트의 중복 전환은 예외다.
- `Start`는 한 번만 허용한다. 초기 상태의 Enter와 StateChanged를 호출한다. 시작 전 Current 접근은 예외다.
- 성공 전환 순서: Guard → 현재 Exit → Current 변경 → 다음 Enter → StateChanged. 자기 자신으로 전환하는 명시적 경로도 Exit/Enter를 수행한다.
- 시작 전, 허용하지 않은 이벤트, 거짓 Guard, 전환 콜백 안의 재진입은 `TrySend`가 false를 반환한다. 내부 요청 큐는 없다. 자동 후속 전환은 콜백 종료 뒤 호출자가 보낸다.
- 콜백 예외는 호출자에게 전파하고 IsFaulted를 켠다. 외부 부작용을 자동 롤백하지 않는다. 이후 전환은 거절된다. 호출자가 외부 자원을 정리하고 새 머신을 만든다. 이벤트 구독자는 예외를 던지지 않는 것을 권장한다.
- Dispose는 정상 활성 상태의 Exit를 한 번 호출하고 구독과 정의 참조를 해제한다. Faulted 상태의 Exit는 중복 부작용 방지를 위해 재호출하지 않는다. Dispose는 반복 호출해도 안전하며, 전환 중 호출은 예외다. 해제 뒤 Start/TrySend는 ObjectDisposedException이다.
- 단일 스레드용이다. 비동기 로딩·페이드·취소·스레드 동기화는 포함하지 않는다. 필요하면 Loading 상태를 정의하고 비동기 작업 완료 후 소유 스레드에서 이벤트를 보낸다. 작업 중 화면을 다음 상태로 미리 넘기지 않는다.

## 어셈블리 경계와 검증

asmdef는 `noEngineReferences: true`, 외부 참조 없음, 자동 참조 꺼짐이다. 프로젝트의 `Tools/AfterlifePrototype/Build-Modules.ps1`은 Unity 참조를 제거한 별도 `Reusable.Flow.ModuleValidation.csproj`를 생성해 `dotnet build --no-restore`로 컴파일한다.

런타임 확인 항목: 정상 전환의 콜백 순서, 거짓 Guard, 미등록 이벤트, 중복 정의의 예외, 콜백 내 TrySend 거절, 콜백 예외 뒤 IsFaulted, 반복 Dispose, 독립 인스턴스 사이의 상태 격리. 이번 작업에서는 저장소 규칙에 따라 실행 테스트 없이 컴파일만 수행했다.
