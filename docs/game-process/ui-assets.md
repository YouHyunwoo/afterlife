# UI 자산과 제작 기록

> 최신 규칙: 초반의 모든 커서 작업은 대상 직접 클릭 1회로 실행하며, 자동화 강화 후에만 호버 주기 작업이 열린다. 아래의 시작부터 자동 기여 설명/과거 QA는 변경 전 기록이다. [최신 규칙·구현·검증](46-click-work-and-automation.md).


## 시각 기준

사용자 지정: 남색·황동·청록, 복잡한 장식 대신 16-bit 픽셀 아트. Slotbound Demo의 명확한 프레임, 아이콘과 수치의 결합, 정보 계층을 참고했다. 게임 자체 자산은 복사하지 않았다. 참고 스크린샷은 `reference/slotbound-01.jpg`에 보관한다.

## 생성 자산

built-in imagegen으로 제작했고 게임 프로젝트의 `Assets/_AfterlifePrototype/Resources/UI/`에 저장했다. `Icons16.png`, `PanelFrame16.png`, `LootIcon16.png`를 실제 UI가 로드한다. 초기 세밀한 버전 `Icons.png`, `PanelFrame.png`, `LootIcon.png`는 대체 전 이력이며 새 UI에서 사용하지 않는다. 이미지는 글자를 포함하지 않으며 한국어는 Mona12 폰트로 별도 렌더링한다. 텍스처는 Point 필터, 프레임은 9-slice로 표시한다.

### Icons16.png · 최종 프롬프트

> Use case stylized-concept. A production SPRITE ATLAS for an original 16-bit pixel art medieval village defense game. EXACTLY 4 columns by 4 rows, 16 equal square cells on a genuinely transparent square canvas. Each symbol centered within its cell and with 20% padding; no dividers, no words. VERY SIMPLE 24x24 sprite aesthetic enlarged by nearest neighbor, SNES era game UI, clearly visible square pixel clusters, 8-12 colors per icon, NO photorealism, NO fine textures, NO antialiasing, NO gradients, NO ornate decoration. Shared dark navy 1-pixel outlines, brass highlights, leather ochre, desaturated teal accents, ivory, slate. Shared top-left lighting. Items in reading order: row1 three wooden logs, a single gray stone, three gold coins, a very simple hooded peasant head; row2 tiny red-roof house, three wheat stalks, market stall with teal striped canopy, simple crossed sword and shield; row3 open book with small lens, altar with teal flame, wooden arrow tower, stone tower with teal crystal; row4 three crenellated wall blocks, simple crescent moon with small sun, brass gear menu icon, a closed padlock with a small ivory camera crosshair. Strong simple silhouettes recognizable at 24 pixels, consistent icon scale and pixel grid. Original assets for Afterlife. NO florid heraldry, NO facial detail, NO smoke or effects escaping cell boundaries.

### PanelFrame16.png · 최종 프롬프트

> Use case stylized-concept. Production UI 9-slice square panel border sprite for an original polished 16-bit pixel art medieval village defense game. Square canvas, frame exactly along all four outside edges. Thin, VERY SIMPLE stepped pixel border: 1 pixel deep navy outer outline, 2 pixel muted brass band with a 1 pixel ivory top highlight, 1 pixel dark inner line. Each corner has one SMALL square teal stud, no ornate jewelry. Art should look like a 64x64 pixel sprite cleanly enlarged nearest neighbor. Flat limited palette maximum 8 colors; identical corner style, straight uninterrupted edges, simple geometric bevel only. Empty transparent center and transparent outside any stepped corners. Outer 12.5 percent of canvas contains all frame decoration so it can be nine-sliced. NO texture noise, NO gradients, NO glow, NO blur, NO text, NO icons, NO gold filigree, NO realistic metal shading. Restrained navy / antique brass / teal 16-bit RPG interface. The empty middle is for legible game text.

### LootIcon.png · 초기 버전 프롬프트

> Use case: stylized-concept. Production game UI sprite for Afterlife, an original medieval fantasy pixel art village defense game. Single loot satchel icon, isolated transparent background, square canvas. Chunky readable 32x32-style pixel clusters scaled up cleanly, warm brown leather pouch with a brass clasp, a small pale bone and rolled parchment peeking from opening, one tiny teal jewel accent. Three-quarter front view, light from upper left, dark navy one-pixel outline, tightly limited warm brass / leather / ivory / teal palette. Strong recognizable silhouette readable at 40 pixels. Icon centered with generous 15% transparent margin. NO text, NO letters, NO numbers, NO UI panel or frame, NO glossy 3D rendering, NO tiny decoration, NO blur, NO ground shadow outside the icon. Match a polished commercial pixel art indie game interface.

### LootIcon16.png · 최종 프롬프트

> Use case stylized-concept. A single SIMPLE 16-bit game loot bag pixel sprite on true transparent background. Render as a TINY 24 by 24 pixel icon enlarged with nearest-neighbor: big square pixel clusters, maximum eight flat colors. One squat brown leather pouch silhouette, a single golden rectangular buckle, simple tied top and a tiny ivory bone sticking out. Brass, brown, deep navy outline, one teal pixel accent. Flat broad shapes with only 2 shadow tones. No leather texture, no folds, no straps or dangling ornaments, no stitching, no little highlights, no realistic lighting, no gradients, no antialiasing, no ground. Center icon with 15 percent empty transparent margin. Match a restrained SNES-style medieval fantasy resource icon set. No text, no frame, no multiple objects arranged separately.

생성 이미지의 넓은 투명 여백은 런타임 UV로 제외해 다른 아이콘과 표시 크기를 맞췄다. PNG 원본을 외부 편집기로 변형하지 않았다.

## Kenney 입력 표시

- 요청 원본: https://kenney.nl/assets/input-prompts — 공식 1.5 ZIP 다운로드, `reference/kenney/input-prompts-1.5.zip`.
- 적용한 공식 픽셀 버전: https://kenney.nl/assets/input-prompts-pixel — 16×16 Input Prompts Pixel 1.0, CC0.
- ESC: `Tiles/tile_0017.png` → `Resources/UI/Input/Escape.png`.
- 가운데 마우스: `Tiles/tile_0079.png` → `Resources/UI/Input/MouseMiddle.png`.
- 원본 그대로 복사해 Point 필터로 표시한다. 동일 폴더의 `License.txt`에 제작자·라이선스를 보존한다. 전체 팩은 참고 폴더에 보존하고 게임에는 필요한 두 아이콘만 사용한다.

## 공통 피드백

호버 청록 강조, 누를 때 2 논리 픽셀 이동, 클릭 테두리 0.18초 강조와 짧은 효과음, 모달 등장 0.18초 이동·배경 암전, 건설 패널 등장 0.18초 이동. 비활성 상태는 배경과 테두리로 구분하되 글자를 읽을 수 있게 유지한다. 상시 번쩍임은 사용하지 않는다.
