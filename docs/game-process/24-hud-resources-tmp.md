# 24 HUD Resources TMP 전환

> 확정 기획 · 2026-10-03: 채집·건설·수리·공격은 버튼을 누르고 있는 동안 작업 주기에 맞춰 반복하고, 강화는 작업 1회당 작업량을 높인다. 자동 수입은 농장 건설·생산 강화로 확장한다. 본편 상호작용 필드·슬롯·충전·호버 자동 작업은 폐기한다. [새 규칙·사용법·검증](71-click-work-and-farm-automation.md). 새 규칙은 게임 코드·에셋에 아직 적용하지 않았다. 이전 규칙·수치·QA는 당시 기록으로 보존한다.




현재: M5 UI 편집 개선 / 변환 코드·메뉴·컴파일 완료. 사용자가 에디터에서 확인한 뒤 빌드하기로 함.

## 작업 목록
- [x] Resources 변환 코드 구현 (동일 오브젝트·RectTransform·스타일·Binding 보존)
- [x] Mona12TextKR.ttf 기반 동적 SDF TMP Font Asset 생성 메뉴 구현
- [ ] 열린 Unity에서 메뉴 실행하여 실제 Font Asset 생성·씬 변환
- [x] 기존 Text/TMP 혼용 갱신과 TMP 어셈블리 참조 연결
- [x] 에디터 변환 메뉴와 반복 실행 시 기존 편집 보존
- [x] C# 컴파일·진행 문서 갱신
- [ ] 에디터에서 표시 확인 후 사용자 씬 저장 (Windows 빌드 미요청)

범위는 HUD Resources의 골드와 인구/병력 표시다. 다른 창/배너/월드 기여 숫자는 이번에 변환하지 않는다. 동일 RectTransform과 오브젝트를 유지하며 Text 컴포넌트만 교체한다. TMP Font Asset은 원본 Mona12TextKR.ttf를 참조하는 Dynamic SDF, 한글/숫자 HUD 문자를 미리 추가하고 다중 Atlas를 지원한다.


## 사용 절차
1. Unity에서 Afterlife_PixelPrototype 씬을 열고 스크립트 컴파일이 끝날 때까지 기다린다. 플레이 모드에서는 변환하지 않는다.
2. Tools > Afterlife > Convert HUD Resources to TMP 실행.
3. Resources 내부 Text만 TextMeshProUGUI로 변환된다. Canvas의 ResourcesTmpFont와 HUD.Money/HUD.Population 바인딩의 TmpText가 연결된다. 기존 Font/Text 참조는 나머지 UI용으로 유지한다.
4. Inspector의 TextMeshPro - Text (UI)에서 폰트/크기/색/정렬을 수정한다. 변환은 Undo 지원, 현재 씬은 자동 저장하지 않는다. 결과를 보고 씬을 저장한다.
5. 생성 위치: Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR SDF.asset. 원본 ttf, 생성 Material/Atlas 참조를 포함한다. 샘플 크기 64, Padding 8, SDFAA, 1024×1024, Dynamic, Multi Atlas. 숫자/영문/필요 한글을 미리 추가하고 새로운 글자는 원본 폰트에서 추가한다.

## 구현 상세
- PrototypeUiBinding에 TMP_Text 참조 추가. Label 갱신은 TMP 우선, 기존 Text fallback. 골드 숫자 애니메이션과 인구 갱신은 같은 Label API로 유지한다.
- Runtime/Editor asmdef 및 컴파일 스크립트에 Unity.TextMeshPro 참조 추가. 새 패키지 설치 없이 프로젝트의 기존 TMP를 사용한다.
- Unity 패키지의 TMP_FontAsset.CreateFontAsset와 에디터 폰트 생성 코드를 확인해 생성 API와 Material/Atlas 하위 에셋 저장을 연결했다. .meta는 직접 작성하지 않는다.
- 위치/앵커/피벗/크기/회전/스케일을 명시적으로 복원하고 글자/색/크기/정렬/굵기·기울임/자동 크기/줄바꿈/잘림/입력 통과를 복사한다. TMP와 기존 Text의 글꼴 메트릭 차이는 실제 확인이 필요하다.
- 생성된 폰트와 이미 TMP인 컴포넌트는 반복 실행 때 스타일을 덮어쓰지 않는다. 기존 TMP의 누락 바인딩만 연결한다. 일반 Install Scene UI에도 연결해 후속 설치 시 누락 변환을 처리한다.

## 현재 증거와 미완료
- dotnet build --no-restore 오류 0, 경고 0.
- 사용자가 “아직 빌드하진 않을거야 수정된거 보고”라고 답했으므로 Windows 빌드/에디터 종료/배치 실행은 하지 않았다.
- 현재 에디터 직접 조작 연결이 없으므로 전용 메뉴를 사용자가 실행해야 한다. 마지막 파일 확인 시 Mona12TextKR SDF.asset은 아직 존재하지 않으며, 실제 씬 변환/Font Asset 생성 완료로 보고하지 않는다.
- 실제 표시 확인: Resources의 골드/주민·병력에 TMP 컴포넌트가 있고 같은 위치/크기인지, 한글·숫자 누락 없이 갱신되는지, 메뉴/일차 배너는 기존 Text로 유지되는지 확인한다.
- Windows 최신 실행 파일은 여전히 23 빌드이며 이번 TMP 변경이 반영된 새 빌드는 없다.
