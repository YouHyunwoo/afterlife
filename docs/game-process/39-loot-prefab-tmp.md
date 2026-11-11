# 39 · Loot 프리팹 TMP와 팝업 교체

## 작업 목록
- [x] Modal/Loot 프리팹과 HUD/팝업 바인딩 확인
- [x] Items 하위 Text 11개 TMP 변환 코드
- [x] 프리팹 내부 참조 저장 및 기존 팝업 교체 코드
- [x] 합계/전리품/귀중품/빈 목록/닫기 연결
- [x] C# 컴파일 검증
- [ ] Unity 실제 프리팹/씬 적용 및 화면 확인

사용자 Loot 프리팹은 Prefabs/Modal/Loot.prefab이며 Common/Titled View 기반 팝업이다. Header의 작은 HUD.Loot 버튼은 새 Modal.Loot 프리팹을 여는 연결을 유지한다.

PrototypeLootPrefabInstaller가 Items 하위 9종 전리품, 귀중품, 빈 목록 안내의 legacy Text를 기존 오브젝트에서 TMP로 교체한다. 기존 TMP는 유지하며 폰트는 Mona12TextKR SDF, RectTransform/색/크기/정렬/줄바꿈 복사. 이미 TMP인 합계와 닫기 버튼을 포함한 참조는 PrototypeLootPanel.Bindings에 저장한다. 기존 공통 Titled View 원본의 스타일은 변경하지 않는다.

열린 씬의 Modal.Loot가 다른 UI이면 동일 부모/형제 순서에 Loot 프리팹을 넣고 바인딩과 카메라 가드를 갱신한 뒤 기존 팝업을 제거한다. 프리팹이 정의한 크기·배치를 사용한다. Undo 가능한 교체이며 자동 씬 저장 없음. 이미 해당 프리팹이면 재생성 없이 누락 연결만 보완한다. 런타임 Bind에서도 프리팹 내부 직렬화 참조로 HUD 바인딩을 연결하므로 이름 변경에 의존하지 않는다.

스크립트 임포트/편집 모드 복귀/씬 열기 시 실행. 수동 메뉴 Tools > Afterlife > Apply Loot Prefab. 프리팹 에셋 변환은 Unity API로 저장하고 .meta를 직접 변경하지 않는다.

검증: C# dotnet build --no-restore 오류·경고 0. 실제 Unity 프리팹 변환/씬 교체/플레이 및 Windows 빌드 미실시. 직접 확인: 임포트 완료 후 프리팹 Items의 TMP 11개 확인 → 씬 저장 → Loot 버튼으로 새 팝업 열기 → 보유 전리품 행/수량/합계/빈 목록 전환 → 닫기 버튼 및 ESC 확인.
