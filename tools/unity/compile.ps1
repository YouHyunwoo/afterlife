# Unity 생성 프로젝트의 DLL 참조로 컴파일한다. Unity 실행·Play·테스트·NuGet 복원 없음.
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$unityProject = Join-Path $taskRoot 'impls\unity\Afterlife'
if (-not (Test-Path -LiteralPath (Join-Path $unityProject 'Afterlife.slnx'))) { throw 'Unity 생성 솔루션이 없습니다.' }
New-Item -ItemType Directory -Path (Join-Path $unityProject 'Logs') -Force | Out-Null
Push-Location -LiteralPath $unityProject
try {
    & dotnet build Afterlife.slnx --no-restore --nologo -v minimal -m:1 -p:SkipResolvePackageAssets=true -p:GenerateDependencyFile=false -p:EnableSourceControlManagerQueries=false *> Logs/organization-dotnet-build.log
    $buildExit = $LASTEXITCODE
    Get-Content -LiteralPath Logs/organization-dotnet-build.log -Tail 12
    if ($buildExit -ne 0) { throw "C# 컴파일 실패: $buildExit" }
} finally { Pop-Location }
