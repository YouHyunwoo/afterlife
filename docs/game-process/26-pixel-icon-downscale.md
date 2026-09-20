# 26 UI 아이콘 실제 해상도 축소

## 작업 목록
- [x] 실제 사용 중인 아이콘과 Sprite 참조 확인
- [x] 고해상도 원본 백업 및 저해상도 PNG 적용
- [x] 내장 imagegen으로 카메라 잠금 아이콘 재생성
- [x] 기존 Sprite 참조 유지용 Unity 임포트 보정 구현
- [x] PNG/미리보기 확인 및 C# 컴파일
- [ ] Unity 임포트 완료와 실제 Game 화면 확인 (사용자 확인)

## 변경 전후
기존 Icons16, LootIcon16, DaySun-v1, NightMoon-v1은 이름이나 생성 프롬프트와 달리 실제로는 모두 1254×1254였다. Point 필터로 확대해도 생성 이미지의 작은 잡음이 남았다.

현재 활성 UI 자산만 축소했다. Icons16은 160×160(셀 40×40, 실제 도형 약 24~32픽셀), LootIcon16은 48×48, 해/달은 각각 32×32다. 원래부터 16×16인 입력 아이콘은 유지했다. 초기 버전 Icons/LootIcon과 패널 프레임, 월드 아트는 대상이 아니다.

면적 평균 축소 후 아이콘별 최대 16색 팔레트, 디더링 없음, 알파 0/255를 적용했다. 확대는 Point 필터로 표시한다. 새 잠금 아이콘은 기존 시트 마지막 셀에 배치해 카메라 버튼과 같은 아이콘을 쓰던 잠금 표시에서도 기존 참조를 유지한다. 별도 CameraLock.png(25×30)도 저장했다.

- 최종 게임 자산: `impls/unity/Afterlife/Assets/_AfterlifePrototype/Resources/UI/`
- 고해상도 원본과 imagegen 출력: `res/art-source/ui/`
- 확대 미리보기: `res/art-source/ui/pixel-icons-preview.png`
- 재현: `python -X utf8 tools/art/build_ui_pixels.py`

## Unity 적용
PrototypeUiPixelImporter는 Unity API로 Point/무압축/mipmap 끔/NPOT 크기 유지/FullRect를 설정한다. 기존 TextureImporter의 Sprite 영역을 1254 기준으로 비례 축소하며 이름과 Sprite ID는 보존한다. 기존 SceneSprites의 16개 아이콘과 Loot도 CopySerialized로 Sprite 데이터만 바꾸고 GUID를 유지한다. 마이그레이션 마커와 크기 검사로 반복 축소를 방지한다. 씬 배치와 사용자 편집은 변경하지 않는다. `.meta`는 직접 수정/생성하지 않았다.

에디터의 다음 스크립트 리로드 후 편집 상태에서 자동 적용한다. 필요하면 `Tools > Afterlife > Apply Pixel Icon Imports` 메뉴를 실행한다. 신규 UI 설치 경로의 Sprite 좌표도 실제 텍스처 크기를 사용한다.

## imagegen 제작
내장 도구 사용. 생성 원본은 CameraLock-generated.png로 보존했다. 사용 프롬프트:

> Use case: stylized-concept. Production game UI camera-lock icon for Afterlife medieval pixel-art village defense game. A single closed brass padlock with a simple ivory camera viewfinder/crosshair badge at the lower right, dark navy outline, slate shackle, restrained brass gold and ivory palette with at most 8 flat colors. TRUE coarse 24x24 pixel sprite aesthetic enlarged using uniform square blocks. Extremely simple readable silhouette, thick connected pixel clusters, only one shadow tone and one highlight tone. Centered on a genuinely transparent background with 12% transparent padding. Front view, square canvas. No text, no letters, no gradients, no antialiasing, no blur, no scattered tiny pixels, no noise, no texture, no soft shadows, no decorative detail. The lock and camera target must remain readable when downscaled to 32x32 actual pixels. Return a single icon, not a sheet.

## 검증
- Build-Prototype.ps1의 `dotnet build --no-restore`: 오류 0, 경고 0.
- 최종 PNG 5개 크기와 알파 0/255 확인. 확대 미리보기에서 형태와 새 잠금 아이콘 확인.
- 완료 시점에 새 importer의 Unity `.meta`와 마이그레이션 마커가 아직 없어 실제 에디터 재임포트는 확인하지 못했다.
- AGENTS.md에 따라 에디터 테스트, PlayMode, Windows 빌드는 실행하지 않았다.
- 직접 확인: 플레이 중이면 종료하고 Unity로 돌아와 임포트/컴파일을 기다린다. 필요하면 위 메뉴 실행. HUD 자원/건설/모집/해·달/카메라/전리품에서 잘림이나 누락 없이 굵고 선명한 픽셀이 보이는지 확인한다. 카메라 버튼과 가운데 마우스 클릭 시 고정/이동이 전환되는지 확인한다.
