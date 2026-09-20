# 모듈 안내

## 의존성 방향

Day01은 입력·카메라·촬영 UI와 모듈 조합을 담당한다. 공용 모듈은 Day01 어셈블리를 참조하지 않는다. 기존 MonoBehaviour 직렬화 참조를 보존하기 위해 일부 네임스페이스의 Day01 표기는 유지했다. 폴더명만으로 재사용 가능성을 판단하지 않고 asmdef 참조를 기준으로 본다.

|모듈|책임|Devlog 내부 의존성|
|---|---|---|
|Contracts|IPickTarget, ISnowTarget 최소 계약|없음|
|Terrain|바이옴·타일 변형·색 혼합 규칙, 순수 C#|없음, noEngineReferences|
|TerrainView|주입된 맵의 청크 지형 메시·높이 조회·정밀 지면 선택|Terrain|
|Interaction|삼각형 선택, 표면 색, 런타임 선택 메시 생성·공유 캐시|Contracts|
|Snow|눈 덮개 생성과 타격 침식|Contracts, Interaction|
|Vegetation|풀/잎 빌보드·바람·색상 배치·적설 노출|Terrain, Interaction|
|Lighting|낮/밤·집/커서 시야·연기|없음|
|Wind|CPU/GPU 공통 방향·공간 돌풍, 전역값 발행·복원|없음|
|DamageMarks.Core|최대 8개 타격 자국 이력·근접 병합, 순수 C#|없음, noEngineReferences|
|DamageMarks|표면 좌표 변환·셰이더 배열 전달|DamageMarks.Core|
|Destruction|쓰러짐 후 제거, 바위 조각, 설정|없음|
|Hover|커서 흔적·먼지·풀 반응|Terrain|
|Shoreline|해안 모서리 메시 변형과 단면|Terrain|
|Audio|음원 데이터, 타격/파괴 채널과 순환|없음|
|AmbientAudio.Core|환경음 페이드·간헐음 간격, 순수 C#|없음, noEngineReferences|
|AmbientAudio|풀 접촉음·간헐적 새 울음 채널, 주입된 설정/음원 재생|AmbientAudio.Core|
|Rendering|공통 표면 셰이더|기능별 HLSL include, C# 어셈블리 없음|
|Water|주입된 맵의 수면 청크·외곽 수벽·재질 수명, 공유 물 셰이더|C# 독립, HLSL Lighting/Hover 포함|
|Cinematics.Core|누적 시간·컷 경계·재생/일시정지, 순수 C#|없음, noEngineReferences|
|Cinematics|시퀀스 데이터·카메라·TMP 자막·뷰 수명|Cinematics.Core, TMP, UI|
|Day01 Runtime|씬 참조와 실행 순서, 타깃 어댑터|위 모듈, Input System, URP|
|Day01 Editor|씬/메시 제작, 마이그레이션·프리팹 저장|Runtime 및 제작에 필요한 모듈, Editor 전용|

## 재사용 방법

프로토타입 식생은 `ShowcaseGrass.Map`에 바이옴/높이/색을 주입하고, `ShowcaseLeafCards`에 읽기 가능한 수관 데이터와 Shader/카드 상한/속성 변환을 주입한다. 풀·잎 HLSL은 출력/조명 훅을 제공한다. PrototypeVegetation이 모델 수명·풀 가림 마스크·색 변경 버전을 조립하며 Wind의 시간을 갱신한다. [상세](../records/prototype-vegetation.md).

프로토타입 해안·수면은 Terrain의 순수 `CoastField` → TerrainView의 선택적 `CoastalTerrain` 설정으로 조립한다. WaterSurface는 물 판별 함수와 Shader·높이만 주입받으며 시야/입력을 소유하지 않는다. Day01과 프로토타입은 공유 수면 HLSL을 사용하고 각각 조명/출력 훅을 선택한다. [상세](../records/prototype-water.md).

클릭 대상은 IPickTarget을 구현한다. 눈이 필요한 대상은 ISnowTarget에서 Visual, HasSnow, SnowRemaining과 표면의 눈 표시 여부를 제공한다. ShowcasePickSurface는 MonoBehaviour 참조를 직렬화하고 인터페이스로 소비한다. 기존 Target 필드 이름은 FormerlySerializedAs로 호환한다.

ShowcaseAudio는 부모 Transform과 ShowcaseAudioBank를 주입한다. Play에 나무 여부·파괴 여부·현재 시간을 전달하고, Reset은 순환 상태와 재생을 초기화한다. Dispose는 생성한 오디오 오브젝트를 정리한다. 나무 쓰러짐과 타격은 별도 채널이다.

물 호버음은 Hover의 WaterRippleCreated 이벤트를 Day01에서 ShowcaseAudio.PlayWaterRipple에 연결한다. Day01은 기본 변형 5개를 주입하며 뱅크의 WaterRippleVariants 및 기존 단일 WaterRipple 설정을 지원한다. 독립 채널 3개, 0.25~0.40초 간격, 연속 이동 볼륨 55%, 피치 ±2%로 여운을 유지한다. Reset은 모든 채널·간격·직전 선택을 초기화하고 OnDisable에서 구독을 해제한다.

DestructionProfile은 타격 횟수, 나무 넘어지는 시간/각도, 대기 시간, 바위 조각 수/굴림 시간, 제거 시간을 보관한다. 기존 장면과 동일한 기본값으로 생성한다. 프리팹별로 다른 프로파일을 사용할 수 있다.

## 수명과 초기화

환경음은 Day01이 AmbientAudio에 클립·풀 접촉 여부·낮/밤·활성 공간을 주입한다. Vegetation.HasGrassAt은 실제 생성된 After 풀의 위치와 폭으로 지면 접촉 영역을 확인한다. Day01은 유효한 지면 선택과 실제 마우스 이동을 함께 요구한다. 풀 접촉음은 선택한 2·3·4번 발췌본을 직전 반복 없이 랜덤 재생한다. 정지 시 추가 재생만 막고 여운은 보존하며, 공간 이탈은 0.12초 페이드한다. 바람 재생은 제거했다. 새 울음은 기존 2초 공간 페이드와 밤 억제를 유지한다. R은 두 채널과 타이머를 초기화한다. Dispose는 모든 채널과 재생 요청 구독을 해제한다. [설정과 확인 절차](../records/nature-ambience.md).

Picking 데이터는 원본 Mesh별로 최초 접근 시 생성하며 파일로 저장하지 않는다. Surface가 사용을 등록하고 마지막 사용자 파괴 시 해제한다. 비활성 풀 객체는 재사용을 위해 유지하고 씬 언로드 시 파괴된 사용자를 정리한다. 플레이 진입/종료와 어셈블리 리로드에서 캐시를 초기화한다. 외부 주입된 눈/변형 지형의 데이터는 해당 모듈이 해제한다. 원본 모델은 Read/Write가 필요하다. [전환 기록](../records/runtime-picking.md).

Day01Showcase의 OnEnable에서 모듈을 구성하고 OnDisable에서 Dispose/복원을 호출한다. 생성한 메시·재질·파티클의 해제 책임은 생성 모듈에 있다. R과 공간 전환은 타깃/흔적/효과의 촬영 상태를 초기화한다. 셰이더 전역값과 전용 렌더 파이프라인은 종료 시 복원한다.

## 현재 일반화의 범위

최적화 API: Interaction의 ShowcasePickIndex는 정적 공간 트리/동적 타깃 후보를 관리하며 정적 배치 변경 시 Rebuild가 필요하다. Destruction의 ShowcaseChipPool은 작은 파편을 소유·재사용하고 Day01이 최대 256개를 조율한다. Vegetation의 ShowcaseVegetationDensity는 거리별 카드 인덱스 수를 변경하고, 잎 속성은 선택적인 버전 함수로 동기화한다. Hover는 활성 풀/지형 흔적을 분리해 CPU/GPU 입력을 압축한다. [범위·수명·한계](../records/runtime-optimization.md).

Wind는 씬의 식생/집을 직접 찾지 않는다. Day01이 바람 샘플 함수를 Smoke에 주입하고, GPU 식생은 같은 수식의 HLSL을 사용한다. Dispose에서 기존 셰이더 전역값을 복원한다. Lighting과 Vegetation C# 어셈블리는 Wind 어셈블리를 직접 참조하지 않는다.

Terrain은 엔진과 분리됐지만 고정 Day01 지도 데이터가 함께 있는 샘플 모듈이다. Day01의 메시 생성기·배치·도구 애니메이션은 에피소드 전용으로 남겼다. 모든 기능을 게임플레이 프레임워크로 재작성한 것은 아니다.

프로토타입은 Terrain의 지도 조회 주입 오버로드와 TerrainHeightGrid, TerrainView를 사용한다. 기존 TerrainMap이 바이옴/이동 규칙의 원본이고, PrototypeView가 지형 높이를 표시 위치에만 연결한다. [지형 적용 계약](../records/prototype-terrain.md).

시야·호버·침식은 셰이더 전역 배열을 공유한다. 여러 쇼케이스 인스턴스를 동시에 실행하는 것은 지원하지 않는다. 큰 맵 적용 전에는 공간별 상태 주입, 파티클/파편 풀링, 선택 메시 가속 구조를 별도로 설계해야 한다.

## 6단계 촬영 구성

Day01의 PresentationStages가 6개 공간을 소유한다. 기존 Stages[2]는 After 업그레이드 호환을 유지한다. 공간 전환에서 기존 효과를 해제한 뒤 해당 단계 모듈을 재생성한다. Vegetation의 단일 공간 생성과 Lighting의 즉시 낮/밤 설정, Wind의 강도 인자는 기존 호출을 보존하는 선택 API다. [상세 단계 계약](../records/stage-progression.md).

촬영 입력은 Day01FilmDirector에서 받고 Cinematics에 IFilmStageView를 주입한다. 기본 쇼케이스는 유지하고 F8로 명시적으로 시작한다. Dispose는 자막 캔버스·카메라 상태·쇼케이스 모드를 복원한다. 픽셀 프레임은 Overlay RawImage로 합성하여 TMP 자막 아래에 표시한다. [연출 시간표와 조작](../records/movie-sequence.md).

## 연속 지형 높이장

Terrain의 ShowcaseTerrainField가 맵 전체 좌표의 gradient Perlin과 실제 0.25m 삼각형 표면 높이를 제공한다. ShowcaseTileVariants.Height는 이 필드로 위임하고 기존 높이는 LegacyHeight로 이관 시에만 사용한다. Day01ContinuousTerrainSetup은 3~6단계를 같은 좌표별 메시로 갱신하고 경계 높이를 확인한다. Vegetation과 타깃 지면 조회도 같은 표면 보간을 사용한다. 쇼케이스 공간 이동 오프셋은 제외하여 전후 비교 구도를 유지한다. [상세 기록](../records/continuous-terrain.md).

프로토타입 지면 색은 PrototypeGroundAppearance HLSL과 런타임 색/법선 텍스처를 지형·풀·식물 장식이 공유한다. TerrainSurface는 원본 맵의 색/높이를 제공하고, 프로토타입 어댑터가 텍스처 생성·소유·전역값 복원을 담당한다. Grass 출력 훅은 Day01과 프로토타입 표현을 분리한다.

프로토타입 커서 반응은 `PrototypeHover`가 `ShowcaseTileHover.Map`으로 지도 범위·바이옴·흙먼지 셰이더를 주입한다. HoverDust 패스를 공유하고 출력 조명만 바꾼다. TerrainSurface는 선택적 높이 함수를 받아 변형 후 삼각형을 선택한다. WaterSurface는 파문 표시를 명시적으로 켜며 생성/초기화/메뉴 정지/해제는 프로토타입 어댑터가 조율한다. [이관 상태와 확인 절차](../temp-prototype-migration-context.md).

`PrototypeSurfaceFeedback`은 나무·바위의 Renderer에 공용 ShowcasePickSurface를 붙여 원본 메시 선택 캐시를 재사용한다. 도구 접촉점으로 ShowcaseDamageMarks를 갱신하며 게임 규칙을 소유하지 않는다. 렌더러 틴트와 자국 MPB를 보존하고 제거/재시작 시 표면 등록을 해제한다.

프로토타입 직접 작업은 PrototypeManualWork(모델의 작업 가능/단일 대상 기여)와 Presentation의 준비/접촉 타이밍으로 나눈다. 모든 커서 자동 반복은 CursorAutomationUnlocked로 제한한다. PrototypeImpactFeedback은 Destruction의 타격 타이밍/작은 파편 풀을 재사용하며 게임 피해량이나 자동화 해금을 결정하지 않는다. [최신 규칙](../../docs/game-process/46-click-work-and-automation.md).

PrototypeDestruction은 소진된 자원의 뷰를 보존하고 공용 ShowcaseDestruction을 조립한다. 원본 메시 선택 필터와 지형 높이를 주입하고 생성 조각/제거 상태를 소유한다. 나무 회전은 어댑터에서 수행하며 잎 속성 버전은 원본 디졸브와 함께 갱신한다.

프로토타입 음향 어댑터: PrototypeEnvironmentAudio가 AudioBank 및 ShowcaseAudio/ShowcaseAmbience를 소비한다. 모델은 Stone/TreeFall/StoneBreak 사건만 발행하며 Resources/AudioSource를 참조하지 않는다. 물 생성 사건은 Hover 어댑터에서 구독/해제한다.

집 연기: ShowcaseSmoke의 방출 대상/셰이더를 선택 인자로 주입한다. ShowcaseSmokePass는 노이즈/투명도 공용 본문이고 프로토타입 Resources 셰이더가 시야·조명 출력을 제공한다. PrototypeSmoke가 집의 완공/이전/제거와 모듈 수명을 조합한다.
