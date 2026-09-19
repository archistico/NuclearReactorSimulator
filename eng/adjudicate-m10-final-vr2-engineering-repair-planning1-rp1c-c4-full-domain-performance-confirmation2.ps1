$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$artifactRoot = 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2'
$requiredFingerprint = '931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336'
$requiredPowerGuid = '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c'
$inv = [Globalization.CultureInfo]::InvariantCulture
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Fail([string]$Message) { Write-Host ("FDPC2 evidence adjudication: FAIL - {0}" -f $Message) -ForegroundColor Red; exit 2 }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }
function D([string]$Value) { return [double]::Parse($Value, [Globalization.NumberStyles]::Float, $inv) }
function I([string]$Value) { return [int]::Parse($Value, $inv) }
function L([string]$Value) { return [long]::Parse($Value, $inv) }
function F([double]$Value) { return $Value.ToString('R', $inv) }
function B([bool]$Value) { if ($Value) { return 'True' }; return 'False' }
function Write-Lines([string]$Path, [string[]]$Lines) { [IO.File]::WriteAllLines($Path, $Lines, $utf8) }
function Read-KeyValues([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("missing key-value file: {0}" -f $Path)
    $map = @{}
    foreach ($line in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path))) {
        $index = $line.IndexOf('=')
        if ($index -gt 0) { $map[$line.Substring(0, $index)] = $line.Substring($index + 1) }
    }
    return $map
}
function Median-Sorted([double[]]$Sorted) {
    Require ($Sorted.Length -gt 0) 'Cannot calculate median of empty array.'
    $middle = [int]($Sorted.Length / 2)
    if (($Sorted.Length % 2) -eq 0) { return 0.5 * ($Sorted[$middle - 1] + $Sorted[$middle]) }
    return $Sorted[$middle]
}
function Percentile-Sorted([double[]]$Sorted, [double]$Percentile) {
    Require ($Sorted.Length -gt 0) 'Cannot calculate percentile of empty array.'
    $index = [int][Math]::Ceiling($Percentile * $Sorted.Length) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Sorted.Length) { $index = $Sorted.Length - 1 }
    return $Sorted[$index]
}
function Require-CompleteMatrix([object[]]$Rows, [int]$Passes, [int]$RowCount, [int]$RunIndex, [string]$Label) {
    $seen = @{}
    foreach ($row in $Rows) {
        $run = I $row.run_index
        $pass = I $row.pass_index
        $idx = I $row.row_index
        Require ($run -eq $RunIndex) ("{0} run_index mismatch: expected {1}; actual {2}" -f $Label,$RunIndex,$run)
        Require ($pass -ge 0 -and $pass -lt $Passes) ("{0} pass_index out of range: {1}" -f $Label,$pass)
        Require ($idx -ge 0 -and $idx -lt $RowCount) ("{0} row_index out of range: {1}" -f $Label,$idx)
        $key = $pass.ToString($inv) + ':' + $idx.ToString($inv)
        Require (-not $seen.ContainsKey($key)) ("{0} duplicate matrix identity: {1}" -f $Label,$key)
        $seen[$key] = $true
    }
    Require ($seen.Count -eq ($Passes * $RowCount)) ("{0} complete matrix identity count mismatch: {1}" -f $Label,$seen.Count)
}

Require (Test-Path -LiteralPath $artifactRoot -PathType Container) 'artifact root missing'
$hostEvidence = Read-KeyValues (Join-Path $artifactRoot '02-execution-host-provenance.txt')
$hostAudit = Read-KeyValues (Join-Path $artifactRoot '03-host-consistency-audit.txt')
Require ($hostEvidence['status'] -eq 'COMPLETE-HOST-PROVENANCE-CAPTURE') 'host provenance incomplete'
Require ($hostEvidence['host-fingerprint-match'] -eq 'True') 'host fingerprint changed during FDPC2'
Require ($hostEvidence['active-power-scheme-stable'] -eq 'True') 'active power scheme changed during FDPC2'
Require ($hostEvidence['start-host-fingerprint-sha256'] -eq $requiredFingerprint) 'FDPC2 start host is not the frozen A2 host'
Require ($hostEvidence['end-host-fingerprint-sha256'] -eq $requiredFingerprint) 'FDPC2 end host is not the frozen A2 host'
Require ($hostEvidence['start-active-power-scheme'].ToLowerInvariant().Contains($requiredPowerGuid)) 'FDPC2 start power scheme GUID mismatch'
Require ($hostEvidence['end-active-power-scheme'].ToLowerInvariant().Contains($requiredPowerGuid)) 'FDPC2 end power scheme GUID mismatch'
Require ($hostAudit['status'] -eq 'PASS-SINGLE-HOST-INTEGRITY') 'host consistency audit incomplete'
Require ($hostAudit['host-fingerprint-sha256'] -eq $requiredFingerprint) 'host audit fingerprint mismatch'
Require ($hostAudit['runtime-profile'] -eq 'AMBIENT-UNSET') 'host audit runtime profile mismatch'

$runLines = New-Object 'System.Collections.Generic.List[string]'
$runLines.Add('run_index,exact_v9_calls,seam_calls,exact_v9_median_us,exact_v9_p95_us,exact_v9_max_us,exact_v9_median_allocated_bytes,exact_v9_unresolved,exact_v9_calls_over_max,seam_unresolved,seam_max_us,seam_calls_over_max,candidate_allocated_bytes_sum,harness_allocated_bytes,strict_corrected_predicate_met')
$sideNames = @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')
$sideStats = @{}
foreach ($side in $sideNames) { $sideStats[$side] = @{ Calls = 0; Max = [double]::NegativeInfinity; Over = 0; Unresolved = 0 } }
$allPass = $true
$totalCalls = 0
$totalCandidateAllocated = [long]0
$totalHarnessAllocated = [long]0
$totalExactOver = 0
$totalSeamOver = 0

for ($runIndex = 1; $runIndex -le 5; $runIndex++) {
    $processDir = Join-Path $artifactRoot ('process-' + $runIndex.ToString('00', $inv))
    Require (Test-Path -LiteralPath $processDir -PathType Container) ("missing process directory: {0}" -f $runIndex)
    foreach ($name in @('01-process-contract.txt','02-exact-v9-call-timing.csv','03-seam-call-timing.csv','04-runtime-context.txt','05-process-summary.txt')) {
        Require (Test-Path -LiteralPath (Join-Path $processDir $name) -PathType Leaf) ("missing process file: process-{0}/{1}" -f $runIndex,$name)
    }

    $processContract = Read-KeyValues (Join-Path $processDir '01-process-contract.txt')
    $runtime = Read-KeyValues (Join-Path $processDir '04-runtime-context.txt')
    $summary = Read-KeyValues (Join-Path $processDir '05-process-summary.txt')
    foreach ($map in @($processContract,$runtime,$summary)) {
        Require ($map['runtime-profile'] -eq 'AMBIENT-UNSET') ("runtime profile mismatch: run {0}" -f $runIndex)
        Require ($map['host-fingerprint-sha256'] -eq $requiredFingerprint) ("host fingerprint mismatch: run {0}" -f $runIndex)
        Require ($map['controlled-runtime-variables-unset'] -eq 'True') ("runtime control contamination reported: run {0}" -f $runIndex)
    }
    Require ($processContract['gate'] -eq 'RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2') ("process gate mismatch: run {0}" -f $runIndex)
    Require ($processContract['candidate'] -eq 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE') ("candidate mismatch: run {0}" -f $runIndex)

    $exact = @(Import-Csv -LiteralPath (Join-Path $processDir '02-exact-v9-call-timing.csv'))
    $seam = @(Import-Csv -LiteralPath (Join-Path $processDir '03-seam-call-timing.csv'))
    Require ($exact.Count -eq 23040) ("exact-v9 row count mismatch: run {0}" -f $runIndex)
    Require ($seam.Count -eq 20480) ("seam row count mismatch: run {0}" -f $runIndex)
    Require-CompleteMatrix $exact 64 360 $runIndex ("exact-v9 run {0}" -f $runIndex)
    Require-CompleteMatrix $seam 16 1280 $runIndex ("seam run {0}" -f $runIndex)

    [double[]]$exactTimes = New-Object double[] $exact.Count
    [double[]]$exactAlloc = New-Object double[] $exact.Count
    $exactUnresolved = 0
    $exactOver = 0
    [long]$candidateAllocated = 0
    for ($i = 0; $i -lt $exact.Count; $i++) {
        $time = D $exact[$i].elapsed_us
        $alloc = L $exact[$i].allocated_bytes
        Require ([double]::IsNaN($time) -eq $false -and [double]::IsInfinity($time) -eq $false -and $time -ge 0) ("invalid exact timing: run {0}, row {1}" -f $runIndex,$i)
        Require ($alloc -ge 0) ("negative exact allocation: run {0}, row {1}" -f $runIndex,$i)
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
        Require ([double]::IsNaN($time) -eq $false -and [double]::IsInfinity($time) -eq $false -and $time -ge 0) ("invalid seam timing: run {0}" -f $runIndex)
        Require ($alloc -ge 0) ("negative seam allocation: run {0}" -f $runIndex)
        $candidateAllocated += $alloc
        if ($row.resolved -ne 'True') { $seamUnresolved++ }
        if ($time -gt 409.30666666666673) { $seamOver++ }
        if ($time -gt $seamMax) { $seamMax = $time }
        $side = $row.probe_side
        Require ($sideStats.ContainsKey($side)) ("unexpected seam side: {0}" -f $side)
        $agg = $sideStats[$side]
        $agg.Calls++
        if ($time -gt $agg.Max) { $agg.Max = $time }
        if ($time -gt 409.30666666666673) { $agg.Over++ }
        if ($row.resolved -ne 'True') { $agg.Unresolved++ }
    }

    $harnessAllocated = L $runtime['harness-allocated-bytes']
    Require ($harnessAllocated -eq 0) ("measured harness allocated bytes in run {0}: {1}" -f $runIndex,$harnessAllocated)
    Require ((L $runtime['candidate-call-allocated-bytes-sum']) -eq $candidateAllocated) ("candidate allocation sum mismatch: run {0}" -f $runIndex)
    Require ($summary['status'] -eq 'PASS-PROCESS-EVIDENCE-COMPLETE') ("process summary incomplete: run {0}" -f $runIndex)
    Require ([Math]::Abs((D $summary['exact-v9-median-us']) - $exactMedian) -le 1e-9) ("exact median summary mismatch: run {0}" -f $runIndex)
    Require ([Math]::Abs((D $summary['exact-v9-p95-us']) - $exactP95) -le 1e-9) ("exact p95 summary mismatch: run {0}" -f $runIndex)
    Require ([Math]::Abs((D $summary['exact-v9-max-us']) - $exactMax) -le 1e-9) ("exact max summary mismatch: run {0}" -f $runIndex)
    Require ([Math]::Abs((D $summary['exact-v9-median-allocated-bytes']) - $exactMedianAlloc) -le 1e-9) ("exact allocation median mismatch: run {0}" -f $runIndex)
    Require ([Math]::Abs((D $summary['seam-max-us']) - $seamMax) -le 1e-9) ("seam max summary mismatch: run {0}" -f $runIndex)

    $strict = $exactUnresolved -eq 0 -and $seamUnresolved -eq 0 -and $exactMedian -le 94.8 -and $exactP95 -le 158.80666666666667 -and $exactMax -le 409.30666666666673 -and $seamMax -le 409.30666666666673 -and $exactMedianAlloc -le 2816
    Require ($summary['strict-corrected-performance-predicate-met'] -eq (B $strict)) ("process strict-predicate summary mismatch: run {0}" -f $runIndex)
    Require ((I $summary['exact-v9-unresolved-calls']) -eq $exactUnresolved) ("exact unresolved summary mismatch: run {0}" -f $runIndex)
    Require ((I $summary['seam-unresolved-calls']) -eq $seamUnresolved) ("seam unresolved summary mismatch: run {0}" -f $runIndex)

    $allPass = $allPass -and $strict
    $runLines.Add(($runIndex.ToString($inv) + ',23040,20480,' + (F $exactMedian) + ',' + (F $exactP95) + ',' + (F $exactMax) + ',' + (F $exactMedianAlloc) + ',' + $exactUnresolved.ToString($inv) + ',' + $exactOver.ToString($inv) + ',' + $seamUnresolved.ToString($inv) + ',' + (F $seamMax) + ',' + $seamOver.ToString($inv) + ',' + $candidateAllocated.ToString($inv) + ',' + $harnessAllocated.ToString($inv) + ',' + (B $strict)))
    $totalCalls += 43520
    $totalCandidateAllocated += $candidateAllocated
    $totalHarnessAllocated += $harnessAllocated
    $totalExactOver += $exactOver
    $totalSeamOver += $seamOver
}

Require ($totalCalls -eq 217600) ("total measured calls mismatch: {0}" -f $totalCalls)
foreach ($side in $sideNames) { Require ($sideStats[$side].Calls -eq 25600) ("seam side call count mismatch: {0}" -f $side) }

$classification = if ($allPass) { 'C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED' } else { 'C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED' }
$selectionReady = $allPass

Write-Lines (Join-Path $artifactRoot '01-contract-and-provenance.txt') @(
    'gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2',
    'status=PASS-EVIDENCE-COMPLETE',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-v1',
    'candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
    'runtime-profile=AMBIENT-UNSET',
    ('host-fingerprint-sha256=' + $requiredFingerprint),
    'fresh-processes=5',
    'measured-calls=217600',
    'exact-v9-resolve-median-ceiling-us=94.8',
    'exact-v9-resolve-p95-ceiling-us=158.80666666666667',
    'single-call-max-ceiling-us=409.30666666666673',
    'exact-v9-median-allocation-ceiling-bytes=2816',
    'fdpc1-history-preserved=True',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'production-repair-authorized=False'
)
Write-Lines (Join-Path $artifactRoot '04-cross-process-run-summary.csv') $runLines.ToArray()

$sideLines = New-Object 'System.Collections.Generic.List[string]'
$sideLines.Add('probe_side,total_calls,max_us,calls_over_max_ceiling,unresolved_calls')
foreach ($side in $sideNames) {
    $agg = $sideStats[$side]
    $sideLines.Add(($side + ',' + $agg.Calls.ToString($inv) + ',' + (F $agg.Max) + ',' + $agg.Over.ToString($inv) + ',' + $agg.Unresolved.ToString($inv)))
}
Write-Lines (Join-Path $artifactRoot '05-cross-process-seam-side-summary.csv') $sideLines.ToArray()

Write-Lines (Join-Path $artifactRoot '06-confirmation-evidence-adjudication.txt') @(
    'status=PASS-EVIDENCE-ADJUDICATED',
    ('classification=' + $classification),
    ('all-five-processes-meet-corrected-performance-predicate=' + (B $allPass)),
    ('total-measured-calls=' + $totalCalls.ToString($inv)),
    ('exact-v9-calls-over-max=' + $totalExactOver.ToString($inv)),
    ('seam-calls-over-max=' + $totalSeamOver.ToString($inv)),
    ('candidate-allocated-bytes-sum=' + $totalCandidateAllocated.ToString($inv)),
    ('harness-allocated-bytes-sum=' + $totalHarnessAllocated.ToString($inv)),
    ('c4-ambient-pair-selection-ready-after-returned-fdpc2-adjudication=' + (B $selectionReady)),
    'negative-performance-outcome-is-runner-failure=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'production-repair-authorized=False'
)
Write-Lines (Join-Path $artifactRoot '07-rp1c-c4-full-domain-performance-confirmation2-summary.txt') @(
    'gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2',
    'status=PASS-CONFIRMATION-EVIDENCE-GENERATED-AND-ADJUDICATED',
    ('classification=' + $classification),
    ('all-five-processes-pass=' + (B $allPass)),
    'runtime-profile=AMBIENT-UNSET',
    ('same-a2-host=True'),
    ('fdpc2-selection-ready=' + (B $selectionReady)),
    'fdpc1-negative-history-preserved=True',
    'rp1c-selection-performed=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False',
    'next-action=RETURN-COMPLETE-FDPC2-ARTIFACT-FOLDER-FOR-ENGINEERING-ADJUDICATION-BEFORE-RP1C-SELECTION'
)

$fileCount = @(Get-ChildItem -LiteralPath $artifactRoot -File -Recurse).Count
Require ($fileCount -eq 32) ("required evidence file count mismatch after adjudication: {0}" -f $fileCount)
Write-Host ("RP1C C4 Full-Domain Performance Confirmation 2 classification: {0}" -f $classification) -ForegroundColor Green
exit 0
