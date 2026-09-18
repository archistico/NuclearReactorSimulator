$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}

function Read-KeyValueFile([string]$Path) {
    Require-File $Path
    $map = @{}
    foreach ($line in [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $index = $line.IndexOf('=')
        if ($index -le 0) { continue }
        $key = $line.Substring(0, $index)
        $value = $line.Substring($index + 1)
        $map[$key] = $value
    }
    return $map
}

function Parse-InvDouble([string]$Text, [string]$Label) {
    $value = 0.0
    $style = [System.Globalization.NumberStyles]::Float -bor [System.Globalization.NumberStyles]::AllowThousands
    if (-not [double]::TryParse($Text, $style, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw ("Invalid invariant double for {0}: {1}" -f $Label, $Text)
    }
    return $value
}

function Inv([double]$Value) {
    return $Value.ToString('R', [System.Globalization.CultureInfo]::InvariantCulture)
}

function Require-Equal([object]$Actual, [object]$Expected, [string]$Label) {
    if ($Actual -ne $Expected) {
        throw ("{0} mismatch: actual={1}; expected={2}" -f $Label, $Actual, $Expected)
    }
}

function Median-Sorted([double[]]$Sorted) {
    if ($Sorted.Length -eq 0) { return [double]::NaN }
    $middle = [int]($Sorted.Length / 2)
    if (($Sorted.Length % 2) -eq 0) {
        return 0.5 * ($Sorted[$middle - 1] + $Sorted[$middle])
    }
    return $Sorted[$middle]
}

function P95-Sorted([double[]]$Sorted) {
    if ($Sorted.Length -eq 0) { return [double]::NaN }
    $index = [int][Math]::Ceiling(0.95 * $Sorted.Length) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Sorted.Length) { $index = $Sorted.Length - 1 }
    return $Sorted[$index]
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1'
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1-contract.json'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
foreach ($path in @($contractPath,$exactPath,$performancePath)) { Require-File $path }
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) { throw 'Attribution artifact root is missing.' }

$contract = ([System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $contractPath).Path, [System.Text.Encoding]::UTF8)) | ConvertFrom-Json
$strictMax = [double]$contract.protocol.strict_max_us
$tailFloor = [double]$contract.protocol.diagnostic_tail_floor_us

$baselineRows = @(Import-Csv -LiteralPath $performancePath)
$baselineMap = @{}
foreach ($row in $baselineRows) {
    if (-not [string]::IsNullOrWhiteSpace([string]$row.metric)) { $baselineMap[[string]$row.metric] = [string]$row.value }
}
$baselineStrictMax = Parse-InvDouble $baselineMap['rp1b_candidate_resolve_max_ceiling_us'] 'Frozen performance max ceiling'
if ((Inv $baselineStrictMax) -ne (Inv $strictMax)) { throw 'Contract strict max does not match frozen performance baseline.' }

$exactCorpus = @(Import-Csv -LiteralPath $exactPath)
if ($exactCorpus.Count -ne 360) { throw ("Frozen exact-v9 corpus row count drifted: {0}" -f $exactCorpus.Count) }

$modes = @('AMBIENT-UNSET-CONTROL','TIERING-OFF','TIERING-ON-PGO-OFF','TIERING-ON-PGO-ON')
$expectedEnv = @{
    'AMBIENT-UNSET-CONTROL' = @('UNSET','UNSET','UNSET','UNSET','UNSET')
    'TIERING-OFF' = @('0','0','0','0','1')
    'TIERING-ON-PGO-OFF' = @('1','0','1','0','1')
    'TIERING-ON-PGO-ON' = @('1','1','1','0','1')
}
$envNames = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')

# Frozen counterbalanced execution sequence. First four blocks form a Latin-square rotation;
# block five adds the fifth process required by the planning contract.
$sequenceByKey = @{
    'AMBIENT-UNSET-CONTROL|1' = 1;  'TIERING-OFF|1' = 2;  'TIERING-ON-PGO-OFF|1' = 3;  'TIERING-ON-PGO-ON|1' = 4
    'TIERING-OFF|2' = 5;  'TIERING-ON-PGO-OFF|2' = 6;  'TIERING-ON-PGO-ON|2' = 7;  'AMBIENT-UNSET-CONTROL|2' = 8
    'TIERING-ON-PGO-OFF|3' = 9;  'TIERING-ON-PGO-ON|3' = 10; 'AMBIENT-UNSET-CONTROL|3' = 11; 'TIERING-OFF|3' = 12
    'TIERING-ON-PGO-ON|4' = 13; 'AMBIENT-UNSET-CONTROL|4' = 14; 'TIERING-OFF|4' = 15; 'TIERING-ON-PGO-OFF|4' = 16
    'AMBIENT-UNSET-CONTROL|5' = 17; 'TIERING-ON-PGO-OFF|5' = 18; 'TIERING-OFF|5' = 19; 'TIERING-ON-PGO-ON|5' = 20
}

$aggregateNames = @(
    '01-contract-and-provenance.txt',
    '02-runtime-mode-run-summary.csv',
    '03-tail-pass-window-summary.csv',
    '04-tail-row-path-summary.csv',
    '05-attribution-evidence-summary.txt',
    '06-attribution1-summary.txt'
)
foreach ($name in $aggregateNames) {
    $path = Join-Path $artifactRoot $name
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}

$runSummaryLines = New-Object 'System.Collections.Generic.List[string]'
$runSummaryLines.Add('mode_id,run_index,sequence_index,measured_calls,median_us,p95_us,max_us,median_allocated_bytes,unresolved_calls,calls_over_100us,calls_over_max_ceiling,candidate_allocated_bytes,harness_allocated_bytes,gc_gen0,gc_gen1,gc_gen2')

$passStats = @{}
$tailGroups = @{}
$modeStats = @{}
$totalCalls = 0

foreach ($mode in $modes) {
    $modeStats[$mode] = [pscustomobject]@{
        Processes = 0
        Calls = 0
        Over100 = 0
        OverMax = 0
        ProcessesWithOver100 = 0
        ProcessesWithOverMax = 0
        MaxUs = 0.0
        Gen0 = 0
        Gen1 = 0
        Gen2 = 0
    }

    for ($pass = 0; $pass -lt 64; $pass++) {
        $passStats[("{0}|{1}" -f $mode, $pass)] = [pscustomobject]@{
            Calls = 0
            Over100 = 0
            OverMax = 0
            MaxUs = 0.0
        }
    }

    for ($runIndex = 1; $runIndex -le 5; $runIndex++) {
        $processDir = Join-Path $artifactRoot ("mode-{0}\process-{1:D2}" -f $mode, $runIndex)
        $contractFile = Join-Path $processDir '01-process-contract.txt'
        $timingFile = Join-Path $processDir '02-exact-v9-call-timing.csv'
        $runtimeFile = Join-Path $processDir '03-runtime-context.txt'
        $summaryFile = Join-Path $processDir '04-process-summary.txt'
        foreach ($path in @($contractFile,$timingFile,$runtimeFile,$summaryFile)) { Require-File $path }

        $processContract = Read-KeyValueFile $contractFile
        $runtime = Read-KeyValueFile $runtimeFile
        $summary = Read-KeyValueFile $summaryFile
        $sequenceKey = "{0}|{1}" -f $mode, $runIndex
        if (-not $sequenceByKey.ContainsKey($sequenceKey)) { throw ("Missing execution sequence for {0}." -f $sequenceKey) }
        $sequenceIndex = [int]$sequenceByKey[$sequenceKey]

        Require-Equal $processContract['gate'] 'RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1' 'Process contract gate'
        Require-Equal $processContract['status'] 'PASS-PROCESS-CONTRACT' 'Process contract status'
        Require-Equal $processContract['mode-id'] $mode 'Process contract mode'
        Require-Equal ([int]$processContract['run-index']) $runIndex 'Process contract run'
        Require-Equal $processContract['candidate'] 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE' 'Process candidate'
        Require-Equal $processContract['candidate-family'] 'C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE' 'Process candidate family'
        Require-Equal $processContract['candidate-source-mutation'] 'False' 'Process candidate mutation'
        Require-Equal ([int]$processContract['exact-v9-rows']) 360 'Process exact-v9 rows'
        Require-Equal ([int]$processContract['warmup-passes']) 16 'Process warmup passes'
        Require-Equal ([int]$processContract['measured-passes']) 64 'Process measured passes'
        Require-Equal ([int]$processContract['measured-calls']) 23040 'Process contract measured calls'
        Require-Equal ([int]$processContract['rotation-stride']) 37 'Process rotation stride'
        Require-Equal (Inv (Parse-InvDouble $processContract['strict-max-us'] 'Process strict max')) (Inv $strictMax) 'Process strict max'
        Require-Equal $processContract['diagnostic-tail-floor-us'] '100' 'Process diagnostic floor'
        Require-Equal $processContract['diagnostic-tail-floor-is-qualification-threshold'] 'False' 'Process diagnostic threshold authority'
        Require-Equal $processContract['automatic-causal-promotion'] 'False' 'Process causal promotion'
        Require-Equal $processContract['automatic-selection-authorized'] 'False' 'Process automatic selection'
        Require-Equal $processContract['rp1c-selection-authorized'] 'False' 'Process RP1C authority'
        Require-Equal $processContract['production-repair-authorized'] 'False' 'Process production authority'

        Require-Equal $runtime['status'] 'PASS-RUNTIME-CONTEXT-CAPTURED' 'Runtime context status'
        Require-Equal $runtime['mode-id'] $mode 'Runtime mode'
        Require-Equal ([int]$runtime['run-index']) $runIndex 'Runtime run'
        for ($envIndex = 0; $envIndex -lt $envNames.Count; $envIndex++) {
            $name = $envNames[$envIndex]
            Require-Equal $runtime[$name] $expectedEnv[$mode][$envIndex] ("Runtime environment {0}" -f $name)
        }

        $wholeRegionAllocated = [long]$runtime['whole-measured-region-allocated-bytes']
        $runtimeCandidateAllocated = [long]$runtime['candidate-call-allocated-bytes-sum']
        $harnessAllocated = [long]$runtime['harness-allocated-bytes']
        if ($harnessAllocated -ne 0) {
            throw ("Measured harness allocated bytes in run {0}/{1}: {2}" -f $mode, $runIndex, $harnessAllocated)
        }
        Require-Equal $wholeRegionAllocated ($runtimeCandidateAllocated + $harnessAllocated) 'Runtime allocation accounting'
        $gen0 = [int]$runtime['gc-gen0-collections-during-measured-region']
        $gen1 = [int]$runtime['gc-gen1-collections-during-measured-region']
        $gen2 = [int]$runtime['gc-gen2-collections-during-measured-region']

        $rows = @(Import-Csv -LiteralPath $timingFile)
        if ($rows.Count -ne 23040) {
            throw ("Timing row count mismatch for {0} run {1}: {2}" -f $mode, $runIndex, $rows.Count)
        }

        [double[]]$elapsedValues = [System.Array]::CreateInstance([double], $rows.Count)
        [double[]]$allocatedValues = [System.Array]::CreateInstance([double], $rows.Count)
        $unresolved = 0
        $over100 = 0
        $overMax = 0
        $candidateAllocated = 0L
        $runMax = 0.0

        for ($index = 0; $index -lt $rows.Count; $index++) {
            $row = $rows[$index]
            $expectedPass = [int][Math]::Floor($index / 360.0)
            $offset = $index % 360
            $expectedRowIndex = (($expectedPass * 37) + $offset) % 360

            Require-Equal $row.mode_id $mode 'Timing mode'
            Require-Equal ([int]$row.run_index) $runIndex 'Timing run'
            Require-Equal ([int]$row.pass_index) $expectedPass 'Timing pass/order'
            Require-Equal ([int]$row.row_index) $expectedRowIndex 'Timing row/order'

            $expectedCorpusRow = $exactCorpus[$expectedRowIndex]
            Require-Equal $row.probe_id $expectedCorpusRow.probe_id 'Timing probe identity'
            Require-Equal ([long]$row.logical_step) ([long]$expectedCorpusRow.logical_step) 'Timing logical step'
            Require-Equal $row.node_id $expectedCorpusRow.node_id 'Timing node identity'

            $elapsed = Parse-InvDouble ([string]$row.elapsed_us) ("{0}/{1} elapsed" -f $mode, $runIndex)
            $allocated = [long]$row.allocated_bytes
            if ($elapsed -lt 0) { throw 'Negative elapsed time is invalid.' }
            if ($allocated -lt 0) { throw 'Negative allocated bytes are invalid.' }
            $elapsedValues[$index] = $elapsed
            $allocatedValues[$index] = [double]$allocated
            $candidateAllocated += $allocated
            if ($row.resolved -ne 'True') { $unresolved++ }

            $isOver100 = $elapsed -gt $tailFloor
            $isOverMax = $elapsed -gt $strictMax
            $expectedOver100 = if ($isOver100) { 'True' } else { 'False' }
            $expectedOverMax = if ($isOverMax) { 'True' } else { 'False' }
            Require-Equal $row.over_100us $expectedOver100 'Timing >100 flag'
            Require-Equal $row.over_max_ceiling $expectedOverMax 'Timing >max flag'

            $passStat = $passStats[("{0}|{1}" -f $mode, $expectedPass)]
            $passStat.Calls++
            if ($elapsed -gt $passStat.MaxUs) { $passStat.MaxUs = $elapsed }
            if ($isOver100) { $passStat.Over100++ }
            if ($isOverMax) { $passStat.OverMax++ }

            if ($isOver100) {
                $over100++
                $groupKey = "{0}|{1}|{2}|{3}|{4}|{5}" -f $mode, $row.row_index, $row.probe_id, $row.logical_step, $row.node_id, $row.resolution_path
                if (-not $tailGroups.ContainsKey($groupKey)) {
                    $tailGroups[$groupKey] = [pscustomobject]@{
                        Mode = $mode
                        RowIndex = [int]$row.row_index
                        ProbeId = [string]$row.probe_id
                        LogicalStep = [long]$row.logical_step
                        NodeId = [string]$row.node_id
                        ResolutionPath = [string]$row.resolution_path
                        Count = 0
                        StrictCount = 0
                        MaxUs = 0.0
                        Runs = @{}
                        RunPasses = @{}
                    }
                }
                $group = $tailGroups[$groupKey]
                $group.Count++
                if ($isOverMax) { $group.StrictCount++ }
                if ($elapsed -gt $group.MaxUs) { $group.MaxUs = $elapsed }
                $group.Runs[[string]$runIndex] = $true
                $group.RunPasses[("{0}:{1}" -f $runIndex, $expectedPass)] = $true
            }

            if ($isOverMax) { $overMax++ }
            if ($elapsed -gt $runMax) { $runMax = $elapsed }
        }

        [Array]::Sort($elapsedValues)
        [Array]::Sort($allocatedValues)
        $median = Median-Sorted $elapsedValues
        $p95 = P95-Sorted $elapsedValues
        $medianAllocated = Median-Sorted $allocatedValues

        Require-Equal $runtimeCandidateAllocated $candidateAllocated 'Runtime candidate allocation'
        Require-Equal $summary['status'] 'PASS-PROCESS-EVIDENCE-COMPLETE' 'Summary status'
        Require-Equal $summary['mode-id'] $mode 'Summary mode'
        Require-Equal ([int]$summary['run-index']) $runIndex 'Summary run'
        Require-Equal ([int]$summary['measured-calls']) 23040 'Summary measured calls'
        Require-Equal $summary['median-us'] (Inv $median) 'Summary median'
        Require-Equal $summary['p95-us'] (Inv $p95) 'Summary p95'
        Require-Equal $summary['max-us'] (Inv $runMax) 'Summary max'
        Require-Equal $summary['median-allocated-bytes'] (Inv $medianAllocated) 'Summary median allocation'
        Require-Equal ([int]$summary['unresolved-calls']) $unresolved 'Summary unresolved count'
        Require-Equal ([int]$summary['calls-over-100us']) $over100 'Summary >100 count'
        Require-Equal ([int]$summary['calls-over-max-ceiling']) $overMax 'Summary >max count'
        Require-Equal ([long]$summary['candidate-call-allocated-bytes-sum']) $candidateAllocated 'Summary candidate allocation'
        Require-Equal ([long]$summary['harness-allocated-bytes']) 0 'Summary harness allocation'
        Require-Equal ([int]$summary['gc-gen0-collections-during-measured-region']) $gen0 'Summary Gen0'
        Require-Equal ([int]$summary['gc-gen1-collections-during-measured-region']) $gen1 'Summary Gen1'
        Require-Equal ([int]$summary['gc-gen2-collections-during-measured-region']) $gen2 'Summary Gen2'
        Require-Equal (Inv (Parse-InvDouble $summary['strict-max-us'] 'Summary strict max')) (Inv $strictMax) 'Summary strict max'
        Require-Equal $summary['diagnostic-tail-floor-us'] '100' 'Summary diagnostic floor'
        Require-Equal $summary['automatic-causal-promotion'] 'False' 'Summary causal promotion'

        $runSummaryLines.Add(("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}" -f
            $mode,
            $runIndex,
            $sequenceIndex,
            $rows.Count,
            (Inv $median),
            (Inv $p95),
            (Inv $runMax),
            (Inv $medianAllocated),
            $unresolved,
            $over100,
            $overMax,
            $candidateAllocated,
            $harnessAllocated,
            $gen0,
            $gen1,
            $gen2))

        $stats = $modeStats[$mode]
        $stats.Processes++
        $stats.Calls += $rows.Count
        $stats.Over100 += $over100
        $stats.OverMax += $overMax
        if ($over100 -gt 0) { $stats.ProcessesWithOver100++ }
        if ($overMax -gt 0) { $stats.ProcessesWithOverMax++ }
        if ($runMax -gt $stats.MaxUs) { $stats.MaxUs = $runMax }
        $stats.Gen0 += $gen0
        $stats.Gen1 += $gen1
        $stats.Gen2 += $gen2
        $totalCalls += $rows.Count
    }
}

if ($totalCalls -ne 460800) { throw ("Total measured call count drifted: {0}" -f $totalCalls) }

$ascii = [System.Text.Encoding]::ASCII

$contractLines = @(
    'status=PASS-ATTRIBUTION-EVIDENCE-INTEGRITY',
    'gate=RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1',
    'revision=REV1-CONCLUSIVE-REVIEW-HARDENING',
    'processes=20',
    'runtime-modes=4',
    'total-measured-calls=460800',
    'execution-order=COUNTERBALANCED-BLOCKED-BY-RUN',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-qualification-threshold=False',
    'automatic-causal-promotion=False',
    'automatic-selection-authorized=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '01-contract-and-provenance.txt'), $contractLines, $ascii)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '02-runtime-mode-run-summary.csv'), $runSummaryLines, $ascii)

$passLines = New-Object 'System.Collections.Generic.List[string]'
$passLines.Add('mode_id,pass_index,calls,calls_over_100us,calls_over_max_ceiling,max_us')
foreach ($mode in $modes) {
    for ($pass = 0; $pass -lt 64; $pass++) {
        $stat = $passStats[("{0}|{1}" -f $mode, $pass)]
        $passLines.Add(("{0},{1},{2},{3},{4},{5}" -f $mode,$pass,$stat.Calls,$stat.Over100,$stat.OverMax,(Inv $stat.MaxUs)))
    }
}
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '03-tail-pass-window-summary.csv'), $passLines, $ascii)

$rowLines = New-Object 'System.Collections.Generic.List[string]'
$rowLines.Add('mode_id,row_index,probe_id,logical_step,node_id,resolution_path,calls_over_100us,calls_over_max_ceiling,max_us,run_indices,run_pass_pairs')
foreach ($entry in @($tailGroups.GetEnumerator() | Sort-Object Key)) {
    $group = $entry.Value
    $runValues = @($group.Runs.Keys | ForEach-Object { [int]$_ } | Sort-Object)
    $runText = [string]::Join('|', [string[]]@($runValues | ForEach-Object { $_.ToString([System.Globalization.CultureInfo]::InvariantCulture) }))
    $runPassText = [string]::Join('|', [string[]]@($group.RunPasses.Keys | Sort-Object))
    $rowLines.Add(("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}" -f
        $group.Mode,$group.RowIndex,$group.ProbeId,$group.LogicalStep,$group.NodeId,$group.ResolutionPath,
        $group.Count,$group.StrictCount,(Inv $group.MaxUs),$runText,$runPassText))
}
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '04-tail-row-path-summary.csv'), $rowLines, $ascii)

$evidenceLines = New-Object 'System.Collections.Generic.List[string]'
$evidenceLines.Add('status=PASS-COMPARATIVE-EVIDENCE-COMPLETE')
$evidenceLines.Add('classification=EVIDENCE-ONLY-NO-CAUSAL-PROMOTION')
$evidenceLines.Add('revision=REV1-CONCLUSIVE-REVIEW-HARDENING')
$evidenceLines.Add('execution-order=COUNTERBALANCED-BLOCKED-BY-RUN')
$evidenceLines.Add('strict-max-us=409.30666666666673')
$evidenceLines.Add('diagnostic-tail-floor-us=100')
foreach ($mode in $modes) {
    $stats = $modeStats[$mode]
    $evidenceLines.Add(("{0}: processes={1}; calls={2}; calls-over-100us={3}; calls-over-max={4}; processes-with-over-100us={5}; processes-with-over-max={6}; max-us={7}; gc-gen0={8}; gc-gen1={9}; gc-gen2={10}" -f
        $mode,$stats.Processes,$stats.Calls,$stats.Over100,$stats.OverMax,$stats.ProcessesWithOver100,$stats.ProcessesWithOverMax,(Inv $stats.MaxUs),$stats.Gen0,$stats.Gen1,$stats.Gen2))
}
$evidenceLines.Add('runtime-tiering-causality-proven=False')
$evidenceLines.Add('dynamic-pgo-causality-proven=False')
$evidenceLines.Add('scheduling-causality-proven=False')
$evidenceLines.Add('thermodynamic-slow-path-proven=False')
$evidenceLines.Add('automatic-selection-authorized=False')
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '05-attribution-evidence-summary.txt'), $evidenceLines, $ascii)

$summaryLines = @(
    'status=PASS-ATTRIBUTION1-EVIDENCE-COMPLETE',
    'classification=ATTRIBUTION-EVIDENCE-COMPLETE-NO-CAUSAL-PROMOTION',
    'revision=REV1-CONCLUSIVE-REVIEW-HARDENING',
    'total-runtime-modes=4',
    'total-processes=20',
    'total-measured-calls=460800',
    'execution-order=COUNTERBALANCED-BLOCKED-BY-RUN',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-qualification-threshold=False',
    'automatic-causal-promotion=False',
    'automatic-selection-authorized=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'next-action=RETURN-COMPLETE-ATTRIBUTION1-ARTIFACTS-FOR-ADJUDICATION'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '06-attribution1-summary.txt'), $summaryLines, $ascii)

$allFiles = @(Get-ChildItem -LiteralPath $artifactRoot -File -Recurse)
if ($allFiles.Count -ne 86) {
    throw ("Attribution evidence tree must contain exactly 86 files; found {0}." -f $allFiles.Count)
}

Write-Host 'Exact-v9 Tail Attribution 1 REV1 evidence adjudication: PASS-COMPLETE'
Write-Host 'Classification: ATTRIBUTION-EVIDENCE-COMPLETE-NO-CAUSAL-PROMOTION'
