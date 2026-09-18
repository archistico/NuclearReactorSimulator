param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10 final replacement-long baseline freeze validation failed: $Message" }
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
function Read-Manifest([string]$Path) {
    $rows = @(Get-Content -LiteralPath $Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $map = @{}
    foreach ($line in $rows) {
        if ($line -notmatch '^([0-9a-f]{64}) \*(.+)$') { Fail "invalid manifest row: $line" }
        $relative = $matches[2].Replace('/','\')
        Require (-not $map.ContainsKey($relative)) "duplicate manifest path: $relative"
        $map[$relative] = $matches[1]
    }
    return $map
}
function Validate-TreeAgainstManifest([string]$TreeName, [string]$ManifestPath, [int]$ExpectedCount) {
    $expected = Read-Manifest $ManifestPath
    Require ($expected.Count -eq $ExpectedCount) "$TreeName manifest file count mismatch."
    $treeRoot = Join-Path $RepositoryRoot $TreeName
    $actual = @(Get-ChildItem -LiteralPath $treeRoot -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
    Require ($actual.Count -eq $expected.Count) "$TreeName file count differs from frozen manifest."
    foreach ($file in $actual) {
        $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\','/')
        Require ($expected.ContainsKey($relative)) "unexpected $TreeName file: $relative"
        Require ((Get-Sha256Hex $file.FullName) -eq $expected[$relative]) "$TreeName file changed after freeze: $relative"
    }
}

$contractPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-validation-contract.json'
$activationRecordPath = Join-Path $RepositoryRoot 'eng\m10-final-v9-production-activation-decision-record.json'
$newSourceManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-v9-baseline-src.sha256'
$newTestManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-v9-baseline-tests.sha256'
$oldSourceManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-long-baseline-src.sha256'
$oldTestManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-long-baseline-tests.sha256'
$selectorPath = Join-Path $RepositoryRoot 'src\NuclearReactorSimulator.Application\Scenarios\Training\DesktopHydraulicProductionPolicy.cs'
$packPath = Join-Path $RepositoryRoot 'src\NuclearReactorSimulator.Application\Scenarios\Challenges\Packs\ProductionOperationalChallengePack.cs'

foreach ($path in @($contractPath,$activationRecordPath,$newSourceManifestPath,$newTestManifestPath,$oldSourceManifestPath,$oldTestManifestPath,$selectorPath,$packPath)) {
    Require (Test-Path -LiteralPath $path -PathType Leaf) "required file missing: $path"
}

$c = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
Require ($c.schema -eq 'm10-final-replacement-long-validation-contract-v1') 'contract schema mismatch.'
Require ($c.status -eq 'FROZEN-AWAITING-BASELINE-FREEZE-GATE') 'contract status must remain frozen before the baseline-freeze gate.'
Require ($c.fixed_step_ms -eq 10) 'fixed step changed.'
Require ($c.authoritative_initial_condition -eq 'integrated-operations-desktop-stable@9') 'authoritative exact-v9 identity changed.'
Require ($c.production_mission_pack -eq 'bounded-demand-following-5-10-5@3') 'production mission pack changed.'
Require ($c.total_simulated_seconds -eq 1920) 'replacement workload total simulated seconds changed.'
Require ($c.total_logical_steps -eq 192000) 'replacement workload total logical steps changed.'
Require (@($c.legs).Count -eq 5) 'exactly five replacement-long legs are required.'

$expectedLegs = @(
    @('RL-H1',900,90000),
    @('RL-M1',480,48000),
    @('RL-D1',300,30000),
    @('RL-P1',180,18000),
    @('RL-R1',60,6000)
)
foreach ($expected in $expectedLegs) {
    $leg = @($c.legs | Where-Object { $_.id -eq $expected[0] })
    Require ($leg.Count -eq 1) "missing or duplicated leg $($expected[0])."
    Require ($leg[0].seconds -eq $expected[1]) "simulated seconds changed for $($expected[0])."
    Require ($leg[0].steps -eq $expected[2]) "logical steps changed for $($expected[0])."
}

Require ($c.wall_clock_policy.target_workstation_minutes_min -eq 35) 'target wall-time minimum changed.'
Require ($c.wall_clock_policy.target_workstation_minutes_max -eq 45) 'target wall-time maximum changed.'
Require ($c.wall_clock_policy.hard_campaign_cap_minutes -eq 60) 'hard wall cap changed.'
Require ($c.wall_clock_policy.hard_cap_is_validation_job_policy_not_physics_tolerance -eq $true) 'wall-cap semantics changed.'
Require ($c.wall_clock_policy.linear_projection_minutes_for_authored_workload -gt 39.9 -and $c.wall_clock_policy.linear_projection_minutes_for_authored_workload -lt 40.1) 'workstation projection changed unexpectedly.'

Require ($c.instantaneous_conservation_ceilings.mass_closure_residual_kg -eq 1e-6) 'mass conservation ceiling changed.'
Require ($c.instantaneous_conservation_ceilings.energy_closure_residual_J -eq 1e-2) 'energy conservation ceiling changed.'
Require ($c.instantaneous_conservation_ceilings.balance_mass_rate_residual_kg_s -eq 1e-8) 'mass-rate closure ceiling changed.'
Require ($c.instantaneous_conservation_ceilings.balance_power_residual_W -eq 1e-3) 'power closure ceiling changed.'
Require ($c.exact_v9_operating_point_sentinels.maximum_absolute_node_mass_slope_kg_s -eq 1e-5) 'node-mass slope sentinel changed.'
Require ($c.exact_v9_operating_point_sentinels.maximum_absolute_late_net_external_power_mw -eq 1e-4) 'late net-power sentinel changed.'
Require ($c.mission_scalability_sentinel.late_to_early_wall_ratio_ceiling -eq 2.0) 'MISSION within-run scaling ratio changed.'
Require ($c.mission_scalability_sentinel.synthetic_equivalence_prerequisite_samples -eq 100000) 'LR-M1 synthetic prerequisite changed.'

foreach ($name in @(
    'unhandled_exceptions','nonfinite_observations','unsupported_water_steam_envelope_excursions',
    'fingerprint_mismatches','fallback_commit_violations','unsafe_corrected_commits',
    'untargeted_branch_disagreements','healthy_unexpected_trips','unexpected_fault_activations',
    'exact_version_identity_drift','duplicate_replay_timeline_rows','wall_deadline_exceeded')) {
    Require ($c.global_blocking_criteria.$name -eq 0) "global blocker '$name' must remain zero."
}

$a = Get-Content -LiteralPath $activationRecordPath -Raw | ConvertFrom-Json
Require ($a.status -eq 'VALIDATED') 'exact-v9 production activation record is not validated.'
Require ($a.productionActivation -eq $true) 'exact-v9 production activation prerequisite is not green.'
Require ($a.authoritativeDefault -eq 'integrated-operations-desktop-stable@9') 'activation record authoritative default changed.'
Require ($a.productionMissionPack -eq 'bounded-demand-following-5-10-5@3') 'activation record mission pack changed.'
Require ($a.selectorEqualsDirectFactory -eq $true) 'activation record selector/factory equivalence is false.'
Require ($a.fingerprint -eq '7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418') 'activation fingerprint changed.'
Require ($a.tripSteps -eq 0 -and $a.rollbacks -eq 0 -and $a.fallbackCommitViolations -eq 0 -and $a.unsafeCommits -eq 0 -and $a.untargetedDisagreements -eq 0) 'activation safety evidence is not clean.'

Require ((Get-Sha256Hex $oldSourceManifestPath) -eq $c.historical_reference.old_src_manifest_file_sha256) 'failed exact-v4 source manifest was rewritten.'
Require ((Get-Sha256Hex $oldTestManifestPath) -eq $c.historical_reference.old_tests_manifest_file_sha256) 'failed exact-v4 tests manifest was rewritten.'
Require ($c.historical_reference.reuse_authorized -eq $false) 'failed exact-v4 manifest reuse must remain forbidden.'

Validate-TreeAgainstManifest 'src' $newSourceManifestPath 959
Validate-TreeAgainstManifest 'tests' $newTestManifestPath 351

$selectorText = Get-Content -LiteralPath $selectorPath -Raw
Require ($selectorText.Contains('AuthoritativeDefaultPolicy')) 'authoritative selector declaration missing.'
Require ($selectorText.Contains('DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate')) 'authoritative selector no longer references exact-v9 policy.'
$packText = Get-Content -LiteralPath $packPath -Raw
Require ($packText.Contains('M10FinalExactV9ProductionScenario')) 'current production challenge pack is not bound to exact-v9 production scenario.'
Require ($packText.Contains('BoundedDemandFollowingV2')) 'historical mission @2 retention seam is missing.'

Write-Host 'm10-final-replacement-long-baseline-freeze-contract-passes=True'
Write-Host 'm10-final-v9-authoritative-prerequisite-recorded=True'
Write-Host 'm10-final-replacement-long-src-manifest-matches=True'
Write-Host 'm10-final-replacement-long-tests-manifest-matches=True'
Write-Host 'm10-final-failed-exact-v4-long-manifests-preserved=True'
Write-Host 'm10-final-replacement-long-workload-frozen=True'
