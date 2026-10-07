[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ZipPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Version,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Platform,
    [string]$BuildCommit,
    [switch]$Move
)
$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'archive-settings.json') -Raw | ConvertFrom-Json
if (-not $Platform) { $Platform = $settings.default_platform }
if ($Platform -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') { throw 'Invalid platform.' }
if (-not [IO.Path]::IsPathRooted($settings.archive_root)) { throw 'Archive root must be absolute.' }

function Assert-NoLinks([string]$Path) {
    $candidate = [IO.Path]::GetFullPath($Path)
    while ($candidate) {
        if (Test-Path -LiteralPath $candidate) {
            if (((Get-Item -LiteralPath $candidate -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Archive/source path contains a link: $candidate"
            }
        }
        $parent = [IO.Path]::GetDirectoryName($candidate)
        if ($parent -eq $candidate) { break }
        $candidate = $parent
    }
}

$source = Get-Item -LiteralPath $ZipPath -Force
if ($source.PSIsContainer -or $source.Extension -ne '.zip') { throw 'Input must be a ZIP file.' }
$archiveRoot = [IO.Path]::GetFullPath($settings.archive_root).TrimEnd([IO.Path]::DirectorySeparatorChar)
$destinationDirectory = [IO.Path]::GetFullPath((Join-Path (Join-Path $archiveRoot $Platform) $Version))
if (-not $destinationDirectory.StartsWith($archiveRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination must remain inside the configured archive root.'
}
Assert-NoLinks $source.FullName
Assert-NoLinks $destinationDirectory
$destination = Join-Path $destinationDirectory $source.Name
if ([string]::Equals($source.FullName, $destination, [StringComparison]::OrdinalIgnoreCase)) { throw 'Input is already archived.' }
$sourceHash = (Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
Assert-NoLinks $destination
if (Test-Path -LiteralPath $destination) {
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourceHash) {
        throw "Conflicting archived file: $destination"
    }
} else {
    Copy-Item -LiteralPath $source.FullName -Destination $destination
}
if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourceHash) {
    throw 'Copied ZIP hash does not match the source.'
}
$metadataPath = $destination + '.manifest.json'
$checksumPath = $destination + '.sha256'
Assert-NoLinks $metadataPath
Assert-NoLinks $checksumPath
$record = [ordered]@{
    file = $source.Name
    platform = $Platform
    version = $Version
    bytes = $source.Length
    sha256 = $sourceHash
    build_commit = $BuildCommit
    archived_at_utc = [DateTime]::UtcNow.ToString('o')
    original_path = $source.FullName
    archive_path = $destination
}
# Existing metadata belongs to the original archival event and is retained on retries.
if (-not (Test-Path -LiteralPath $metadataPath)) {
    $record | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding UTF8
}
if (-not (Test-Path -LiteralPath $checksumPath)) {
    "$sourceHash  $($source.Name)" | Set-Content -LiteralPath $checksumPath -Encoding UTF8
}
if ($Move) {
    Assert-NoLinks $source.FullName
    if ((Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourceHash) {
        throw 'Source changed during archival; it will be retained.'
    }
    Remove-Item -LiteralPath $source.FullName
}
[pscustomobject]$record
