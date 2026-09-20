# 41 프로토타입 생성 도구 제거

M5 · 에디터 도구 정리

- [x] 씬 열기·에셋 생성 메뉴와 구현 제거
- [x] UI 설치 전용 코드 제거
- [x] Windows 빌드에서 자동 생성 호출 제거 및 누락 검증
- [x] 런타임 오류 안내 정리·컴파일 확인

## 제거 파일과 내용

- Editor/PrototypeTools.cs: Open 메뉴/함수, CreateAssets 메뉴/함수, EnsureFolder 제거. Windows 빌드의 에셋 생성/UI 설치 호출 제거. 저장된 씬/Settings/Pipeline 누락 시 명시적으로 실패하도록 검증. 기존 BuildWindows와 EmbedMoonstone 유지.
- Editor/PrototypeUiInstaller.cs: 파일 삭제. Install 및 UI 생성·보정·TMP 변환·스프라이트 생성 보조 코드 전체 제거. 외부 호출은 BuildWindows뿐임을 검색으로 확인.
- Runtime/PrototypeScene.cs, Runtime/PrototypeUgui.cs: 제거된 메뉴를 실행하라는 오류 안내를 씬/참조 확인 안내로 변경.

기존 씬, 프리팹, UI, 폰트, 렌더 에셋은 수정하지 않았다. .meta는 직접 조작하지 않았으며 Unity가 삭제된 스크립트의 메타를 정리한다. 이후 빌드는 씬/UI를 재설치하지 않고 저장된 콘텐츠를 사용한다.

검증: 프로토타입 C# 컴파일 오류·경고 0. 소스에서 세 메뉴명/PrototypeUiInstaller/CreateAssets 참조 잔존 없음. Unity 플레이/Windows 빌드 미실시. 사용자 확인: Unity 컴파일/임포트 후 Tools/Afterlife에서 세 항목이 사라졌는지 확인. 과거 문서의 세 메뉴 실행 안내는 더 이상 유효하지 않다.
