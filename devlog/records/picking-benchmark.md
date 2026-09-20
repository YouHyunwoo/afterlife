# Picking 데이터 생성 시간 실측

이 기록은 저장 에셋을 사용하던 시점의 측정이다. 이후 [런타임 생성으로 전환](runtime-picking.md)하면서 Picking 에셋은 제거했고, 측정 도구도 프리팹 원본 수집과 런타임 팩토리 호출 방식으로 변경했다.

- 측정: 2026-09-20 10:47 KST
- 환경: Windows 11, AMD Ryzen 7 7800X3D, Unity 6000.0.69f1
- 대상: Day01/Picking의 72개 에셋에 대응하는 원본 메시. 총 정점 21,053개, 삼각형 9,032개.
- 원본 결과: [picking-benchmark.json](picking-benchmark.json)

## 결과

|항목|측정값|
|---|---:|
|원본 메시 탐색·로드·측정 목록 준비|25.62ms|
|72개 메모리 생성, 첫 실행|2.87ms|
|72개 메모리 생성, 후속 31회 중앙값|1.034ms|
|72개 메모리 생성, 후속 31회 P95|1.083ms|
|72개 메모리 생성, 후속 31회 최대|1.086ms|
|72개 CreateAsset 저장·임포트, 1회|2,506.20ms|
|가장 느린 단일 메시 중앙값: AbandonedAltarV2_Mesh|0.0694ms|
|생성 결과 배열의 요소 데이터 크기 합계|975,064 bytes, 약 0.93MiB|

## 측정 방법과 범위

현재 열린 에디터에서 Play 모드 및 Test Runner 없이 생성 작업만 측정했다. `Day01PickingBenchmark.cs`는 `BakePickMesh`에서 캐시 조회와 AssetDatabase 저장을 제외한 경로를 재현한다. ScriptableObject 생성, 이름 지정, 정점·삼각형·경계 복사, 실제 `Day01PickColorUpgrade.CopyColors` 호출을 포함한다. 기존 캐시를 반환하는 시간을 측정한 것이 아니다.

원본 메시를 모두 준비한 뒤 첫 회와 추가 31회의 생성을 측정했다. 회차별 총 시간은 각 메시 생성 구간의 Stopwatch 시간을 합한 값이다. 생성물 해제·결과 집계·원본 탐색은 해당 생성 시간에서 제외했다. 최초 측정은 이 측정 함수의 첫 호출이며, 운영체제 파일 캐시를 비운 콜드 스타트는 아니다.

파일 저장은 별도로 준비한 72개 결과를 임시 폴더에 `AssetDatabase.CreateAsset`으로 하나씩 저장했다. 현재 생성 코드처럼 묶음 임포트 최적화는 사용하지 않았다. 기존 Picking 파일이나 참조는 변경하지 않았으며, 임시 폴더와 Unity가 생성한 메타데이터는 AssetDatabase로 정리했다. 전역 SaveAssets 및 씬 저장은 실행하지 않았다.

배열 데이터 크기는 요소 개수로 계산한 값으로, 원본 메시의 CPU 복사본·배열 헤더·ScriptableObject·중간 List 할당은 포함하지 않는다. 이 에디터의 `GC.GetAllocatedBytesForCurrentThread` 차이가 0으로 반환되어 JSON의 `medianManagedAllocationBytes: 0`은 유효한 할당량 측정값으로 사용하지 않는다. 할당이 없다는 뜻이 아니다.

## 런타임 생성 판단

현재 데이터 규모에서는 생성 연산 자체가 작다. 모델 종류별로 한 번 만들고 공유하는 런타임 캐시를 검토할 근거가 된다. 다만 이번 수치는 에디터 실측이며 Player/IL2CPP나 다른 기기의 프레임 시간을 보장하지 않는다. 디스크 로딩, 실제 플레이 중 GC·메모리 영향은 따로 확인해야 한다.

72개 중 12개 원본 메시의 `isReadable`이 false였다. Tree0~3, Rock0~3, AbandonedAltar, SwordTool, Ruins, House가 해당한다. 에디터에서 복사가 성공했다고 빌드된 게임에서도 동일하게 접근할 수 있는 것은 아니다. 현재 경로를 런타임으로 옮기려면 원본 Read/Write 활성화 또는 다른 데이터 공급 경로가 필요하다. Read/Write 활성화는 CPU 쪽 메시 데이터 유지에 따른 메모리 비용을 수반한다. [Unity Mesh.isReadable 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh-isReadable.html)

따라서 생성 비용 때문에 반드시 에셋을 Git에 유지해야 하는 것은 아니다. 다만 런타임 전환에는 원본 접근 설정, 모델별 공유 캐시, 기존 직렬화 참조 교체가 필요하며 이번 작업에서는 전환하지 않았다.

## 재측정과 변경 사항

에디터 전용 측정 코드만 추가했다. Unity 프로젝트의 `Library/Day01PickingBenchmark.request` 파일이 있을 때 다음 도메인 리로드에서 한 번 실행하고 요청 파일을 소비한다. 결과는 `Library/Day01PickingBenchmark.json`에 기록한다. 반복 실행하려면 요청 파일을 다시 만들고 스크립트 리로드를 수행한다. 일반 에디터 실행에서는 요청 파일이 없으면 아무 작업도 하지 않는다.

`Afterlife.Devlog.Day01.Editor.ModuleValidation.csproj`에 측정 파일을 포함하고 `dotnet build --no-restore`로 컴파일을 확인했다. 경고 0, 오류 0. 게임 동작 코드·원본 임포터·기존 Picking 에셋은 이번 측정을 위해 수정하지 않았다.
