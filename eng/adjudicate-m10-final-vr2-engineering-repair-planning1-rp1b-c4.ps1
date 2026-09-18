$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$artifactRoot = Join-Path $root 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$inv = [Globalization.CultureInfo]::InvariantCulture

function Fail([string]$Message) {
    Write-Host "C4 evidence adjudication: FAIL - $Message" -ForegroundColor Red
    exit 2
}

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { Fail $Message }
}

function Write-Lines([string]$Path, [string[]]$Lines) {
    [System.IO.File]::WriteAllLines($Path, $Lines, $utf8)
}

function Read-KeyValues([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) "missing key-value artifact: $Path"
    $map = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $index = $line.IndexOf('=')
        if ($index -gt 0) { $map[$line.Substring(0, $index)] = $line.Substring($index + 1) }
    }
    return $map
}

function D([string]$Value) { return [double]::Parse($Value, [Globalization.NumberStyles]::Float, $inv) }
function L([string]$Value) { return [long]::Parse($Value, [Globalization.NumberStyles]::Integer, $inv) }
function I([string]$Value) { return [int]::Parse($Value, [Globalization.NumberStyles]::Integer, $inv) }
function F([double]$Value) { return $Value.ToString('R', $inv) }
function B([bool]$Value) { if ($Value) { return 'True' } else { return 'False' } }

function Median-Sorted([double[]]$Values) {
    if ($Values.Length -eq 0) { return [double]::NaN }
    $middle = [int]($Values.Length / 2)
    if (($Values.Length % 2) -eq 0) { return 0.5 * ($Values[$middle - 1] + $Values[$middle]) }
    return $Values[$middle]
}

function P95-Sorted([double[]]$Values) {
    if ($Values.Length -eq 0) { return [double]::NaN }
    $index = [int][Math]::Ceiling(0.95 * $Values.Length) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Values.Length) { $index = $Values.Length - 1 }
    return $Values[$index]
}

Require (Test-Path -LiteralPath $artifactRoot -PathType Container) 'artifact root is missing'

$statePath = Join-Path $artifactRoot '02-state-semantic-equivalence.csv'
$hydraulicPath = Join-Path $artifactRoot '03-hydraulic-semantic-equivalence.csv'
$semanticSummaryPath = Join-Path $artifactRoot '04-semantic-equivalence-summary.txt'
Require (Test-Path -LiteralPath $statePath -PathType Leaf) 'state semantic evidence missing'
Require (Test-Path -LiteralPath $hydraulicPath -PathType Leaf) 'hydraulic semantic evidence missing'
Require (Test-Path -LiteralPath $semanticSummaryPath -PathType Leaf) 'semantic summary missing'

$stateRows = @(Import-Csv -LiteralPath $statePath)
$hydraulicRows = @(Import-Csv -LiteralPath $hydraulicPath)
Require ($stateRows.Count -eq 1679) "state semantic row count drift: $($stateRows.Count)"
Require ($hydraulicRows.Count -eq 288) "hydraulic semantic row count drift: $($hydraulicRows.Count)"
$semanticSummary = Read-KeyValues $semanticSummaryPath
Require ($semanticSummary['status'] -eq 'PASS-SEMANTIC-EVIDENCE-COMPLETE') 'semantic process did not complete its evidence contract'
Require ((I $semanticSummary['state-observation-count']) -eq 1679) 'semantic state count mismatch'
Require ((I $semanticSummary['hydraulic-observation-count']) -eq 288) 'semantic hydraulic count mismatch'
Require ((I $semanticSummary['total-observation-count']) -eq 1967) 'semantic total count mismatch'

$stateMismatchCount = @($stateRows | Where-Object { $_.bit_equivalent -ne 'true' }).Count
$hydraulicMismatchCount = @($hydraulicRows | Where-Object { $_.bit_equivalent -ne 'true' }).Count
$repeatMismatchCount = @($stateRows | Where-Object { $_.c4_repeat_deterministic -ne 'true' }).Count + @($hydraulicRows | Where-Object { $_.c4_repeat_deterministic -ne 'true' }).Count
Require ($stateMismatchCount -eq (I $semanticSummary['state-bit-mismatch-count'])) 'state semantic summary mismatch'
Require ($hydraulicMismatchCount -eq (I $semanticSummary['hydraulic-bit-mismatch-count'])) 'hydraulic semantic summary mismatch'
Require ($repeatMismatchCount -eq (I $semanticSummary['repeat-mismatch-count'])) 'repeat semantic summary mismatch'

$runLines = New-Object 'System.Collections.Generic.List[string]'
$runLines.Add('lane,run_index,measured_calls,median_us,p95_us,max_us,calls_over_max_ceiling,unresolved_calls,nonzero_allocation_calls,candidate_allocated_bytes,harness_allocated_bytes,fallback_calls,median_within_ceiling,p95_within_ceiling,max_within_ceiling,zero_allocation,harness_allocation_neutral,zero_fallback')
$boundaryAggregates = @{}
$totalMeasuredCalls = 0
$totalCallsOver = 0
$totalUnresolved = 0
$totalNonzeroAllocation = 0
$totalCandidateAllocated = [long]0
$totalHarnessAllocated = [long]0
$totalFallbackCalls = 0
$allMedianWithin = $true
$allP95Within = $true
$allMaxWithin = $true

foreach ($lane in @('A', 'B')) {
    $laneDirName = if ($lane -eq 'A') { 'lane-a' } else { 'lane-b' }
    for ($runIndex = 1; $runIndex -le 5; $runIndex++) {
        $processDir = Join-Path $artifactRoot (Join-Path $laneDirName ('process-{0:D2}' -f $runIndex))
        Require (Test-Path -LiteralPath $processDir -PathType Container) "missing process directory: $lane/$runIndex"
        $required = @('01-process-contract.txt','02-c4-r1-call-timing.csv','03-c4-r1-boundary-summary.csv','04-runtime-context.txt','05-process-summary.txt')
        foreach ($name in $required) { Require (Test-Path -LiteralPath (Join-Path $processDir $name) -PathType Leaf) "missing process artifact: $lane/$runIndex/$name" }
        Require (@(Get-ChildItem -LiteralPath $processDir -File).Count -eq 5) "unexpected file count in process directory: $lane/$runIndex"

        $callRows = @(Import-Csv -LiteralPath (Join-Path $processDir '02-c4-r1-call-timing.csv'))
        $boundaryRows = @(Import-Csv -LiteralPath (Join-Path $processDir '03-c4-r1-boundary-summary.csv'))
        Require ($callRows.Count -eq 20480) "call timing row count drift: $lane/$runIndex"
        Require ($boundaryRows.Count -eq 320) "boundary summary row count drift: $lane/$runIndex"
        Require (@($callRows | Where-Object { $_.lane -ne $lane -or (I $_.run_index) -ne $runIndex }).Count -eq 0) "call timing lane/run identity drift: $lane/$runIndex"
        Require (@($boundaryRows | Where-Object { $_.lane -ne $lane -or (I $_.run_index) -ne $runIndex }).Count -eq 0) "boundary summary lane/run identity drift: $lane/$runIndex"

        [double[]]$elapsed = @($callRows | ForEach-Object { D $_.elapsed_us })
        [Array]::Sort($elapsed)
        $median = Median-Sorted $elapsed
        $p95 = P95-Sorted $elapsed
        $maximum = $elapsed[$elapsed.Length - 1]
        $callsOver = @($callRows | Where-Object { (D $_.elapsed_us) -gt 409.30666666666673 }).Count
        $unresolved = @($callRows | Where-Object { $_.resolved -ne 'true' }).Count
        $nonzeroAllocation = @($callRows | Where-Object { (L $_.allocated_bytes) -ne 0 }).Count
        $candidateAllocated = [long](($callRows | ForEach-Object { L $_.allocated_bytes } | Measure-Object -Sum).Sum)
        $fallbackCalls = @($callRows | Where-Object { $_.resolution_path -eq 'IMMUTABLE-C2-FALLBACK' }).Count

        $runtime = Read-KeyValues (Join-Path $processDir '04-runtime-context.txt')
        $summary = Read-KeyValues (Join-Path $processDir '05-process-summary.txt')
        $harnessAllocated = (L $runtime['harness-allocated-bytes'])
        Require ($summary['status'] -eq 'PASS-PROCESS-EVIDENCE-COMPLETE') "process summary incomplete: $lane/$runIndex"
        Require ((I $summary['measured-calls']) -eq 20480) "process measured-call count mismatch: $lane/$runIndex"
        Require ([Math]::Abs((D $summary['median-us']) - $median) -le 1e-9) "process median mismatch: $lane/$runIndex"
        Require ([Math]::Abs((D $summary['p95-us']) - $p95) -le 1e-9) "process p95 mismatch: $lane/$runIndex"
        Require ([Math]::Abs((D $summary['max-us']) - $maximum) -le 1e-9) "process max mismatch: $lane/$runIndex"
        Require ((I $summary['calls-over-max-ceiling']) -eq $callsOver) "process exceedance count mismatch: $lane/$runIndex"
        Require ((I $summary['unresolved-calls']) -eq $unresolved) "process unresolved count mismatch: $lane/$runIndex"
        Require ((I $summary['nonzero-allocation-calls']) -eq $nonzeroAllocation) "process allocation count mismatch: $lane/$runIndex"
        Require ((L $summary['candidate-call-allocated-bytes-sum']) -eq $candidateAllocated) "process candidate allocation sum mismatch: $lane/$runIndex"
        Require ((L $summary['harness-allocated-bytes']) -eq $harnessAllocated) "process harness allocation mismatch: $lane/$runIndex"
        Require ((I $summary['fallback-calls']) -eq $fallbackCalls) "process fallback count mismatch: $lane/$runIndex"

        $medianWithin = $median -le 94.8
        $p95Within = $p95 -le 158.80666666666667
        $maxWithin = $maximum -le 409.30666666666673
        $zeroAllocation = $nonzeroAllocation -eq 0 -and $candidateAllocated -eq 0
        $harnessNeutral = $harnessAllocated -eq 0
        $zeroFallback = $fallbackCalls -eq 0
        $runLines.Add(($lane + ',' + $runIndex.ToString($inv) + ',20480,' + (F $median) + ',' + (F $p95) + ',' + (F $maximum) + ',' + $callsOver.ToString($inv) + ',' + $unresolved.ToString($inv) + ',' + $nonzeroAllocation.ToString($inv) + ',' + $candidateAllocated.ToString($inv) + ',' + $harnessAllocated.ToString($inv) + ',' + $fallbackCalls.ToString($inv) + ',' + (B $medianWithin) + ',' + (B $p95Within) + ',' + (B $maxWithin) + ',' + (B $zeroAllocation) + ',' + (B $harnessNeutral) + ',' + (B $zeroFallback)))

        $totalMeasuredCalls += 20480
        $totalCallsOver += $callsOver
        $totalUnresolved += $unresolved
        $totalNonzeroAllocation += $nonzeroAllocation
        $totalCandidateAllocated += $candidateAllocated
        $totalHarnessAllocated += $harnessAllocated
        $totalFallbackCalls += $fallbackCalls
        $allMedianWithin = $allMedianWithin -and $medianWithin
        $allP95Within = $allP95Within -and $p95Within
        $allMaxWithin = $allMaxWithin -and $maxWithin

        foreach ($row in $boundaryRows) {
            $boundary = I $row.boundary_index
            if (-not $boundaryAggregates.ContainsKey($boundary)) {
                $boundaryAggregates[$boundary] = @{
                    Temperature = D $row.boundary_temperature_c
                    RunCount = 0
                    Calls = 0
                    Medians = New-Object 'System.Collections.Generic.List[double]'
                    MaxP95 = [double]::NegativeInfinity
                    Max = [double]::NegativeInfinity
                    Over = 0
                    GcCalls = 0
                    GcExceedances = 0
                    NonzeroAllocation = 0
                    Fallback = 0
                }
            }
            $agg = $boundaryAggregates[$boundary]
            $agg.RunCount++
            $agg.Calls += (I $row.sample_count)
            $agg.Medians.Add((D $row.median_us))
            $agg.MaxP95 = [Math]::Max($agg.MaxP95, (D $row.p95_us))
            $agg.Max = [Math]::Max($agg.Max, (D $row.max_us))
            $agg.Over += (I $row.calls_over_max_ceiling)
            $agg.GcCalls += (I $row.calls_with_gc_activity)
            $agg.GcExceedances += (I $row.exceedances_with_gc_activity)
            $agg.NonzeroAllocation += (I $row.nonzero_allocation_calls)
            $agg.Fallback += (I $row.fallback_calls)
        }
    }
}

Require ($totalMeasuredCalls -eq 204800) "total measured call count drift: $totalMeasuredCalls"
Require ($boundaryAggregates.Count -eq 320) "cross-process boundary identity count drift: $($boundaryAggregates.Count)"

$boundaryLines = New-Object 'System.Collections.Generic.List[string]'
$boundaryLines.Add('boundary_index,boundary_temperature_c,lane_run_count,total_calls,median_of_lane_run_medians_us,max_lane_run_p95_us,max_us,total_calls_over_max_ceiling,total_calls_with_gc_activity,total_exceedances_with_gc_activity,total_nonzero_allocation_calls,total_fallback_calls')
for ($boundary = 0; $boundary -lt 320; $boundary++) {
    Require ($boundaryAggregates.ContainsKey($boundary)) "missing boundary aggregate: $boundary"
    $agg = $boundaryAggregates[$boundary]
    Require ($agg.RunCount -eq 10) "boundary lane-run count drift: $boundary"
    Require ($agg.Calls -eq 640) "boundary call count drift: $boundary"
    [double[]]$medians = $agg.Medians.ToArray()
    [Array]::Sort($medians)
    $medianOfMedians = Median-Sorted $medians
    $boundaryLines.Add(($boundary.ToString($inv) + ',' + (F $agg.Temperature) + ',' + $agg.RunCount.ToString($inv) + ',' + $agg.Calls.ToString($inv) + ',' + (F $medianOfMedians) + ',' + (F $agg.MaxP95) + ',' + (F $agg.Max) + ',' + $agg.Over.ToString($inv) + ',' + $agg.GcCalls.ToString($inv) + ',' + $agg.GcExceedances.ToString($inv) + ',' + $agg.NonzeroAllocation.ToString($inv) + ',' + $agg.Fallback.ToString($inv)))
}

$semanticEquivalent = $stateMismatchCount -eq 0 -and $hydraulicMismatchCount -eq 0 -and $repeatMismatchCount -eq 0 -and $totalUnresolved -eq 0
$allocationClosed = $totalNonzeroAllocation -eq 0 -and $totalCandidateAllocated -eq 0 -and $totalHarnessAllocated -eq 0 -and $totalFallbackCalls -eq 0
$wallClockClosed = $allMedianWithin -and $allP95Within -and $allMaxWithin -and $totalCallsOver -eq 0

if (-not $semanticEquivalent) { $classification = 'C4-SEMANTIC-EQUIVALENCE-FAILED' }
elseif (-not $allocationClosed) { $classification = 'C4-ALLOCATION-NOT-CLOSED' }
elseif (-not $wallClockClosed) { $classification = 'C4-WALL-CLOCK-TAIL-REMAINS' }
else { $classification = 'C4-QUALIFIED-ALLOCATION-TAIL-CLOSED' }

Write-Lines (Join-Path $artifactRoot '01-contract-and-provenance.txt') @(
    'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-C4',
    'status=PASS-EVIDENCE-COMPLETE',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1b-c4-v1',
    'candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
    'returned-planning-adjudication=PASS',
    'focused-process-invocations=11',
    'semantic-process-invocations=1',
    'timing-process-invocations=10',
    'measured-calls=204800',
    'strict-resolve-max-us=409.30666666666673',
    'required-bytes-per-call=0',
    'required-fallback-calls=0',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False'
)

Write-Lines (Join-Path $artifactRoot '05-allocation-closure-summary.txt') @(
    'status=PASS-ALLOCATION-EVIDENCE-COMPLETE',
    ('measured-calls=' + $totalMeasuredCalls.ToString($inv)),
    ('nonzero-allocation-calls=' + $totalNonzeroAllocation.ToString($inv)),
    ('candidate-call-allocated-bytes-sum=' + $totalCandidateAllocated.ToString($inv)),
    ('harness-allocated-bytes-sum=' + $totalHarnessAllocated.ToString($inv)),
    ('fallback-calls=' + $totalFallbackCalls.ToString($inv)),
    ('zero-allocation-closed=' + (B ($totalNonzeroAllocation -eq 0 -and $totalCandidateAllocated -eq 0))),
    ('harness-allocation-neutral=' + (B ($totalHarnessAllocated -eq 0))),
    ('r1-zero-fallback=' + (B ($totalFallbackCalls -eq 0))),
    ('allocation-closure-met=' + (B $allocationClosed))
)

Write-Lines (Join-Path $artifactRoot '06-cross-process-run-summary.csv') $runLines.ToArray()
Write-Lines (Join-Path $artifactRoot '07-cross-process-boundary-summary.csv') $boundaryLines.ToArray()
Write-Lines (Join-Path $artifactRoot '08-c4-evidence-adjudication.txt') @(
    'status=PASS-EVIDENCE-ADJUDICATED',
    ('classification=' + $classification),
    ('semantic-bit-equivalent=' + (B $semanticEquivalent)),
    ('state-bit-mismatch-count=' + $stateMismatchCount.ToString($inv)),
    ('hydraulic-bit-mismatch-count=' + $hydraulicMismatchCount.ToString($inv)),
    ('repeat-mismatch-count=' + $repeatMismatchCount.ToString($inv)),
    ('timing-unresolved-calls=' + $totalUnresolved.ToString($inv)),
    ('allocation-closure-met=' + (B $allocationClosed)),
    ('wall-clock-median-all-runs-within-ceiling=' + (B $allMedianWithin)),
    ('wall-clock-p95-all-runs-within-ceiling=' + (B $allP95Within)),
    ('wall-clock-max-all-calls-within-ceiling=' + (B $allMaxWithin)),
    ('calls-over-max-ceiling=' + $totalCallsOver.ToString($inv)),
    ('wall-clock-tail-closed=' + (B $wallClockClosed)),
    'engineering-negative-outcome-is-runner-failure=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)

Write-Lines (Join-Path $artifactRoot '09-rp1b-c4-summary.txt') @(
    'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-C4',
    'status=PASS-C4-EVIDENCE-GENERATED-AND-ADJUDICATED',
    ('classification=' + $classification),
    'candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
    'semantic-observations=1967',
    'measured-timing-calls=204800',
    ('semantic-bit-equivalent=' + (B $semanticEquivalent)),
    ('allocation-closure-met=' + (B $allocationClosed)),
    ('wall-clock-tail-closed=' + (B $wallClockClosed)),
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'next-action=RETURN-COMPLETE-C4-ARTIFACT-FOLDER-FOR-ENGINEERING-ADJUDICATION-BEFORE-RP1C'
)

$fileCount = @(Get-ChildItem -LiteralPath $artifactRoot -File -Recurse).Count
if ($fileCount -ne 59) {
    $adjPath = Join-Path $artifactRoot '08-c4-evidence-adjudication.txt'
    $lines = [System.IO.File]::ReadAllLines($adjPath)
    $updated = New-Object 'System.Collections.Generic.List[string]'
    foreach ($line in $lines) {
        if ($line.StartsWith('classification=', [StringComparison]::Ordinal)) { $updated.Add('classification=C4-EVIDENCE-INCOMPLETE') }
        else { $updated.Add($line) }
    }
    $updated.Add('required-file-count=59')
    $updated.Add(('actual-file-count=' + $fileCount.ToString($inv)))
    Write-Lines $adjPath $updated.ToArray()
    Fail "required evidence file count is 59, actual is $fileCount"
}

Write-Host ("C4 evidence adjudication: PASS - " + $classification) -ForegroundColor Green
Write-Host ("Artifacts: artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4")
exit 0
