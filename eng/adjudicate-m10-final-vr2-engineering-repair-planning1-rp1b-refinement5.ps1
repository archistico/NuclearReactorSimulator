$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Read-KeyValue([string]$Path) {
    Require-File $Path
    $map = @{}
    foreach ($line in [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $separatorIndex = $line.IndexOf('=')
        if ($separatorIndex -le 0) { continue }
        $key = $line.Substring(0, $separatorIndex)
        $value = $line.Substring($separatorIndex + 1)
        $map[$key] = $value
    }
    return $map
}

function Parse-Double([string]$Value, [string]$Name) {
    $result = 0.0
    if (-not [double]::TryParse($Value, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$result)) {
        throw ("Invalid invariant double for {0}: {1}" -f $Name, $Value)
    }
    return $result
}

function F([double]$Value) {
    return $Value.ToString('R', [System.Globalization.CultureInfo]::InvariantCulture)
}

function B([bool]$Value) {
    if ($Value) { return 'True' }
    return 'False'
}

function Write-Utf8NoBom([string]$Path, [string[]]$Lines) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($Path, $Lines, $encoding)
}

$scriptBytes = [System.IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
if ($scriptBytes | Where-Object { $_ -gt 127 }) {
    throw 'Refinement 5 adjudicator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

$reportDir = Join-Path (Get-Location) 'artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement5'
if (-not (Test-Path -LiteralPath $reportDir -PathType Container)) {
    throw "Refinement 5 artifact directory not found: $reportDir"
}

$expectedRuns = 5
$expectedBoundaries = 320
$expectedCalls = 20480
$ceiling = 409.30666666666673
$runRows = @()
$boundaryByIndex = @{}

for ($runIndex = 1; $runIndex -le $expectedRuns; $runIndex++) {
    $runName = 'process-{0:D2}' -f $runIndex
    $runDir = Join-Path $reportDir $runName
    $summaryPath = Join-Path $runDir '05-process-summary.txt'
    $boundaryPath = Join-Path $runDir '03-c3-r1-boundary-summary.csv'
    foreach ($name in @('01-process-contract.txt','02-c3-r1-call-timing.csv','03-c3-r1-boundary-summary.csv','04-runtime-context.txt','05-process-summary.txt')) {
        Require-File (Join-Path $runDir $name)
    }

    $summary = Read-KeyValue $summaryPath
    if ($summary['status'] -ne 'PASS-PROCESS-EVIDENCE-COMPLETE') { throw "Process $runIndex status drifted." }
    if ([int]$summary['process-run-index'] -ne $runIndex) { throw "Process run index drifted for $runName." }
    if ([int]$summary['measured-calls'] -ne $expectedCalls) { throw "Measured call count drifted for $runName." }
    $runCeiling = Parse-Double $summary['resolve-max-ceiling-us'] "process $runIndex ceiling"
    if ([Math]::Abs($runCeiling - $ceiling) -gt 1e-12) { throw "Performance ceiling drifted for $runName." }

    $boundaryRows = @(Import-Csv -LiteralPath $boundaryPath)
    if ($boundaryRows.Count -ne $expectedBoundaries) { throw "Boundary row count drifted for $runName." }
    if (@($boundaryRows | Group-Object -Property boundary_index | Where-Object { $_.Count -ne 1 }).Count -ne 0) {
        throw "Boundary identity is not unique in $runName."
    }

    foreach ($row in $boundaryRows) {
        if ([int]$row.sample_count -ne 64) { throw "Boundary sample count drifted for $runName." }
    }
    $runMax = Parse-Double $summary['process-max-us'] "process $runIndex max"
    $runMaxBoundary = [int]$summary['process-max-boundary-index']
    $callsOver = [int]$summary['calls-over-max-ceiling']
    $boundariesOver = [int]$summary['boundaries-over-max-ceiling']
    $gcOver = [int]$summary['exceedances-with-gc-activity']
    $boundaryCallsOver = ($boundaryRows | Measure-Object -Property calls_over_max_ceiling -Sum).Sum
    $boundaryCountOver = @($boundaryRows | Where-Object { [int]$_.calls_over_max_ceiling -gt 0 }).Count
    $boundaryMaxOwner = $boundaryRows | Sort-Object -Property @{ Expression = { Parse-Double $_.max_us 'boundary max' }; Descending = $true } | Select-Object -First 1
    $boundaryMax = Parse-Double $boundaryMaxOwner.max_us "process $runIndex boundary max"
    $summaryOwnerRows = @($boundaryRows | Where-Object { [int]$_.boundary_index -eq $runMaxBoundary })
    if ($summaryOwnerRows.Count -ne 1) { throw "Process summary maximum boundary is missing or duplicated for $runName." }
    $summaryOwnerMax = Parse-Double $summaryOwnerRows[0].max_us "process $runIndex summary-owner max"
    if ([int]$boundaryCallsOver -ne $callsOver) { throw "Boundary/summary exceedance count mismatch for $runName." }
    if ($boundaryCountOver -ne $boundariesOver) { throw "Boundary/summary exceedance-owner count mismatch for $runName." }
    if ([Math]::Abs($boundaryMax - $runMax) -gt 1e-9 -or [Math]::Abs($summaryOwnerMax - $runMax) -gt 1e-9) { throw "Boundary/summary maximum mismatch for $runName." }
    $runRows += [pscustomobject]@{
        RunIndex = $runIndex
        ProcessMaxUs = $runMax
        ProcessMaxBoundaryIndex = $runMaxBoundary
        CallsOver = $callsOver
        BoundariesOver = $boundariesOver
        ExceedancesWithGc = $gcOver
    }

    foreach ($row in $boundaryRows) {
        $boundaryIndex = [int]$row.boundary_index
        if (-not $boundaryByIndex.ContainsKey($boundaryIndex)) {
            $boundaryByIndex[$boundaryIndex] = @()
        }
        $boundaryByIndex[$boundaryIndex] += [pscustomobject]@{
            RunIndex = $runIndex
            BoundaryIndex = $boundaryIndex
            TemperatureC = Parse-Double $row.boundary_temperature_c "boundary temperature"
            MaxUs = Parse-Double $row.max_us "boundary max"
            CallsOver = [int]$row.calls_over_max_ceiling
            ExceedancesWithGc = [int]$row.exceedances_with_gc_activity
        }
    }
}

if ($boundaryByIndex.Count -ne $expectedBoundaries) { throw 'Cross-process boundary count drifted.' }

$crossRows = @()
foreach ($boundaryIndex in ($boundaryByIndex.Keys | Sort-Object {[int]$_})) {
    $rows = @($boundaryByIndex[$boundaryIndex])
    if ($rows.Count -ne $expectedRuns) { throw "Boundary $boundaryIndex does not appear in all process runs." }
    $temperature = $rows[0].TemperatureC
    if (@($rows | Where-Object { [Math]::Abs($_.TemperatureC - $temperature) -gt 1e-12 }).Count -ne 0) { throw "Boundary temperature drifted across process runs for boundary $boundaryIndex." }
    $runsWithExceedance = @($rows | Where-Object { $_.CallsOver -gt 0 }).Count
    $totalCallsOver = ($rows | Measure-Object -Property CallsOver -Sum).Sum
    $totalGcOver = ($rows | Measure-Object -Property ExceedancesWithGc -Sum).Sum
    $maxOwner = $rows | Sort-Object -Property MaxUs -Descending | Select-Object -First 1
    $confirmed = $runsWithExceedance -ge 2
    $crossRows += [pscustomobject]@{
        BoundaryIndex = [int]$boundaryIndex
        TemperatureC = $temperature
        ProcessRunsWithExceedance = $runsWithExceedance
        TotalCallsOver = [int]$totalCallsOver
        TotalExceedancesWithGc = [int]$totalGcOver
        CrossProcessMaxUs = $maxOwner.MaxUs
        MaxOwnerProcessRunIndex = $maxOwner.RunIndex
        SameBoundarySlowPathConfirmed = $confirmed
    }
}

$confirmedRows = @($crossRows | Where-Object { $_.SameBoundarySlowPathConfirmed })
$processRunsWithAnyExceedance = @($runRows | Where-Object { $_.CallsOver -gt 0 }).Count
$totalCallsOverAllRuns = ($runRows | Measure-Object -Property CallsOver -Sum).Sum
$overallMaxOwner = $runRows | Sort-Object -Property ProcessMaxUs -Descending | Select-Object -First 1

if ($confirmedRows.Count -gt 0) {
    $classification = 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED'
    $c4PlanningJustified = $true
}
elseif ($totalCallsOverAllRuns -gt 0) {
    $classification = 'C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL'
    $c4PlanningJustified = $false
}
else {
    $classification = 'C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED'
    $c4PlanningJustified = $false
}

$runLines = @('process_run_index,process_max_us,process_max_boundary_index,calls_over_max_ceiling,boundaries_over_max_ceiling,exceedances_with_gc_activity')
foreach ($row in $runRows) {
    $runLines += ('{0},{1},{2},{3},{4},{5}' -f $row.RunIndex, (F $row.ProcessMaxUs), $row.ProcessMaxBoundaryIndex, $row.CallsOver, $row.BoundariesOver, $row.ExceedancesWithGc)
}
Write-Utf8NoBom (Join-Path $reportDir '02-cross-process-run-summary.csv') $runLines

$boundaryLines = @('boundary_index,boundary_temperature_c,process_runs_with_exceedance,total_calls_over_max_ceiling,total_exceedances_with_gc_activity,cross_process_max_us,max_owner_process_run_index,same_boundary_slow_path_confirmed')
foreach ($row in $crossRows) {
    $boundaryLines += ('{0},{1},{2},{3},{4},{5},{6},{7}' -f $row.BoundaryIndex, (F $row.TemperatureC), $row.ProcessRunsWithExceedance, $row.TotalCallsOver, $row.TotalExceedancesWithGc, (F $row.CrossProcessMaxUs), $row.MaxOwnerProcessRunIndex, (B $row.SameBoundarySlowPathConfirmed))
}
Write-Utf8NoBom (Join-Path $reportDir '03-cross-process-boundary-summary.csv') $boundaryLines

$contractLines = @(
    'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT5',
    'scope=C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY',
    'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE',
    'candidate-source-changed=False',
    'performance-replanning1-status=PASS-AS-AUTHORED',
    'independent-process-runs=5',
    'r1-boundary-count=320',
    'measured-calls-per-process=20480',
    'resolve-max-ceiling-us=' + (F $ceiling),
    'candidate-specific-slow-path-rule=SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS',
    'strict-single-call-max-preserved=True',
    'historical-refinement3-r1-max-us=3667.1',
    'historical-refinement4-screen-max-us=2620.6',
    'historical-refinement4-targeted-max-us=853.5',
    'c4-created=False',
    'rp1c-selection-authorized=False',
    'production-src-change-authorized=False'
)
Write-Utf8NoBom (Join-Path $reportDir '01-contract-and-provenance.txt') $contractLines

$adjudicationLines = @(
    'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE',
    'classification=' + $classification,
    'process-runs-with-any-exceedance=' + $processRunsWithAnyExceedance,
    'total-calls-over-max-ceiling=' + [int]$totalCallsOverAllRuns,
    'same-boundary-confirmed-count=' + $confirmedRows.Count,
    'cross-process-max-us=' + (F $overallMaxOwner.ProcessMaxUs),
    'cross-process-max-owner-process-run-index=' + $overallMaxOwner.RunIndex,
    'cross-process-max-owner-boundary-index=' + $overallMaxOwner.ProcessMaxBoundaryIndex,
    'c4-planning-justified=' + (B $c4PlanningJustified),
    'c4-implementation-authorized=False',
    'rp1c-selection-authorized=False',
    'strict-single-call-max-preserved=True',
    'historical-exceedances-preserved=True'
)
Write-Utf8NoBom (Join-Path $reportDir '04-performance-reproducibility-adjudication.txt') $adjudicationLines

$summaryLines = @(
    'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT5',
    'status=PASS-CROSS-PROCESS-REPRODUCIBILITY-EVIDENCE-COMPLETE',
    'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE',
    'candidate-source-changed=False',
    'independent-process-runs=5',
    'process-runs-with-any-exceedance=' + $processRunsWithAnyExceedance,
    'total-calls-over-max-ceiling=' + [int]$totalCallsOverAllRuns,
    'same-boundary-confirmed-count=' + $confirmedRows.Count,
    'classification=' + $classification,
    'c4-planning-justified=' + (B $c4PlanningJustified),
    'c4-implementation-authorized=False',
    'rp1c-selection-performed=False',
    'rp1c-selection-authorized=False',
    'production-src-change-authorized=False',
    'thermodynamic-repair-authorized=False',
    'thermodynamic-tolerance-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False',
    'next-action=Return complete Refinement 5 artifacts for engineering review before any C4 planning, performance-contract adjudication or RP1C.'
)
Write-Utf8NoBom (Join-Path $reportDir '05-rp1b-refinement5-summary.txt') $summaryLines

Write-Host ('RP1B Refinement 5 cross-process adjudication: {0}' -f $classification)
Write-Host ('Same-boundary confirmed count: {0}' -f $confirmedRows.Count)
