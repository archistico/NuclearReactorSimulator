param(
    [string]$LogicalPrefix = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-Sha256HexFromStream([System.IO.Stream]$Stream) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($Stream)
        return ([BitConverter]::ToString($hash)).Replace('-', '').ToUpperInvariant()
    }
    finally { $sha.Dispose() }
}

function Get-FileSha256([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try { return Get-Sha256HexFromStream $stream }
    finally { $stream.Dispose() }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot 'eng\frozen-evidence\large-payload-manifest.csv'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Missing manifest: $manifestPath" }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$rows = @(Import-Csv -LiteralPath $manifestPath | Where-Object { $_.storage -like 'bundled-compressed-archive:*' })
if ($LogicalPrefix) {
    $prefix = $LogicalPrefix.Replace('\','/').TrimStart('/')
    $rows = @($rows | Where-Object { $_.logical_path.StartsWith($prefix, [System.StringComparison]::Ordinal) })
}
if ($rows.Count -eq 0) { throw 'No bundled compressed payload rows matched the request.' }

$archives = @{}
$removed = 0
$absent = 0
try {
    foreach ($row in $rows) {
        $archiveRelative = $row.storage.Substring('bundled-compressed-archive:'.Length)
        $archivePath = Join-Path $repoRoot ($archiveRelative.Replace('/','\'))
        if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) { throw "Missing archive: $archivePath" }
        if (-not $archives.ContainsKey($archivePath)) { $archives[$archivePath] = [System.IO.Compression.ZipFile]::OpenRead($archivePath) }
        $zip = $archives[$archivePath]
        $entry = $zip.GetEntry($row.logical_path)
        if ($null -eq $entry) { throw "Archive entry missing: $($row.logical_path)" }
        $entryStream = $entry.Open()
        try { $entryHash = Get-Sha256HexFromStream $entryStream } finally { $entryStream.Dispose() }
        if ($entryHash -ne $row.canonical_sha256.ToUpperInvariant()) { throw "Archive entry SHA-256 mismatch: $($row.logical_path)" }
        if ([int64]$entry.Length -ne [int64]$row.uncompressed_bytes) { throw "Archive entry size mismatch: $($row.logical_path)" }

        $target = Join-Path $repoRoot ('eng\frozen-evidence\ordinary\' + $row.logical_path.Replace('/','\'))
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { $absent++; continue }
        if ((Get-FileSha256 $target) -ne $row.canonical_sha256.ToUpperInvariant()) { throw "Expanded payload is not canonical; refusing to delete: $target" }
        Remove-Item -LiteralPath $target -Force
        $removed++
    }
}
finally {
    foreach ($zip in $archives.Values) { $zip.Dispose() }
}

Write-Host ("Frozen ordinary compaction PASS. Removed={0}; already-compact={1}; matched={2}." -f $removed,$absent,$rows.Count)
