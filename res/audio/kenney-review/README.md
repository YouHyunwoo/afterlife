# Kenney 효과음 청음 후보

## 작업 목록
- [x] 공식 오디오 8개 팩 다운로드 및 원본/라이선스 보존
- [x] 개별 오디오 디코딩·길이·음량 검사
- [x] 용도별 후보 분리 및 WAV 청음본 생성
- [x] 선택/비교/내보내기 청음 페이지
- [ ] 사용자 청음 및 최종 선택

## 기준
청각적 청취 평가를 했다고 주장하지 않습니다. Kenney 원본 파일의 의미·팩 분류와 디코딩/길이/음량 검사로 추린 1차 후보입니다. A는 용도상 먼저 비교할 후보이며 최종 추천 확정은 아닙니다.
원본 OGG는 바이트 그대로 복사했습니다. WAV는 피크 -6 dBFS 목표, 증폭 최대 +12 dB로 음량만 조절한 16-bit 청음본입니다. 컷/피치/속도 변경, 합성 없음. 체감 음량을 완전히 같게 만든 것은 아닙니다.
명시적 활/화염/붕괴/뿔피리 전용음이 부족한 용도는 대체 후보로 표시했습니다. 승패 징글의 감정 배정은 청음 후 결정합니다.

## 사용법
index.html을 브라우저로 열거나 로컬 서버의 청음 페이지에서 재생합니다. 그룹별 A→B→C 비교, 3회 반복, 원본/WAV 전환, 선택, 메모를 지원합니다.
선택은 같은 브라우저/주소에 자동 보관됩니다. 「선택 JSON 저장」으로 확정 기록을 파일로 보관하세요. 서버나 게임에는 자동 반영되지 않습니다.
전체 원본 탭에서 미선정 소리도 재생하고 추가 후보로 표시할 수 있습니다.

## 후보 목록

### 01. 나무 채집
도끼·목재 계열 비교. 반복 작업의 기본음 후보.

- **01A**: `rpg-audio/chop.ogg` · 0.24초 · [WAV](candidates/01_나무 채집/01A_chop.wav)
- **01B**: `impact-sounds/impactWood_medium_000.ogg` · 0.333초 · [WAV](candidates/01_나무 채집/01B_impactWood_medium_000.wav)
- **01C**: `impact-sounds/impactWood_light_002.ogg` · 0.266초 · [WAV](candidates/01_나무 채집/01C_impactWood_light_002.wav)

### 02. 돌 채집
채광 전용 파일을 우선 비교.

- **02A**: `impact-sounds/impactMining_000.ogg` · 0.937초 · [WAV](candidates/02_돌 채집/02A_impactMining_000.wav)
- **02B**: `impact-sounds/impactMining_002.ogg` · 0.805초 · [WAV](candidates/02_돌 채집/02B_impactMining_002.wav)
- **02C**: `impact-sounds/impactMining_004.ogg` · 0.83초 · [WAV](candidates/02_돌 채집/02C_impactMining_004.wav)

### 03. 자원 소진
전용 파괴음은 없어 강한 목재·파열음을 대체 후보로 제시.

- **03A**: `impact-sounds/impactWood_heavy_000.ogg` · 0.313초 · [WAV](candidates/03_자원 소진/03A_impactWood_heavy_000.wav)
- **03B**: `sci-fi-sounds/explosionCrunch_000.ogg` · 0.777초 · [WAV](candidates/03_자원 소진/03B_explosionCrunch_000.wav)

### 04. 골드 획득·정산
실제 동전 계열 우선. 자주 재생할 때 길이 확인.

- **04A**: `rpg-audio/handleCoins.ogg` · 0.846초 · [WAV](candidates/04_골드 획득_정산/04A_handleCoins.wav)
- **04B**: `rpg-audio/handleCoins2.ogg` · 0.338초 · [WAV](candidates/04_골드 획득_정산/04B_handleCoins2.wav)
- **04C**: `casino-audio/chips-collide-1.ogg` · 0.259초 · [WAV](candidates/04_골드 획득_정산/04C_chips-collide-1.wav)

### 05. 건물 배치
판자·목재 접촉으로 배치 확정 표현.

- **05A**: `impact-sounds/impactPlank_medium_000.ogg` · 0.779초 · [WAV](candidates/05_건물 배치/05A_impactPlank_medium_000.wav)
- **05B**: `impact-sounds/impactWood_medium_002.ogg` · 0.333초 · [WAV](candidates/05_건물 배치/05B_impactWood_medium_002.wav)

### 06. 공사·수리 기여
가벼운 목재/금속 타격을 망치질 대용으로 비교.

- **06A**: `impact-sounds/impactWood_light_000.ogg` · 0.266초 · [WAV](candidates/06_공사_수리 기여/06A_impactWood_light_000.wav)
- **06B**: `impact-sounds/impactMetal_light_001.ogg` · 0.252초 · [WAV](candidates/06_공사_수리 기여/06B_impactMetal_light_001.wav)
- **06C**: `impact-sounds/impactPlank_medium_002.ogg` · 0.779초 · [WAV](candidates/06_공사_수리 기여/06C_impactPlank_medium_002.wav)

### 07. 완공·병력 생산 완료
짧은 확인음과 상승 계열 비교.

- **07A**: `interface-sounds/confirmation_001.ogg` · 0.29초 · [WAV](candidates/07_완공_병력 생산 완료/07A_confirmation_001.wav)
- **07B**: `interface-sounds/confirmation_002.ogg` · 0.539초 · [WAV](candidates/07_완공_병력 생산 완료/07B_confirmation_002.wav)
- **07C**: `digital-audio/powerUp1.ogg` · 1.202초 · [WAV](candidates/07_완공_병력 생산 완료/07C_powerUp1.wav)

### 08. 연구·타워 강화
상승형 효과와 확인음을 비교. 전자음의 세계관 적합성 확인.

- **08A**: `digital-audio/powerUp3.ogg` · 1.149초 · [WAV](candidates/08_연구_타워 강화/08A_powerUp3.wav)
- **08B**: `interface-sounds/confirmation_004.ogg` · 0.49초 · [WAV](candidates/08_연구_타워 강화/08B_confirmation_004.wav)
- **08C**: `interface-sounds/maximize_001.ogg` · 0.258초 · [WAV](candidates/08_연구_타워 강화/08C_maximize_001.wav)

### 09. 커서·근접 적중
펀치·부드러운 충격 계열을 일반 적중음으로 비교.

- **09A**: `impact-sounds/impactPunch_medium_000.ogg` · 0.431초 · [WAV](candidates/09_커서_근접 적중/09A_impactPunch_medium_000.wav)
- **09B**: `impact-sounds/impactPunch_heavy_000.ogg` · 0.649초 · [WAV](candidates/09_커서_근접 적중/09B_impactPunch_heavy_000.wav)
- **09C**: `impact-sounds/impactSoft_medium_000.ogg` · 0.118초 · [WAV](candidates/09_커서_근접 적중/09C_impactSoft_medium_000.wav)

### 10. 무기 휘두르기
칼날 동작 계열. 실제 적중음과 별도 역할.

- **10A**: `rpg-audio/knifeSlice.ogg` · 0.6초 · [WAV](candidates/10_무기 휘두르기/10A_knifeSlice.wav)
- **10B**: `rpg-audio/knifeSlice2.ogg` · 0.569초 · [WAV](candidates/10_무기 휘두르기/10B_knifeSlice2.wav)
- **10C**: `rpg-audio/drawKnife1.ogg` · 0.4초 · [WAV](candidates/10_무기 휘두르기/10C_drawKnife1.wav)

### 11. 화살 발사·명중 대체
확인한 팩에 명시적 활 발사음이 없어 대체 후보만 제시. 전용 활 소리는 후속 보강 권장.

- **11A**: `rpg-audio/cloth1.ogg` · 0.661초 · [WAV](candidates/11_화살 발사_명중 대체/11A_cloth1.wav)
- **11B**: `rpg-audio/knifeSlice2.ogg` · 0.569초 · [WAV](candidates/11_화살 발사_명중 대체/11B_knifeSlice2.wav)
- **11C**: `impact-sounds/impactWood_light_001.ogg` · 0.266초 · [WAV](candidates/11_화살 발사_명중 대체/11C_impactWood_light_001.wav)

### 12. 마법 발사
SF/디지털 효과를 마법으로 전용할지 비교.

- **12A**: `digital-audio/zap1.ogg` · 1.019초 · [WAV](candidates/12_마법 발사/12A_zap1.wav)
- **12B**: `sci-fi-sounds/laserSmall_000.ogg` · 0.239초 · [WAV](candidates/12_마법 발사/12B_laserSmall_000.wav)
- **12C**: `digital-audio/phaserUp1.ogg` · 0.496초 · [WAV](candidates/12_마법 발사/12C_phaserUp1.wav)

### 13. 마법 명중·발화
전용 판타지 화염이 아닌 파열·추진 불꽃 계열 대체 후보.

- **13A**: `sci-fi-sounds/explosionCrunch_002.ogg` · 1.256초 · [WAV](candidates/13_마법 명중_발화/13A_explosionCrunch_002.wav)
- **13B**: `sci-fi-sounds/thrusterFire_000.ogg` · 5.0초 · [WAV](candidates/13_마법 명중_발화/13B_thrusterFire_000.wav)
- **13C**: `digital-audio/zap2.ogg` · 1.228초 · [WAV](candidates/13_마법 명중_발화/13C_zap2.wav)

### 14. 적 처치·슬라임
슬라임 효과와 일반적인 충격음을 비교. 종별 전용 사망음은 아님.

- **14A**: `sci-fi-sounds/slime_000.ogg` · 0.5초 · [WAV](candidates/14_적 처치_슬라임/14A_slime_000.wav)
- **14B**: `sci-fi-sounds/slime_001.ogg` · 4.71초 · [WAV](candidates/14_적 처치_슬라임/14B_slime_001.wav)
- **14C**: `impact-sounds/impactSoft_heavy_000.ogg` · 0.505초 · [WAV](candidates/14_적 처치_슬라임/14C_impactSoft_heavy_000.wav)

### 15. 치명타·강한 타격
일반 적중보다 강한 후보. 아군 손실 경고와 중복 사용은 피하기.

- **15A**: `impact-sounds/impactPunch_heavy_002.ogg` · 0.457초 · [WAV](candidates/15_치명타_강한 타격/15A_impactPunch_heavy_002.wav)
- **15B**: `impact-sounds/impactMetal_heavy_000.ogg` · 0.168초 · [WAV](candidates/15_치명타_강한 타격/15B_impactMetal_heavy_000.wav)

### 16. 건물 피해·붕괴
무거운 목재 충격과 파열음을 비교. 완전한 붕괴 녹음은 아님.

- **16A**: `impact-sounds/impactWood_heavy_002.ogg` · 0.313초 · [WAV](candidates/16_건물 피해_붕괴/16A_impactWood_heavy_002.wav)
- **16B**: `sci-fi-sounds/explosionCrunch_004.ogg` · 1.98초 · [WAV](candidates/16_건물 피해_붕괴/16B_explosionCrunch_004.wav)

### 17. 상자 잠금·개봉
잠금쇠와 문 여는 소리를 상자 연출에 전용하는 후보.

- **17A**: `rpg-audio/metalLatch.ogg` · 0.261초 · [WAV](candidates/17_상자 잠금_개봉/17A_metalLatch.wav)
- **17B**: `rpg-audio/doorOpen_1.ogg` · 0.919초 · [WAV](candidates/17_상자 잠금_개봉/17B_doorOpen_1.wav)
- **17C**: `rpg-audio/creak1.ogg` · 0.661초 · [WAV](candidates/17_상자 잠금_개봉/17C_creak1.wav)

### 18. 유물 획득·유적 활성화
유리·벨 계열과 상승음을 비교.

- **18A**: `interface-sounds/glass_001.ogg` · 0.278초 · [WAV](candidates/18_유물 획득_유적 활성화/18A_glass_001.wav)
- **18B**: `impact-sounds/impactBell_heavy_000.ogg` · 1.48초 · [WAV](candidates/18_유물 획득_유적 활성화/18B_impactBell_heavy_000.wav)
- **18C**: `digital-audio/powerUp7.ogg` · 0.522초 · [WAV](candidates/18_유물 획득_유적 활성화/18C_powerUp7.wav)

### 19. 확인·선택·병력 명령
일상 클릭은 이 그룹에서 하나로 통일하는 안.

- **19A**: `interface-sounds/click_001.ogg` · 0.1초 · [WAV](candidates/19_확인_선택_병력 명령/19A_click_001.wav)
- **19B**: `interface-sounds/select_002.ogg` · 0.043초 · [WAV](candidates/19_확인_선택_병력 명령/19B_select_002.wav)
- **19C**: `ui-audio/click1.ogg` · 0.094초 · [WAV](candidates/19_확인_선택_병력 명령/19C_click1.wav)

### 20. 취소·닫기
메뉴 닫기·공사 취소 공통 후보.

- **20A**: `interface-sounds/back_001.ogg` · 0.064초 · [WAV](candidates/20_취소_닫기/20A_back_001.wav)
- **20B**: `interface-sounds/close_001.ogg` · 0.148초 · [WAV](candidates/20_취소_닫기/20B_close_001.wav)
- **20C**: `ui-audio/switch2.ogg` · 0.297초 · [WAV](candidates/20_취소_닫기/20C_switch2.wav)

### 21. 메뉴 열기·탭 전환
건설 메뉴·관리창 열기 및 설정 탭 전환.

- **21A**: `interface-sounds/open_001.ogg` · 0.148초 · [WAV](candidates/21_메뉴 열기_탭 전환/21A_open_001.wav)
- **21B**: `interface-sounds/switch_001.ogg` · 0.618초 · [WAV](candidates/21_메뉴 열기_탭 전환/21B_switch_001.wav)
- **21C**: `rpg-audio/bookOpen.ogg` · 0.154초 · [WAV](candidates/21_메뉴 열기_탭 전환/21C_bookOpen.wav)

### 22. 골드 부족·행동 불가
거절 시점에 한 번만 재생.

- **22A**: `interface-sounds/error_001.ogg` · 0.165초 · [WAV](candidates/22_골드 부족_행동 불가/22A_error_001.wav)
- **22B**: `interface-sounds/error_004.ogg` · 0.103초 · [WAV](candidates/22_골드 부족_행동 불가/22B_error_004.wav)
- **22C**: `digital-audio/lowDown.ogg` · 0.784초 · [WAV](candidates/22_골드 부족_행동 불가/22C_lowDown.wav)

### 23. 황혼·웨이브 경고
벨·낮은 다중 음 후보. 전용 뿔피리 소리는 포함되지 않음.

- **23A**: `impact-sounds/impactBell_heavy_003.ogg` · 0.654초 · [WAV](candidates/23_황혼_웨이브 경고/23A_impactBell_heavy_003.wav)
- **23B**: `interface-sounds/bong_001.ogg` · 0.123초 · [WAV](candidates/23_황혼_웨이브 경고/23B_bong_001.wav)
- **23C**: `digital-audio/lowThreeTone.ogg` · 1.019초 · [WAV](candidates/23_황혼_웨이브 경고/23C_lowThreeTone.wav)

### 24. 하루 시작·완료 징글
피치카토/8비트/스틸 음색 비교. 곡의 감정은 직접 청음 후 배정.

- **24A**: `music-jingles/jingles_PIZZI00.ogg` · 0.494초 · [WAV](candidates/24_하루 시작_완료 징글/24A_jingles_PIZZI00.wav)
- **24B**: `music-jingles/jingles_NES00.ogg` · 1.758초 · [WAV](candidates/24_하루 시작_완료 징글/24B_jingles_NES00.wav)
- **24C**: `music-jingles/jingles_STEEL00.ogg` · 0.933초 · [WAV](candidates/24_하루 시작_완료 징글/24C_jingles_STEEL00.wav)

### 25. 승리·패배 징글 후보
번호별 선율 비교용. 승리/패배 배정은 청음 후 결정.

- **25A**: `music-jingles/jingles_PIZZI07.ogg` · 1.324초 · [WAV](candidates/25_승리_패배 징글 후보/25A_jingles_PIZZI07.wav)
- **25B**: `music-jingles/jingles_PIZZI10.ogg` · 0.797초 · [WAV](candidates/25_승리_패배 징글 후보/25B_jingles_PIZZI10.wav)
- **25C**: `music-jingles/jingles_PIZZI15.ogg` · 0.811초 · [WAV](candidates/25_승리_패배 징글 후보/25C_jingles_PIZZI15.wav)

### 26. 환생·재출발
공간 전환·역장 계열 대체 후보. SF 느낌이 강하면 제외.

- **26A**: `digital-audio/phaseJump1.ogg` · 0.47초 · [WAV](candidates/26_환생_재출발/26A_phaseJump1.wav)
- **26B**: `digital-audio/phaserDown1.ogg` · 0.47초 · [WAV](candidates/26_환생_재출발/26B_phaserDown1.wav)
- **26C**: `sci-fi-sounds/forceField_000.ogg` · 0.954초 · [WAV](candidates/26_환생_재출발/26C_forceField_000.wav)

## 출처 및 라이선스
각 ZIP에 동봉된 License.txt를 originals 아래 보존했습니다. 모두 CC0 표기를 확인했습니다.

- [impact-sounds](https://kenney.nl/assets/impact-sounds)
- [rpg-audio](https://kenney.nl/assets/rpg-audio)
- [interface-sounds](https://kenney.nl/assets/interface-sounds)
- [music-jingles](https://kenney.nl/assets/music-jingles)
- [casino-audio](https://kenney.nl/assets/casino-audio)
- [ui-audio](https://kenney.nl/assets/ui-audio)
- [digital-audio](https://kenney.nl/assets/digital-audio)
- [sci-fi-sounds](https://kenney.nl/assets/sci-fi-sounds)

## 변경 전후
이전: 효과음 적용 지점 목록만 존재. 이후: 원본 팩, 분리 후보, 청음용 WAV, 선택 페이지 및 출처 기록 추가. 게임 코드/Unity Assets/씬은 변경하지 않았습니다.