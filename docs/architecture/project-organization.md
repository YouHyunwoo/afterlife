# 프로젝트 폴더 정리와 운영

## 작업 목록 · 2026-10-07

- [ ] 기존 채집 피드백·Day02·Day03·경제 기획 변경 검증과 목적별 커밋
- [ ] Library와 외부 에셋을 보존하고 로그·빌드·임시 자료·Git 임시 객체 정리
- [ ] TTS 실행 환경·추론 코드·라이선스를 유튜브 공용으로 분리
- [ ] 리소스 목록 도구 공유, 분석 캐시 제외, 참고 자료·제작 원본 위치 정리
- [ ] Unity 최상위 Legacy·Prototype·Modules·Samples·Devlog 구분과 경로 갱신
- [ ] 최신 안내·출처·문서 링크와 대시보드 갱신
- [ ] GUID·파일 보존·문서 링크·C# 컴파일 검증과 목적별 커밋

## 보존 범위

Unity Library와 Externals의 파일 내용은 보존한다. Recordings는 사용자 정리 영역이다. 현재 편집 프로젝트와 미디어, 음성 모델, 제작 원본, 로컬 복구 백업과 과거 배포 ZIP은 보존한다. 배포 보관소 이동은 경로 확정 뒤 수행한다.

## 검증

Unity 에셋은 AssetDatabase로 이동해 GUID를 유지한다. .meta를 직접 만들거나 수정하지 않는다. C#은 관련 프로젝트의 `dotnet build --no-restore`로 검증하며 Unity 테스트·Play는 실행하지 않는다. 구조 변경 이후 사용자가 본편·Day01~03 씬을 열어 참조와 표시를 확인한다.

## Unity 최상위 구조

|폴더|역할|이전 위치|
|---|---|---|
|Legacy/Scenes·Scripts·Prefabs·Data·Content·Experiments·Editor|기존 구현과 기존 전용 리소스·도구|01_Scenes·02_Scripts·03_Prefabs·04_Data·05_Resources·99_Dev·Editor의 기존 도구|
|Prototype/|현재 게임 프로토타입|_AfterlifePrototype/|
|Modules/|순수 코어·Unity 표현으로 구분된 공용 시스템|Afterlife/Modules/|
|Samples/|ThresholdLab 등 독립 샘플·실험|Afterlife/Samples/|
|Devlog/|Youtube·DC 회차 구현|기존 위치 유지|
|Editor/ProjectMaintenance/|에셋 구조 이전의 CLI 진입점|이번 정리에서 추가; Tools 메뉴 없음|

Plugins·Resources·StreamingAssets·Settings·ProjectSettings와 csc.rsp는 Unity/외부 패키지 계약에 따라 유지한다. 특히 기존 05_Resources를 Legacy/Content로 이동해 새 Resources 특수 폴더로 만드는 일이 없도록 했다. Externals의 모든 파일 내용은 유지했다. Legacy에 새 기능을 누적하지 않고 Modules와 Prototype의 얇은 연결로 구현한다.

## 이전과 해제

현재 구조는 이미 적용됐다. AssetLayoutMigration.Apply는 소스/대상 동시 존재를 거부하고 AssetDatabase.MoveAsset로 이동한다. 같은 이전을 다시 호출해도 목적지 존재를 확인해 넘어간다. 빌드 씬 경로는 기존 GUID에서 새 경로를 계산했다. 소스·문서·도구의 AssetDatabase 경로도 함께 갱신했다. 역이전이 필요하면 이번 구조 커밋을 별도 작업 폴더에서 검토해 되돌리고 해당 프로젝트를 에디터에서 다시 임포트한다. .meta를 직접 편집하지 않는다.

## 빌드 출력

프로토타입 BuildWindows는 Builds/PixelPrototypeWindows 내부의 이전 산출물만 지운 뒤 새 빌드를 출력한다. Builds 상위 폴더는 유지하며 다른 출력 폴더는 건드리지 않는다. 절대 출력 경로와 모든 하위 경로의 링크 여부를 먼저 검사한다. 예전 Afterlife.exe와 새 AfterlifePrototype.exe가 같은 출력에 공존하던 문제를 방지한다. 공개·복구가 필요한 빌드는 먼저 외부 보관소로 복사한다. 이번 작업에서는 새 배포 빌드를 만들지 않았다.

## 로그·임시 자료·Git 정리

Unity Logs에서는 설정 복구 자료·씬 확인 JSON과 종류별 마지막 텍스트 로그를 보존했다. [보존 목록](logs-retained.json)을 따른다. QA 이미지 증거는 docs/game-process/qa에 있다. 이전 Logs 파일1,041개·194,532,585바이트를 제거했고 새 구조 이전·컴파일 로그는 남긴다. 루트 Temp와 logs, Unity Builds의 기존 내용은 비웠다. Library는 삭제하지 않았다. Recordings는 사용자 정리 영역으로 유지한다.

Git은 실행 중 작업이 없는 상태를 확인하고 2026-10-07 이전 tmp_obj 임시 파일51개·438,231,685바이트를 제거했다. git prune-packed로 이미 팩에 든 중복 loose 객체만 정리했으며 참조·reflog·과거 커밋 이력은 정리 대상으로 삼지 않았다. 확인 결과 garbage=0, prune-packable=0이다.

## 공용 제작 도구와 리소스

- TTS 추론 코드·요구 패키지·라이선스는 tools/tts, 모델·Python 환경은 .tooling/youtube-tts에 둔다. 이미 Day01·Day02가 공유했고 새 회차도 --day day-XX로 사용한다. 회차 입력·음성·필름은 그대로 보존한다.
- 가상환경 이동 후 Python 실행파일과 numpy·soundfile·onnxruntime·공용 helper import를 확인했다. 재설치는 tools/tts/setup.ps1을 사용한다. pip.exe 대신 python.exe -m pip를 사용한다.
- 리소스 목록 갱신은 tools/resources/update_manifest.py를 사용한다. 기존 devlog 명령은 공용 코드로 위임하는 호환 어댑터다.
- Kenney 참고 자료는 res/references/kenney로 모았다. 원본과 라이선스는 보존한다.
- 출력 이미지 폴더의 frame.aseprite는 내용이 다른 편집본이므로 res/art-source/pixel-art/frame-edited.aseprite로 옮겼다. 기존 frame.aseprite를 덮어쓰지 않았다.
- Graphify 최종 분석은 과거 기록으로 보존하고 실행 cache와 숨김 로컬 상태를 Git·리소스 목록에서 제외한다. 현재 구조의 최신 그래프를 새로 만들지는 않았다.
- 음원 가공은 보관 원본이나 --input 경로를 사용한다. 문자열 정리 도구는 실제 없는 기본 en.json 대신 --order를 요구하고 별도 출력 파일을 만든다.

## 로컬 백업과 배포 보관소

local-backups에는 Git 루트 이전 전 bundle·미커밋 ZIP/패치·해시 기록, 이전 Moonstone 패키지, 설정 백업, 폐기한 csproj·복원 캐시가 있다. 이번에 삭제하지 않았다. 로컬 폴더의 README에 각각의 복구 범위를 기록했다. Git 추적 문서인 이 문서에서도 보존 범위를 안내한다.

배포 보관소 추천은 프로젝트 밖 D:/area/archives/Afterlife/releases/<플랫폼>/<버전 또는 날짜>/다. 소스와 배포 파일을 분리하고 ZIP·SHA-256·버전·커밋 ID·빌드 결과를 함께 보관한다. 같은 디스크 경로 분리는 디스크 장애에 대비한 백업이 아니므로 공개본은 별도 디스크나 기존 백업 저장소에도 복사한다. 외부 경로 확정 전에는 docs/steam/builds의 ZIP4개를 보존한다.

## 사용자 확인

Unity에서 Prototype/Scenes/Afterlife_PixelPrototype과 Devlog/Youtube/Day01~03 씬을 열어 Missing Script·에셋 참조 오류가 없는지 확인한다. Play에서 본편 입력·HUD·음원과 각 회차 재생·이전/다음 이동·크레딧을 확인한다. 기대 결과는 이동 전 동작과 동일하다. 에이전트는 Play나 Unity 테스트를 실행하지 않았다. [정적 보존 검증](organization-verification.json)과 Logs/organization-dotnet-build.log를 확인한다.

Unity의 용도 없는 빈 폴더8개는 AssetDatabase로 제거한다. 제거 대상은 이전부터 파일이 없던 Presentation·Shared·World·실험 Prefabs/View/Shader·DC07/Day01 Rendering이며, 내용이 생긴 폴더는 삭제하지 않는다. 폴더 메타데이터 삭제도 에디터가 수행한다.

정적 확인: 의도적으로 삭제한 빈 폴더를 제외한 기존 .meta 2,754개는 바이트가 동일하다. Externals·회차 미디어547개와 현재 편집 프로젝트/미디어20개도 동일하다. 중복 GUID·메타 누락0, 리소스 목록1,993개 해시 일치, 유튜브 문서/대시보드 링크710개 누락0, 경제 계산 결과 재현 일치다.
