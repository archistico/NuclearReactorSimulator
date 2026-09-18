$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$artifactRoot = 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1'
$inv = [Globalization.CultureInfo]::InvariantCulture
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Fail([string]$Message) { Write-Host "Confirmation evidence adjudication: FAIL - $Message" -ForegroundColor Red; exit 2 }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }
function D([string]$Value) { return [double]::Parse($Value, [Globalization.NumberStyles]::Float, $inv) }
function I([string]$Value) { return [int]::Parse($Value, $inv) }
function L([string]$Value) { return [long]::Parse($Value, $inv) }
function F([double]$Value) { return $Value.ToString('R', $inv) }
function B([bool]$Value) { if ($Value) { return 'True' }; return 'False' }
function Write-Lines([string]$Path, [string[]]$Lines) { [IO.File]::WriteAllLines($Path, $Lines, $utf8) }
function Read-KeyValues([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) "missing key-value file: $Path"
    $map = @{}
    foreach ($line in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path))) {
        $index = $line.IndexOf('=')
        if ($index -gt 0) { $map[$line.Substring(0, $index)] = $line.Substring($index + 1) }
    }
    return $map
}
function Median-Sorted([double[]]$Sorted) {
    if ($Sorted.Length -eq 0) { return [double]::NaN }
    $middle = [int]($Sorted.Length / 2)
    if (($Sorted.Length % 2) -eq 0) { return 0.5 * ($Sorted[$middle - 1] + $Sorted[$middle]) }
    return $Sorted[$middle]
}
function Percentile-Sorted([double[]]$Sorted, [double]$Percentile) {
    if ($Sorted.Length -eq 0) { return [double]::NaN }
    $index = [int][Math]::Ceiling($Percentile * $Sorted.Length) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Sorted.Length) { $index = $Sorted.Length - 1 }
    return $Sorted[$index]
}

Require (Test-Path -LiteralPath $artifactRoot -PathType Container) 'artifact root missing'
$runLines = New-Object 'System.Collections.Generic.List[string]'
$runLines.Add('run_index,exact_v9_calls,seam_calls,exact_v9_median_us,exact_v9_p95_us,exact_v9_max_us,exact_v9_median_allocated_bytes,exact_v9_unresolved,seam_unresolved,seam_max_us,seam_calls_over_max_ceiling,candidate_allocated_bytes_sum,harness_allocated_bytes,strict_corrected_predicate_met')
$sideNames = @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')
$sideStats = @{}
foreach ($side in $sideNames) { $sideStats[$side] = @{ Calls = 0; Max = [double]::NegativeInfinity; Over = 0; Unresolved = 0 } }
$allPass = $true
$totalCalls = 0
$totalCandidateAllocated = [long]0
$totalHarnessAllocated = [long]0

for ($runIndex = 1; $runIndex -le 5; $runIndex++) {
    $processDir = Join-Path $artifactRoot ('process-' + $runIndex.ToString('00', $inv))
    Require (Test-Path -LiteralPath $processDir -PathType Container) "missing process directory: $runIndex"
    foreach ($name in @('01-process-contract.txt','02-exact-v9-call-timing.csv','03-seam-call-timing.csv','04-runtime-context.txt','05-process-summary.txt')) {
        Require (Test-Path -LiteralPath (Join-Path $processDir $name) -PathType Leaf) "missing process file: process-$runIndex/$name"
    }

    $exact = @(Import-Csv -LiteralPath (Join-Path $processDir '02-exact-v9-call-timing.csv'))
    $seam = @(Import-Csv -LiteralPath (Join-Path $processDir '03-seam-call-timing.csv'))
    Require ($exact.Count -eq 23040) "exact-v9 row count mismatch: run $runIndex"
    Require ($seam.Count -eq 20480) "seam row count mismatch: run $runIndex"

    [double[]]$exactTimes = New-Object double[] $exact.Count
    [double[]]$exactAlloc = New-Object double[] $exact.Count
    $exactUnresolved = 0
    $exactOver = 0
    [long]$candidateAllocated = 0
    for ($i = 0; $i -lt $exact.Count; $i++) {
        $time = D $exact[$i].elapsed_us
        $alloc = L $exact[$i].allocated_bytes
        $exactTimes[$i] = $time
        $exactAlloc[$i] = [double]$alloc
        $candidateAllocated += $alloc
        if ($exact[$i].resolved -ne 'True') { $exactUnresolved++ }
        if ($time -gt 409.30666666666673) { $exactOver++ }
    }
    [Array]::Sort($exactTimes)
    [Array]::Sort($exactAlloc)
    $exactMedian = Median-Sorted $exactTimes
    $exactP95 = Percentile-Sorted $exactTimes 0.95
    $exactMax = $exactTimes[$exactTimes.Length - 1]
    $exactMedianAlloc = Median-Sorted $exactAlloc

    $seamUnresolved = 0
    $seamOver = 0
    $seamMax = [double]::NegativeInfinity
    foreach ($row in $seam) {
        $time = D $row.elapsed_us
        $alloc = L $row.allocated_bytes
        $candidateAllocated += $alloc
        if ($row.resolved -ne 'True') { $seamUnresolved++ }
        if ($time -gt 409.30666666666673) { $seamOver++ }
        if ($time -gt $seamMax) { $seamMax = $time }
        $side = $row.probe_side
        Require ($sideStats.ContainsKey($side)) "unexpected seam side: $side"
        $agg = $sideStats[$side]
        $agg.Calls++
        if ($time -gt $agg.Max) { $agg.Max = $time }
        if ($time -gt 409.30666666666673) { $agg.Over++ }
        if ($row.resolved -ne 'True') { $agg.Unresolved++ }
    }

    $runtime = Read-KeyValues (Join-Path $processDir '04-runtime-context.txt')
    $summary = Read-KeyValues (Join-Path $processDir '05-process-summary.txt')
    $harnessAllocated = L $runtime['harness-allocated-bytes']
    Require ($harnessAllocated -eq 0) "measured harness allocated bytes in run ${runIndex}: $harnessAllocated"
    Require ((L $runtime['candidate-call-allocated-bytes-sum']) -eq $candidateAllocated) "candidate allocation sum mismatch: run $runIndex"
    Require ($summary['status'] -eq 'PASS-PROCESS-EVIDENCE-COMPLETE') "process summary incomplete: run $runIndex"
    Require ([Math]::Abs((D $summary['exact-v9-median-us']) - $exactMedian) -le 1e-9) "exact median summary mismatch: run $runIndex"
    Require ([Math]::Abs((D $summary['exact-v9-p95-us']) - $exactP95) -le 1e-9) "exact p95 summary mismatch: run $runIndex"
    Require ([Math]::Abs((D $summary['exact-v9-max-us']) - $exactMax) -le 1e-9) "exact max summary mismatch: run $runIndex"
    Require ([Math]::Abs((D $summary['exact-v9-median-allocated-bytes']) - $exactMedianAlloc) -le 1e-9) "exact allocation median mismatch: run $runIndex"
    Require ([Math]::Abs((D $summary['seam-max-us']) - $seamMax) -le 1e-9) "seam max summary mismatch: run $runIndex"

    $strict = $exactUnresolved -eq 0 -and $seamUnresolved -eq 0 -and $exactMedian -le 94.8 -and $exactP95 -le 158.80666666666667 -and $exactMax -le 409.30666666666673 -and $seamMax -le 409.30666666666673 -and $exactMedianAlloc -le 2816
    Require ($summary['strict-corrected-performance-predicate-met'] -eq (B $strict)) "process strict-predicate summary mismatch: run $runIndex"
    Require ((I $summary['exact-v9-unresolved-calls']) -eq $exactUnresolved) "exact unresolved summary mismatch: run $runIndex"
    Require ((I $summary['seam-unresolved-calls']) -eq $seamUnresolved) "seam unresolved summary mismatch: run $runIndex"
    $allPass = $allPass -and $strict
    $runLines.Add(($runIndex.ToString($inv) + ',23040,20480,' + (F $exactMedian) + ',' + (F $exactP95) + ',' + (F $exactMax) + ',' + (F $exactMedianAlloc) + ',' + $exactUnresolved.ToString($inv) + ',' + $seamUnresolved.ToString($inv) + ',' + (F $seamMax) + ',' + $seamOver.ToString($inv) + ',' + $candidateAllocated.ToString($inv) + ',' + $harnessAllocated.ToString($inv) + ',' + (B $strict)))
    $totalCalls += 43520
    $totalCandidateAllocated += $candidateAllocated
    $totalHarnessAllocated += $harnessAllocated
}

Require ($totalCalls -eq 217600) "total measured calls mismatch: $totalCalls"
foreach ($side in $sideNames) { Require ($sideStats[$side].Calls -eq 25600) "seam side call count mismatch: $side" }

$classification = if ($allPass) { 'C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED' } else { 'C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED' }
$selectionReadyAfterAdjudication = $allPass

Write-Lines (Join-Path $artifactRoot '01-contract-and-provenance.txt') @(
    'gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1',
    'status=PASS-EVIDENCE-COMPLETE',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1-v1',
    'candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
    'fresh-processes=5',
    'measured-calls=217600',
    'exact-v9-resolve-median-ceiling-us=94.8',
    'exact-v9-resolve-p95-ceiling-us=158.80666666666667',
    'single-call-max-ceiling-us=409.30666666666673',
    'exact-v9-median-allocation-ceiling-bytes=2816',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)
Write-Lines (Join-Path $artifactRoot '02-cross-process-run-summary.csv') $runLines.ToArray()

$sideLines = New-Object 'System.Collections.Generic.List[string]'
$sideLines.Add('probe_side,total_calls,max_us,calls_over_max_ceiling,unresolved_calls')
foreach ($side in $sideNames) {
    $agg = $sideStats[$side]
    $sideLines.Add(($side + ',' + $agg.Calls.ToString($inv) + ',' + (F $agg.Max) + ',' + $agg.Over.ToString($inv) + ',' + $agg.Unresolved.ToString($inv)))
}
Write-Lines (Join-Path $artifactRoot '03-cross-process-seam-side-summary.csv') $sideLines.ToArray()

Write-Lines (Join-Path $artifactRoot '04-confirmation-evidence-adjudication.txt') @(
    'status=PASS-EVIDENCE-ADJUDICATED',
    ('classification=' + $classification),
    ('all-five-processes-meet-corrected-performance-predicate=' + (B $allPass)),
    ('total-measured-calls=' + $totalCalls.ToString($inv)),
    ('candidate-allocated-bytes-sum=' + $totalCandidateAllocated.ToString($inv)),
    ('harness-allocated-bytes-sum=' + $totalHarnessAllocated.ToString($inv)),
    ('c4-selection-ready-after-returned-confirmation-adjudication=' + (B $selectionReadyAfterAdjudication)),
    'negative-performance-outcome-is-runner-failure=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)
Write-Lines (Join-Path $artifactRoot '05-rp1c-c4-full-domain-performance-confirmation1-summary.txt') @(
    'gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1',
    'status=PASS-CONFIRMATION-EVIDENCE-GENERATED-AND-ADJUDICATED',
    ('classification=' + $classification),
    ('all-five-processes-pass=' + (B $allPass)),
    'rp1c-selection-performed=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False',
    'next-action=RETURN-COMPLETE-CONFIRMATION-ARTIFACT-FOLDER-FOR-ENGINEERING-ADJUDICATION-BEFORE-RP1C-SELECTION'
)

$fileCount = @(Get-ChildItem -LiteralPath $artifactRoot -File -Recurse).Count
Require ($fileCount -eq 30) "required evidence file count mismatch after adjudication: $fileCount"
Write-Host ("RP1C C4 Full-Domain Performance Confirmation 1 classification: " + $classification) -ForegroundColor Green
exit 0
