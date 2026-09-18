param(
    [string]$LogicalPrefix = '',
    [switch]$Force
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
$restored = 0
$skipped = 0
try {
    foreach ($row in $rows) {
        $archiveRelative = $row.storage.Substring('bundled-compressed-archive:'.Length)
        $archivePath = Join-Path $repoRoot ($archiveRelative.Replace('/','\'))
        if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) { throw "Missing archive: $archivePath" }
        if (-not $archives.ContainsKey($archivePath)) {
            $archives[$archivePath] = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
        }
        $zip = $archives[$archivePath]
        $entry = $zip.GetEntry($row.logical_path)
        if ($null -eq $entry) { throw "Archive entry missing: $($row.logical_path)" }
        if ([int64]$entry.Length -ne [int64]$row.uncompressed_bytes) { throw "Archive entry size mismatch: $($row.logical_path)" }
        $entryStream = $entry.Open()
        try { $entryHash = Get-Sha256HexFromStream $entryStream }
        finally { $entryStream.Dispose() }
        if ($entryHash -ne $row.canonical_sha256.ToUpperInvariant()) { throw "Archive entry SHA-256 mismatch: $($row.logical_path)" }

        $target = Join-Path $repoRoot ('eng\frozen-evidence\ordinary\' + $row.logical_path.Replace('/','\'))
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            $existingHash = Get-FileSha256 $target
            if ($existingHash -eq $row.canonical_sha256.ToUpperInvariant()) { $skipped++; continue }
            if (-not $Force) { throw "Target exists with non-canonical content: $target" }
        }
        $targetDir = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetDir -PathType Container)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
        $entryStream = $entry.Open()
        try {
            $out = [System.IO.File]::Create($target)
            try { $entryStream.CopyTo($out) } finally { $out.Dispose() }
        }
        finally { $entryStream.Dispose() }
        if ((Get-FileSha256 $target) -ne $row.canonical_sha256.ToUpperInvariant()) { throw "Restored file SHA-256 mismatch: $target" }
        $restored++
    }
}
finally {
    foreach ($zip in $archives.Values) { $zip.Dispose() }
}

Write-Host ("Frozen large evidence restore PASS. Restored={0}; already-present={1}; matched={2}." -f $restored,$skipped,$rows.Count)
