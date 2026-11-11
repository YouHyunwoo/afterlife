# 33 화면 출력용 카메라

M5 · 화면 출력 개선

- [x] 월드 카메라를 유지하며 Display 1 전용 빈 카메라 추가 도구
- [x] 중복 방지, Undo, 사용자 씬 편집 보존
- [x] C# 컴파일 및 확인 절차 문서화


기존 월드 카메라는 RenderTexture를 계속 렌더링한다. 추가 Display Camera는 Culling Mask=Nothing으로 오브젝트를 다시 그리지 않고 검은 배경만 화면에 출력한다. Overlay Canvas가 그 위에 월드 이미지/UI를 표시한다. HDR/MSAA/후처리/그림자/Depth 및 Color Texture 요청/오클루전 컬링을 끈다. 추가 카메라와 화면 클리어 비용은 존재하며 실제 성능은 미측정이다.

PrototypeDisplayCameraInstaller가 스크립트 리로드 후 편집 모드 또는 플레이 종료 후 열린 프로토타입 씬에 설치한다. 기존 활성 Display 1 카메라가 있으면 추가하지 않는다. Undo 가능, 자동 저장 없음. 수동 메뉴 Tools > Afterlife > Add Display Camera. 설치 후 사용자 씬 저장 필요. 현재 씬 적용은 아직 확인하지 못했다.

C# 오류·경고 0. Unity 플레이 테스트/Windows 빌드 미실시. 사용자 확인: 플레이 종료 후 컴파일 → Hierarchy의 Display Camera 확인 → 씬 저장 → 재생하여 게임/UI 정상 표시 및 No cameras rendering 안내 제거 확인.

## 후속 Tools 정리

- [x] Convert HUD Resources to TMP 메뉴와 전용 진입 함수 제거
- [x] 기존 TMP 에셋/설치 경로 보존 및 컴파일 확인

메뉴 등록과 ConvertOpenResourcesToTmp 전용 진입 함수를 제거했다. 씬 설치의 내부 TMP 처리 및 기존 에셋은 유지한다. C# 오류·경고 0. Unity 컴파일 후 Tools > Afterlife에서 해당 항목이 사라졌는지 사용자 확인 필요.
