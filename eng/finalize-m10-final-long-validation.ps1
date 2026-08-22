param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$report = Join-Path $RepositoryRoot 'artifacts\m10-final-long-validation'
$summaryPath = Join-Path $report '01-m10-final-long-validation.summary.txt'
$required = @(
    '00-progress.txt','02-workload-contract.json','03-leg-summary.csv','04-conservation-maxima.csv',
    '05-healthy-window-i3-budget-comparison.csv','06-numerical-coupling-telemetry.csv',
    '07-trip-fault-protection-classification.csv','08-mission-demand-score-evidence.csv',
    '09-replay-checkpoint-fingerprint-sentinels.csv','10-evidence-growth.csv','11-performance-diagnostics.csv'
)

$missing = @($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $report $_) -PathType Leaf) })
$legRows = @()
if (Test-Path -LiteralPath (Join-Path $report '03-leg-summary.csv')) {
    $legRows = @(Import-Csv -LiteralPath (Join-Path $report '03-leg-summary.csv'))
}
$expectedLegs = @('LR-H1','LR-M1','LR-D1','LR-P1','LR-R1')
$allLegsPresent = ($legRows.Count -eq 5) -and (@($legRows.leg_id | Sort-Object) -join ',') -eq (@($expectedLegs | Sort-Object) -join ',')
$allLegsPass = $allLegsPresent -and (@($legRows | Where-Object { $_.passes -ne 'True' }).Count -eq 0)

$classificationRows = @()
if (Test-Path -LiteralPath (Join-Path $report '07-trip-fault-protection-classification.csv')) {
    $classificationRows = @(Import-Csv -LiteralPath (Join-Path $report '07-trip-fault-protection-classification.csv'))
}
$unhandledExceptions = @($classificationRows | Where-Object { $_.classification -like 'UNHANDLED *' }).Count
$nonFiniteObservations = 0L
$envelopeExcursions = 0L
$unexpectedTripsOrProtection = 0L
$unexpectedFaultActivations = 0L
foreach ($row in $classificationRows) {
    if ($null -ne $row.nonfinite_observations -and $row.nonfinite_observations -ne '') {
        $nonFiniteObservations += [int64]$row.nonfinite_observations
    }
    if ($null -ne $row.envelope_excursions -and $row.envelope_excursions -ne '') {
        $envelopeExcursions += [int64]$row.envelope_excursions
    }
    if ($null -ne $row.unexpected_trip_or_protection_count -and $row.unexpected_trip_or_protection_count -ne '') {
        $unexpectedTripsOrProtection += [int64]$row.unexpected_trip_or_protection_count
    }
    if ($null -ne $row.unexpected_fault_activations -and $row.unexpected_fault_activations -ne '') {
        $unexpectedFaultActivations += [int64]$row.unexpected_fault_activations
    }
}
$classificationPass = ($classificationRows.Count -eq 5) -and
    ($unhandledExceptions -eq 0) -and
    ($nonFiniteObservations -eq 0) -and
    ($envelopeExcursions -eq 0) -and
    ($unexpectedTripsOrProtection -eq 0) -and
    ($unexpectedFaultActivations -eq 0)

$budgetPass = $false
if (Test-Path -LiteralPath (Join-Path $report '05-healthy-window-i3-budget-comparison.csv')) {
    $budgetRows = @(Import-Csv -LiteralPath (Join-Path $report '05-healthy-window-i3-budget-comparison.csv'))
    $budgetPass = ($budgetRows.Count -eq 456) -and (@($budgetRows | Where-Object { $_.passes -ne 'True' }).Count -eq 0)
}
$conservationPass = $false
if (Test-Path -LiteralPath (Join-Path $report '04-conservation-maxima.csv')) {
    $conservation = @(Import-Csv -LiteralPath (Join-Path $report '04-conservation-maxima.csv'))
    $conservationPass = ($conservation.Count -eq 1) -and ($conservation[0].passes -eq 'True')
}
$telemetryPass = $false
if (Test-Path -LiteralPath (Join-Path $report '06-numerical-coupling-telemetry.csv')) {
    $t = @(Import-Csv -LiteralPath (Join-Path $report '06-numerical-coupling-telemetry.csv'))
    if ($t.Count -eq 1) {
        $telemetryPass = ([int64]$t[0].triggered -gt 0) -and
            ([int64]$t[0].triggered -eq [int64]$t[0].eligible) -and
            ([int64]$t[0].triggered -eq [int64]$t[0].authorized) -and
            ([int64]$t[0].triggered -eq [int64]$t[0].committed) -and
            ([int64]$t[0].rollbacks -eq 0) -and ([int64]$t[0].explicit_fallbacks -eq 0) -and
            ([int64]$t[0].fallback_commit_violations -eq 0) -and ([int64]$t[0].unsafe_commits -eq 0) -and
            ([int64]$t[0].untargeted_branch_disagreements -eq 0)
    }
}
$replayPass = $false
if (Test-Path -LiteralPath (Join-Path $report '09-replay-checkpoint-fingerprint-sentinels.csv')) {
    $r = @(Import-Csv -LiteralPath (Join-Path $report '09-replay-checkpoint-fingerprint-sentinels.csv'))
    $replayPass = ($r.Count -eq 1) -and ($r[0].passes -eq 'True') -and
        ($r[0].recording_equivalent -eq 'True') -and
        ($r[0].final_fingerprint -eq $r[0].full_replay_fingerprint) -and
        ($r[0].final_fingerprint -eq $r[0].checkpoint_continuation_fingerprint) -and
        ($r[0].challenge_fingerprint -eq $r[0].full_replay_challenge_fingerprint) -and
        ($r[0].challenge_fingerprint -eq $r[0].checkpoint_continuation_challenge_fingerprint)
}
$growthPass = $false
if (Test-Path -LiteralPath (Join-Path $report '10-evidence-growth.csv')) {
    $g = @(Import-Csv -LiteralPath (Join-Path $report '10-evidence-growth.csv'))
    $growthPass = ($g.Count -eq 2) -and
        (@($g | Where-Object { $_.passes -ne 'True' }).Count -eq 0) -and
        (@($g | Where-Object { $_.leg_id -eq 'LR-M1' }).Count -eq 1) -and
        (@($g | Where-Object { $_.leg_id -eq 'LR-R1' }).Count -eq 1)
}

$missionPass = (@($legRows | Where-Object { $_.leg_id -eq 'LR-M1' -and $_.passes -eq 'True' }).Count -eq 1)
$degradedPass = (@($legRows | Where-Object { $_.leg_id -eq 'LR-D1' -and $_.passes -eq 'True' }).Count -eq 1)
$protectionPass = (@($legRows | Where-Object { $_.leg_id -eq 'LR-P1' -and $_.passes -eq 'True' }).Count -eq 1)
$pass = ($missing.Count -eq 0) -and $allLegsPass -and $classificationPass -and $budgetPass -and $conservationPass -and $telemetryPass -and $replayPass -and $growthPass
$nextStep = if ($pass) { 'M10 closure documentation / M11 planning' } else { 'investigate long validation evidence; do not close M10' }

$lines = @(
    '=== M10 Final Pre-M11 long validation artifact summary ===',
    'scope=M10 Final Pre-M11 Long Validation over validated cumulative Hotfix 1; scheduled-long integral qualification only; production runtime and Simulation physics unchanged;',
    "m10-long-workload-completed=$allLegsPresent; m10-long-simulated-seconds=14400; m10-long-logical-steps=1440000; long-legs=5;",
    "m10-long-unhandled-exceptions=$unhandledExceptions; m10-long-nonfinite-observations=$nonFiniteObservations; m10-long-envelope-excursions=$envelopeExcursions; m10-long-healthy-unexpected-trips=$unexpectedTripsOrProtection; m10-long-unexpected-fault-activations=$unexpectedFaultActivations;",
    "m10-long-conservation-ceilings-pass=$conservationPass; m10-long-healthy-budget-sentinels-pass=$budgetPass; m10-long-numerical-coupling-safety-pass=$telemetryPass;",
    "m10-long-mission-pack-v2-pass=$missionPass; m10-long-degraded-recovery-pass=$degradedPass; m10-long-protection-takeover-pass=$protectionPass;",
    "m10-long-replay-checkpoint-sentinels-pass=$replayPass; m10-long-evidence-growth-bounded=$growthPass; m10-long-classification-blockers-clear=$classificationPass; missing-required-artifacts=$($missing.Count);",
    "m10-final-long-validation-passes=$pass; m10-closure-eligible=$pass; next-step=$nextStep;"
)
[System.IO.File]::WriteAllLines($summaryPath, $lines, (New-Object System.Text.UTF8Encoding($false)))
Get-Content -LiteralPath $summaryPath
if (-not $pass) { exit 1 }
exit 0
