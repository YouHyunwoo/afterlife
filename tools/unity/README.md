# Unity C# 컴파일

저장소 루트에서 `powershell -File tools/unity/compile.ps1`을 실행한다. Unity가 생성한 솔루션과 기존 Editor/Library DLL 참조를 사용해 관련 csproj를 `dotnet build --no-restore`로 컴파일한다. Unity 실행·Play·테스트·배포 빌드는 수행하지 않는다.

Unity가 임시 복원 캐시를 비웠을 때의 NETSDK1004를 피하도록 기존 모듈 검증 방식과 같은 SkipResolvePackageAssets·GenerateDependencyFile=false를 지정한다. NuGet 패키지를 사용하는 별도 .NET 프로젝트의 검증용이 아니며 Unity DLL과 생성 프로젝트가 먼저 있어야 한다. 결과는 Unity `Logs/organization-dotnet-build.log`에 기록한다.

프로토타입 배포는 기존 `Afterlife.Prototype.Editor.PrototypeTools.BuildWindows`를 사용한다. 이 진입점은 Builds/PixelPrototypeWindows 내부만 비우고 새 결과를 만든다. 과거 배포본을 유지하려면 먼저 별도 보관소에 복사한다.
