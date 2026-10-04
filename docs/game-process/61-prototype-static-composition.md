# 61 · 프로토타입 고정 오브젝트의 편집 모드 구성

> 확정 기획 · 2026-10-03: 채집·건설·수리·공격은 버튼을 누르고 있는 동안 작업 주기에 맞춰 반복하고, 강화는 작업 1회당 작업량을 높인다. 자동 수입은 농장 건설·생산 강화로 확장한다. 본편 상호작용 필드·슬롯·충전·호버 자동 작업은 폐기한다. [새 규칙·사용법·검증](71-click-work-and-farm-automation.md). 새 규칙은 게임 코드·에셋에 아직 적용하지 않았다. 이전 규칙·수치·QA는 당시 기록으로 보존한다.


- 날짜: 2026-09-27
- 작업 목록: 런타임 고정 오브젝트 조사 → 프리팹·씬 배치 → 직렬화 참조 연결 → C# 빌드와 에디터 참조 확인 → 플레이 확인 절차 기록.

## 변경 전후

기존 프로토타입은 `AudioRoot`와 게임 Canvas가 씬에 있어도 환경음 채널 7개, 영력 HUD, 환생 기록, 강화 트리의 고정 틀, 스킬 바, 모달 캡처 카메라와 흙먼지 ParticleSystem을 시작 시 생성했다. 편집자가 하이어라키에서 스타일·배치·참조를 확인하기 어려웠고, 일부 컴포넌트는 화면을 열 때 추가됐다.

이제 다음을 편집 모드에서 관리한다.

| 위치 | 편집 대상 | 런타임 책임 |
| --- | --- | --- |
| `PrototypeAudioRoot.prefab` / 씬 `AudioRoot` | 기존 효과음 12채널과 풀·새·타격·파괴·물결 7채널 | 클립 선택, 볼륨, 재생·정지 |
| 씬 `Canvas` | `Divine Power`, `Day Start Banner/Label`의 CanvasGroup, `ModalLayer`의 CanvasGroup | 숫자·문구·표시 상태 |
| `RebirthReview.prefab` / `Canvas/ModalLayer` | 환생 기록 패널과 바인딩 | 모델 값 갱신, 모달 스택 연결 |
| `UpgradeTreeScreen.prefab` / 연구소·제단 | 배경, 뷰포트, 루트·파티클, 제목·재화·버튼·범례·툴팁 | 카탈로그 노드·연결선 생성, 구매·호버·줌 |
| `AltarSkillBar.prefab` / 씬 Canvas | Q/W/E 버튼과 TMP 레이블 | 해금·쿨다운·문구·입력 |
| `ModalUiCaptureCamera.prefab` / 씬 루트 | 비활성 캡처 카메라 및 URP 설정 | 해상도별 RenderTexture·블러 Material 생성과 해제 |
| `HoverDirtDust.prefab` / `GameRoot` | ParticleSystem 설정 | 생물군계에 따른 방출과 셰이더 Material |

`PrototypeView`, `PrototypeAudio`, `PrototypeModalBackdrop`, `UpgradeTreeScreen`, `UpgradeHoverTooltip`의 Inspector 참조가 프리팹/씬 인스턴스를 연결한다. 씬 소유 오디오·카메라·UI·ParticleSystem은 사용 종료 시 파괴하지 않는다. 공유 `AmbientAudioController`, `WorldAudioPlayer`, `TerrainHoverEffects`는 주입 생성자를 제공하고 기존 소비자를 위한 생성자도 유지한다. 데이터량이나 화면 크기에 따라 달라지는 맵·강화 노드/선·영력 필드 링·렌더 텍스처·툴팁 값은 런타임에서 갱신하거나 생성한다. `Build.Item`의 Outline은 프리팹 필수 구성으로 확인한다.

## 편집 방법

1. 환경음 채널 수와 기본 `AudioSource` 설정은 `PrototypeAudioRoot.prefab`에서 편집한다. 씬 `AudioRoot`의 `PrototypeAudio` 참조 4개와 물결 3개를 유지한다. 음원 데이터는 Audio 설정의 `WorldBank` 또는 `Resources/WorldAudio/WorldAudioBank` 에셋에 둔다.
2. 스킬 버튼 배치·색은 `AltarSkillBar.prefab`, 강화 화면의 고정 틀·툴팁은 `UpgradeTreeScreen.prefab`에서 편집한다. 노드 좌표·부모·가격·제목은 기존 `ProgressionCatalog`에서 편집한다.
3. 환생 기록의 바인딩 ID는 `Modal.RebirthReview`, `RebirthReview.*`를 유지한다. 화면 블러용 카메라는 월드 카메라와 별개이며 비활성 상태로 둔다.
4. `GameRoot`의 `PrototypeView`와 Canvas의 `PrototypeModalBackdrop` 직렬화 참조를 제거하지 않는다. 프리팹을 교체할 때는 Inspector의 연결 상태를 다시 확인한다.

## 검증

`Afterlife.Prototype.Editor.csproj`의 `dotnet build --no-restore`에서 오류·경고 0. Unity CLI로 열린 에디터의 씬을 검사한 결과 오디오 7개, 뷰 참조, 모달/배너 그룹, 연구소·제단 트리 2개와 각 프리팹 내부 참조, 환생 기록 바인딩 5개가 모두 연결되어 있고 씬이 저장된 상태였다. Unity 테스트와 PlayMode는 저장소 규칙에 따라 실행하지 않았다.

직접 확인할 때는 `Afterlife_PixelPrototype` 씬을 재생해 타이틀에서 게임으로 들어간다. 풀·새·타격·물결을 듣고, Q/W/E 스킬 바 및 영력 HUD를 확인한다. 연구소/제단에서 노드 드래그·줌·툴팁·구매, 제단의 환생 기록, 모달 암전/블러와 일차 배너를 확인한다. 흙 지면 위로 커서를 움직여 먼지 방출을 확인한다. 실행 중 `AmbientGrass`, `RebirthReview`, `UpgradeTreeScreen`, `AltarSkillBar`, `Modal UI Capture Camera`, `HoverDirtDust`가 중복 생성되지 않아야 한다.
