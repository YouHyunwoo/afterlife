# 음원 가공 도구

- `split_stone_sounds.py`: `res/audio/stone-analysis/`의 돌 원본2개에서 후보·분석 생성. 바탕화면에 의존하지 않는다.
- `split_tree_fall.py --input <원본.wav>`: 이전 ecfike 나무 녹음의 구간 후보 생성.
- `process_tree_fall.py --input <원본.wav>`: 같은 원본과 기존 짧은 후보에서 후처리.
- `build_clean_tree_fall.py`, `build_extended_tree_fall.py`: 이전 나무 후보의 파생 편집본 생성.

필요 패키지는 numpy·scipy다. 출력은 해당 `res/audio/` 분석 폴더이며 실행하면 파생 파일이 갱신된다. 원본은 보존한다. ecfike 후보는 현재 비활성으로, 현재 공유마당 음원을 이전 후보로 바꾸지 않는다.

사용·공개 표기는 [음원 크레딧](../../docs/audio-credits.md), 제작 후 목록 갱신은 `python tools/resources/update_manifest.py`를 따른다. 정리 검증에서는 새 음원을 생성하지 않고 입력 경로와 구문을 확인한다.
