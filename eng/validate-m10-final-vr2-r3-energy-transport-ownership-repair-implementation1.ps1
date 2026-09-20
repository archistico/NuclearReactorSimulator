param(
    [Parameter(Mandatory=$true)][string]$Root,
    [string]$ArtifactDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Problems = New-Object System.Collections.Generic.List[string]
$ContractPath = Join-Path $Root 'eng/m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-contract.json'

function Add-Problem([string]$Message) { $Problems.Add($Message) }
function Require([bool]$Condition,[string]$Message) { if (-not $Condition) { Add-Problem $Message } }
function Sha256([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { return ([System.BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','') }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}
function Hex-ToBytes([string]$Hex) {
    $bytes = New-Object byte[] ($Hex.Length / 2)
    for ($i = 0; $i -lt $Hex.Length; $i += 2) {
        $bytes[$i / 2] = [Convert]::ToByte($Hex.Substring($i, 2), 16)
    }
    Write-Output -NoEnumerate $bytes
}
function Require-FileHash([string]$RelativePath,[string]$Expected) {
    $path = Join-Path $Root ($RelativePath.Replace('/',[System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Add-Problem ("missing file: {0}" -f $RelativePath); return }
    $actual = Sha256 $path
    if ($actual -cne $Expected) { Add-Problem ("hash drift: {0}" -f $RelativePath) }
}
function Get-FrozenTree([string]$RelativeRoot,[string[]]$ExcludedRelativePaths) {
    $base = Join-Path $Root $RelativeRoot
    $excluded = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($item in $ExcludedRelativePaths) { [void]$excluded.Add($item.Replace('\','/')) }
    [string[]]$relativePaths = @(
        Get-ChildItem -LiteralPath $base -Recurse -File | ForEach-Object {
            $_.FullName.Substring($Root.Length).TrimStart('\','/').Replace('\','/')
        } | Where-Object {
            $_ -notmatch '(^|/)(bin|obj)(/|$)' -and -not $excluded.Contains($_)
        }
    )
    [System.Array]::Sort($relativePaths,[System.StringComparer]::Ordinal)
    $memory = New-Object System.IO.MemoryStream
    try {
        $utf8 = New-Object System.Text.UTF8Encoding($false)
        foreach ($rel in $relativePaths) {
            $relBytes = $utf8.GetBytes($rel)
            $memory.Write($relBytes,0,$relBytes.Length)
            $memory.WriteByte(0)
            $hashBytes = Hex-ToBytes (Sha256 (Join-Path $Root $rel))
            $memory.Write($hashBytes,0,$hashBytes.Length)
            $memory.WriteByte(10)
        }
        $memory.Position = 0
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { $treeHash = ([System.BitConverter]::ToString($sha.ComputeHash($memory))).Replace('-','') }
        finally { $sha.Dispose() }
    }
    finally { $memory.Dispose() }
    return [pscustomobject]@{ Count=$relativePaths.Count; Sha256=$treeHash }
}
function Require-ExactMarker([string]$RelativePath,[string]$Marker) {
    $path = Join-Path $Root ($RelativePath.Replace('/',[System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Add-Problem ("missing marker file: {0}" -f $RelativePath); return }
    $count = @((Get-Content -LiteralPath $path) | Where-Object { $_ -ceq $Marker }).Count
    if ($count -ne 1) { Add-Problem ("marker cardinality drift: {0} :: {1}" -f $RelativePath,$Marker) }
}

if (-not (Test-Path -LiteralPath $ContractPath -PathType Leaf)) { throw 'implementation contract missing' }
$Contract = Get-Content -LiteralPath $ContractPath -Raw | ConvertFrom-Json

Require ($Contract.status -eq 'CANDIDATE-IMPLEMENTATION-FAST-GATE') 'implementation contract status drift'
Require ($Contract.planning_authority.planning_status -eq 'PASS-AS-AUTHORED') 'planning authority is not PASS-AS-AUTHORED'
Require ($Contract.planning_authority.selected_family -eq 'B') 'selected family drift'
Require ($Contract.planning_authority.repair_owner -eq 'ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT') 'repair owner drift'
Require ($Contract.planning_authority.next_authorized_gate -eq 'R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1') 'planning successor authority drift'

foreach ($entry in @($Contract.planning_authority.returned_artifacts)) { Require-FileHash ([string]$entry.path) ([string]$entry.sha256) }

$sourceTargets = @($Contract.production_surface | ForEach-Object { [string]$_.path })
$testTarget = [string]$Contract.focused_test.path
$srcTree = Get-FrozenTree 'src' $sourceTargets
$testTree = Get-FrozenTree 'tests' @($testTarget)
Require ($srcTree.Count -eq [int]$Contract.frozen_non_target_trees.src.file_count) ("non-target src count drift: {0}" -f $srcTree.Count)
Require ($srcTree.Sha256 -ceq [string]$Contract.frozen_non_target_trees.src.tree_sha256) 'non-target src tree drift'
Require ($testTree.Count -eq [int]$Contract.frozen_non_target_trees.tests.file_count) ("frozen tests count drift: {0}" -f $testTree.Count)
Require ($testTree.Sha256 -ceq [string]$Contract.frozen_non_target_trees.tests.tree_sha256) 'frozen tests tree drift'

foreach ($entry in @($Contract.production_surface)) { Require-FileHash ([string]$entry.path) ([string]$entry.post_sha256) }
Require-FileHash $testTarget ([string]$Contract.focused_test.sha256)
Require-FileHash ([string]$Contract.payload.path) ([string]$Contract.payload.sha256)
Require (-not [bool]$Contract.payload.mutation_allowed) 'payload mutation unexpectedly allowed'
Require (-not [bool]$Contract.payload.runtime_if97_allowed) 'runtime IF97 unexpectedly allowed'

Require ([double]$Contract.transport_acceptance.mode2_vs_if97_abs_delta_j_kg_max -eq 0.001d) 'IF97 transport guard drift'
Require ([double]$Contract.transport_acceptance.mode2_vs_raw_suction_abs_delta_j_kg_max -eq 0.001d) 'raw suction transport guard drift'
Require ([int]$Contract.transport_acceptance.steady_state_lookup_allocated_bytes_per_call_max -eq 0) 'allocation guard drift'
Require ([int]$Contract.fast_gate.running_steps -eq 100) 'fast-gate step count drift'
Require ([int]$Contract.fast_gate.max_envelope_violation_steps -eq 0) 'fast-gate envelope guard drift'
Require (-not [bool]$Contract.authority.r3_passed) 'R3 must remain RED'
Require (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 must remain blocked'
Require ([bool]$Contract.authority.no_retuning_on_red) 'no-retuning-on-RED guard drift'

Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-IMPLEMENTATION1'
Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-SURFACE'
Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-MODE-BEHAVIOR'
Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-CONSUMPTION-BOUNDARY'
Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-IMPLEMENTATION-FAST-GATE'
Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1_PREEXECUTION_REVIEW.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-IMPLEMENTATION1-PREEXECUTION-REVIEW'
Require-ExactMarker 'docs/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_IMPLEMENTATION1_PREEXECUTION_REVIEW.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-IMPLEMENTATION1-R3-RED'
Require-ExactMarker 'docs/PROJECT.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-IMPLEMENTATION1-CURRENT'
Require-ExactMarker 'docs/ROADMAP.md' 'NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-IMPLEMENTATION1-NEXT'

if ($Problems.Count -gt 0) {
    Write-Host 'M10 Final VR2 R3 Energy-Transport Ownership Repair Implementation 1 validator: FAIL'
    foreach ($problem in $Problems) { Write-Host (" - {0}" -f $problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}

Write-Host 'M10 Final VR2 R3 Energy-Transport Ownership Repair Implementation 1 validator: PASS'
Write-Host 'Family B production surface is bounded; R3 remains RED pending runtime fast gate.'

if ($ArtifactDirectory) {
    New-Item -ItemType Directory -Path $ArtifactDirectory -Force | Out-Null
    @(
        'static-validator=PASS',
        'planning-authority=PASS-AS-AUTHORED',
        'selected-family=B',
        'repair-owner=ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT',
        'non-target-src-count=' + $srcTree.Count,
        'non-target-src-tree-sha256=' + $srcTree.Sha256,
        'frozen-tests-count=' + $testTree.Count,
        'frozen-tests-tree-sha256=' + $testTree.Sha256,
        'c4-payload-sha256=' + [string]$Contract.payload.sha256,
        'r3=RED'
    ) | Set-Content -LiteralPath (Join-Path $ArtifactDirectory '01-static-contract-and-provenance.txt') -Encoding ASCII
}
