Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Add-Problem([string]$message) {
    [void]$script:Problems.Add($message)
}

function Get-Sha256([string]$path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return (($sha.ComputeHash($stream) | ForEach-Object { $_.ToString('X2') }) -join '')
        }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Normalize-Rel([string]$fullPath, [string]$root) {
    return $fullPath.Substring($root.Length).TrimStart('\','/').Replace('\','/')
}

$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $Root
$Problems = New-Object System.Collections.ArrayList

$ContractPath = 'eng/m10974-exact-v9-cross-host-determinism-v2-contract.json'
$ManifestPath = 'eng/m10974-exact-v9-cross-host-determinism-v2-frozen-source-manifest.json'
if (-not (Test-Path -LiteralPath $ContractPath)) { Add-Problem 'missing V2 contract' }
if (-not (Test-Path -LiteralPath $ManifestPath)) { Add-Problem 'missing frozen source manifest' }
if ($Problems.Count -gt 0) { throw ("validator found {0} problem(s)" -f $Problems.Count) }

$Contract = Get-Content -LiteralPath $ContractPath -Raw | ConvertFrom-Json
$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json

if ($Contract.schema -ne 'm10974-exact-v9-cross-host-determinism-contract-v2') { Add-Problem 'contract schema drift' }
if ($Contract.algorithms.raw_v1.id -ne 'sha256-control-room-snapshot-v1') { Add-Problem 'raw V1 algorithm drift' }
if ($Contract.algorithms.cross_host_v2.id -ne 'sha256-control-room-snapshot-v2-presentation-canonical') { Add-Problem 'cross-host V2 algorithm drift' }
if ($Contract.algorithms.cross_host_v2.frozen_exact_v9_aggregate -ne '99B9D27A8F5791A194771D698E8A0740F7024058C172D645E2DF74F1B3C09E73') { Add-Problem 'cross-host V2 anchor drift' }
if ($Contract.algorithms.raw_v1.historical_local_exact_v9_aggregate -ne '7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418') { Add-Problem 'historical raw V1 anchor drift' }
if ($Contract.algorithms.raw_v1.cross_host_frozen -ne $false) { Add-Problem 'raw V1 must not be cross-host frozen' }
if ($Contract.prohibitions.physics_change_authorized -ne $false) { Add-Problem 'physics change unexpectedly authorized' }
if ($Contract.prohibitions.tolerance_change_authorized -ne $false) { Add-Problem 'tolerance change unexpectedly authorized' }
if ($Contract.prohibitions.vr2_r3_change_authorized -ne $false) { Add-Problem 'VR2/R3 change unexpectedly authorized' }
if ($Contract.prohibitions.accept_multiple_host_specific_raw_hashes -ne $false) { Add-Problem 'host-specific raw hash whitelist unexpectedly authorized' }

if (Test-Path -LiteralPath '.github/workflows/exact-v9-runtime-alignment-diagnostic.yml') {
    Add-Problem 'one-shot runtime-alignment workflow must be removed after adjudication'
}

$ExpectedManifestSha = [string]$Contract.frozen_source_manifest.sha256
if ((Get-Sha256 $ManifestPath) -ne $ExpectedManifestSha) { Add-Problem 'frozen source manifest hash drift' }

$Allowed = @{}
foreach ($item in $Manifest.excluded_allowed_files) { $Allowed[[string]$item] = $true }
$Expected = @{}
foreach ($prop in $Manifest.files.PSObject.Properties) { $Expected[$prop.Name] = [string]$prop.Value }

$Actual = @{}
foreach ($tree in @('src','tests')) {
    $TreeRoot = (Resolve-Path -LiteralPath $tree).Path
    foreach ($file in Get-ChildItem -LiteralPath $tree -File -Recurse) {
        $rel = Normalize-Rel $file.FullName $Root
        if ($rel -match '(^|/)(bin|obj)(/|$)') { continue }
        if ($Allowed.ContainsKey($rel)) { continue }
        $Actual[$rel] = $file.FullName
    }
}

foreach ($rel in $Expected.Keys) {
    if (-not $Actual.ContainsKey($rel)) {
        Add-Problem ("frozen file missing: {0}" -f $rel)
        continue
    }
    if ((Get-Sha256 $Actual[$rel]) -ne $Expected[$rel]) { Add-Problem ("frozen file hash drift: {0}" -f $rel) }
}
foreach ($rel in $Actual.Keys) {
    if (-not $Expected.ContainsKey($rel)) { Add-Problem ("unfrozen file present: {0}" -f $rel) }
}

foreach ($prop in $Contract.evidence_files.PSObject.Properties) {
    $rel = $prop.Name
    if (-not (Test-Path -LiteralPath $rel)) {
        Add-Problem ("evidence file missing: {0}" -f $rel)
        continue
    }
    if ((Get-Sha256 $rel) -ne [string]$prop.Value) { Add-Problem ("evidence file hash drift: {0}" -f $rel) }
}

foreach ($prop in $Contract.file_hashes.PSObject.Properties) {
    $rel = $prop.Name
    if (-not (Test-Path -LiteralPath $rel)) {
        Add-Problem ("required file missing: {0}" -f $rel)
        continue
    }
    if ((Get-Sha256 $rel) -ne [string]$prop.Value) { Add-Problem ("required file hash drift: {0}" -f $rel) }
}

$SourceMarker = 'NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-DETERMINISM-V2'
$FocusedMarker = 'NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-DETERMINISM-V2-FOCUSED'
$SourceFiles = @(
    'src/NuclearReactorSimulator.Application/Scenarios/Recording/ControlRoomSnapshotFingerprint.cs',
    'tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalExactV9ProductionActivationDecisionTests.cs'
)
foreach ($path in $SourceFiles) {
    $count = @((Get-Content -LiteralPath $path) | Where-Object { $_.Trim() -eq ("// {0}" -f $SourceMarker) }).Count
    if ($count -ne 1) { Add-Problem ("V2 marker cardinality drift: {0}" -f $path) }
}
$focusedPath = 'tests/NuclearReactorSimulator.Application.Tests/ControlRoom/MissionPerformance/M10974FingerprintV2CrossHostContractTests.cs'
$focusedCount = @((Get-Content -LiteralPath $focusedPath) | Where-Object { $_.Trim() -eq ("// {0}" -f $FocusedMarker) }).Count
if ($focusedCount -ne 1) { Add-Problem 'focused V2 marker cardinality drift' }

$ExactTest = Get-Content -LiteralPath 'tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalExactV9ProductionActivationDecisionTests.cs' -Raw
if ($ExactTest -notmatch [regex]::Escape('Assert.Equal(FrozenCrossHostV2DeterminismFingerprint, selectorCrossHostV2Fingerprint);')) { Add-Problem 'V2 frozen assertion missing' }
if ($ExactTest -match [regex]::Escape('Assert.Equal(HistoricalRawV1DeterminismFingerprint, selectorFingerprint);')) { Add-Problem 'raw V1 historical anchor is still asserted cross-host' }
if ($ExactTest -notmatch [regex]::Escape('Assert.Equal(directFingerprint, selectorFingerprint);')) { Add-Problem 'same-host raw V1 selector/direct assertion missing' }

if ($Problems.Count -gt 0) {
    Write-Host 'M10974 Exact-V9 Cross-Host Determinism Contract V2 validator: FAIL'
    foreach ($problem in $Problems) { Write-Host (" - {0}" -f $problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}

Write-Host 'M10974 Exact-V9 Cross-Host Determinism Contract V2 validator: PASS'
Write-Host 'Raw V1 remains same-host exact; frozen cross-host authority is V2 presentation-canonical.'
exit 0
