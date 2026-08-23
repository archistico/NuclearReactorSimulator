param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$report = Join-Path $RepositoryRoot 'artifacts\m10-final-replacement-long-baseline-freeze'
$contractPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-validation-contract.json'
$activationPath = Join-Path $RepositoryRoot 'eng\m10-final-v9-production-activation-decision-record.json'
$srcManifest = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-v9-baseline-src.sha256'
$testsManifest = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-v9-baseline-tests.sha256'
$summaryPath = Join-Path $report '01-replacement-long-baseline-freeze.summary.txt'
$manifestSummaryPath = Join-Path $report '03-manifest-summary.txt'
$timingPath = Join-Path $report '04-workstation-timing-plan.txt'

New-Item -ItemType Directory -Force -Path $report | Out-Null
$c = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$a = Get-Content -LiteralPath $activationPath -Raw | ConvertFrom-Json
$srcCount = @(Get-Content -LiteralPath $srcManifest | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
$testCount = @(Get-Content -LiteralPath $testsManifest | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count

$lines = @(
    '=== M10 Final exact-v9 replacement-long baseline freeze ===',
    'scope=freeze authoritative exact-v9 production source/tests manifests and redesigned replacement-long workload after validated production activation; no long leg executed by this gate;',
    "authoritative-default=$($a.authoritativeDefault); production-mission=$($a.productionMissionPack); activation-fingerprint=$($a.fingerprint);",
    "replacement-authored-seconds=$($c.total_simulated_seconds); replacement-authored-steps=$($c.total_logical_steps); legs=$(@($c.legs).Count);",
    "target-workstation-minutes=$($c.wall_clock_policy.target_workstation_minutes_min)-$($c.wall_clock_policy.target_workstation_minutes_max); hard-wall-cap-minutes=$($c.wall_clock_policy.hard_campaign_cap_minutes); projected-authored-wall-minutes=$($c.wall_clock_policy.linear_projection_minutes_for_authored_workload);",
    "replacement-src-manifest-files=$srcCount; replacement-tests-manifest-files=$testCount; old-exact-v4-manifest-reuse=False;",
    'm10-final-v9-authoritative-prerequisite-recorded=True;',
    'm10-final-replacement-long-src-manifest-frozen=True;',
    'm10-final-replacement-long-tests-manifest-frozen=True;',
    'm10-final-replacement-long-workload-frozen=True;',
    'm10-final-failed-exact-v4-long-manifests-preserved=True;',
    'replacement-long-authorized=True;',
    'replacement-long-executed=False;',
    'm10-closure-eligible=False;',
    'next-step=prepare/run the execution candidate against these exact manifests and contract; any src or pre-existing test change invalidates authorization and requires a new freeze;'
)
[System.IO.File]::WriteAllLines($summaryPath, $lines, (New-Object System.Text.UTF8Encoding($false)))

$manifestLines = @(
    "src-manifest=eng/m10-final-replacement-long-v9-baseline-src.sha256; files=$srcCount;",
    "tests-manifest=eng/m10-final-replacement-long-v9-baseline-tests.sha256; files=$testCount;",
    "execution-candidate-allowed-new-test-files=$($c.replacement_baseline_manifests.execution_candidate_allowed_test_additions);",
    'production-src-changes-after-freeze-allowed=False;',
    "historical-src-manifest=$($c.historical_reference.old_src_manifest); reuse-authorized=$($c.historical_reference.reuse_authorized);",
    "historical-tests-manifest=$($c.historical_reference.old_tests_manifest);"
)
[System.IO.File]::WriteAllLines($manifestSummaryPath, $manifestLines, (New-Object System.Text.UTF8Encoding($false)))

$timingLines = @(
    "calibration-source=$($c.wall_clock_policy.calibration_source);",
    "calibration-simulated-seconds=$($c.wall_clock_policy.calibration_simulated_seconds);",
    "calibration-wall-seconds=$($c.wall_clock_policy.calibration_wall_seconds);",
    "replacement-authored-seconds=$($c.total_simulated_seconds);",
    "linear-projection-minutes=$($c.wall_clock_policy.linear_projection_minutes_for_authored_workload);",
    "target-minutes=$($c.wall_clock_policy.target_workstation_minutes_min)-$($c.wall_clock_policy.target_workstation_minutes_max);",
    "hard-cap-minutes=$($c.wall_clock_policy.hard_campaign_cap_minutes);",
    'hard-cap-semantics=validation job budget only; not a physics tolerance;',
    'replay-extra-physical-steps=not counted in authored simulated seconds; execution harness must enforce the 60-minute wall deadline across the full campaign;'
)
[System.IO.File]::WriteAllLines($timingPath, $timingLines, (New-Object System.Text.UTF8Encoding($false)))
Get-Content -LiteralPath $summaryPath
