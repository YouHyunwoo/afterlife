# Legacy 컴파일 경고9개 수정 · 2026-10-07

모두 CS0649 (값을 할당하지 않은 필드)였다. Moonstone 주입 필드7개는 런타임 리플렉션 할당을 C# 컴파일러가 알지 못해 발생했다. 카메라 좌표2개는 드래그 시작 처리와 이벤트 구독이 주석 처리된 상태로 남아 발생했다.

## 경고별 변경

경로는 Unity `Assets/Legacy/Scripts/` 기준이다.

|번호|파일·기존 필드|원인|수정|
|---|---|---|---|
|1|Field/CameraSystem.cs · _worldRepository|Moonstone 런타임 필드 주입|WorldRepository 주입 속성으로 변경|
|2|Field/ObjectSpawnSystem.cs · _worldRepository|동일|WorldRepository 주입 속성으로 변경|
|3|Field/ObjectSpawnSystem.cs · _objectRepository|동일|ObjectRepository 주입 속성으로 변경|
|4|Field/ObjectSpawnSystem.cs · _objectSystem|동일|ObjectSystem 주입 속성으로 변경|
|5|Field/Event/EnemySpawnEvent.cs · _worldRepository|동일|WorldRepository 주입 속성으로 변경|
|6|Field/Event/EventSystem.cs · _timeSystem|동일|TimeSystem 주입 속성으로 변경|
|7|Field/Mode/BuildMode.cs · _worldRepository|동일|WorldRepository 주입 속성으로 변경|
|8|GameSystem/Stage/CameraSystem.cs · dragStartPosition|비활성 드래그 코드의 잔여 상태|상태·실행되지 않는 분기·주석 처리된 핸들러 제거|
|9|GameSystem/Stage/CameraSystem.cs · initialCameraPosition|동일|동일|

## 설계와 적용

기존 Moonstone 0.11.6 패키지의 DependencyInjector.InjectProperties는 public/private 속성을 찾고 setter로 값을 주입한다. InjectAttribute도 필드와 속성을 모두 지원한다. 따라서7개 의존성은 같은 타입·같은 InjectAttribute를 유지한 private 자동 속성으로 표현했다. 경고를 pragma로 숨기거나 null 초기값으로 덮지 않았다. 씬/프리팹을 새로 설치하거나 Inspector 값을 다시 연결할 필요는 없다.

카메라의 isDragging은 false만 할당했고 true로 바꾸는 시작 핸들러와 구독은 이미 비활성 상태였다. 드래그 기능을 새로 활성화하지 않고 해당 잔여 코드만 제거했다. 기존 포인터 위치 추적·화면 가장자리 이동·SetCameraPosition은 유지한다. 변경한 주입 필드와 드래그 상태는 Unity 직렬화 대상이 아니며 씬·프리팹·meta는 변경하지 않았다.

새 기능이나 모듈을 Legacy에 추가하지 않았다. 이 경고 수정은 기존 의존성 표현과 비활성 코드 정리에 한정한다. 다시 이전 코드로 돌아가려면 이 기능 커밋을 되돌리고 같은 컴파일을 수행한다.

## 검증

직전 구조 변경 검증은 전체62개 프로젝트 성공·경고9개였다. 이번 변경 후 해당6개 파일을 포함하는 DefaultAssemblyDefinition을 다시 컴파일해 **경고0개·오류0개**를 확인했다. 결과는 `impls/unity/Afterlife/Logs/legacy-warnings-build.log`에 있다.

Unity 프로젝트 폴더에서 재현한다. 기존 전체 컴파일 결과와 참조 DLL이 먼저 있어야 한다.

```powershell
dotnet build DefaultAssemblyDefinition.csproj --no-restore --nologo -v minimal -m:1 -p:BuildProjectReferences=false -p:SkipResolvePackageAssets=true -p:GenerateDependencyFile=false -p:EnableSourceControlManagerQueries=false
```

Unity 테스트·Play는 실행하지 않았다. 사용자는 기존 Legacy 시작/조합 절차로 Main·Field·World 씬을 확인한다. Moonstone 주입 예외가 없고 카메라 가장자리 이동·줌, 건설 미리보기/확정/철거, 등록된 필드 이벤트의 진행이 이전과 같아야 한다. Stage 카메라의 비활성 드래그는 계속 동작하지 않는다. 미완성 EnemySpawnEvent의 주석 처리된 적 생성 로직은 이번에 구현하지 않았다.
