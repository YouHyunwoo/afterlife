# 배포 ZIP 보관

기본 보관소는 `archive-settings.json`의 `D:/area/archives/Afterlife/releases`다. `<플랫폼>/<버전>/` 아래 ZIP·SHA-256·보관 메타데이터를 함께 둔다. 프로젝트에는 배포 파일 대신 [색인](../../docs/steam/archive-index.json)을 남긴다.

저장소 루트에서 실행한다. 기본 플랫폼은 Windows64이며 다른 플랫폼은 `-Platform`으로 지정한다.

```powershell
powershell -File tools/releases/archive_release.ps1 -ZipPath impls/unity/Afterlife/Builds/Afterlife-2026-10-07.zip -Version 2026-10-07 -BuildCommit <빌드에_사용한_커밋>
```

기본은 원본을 유지하고 복사한다. `-Move`는 복사본과 원본의 SHA-256 일치를 확인한 뒤 원본 ZIP만 삭제한다. 이름이 같은 다른 내용은 덮어쓰지 않으며 경로의 링크·정션을 거부한다. 버전·플랫폼 이름은 단일 폴더 이름으로 제한한다. 기존 보관본과 해시가 같으면 재실행할 수 있다.

과거 ZIP4개의 빌드 커밋은 미확인이므로 메타데이터에서 비워 뒀다. 보관 시점의 최신 커밋을 빌드 커밋으로 기입하지 않는다. 이 도구는 ZIP 생성·새 게임 빌드·Steam 업로드를 수행하지 않는다.

검증은 원본/보관본 해시 비교와 ZIP 무결성 검사다. 복구할 때 ZIP을 원하는 별도 폴더에 풀어 사용한다. 현재 보관소는 같은 디스크의 정리 위치이며, 디스크 장애 복구용 복사본은 별도 백업 장치에 보관한다.
