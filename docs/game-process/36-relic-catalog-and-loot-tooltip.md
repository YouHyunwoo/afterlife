# 36 · 유물 데이터셋과 프리팹 HUD

> 최신 규칙: 초반의 모든 커서 작업은 대상 직접 클릭 1회로 실행하며, 자동화 강화 후에만 호버 주기 작업이 열린다. 아래의 시작부터 자동 기여 설명/과거 QA는 변경 전 기록이다. [최신 규칙·구현·검증](46-click-work-and-automation.md).


## 작업 목록
- [x] Header Menu Loot 툴팁 추가
- [x] 유물 ID 기반 ScriptableObject 데이터셋과 최초 생성 코드
- [x] 기존 Relic List Item 프리팹으로 획득 목록 생성
- [x] 데이터 기반 HUD 툴팁과 하루 보상 선택지
- [x] 중복 획득 단계 갱신, 환생/재도전 목록 정리
- [x] C# 컴파일 확인
- [ ] Unity 에셋 생성 및 실제 화면 확인

## 변경 전후
기존에는 Relic 7개가 씬에 미리 존재하고 보유 여부에 따라 활성화했다. 이제 기존 항목은 런타임에 숨기며 모델의 RelicsChanged 이벤트로 획득 ID를 전달받은 목록을 동기화하고, 데이터셋에서 ID를 조회하여 프리팹을 한 번 생성한다. 0.1초 기존 HUD 갱신에서도 보유 상태를 보수한다. 같은 ID의 재획득은 중복 생성하지 않고 현재 단계로 툴팁을 표시한다. 리셋/환생으로 없어진 유물은 항목도 제거한다.

데이터 경로: Assets/_AfterlifePrototype/Resources/Data/RelicCatalog.asset. Unity가 스크립트를 임포트하면 최초 1회 생성하며 기존 에셋은 수정하지 않는다. 플레이 중 임포트했다면 편집 모드 복귀 후 생성한다. 필요 시 Tools > Afterlife > Create Missing Relic Catalog 메뉴 사용.

Entries는 Id(RelicKind enum의 기존 값), DisplayName, Icon, TooltipTemplate을 갖는다. TooltipTemplate의 {name}, {level}, {effect}가 표시 이름, 현재 단계, 실제 모델 효과 설명으로 치환된다. 효과 계산은 모델에 유지하여 툴팁 수치와 실제 효과가 따로 변경되는 일을 피한다. 이름·아이콘·문구를 바꾸려면 데이터셋, 크기·레이아웃을 바꾸려면 기존 Prefabs/Relic List Item.prefab을 편집한다.

PrototypeCanvas의 RelicCatalog/RelicRoot는 명시적으로 지정할 수 있다. 미지정 시 Resources 데이터셋과 기존 Relic 바인딩 부모(Header/Relics)를 사용한다. 씬을 자동 저장하거나 기존 프리팹 스타일/아이콘 임포트 설정을 덮어쓰는 도구는 추가하지 않는다.

## 검증
C# dotnet build --no-restore 오류·경고 0. Unity 테스트/플레이 및 Windows 빌드는 실행하지 않는다. 에셋 실제 생성과 화면은 별도 확인이 필요하다.

직접 확인: Loot 호버 시 전리품/결산 설명 표시 → 상자 또는 하루 보상으로 유물 획득 시 프리팹 항목 1개 추가 → 호버 시 데이터의 이름/현재 단계/효과 표시 → 같은 유물 강화 시 아이콘 개수 유지 → 재도전/환생 시 목록 제거. 데이터셋 아이콘과 TooltipTemplate을 수정하고 새 플레이에서 반영되는지 확인한다.
