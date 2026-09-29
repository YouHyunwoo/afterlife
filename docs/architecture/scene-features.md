# 공용 씬 기능 구성

## 목적과 경계

게임, 개발일지, 커스텀 에디터가 같은 계산/표현 서비스를 사용한다. 공용 모듈은 Day01·Day02·Prototype을 참조하지 않는다. 순수 상태/선택은 `Afterlife.SceneFeatures.Core`, Unity 표현/설정은 `Afterlife.SceneFeatures`, 선택적인 Input System 입력은 `Afterlife.SceneFeatures.Input`, 저작 UI는 `Afterlife.SceneFeatures.Editor`에 둔다. 렌더링 수학 서비스는 Unity의 벡터/행렬을 사용하지만 GameObject를 생성하지 않는다.

## 세 그룹

| 그룹 | 기능과 용도 |
|---|---|
| 맵 환경 | 맵 생성(지형 종류 결정), 지형 표현(메시), 해변(모래 영역), 해안(경계 형상), 물(수면), 풀/장식/나뭇잎(배치와 카드), 바람(공통 흐름) |
| 상호작용 피드백 | 표면 선택(광선 판정), 지형 반응(풀 밀림/먼지/물결), 클릭 파티클, 도구 접촉, 피격 반응/카메라 흔들림, 자국, 파괴, 눈, 효과음 |
| 분위기·화면 표현 | 낮/밤 조명, 집 대신 명시적으로 지정한 시야 공급원과 커서 시야, 픽셀 출력, 시야 블러, Bloom, 환경음, 연기 |

`SceneFeatureProfile`에서 그룹과 개별 기능을 선택한다. 신규 프로필의 개별 기능은 모두 꺼져 있다. 그룹 비활성화는 개별 값/설정을 지우지 않는다. `After.asset`은 Day01 After의 조명/타격/음원/재질 기본값이다. 기본 시야 데모는 꺼져 있다. Day01 어댑터는 기존 V/필름 조작을 유지하도록 시야 기능을 명시적으로 덮어쓴다.

실행 순서는 **프로필 → 명시적인 씬 덮어쓰기 → 실행 설정 복제**다. 기능 토글은 개별 덮어쓰기, 상세 수치는 지도/식생/피드백/분위기 설정 묶음별 덮어쓰기를 제공한다. 실행 중 원본 ScriptableObject를 수정하지 않는다.

## 씬에서 사용

1. 빈 GameObject에 `SceneFeatureHost`를 붙이고 공용 `After` 프로필 또는 자신의 프로필을 연결한다.
2. `Bindings`에 카메라·태양을 연결한다. 자동 생성 대신 수작업 지도를 사용하려면 MapGeneration을 끄고 `MapAsset`을 연결한다.
3. 자원 프리팹에는 `FeedbackTarget`을 붙인다. Visual에는 흔들릴 시각 루트를 지정한다. 렌더링 표면의 `PickSurface.Target`에 타깃을 연결하고 Host의 Targets/PickSurfaces 목록에 등록한다.
4. FoliageSurfaces에는 잎을 만들 원본 MeshFilter만 등록한다. 현재 After 샘플러는 정점색으로 녹색 수관을 고른다. 도구에는 Axe/Pick과 실제 날 끝의 Contact Transform을 연결한다.
5. 시야 공급원, 창문 Renderer, Bloom 대상, 연기 발생 지점은 이름 검색 없이 Bindings에서 지정한다.
6. Inspector의 **적용 / 미리보기 갱신**을 누른다. 설정을 편집하거나 씬을 열기만 해서는 결과를 덮어쓰지 않는다.
7. **미리보기 시간 재생**으로 효과 시간을 진행한다. **생성 결과 저장**은 Assets 아래에 메시/재질 복사본과 정적인 BakedPreview를 저장한다. Play에서는 이것을 숨기고 실행 서비스를 만든다.
8. **생성 결과 정리**는 Host가 소유한 생성물과 연결된 BakedPreview만 정리한다. 외부에서 배치한 나무/건물은 유지한다. 저장 에셋 파일은 자동 삭제하지 않는다.

일반 마우스 조작은 별도 `SceneFeaturePointer`에 Host를 연결한다. UI 위 입력을 차단하며 픽셀 화면/레터박스 좌표를 보정한다. 이미 게임 입력이 있는 경우 이 컴포넌트를 추가하지 않고 `Session.Pointer`를 호출한다. Day02는 자신의 입력·진행 코드를 사용한다.

필수 참조가 빠진 기능은 Inspector Diagnostics에 표시한다. 다른 기능의 토글을 자동으로 켜지 않는다. 도구/접촉 지점이 없으면 도구 연출을 생략하고 즉시 접촉으로 처리한다. 맵이 없으면 맵을 필요로 하는 기능이 보류되지만 오브젝트 피드백/분위기는 별도로 사용할 수 있다.

## 생성 로직 직접 사용

```csharp
var map = MapGenerator.Generate(new MapGenerationSettings {
    Radius = 7, Seed = 1, Shape = MapShape.SmallIsland, Beach = true
});
var geometry = new TerrainGeometry(map.Radius, map.At, palette,
    new CoastalTerrain(CoastShape.Rounded, -.12f, -.85f));
// geometry.Chunks: 정점/법선/색/삼각형. 여기까지 GameObject 생성 없음.
var view = new TerrainSurface(parent, geometry, material);
// 사용 종료 시 view.Dispose();
```

- `MapData`: 범위, 지형 종류 배열, 선택적인 높이 샘플. 결과와 생성기는 분리되어 있다. `UseHeightSamples`를 켜면 저장한 높이를 사용한다.
- `MapGenerator`: 설정/시드 → MapData. 기존 WorldMapLayout은 본편 도로/랜드마크 소비자의 호환 경로로 유지한다.
- `TerrainGeometry`: 지형 높이·색·청크 형상·광선 판정. TerrainSurface는 이 결과를 Unity에 적용한다.
- `WaterGeometry.Calculate`: 수면/외곽 수벽 데이터만 계산한다.
- `GrassGeometry.Calculate`: 지도/표면/장애물 정보 → 풀 카드와 실제 접촉 위치 목록.
- `LeafGeometry.Calculate`: 표면 배열과 변환행렬 → 잎 카드 형상. 원본 Transform을 조회하는 단계는 렌더러 어댑터가 담당한다.
- `SurfaceScatter.Generate`: 지도/바이옴/시드 → 장식 위치·회전·크기 목록.

## 런타임·커스텀 에디터 공통 호출

```csharp
var settings = overrides.Resolve(profile.Settings);
var session = new SceneFeatureSession(root, profile, settings, bindings,
    activeEnvironment: true);
session.Contact += (target, point) => { /* 게임 쪽에서 채집/체력 처리 */ };
// 게임 delta 또는 에디터 미리보기 delta를 전달한다.
session.Tick(delta, paused);
// 촬영 재시작: session.Reset();
// 종료: session.Dispose();
```

접촉 확정은 `Contact` 한 곳에서 게임 처리에 연결한다. 클릭 시점과 접촉 시점에서 각각 채집을 호출하지 않는다. Host.Contact는 재적용으로 Session이 교체되어도 구독을 유지한다.

`ObjectFeedback`은 MonoBehaviour 없이 생성하는 오브젝트 연출 서비스다. Owner/Visual과 표면 정보·설정을 제공하고 Initialize/Hit/Tick/ResetTake/Dispose를 호출한다. `FeedbackTarget`은 직렬화·Unity 수명·게임 이벤트 연결용 어댑터다. `ImpactPresentation`은 구체 컴포넌트 대신 `IImpactReceiver`를 받는다. `ToolContactClock`과 `DayNightTransition`은 엔진 참조가 없는 시간 로직이다.

커스텀 에디터는 같은 생성 서비스를 호출하고, Undo/AssetDatabase 저장만 Editor 어셈블리에서 처리한다. 에디터 미리보기는 도메인 재로드·Play 진입 전·에디터 종료 때 정리한다. 자동으로 사용자 씬을 마이그레이션하지 않는다.

## 수명·전역 상태

- 여러 Host의 지형/오브젝트 생성물은 별개다. 현재 전역 셰이더/카메라 효과는 활성 환경 하나가 소유한다. 다른 Host는 `ActiveEnvironment=false`로 사용한다. 서로 다른 밤/시야를 동시에 렌더링하는 다중 월드 렌더러는 제공하지 않는다.
- 기본 갱신은 Host가 맡는다. 게임/필름에서 직접 시간을 제어하려면 ExternalTick을 켜고 Session.Tick에 일시정지를 전달한다.
- 피격·눈·자국은 기존 MaterialPropertyBlock을 읽어 자신의 속성만 갱신한다. Bloom도 자신의 발광 속성만 복원한다.
- 도구·파티클·음원·생성 Mesh/Material은 생성한 서비스가 정리한다. Bake는 별도 복사본이므로 실행 서비스 해제가 저장 에셋을 지우지 않는다.
- 모듈에 게임 체력/골드/건설 규칙을 넣지 않는다. 채집 소진 여부는 게임에서 결정한 뒤 `Break`를 호출한다.

## 기존 소비자

- Day01: 기존 MonoBehaviour GUID와 public 직렬화 필드를 유지한 어댑터. 지도/계층 해석과 시연용 타격 횟수는 Day01에 남고, 실제 피격·도구·조명·픽셀 출력은 공용 서비스를 사용한다. 생성 토글은 이미 저작된 Day01 지도를 재생성하지 않는다.
- Prototype: 기존 WorldMapLayout 소비자를 유지하며, 지형을 TerrainGeometry 계산 후 TerrainSurface에 적용한다.
- Day02: 1번 섬에만 Host를 연결한다. 물→모래→풀→나무 구성을 생성하고 직접 클릭의 도구 접촉 이벤트에서 한 번 채집한다. 2~4번은 기존 구현을 유지한다. 이전 1번 생성물은 비활성 LegacyFirstIsland로 보존한다.

## 검증

`python impls/unity/Afterlife/Tools/SceneFeatures/validate.py`는 Unity가 생성한 프로젝트/로컬 참조를 바탕으로 임시 FeatureValidation 프로젝트를 만든 후 `dotnet build --no-restore`만 실행한다. NuGet 복원과 Test Runner는 실행하지 않는다. 순수 Core 프로젝트에는 Unity 참조를 넣지 않는다.

사용자가 Unity에서 확인할 항목:

1. 같은 프로필/시드로 Inspector 생성 결과와 Play 생성 결과가 같은지.
2. 그룹/기능 토글 후 적용, 씬 덮어쓰기와 원본 프로필 보존 여부.
3. 적용→정리→재적용을 반복해 중복 생성물/소리/전역 효과가 남지 않는지.
4. Day01 단계 전환, 클릭 피격/눈/파괴, N 낮밤, V 시야, P 픽셀 출력.
5. Day02 1번에서 한 번 클릭→한 번 채집, 커서와 실제 접촉 위치, F8/Space/좌우/Home 동작.
6. 일시정지 중 도구/바람/파티클/소리가 멈추고 재시작 때 상태가 초기화되는지.
7. 여러 Host 중 활성 환경 하나만 전역 효과를 관리하는지.

빌드·에셋 참조 검사는 실제 Play 화면/소리/손맛 검증을 대신하지 않는다. 공개 음원 크레딧은 docs/audio-credits.md를 따른다.
