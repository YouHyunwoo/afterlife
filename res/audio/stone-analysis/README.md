# 돌 효과음 분할

원본은 수정하지 않았다. 두 파일 모두 44.1kHz / 16bit / stereo / 약 4.202초다. RMS 피크와 앞쪽 에너지 골짜기를 기준으로 분할했으며 충돌이 겹친 부분은 그대로 남아 있다. 단발음은 0.065~0.24초이며 0.7ms/12ms 페이드와 공통 -2dB 게인을 적용했다. 피치·EQ·개별 정규화는 적용하지 않았다.

- 추출 구간: 40개 (추천 폴더의 파일은 이 중 복사본)
- recommended: 어택 대비 꼬리 에너지로 고른 12개 후보. 청음 평가가 아닌 파형 지표 기반이다.
- impacts: 원본별 짧은 충돌 구간
- debris: 원본별 연속 충돌/잔해 구간 3개
- segments.csv / json: 원본 시작·종료 시간, 길이, 피크, 주파수 지표
- 게임에는 아직 연결하지 않았다.

## 추천 미리듣기 순서

01. 0.00초 — 01_stones_falling_impact_16.wav (원본 2.805–3.045초)
02. 0.59초 — 02_stones_falling_impact_04.wav (원본 0.677–0.885초)
03. 1.15초 — 03_stones_falling_impact_15.wav (원본 2.645–2.805초)
04. 1.66초 — 04_stones_falling_impact_14.wav (원본 2.473–2.645초)
05. 2.18초 — 05_stones_falling_impact_10.wav (원본 1.779–1.903초)
06. 2.65초 — 06_stones_falling_impact_02.wav (원본 0.314–0.478초)
07. 3.17초 — 07_dropping_rocks_impact_16.wav (원본 2.817–3.004초)
08. 3.70초 — 08_dropping_rocks_impact_05.wav (원본 1.009–1.192초)
09. 4.24초 — 09_dropping_rocks_impact_12.wav (원본 2.146–2.314초)
10. 4.75초 — 10_dropping_rocks_impact_17.wav (원본 3.004–3.208초)
11. 5.31초 — 11_dropping_rocks_impact_15.wav (원본 2.629–2.817초)
12. 5.85초 — 12_dropping_rocks_impact_18.wav (원본 3.208–3.448초)

출처: iwanPlays — https://freesound.org/people/iwanPlays/sounds/567251/ 및 https://freesound.org/people/iwanPlays/sounds/567250/ . 567251의 CC0는 앞서 페이지에서 확인했다. 567250의 라이선스는 이 분할 작업에서는 별도로 확인하지 않았다.
