param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$HistoricalReuse
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10.9.8.5 integrated HMI closure validation failed: $Message" }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }
function Get-Sha256Hex([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try { $bytes = $sha256.ComputeHash($stream) }
        finally { if ($null -ne $sha256) { $sha256.Dispose() } }
    }
    finally { if ($null -ne $stream) { $stream.Dispose() } }
    return ([System.BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

$v1Path = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix.json'
$v2Path = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix-v2.json'
$m83Path = Join-Path $RepositoryRoot 'eng\m10983-degraded-fault-protection-takeover-matrix.json'
$m84Path = Join-Path $RepositoryRoot 'eng\m10984-replay-checkpoint-same-seed-integrity-matrix.json'
$m85Path = Join-Path $RepositoryRoot 'eng\m10985-manual-integrated-hmi-acceptance-contract.json'
foreach ($p in @($v1Path,$v2Path,$m83Path,$m84Path,$m85Path)) { Require (Test-Path -LiteralPath $p -PathType Leaf) "Required M10.9.8 contract file '$p' is missing." }
Require ((Get-Sha256Hex $v1Path) -eq '272e4eb2c958254c18cf19c1818006325ea0363c4f76eae7d8432fdb42d6da4e') 'Accepted M10.9.8.1 matrix v1 changed.'
Require ((Get-Sha256Hex $v2Path) -eq '218d341111e4fa273643dce7dc9a18a6b3285bc498869cd12784f0a3d51c3223') 'Validated M10.9.8.2 matrix v2 changed.'
Require ((Get-Sha256Hex $m83Path) -eq '3e5e4a2622cf8f445b1ad44901f0825d7d43ef50abfcecd764735b19aaa1ebf0') 'Validated M10.9.8.3 matrix changed.'
Require ((Get-Sha256Hex $m84Path) -eq 'def8d36e26973b2bdac8046f5f3fe3991dfcf6106ea19937866ab3c4ba86e7d3') 'Validated M10.9.8.4 Hotfix 1 matrix changed.'

$m = Get-Content -LiteralPath $m85Path -Raw | ConvertFrom-Json
Require ($m.schemaVersion -eq 1) 'schemaVersion must remain 1.'
Require ($m.milestone -eq 'M10.9.8.5') 'milestone mismatch.'
Require ($m.contractId -eq 'm10985-manual-integrated-hmi-acceptance-v1') 'contractId mismatch.'
Require ($m.baseline -eq 'M10.9.8.4 Hotfix 1 VALIDATED') 'baseline mismatch.'
Require ($m.productionRuntimeChanged -eq $false) 'M10.9.8.5 must not change production runtime.'
Require ($m.compiledSurfaceChanged -eq $false) 'M10.9.8.5 must not change compiled source.'
Require ($m.testSurfaceChanged -eq $false) 'M10.9.8.5 must not change test source.'
Require ($m.simulationPhysicsChanged -eq $false) 'M10.9.8.5 must not change Simulation physics.'
Require ($m.manualAcceptanceRequired -eq $true) 'Manual acceptance must remain mandatory.'
Require ($m.m1098ClosureAfterManualAcceptance -eq $true) 'M10.9.8 closure must require manual acceptance.'
Require ($m.m10ClosureRequiresFinalPreM11Validation -eq $true) 'M10 closure must remain blocked on the final pre-M11 validation.'
Require ($m.requiredAcceptanceText -eq 'M10.9.8.5 manual integrated HMI acceptance OK') 'Manual acceptance text changed.'
Require (@($m.automatedPrerequisites).Count -eq 4) 'Exactly four automated M10.9.8 prerequisites are required.'
Require (@($m.manualRoutes).Count -eq 12) 'Exactly twelve integrated manual routes are required.'
Require (@($m.manualRoutes.routeId | Select-Object -Unique).Count -eq 12) 'Manual route IDs must be unique.'
for ($i=1; $i -le 12; $i++) {
    $id = 'HMI-{0:D2}' -f $i
    Require (@($m.manualRoutes | Where-Object { $_.routeId -eq $id }).Count -eq 1) "$id missing or duplicated."
}

$checklistPath = Join-Path $RepositoryRoot 'docs\M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md'
$closurePath = Join-Path $RepositoryRoot 'docs\M10_9_8_CLOSURE.md'
$finalPlanPath = Join-Path $RepositoryRoot 'docs\M10_FINAL_PRE_M11_VALIDATION_PLAN.md'
$manualPath = Join-Path $RepositoryRoot 'docs\usermanual\MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md'
$limitationsPath = Join-Path $RepositoryRoot 'docs\KNOWN_MODEL_LIMITATIONS.md'
foreach ($p in @($checklistPath,$closurePath,$finalPlanPath,$manualPath,$limitationsPath)) { Require (Test-Path -LiteralPath $p -PathType Leaf) "Required closure document '$p' is missing." }
$checklist = Get-Content -LiteralPath $checklistPath -Raw
foreach ($id in 1..12 | ForEach-Object { 'HMI-{0:D2}' -f $_ }) { Require ($checklist.Contains($id)) "Checklist route '$id' is missing." }
Require ($checklist.Contains('M10.9.8.5 manual integrated HMI acceptance OK')) 'Checklist acceptance phrase is missing.'
$closure = Get-Content -LiteralPath $closurePath -Raw
Require ($closure.Contains('M10 remains OPEN')) 'Closure document must state that M10 remains open after M10.9.8 closure.'
Require ($closure.Contains('run-m10-final-validation.cmd')) 'Final cumulative M10 gate reference is missing.'
Require ($closure.Contains('run-m10-final-long-validation.cmd')) 'Final long M10 gate reference is missing.'
$manual = Get-Content -LiteralPath $manualPath -Raw
Require ($manual.Contains('M10.9.8.4 Hotfix 1 VALIDATED')) 'User manual current validated integration anchor is missing.'
$limitations = Get-Content -LiteralPath $limitationsPath -Raw
Require ($limitations.Contains('populated golden anchor')) 'Fingerprint-v1 limitation anchor is missing.'

$surfaceManifestPath = Join-Path $RepositoryRoot 'eng\m10985-baseline-compiled-test-surface.sha256'
Require (Test-Path -LiteralPath $surfaceManifestPath -PathType Leaf) 'M10.9.8.5 baseline compiled/test surface manifest is missing.'
Require ((Get-Sha256Hex $surfaceManifestPath) -eq 'a568dacb03842dcdd826f9070f56360159c09326b586274446347c0ecec28100') 'M10.9.8.5 baseline compiled/test surface manifest changed.'
if (-not $HistoricalReuse) {
    $manifest = @{}
    foreach ($line in @(Get-Content -LiteralPath $surfaceManifestPath)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split '\|', 2
        Require ($parts.Count -eq 2) 'Malformed compiled/test surface manifest line.'
        $manifest[$parts[1]] = $parts[0]
    }
    Require ($manifest.Count -eq 1286) 'Compiled/test surface manifest entry count changed.'
    $current = @{}
    foreach ($dir in @('src','tests')) {
        $base = Join-Path $RepositoryRoot $dir
        Require (Test-Path -LiteralPath $base -PathType Container) "Required surface directory '$dir' is missing."
        foreach ($file in @(Get-ChildItem -LiteralPath $base -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|Evidence)[\\/]' })) {
            $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\','/').Replace('\','/')
            $current[$relative] = $file.FullName
        }
    }
    Require ($current.Count -eq $manifest.Count) 'Compiled/test surface file count differs from the M10.9.8.4 Hotfix 1 validated baseline.'
    foreach ($relative in $manifest.Keys) {
        Require ($current.ContainsKey($relative)) "Baseline compiled/test file missing: $relative"
        Require ((Get-Sha256Hex $current[$relative]) -eq $manifest[$relative]) "Compiled/test surface changed: $relative"
    }
} else {
    Write-Host 'M10.9.8.5 compiled/test surface hash check skipped in historical-reuse mode.'
}

Write-Host 'M10.9.8.5 integrated HMI closure contract validation passed.'
