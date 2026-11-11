# Moonstone 0.11.6 소비 코드 마이그레이션

## 작업 목록
- [x] Ore 사용 코드와 0.11.6 API 대조
- [x] 씬별 Arc ServiceResolver와 명시적 의존성 주입으로 전환
- [x] Core 수명주기 및 D3 엔티티로 전환
- [x] 제거된 패키지 컴포넌트와 설정 참조 정리
- [x] 어셈블리 경계별 C# 빌드
- [ ] Unity 새로고침 및 사용자의 씬 실행 확인

## 패키지와 변경 범위

GitHub 주소는 `https://github.com/material-stone-games/moonstone.git`이다. 현재 lock의 커밋은 `9ed4ad67f3b7875a7b488480b20bb00406fa9506`, 패키지 버전은 0.11.6이다. Moonstone 원본 및 PackageCache 소스는 수정하지 않았다. 로컬 embedded 패키지를 다시 만드는 PrototypeTools.EmbedMoonstone 진입점도 제거했다.

## API 대응

|기존|현재|
|---|---|
|Ore.Container MonoBehaviour|씬 조립 코드가 소유하는 Arc.DependencyInjection.ServiceResolver|
|빈 필드 자동 검색과 Bind|Register<T>, [Inject], BindEachOther|
|동적 WaveEvent의 Inject|생성자에 IResolver로 조회한 네 가지 의존성 전달|
|Ore.Model|D3.Domain.Entity, System.Guid로 새 ID 생성|
|Ore.Repository|독립 Afterlife.Storage의 EntityStore<T>|
|Ore.Local.Entity / Visible|Core.LifecycleBehaviour / Visible 기반 Afterlife 수명주기 어댑터|
|Ore.Local.System|위 수명주기 어댑터에 SetUp / TearDown을 제공하는 SceneSystem|

Afterlife.Storage는 Unity 참조가 없는 실행 중 엔티티 저장소다. D3의 비동기 영속 저장소 인터페이스로 기존 동기 게임 소비자를 강제 변경하지 않는다. WorldRepository와 ObjectRepository의 기존 조회/저장 API는 보존한다.

Afterlife.Lifecycle.Unity는 프리팹 Awake에서 Initialize, OnDestroy에서 Dispose를 호출한다. Core의 중복 호출 방지 로직을 사용한다. 시스템 활성 구간의 SetUp/TearDown은 씬 생성/소멸과 구분한다.

## 씬과 수명

기존 게임 Bootstrapper는 생성 → 등록/주입 → 모드 초기화 → Start에서 월드 준비 순서를 사용한다. 프로토타입도 전역 Container 대신 자체 ServiceResolver를 소유하며 씬 종료 때 Dispose한다. 프로토타입의 재도전/환생은 기존 모델 Reset 흐름을 유지하며 씬별 컨테이너를 재생성하지 않는다. 기존 뷰의 이벤트 구독 해제는 유지했다.

BuildMode의 직렬화된 가이드 참조가 있으면 보존하고, 비어 있을 때만 [Inject] 프로퍼티로 등록된 가이드를 채운다. 주입 대상 외의 설정·캐시 필드에는 [Inject]를 붙이지 않는다.

Field.unity와 World.unity에서 사라진 Ore.Container 컴포넌트와 해당 _container 필드만 제거했다. 나머지 컴포넌트 파일 ID와 기존 스크립트 GUID는 보존했다. 사용되지 않던 비활성 Moonstone Arc Settings.asset은 .meta와 함께 당시 저장소 상위 local-backups/moonstone-migration-settings에 백업했다. 2026-10-07 외부 D:/area/archives/Afterlife/backups/project-local-backups-2026-10-07/moonstone-migration-settings로 이전·해시 검증했다. .meta 내용은 수정하거나 직접 생성하지 않았다.

## 검증

Unity가 만든 기존 csproj에는 0.10.1 경로가 남아 있어 Temp/MoonstoneMigration 아래에 현재 소스 및 어셈블리 경계를 반영한 임시 검증 csproj를 만들었다. 기존 Unity 참조를 사용하고 패키지 복원 없이 dotnet build --no-restore로 검증했다.

- DefaultAssemblyDefinition: 오류 0개, 직렬화/주입 필드 등 경고 278개.
- Afterlife.Prototype 및 Afterlife.Prototype.Editor: 오류 0개, 경고 0개.
- Moonstone.Core / Arc / D3.Domain과 신규 Lifecycle / Storage를 별도 어셈블리로 컴파일했다. Storage와 D3.Domain 검증에서는 Unity 참조를 제외했다.
- Assets 코드의 Moonstone.Ore 참조와 제거된 패키지 GUID 참조가 남지 않았는지 정적으로 확인했다.
- Unity 에디터 테스트, 플레이 모드, Player 빌드는 실행하지 않았다.

## 사용자 확인 절차

1. Unity에서 새로고침한다. 신규 Lifecycle/Storage 폴더의 .meta는 Unity가 생성하게 하고, 커밋 때 함께 포함한다. 필요하면 C# 프로젝트 파일을 재생성한다.
2. Console에서 Moonstone.Ore 및 어셈블리 누락 오류가 사라졌는지 확인한다.
3. Main에서 기존 게임으로 진입하거나 Field 씬을 실행한다. 월드, 기존 건물과 유닛 생성, 카메라 이동, C 키 건설 모드의 가이드, 웨이브 스폰이 정상인지 확인한다.
4. World 씬에서 지도 생성과 새로고침을 확인한다.
5. Afterlife_PixelPrototype에서 타이틀 → 인트로 → 플레이, 건설/강화/스킬, 종료 후 재진입을 확인한다. 주입 실패나 중복 초기화 오류 없이 기존 화면 전환이 유지되어야 한다.
