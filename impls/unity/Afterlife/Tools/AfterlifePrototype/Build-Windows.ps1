# Unity CLI로 프로토타입 씬만 빌드한다. 실행/플레이 테스트는 수행하지 않는다.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unityPath = 'C:/Program Files/Unity/Hub/Editor/6000.0.69f1/Editor/Unity.exe'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity 에디터가 실행 중입니다. 프로젝트를 닫은 후 실행하세요.' }
if (-not (Test-Path -LiteralPath $unityPath)) { throw "Unity 실행 파일이 없습니다: $unityPath" }
$runRoot = Join-Path $projectRoot ('Logs/PrototypeBuild/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$settings = @('ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','ProjectSettings/ProjectSettings.asset','ProjectSettings/EditorBuildSettings.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')
foreach ($relativePath in $settings) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $relativePath) -Destination (Join-Path $runRoot ([IO.Path]::GetFileName($relativePath)))
}
$logPath = Join-Path $runRoot 'unity-build.log'
$arguments = @('-batchmode','-quit','-projectPath',('"' + $projectRoot + '"'),'-buildTarget','Win64','-executeMethod','Afterlife.Prototype.Editor.PrototypeTools.BuildWindows','-logFile',('"' + $logPath + '"'))
try {
    $buildProcess = Start-Process -FilePath $unityPath -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
    $buildProcess.WaitForExit()
    if ($buildProcess.ExitCode -ne 0) { throw "Unity 빌드 실패 ($($buildProcess.ExitCode)). 로그: $logPath" }
    Get-Content -LiteralPath (Join-Path $projectRoot 'Builds/PixelPrototypeWindows/build-summary.txt')
} finally {
    foreach ($relativePath in $settings) {
        Copy-Item -LiteralPath (Join-Path $runRoot ([IO.Path]::GetFileName($relativePath))) -Destination (Join-Path $projectRoot $relativePath) -Force
    }
}
