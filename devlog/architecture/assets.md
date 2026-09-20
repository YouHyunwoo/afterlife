# 에셋과 프리팹

## 원본 → 임포트 → 장면

|경로|소유하는 것|수정 원칙|
|---|---|---|
|res/art-source/|Blender 등 제작 원본|모델 제작 작업|
|res/audio/|선정·분석 음원과 출처|원본/편집본 구분, 출처 보존|
|res/textures/vegetation/|사용자 제공 4종 풀 PNG 보관본|알파 유지, 픽셀 텍스처|
|res/models/world/|기존 공유 FBX 17종 보관본|Unity 공유 원본과 동기화 필요|
|Assets/Devlog/Modules/|공용 코드·셰이더·런타임 리소스|특정 에피소드 참조 금지|
|Assets/Devlog/Day01/|씬·배치·생성 메시·재질·프리팹|촬영 구도와 구성|
|Assets/_AfterlifePrototype/|게임과 공유하는 모델/기존 음원|기존 소비자를 깨뜨리지 않도록 현재 위치 유지|

Unity는 루트 res를 직접 임포트하지 않는다. res는 엔진 비의존 보관본이며 실제 장면은 Assets의 GUID를 참조한다. 복사본 수정이 자동 전파되지는 않는다. 에셋 이동은 AssetDatabase.MoveAsset, 프리팹 저장은 PrefabUtility를 사용한다.

## 프리팹 구성

|위치|내용|재사용 범위|
|---|---|---|
|Modules/Lighting/Prefabs/ChimneySmoke|수명·크기·노이즈가 설정된 연기 emitter|모듈이 굴뚝 위치/재질을 연결|
|Modules/Interaction/Prefabs/HitChip|표면 파편 기본 큐브|독립 효과 템플릿|
|Day01/Prefabs/Resources|완성 공간의 나무·바위 자원|자원 배치와 설정|
|Day01/Prefabs/Environment|집·폐허 등 환경 구성|환경 배치|
|Day01/Prefabs/Terrain|단계별 지형 묶음|고정 지도 스냅샷|
|Day01/Prefabs/Stages|Before / Process / After|촬영 공간 단위|
|Day01/Prefabs/Tools, Camera|도구와 카메라|조립 참조를 다시 연결하는 템플릿|
|Day01/Prefabs/Composition|전체 Day01Showcase|씬 구성 재사용의 기준|

실제 생성 완료 여부와 경로 목록은 [Unity 처리 보고서](../records/unity-organization.json)를 확인한다. 단독 카메라/도구 프리팹은 완성 씬 전체의 연결을 보장하지 않는다. 전체 구성 프리팹을 우선 사용한다.

동적인 눈 덮개, 침식된 메시, 호버 흔적, 해안 선택 결과는 런타임에 생성한다. 이를 고정 프리팹으로 저장하면 현재 지형/타깃과 맞지 않으므로 생성 코드를 모듈로 유지했다. 연기 emitter는 프리팹 설정을 사용하고 모듈이 위치·재질을 연결한다. 흙먼지는 실행 중 모듈이 구성하고 정리한다.

## 설정

ShowcaseAudioBank.asset에서 실제 음원과 볼륨을 바꾼다. Day01Destruction.asset에서 파괴 수치를 바꾼다. HitChip은 파편의 원형이다. 변경한 에셋이 여러 프리팹에서 공유되면 모든 소비자에 반영되므로 에피소드별 차이는 복제한 설정 에셋으로 연결한다.

Resources에는 사용 중인 음원 4개를 두는 구조다. 이전 후보의 Audio/Archive 이동은 후속 정리 코드가 다음 에디터 임포트에 수행한다. 실제 공개용 크레딧 지침은 [audio-credits](../../docs/audio-credits.md)다.
