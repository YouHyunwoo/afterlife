# Unity 모듈 이름 점검

> 확정 기획 · 2026-10-03: 채집·건설·수리·공격은 버튼을 누르고 있는 동안 작업 주기에 맞춰 반복하고, 강화는 작업 1회당 작업량을 높인다. 자동 수입은 농장 건설·생산 강화로 확장한다. 본편 상호작용 필드·슬롯·충전·호버 자동 작업은 폐기한다. [새 규칙·사용법·검증](../game-process/71-click-work-and-farm-automation.md). 새 규칙은 게임 코드·에셋에 아직 적용하지 않았다. 이전 규칙·수치·QA는 당시 기록으로 보존한다.


## 본편 규칙 변경과 모듈 보존 · 2026-10-03

본편은 버튼 유지 작업 작업과 농장 자동 생산으로 전환할 설계다. DivineFields와 RechargeSlots는 Day02 등 과거 시연 소비자가 있어 일괄 삭제/이름 변경하지 않는다. Interaction은 표면 선택·캐시 기능이므로 유지한다. 버튼 유지 작업의 순수 규칙과 Unity 입력/표현은 별도 책임으로 설계하고 농장 생산은 Buildings를 사용한다. 실제 새 어셈블리/타입 이름은 후속 구현에서 정한다. 아래 표는 기존 이름/의존성 변경 기록이다.

대상은 `Assets/Modules/`의 공용 모듈 21개와 `Assets/Devlog/Youtube/Modules/`의 촬영 전용 모듈 3개다. 아래의 '변경'은 폴더명뿐 아니라 어셈블리명, 네임스페이스, 공개 타입의 `Showcase` 접두어 중 하나 이상을 바꾼 경우다. Day01 런타임·에디터와 프로토타입은 모듈 소비자이므로 이름을 유지하고 참조를 갱신했다.

| 기존 이름 | 기능·역할 | 판단 | 변경 후 이름 |
|---|---|---|---|
| AmbientAudio | 풀 접촉음과 새 울음의 재생·페이드·간격 | 변경: Devlog 어셈블리·타입 제거 | `AmbientAudio` / `Afterlife.AmbientAudio` |
| Audio | 자원 타격·파괴·물 파문 효과음과 음원 뱅크 | 변경: 포괄적인 이름을 실제 범위로 한정 | `WorldAudio` / `Afterlife.WorldAudio` |
| Contracts | 선택·적설 대상의 최소 인터페이스 | 변경: 계약 대상을 명시 | `TargetContracts` / `Afterlife.TargetContracts` |
| DamageMarks | 타격 자국 이력과 셰이더 전달 | 변경: Devlog 어셈블리·타입 제거 | `DamageMarks` / `Afterlife.DamageMarks` |
| Destruction | 자원 소멸, 파편 풀, 타격 타이밍 | 변경: Devlog 어셈블리·타입 제거 | `Destruction` / `Afterlife.Destruction` |
| DivineFields | 신성 상호작용 영역의 게임 규칙 | 유지: 역할과 이름 일치 | `DivineFields` / `Afterlife.DivineFields` |
| Flow | 엔진 비의존 상태·전환 머신 | 변경: `Reusable.Flow`을 프로젝트 어셈블리 규칙에 맞춤 | `Flow` / `Afterlife.Flow` |
| Hover | 커서 지면 흔적·먼지·풀 반응·물 파문 | 변경: Devlog 어셈블리·타입 제거 | `Hover` / `Afterlife.Hover` |
| Interaction | 메시 표면 선택·공유 캐시·눈 노출 조회 | 변경: Devlog 어셈블리·타입 제거 | `Interaction` / `Afterlife.Interaction` |
| Lifecycle | 씬 객체의 초기화·가시성·해제 계약 | 유지: 역할과 이름 일치 | `Lifecycle` / `Afterlife.Lifecycle.Unity` |
| Lighting | 시야·낮밤 조명·수정 발광·집 연기 | 변경: Devlog 어셈블리·타입 제거 | `Lighting` / `Afterlife.Lighting` |
| Progression.Unity | 강화 카탈로그와 Unity UI 표현 | 유지: Unity 어댑터임을 명시 | `Progression.Unity` / `Afterlife.Progression.Unity` |
| Skills | 스킬 상태와 규칙 | 유지: 역할과 이름 일치 | `Skills` / `Afterlife.Skills` |
| Snow | 대상 적설 덮개와 타격 침식 | 변경: Devlog 어셈블리·타입 제거 | `Snow` / `Afterlife.Snow` |
| Storage | 엔진 비의존 엔티티 저장 | 유지: 역할과 이름 일치 | `Storage` / `Afterlife.Storage` |
| Terrain | 바이옴·타일·높이장·지도 배치 규칙 | 변경: Devlog 어셈블리·타입 제거 | `Terrain` / `Afterlife.Terrain` |
| TerrainView | 지형 메시·높이·표면 선택의 Unity 뷰 | 변경: Devlog 어셈블리 제거 | `TerrainView` / `Afterlife.TerrainView` |
| Upgrades | 강화 트리 상태와 규칙 | 유지: 역할과 이름 일치 | `Upgrades` / `Afterlife.Upgrades` |
| Vegetation | 풀·잎 카드 생성과 거리별 밀도 | 변경: Devlog 어셈블리·타입 제거 | `Vegetation` / `Afterlife.Vegetation` |
| Water | 수면 청크·외곽 수벽·재질 수명 | 변경: Devlog 어셈블리 제거 | `Water` / `Afterlife.Water` |
| Wind | 공통 풍향·돌풍·셰이더 전역값 | 변경: Devlog 어셈블리·타입 제거 | `Wind` / `Afterlife.Wind` |
| Cinematics | Day01 촬영 시퀀스·나레이션·크레딧 | 유지: Devlog 전용 경계와 이름 일치 | `Cinematics` / `Afterlife.Devlog.Cinematics` |
| Rendering | Day01 비교 화면용 표면 셰이더 | 유지: Devlog 전용 경계와 이름 일치 | `Rendering` (C# 어셈블리 없음) |
| Shoreline | Day01 해안 변형·단면 미리보기 | 유지: Devlog 전용 경계와 이름 일치 | `Shoreline` / `Afterlife.Devlog.Shoreline` |

승격된 공용 코드의 네임스페이스는 `Afterlife.Modules.<기능>`이며 Core와 Runtime 분리 어셈블리는 기존 분리를 유지한다. 공개 타입은 `PickSurface`, `TerrainBiome`, `GrassRenderer`, `WorldAudioBank`처럼 실제 책임을 드러내도록 바꿨다. 공용 셰이더 파일명·함수명·전역 속성과 Resources 경로도 함께 갱신했다. Day01 전용 `Day01Showcase`와 `ShowcaseSurface` 등의 이름은 촬영 구성의 의미를 보존한다.

Unity 직렬화 참조를 위해 이동한 에셋과 스크립트의 `.meta` 내용 및 GUID를 유지했다. 기존 필드명은 바꾸지 않아 씬·프리팹·ScriptableObject 값의 마이그레이션이 필요하지 않다.
