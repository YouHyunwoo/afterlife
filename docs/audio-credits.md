# 외부 음원 크레딧 지침

## 프리소리 — 물 호버 효과음

- 배포 페이지: https://freesori.tistory.com/17 (물소리, 비소리 20개)
- 사용자 제공 원본: `C:/Users/User/Desktop/tfile.wav`; 보관본 `res/audio/freesori-water/tfile.wav`.
- 적용 근거: 2026-09-20 사용자가 무료 이용을 확인했다고 알리고 적용을 지시함. 배포 페이지의 명시적 라이선스 원문·원저작자는 별도로 확인되지 않았으며 CC0/CC BY로 표기하지 않는다.
- 편집본: `res/audio/freesori-water/freesori_water_ripple.wav`.
- Unity 임포트본: `impls/unity/Afterlife/Assets/Devlog/Modules/Audio/Resources/DevlogAudio/freesori_water_ripple.wav`.
- 편집: 원본 2.30~2.60초 발췌(0.30초), 기존 편집본의 첫 100ms 어택 제거, 채널별 DC 제거, 시작 30ms/끝 140ms 페이드. 이전 게인 유지, 결과 피크 -16.12dBFS. 44.1kHz/16bit/스테레오 유지.
- 분석과 재현: 같은 폴더의 `analysis.json`, `extract_ripple.py`. RMS 파형 기준 선정이며 청취 검증은 아직 하지 않음.
- 현재 사용 음원: `res/audio/freesori-water/variants/freesori_water_ripple_01~05.wav`; Unity는 같은 파일을 `Resources/DevlogAudio/WaterRipples/`에서 임포트한다. 기존 단일 편집본은 참조 호환용으로 유지한다.
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
- 게임 리소스: `impls/unity/Afterlife/Assets/Devlog/Modules/Audio/Resources/DevlogAudio/gongu_13220749_tree_fall.mp3`
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
