param([Parameter(Mandatory = $true)][string]$PythonExecutable)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$ttsEnvironment = Join-Path $taskRoot '.tooling\youtube-tts\venv'
if (-not (Test-Path -LiteralPath (Join-Path $ttsEnvironment 'Scripts\python.exe'))) {
    & $PythonExecutable -m venv $ttsEnvironment
    if ($LASTEXITCODE -ne 0) { throw 'TTS 가상환경 생성 실패' }
}
& (Join-Path $ttsEnvironment 'Scripts\python.exe') -m pip install -r (Join-Path $PSScriptRoot 'requirements.txt')
if ($LASTEXITCODE -ne 0) { throw 'TTS 의존성 설치 실패' }
Write-Output '환경 준비 완료. 기존 모델은 .tooling/youtube-tts/assets에 보존하거나 복원하세요.'
