param(
    [string]$ArtifactDirectory = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Problems = New-Object System.Collections.Generic.List[string]
$Root = Split-Path -Parent $PSScriptRoot
$ContractPath = Join-Path $Root 'eng\m10-final-vr2-r3-post-repair-dynamic-equilibrium-replanning1-contract.json'

function Add-Problem([string]$Message) {
    $Problems.Add($Message)
}

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { Add-Problem $Message }
}

function Get-Sha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha.ComputeHash($stream)
            return ([BitConverter]::ToString($bytes)).Replace('-', '')
        }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-StringSha256([string]$Text) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally { $sha.Dispose() }
}

function Normalize-Relative([string]$FullPath) {
    $rootPrefix = $Root.TrimEnd('\') + '\'
    if (-not $FullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "path outside repository root: $FullPath"
    }
    return $FullPath.Substring($rootPrefix.Length).Replace('\', '/')
}

function Get-FrozenTree([string]$RelativeRoot) {
    $fullRoot = Join-Path $Root $RelativeRoot
    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($file in [IO.Directory]::EnumerateFiles($fullRoot, '*', [IO.SearchOption]::AllDirectories)) {
        $rel = Normalize-Relative $file
        if ($rel -match '(^|/)(bin|obj)(/|$)') { continue }
        $rows.Add([PSCustomObject]@{
            Path = $rel
            Sha256 = Get-Sha256 $file
        })
    }

    $array = $rows.ToArray()
    [Array]::Sort($array, [System.Collections.Generic.Comparer[object]]::Create(
        [System.Comparison[object]]{
            param($a, $b)
            return [StringComparer]::Ordinal.Compare([string]$a.Path, [string]$b.Path)
        }))

    $sb = New-Object Text.StringBuilder
    foreach ($row in $array) {
        [void]$sb.Append([string]$row.Path)
        [void]$sb.Append('|')
        [void]$sb.Append([string]$row.Sha256)
        [void]$sb.Append("`n")
    }

    return [PSCustomObject]@{
        Count = $array.Count
        Sha256 = Get-StringSha256 $sb.ToString()
    }
}

function Require-FileHash([string]$RelativePath, [string]$Expected) {
    $path = Join-Path $Root ($RelativePath.Replace('/', '\'))
    if (-not [IO.File]::Exists($path)) {
        Add-Problem "missing file: $RelativePath"
        return
    }
    $actual = Get-Sha256 $path
    if (-not [string]::Equals($actual, $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        Add-Problem "sha drift: $RelativePath"
    }
}

function Require-ExactMarker([string]$RelativePath, [string]$Marker) {
    $path = Join-Path $Root ($RelativePath.Replace('/', '\'))
    if (-not [IO.File]::Exists($path)) {
        Add-Problem "missing marker file: $RelativePath"
        return
    }
    $count = 0
    foreach ($line in [IO.File]::ReadAllLines($path)) {
        if ([string]::Equals($line.Trim(), $Marker, [StringComparison]::Ordinal)) { $count++ }
    }
    if ($count -ne 1) {
        Add-Problem ("marker cardinality drift: {0} => {1}" -f $Marker, $count)
    }
}

if (-not [IO.File]::Exists($ContractPath)) {
    throw 'planning contract missing'
}
$Contract = ConvertFrom-Json ([IO.File]::ReadAllText($ContractPath))

Require ($Contract.schema -eq 'm10-final-vr2-r3-post-repair-dynamic-equilibrium-replanning1-v1') 'schema drift'
Require ($Contract.status -eq 'PLANNING-CANDIDATE') 'planning status drift'

$srcTree = Get-FrozenTree 'src'
$testTree = Get-FrozenTree 'tests'
Require ($srcTree.Count -eq [int]$Contract.baseline.src.file_count) ("src count drift: {0}" -f $srcTree.Count)
Require ($srcTree.Sha256 -eq [string]$Contract.baseline.src.tree_sha256) 'src tree drift'
Require ($testTree.Count -eq [int]$Contract.baseline.tests.file_count) ("tests count drift: {0}" -f $testTree.Count)
Require ($testTree.Sha256 -eq [string]$Contract.baseline.tests.tree_sha256) 'tests tree drift'

foreach ($entry in @($Contract.baseline.family_b_production_files)) {
    Require-FileHash ([string]$entry.path) ([string]$entry.sha256)
}

foreach ($prop in $Contract.evidence.PSObject.Properties) {
    Require-FileHash ([string]$prop.Value.path) ([string]$prop.Value.sha256)
}

foreach ($prop in $Contract.documents.PSObject.Properties) {
    Require-FileHash ([string]$prop.Value.path) ([string]$prop.Value.sha256)
}

foreach ($prop in $Contract.document_markers.PSObject.Properties) {
    foreach ($marker in @($prop.Value)) {
        Require-ExactMarker ([string]$prop.Name) ([string]$marker)
    }
}

Require ($Contract.adjudication.family_b_causal_closure -eq 'POST-REPAIR-CAUSAL-CLOSURE-CONFIRMED') 'causal closure drift'
Require ($Contract.adjudication.historical_fast_gate_as_family_b_discriminator -eq 'INVALID-HISTORICAL-CONTRADICTION') 'historical fast-gate contradiction drift'
Require ($Contract.adjudication.dynamic_classification -eq 'POST-REPAIR-DYNAMIC-EQUILIBRIUM-RECONSTRUCTION-REQUIRED') 'dynamic classification drift'
Require ($Contract.adjudication.primary_hydraulic_owner -eq 'POST-REPAIR-OPERATING-POINT-RESIDUAL') 'primary owner drift'
Require ($Contract.adjudication.governor_owner -eq 'COMMON-MODE-CONTROLLER-INITIAL-STATE-RESIDUAL') 'governor owner drift'

Require ($Contract.returned_metrics.post_repair_causal_closure.classification -eq 'POST-REPAIR-CAUSAL-CLOSURE-CONFIRMED') 'returned causal classification drift'
Require ([double]$Contract.returned_metrics.post_repair_causal_closure.mass_identity_abs_residual_kg_s -eq 0.0) 'mass residual drift'
Require ([double]$Contract.returned_metrics.post_repair_causal_closure.transport_specific_energy_abs_delta_j_kg -le 0.001) 'transport delta no longer inside frozen causal budget'
Require ([double]$Contract.returned_metrics.post_repair_causal_closure.observed_net_energy_abs_w -le 0.10223668223103162) 'observed net-energy no longer inside frozen causal budget'
Require ([double]$Contract.returned_metrics.post_repair_causal_closure.production_energy_identity_abs_residual_w -le 0.001) 'energy identity residual no longer inside frozen causal budget'

Require ([int]$Contract.returned_metrics.historical_fast_gate.violation_steps -eq 86) 'historical violation count drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.violation_steps -eq 84) 'post-repair violation count drift'
Require ([int]$Contract.returned_metrics.historical_fast_gate.first_violation_step -eq 15) 'historical first violation drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.first_violation_step -eq 17) 'post-repair first violation drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.electrical_violation_steps -eq 0) 'post-repair electrical safety drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.drum_level_violation_steps -eq 0) 'post-repair drum-level safety drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.trip_steps -eq 0) 'post-repair trip drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.breaker_open_steps -eq 0) 'post-repair breaker drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.rollback_steps -eq 0) 'post-repair rollback drift'
Require ([int]$Contract.returned_metrics.post_repair_fast_gate.nonfinite_steps -eq 0) 'post-repair nonfinite drift'
Require ([bool]$Contract.returned_metrics.comparison.primary_flow_drift_reversed) 'primary-flow drift reversal evidence missing'
Require (-not [bool]$Contract.returned_metrics.comparison.historical_zero_envelope_valid_as_family_b_discriminator) 'historical zero-envelope incorrectly restored as Family B discriminator'

Require ($Contract.next_diagnostic.id -eq 'R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1') 'next diagnostic drift'
Require (-not [bool]$Contract.next_diagnostic.production_change_authorized) 'next diagnostic must remain production-neutral'
Require ([bool]$Contract.next_diagnostic.test_only_diagnostic_authorized) 'test-only diagnostic authority missing'

Require ([bool]$Contract.frozen_constraints.family_b_transport_ownership_locked) 'Family B lock missing'
Require (-not [bool]$Contract.frozen_constraints.threshold_change_allowed) 'threshold changes must remain forbidden'
Require (-not [bool]$Contract.frozen_constraints.pump_governor_gain_retuning_allowed) 'control gain retuning must remain forbidden'
Require (-not [bool]$Contract.frozen_constraints.hydraulic_resistance_retuning_allowed) 'hydraulic resistance retuning must remain forbidden'
Require (-not [bool]$Contract.frozen_constraints.steam_capacity_retuning_allowed) 'steam capacity retuning must remain forbidden'
Require (-not [bool]$Contract.frozen_constraints.c4_payload_change_allowed) 'C4 mutation must remain forbidden'
Require (-not [bool]$Contract.frozen_constraints.raw_seed_change_allowed_now) 'raw seed change must not be authorized yet'
Require (-not [bool]$Contract.frozen_constraints.controller_state_change_allowed_now) 'controller state change must not be authorized yet'
Require (-not [bool]$Contract.frozen_constraints.r3_passed) 'R3 must remain RED'
Require (-not [bool]$Contract.frozen_constraints.requalification3_authorized) 'Requalification 3 must remain blocked'
Require (-not [bool]$Contract.frozen_constraints.r4_planning_authorized) 'R4 must remain blocked'
Require ($Contract.authority.next_authorized_gate_after_planning_pass -eq 'R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1') 'next gate authority drift'

if ($Problems.Count -gt 0) {
    Write-Host 'M10 Final VR2 R3 Post-Repair Dynamic-Equilibrium Replanning 1 validator: FAIL'
    foreach ($problem in $Problems) { Write-Host (" - {0}" -f $problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}

Write-Host 'M10 Final VR2 R3 Post-Repair Dynamic-Equilibrium Replanning 1 validator: PASS'
Write-Host 'Family B causal closure stays frozen; next authority is diagnostic-only.'

if ($ArtifactDirectory) {
    [IO.Directory]::CreateDirectory($ArtifactDirectory) | Out-Null
    $ascii = [Text.Encoding]::ASCII

    $p1 = Join-Path $ArtifactDirectory '01-provenance.txt'
    [IO.File]::WriteAllLines($p1, @(
        'status=PASS-AS-AUTHORED',
        'planning-id=R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-REPLANNING1',
        'family-b-causal-closure=POST-REPAIR-CAUSAL-CLOSURE-CONFIRMED',
        'historical-fast-gate-discriminator=INVALID-HISTORICAL-CONTRADICTION',
        ('src-count={0}' -f $srcTree.Count),
        ('src-tree-sha256={0}' -f $srcTree.Sha256),
        ('tests-count={0}' -f $testTree.Count),
        ('tests-tree-sha256={0}' -f $testTree.Sha256),
        'production-change=False',
        'test-source-change=False'
    ), $ascii)

    $p2 = Join-Path $ArtifactDirectory '02-residual-owners.txt'
    [IO.File]::WriteAllLines($p2, @(
        'status=PASS-AS-AUTHORED',
        'classification=POST-REPAIR-DYNAMIC-EQUILIBRIUM-RECONSTRUCTION-REQUIRED',
        'primary-hydraulic-owner=POST-REPAIR-OPERATING-POINT-RESIDUAL',
        'primary-flow-pre-delta-kg-s=-0.6840714032278186',
        'primary-flow-post-delta-kg-s=0.4220335386604859',
        'primary-flow-drift-reversed=True',
        'governor-owner=COMMON-MODE-CONTROLLER-INITIAL-STATE-RESIDUAL',
        'governor-pre-delta-percent=0.16301471662901434',
        'governor-post-delta-percent=0.1630149835634036',
        'governor-post-minus-pre-delta-percent=2.669343892591769E-07'
    ), $ascii)

    $p3 = Join-Path $ArtifactDirectory '03-planning-summary.txt'
    [IO.File]::WriteAllLines($p3, @(
        'status=PASS-AS-AUTHORED',
        'selected-production-repair=NONE',
        'next-authorized-gate=R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1',
        'future-families=H-HYDRAULIC-SEED-RECONSTRUCTION;C-CONTROLLER-STATE-INITIALIZATION;J-JOINT-EQUILIBRIUM-RECONSTRUCTION',
        'threshold-change=False',
        'physics-retuning=False',
        'seed-change-authorized-now=False',
        'controller-state-change-authorized-now=False',
        'r3-passed=False',
        'requalification3-authorized=False'
    ), $ascii)

    $p4 = Join-Path $ArtifactDirectory '04-preexecution-review.txt'
    [IO.File]::WriteAllLines($p4, @(
        'status=PASS-PREEXECUTION-REVIEW',
        'next-activity=DIAGNOSTIC-ONLY',
        'family-b-locked=True',
        'historical-envelope-retained-for-future-equilibrium-qualification=True',
        'historical-envelope-valid-as-family-b-discriminator=False',
        'no-retuning=True',
        'r3-remains-red=True',
        'r4-remains-blocked=True'
    ), $ascii)
}
