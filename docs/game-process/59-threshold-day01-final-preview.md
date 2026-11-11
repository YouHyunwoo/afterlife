# 균열 내부에 Day01 마지막 After 단계 연결

2026-09-26. 사용자가 기본 프리팹 모습과 Day01 마지막 단계의 차이를 지적했다.

## 원인과 변경

이전에는 `After_PixelForest`의 저장 오브젝트만 중첩했고 MonoBehaviour를 비활성화했다. Day01의 `6 After`는 `Day01StageEffects.Create(..., 5, ...)`에서 풀·잎 카드·연기·둥근 해안선·지형 반응·수정 발광을 추가하므로 정적 프리팹만으로는 마지막 단계 표현이 완성되지 않는다.

`Day01FinalStagePreview`를 Day01 구성 계층에 추가하고 기존 `Day01StageEffects`를 phase 5로 재사용한다. 실제 `Devlog_Day01.unity`가 참조하는 공용 `SceneFeatures/Presets/After.asset`과 동일한 프로필 GUID를 확인해 연결했다. 타깃의 지형별 표현을 초기화하고 생성된 렌더러를 마을 프리뷰 레이어 30으로 연결한다. 수정 발광 볼륨은 원본 서비스와 같이 레이어 31을 유지한다.

촬영 HUD·단계 선택 키·채집 입력·오디오·메인 카메라 제어는 실행하지 않는다. 전용 카메라는 ThresholdPortal이 계속 소유한다. 시야 데모는 원본도 시작 시 꺼져 있으므로 켜지 않는다. 원본 Day01 씬/프리팹은 변경하지 않는다. Threshold 공용 런타임에 Day01 의존성을 추가하지 않고 선택적인 에디터 조합만 사용한다.

효과와 바람은 비활성화 시 해제하며 기존 전역 바람 값과 카메라 발광 설정은 서비스 Dispose로 복원한다. 현재 독립 샘플을 위한 프리뷰이므로 다른 활성 씬의 전역 바람 시스템과 동시 구동은 별도 통합 검토가 필요하다.

## 검증과 확인

열린 에디터의 `connect-day01-after` 요청으로 VoidEnvironment 프리팹에 Stage·ViewCamera·After Profile·SmokePrefab을 저장했다. 신규 .meta는 Unity가 생성했다.

- Threshold 관련 4개 어셈블리: 오류 0·경고 0.
- 원본 Day01 csproj의 `--no-restore`는 기존 project.assets.json 누락으로 중단됐다. 패키지를 복원하는 대신 Unity의 기존 DLL 참조를 이용하는 `Tools/Threshold/Build-Day01Preview.ps1`로 Day01 Runtime 전체를 별도 출력에 빌드했고 오류 0·경고 0이었다.
- Unity 플레이/Test Runner는 미실행. 효과의 실제 외관·최종 카메라 렌더링은 사용자 확인 대기다.

테스트 씬 재생 → 새 게임 → 첫 대사에서 대기한다. 이전의 단순한 수관에 잎 카드가 붙고 풀과 둥근 해안선, 굴뚝 연기, 수정 발광이 표현되는지 확인한다. 편집 모드에서는 실행 시 생성되는 효과가 아직 없으므로 재생 상태에서 비교해야 한다. 최종 640×320 화면과 균열의 물결·어두운 외곽은 유지한다.
