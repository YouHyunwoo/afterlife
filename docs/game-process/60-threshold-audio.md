# 화면 전환 효과음·물 루프·배경음악

2026-09-26. 적용 대상은 독립 ThresholdLab 씬이다. 기존 본편 프로토타입이나 Day01 촬영 음원은 변경하지 않는다.

## 선정과 권리

최초 적용 시 무료 원본 6개를 확보했고, 분위기 수정으로 2곡을 추가했다. 아래 표는 현재 적용 곡이다. 유료 결제나 음악 생성 모델 설치는 하지 않았다. 음악은 CC BY 4.0이므로 출처 표기가 필요하고, 효과음은 CC0이다.

|용도|음원 / 저작자|라이선스·출처|
|---|---|---|
|타이틀·인트로|Decoherence / Scott Buckley|[CC BY 4.0](https://www.scottbuckley.com.au/library/decoherence/)|
|인게임 낮|Undertow / Scott Buckley|[CC BY 4.0](https://www.scottbuckley.com.au/library/undertow/)|
|인게임 밤|Intervention (No Piano Melody) / Scott Buckley|[CC BY 4.0](https://www.scottbuckley.com.au/library/intervention/)|
|게임 오버|Unraveling / Scott Buckley|[CC BY 4.0](https://www.scottbuckley.com.au/library/unraveling/)|
|게임 클리어|Ascension / Scott Buckley|[CC BY 4.0](https://www.scottbuckley.com.au/library/ascension/)|
|카메라 이동·균열 진입|Backwards Whoosh / SlavicMagic|[CC0](https://freesound.org/people/SlavicMagic/sounds/446010/)|
|전환·모달·결과 화면 물소리|Water bubbles loop / GoldenTricycle|[CC0](https://freesound.org/people/GoldenTricycle/sounds/521392/)|

현재 선정 의도: 사후세계는 침침하고 불길한 앰비언트, 마을 낮은 피아노·현악, 밤은 스릴러 긴장감, 실패는 상실감, 통과는 희망을 둔다. 타이틀과 인트로는 같은 음악을 끊지 않고 대화 중 볼륨만 낮춘다. 청취 평가를 완료한 것으로 주장하지 않으며 사용자 조정이 필요하다.

Freesound는 원본 WAV 대신 공식 공개 HQ MP3 미리듣기를 사용했다. 음악은 작곡가 공식 MP3 링크에서 받았다. 다운로드 URL·SHA-256·라이선스는 `res/audio/threshold/sources.json`, 공식 페이지 보관본과 공개용 문구도 같은 폴더에 둔다. 일부 작곡가 페이지의 직접 저장은 HTTP 406이었으나 웹 도구로 라이선스를 확인했고 공식 MP3 다운로드는 완료됐다.

## 편집

- 물: 80% 속도로 낮추고 65~2400Hz 대역 필터·DC 제거·음량 조정. 1.2초 앞뒤 교차 연결로 약 13.6초 루프를 제작했다. 이 루프에서 진입/이탈용 짧은 클립도 만들었다. 이탈은 역재생과 페이드를 적용한다.
- 이동음: 75% 속도·25ms/100ms 페이드로 약 0.88초. 균열 진입 시 피치를 1.25로 재생해 짧은 진입 동작과 맞춘다.
- 음악: Undertow 8~128초, Childhood 4~116초, Unraveling 12~132초, Ascension 35~147초 발췌. 5초 교차 연결·DC 제거·음량 조정으로 각각 115/107/115/107초 반복본을 만들었다.
- 출력: 44.1kHz PCM16 WAV. Unity 효과음은 PCM/DecompressOnLoad, 음악은 Vorbis/Streaming으로 임포트한다. 외부 Vorbis 인코더의 실패한 후보는 삭제했고 최종본은 WAV만 사용한다.
- 재현: `res/audio/threshold/download.py`, `prepare.py`. 편집용 SoundFile만 프로젝트 Temp에 설치했다. 시스템 Python이나 전역 패키지는 변경하지 않았다.

## 재생 연결

`ThresholdAudio.prefab`은 `ThresholdSoundscape`와 5개 AudioSource(이동·전환 물·반복 물·음악 A/B)를 소유한다. `ThresholdAudioBank.asset`에서 클립과 음악/효과음/물 루프 음량을 교체한다. 기존 합성 저음 `ThresholdAmbience`는 샘플에서 비활성화한다.

- 타이틀↔인트로 카메라 이동: whoosh 한 번.
- 인트로→균열: whoosh, 암전 도착 시 물 진입음, 중앙에서 어둠이 걷힐 때 물 이탈음.
- 기타 전체 화면 전환: 어둠 진입/해제 각각 물소리.
- 모달 열기/닫기: 짧은 진입/이탈 물소리.
- 모달·게임 오버·클리어 유지: 약한 물 루프를 계속 재생한다. 전환 중에는 약간 높이고 종료 시 부드럽게 낮춘다.
- 음악: 화면에 맞춰 1.6초 교차 페이드한다. 모달·인트로·전환 중에는 음악을 낮춘다. 설정의 `배경음 · 효과음` 슬라이더가 이 샘플 음악·효과음을 함께 조절한다.
- 컴포넌트 해제 시 소스를 정지하고 참조를 해제한다. 전역 음악 플레이어나 추가 AudioListener를 만들지 않는다.

## 크레딧

설정에 `음악 · 효과음 출처` 버튼과 실제 크레딧 패널을 추가했다. 곡명·저작자·원본 링크·라이선스 링크·편집 내용을 표시한다. 이 샘플의 화면 안 표기는 구현했으나 본편 프로토타입 크레딧에 적용하거나 외부에 게시한 것은 아니다.

Scott Buckley 음악을 사용한 유튜브 영상은 작곡가 안내에 따라 영상 설명란에도 크레딧을 넣어야 한다. `res/audio/threshold/CREDITS.txt`와 `docs/audio-credits.md`의 문구를 사용한다. 공개 설명란에 게시하지 않았다.

## 검증과 사용자가 확인할 항목

현재 열린 Unity 에디터의 `connect-audio` 요청으로 오디오 뱅크·프리팹·설정 크레딧 저장을 완료했다. 오디오 뱅크의 8개 클립과 크레딧 참조가 모두 연결된 것을 확인했다.

관련 C# 4개 어셈블리 `dotnet build --no-restore` 오류·경고 0. 편집 파일을 다시 읽어 길이·피크·RMS·루프 경계 차이·해시를 `analysis.json`에 기록했다. 모든 최종 클립 피크는 -4dBFS 이하이고 물 루프의 경계 샘플 차이는 약 0.003이다. 파일 연결부의 수치 확인은 실제 청취 검증을 대체하지 않는다.

프로젝트 지침에 따라 Unity Play Mode/Test Runner를 실행하지 않았다. 청취·게임 내 동기화·볼륨 균형은 사용자 확인 대기다.

1. 타이틀에서 음악을 듣고 새 게임의 카메라 이동음과 인트로 음악 감쇠를 확인한다.
2. Esc로 균열 진입해 이동음·암전 물소리·어둠 해제 물소리를 확인한다.
3. 설정 모달을 30초 이상 유지해 약한 물소리가 반복 경계에서 끊기거나 튀지 않는지 확인한다.
4. 게임 오버/클리어에서 각각 음악이 바뀌고 물 루프가 유지되는지 확인한다.
5. 볼륨 0으로 음악과 효과음이 사라지는지, 출처 패널과 설정 복귀 버튼이 동작하는지 확인한다.


## 사후세계·밤 분위기 수정 — 2026-09-26

사용자 피드백: 기존 사후세계 음악은 밝고 마을 낮에 더 적합하다. 타이틀·인트로는 어둡고 침침하게, 밤은 긴장감 있게 구분한다.

작업 목록: 공식 출처/무료 조건 확인 → 새 음악 2곡 확보·반복 편집 → 사후세계/낮/밤 연결 → 실제 크레딧·문서 갱신 → 관련 C# 빌드·에디터 저장.

- 사후세계: Decoherence 20~140초, 불길한 현악과 변화하는 신스 중심의 앰비언트. 공식 소개를 기준으로 선정했으며 직접 청취 판단은 사용자 확인 대기.
- 밤: Intervention의 공식 No Piano Melody 믹스 12~132초. 피아노 주선율을 제외한 스릴러 분위기로 선정.
- 낮: 기존 타이틀의 Undertow를 `music_day.wav`로 이동. Childhood는 비교/재사용 원본으로 보존하되 현재 재생 목록에서는 제외한다.
- 새 루프는 각각 115초(5초 교차 연결), PCM16/44.1kHz. 기존 물 효과음과 결과 음악은 유지한다.
- `ThresholdAudioBank.PlayingMusic`은 낮, `NightMusic`은 밤이다. `ThresholdSoundscape.SetNight(bool)`로 낮/밤 상태를 주입하면 기존 1.6초 음악 페이드로 전환한다. NightMusic이 없으면 낮 음악으로 폴백한다.
- 빈 테스트 씬에는 자동 낮/밤 시스템이 없다. 기본은 낮이며 인게임에서 `ThresholdAudio`의 `ThresholdSoundscape.IsNight`를 체크하면 밤 음악, 해제하면 낮 음악을 확인할 수 있다. 본편 자동 주기에 연결했다고 간주하지 않는다.
- 출처 패널 재적용 시 기존 텍스트도 갱신하도록 수정했다.
- 관련 4개 C# 어셈블리 빌드 오류·경고 0. Unity 플레이 테스트는 지침상 실행하지 않았다. 타이틀의 새 음악, 인게임 IsNight 변경 시 곡 교차 페이드, 결과 화면 음악 유지, 설정 출처 갱신을 직접 확인한다.

저장 확인: `connect-audio` 에디터 요청 완료. 낮/밤 클립 참조와 ModalPage의 Decoherence/Intervention 출처 텍스트 직렬화 확인.

## 물방울 재생 속도 — 2026-09-26

작업 목록: 물 전환/루프 속도 조정 → 조절값 노출 → 빌드 및 문서 갱신.

전환 물소리와 모달/결과 물 루프를 기존 대비 0.8배 속도로 재생한다. AudioSource.pitch를 사용하므로 음높이도 함께 낮아진다. 13.6초 루프는 약 17초, 1.15초 전환 클립은 약 1.44초가 된다. 재생 시작 시점은 그대로다. 음악과 카메라 이동음은 영향받지 않는다.

`ThresholdAudioBank.WaterPlaybackSpeed`에서 0.5~1.5로 조절 가능하며 기본값은 0.8이다. 재생 중 변경도 두 물소리 소스에 반영한다. 원본/편집 WAV 자체는 변경하지 않았다.

관련 4개 어셈블리 빌드 오류·경고 0. 지침에 따라 플레이 테스트는 실행하지 않았다. 모달을 열고 35초 유지해 낮아진 물소리 속도와 반복 연결을 확인하고, 모달을 닫거나 균열로 진입할 때 전환 소리의 길이가 어울리는지 청취한다.
