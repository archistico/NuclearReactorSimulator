param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$report = Join-Path $RepositoryRoot 'artifacts\m10-final-replacement-long-validation'
$contractPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-validation-contract.json'
$freezeRecordPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-baseline-freeze-record.json'
$summaryPath = Join-Path $report '01-m10-final-replacement-long-validation.summary.txt'
$required = @(
    '00-progress.txt',
    '02-workload-contract.json',
    '03-leg-summary.csv',
    '04-conservation-maxima.csv',
    '05-healthy-window-v9-operating-point-sentinels.csv',
    '06-numerical-coupling-telemetry.csv',
    '07-trip-fault-protection-classification.csv',
    '08-mission-demand-score-evidence.csv',
    '09-replay-checkpoint-fingerprint-sentinels.csv',
    '10-evidence-growth.csv',
    '11-performance-diagnostics.csv',
    '12-wall-budget-summary.txt'
)

New-Item -ItemType Directory -Force -Path $report | Out-Null
$c = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$f = Get-Content -LiteralPath $freezeRecordPath -Raw | ConvertFrom-Json
$missing = @($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $report $_) -PathType Leaf) })

$legRows = @()
$legPath = Join-Path $report '03-leg-summary.csv'
if (Test-Path -LiteralPath $legPath) { $legRows = @(Import-Csv -LiteralPath $legPath) }
$expectedLegs = @('RL-H1','RL-M1','RL-D1','RL-P1','RL-R1')
$allLegsPresent = ($legRows.Count -eq 5) -and ((@($legRows.leg_id | Sort-Object) -join ',') -eq (@($expectedLegs | Sort-Object) -join ','))
$allLegsPass = $allLegsPresent -and (@($legRows | Where-Object { $_.passes -ne 'True' }).Count -eq 0)
$authoredSteps = 0L
$authoredSeconds = 0L
foreach ($row in $legRows) {
    if ($null -ne $row.logical_steps -and $row.logical_steps -ne '') { $authoredSteps += [int64]$row.logical_steps }
    if ($null -ne $row.simulated_seconds -and $row.simulated_seconds -ne '') { $authoredSeconds += [int64]$row.simulated_seconds }
}
$authoredWorkloadPass = $allLegsPresent -and $authoredSteps -eq 192000 -and $authoredSeconds -eq 1920

$classificationRows = @()
$classificationPath = Join-Path $report '07-trip-fault-protection-classification.csv'
if (Test-Path -LiteralPath $classificationPath) { $classificationRows = @(Import-Csv -LiteralPath $classificationPath) }
$unhandledExceptions = @($classificationRows | Where-Object { $_.classification -like 'UNHANDLED *' }).Count
$nonFiniteObservations = 0L
$envelopeExcursions = 0L
$unexpectedTripsOrProtection = 0L
$unexpectedFaultActivations = 0L
foreach ($row in $classificationRows) {
    if ($row.nonfinite_observations -ne '') { $nonFiniteObservations += [int64]$row.nonfinite_observations }
    if ($row.envelope_excursions -ne '') { $envelopeExcursions += [int64]$row.envelope_excursions }
    if ($row.unexpected_trip_or_protection_count -ne '') { $unexpectedTripsOrProtection += [int64]$row.unexpected_trip_or_protection_count }
    if ($row.unexpected_fault_activations -ne '') { $unexpectedFaultActivations += [int64]$row.unexpected_fault_activations }
}
$classificationPass = ($classificationRows.Count -eq 5) -and
    ($unhandledExceptions -eq 0) -and
    ($nonFiniteObservations -eq 0) -and
    ($envelopeExcursions -eq 0) -and
    ($unexpectedTripsOrProtection -eq 0) -and
    ($unexpectedFaultActivations -eq 0)

$conservationPass = $false
$conservationPath = Join-Path $report '04-conservation-maxima.csv'
if (Test-Path -LiteralPath $conservationPath) {
    $rows = @(Import-Csv -LiteralPath $conservationPath)
    if ($rows.Count -eq 1 -and $rows[0].leg_id -eq 'RL-H1') {
        $conservationPass = ($rows[0].passes -eq 'True') -and
            ([double]$rows[0].max_mass_closure_kg -le [double]$c.instantaneous_conservation_ceilings.mass_closure_residual_kg) -and
            ([double]$rows[0].max_energy_closure_j -le [double]$c.instantaneous_conservation_ceilings.energy_closure_residual_J) -and
            ([double]$rows[0].max_balance_mass_rate_kg_s -le [double]$c.instantaneous_conservation_ceilings.balance_mass_rate_residual_kg_s) -and
            ([double]$rows[0].max_balance_power_w -le [double]$c.instantaneous_conservation_ceilings.balance_power_residual_W)
    }
}

$sentinelPass = $false
$sentinelPath = Join-Path $report '05-healthy-window-v9-operating-point-sentinels.csv'
if (Test-Path -LiteralPath $sentinelPath) {
    $rows = @(Import-Csv -LiteralPath $sentinelPath)
    $expectedEnds = @(300,600,900)
    $sentinelPass = ($rows.Count -eq 3) -and
        ((@($rows.window_end_seconds | ForEach-Object { [int]$_ } | Sort-Object) -join ',') -eq ($expectedEnds -join ',')) -and
        (@($rows | Where-Object { $_.passes -ne 'True' }).Count -eq 0) -and
        (@($rows | Where-Object { [double]$_.max_abs_node_mass_slope_kg_s -gt [double]$c.exact_v9_operating_point_sentinels.maximum_absolute_node_mass_slope_kg_s }).Count -eq 0) -and
        (@($rows | Where-Object { [double]$_.max_abs_net_external_power_mw -gt [double]$c.exact_v9_operating_point_sentinels.maximum_absolute_late_net_external_power_mw }).Count -eq 0)
}

$telemetryPass = $false
$telemetryPath = Join-Path $report '06-numerical-coupling-telemetry.csv'
if (Test-Path -LiteralPath $telemetryPath) {
    $t = @(Import-Csv -LiteralPath $telemetryPath)
    if ($t.Count -eq 1) {
        $telemetryPass = ([int64]$t[0].observed_steps -eq 90000) -and
            ([int64]$t[0].four_node_steps -eq 90000) -and
            ([int64]$t[0].rollbacks -eq 0) -and
            ([int64]$t[0].explicit_fallbacks -eq 0) -and
            ([int64]$t[0].fallback_commit_violations -eq 0) -and
            ([int64]$t[0].unsafe_commits -eq 0) -and
            ([int64]$t[0].untargeted_branch_disagreements -eq 0)
    }
}

$missionPass = (@($legRows | Where-Object { $_.leg_id -eq 'RL-M1' -and $_.passes -eq 'True' }).Count -eq 1)
$missionEvidencePass = $false
$missionPath = Join-Path $report '08-mission-demand-score-evidence.csv'
if (Test-Path -LiteralPath $missionPath) {
    $missionRows = @(Import-Csv -LiteralPath $missionPath)
    $missionEvidencePass = ($missionRows.Count -eq 480) -and
        (@($missionRows | Where-Object { $_.lifecycle -eq 'Failed' }).Count -eq 0) -and
        (@($missionRows | Where-Object { [int]$_.lifecycle_spine_count -gt 32 }).Count -eq 0) -and
        (@($missionRows | Where-Object { [int]$_.recent_operational_count -gt 100 }).Count -eq 0)
}

$replayPass = $false
$replayPath = Join-Path $report '09-replay-checkpoint-fingerprint-sentinels.csv'
if (Test-Path -LiteralPath $replayPath) {
    $r = @(Import-Csv -LiteralPath $replayPath)
    $replayPass = ($r.Count -eq 1) -and ($r[0].passes -eq 'True') -and ($r[0].recording_equivalent -eq 'True') -and
        ($r[0].final_fingerprint -eq $r[0].full_replay_fingerprint) -and
        ($r[0].final_fingerprint -eq $r[0].checkpoint_continuation_fingerprint) -and
        ($r[0].challenge_fingerprint -eq $r[0].full_replay_challenge_fingerprint) -and
        ($r[0].challenge_fingerprint -eq $r[0].checkpoint_continuation_challenge_fingerprint)
}

$growthPass = $false
$growthPath = Join-Path $report '10-evidence-growth.csv'
if (Test-Path -LiteralPath $growthPath) {
    $g = @(Import-Csv -LiteralPath $growthPath)
    $missionGrowth = @($g | Where-Object { $_.leg_id -eq 'RL-M1' })
    $replayGrowth = @($g | Where-Object { $_.leg_id -eq 'RL-R1' })
    $growthPass = ($g.Count -eq 2) -and ($missionGrowth.Count -eq 1) -and ($replayGrowth.Count -eq 1) -and
        (@($g | Where-Object { $_.passes -ne 'True' }).Count -eq 0) -and
        ([int]$missionGrowth[0].lifecycle_spine_max -le [int]$c.evidence_growth.lifecycle_spine_cap) -and
        ([int]$missionGrowth[0].recent_operational_max -le [int]$c.evidence_growth.recent_operational_evidence_cap) -and
        ([int]$missionGrowth[0].duplicate_timeline_rows -eq 0) -and
        ([double]$replayGrowth[0].full_to_half_ratio -le [double]$c.evidence_growth.full_to_half_size_ratio_ceiling)
}

$performancePass = $false
$wallPass = $false
$targetWallPass = $false
$campaignWallSeconds = [double]::PositiveInfinity
$missionScalingRatio = [double]::PositiveInfinity
$performancePath = Join-Path $report '11-performance-diagnostics.csv'
if (Test-Path -LiteralPath $performancePath) {
    $p = @(Import-Csv -LiteralPath $performancePath)
    $missionWindows = @($p | Where-Object { $_.scope -like 'RL-M1-W*' } | Sort-Object { [int]($_.scope.Substring(7)) })
    $campaignRows = @($p | Where-Object { $_.scope -eq 'CAMPAIGN' })
    $legPerformance = @($p | Where-Object { $_.scope -in @('RL-H1','RL-M1','RL-D1','RL-P1','RL-R1') })
    if ($missionWindows.Count -eq 8 -and $campaignRows.Count -eq 1 -and $legPerformance.Count -eq 5) {
        $early = [double]$missionWindows[0].wall_seconds
        $late = [double]$missionWindows[7].wall_seconds
        if ($early -gt 0) { $missionScalingRatio = $late / $early }
        $campaignWallSeconds = [double]$campaignRows[0].wall_seconds
        $wallPass = ($campaignRows[0].passes -eq 'True') -and ($campaignWallSeconds -le ([double]$c.wall_clock_policy.hard_campaign_cap_minutes * 60.0))
        $targetWallPass = ($campaignWallSeconds -ge ([double]$c.wall_clock_policy.target_workstation_minutes_min * 60.0)) -and
            ($campaignWallSeconds -le ([double]$c.wall_clock_policy.target_workstation_minutes_max * 60.0))
        $performancePass = (@($p | Where-Object { $_.passes -ne 'True' }).Count -eq 0) -and
            ($missionScalingRatio -le [double]$c.mission_scalability_sentinel.late_to_early_wall_ratio_ceiling) -and $wallPass
    }
}

$degradedPass = (@($legRows | Where-Object { $_.leg_id -eq 'RL-D1' -and $_.passes -eq 'True' }).Count -eq 1)
$protectionPass = (@($legRows | Where-Object { $_.leg_id -eq 'RL-P1' -and $_.passes -eq 'True' }).Count -eq 1)
$freezeAuthorizationPass = ($f.status -eq 'VALIDATED') -and ($f.replacementLongAuthorized -eq $true)

$pass = ($missing.Count -eq 0) -and $freezeAuthorizationPass -and $allLegsPass -and $authoredWorkloadPass -and
    $classificationPass -and $conservationPass -and $sentinelPass -and $telemetryPass -and $missionPass -and
    $missionEvidencePass -and $degradedPass -and $protectionPass -and $replayPass -and $growthPass -and $performancePass
$nextStep = if ($pass) { 'explicit M10 closure documentation/promotion; do not start M11 before closure is recorded' } else { 'investigate replacement-long evidence; M10 remains open' }

$lines = @(
    '=== M10 Final exact-v9 replacement-long validation artifact summary ===',
    'scope=authorized replacement long over the frozen exact-v9 authoritative production baseline; one new explicit test only; no frozen src or pre-existing test change;',
    "freeze-authorization-valid=$freezeAuthorizationPass; authoritative-default=$($f.authoritativeDefault); production-mission=$($f.productionMissionPack); activation-fingerprint=$($f.activationFingerprint);",
    "replacement-long-workload-completed=$allLegsPresent; replacement-long-authored-seconds=$authoredSeconds; replacement-long-authored-steps=$authoredSteps; legs=$($legRows.Count);",
    "replacement-long-unhandled-exceptions=$unhandledExceptions; replacement-long-nonfinite-observations=$nonFiniteObservations; replacement-long-envelope-excursions=$envelopeExcursions; replacement-long-unexpected-trips-or-protection=$unexpectedTripsOrProtection; replacement-long-unexpected-fault-activations=$unexpectedFaultActivations;",
    "replacement-long-conservation-pass=$conservationPass; exact-v9-operating-point-sentinels-pass=$sentinelPass; numerical-coupling-safety-pass=$telemetryPass; mission-v3-pass=$missionPass; mission-evidence-pass=$missionEvidencePass; mission-late-to-early-wall-ratio=$missionScalingRatio;",
    "degraded-recovery-pass=$degradedPass; protection-takeover-pass=$protectionPass; replay-checkpoint-pass=$replayPass; evidence-growth-bounded=$growthPass; classification-blockers-clear=$classificationPass;",
    "campaign-wall-seconds=$campaignWallSeconds; hard-wall-cap-pass=$wallPass; target-35-45-minute-band-observed=$targetWallPass; target-band-is-diagnostic-only=True; missing-required-artifacts=$($missing.Count);",
    "replacement-long-validation-passes=$pass; replacement-long-executed=$allLegsPresent; m10-closure-eligible=$pass; next-step=$nextStep;"
)
[System.IO.File]::WriteAllLines($summaryPath, $lines, (New-Object System.Text.UTF8Encoding($false)))
Get-Content -LiteralPath $summaryPath
if (-not $pass) { exit 1 }
exit 0
