param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10 final long contract validation failed: $Message" }
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

$contractPath = Join-Path $RepositoryRoot 'eng\m10-final-long-validation-contract.json'
$cumulativeRecordPath = Join-Path $RepositoryRoot 'eng\m10-final-cumulative-validation-record.json'
$matrixPath = Join-Path $RepositoryRoot 'eng\m10-final-vv-matrix.json'
$testPath = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\M10FinalLongValidationTests.cs'
$scriptPath = Join-Path $RepositoryRoot 'scripts\run-m10-final-long-validation.cmd'
$sourceManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-long-baseline-src.sha256'
$testManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-long-baseline-tests.sha256'

Require (Test-Path -LiteralPath $contractPath -PathType Leaf) 'long validation contract is missing.'
Require (Test-Path -LiteralPath $cumulativeRecordPath -PathType Leaf) 'validated cumulative prerequisite record is missing.'
Require (Test-Path -LiteralPath $matrixPath -PathType Leaf) 'final V&V matrix is missing.'
Require (Test-Path -LiteralPath $testPath -PathType Leaf) 'explicit long validation test class is missing.'
Require (Test-Path -LiteralPath $scriptPath -PathType Leaf) 'run-m10-final-long-validation.cmd is missing.'
Require (Test-Path -LiteralPath $sourceManifestPath -PathType Leaf) 'frozen src manifest is missing.'
Require (Test-Path -LiteralPath $testManifestPath -PathType Leaf) 'frozen tests manifest is missing.'

$c = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
Require ($c.schema -eq 'm10-final-long-validation-contract-v2') 'contract schema mismatch.'
Require ($c.status -eq 'FROZEN-AWAITING-EXECUTION') 'contract must be frozen before acceptance execution.'
Require ($c.prerequisite_marker -eq 'm10-final-cumulative-validation-passes=True') 'cumulative prerequisite marker changed.'
Require ($c.fixed_step_ms -eq 10) 'fixed step changed.'
Require ($c.total_simulated_seconds -eq 14400) 'total simulated seconds must remain 14400.'
Require ($c.total_logical_steps -eq 1440000) 'total logical steps must remain 1440000.'
Require (@($c.legs).Count -eq 5) 'exactly five long validation legs are required.'
foreach ($name in @(
    'unhandled_exceptions','nonfinite_observations','unsupported_water_steam_envelope_excursions',
    'fingerprint_mismatches','fallback_commit_violations','unsafe_corrected_commits',
    'untargeted_branch_disagreements','healthy_unexpected_trips','unexpected_fault_activations',
    'exact_version_identity_drift','duplicate_replay_timeline_rows')) {
    Require ($c.global_blocking_criteria.$name -eq 0) "global blocker '$name' must remain zero."
}

$expectedLegs = @(
    @('LR-H1',7200,720000),
    @('LR-M1',4400,440000),
    @('LR-D1',1800,180000),
    @('LR-P1',900,90000),
    @('LR-R1',100,10000)
)
foreach ($expected in $expectedLegs) {
    $leg = @($c.legs | Where-Object { $_.id -eq $expected[0] })
    Require ($leg.Count -eq 1) "missing or duplicated leg $($expected[0])."
    Require ($leg[0].seconds -eq $expected[1]) "simulated seconds changed for $($expected[0])."
    Require ($leg[0].steps -eq $expected[2]) "logical steps changed for $($expected[0])."
}
$h1 = @($c.legs | Where-Object { $_.id -eq 'LR-H1' })[0]
Require ($h1.initial_condition -eq 'integrated-operations-desktop-stable@4') 'LR-H1 exact-v4 identity changed.'
Require ($h1.hydraulic_mode -eq 'FourNodeBranchContinuityCorrectedCommitOptIn') 'LR-H1 hydraulic mode changed.'
Require ($h1.thermodynamic_closure -eq 'CorrelationConsistentInverseDomain') 'LR-H1 thermodynamic closure changed.'
$m1 = @($c.legs | Where-Object { $_.id -eq 'LR-M1' })[0]
Require ($m1.pack_exact_id -eq 'bounded-demand-following-5-10-5@2') 'LR-M1 production mission exact id changed.'
$d1 = @($c.legs | Where-Object { $_.id -eq 'LR-D1' })[0]
Require ($d1.fault_type -eq 'instrumentation.sensor-unavailable') 'LR-D1 fault type changed.'
Require ($d1.target -eq 'power') 'LR-D1 fault target changed.'
Require ($d1.activation_step -eq 54000 -and $d1.clear_step -eq 90000) 'LR-D1 fault timing changed.'
$p1 = @($c.legs | Where-Object { $_.id -eq 'LR-P1' })[0]
Require ($p1.scram_commit_step -eq 54000) 'LR-P1 SCRAM step changed.'
Require ($p1.authority_observation_step -eq 54001) 'LR-P1 authority observation boundary changed.'
Require ($p1.blocked_normal_command_step -eq 60000) 'LR-P1 blocked command step changed.'
Require ($p1.manual_takeover_step -eq 72000) 'LR-P1 manual takeover step changed.'
$r1 = @($c.legs | Where-Object { $_.id -eq 'LR-R1' })[0]
Require ($r1.pack_exact_id -eq 'bounded-demand-following-5-10-5@2') 'LR-R1 mission exact id changed.'
Require ($r1.load_raise_step -eq 500 -and $r1.load_lower_step -eq 3000) 'LR-R1 load action timing changed.'
Require ($r1.checkpoint_step -eq 5000 -and $r1.rod_hold_step -eq 6000) 'LR-R1 checkpoint/rod timing changed.'
Require ($c.frozen_i3_budgets.count -eq 19) 'frozen I.3 budget count changed.'
Require ($c.frozen_i3_budgets.rolling_window_seconds -eq 60) 'healthy rolling budget window changed.'
Require (@($c.frozen_i3_budgets.window_end_seconds).Count -eq 24) 'healthy budget sentinel schedule must contain 24 windows.'
Require ((@($c.frozen_i3_budgets.window_end_seconds) -join ',') -eq ((1..24 | ForEach-Object { $_ * 300 }) -join ',')) 'healthy budget sentinel schedule changed.'
Require (-not $c.frozen_i3_budgets.retuning_allowed) 'frozen I.3 budgets may not be retuned.'
Require ($c.instantaneous_conservation_ceilings.mass_closure_residual_kg -eq 0.000001) 'mass closure ceiling changed.'
Require ($c.instantaneous_conservation_ceilings.energy_closure_residual_J -eq 0.01) 'energy closure ceiling changed.'
Require ($c.instantaneous_conservation_ceilings.balance_mass_rate_residual_kg_s -eq 0.00000001) 'mass-rate closure ceiling changed.'
Require ($c.instantaneous_conservation_ceilings.balance_power_residual_W -eq 0.001) 'power closure ceiling changed.'
Require ($c.evidence_growth.lifecycle_spine_cap -eq 32) 'lifecycle spine cap changed.'
Require ($c.evidence_growth.recent_operational_evidence_cap -eq 100) 'recent operational cap changed.'
Require ($c.evidence_growth.full_to_half_size_ratio_ceiling -eq 2.25) 'archive growth sentinel changed.'
Require (@($c.required_artifacts).Count -eq 12) 'required artifact set must contain 12 files.'

$r = Get-Content -LiteralPath $cumulativeRecordPath -Raw | ConvertFrom-Json
Require ($r.status -eq 'VALIDATED') 'cumulative prerequisite is not recorded as VALIDATED.'
Require ($r.marker -eq 'm10-final-cumulative-validation-passes=True') 'cumulative pass marker missing.'
Require ($r.m10_closure_pending_long -eq $true) 'cumulative record must leave M10 pending only the long gate.'

$m = Get-Content -LiteralPath $matrixPath -Raw | ConvertFrom-Json
Require ($m.status -eq 'FROZEN-PRE-LONG') 'V&V matrix must remain frozen pre-long during execution.'
$long = @($m.rows | Where-Object { $_.id -eq 'LONG-SOAK-01' })
Require ($long.Count -eq 1) 'LONG-SOAK-01 missing or duplicated.'
Require ($long[0].closure_status -eq 'PENDING-LONG-GATE') 'LONG-SOAK-01 must be pending before execution.'
Require (@($m.authoritative_exact_v4_reference.frozen_i3_budgets).Count -eq 19) 'matrix frozen I.3 budget count changed.'

$testText = Get-Content -LiteralPath $testPath -Raw
foreach ($method in @(
    'LR_H1_HealthyExactV4_LongSoakPreservesConservationBudgetsAndNumericalSafety',
    'LR_M1_ProductionMissionV2_LongContinuationPreservesDemandEvidenceAndPlantHealth',
    'LR_D1_DegradedMeasurement_LongRecoveryRemainsFailClosedAndDeterministic',
    'LR_P1_ProtectionAndTakeover_LongObservationPreservesProtectionPrecedence',
    'LR_R1_ReplayCheckpoint_LongSentinelRemainsExactlyEquivalent')) {
    Require ($testText.Contains($method)) "long validation method '$method' is missing."
}
Require ($testText.Contains('[Fact(Explicit = true)]')) 'long test surface must remain explicit.'
Require ($testText.Contains('NRS_M10_FINAL_LONG_VALIDATION')) 'long test opt-in variable is missing.'
Require ($testText.Contains('const int totalSteps = 720_000;')) 'LR-H1 authored step count drifted from contract.'
Require ($testText.Contains('const int totalSteps = 440_000;')) 'LR-M1 authored step count drifted from contract.'
Require ($testText.Contains('const int activationStep = 54_000;')) 'LR-D1 activation step drifted from contract.'
Require ($testText.Contains('const int clearStep = 90_000;')) 'LR-D1 clear step drifted from contract.'
Require ($testText.Contains('const int scramStep = 54_000;')) 'LR-P1 SCRAM step drifted from contract.'
Require ($testText.Contains('const int authorityObservationStep = 54_001;')) 'LR-P1 authority observation step drifted from contract.'
Require ($testText.Contains('const int blockedCommandStep = 60_000;')) 'LR-P1 blocked command step drifted from contract.'
Require ($testText.Contains('const int manualTakeoverStep = 72_000;')) 'LR-P1 manual takeover step drifted from contract.'
Require ($testText.Contains('const int checkpointStep = 5_000;')) 'LR-R1 checkpoint step drifted from contract.'
Require ($testText.Contains('const double MaximumFullToHalfArchiveSizeRatio = 2.25d;')) 'LR-R1 archive growth ceiling drifted from contract.'


$scriptText = Get-Content -LiteralPath $scriptPath -Raw
Require ($scriptText.Contains('--explicit only')) 'long script must execute explicit tests explicitly.'
Require ($scriptText.Contains('NRS_M10_FINAL_LONG_VALIDATION=1')) 'long script must set the explicit opt-in variable.'
Require ($scriptText.Contains('finalize-m10-final-long-validation.ps1')) 'long script must run the artifact finalizer.'
Require (-not $scriptText.Contains('run-m10-final-validation.cmd')) 'long script must remain separate from the already validated cumulative gate.'

$manifestLines = @(Get-Content -LiteralPath $sourceManifestPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
Require ($manifestLines.Count -eq 950) 'frozen src manifest must contain exactly 950 files from the validated cumulative baseline.'
$expected = @{}
foreach ($line in $manifestLines) {
    if ($line -notmatch '^([0-9a-f]{64}) \*(.+)$') { Fail "invalid src manifest row: $line" }
    $expected[$matches[2].Replace('/','\')] = $matches[1]
}
$actualFiles = @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
Require ($actualFiles.Count -eq $expected.Count) 'production src file count differs from the validated cumulative baseline after excluding generated bin/obj build output.'
foreach ($file in $actualFiles) {
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\','/')
    Require ($expected.ContainsKey($relative)) "unexpected production src file: $relative"
    Require ((Get-Sha256Hex $file.FullName) -eq $expected[$relative]) "production src file changed: $relative"
}


$testManifestLines = @(Get-Content -LiteralPath $testManifestPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
Require ($testManifestLines.Count -eq 336) 'frozen tests manifest must contain exactly 336 files from the validated cumulative baseline.'
$expectedTests = @{}
foreach ($line in $testManifestLines) {
    if ($line -notmatch '^([0-9a-f]{64}) \*(.+)$') { Fail "invalid tests manifest row: $line" }
    $expectedTests[$matches[2].Replace('/','\')] = $matches[1]
}
$allowedNewTest = 'tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\M10FinalLongValidationTests.cs'
$actualTestFiles = @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'tests') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
Require ($actualTestFiles.Count -eq ($expectedTests.Count + 1)) 'test surface must equal validated cumulative baseline plus exactly one long-validation test file after excluding generated bin/obj build output.'
foreach ($file in $actualTestFiles) {
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\','/')
    if ($relative -eq $allowedNewTest) { continue }
    Require ($expectedTests.ContainsKey($relative)) "unexpected test file: $relative"
    Require ((Get-Sha256Hex $file.FullName) -eq $expectedTests[$relative]) "validated cumulative test file changed: $relative"
}
Require (Test-Path -LiteralPath (Join-Path $RepositoryRoot $allowedNewTest) -PathType Leaf) 'long-validation test file is missing.'

Write-Host 'm10-final-long-contract-passes=True'
Write-Host 'm10-final-cumulative-prerequisite-recorded=True'
Write-Host 'm10-final-long-production-src-unchanged=True'
Write-Host 'm10-final-long-baseline-tests-unchanged=True'
Write-Host 'm10-final-long-test-surface-addition-count=1'
Write-Host 'm10-final-long-workload-frozen=True'
