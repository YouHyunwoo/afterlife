# 공용 제작 도구

|폴더|책임|
|---|---|
|art/|Blender 모델·이미지·아이콘 제작|
|audio/|음원 분석·구간 분리·가공|
|tts/|여러 유튜브 회차가 공유하는 로컬 음성 생성|
|unity/|Unity 생성 프로젝트 C# 컴파일과 빌드 출력 사용법|
|docs/|게임 문서 대시보드와 공용 Markdown 파서|
|resources/|게임 리소스 목록·해시 갱신|
|localization/|기존 게임 문자열 정리|

회차 대본·음성·영상은 `devlog/youtube/day-XX/`에 두고 공용 모델·Python 환경은 `.tooling/youtube-tts/`에 둔다. 아트·음원 도구의 입력·출력 위치는 해당 스크립트의 옵션과 `res/README.md`를 확인한다. EVE 편집기 소스는 외부 독립 프로젝트에서 관리한다.
