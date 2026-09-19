param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [string]$ArtifactDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Problems = New-Object System.Collections.Generic.List[string]

function Add-Problem([string]$Message) {
    $script:Problems.Add($Message)
}

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([System.BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '')
        }
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

function Get-FrozenTree([string]$RelativeRoot) {
    $base = Join-Path $Root $RelativeRoot
    [string[]]$relativePaths = @(
        Get-ChildItem -LiteralPath $base -Recurse -File | ForEach-Object {
            $_.FullName.Substring($Root.Length).TrimStart('\','/').Replace('\','/')
        } | Where-Object {
            $_ -notmatch '(^|/)(bin|obj)(/|$)'
        }
    )

    # Tree hashes are repository contracts. Use explicit ordinal ordering so
    # Windows PowerShell culture/case rules cannot change the same tree hash.
    [System.Array]::Sort($relativePaths, [System.StringComparer]::Ordinal)

    $memory = New-Object System.IO.MemoryStream
    try {
        $utf8 = New-Object System.Text.UTF8Encoding($false)
        foreach ($rel in $relativePaths) {
            $relBytes = $utf8.GetBytes($rel)
            $memory.Write($relBytes, 0, $relBytes.Length)
            $memory.WriteByte(0)
            $fileHash = Get-Sha256 (Join-Path $Root $rel)
            $hashBytes = Hex-ToBytes $fileHash
            $memory.Write($hashBytes, 0, $hashBytes.Length)
            $memory.WriteByte(10)
        }
        $memory.Position = 0
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $treeHash = ([System.BitConverter]::ToString($sha.ComputeHash($memory))).Replace('-', '')
        }
        finally { $sha.Dispose() }
    }
    finally { $memory.Dispose() }

    return [pscustomobject]@{ Count = $relativePaths.Count; Sha256 = $treeHash }
}

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { Add-Problem $Message }
}

function Require-FileHash([string]$RelativePath, [string]$Expected) {
    $actual = Get-Sha256 (Join-Path $Root $RelativePath)
    if ($null -eq $actual) {
        Add-Problem ("missing file: {0}" -f $RelativePath)
    }
    elseif (-not [string]::Equals($actual, $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        Add-Problem ("hash drift: {0}" -f $RelativePath)
    }
}

function Require-ExactMarker([string]$RelativePath, [string]$Marker) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Problem ("marker file missing: {0}" -f $RelativePath)
        return
    }
    $lines = @(Get-Content -LiteralPath $path)
    $count = @($lines | Where-Object { $_ -ceq $Marker }).Count
    if ($count -ne 1) {
        Add-Problem ("marker cardinality {0} in {1}: {2}" -f $Marker, $RelativePath, $count)
    }
}

$contractPath = Join-Path $Root 'eng/m10-final-vr2-r3-energy-transport-ownership-repair-planning1-contract.json'
if (-not (Test-Path -LiteralPath $contractPath -PathType Leaf)) {
    throw 'Planning contract is missing.'
}
$Contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json

Require ($Contract.schema -eq 'm10-final-vr2-r3-energy-transport-ownership-repair-planning1-v1') 'contract schema drift'
Require ($Contract.status -eq 'CANDIDATE-PLANNING-ONLY-FAMILY-B-SELECTED') 'contract status drift'
Require ($Contract.engineering_classification -eq 'CAUSAL-CLOSURE-CONFIRMED') 'causal classification drift'
Require ($Contract.localized_seam -eq 'STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT') 'localized seam drift'
Require ($Contract.process_authority.policy -eq 'LOCAL-QUALIFICATION-AUTHORITATIVE-HOSTED-ADVISORY') 'local authority policy drift'
Require ([bool]$Contract.process_authority.supersedes_prior_hosted_green_prerequisite) 'hosted hold not superseded in current planning contract'
Require (-not [bool]$Contract.process_authority.physics_thresholds_weakened) 'planning must not weaken physics thresholds'

foreach ($name in @('diagnostic3_rev1_returned_adjudication','diagnostic3_summary','diagnostic3_pre_repair_review','local_contract_v2_status','local_contract_v2_contract')) {
    $entry = $Contract.prerequisites.$name
    Require-FileHash ([string]$entry.path) ([string]$entry.sha256)
}

$statusPath = Join-Path $Root ([string]$Contract.prerequisites.local_contract_v2_status.path)
if (Test-Path -LiteralPath $statusPath -PathType Leaf) {
    $statusLines = @(Get-Content -LiteralPath $statusPath)
    Require (@($statusLines | Where-Object { $_ -ceq 'status=PASS' }).Count -eq 1) 'local Contract V2 qualification is not PASS'
    Require (@($statusLines | Where-Object { $_ -ceq 'build=PASS' }).Count -eq 1) 'local Contract V2 build is not PASS'
    Require (@($statusLines | Where-Object { $_ -ceq 'exact-v9-authoritative-v2=PASS' }).Count -eq 1) 'local Contract V2 exact-v9 audit is not PASS'
}

$srcTree = Get-FrozenTree 'src'
$testTree = Get-FrozenTree 'tests'
Require ($srcTree.Count -eq [int]$Contract.baseline.src.file_count) ("src count drift: {0}" -f $srcTree.Count)
Require ($srcTree.Sha256 -eq [string]$Contract.baseline.src.tree_sha256) 'src tree drift'
Require ($testTree.Count -eq [int]$Contract.baseline.tests.file_count) ("tests count drift: {0}" -f $testTree.Count)
Require ($testTree.Sha256 -eq [string]$Contract.baseline.tests.tree_sha256) 'tests tree drift'

foreach ($entry in @($Contract.planned_production_surface)) {
    $path = [string]$entry.path
    if ([string]$entry.change -eq 'ADD') {
        Require (-not (Test-Path -LiteralPath (Join-Path $Root $path))) ("planning candidate already contains planned production add: {0}" -f $path)
    }
    else {
        Require-FileHash $path ([string]$entry.pre_sha256)
    }
}

Require-FileHash 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/IWaterSteamSaturationPropertyProvider.cs' ([string]$Contract.frozen_non_target_source.iwatersteam_saturation_provider_sha256)
Require-FileHash 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamSaturationProperties.cs' ([string]$Contract.frozen_non_target_source.watersteam_saturation_properties_sha256)

Require ($Contract.families.A.disposition -eq 'NOT-SELECTED-INSUFFICIENT-AS-CURRENTLY-EXPOSED') 'Family A disposition drift'
Require ($Contract.families.B.disposition -eq 'SELECTED') 'Family B is not selected'
Require ($Contract.families.C.disposition -eq 'NOT-SELECTED-CONTINGENCY-ONLY') 'Family C disposition drift'
Require ($Contract.selection.repair_owner -eq 'ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT') 'repair owner drift'
Require ($Contract.selection.selected_family -eq 'B') 'selected family drift'
Require ($Contract.selection.mode2_lookup_topology -eq 'PRESSURE-KEYED-DENSE-SATURATION-LINEAR-INTERPOLATION') 'mode2 lookup topology drift'
Require (-not [bool]$Contract.selection.production_if97_dependency_allowed) 'production IF97 dependency must remain forbidden'
Require (-not [bool]$Contract.selection.c4_payload_change_allowed) 'C4 payload mutation must remain forbidden'
Require (-not [bool]$Contract.selection.raw_seed_retuning_allowed) 'raw seed retuning must remain forbidden'
Require (-not [bool]$Contract.selection.threshold_change_allowed) 'threshold changes must remain forbidden'
Require (-not [bool]$Contract.selection.canonical_exact_v9_reinterpretation_allowed) 'canonical exact-v9 reinterpretation must remain forbidden'

Require ([double]$Contract.implementation_acceptance.transport.mode2_vs_if97_abs_delta_j_kg_max -eq 0.001) 'transport IF97 guard drift'
Require ([double]$Contract.implementation_acceptance.transport.mode2_vs_raw_suction_abs_delta_j_kg_max -eq 0.001) 'transport suction guard drift'
Require ([int]$Contract.implementation_acceptance.transport.steady_state_lookup_allocated_bytes_per_call_max -eq 0) 'allocation guard drift'
Require ([double]$Contract.implementation_acceptance.seed_step1.corrected_net_energy_abs_w_max -eq 0.10223668223103162) 'seed-step energy budget drift'
Require ([int]$Contract.implementation_acceptance.fast_gate.running_steps -eq 100) 'fast-gate step count drift'
Require ([int]$Contract.implementation_acceptance.fast_gate.max_envelope_violation_steps -eq 0) 'fast-gate violation count drift'
Require ([double]$Contract.implementation_acceptance.fast_gate.electrical_export_mwe[0] -eq 4.99 -and [double]$Contract.implementation_acceptance.fast_gate.electrical_export_mwe[1] -eq 5.01) 'electrical envelope drift'
Require ([double]$Contract.implementation_acceptance.fast_gate.primary_pump_mass_flow_kg_s[0] -eq 99.9 -and [double]$Contract.implementation_acceptance.fast_gate.primary_pump_mass_flow_kg_s[1] -eq 100.1) 'primary-flow envelope drift'
Require ([double]$Contract.implementation_acceptance.fast_gate.drum_level_fraction[0] -eq 0.49 -and [double]$Contract.implementation_acceptance.fast_gate.drum_level_fraction[1] -eq 0.51) 'drum-level envelope drift'
Require ([double]$Contract.implementation_acceptance.fast_gate.governor_output_percent[0] -eq 29.27 -and [double]$Contract.implementation_acceptance.fast_gate.governor_output_percent[1] -eq 29.30) 'governor envelope drift'
Require (-not [bool]$Contract.authority.production_repair_authorized_now) 'planning candidate must not authorize production repair immediately'
Require ($Contract.authority.next_authorized_gate_after_planning_pass -eq 'R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1') 'next gate drift'
Require (-not [bool]$Contract.authority.r3_passed) 'R3 must remain RED'
Require (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 must remain blocked'

foreach ($docName in @('planning','preimplementation_review')) {
    $doc = $Contract.documents.$docName
    Require-FileHash ([string]$doc.path) ([string]$doc.sha256)
}
foreach ($prop in $Contract.document_markers.PSObject.Properties) {
    foreach ($marker in @($prop.Value)) {
        Require-ExactMarker ([string]$prop.Name) ([string]$marker)
    }
}

if ($Problems.Count -gt 0) {
    Write-Host 'M10 Final VR2 R3 Energy-Transport Ownership Repair Planning 1 validator: FAIL'
    foreach ($problem in $Problems) { Write-Host (" - {0}" -f $problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}

Write-Host 'M10 Final VR2 R3 Energy-Transport Ownership Repair Planning 1 validator: PASS'
Write-Host 'Family B selected for audit; production remains unchanged and R3 remains RED.'

if ($ArtifactDirectory) {
    New-Item -ItemType Directory -Path $ArtifactDirectory -Force | Out-Null

    @(
        'status=PASS-AS-AUTHORED',
        'planning-id=R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-PLANNING1',
        'causal-classification=CAUSAL-CLOSURE-CONFIRMED',
        'localized-seam=STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT',
        'local-authority=LOCAL-QUALIFICATION-AUTHORITATIVE-HOSTED-ADVISORY',
        'diagnostic3-summary-sha256=' + [string]$Contract.prerequisites.diagnostic3_summary.sha256,
        'local-contract-v2-status-sha256=' + [string]$Contract.prerequisites.local_contract_v2_status.sha256,
        'src-count=' + $srcTree.Count,
        'src-tree-sha256=' + $srcTree.Sha256,
        'tests-count=' + $testTree.Count,
        'tests-tree-sha256=' + $testTree.Sha256,
        'production-src-changed=False'
    ) | Set-Content -LiteralPath (Join-Path $ArtifactDirectory '01-contract-and-provenance.txt') -Encoding ASCII

    @(
        'status=PASS-AS-AUTHORED',
        'family-a=NOT-SELECTED-INSUFFICIENT-AS-CURRENTLY-EXPOSED',
        'family-b=SELECTED',
        'family-c=NOT-SELECTED-CONTINGENCY-ONLY',
        'repair-owner=ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT',
        'composition-root=IntegratedPrimaryCircuitSolver',
        'consumer=SteamDrumSeparationSolver',
        'mode2-lookup=PRESSURE-KEYED-DENSE-SATURATION-LINEAR-INTERPOLATION',
        'runtime-if97=False',
        'c4-payload-change=False',
        'seed-retuning=False',
        'threshold-change=False',
        'canonical-exact-v9-reinterpretation=False'
    ) | Set-Content -LiteralPath (Join-Path $ArtifactDirectory '02-option-matrix-and-selected-seam.txt') -Encoding ASCII

    @(
        'status=PASS-AS-AUTHORED',
        'selected-family=B',
        'selected-repair-owner=ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT',
        'production-repair-authorized-now=False',
        'next-authorized-gate=R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1',
        'implementation-fast-gate-running-steps=100',
        'r3-passed=False',
        'r4-planning-authorized=False'
    ) | Set-Content -LiteralPath (Join-Path $ArtifactDirectory '03-planning-summary.txt') -Encoding ASCII

    @(
        'status=PASS-PREIMPLEMENTATION-REVIEW',
        'implementation-surface-count=5',
        'new-production-path-count=1',
        'historical-modes-must-remain-bit-identical=True',
        'transport-lookup-allocation-max-bytes-per-call=0',
        'first-100-step-envelope-violations-max=0',
        'stop-on-fast-gate-red=True',
        'no-retuning-on-red=True',
        'r3-remains-red=True'
    ) | Set-Content -LiteralPath (Join-Path $ArtifactDirectory '04-preimplementation-review.txt') -Encoding ASCII
}
