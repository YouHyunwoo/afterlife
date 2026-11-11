# 문서 대시보드 도구

저장소 루트에서 `python tools/docs/render_game_dashboard.py --docs-dir docs/game-process`를 실행한다. Markdown과 dashboard-index.json에서 게임 진행 HTML을 재생성한다. 템플릿과 MIT 라이선스의 Mistune 파서는 이 폴더에 함께 보관해 로컬 스킬 설치에 의존하지 않는다.

유튜브 대시보드는 로컬 `python devlog/youtube/tools/render_dashboard.py`로 생성하며 같은 파서를 공유한다. `python devlog/youtube/tools/check_links.py`로 문서·대시보드 링크를 확인한다. HTML은 원본이 아니므로 직접 편집하지 않는다.
