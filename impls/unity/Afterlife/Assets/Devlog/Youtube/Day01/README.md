# Devlog Day01

독립 씬: `Scenes/Devlog_Day01.unity`

제작 문서는 프로젝트 루트의 로컬 [유튜브 개발일지](../../../../../../../devlog/youtube/README.md)로 모았다. `devlog/`는 Git에서 제외하므로 아래 링크는 해당 로컬 자료가 있을 때만 열린다.

- [HTML 관리 화면](../../../../../../../devlog/youtube/index.html)
- [Day01 현재 상태](../../../../../../../devlog/youtube/day-01/status.md)
- [기능/조작법](../../../../../../../devlog/youtube/day-01/features.md)
- [모듈 안내](../../../../../../../devlog/youtube/architecture/modules.md)
- [에셋/프리팹](../../../../../../../devlog/youtube/architecture/assets.md)
- [기존 누적 개발 기록](../../../../../../../devlog/youtube/day-01/records/day-01-history.md)

프로토타입과 공유하는 모듈은 `Assets/Modules/`, 개발일지 전용 Cinematics·Rendering·Shoreline 모듈은 `Assets/Devlog/Youtube/Modules/`에 둔다. 고정 지도, 장면, 촬영 조합과 전용 프리팹은 이 Day01 폴더에 둔다. 초기 Unity 이동과 프리팹 생성 기록은 `devlog/youtube/day-01/records/unity-organization.json`에서 확인한다.

재촬영에 필요한 현재 `Data/Day01Film.asset`·`Data/Day01Narration.json`과 `Audio/Narration/`·`Audio/Music/Childhood-Day01.wav`는 Unity GUID 참조를 유지한다. 참조가 없는 이전 Film 백업과 음원 후보·음악 원본은 외부 원본 또는 Git 이력으로 복구할 수 있어 Unity 임포트본에서 제외했다.

현재 씬은 촬영용 6단계(`PresentationStages`)를 직접 참조한다. 이전 3단계 씬과 비활성 백업 단계, 완료된 자동 이관 코드는 제거했다. `Day01Showcase`는 Film 연동과 장면 조립을 맡고, 단계 활성화·화면 출력·단계 효과·포인터 입력·타격 표현·안내 UI는 Day01 전용 클래스로 나눴다. 단계 전환 또는 씬 종료 시 효과와 임시 출력을 해제한다.

- `Geometry/ContinuousTerrain/`: 3~6단계가 공유하는 지도 좌표별 연속 지형 메쉬.
- `Geometry/TileTerrain/`: 초기 단계의 `TerrainV2_2_*` 타일 메쉬와 공용 단위 블록.
- `Geometry/Water/`: 수면·해저 블록 메쉬.
- `Materials/Progression/Stage00_Before` ~ `Stage04_Interaction`: 단계별로 지도 좌표와 표현 설정을 분리한 재질. 최종 After의 기본 재질은 `Materials/`에 둔다.

에셋은 씬·단계 프리팹의 GUID 참조로 관리한다. 파일 경로를 옮길 때는 에셋과 Unity가 만든 `.meta`를 함께 이동해 GUID를 유지한다.
