# 프로젝트 폴더 정리와 운영

## 작업 목록 · 2026-10-07

- [x] 기존 채집 피드백·Day02·Day03·경제 기획 변경 검증과 목적별 커밋
- [x] Library와 외부 에셋을 보존하고 로그·빌드·임시 자료·Git 임시 객체 정리
- [x] TTS 실행 환경·추론 코드·라이선스를 유튜브 공용으로 분리
- [x] 리소스 목록 도구 공유, 분석 캐시 제외, 참고 자료·제작 원본 위치 정리
- [x] Unity 최상위 Legacy·Prototype·Modules·Samples·Devlog 구분과 경로 갱신
- [x] 최신 안내·출처·문서 링크와 대시보드 갱신
- [x] GUID·파일 보존·문서 링크·C# 컴파일 검증과 목적별 커밋

## 보존 범위

Unity Library와 Externals의 파일 내용은 보존한다. Recordings는 사용자 정리 영역이다. 현재 편집 프로젝트와 미디어, 음성 모델, 제작 원본, 로컬 복구 백업과 과거 배포 ZIP은 보존한다. 과거 배포 ZIP4개는 사용자가 확정한 외부 보관소로 이전했다.

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

Unity Logs에서는 설정 복구 자료·씬 확인 JSON과 종류별 마지막 텍스트 로그를 보존했다. [보존 목록](logs-retained.json)을 따른다. QA 이미지 증거는 docs/game-process/qa에 있다. 이전 Logs 파일1,041개·194,532,585바이트를 제거했고 새 구조 이전·컴파일 로그는 남긴다. 루트 Temp 205파일·877,119바이트와 logs 2파일·12,917바이트, Unity Builds 520파일·304,677,849바이트의 기존 내용은 비웠다. Library는 삭제하지 않았다. Recordings는 사용자 정리 영역으로 유지한다.

Git은 실행 중 작업이 없는 상태를 확인하고 2026-10-07 이전 tmp_obj 임시 파일51개·438,231,685바이트를 제거했다. git prune-packed로 이미 팩에 든 중복 loose 객체1,856개 (약22MiB)만 정리했으며 참조·reflog·과거 커밋 이력은 정리 대상으로 삼지 않았다. 확인 결과 garbage=0, prune-packable=0이다.

## 공용 제작 도구와 리소스

- TTS 추론 코드·요구 패키지·라이선스는 tools/tts, 모델·Python 환경은 .tooling/youtube-tts에 둔다. 이미 Day01·Day02가 공유했고 새 회차도 --day day-XX로 사용한다. 회차 입력·음성·필름은 그대로 보존한다.
- 가상환경 이동 후 Python 실행파일과 numpy·soundfile·onnxruntime·공용 helper import를 확인했다. 재설치는 tools/tts/setup.ps1을 사용한다. pip.exe 대신 python.exe -m pip를 사용한다.
- 리소스 목록 갱신은 tools/resources/update_manifest.py를 사용한다. 기존 devlog 명령은 공용 코드로 위임하는 호환 어댑터다.
- Kenney 참고 자료는 res/references/kenney로 모았다. 원본과 라이선스는 보존한다.
- 출력 이미지 폴더의 frame.aseprite는 내용이 다른 편집본이므로 res/art-source/pixel-art/frame-edited.aseprite로 옮겼다. 기존 frame.aseprite를 덮어쓰지 않았다.
- Graphify 최종 분석은 과거 기록으로 보존하고 실행 cache와 숨김 로컬 상태를 Git·리소스 목록에서 제외한다. 현재 구조의 최신 그래프를 새로 만들지는 않았다.
- 음원 가공은 보관 원본이나 --input 경로를 사용한다. 문자열 정리 도구는 실제 없는 기본 en.json 대신 --order를 요구하고 별도 출력 파일을 만든다.

## 재귀 조사에서 남긴 중복과 다음 정리 기준

조사 시작 시 루트와 자식 폴더의 파일 67,334개를 확인했다. Unity/Git 캐시를 제외한 비교에서 내용이 같은 그룹 386개·추가 사본 약296.5MiB를 찾았다. 파일 내용이 같아도 아래의 사용 목적이 달라 그대로 보존했다.

|위치·예시|보존 이유와 관리 기준|
|---|---|
|res의 제작 원본·출력과 Unity Assets의 임포트본|원본 보관과 엔진 실행 역할이 다르다. Assets 사본에는 GUID·임포트 설정이 있으므로 파일 크기만 보고 합치지 않는다.|
|회차 녹화 원본과 편집 프로젝트 .media 안의 MP4·WAV|편집 프로젝트가 가져온 미디어 사본을 참조한다. Day02 영상 사본 약77.5MiB를 포함해 편집 프로젝트와 함께 유지한다.|
|Day01 음원과 Assets/Devlog의 임포트 음원|영상 제작과 Unity 시연이 각각 소비한다. 공용 제작 원본 위치와 공개 크레딧을 문서로 연결한다.|
|외부 backups와 현재 소스의 이전 버전|재사용 파일이 아니라 복구 시점의 기록이다. 원본은 외부 보관소로 옮겼고 프로젝트 local-backups는 제거했다.|
|.tooling의 환경·모델과 공용 tools 코드|환경·모델은 로컬 실행 자료이며 tools는 버전 관리하는 실행 코드다. 회차별 환경을 추가하지 않고 youtube-tts 하나를 공유한다.|

Legacy의 실험 코드·리소스는 새 구조로 격리했다. 참조되지 않는 것처럼 보이는 에셋도 직렬화 GUID, Resources.Load, 문자열 경로로 사용할 수 있어 이번에는 삭제하지 않았다. 다음 정리는 사용자가 기존 기능의 폐기를 결정한 뒤 해당 소비자와 함께 기능 단위로 진행한다. 용량이 큰 Library·Git pack은 현재 프로젝트와 이력을 위한 데이터이므로 유지했다.

## 로컬 백업과 배포 보관소

local-backups는 게임 실행·빌드에 사용하지 않는다. Git 루트 이전 전 bundle·미커밋 ZIP/패치·해시 기록, 이전 Moonstone 패키지, 설정 백업268개 파일·363,761,587바이트를 D:/area/archives/Afterlife/backups/project-local-backups-2026-10-07로 옮기고 해시 일치를 검증했다. 재생성 가능한 unused-csproj 캐시128개 파일·2,347,476바이트는 삭제했다. 프로젝트 local-backups 폴더는 제거했다. [보관 기록](backup-archive.json)의 위치와 외부 archive-manifest.json을 통해 복구 파일을 찾을 수 있다. bundle의 모든 기준 커밋은 현재 Git에도 존재함을 확인했다. devlog·모델·현재 편집 프로젝트를 모두 포함한 전체 프로젝트 백업은 아니다.

배포 보관소는 사용자가 확정한 프로젝트 밖 D:/area/archives/Afterlife/releases/<플랫폼>/<버전>/다. 소스와 배포 파일을 분리하고 ZIP·SHA-256·버전·커밋 ID·빌드 결과를 함께 보관한다. 같은 디스크 경로 분리는 디스크 장애에 대비한 백업이 아니므로 공개본은 별도 디스크나 기존 백업 저장소에도 복사한다. 기존 Windows64 ZIP4개는 날짜별 폴더로 이전했고 SHA-256·ZIP CRC 검증을 통과했다. [보관 도구](../../tools/releases/README.md)와 [색인](../steam/archive-index.json)을 따른다. 과거 빌드 커밋은 미확인으로 비워 둔다.

## 사용자 확인

Unity에서 Prototype/Scenes/Afterlife_PixelPrototype과 Devlog/Youtube/Day01~03 씬을 열어 Missing Script·에셋 참조 오류가 없는지 확인한다. Play에서 본편 입력·HUD·음원과 각 회차 재생·이전/다음 이동·크레딧을 확인한다. 기대 결과는 이동 전 동작과 동일하다. 에이전트는 Play나 Unity 테스트를 실행하지 않았다. [정적 보존 검증](organization-verification.json)과 Logs/organization-dotnet-build.log를 확인한다.

Unity의 용도 없는 빈 폴더8개는 AssetDatabase로 제거했다. 제거 대상은 이전부터 파일이 없던 Presentation·Shared·World·실험 Prefabs/View/Shader·DC07/Day01 Rendering이며, 내용이 있는 폴더는 보존했다. 폴더 메타데이터 삭제도 에디터가 수행했다.

정적 확인: 의도적으로 삭제한 빈 폴더를 제외한 기존 .meta 2,754개는 바이트가 동일하다. Externals·회차 미디어547개와 현재 편집 프로젝트/미디어20개도 동일하다. 중복 GUID·메타 누락0, 리소스 목록1,993개 해시 일치, 유튜브 문서/대시보드 링크710개 누락0, 경제 계산 결과 재현 일치다.

전체 C# 검증의 재현 명령은 `powershell -File tools/unity/compile.ps1`이다. Unity DLL을 직접 참조하는 생성 프로젝트는 임시 NuGet 복원 캐시가 없어도 기존 모듈 검증과 같은 옵션으로 컴파일한다. 새 배포 빌드나 Unity 테스트는 수행하지 않는다.

구조 이전 직후 컴파일 결과: 생성 솔루션의 62개 프로젝트 통과, 오류0개·경고9개. 이후 Legacy 경고9개를 수정하고 해당 어셈블리 재컴파일에서 경고0개·오류0개를 확인했다. [경고별 원인·수정·검증](legacy-warning-fixes.md)을 따른다. Unity 실제 표시·Play 확인은 사용자 확인 항목으로 남는다. 문서 로컬 링크422개와 유튜브 링크710개는 누락0개다.

## 후속 작업 목록 · 2026-10-07

- [x] 폴더 이전 뒤 남은 셰이더/HLSL include 경로 확인
- [x] 공용 모듈·프로토타입 셰이더의 이전 경로 수정
- [x] include 대상 확인·Unity 임포트 반영·수정/사용자 확인 기록

- [x] 배포 보관소를 D:/area/archives/Afterlife/releases로 설정하고 Windows64 ZIP4개를 해시 검증해 이전
- [x] 복구 자료는 외부 backups에 보관하고 프로젝트 local-backups와 재생성 가능한 캐시 제거
- [x] Legacy CS0649 경고9개 원인 정리와 주입/비활성 카메라 코드 수정
- [x] 관련 C# 컴파일·운영 문서 갱신·목적별 커밋

## 셰이더 include 경로 복구 · 2026-10-07

Grass.shader가 이전 `Assets/Afterlife/Modules/Vegetation/Shaders/GrassPass.hlsl`을 참조해 임포트 오류가 발생했다. 폴더 이동은 GUID를 유지하지만 셰이더/HLSL의 문자열 include 경로는 자동으로 변경되지 않는다.

Modules/Prototype의21파일·47참조를 `Assets/Modules/`, `Assets/Prototype/`로 갱신했다. GrassPass의 Lighting/Wind/Dissolve 의존성과 LeafCards/Water/Smoke/HoverDust/ShowcaseSurface 및 프로토타입 표현도 같은 원인을 수정했다. 렌더링 계산·셰이더 이름·재질 참조·.meta는 직접 변경하지 않았다. 기존 모듈/프리팹을 그대로 사용하며 추가 설치나 별도 기능 모듈은 없다.

프로젝트 경로로 선언된 include47개가 실제 파일로 연결되고 누락0개다. 열린 Unity 에디터에 소스를 임포트한 뒤19셰이더의 저장된 임포트 진단을 읽어 오류0개·Play=false를 확인했다. C# 변경은 없으며 dotnet은 HLSL을 컴파일하지 않으므로 이 수정의 셰이더 검증으로 사용하지 않았다. Play/Test Runner·실제 GPU 화면 테스트는 수행하지 않았다. [변경/확인 기록](shader-include-path-fix.json).

사용자는 Day03과 본편 씬에서 풀이 정상 색으로 표시되고 물/나뭇잎/표면에 분홍 오류 재질이 없는지 확인한다. 새 include 오류가 Console에 나타나지 않아야 한다. 다음 폴더 이동에서는 GUID 보존과 함께 .shader/.hlsl/.cginc의 include 문자열도 검색해 갱신한다.
