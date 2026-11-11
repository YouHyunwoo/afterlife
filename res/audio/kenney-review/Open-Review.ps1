$ErrorActionPreference = 'Stop'
$reviewRoot = $PSScriptRoot
$reviewUrl = 'http://127.0.0.1:8766/'
$running = $false
try {
    $response = Invoke-WebRequest -Uri $reviewUrl -UseBasicParsing -TimeoutSec 2
    $running = $response.Content.Contains('AFTERLIFE / SOUND PALETTE')
    if (-not $running) { throw 'Port 8766 is already used by a different service.' }
} catch {
    if ($_.Exception.Message -like '*already used*') { throw }
}
if (-not $running) {
    $pythonExe = (Get-Command python -ErrorAction Stop).Source
    Start-Process -FilePath $pythonExe -ArgumentList '-m http.server 8766 --bind 127.0.0.1' -WorkingDirectory $reviewRoot -WindowStyle Hidden
}
Start-Process $reviewUrl
