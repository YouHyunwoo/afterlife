# 외부 음원 크레딧 지침

## ThresholdLab 모달 효과음 재사용 (2026-09-28)

프로토타입 전리품·설정 등 모달과 타이틀 설정에 기존 `ThresholdAudioBank`의 `WaterEnter`, `WaterExit`, `WaterLoop`를 연결했다. 파일 복제나 추가 편집은 없다. 원본 출처·편집 기록은 Unity의 `Assets/Samples/ThresholdLab/Audio/AudioCredits.txt`에 있다. 물소리 원본은 GoldenTricycle의 Freesound 녹음(CC0 1.0)이며, 현재 기록은 내부 출처 관리다.

## Freesound — 자연 환경음 (2026-09-20)

|용도|저작물 / 저작자|원 배포 페이지|확인한 이용조건|
|---|---|---|---|
|바람 (현재 미사용)|a gentle breeze, wind 1 / mario1298|https://freesound.org/people/mario1298/sounds/181250/|CC0 1.0|
|풀 스침|Dry Grass Rustle.wav / TheSupremeMuffinPooter|https://freesound.org/people/TheSupremeMuffinPooter/sounds/525268/|CC0 1.0|
|새 울음|Bird Chirp / swiftoid|https://freesound.org/people/swiftoid/sounds/182507/|CC0 1.0|

- 라이선스: https://creativecommons.org/publicdomain/zero/1.0/ . 세 배포 페이지의 CC0 표시와 링크를 확인했다. 상업 이용·편집 가능하며 저작자 표시는 필수 조건이 아니다. 추적 가능한 내부 출처로 보존한다.
- 원 자료: `res/audio/nature-ambience/*_hq.mp3`. Freesound 공개 HQ MP3 미리듣기를 내려받았으며 원본 WAV 다운로드가 아니다. 실제 다운로드 URL·저작자·SHA-256은 `sources.json`에 기록했다.
- 풀 스침은 실제 마른 밀풀을 손으로 가볍게 스친 녹음이다. 바람이 풀을 흔드는 현장 녹음으로 오기하지 않는다.
- 편집: 바람 12~52초 발췌/2초 교차 연결, 풀 0.3~10초 발췌/1초 교차 연결/짧은 피크 완화, 새 0~4.727초/80ms 시작·250ms 끝 페이드. 공통 DC 제거·고역통과 필터·음량 조정·44.1kHz/16bit 스테레오 WAV 변환. 상세 수치는 `analysis.json`, 재현은 `prepare.py`.
- Unity: `Assets/Modules/AmbientAudio/Resources/AmbientSounds/{wind,grass,bird}.wav`. 현재 Day01 After에서는 grass(마우스 풀 접촉 중)와 bird(간헐적)만 사용한다. wind는 참조/로딩/재생을 제거했으며 기존 파일은 출처 추적용으로 보관한다. 실제 청취 검증은 대기 중이다.
- 기존 공유마당 CC BY 음원의 공개 표기 의무는 별도로 유지한다.

- 풀 스침 추가 편집: 기존 grass.wav에서 0.10~0.65 / 1.55~2.15 / 4.15~4.85 / 6.45~7.15초를 발췌. 원 MP3 기준 1.40~1.95 / 2.85~3.45 / 5.45~6.15 / 7.75~8.45초. tanh 피크 완화, 35ms/140ms 페이드, RMS -25dBFS·피크 -13dBFS 상한. 현재 풀 재생은 사용자 선택에 따라 `GrassRustles/grass_rustle_02~04.wav`만 사용한다. 01은 미사용 후보로 보관한다. 원래 grass.wav는 편집 원본으로 보존한다. 재현/분석은 `extract_grass_variants.py`, `grass-variants-analysis.json`.

## 프리소리 — 물 호버 효과음

- 배포 페이지: https://freesori.tistory.com/17 (물소리, 비소리 20개)
- 사용자 제공 원본: `C:/Users/User/Desktop/tfile.wav`; 보관본 `res/audio/freesori-water/tfile.wav`.
- 적용 근거: 2026-09-20 사용자가 무료 이용을 확인했다고 알리고 적용을 지시함. 배포 페이지의 명시적 라이선스 원문·원저작자는 별도로 확인되지 않았으며 CC0/CC BY로 표기하지 않는다.
- 편집본: `res/audio/freesori-water/freesori_water_ripple.wav`.
- Unity 임포트본: `impls/unity/Afterlife/Assets/Modules/WorldAudio/Resources/WorldAudio/freesori_water_ripple.wav`.
- 편집: 원본 2.30~2.60초 발췌(0.30초), 기존 편집본의 첫 100ms 어택 제거, 채널별 DC 제거, 시작 30ms/끝 140ms 페이드. 이전 게인 유지, 결과 피크 -16.12dBFS. 44.1kHz/16bit/스테레오 유지.
- 분석과 재현: 같은 폴더의 `analysis.json`, `extract_ripple.py`. RMS 파형 기준 선정이며 청취 검증은 아직 하지 않음.
- 현재 사용 음원: `res/audio/freesori-water/variants/freesori_water_ripple_01~05.wav`; Unity는 같은 파일을 `Resources/WorldAudio/WaterRipples/`에서 임포트한다. 기존 단일 편집본은 참조 호환용으로 유지한다.
- 변형 편집: 원본 1.12~1.60, 2.30~2.68, 2.84~3.30, 4.64~5.00, 6.86~7.38초 발췌. DC 제거, tanh로 좁은 피크 완화, 45ms/180ms 페이드, RMS -32dBFS 목표와 피크 -16dBFS 상한으로 감쇠 조정. 분석은 `variants-analysis.json`에 기록한다.
- 사용: Day01 After 물 호버 동심원 생성 시, 기본 볼륨 0.525, 연속 이동 55%, 재생 간격 0.25~0.40초, 피치 ±2%, 볼륨 ±8%.
- 내부 출처 기록이며 공개 게임 크레딧/영상 설명란 반영 완료를 의미하지 않는다.

## 작업 목록

- [x] 공유마당 음원 출처 및 이용조건 기록
- [x] 게임/Devlog 공개 전 출처 표기 지침과 문구 작성
- [ ] 해당 음원을 실제 사용하면 게임 크레딧 및 사용 영상 설명란에 반영

## 공유마당 — 나무 쓰러지는 소리

- 저작물명: 나무 쓰러지는 소리
- 저작(권)자: 한국저작권위원회
- 출처: https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13220749&menuNo=200020
- 이용조건: CC BY (저작권정보 표시). 상세 페이지에서 확인한 버전 번호는 없으므로 임의로 4.0 등으로 기재하지 않는다.
- 공식 이용 안내: https://gongu.copyright.or.kr/gongu/main/contents.do?menuNo=200222
- 사용자 제공 ZIP: `C:/Users/User/Desktop/나무_쓰러지는_소리.zip`
- 출처 확인일: 2026-09-19
- 상태: Devlog After 나무 쓰러짐 음원으로 사용 중. 기존 ecfike 음원을 대체했다. 공개용 게임 크레딧/영상 설명란 반영은 아직 필요하다.
- 원본 보관: `res/audio/gongu-tree-fall/gongu_13220749_tree_fall.mp3`
- 게임 리소스: `impls/unity/Afterlife/Assets/Modules/WorldAudio/Resources/WorldAudio/gongu_13220749_tree_fall.mp3`
- 편집: 없음. ZIP의 MP3 바이트를 그대로 복사했다. 게임 재생 피치 1, 볼륨 배율 0.65.

## 적용 규칙

1. 이 음원 또는 편집본을 게임에 사용하면 출시/공개 빌드 전에 게임의 크레딧 또는 외부 저작물 안내 화면에 아래 항목을 넣는다.
2. 음원이 들리는 Devlog, 트레일러 등 영상을 공개할 때 해당 영상 설명란에도 같은 출처를 표시한다.
3. 저작물명, 저작자명, 원본 출처 링크, CC BY 표시를 함께 유지한다. 정확한 라이선스 링크/버전은 다운로드 시 제공되는 정보에서 확인하고, 추정해서 적지 않는다.
4. 편집했다면 실제 변경 사항만 추가한다. 예: 구간 발췌, 잡음 억제, 피치 조정. 편집하지 않았다면 편집 문구를 생략한다.
5. 파일 이름 변경, WAV 변환, 자르기, 후처리를 하더라도 원본 출처와 표기 의무를 유지한다. CC0 또는 자체 제작 음원으로 기록하지 않는다.
6. 프로젝트 내부에 이 문서만 저장하는 것으로 공개용 크레딧 표기를 완료했다고 간주하지 않는다. 실제 게임/영상에 반영했는지 공개 전에 확인한다.

## 공개용 표기 예시

> 효과음: 「나무 쓰러지는 소리」 — 한국저작권위원회
> 출처: 공유마당 https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13220749&menuNo=200020
> 라이선스: CC BY (저작권정보 표시)

편집한 경우 위 문구 아래에 `편집: [실제로 수행한 변경 사항]`을 추가한다.

## 프로토타입 이관 (2026-09-21)

Day01과 동일한 임포트 음원을 참조하여 나무 쓰러짐, 바위 타격/파괴, 물 파문 5종, 풀 스침 02~04, 새 울음을 프로토타입에 연결했다. 이번 이관에서 음원 파일의 추가 편집/복제는 없다. 나무 쓰러짐은 피치 1·기본 배율 0.65에 사용자 SoundVolume을 곱한다. 기존 출처/이용조건을 그대로 유지한다. 내부 구현과 출처 기록만 완료했으며, 공개용 게임 크레딧 및 음원이 포함된 영상 설명란 표기는 아직 미완료다. 실제 청취 검증 대기.

## Day01 AI 나레이션 — Supertonic 3

- 공식 코드: https://github.com/supertone-oss-archive/supertonic (MIT)
- 모델: https://huggingface.co/supertone-oss-archive/supertonic-3 (OpenRAIL-M)
- 모델 revision: aafc6e32416a594460b32413efc49d7fe4ce6d46. 남성 M2, ko, speed 0.98, steps 12. 특정인의 음성 복제를 하지 않고 제공 프리셋을 사용했다.
- 2026-09-21 공식 LICENSE 확인. 로컬 무상 실행과 생성물 이용에 비상업 전용 제한은 없으며, 금지 용도와 AI 생성 고지를 준수한다. 모델 제6항은 생성물에 대한 권리 주장을 하지 않음을 명시하고 Attachment A(e)는 기계 생성 사실 고지를 요구한다. 코드 MIT와 모델 조건을 혼동하지 않는다.
- 원문 라이선스/제작 대본: devlog/youtube/day-01/source/narration/. 음원: devlog/youtube/day-01/media/audio/narration/. Unity는 Assets/Devlog/Youtube/Day01/Audio/Narration/의 문장별 WAV를 사용한다.
- 편집: DC 제거, 피크 -3dBFS 이하, 양끝 8ms 페이드, PCM16/44.1kHz/mono 저장. 음성 속도는 합성 시 설정이며 피치 변환 없음.
- Film 엔딩 크레딧의 나레이션 항목에 `Supertonic 3`와 `AI 생성 음성` 표시를 적용했다. 공개 영상에서 이를 제거할 경우 다른 명확한 고지를 유지한다. 영상 설명란 표기는 아직 게시하지 않았다.
- 공개 설명란 문구: `나레이션은 Supertonic 3의 남성 프리셋 M2으로 로컬 생성한 AI 음성입니다. https://github.com/supertone-oss-archive/supertonic`
- 이 나레이션 고지는 기존 공유마당 효과음의 출처 표시 의무를 대체하지 않는다.

### Supertonic 고지 위치 확인
Attachment A(e)는 AI 생성 사실의 명확한 고지를 요구하며 특정 위치나 상시 화면 표기를 명시하지 않는다. 영상 설명란 상단에 `이 영상의 나레이션은 Supertonic 3로 생성한 AI 음성입니다.`라고 명확히 기재하는 방식을 사용할 수 있다고 해석한다. 설명란만으로 충분하다는 공식 확인을 받은 것은 아니며 기존 화면 표시는 구현상 선택이었다. 이후 사용자 요청으로 Unity 엔딩 크레딧으로 고지를 이동했다. 공개 설명란은 아직 게시하지 않았다.

## Day01 배경음악 — Childhood

- 작곡가: Scott Buckley. 공식 출처: https://www.scottbuckley.com.au/library/childhood/
- 라이선스: CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
- 원본 MP3는 devlog/youtube/day-01/media/audio/music/, Unity 임포트는 Assets/Devlog/Youtube/Day01/Audio/Music/. 원본 파일 편집 없음. Film 재생 시 볼륨 0.08, 시작 2초 페이드 인·끝 3초 페이드 아웃 적용.
- 엔딩 크레딧에 곡명·작곡가·라이선스·사이트를 추가했다. 유튜브 설명란에는 아래 문구를 별도로 넣어야 한다. 아직 공개 게시하지 않았다.

```text
Childhood — Scott Buckley
Released under CC BY 4.0. www.scottbuckley.com.au
https://www.scottbuckley.com.au/library/childhood/
https://creativecommons.org/licenses/by/4.0/
영상 길이에 맞춰 반복·4초 크로스페이드 연결, 볼륨 조절 및 페이드 인/아웃을 적용했습니다.
```

음원 길이 보정: 원곡 137.079초를 4초 선형 크로스페이드로 반복 연결한 Childhood-Day01.wav(192.85초)를 재생한다. 원본 MP3는 보존했다. 공개 편집 내역에도 반복·크로스페이드를 명시한다.

시야 샷 추가 후 현재 BGM 편집본은 206.85초, Film 볼륨 0.10이다. 반복·크로스페이드와 시작/종료 페이드 조건은 동일하다.


## ThresholdLab 화면 음악·전환 음원 — 2026-09-26

음악 Undertow / Childhood / Unraveling / Ascension — Scott Buckley, CC BY 4.0. 공식 출처와 효과음 출처는 아래 공개용 문구를 따른다. 음악의 구간 발췌·음량 조정·5초 교차 연결 편집을 수행했다. 효과음은 CC0 Water bubbles loop / GoldenTricycle와 Backwards Whoosh / SlavicMagic의 공식 HQ MP3를 편집했다. 속도·필터·페이드·루프 교차 연결·역재생 내역은 `res/audio/threshold/analysis.json`에 기록했다.

ThresholdLab의 설정 → 음악·효과음 출처 패널에 아래 내용을 표시한다. 본편 프로토타입이나 유튜브 설명란에 게시한 것은 아니다. 공개 영상에는 작곡가 안내에 따라 설명란 크레딧도 필요하다.

```text
음악 · 효과음 크레딧

Water bubbles loop — GoldenTricycle (CC0-1.0)
https://freesound.org/people/GoldenTricycle/sounds/521392/
Backwards Whoosh — SlavicMagic (CC0-1.0)
https://freesound.org/people/SlavicMagic/sounds/446010/
Undertow — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/undertow/
Childhood — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/childhood/
Unraveling — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/unraveling/
Ascension — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/ascension/

음악: Scott Buckley · www.scottbuckley.com.au
라이선스: https://creativecommons.org/licenses/by/4.0/
효과음: https://creativecommons.org/publicdomain/zero/1.0/
편집: 구간 발췌·음량 조정·반복 교차 연결. 물/이동음 속도 조정, 물소리 필터·역재생.
```


## ThresholdLab 사후세계·밤 음악 교체 — 2026-09-26

현재 타이틀/인트로는 Decoherence, 낮은 Undertow, 밤은 Intervention (No Piano Melody), 결과는 Unraveling/Ascension을 사용한다. 이전 Childhood는 보관본이며 현재 샘플에서는 재생하지 않는다.
Decoherence 20~140초와 Intervention 12~132초를 발췌해 DC 제거·음량 조정·5초 교차 연결을 적용했다. 음악별 115초 반복본. 기존 출처 패널을 아래 문구로 갱신한다. 외부 공개 영상 설명란은 별도 게시해야 한다.

```text
음악 · 효과음 크레딧

Water bubbles loop — GoldenTricycle (CC0-1.0)
https://freesound.org/people/GoldenTricycle/sounds/521392/
Backwards Whoosh — SlavicMagic (CC0-1.0)
https://freesound.org/people/SlavicMagic/sounds/446010/
Undertow — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/undertow/
Decoherence — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/decoherence/
Intervention (No Piano Melody) — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/intervention/
Unraveling — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/unraveling/
Ascension — Scott Buckley (CC-BY-4.0)
https://www.scottbuckley.com.au/library/ascension/

음악: Scott Buckley · www.scottbuckley.com.au
라이선스: https://creativecommons.org/licenses/by/4.0/
효과음: https://creativecommons.org/publicdomain/zero/1.0/
편집: 구간 발췌·음량 조정·반복 교차 연결. 물/이동음 속도 조정, 물소리 필터·역재생.

```

## Day02 마지막 크레딧 — 2026-10-04

Day01의 Supertonic 3 / AI 생성 음성 및 Childhood / Scott Buckley / CC BY 4.0 문구를 Day02 마지막 크레딧에 동일하게 저장했다. 기존 Childhood-Day01.wav 참조를 재사용하며 신규 다운로드·편집은 없다. 사용자 요청으로 Day02 음악 재생 토글을 켜서 저장했다. 실제 음악을 켠 공개본은 위 Childhood 출처·반복/크로스페이드 편집 문구를 영상 설명란에도 반영한다. 영상 내부 크레딧 적용은 공개 설명란 반영 완료를 뜻하지 않는다. 상세는 devlog/youtube/day-02/records/ending-credits.md.

## Day03 AI 나레이션 — 2026-10-07

Day03 나레이션v1도 기존 로컬 모델 revision과 M2 한국어 프리셋(speed0.98, steps12)을 사용한다.12문장·실측 말소리54.513초·PCM16/44.1kHz/mono이며 동일한 DC 제거·피크 제한·8ms 페이드를 적용했다. 별도 음성 복제나 새 외부 음악은 사용하지 않았다.

Day03 필름 크레딧에 `나레이션 / Supertonic 3 · M2 / AI 생성 음성`을 저장했다. 실제 녹화·최종 영상·공개 설명란 반영은 아직 수행하지 않았다. 원본·출력·편집본과 청취 확인 절차는 [Day03 제작 기록](../devlog/youtube/day-03/records/narration.md)을 따른다.

## Day03 배경음악·Day02 형식 크레딧 — 2026-10-07

사용자 요청으로 Day02와 같은 Childhood-Day01.wav(206.85초) 참조와 음악 재생 활성·볼륨0.3·크레딧 배율2를 Day03Film에 저장했다. 신규 다운로드·파일 복제·음원 편집은 없다. 기존 반복·4초 크로스페이드 편집본을 그대로 사용하며 재생 시 시작2초/끝3초 페이드와 크레딧 진입2초 음량 상승을 적용한다.

마지막 화면 크레딧도 Day02와 동일한 `나레이션 / Supertonic 3 / AI 생성 음성` 및 `배경음악 / Childhood / Scott Buckley / CC BY 4.0 · www.scottbuckley.com.au`로 변경했다. 곡명·작곡가·공식 출처·라이선스와 기존 편집 내역은 위 Childhood 공개용 표기를 따른다. 실제 영상 설명란 게시·녹화·최종 영상 반영은 아직 수행하지 않았다. 공개용 준비 문구와 사용/확인 절차는 [Day03 음악·크레딧 기록](../devlog/youtube/day-03/records/ending-credits.md)에 있다.
