$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
}

function Read-KeyValueFile([string]$Path) {
    Require-File $Path
    $map = @{}
    foreach ($line in [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $index = $line.IndexOf('=')
        if ($index -le 0) { continue }
        $map[$line.Substring(0, $index)] = $line.Substring($index + 1)
    }
    return $map
}

function Require-Equal([object]$Actual, [object]$Expected, [string]$Label) {
    if ($Actual -ne $Expected) { throw ("{0} mismatch: actual={1}; expected={2}" -f $Label, $Actual, $Expected) }
}

function Parse-InvDouble([string]$Text, [string]$Label) {
    $value = 0.0
    $style = [System.Globalization.NumberStyles]::Float -bor [System.Globalization.NumberStyles]::AllowThousands
    if (-not [double]::TryParse($Text, $style, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw ("Invalid invariant double for {0}: {1}" -f $Label, $Text)
    }
    return $value
}

function Inv([double]$Value) { return $Value.ToString('R', [System.Globalization.CultureInfo]::InvariantCulture) }
function B([bool]$Value) { if ($Value) { return 'True' } return 'False' }

function Median-Sorted([double[]]$Sorted) {
    if ($Sorted.Length -eq 0) { return [double]::NaN }
    $middle = [int]($Sorted.Length / 2)
    if (($Sorted.Length % 2) -eq 0) { return 0.5 * ($Sorted[$middle - 1] + $Sorted[$middle]) }
    return $Sorted[$middle]
}

function P95-Sorted([double[]]$Sorted) {
    if ($Sorted.Length -eq 0) { return [double]::NaN }
    $index = [int][Math]::Ceiling(0.95 * $Sorted.Length) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Sorted.Length) { $index = $Sorted.Length - 1 }
    return $Sorted[$index]
}

$ascii = [System.Text.Encoding]::ASCII
$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1'
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1-contract.json'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
$hostPath = Join-Path $artifactRoot '02-execution-host-provenance.txt'
foreach ($path in @($contractPath,$exactPath,$performancePath,$hostPath)) { Require-File $path }
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) { throw 'A2 artifact root is missing.' }

$contract = ([System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $contractPath).Path, [System.Text.Encoding]::UTF8)) | ConvertFrom-Json
$strictMax = [double]$contract.protocol.strict_max_us
$tailFloor = [double]$contract.protocol.diagnostic_tail_floor_us

$baselineMap = @{}
foreach ($row in @(Import-Csv -LiteralPath $performancePath)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$row.metric)) { $baselineMap[[string]$row.metric] = [string]$row.value }
}
$baselineStrictMax = Parse-InvDouble $baselineMap['rp1b_candidate_resolve_max_ceiling_us'] 'Frozen performance max ceiling'
if ((Inv $baselineStrictMax) -ne (Inv $strictMax)) { throw 'Contract strict max does not match frozen performance baseline.' }

$exactCorpus = @(Import-Csv -LiteralPath $exactPath)
if ($exactCorpus.Count -ne 360) { throw ("Frozen exact-v9 corpus row count drifted: {0}" -f $exactCorpus.Count) }

$hostEvidence = Read-KeyValueFile $hostPath
Require-Equal $hostEvidence['status'] 'COMPLETE-HOST-PROVENANCE-CAPTURE' 'Host provenance status'
Require-Equal $hostEvidence['host-fingerprint-match'] 'True' 'Host fingerprint match'
Require-Equal $hostEvidence['active-power-scheme-stable'] 'True' 'Power scheme stability'
Require-Equal $hostEvidence['raw-system-uuid-retained'] 'False' 'Raw UUID privacy'
Require-Equal $hostEvidence['machine-name-retained'] 'False' 'Machine name privacy'
Require-Equal $hostEvidence['user-name-retained'] 'False' 'User name privacy'
$rootFingerprint = [string]$hostEvidence['start-host-fingerprint-sha256']
if ($rootFingerprint.Length -ne 64 -or $rootFingerprint -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Host fingerprint format is invalid.' }
Require-Equal $hostEvidence['end-host-fingerprint-sha256'] $rootFingerprint 'End host fingerprint'

$profiles = @('AMBIENT-UNSET','EXPLICIT-REFERENCE-ALL-ON')
$expectedEnv = @{
    'AMBIENT-UNSET' = @('UNSET','UNSET','UNSET','UNSET','UNSET')
    'EXPLICIT-REFERENCE-ALL-ON' = @('1','1','1','1','1')
}
$envNames = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')

$aggregateNames = @(
    '01-contract-and-provenance.txt',
    '03-host-consistency-audit.txt',
    '04-exact-v9-profile-summary.csv',
    '05-ordinary-suite-impact-summary.csv',
    '06-replay-determinism-impact-summary.csv',
    '07-non-vr2-performance-impact-summary.csv',
    '08-runtime-configuration-impact-assessment1-summary.txt'
)
foreach ($name in $aggregateNames) {
    $path = Join-Path $artifactRoot $name
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}

$profileStats = @{}
$strictGroups = @{}
$totalCalls = 0
foreach ($profile in $profiles) {
    $profileStats[$profile] = [pscustomobject]@{
        Processes=0; Calls=0; Over100=0; OverMax=0; ProcessesWithOverMax=0; MaxUs=0.0; Unresolved=0
        CandidateAllocated=0L; HarnessAllocated=0L; Gen0=0; Gen1=0; Gen2=0; ProcessMedians=@(); ProcessP95=@()
    }

    for ($runIndex = 1; $runIndex -le 5; $runIndex++) {
        $processDir = Join-Path $artifactRoot ("profile-{0}\process-{1:D2}" -f $profile, $runIndex)
        $contractFile = Join-Path $processDir '01-process-contract.txt'
        $timingFile = Join-Path $processDir '02-exact-v9-call-timing.csv'
        $runtimeFile = Join-Path $processDir '03-runtime-context.txt'
        $summaryFile = Join-Path $processDir '04-process-summary.txt'
        foreach ($path in @($contractFile,$timingFile,$runtimeFile,$summaryFile)) { Require-File $path }

        $processContract = Read-KeyValueFile $contractFile
        $runtime = Read-KeyValueFile $runtimeFile
        $summary = Read-KeyValueFile $summaryFile

        Require-Equal $processContract['gate'] 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1' 'Process gate'
        Require-Equal $processContract['status'] 'PASS-PROCESS-CONTRACT' 'Process contract status'
        Require-Equal $processContract['profile-id'] $profile 'Process profile'
        Require-Equal ([int]$processContract['run-index']) $runIndex 'Process run'
        Require-Equal $processContract['candidate'] 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE' 'Process candidate'
        Require-Equal $processContract['candidate-family'] 'C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE' 'Process candidate family'
        Require-Equal $processContract['candidate-source-mutation'] 'False' 'Process candidate mutation'
        Require-Equal ([int]$processContract['exact-v9-rows']) 360 'Process exact-v9 rows'
        Require-Equal ([int]$processContract['warmup-passes']) 16 'Process warmup passes'
        Require-Equal ([int]$processContract['measured-passes']) 64 'Process measured passes'
        Require-Equal ([int]$processContract['measured-calls']) 23040 'Process measured calls'
        Require-Equal ([int]$processContract['rotation-stride']) 37 'Process rotation stride'
        Require-Equal (Inv (Parse-InvDouble $processContract['strict-max-us'] 'Process strict max')) (Inv $strictMax) 'Process strict max'
        Require-Equal $processContract['diagnostic-tail-floor-us'] '100' 'Process diagnostic floor'
        Require-Equal $processContract['diagnostic-tail-floor-is-qualification-threshold'] 'False' 'Diagnostic threshold authority'
        Require-Equal $processContract['runtime-configuration-profile-evidence-only'] 'True' 'Profile evidence-only contract'
        Require-Equal $processContract['same-physical-host-required'] 'True' 'Same-host contract'
        Require-Equal $processContract['host-fingerprint-sha256'] $rootFingerprint 'Process contract host fingerprint'
        Require-Equal $processContract['automatic-selection-authorized'] 'False' 'Automatic selection authority'
        Require-Equal $processContract['rp1c-selection-authorized'] 'False' 'RP1C authority'
        Require-Equal $processContract['production-runtime-change-authorized'] 'False' 'Runtime authority'
        Require-Equal $processContract['production-repair-authorized'] 'False' 'Production repair authority'

        Require-Equal $runtime['status'] 'PASS-RUNTIME-CONTEXT-CAPTURED' 'Runtime status'
        Require-Equal $runtime['profile-id'] $profile 'Runtime profile'
        Require-Equal ([int]$runtime['run-index']) $runIndex 'Runtime run'
        Require-Equal $runtime['host-fingerprint-sha256'] $rootFingerprint 'Runtime host fingerprint'
        for ($envIndex = 0; $envIndex -lt $envNames.Count; $envIndex++) {
            Require-Equal $runtime[$envNames[$envIndex]] $expectedEnv[$profile][$envIndex] ("Runtime environment {0}" -f $envNames[$envIndex])
        }

        $wholeRegionAllocated = [long]$runtime['whole-measured-region-allocated-bytes']
        $runtimeCandidateAllocated = [long]$runtime['candidate-call-allocated-bytes-sum']
        $harnessAllocated = [long]$runtime['harness-allocated-bytes']
        if ($harnessAllocated -ne 0) { throw ("Measured harness allocated bytes in {0} run {1}: {2}" -f $profile, $runIndex, $harnessAllocated) }
        Require-Equal $wholeRegionAllocated ($runtimeCandidateAllocated + $harnessAllocated) 'Runtime allocation accounting'
        $gen0 = [int]$runtime['gc-gen0-collections-during-measured-region']
        $gen1 = [int]$runtime['gc-gen1-collections-during-measured-region']
        $gen2 = [int]$runtime['gc-gen2-collections-during-measured-region']

        $rows = @(Import-Csv -LiteralPath $timingFile)
        if ($rows.Count -ne 23040) { throw ("Timing row count mismatch for {0} run {1}: {2}" -f $profile, $runIndex, $rows.Count) }
        [double[]]$elapsedValues = [System.Array]::CreateInstance([double], $rows.Count)
        [double[]]$allocatedValues = [System.Array]::CreateInstance([double], $rows.Count)
        $unresolved = 0; $over100 = 0; $overMax = 0; $candidateAllocated = 0L; $runMax = 0.0

        for ($index = 0; $index -lt $rows.Count; $index++) {
            $row = $rows[$index]
            $expectedPass = [int][Math]::Floor($index / 360.0)
            $offset = $index % 360
            $expectedRowIndex = (($expectedPass * 37) + $offset) % 360
            Require-Equal $row.profile_id $profile 'Timing profile'
            Require-Equal ([int]$row.run_index) $runIndex 'Timing run'
            Require-Equal ([int]$row.pass_index) $expectedPass 'Timing pass/order'
            Require-Equal ([int]$row.row_index) $expectedRowIndex 'Timing row/order'
            $expectedCorpusRow = $exactCorpus[$expectedRowIndex]
            Require-Equal $row.probe_id $expectedCorpusRow.probe_id 'Timing probe identity'
            Require-Equal ([long]$row.logical_step) ([long]$expectedCorpusRow.logical_step) 'Timing logical step'
            Require-Equal $row.node_id $expectedCorpusRow.node_id 'Timing node identity'

            $elapsed = Parse-InvDouble ([string]$row.elapsed_us) ("{0}/{1} elapsed" -f $profile, $runIndex)
            $allocated = [long]$row.allocated_bytes
            if ($elapsed -lt 0 -or $allocated -lt 0) { throw 'Negative timing or allocation value is invalid.' }
            $elapsedValues[$index] = $elapsed
            $allocatedValues[$index] = [double]$allocated
            $candidateAllocated += $allocated
            if ($row.resolved -ne 'True') { $unresolved++ }
            $isOver100 = $elapsed -gt $tailFloor
            $isOverMax = $elapsed -gt $strictMax
            Require-Equal $row.over_100us $(if ($isOver100) { 'True' } else { 'False' }) 'Timing >100 flag'
            Require-Equal $row.over_max_ceiling $(if ($isOverMax) { 'True' } else { 'False' }) 'Timing >max flag'
            if ($isOver100) { $over100++ }
            if ($isOverMax) {
                $overMax++
                $groupKey = "{0}|{1}|{2}|{3}|{4}|{5}" -f $profile,$row.row_index,$row.probe_id,$row.logical_step,$row.node_id,$row.resolution_path
                if (-not $strictGroups.ContainsKey($groupKey)) {
                    $strictGroups[$groupKey] = [pscustomobject]@{ Profile=$profile; Runs=@{} }
                }
                $strictGroups[$groupKey].Runs[[string]$runIndex] = $true
            }
            if ($elapsed -gt $runMax) { $runMax = $elapsed }
        }

        [Array]::Sort($elapsedValues); [Array]::Sort($allocatedValues)
        $median = Median-Sorted $elapsedValues
        $p95 = P95-Sorted $elapsedValues
        $medianAllocated = Median-Sorted $allocatedValues
        Require-Equal $runtimeCandidateAllocated $candidateAllocated 'Runtime candidate allocation'
        Require-Equal $summary['status'] 'PASS-PROCESS-EVIDENCE-COMPLETE' 'Summary status'
        Require-Equal $summary['profile-id'] $profile 'Summary profile'
        Require-Equal ([int]$summary['run-index']) $runIndex 'Summary run'
        Require-Equal ([int]$summary['measured-calls']) 23040 'Summary calls'
        Require-Equal $summary['median-us'] (Inv $median) 'Summary median'
        Require-Equal $summary['p95-us'] (Inv $p95) 'Summary p95'
        Require-Equal $summary['max-us'] (Inv $runMax) 'Summary max'
        Require-Equal $summary['median-allocated-bytes'] (Inv $medianAllocated) 'Summary median allocation'
        Require-Equal ([int]$summary['unresolved-calls']) $unresolved 'Summary unresolved'
        Require-Equal ([int]$summary['calls-over-100us']) $over100 'Summary >100'
        Require-Equal ([int]$summary['calls-over-max-ceiling']) $overMax 'Summary >max'
        Require-Equal ([long]$summary['candidate-call-allocated-bytes-sum']) $candidateAllocated 'Summary candidate allocation'
        Require-Equal ([long]$summary['harness-allocated-bytes']) $harnessAllocated 'Summary harness allocation'
        Require-Equal $summary['host-fingerprint-sha256'] $rootFingerprint 'Summary host fingerprint'
        Require-Equal $summary['runtime-configuration-profile-evidence-only'] 'True' 'Summary evidence-only contract'
        Require-Equal $summary['automatic-selection-authorized'] 'False' 'Summary automatic selection'

        $stats = $profileStats[$profile]
        $stats.Processes++
        $stats.Calls += $rows.Count
        $stats.Over100 += $over100
        $stats.OverMax += $overMax
        if ($overMax -gt 0) { $stats.ProcessesWithOverMax++ }
        if ($runMax -gt $stats.MaxUs) { $stats.MaxUs = $runMax }
        $stats.Unresolved += $unresolved
        $stats.CandidateAllocated += $candidateAllocated
        $stats.HarnessAllocated += $harnessAllocated
        $stats.Gen0 += $gen0; $stats.Gen1 += $gen1; $stats.Gen2 += $gen2
        $stats.ProcessMedians += $median; $stats.ProcessP95 += $p95
        $totalCalls += $rows.Count
    }
}
if ($totalCalls -ne 230400) { throw ("Total exact-v9 measured calls drifted: {0}" -f $totalCalls) }

$reproStrict = @{ 'AMBIENT-UNSET'=0; 'EXPLICIT-REFERENCE-ALL-ON'=0 }
foreach ($group in $strictGroups.Values) {
    if ($group.Runs.Count -ge 2) { $reproStrict[$group.Profile]++ }
}

$exactLines = New-Object 'System.Collections.Generic.List[string]'
$exactLines.Add('profile_id,processes,measured_calls,median_of_process_medians_us,median_of_process_p95_us,max_us,unresolved_calls,calls_over_100us,calls_over_max_ceiling,processes_with_over_max,reproducible_strict_owner_groups,candidate_allocated_bytes,harness_allocated_bytes,gc_gen0,gc_gen1,gc_gen2')
foreach ($profile in $profiles) {
    $s = $profileStats[$profile]
    [double[]]$med = @($s.ProcessMedians); [double[]]$p95s = @($s.ProcessP95)
    [Array]::Sort($med); [Array]::Sort($p95s)
    $exactLines.Add(('{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}' -f
        $profile,$s.Processes,$s.Calls,(Inv (Median-Sorted $med)),(Inv (Median-Sorted $p95s)),(Inv $s.MaxUs),$s.Unresolved,$s.Over100,$s.OverMax,$s.ProcessesWithOverMax,$reproStrict[$profile],$s.CandidateAllocated,$s.HarnessAllocated,$s.Gen0,$s.Gen1,$s.Gen2))
}

function Validate-FamilySummary([string]$FamilyFolder, [string]$ProfileId) {
    $dir = Join-Path $artifactRoot ("{0}\profile-{1}" -f $FamilyFolder,$ProfileId)
    $contractFile = Join-Path $dir '01-run-contract.txt'
    $logFile = Join-Path $dir '02-run-log.txt'
    $summaryFile = Join-Path $dir '03-run-summary.txt'
    foreach ($p in @($contractFile,$logFile,$summaryFile)) { Require-File $p }
    $fc = Read-KeyValueFile $contractFile
    $fs = Read-KeyValueFile $summaryFile
    Require-Equal $fc['gate'] 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1' ("{0} gate" -f $FamilyFolder)
    Require-Equal $fc['profile-id'] $ProfileId ("{0} profile" -f $FamilyFolder)
    Require-Equal $fc['host-fingerprint-sha256'] $rootFingerprint ("{0} host" -f $FamilyFolder)
    Require-Equal $fs['status'] 'COMPLETE-ENGINEERING-EVIDENCE' ("{0} summary status" -f $FamilyFolder)
    Require-Equal $fs['profile-id'] $ProfileId ("{0} summary profile" -f $FamilyFolder)
    Require-Equal $fs['host-fingerprint-sha256'] $rootFingerprint ("{0} summary host" -f $FamilyFolder)
    Require-Equal $fs['summary-source'] 'XUNIT-XML-V2PLUS' ("{0} structured result source" -f $FamilyFolder)
    Require-Equal $fs['console-language-independent'] 'True' ("{0} localization contract" -f $FamilyFolder)
    return $fs
}

$ordinarySummaries = @{}
$replaySummaries = @{}
foreach ($profile in $profiles) {
    $ordinarySummaries[$profile] = Validate-FamilySummary 'ordinary' $profile
    $replaySummaries[$profile] = Validate-FamilySummary 'replay' $profile
}
if ([int]$ordinarySummaries['AMBIENT-UNSET']['executed'] -ne [int]$ordinarySummaries['EXPLICIT-REFERENCE-ALL-ON']['executed']) { throw 'Ordinary suite executed test count differs between profiles.' }
if ([int]$replaySummaries['AMBIENT-UNSET']['executed'] -ne [int]$replaySummaries['EXPLICIT-REFERENCE-ALL-ON']['executed']) { throw 'Replay/determinism executed test count differs between profiles.' }

$ordinaryLines = New-Object 'System.Collections.Generic.List[string]'
$ordinaryLines.Add('profile_id,reported_total,executed,passed,failed,skipped,errors,not_run,all_exit_codes_zero,engineering_pass')
$replayLines = New-Object 'System.Collections.Generic.List[string]'
$replayLines.Add('profile_id,reported_total,executed,passed,failed,skipped,errors,not_run,all_exit_codes_zero,engineering_pass')
foreach ($profile in $profiles) {
    $o = $ordinarySummaries[$profile]
    $ordinaryLines.Add(('{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}' -f $profile,$o['reported-total'],$o['executed'],$o['passed'],$o['failed'],$o['skipped'],$o['errors'],$o['not-run'],$o['all-project-exit-codes-zero'],$o['engineering-pass']))
    $r = $replaySummaries[$profile]
    $replayLines.Add(('{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}' -f $profile,$r['reported-total'],$r['executed'],$r['passed'],$r['failed'],$r['skipped'],$r['errors'],$r['not-run'],$r['all-class-exit-codes-zero'],$r['engineering-pass']))
}

$hotPathLines = New-Object 'System.Collections.Generic.List[string]'
$hotPathLines.Add('profile_id,run_index,reported_total,executed,passed,failed,skipped,errors,not_run,xunit_schema_version,accounting_mode,engineering_pass,source_evidence_produced,plant_definition_lookup_elapsed_ratio,plant_state_lookup_elapsed_ratio,observation_change_tracking_elapsed_ratio,critical_ratio_elapsed_ratio')
$hotPathPassCounts = @{ 'AMBIENT-UNSET'=0; 'EXPLICIT-REFERENCE-ALL-ON'=0 }
foreach ($profile in $profiles) {
    for ($runIndex = 1; $runIndex -le 3; $runIndex++) {
        $dir = Join-Path $artifactRoot ("non-vr2\profile-{0}\process-{1:D2}" -f $profile,$runIndex)
        $runFile = Join-Path $dir '01-run-contract.txt'
        $summaryFile = Join-Path $dir '02-hotpath-summary.txt'
        $metricsFile = Join-Path $dir '03-hotpath-metrics.csv'
        foreach ($p in @($runFile,$summaryFile,$metricsFile)) { Require-File $p }
        $run = Read-KeyValueFile $runFile
        Require-Equal $run['gate'] 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1' 'Hot-path gate'
        Require-Equal $run['profile-id'] $profile 'Hot-path profile'
        Require-Equal ([int]$run['run-index']) $runIndex 'Hot-path run index'
        Require-Equal $run['host-fingerprint-sha256'] $rootFingerprint 'Hot-path host fingerprint'
        if ([int]$run['test-executed'] -ne 1) { throw ("Hot-path executed test count drifted for {0}/{1}: {2}" -f $profile,$runIndex,$run['test-executed']) }
        $engineeringPass = $run['engineering-pass'] -eq 'True'
        if ($engineeringPass) { $hotPathPassCounts[$profile]++ }
        $summary = Read-KeyValueFile $summaryFile
        $sourceProduced = $summary['status'] -ne 'SOURCE-EVIDENCE-NOT-PRODUCED-DUE-ENGINEERING-FAILURE'
        $ratios = @{ plant_definition_lookup_elapsed_ticks=''; plant_state_lookup_elapsed_ticks=''; observation_change_tracking_elapsed_ticks=''; critical_ratio_elapsed_ticks='' }
        if ($sourceProduced) {
            foreach ($metric in @(Import-Csv -LiteralPath $metricsFile)) {
                if ($ratios.ContainsKey([string]$metric.metric)) { $ratios[[string]$metric.metric] = [string]$metric.ratio }
            }
        }
        $hotPathLines.Add(('{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}' -f
            $profile,$runIndex,$run['test-reported-total'],$run['test-executed'],$run['test-passed'],$run['test-failed'],$run['test-skipped'],$run['test-errors'],$run['test-not-run'],$run['test-xunit-schema-version'],$run['test-accounting-mode'],$run['engineering-pass'],(B $sourceProduced),$ratios['plant_definition_lookup_elapsed_ticks'],$ratios['plant_state_lookup_elapsed_ticks'],$ratios['observation_change_tracking_elapsed_ticks'],$ratios['critical_ratio_elapsed_ticks']))
    }
}

$contractLines = @(
    'status=PASS-A2-EVIDENCE-INTEGRITY',
    'gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1',
    'profiles=2',
    'exact-v9-processes=10',
    'exact-v9-measured-calls=230400',
    'ordinary-suite-profiles=2',
    'replay-determinism-profiles=2',
    'non-vr2-performance-processes=6',
    'required-files=78',
    ('host-fingerprint-sha256={0}' -f $rootFingerprint),
    'single-physical-host-required=True',
    'cross-host-evidence-mixing-authorized=False',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-threshold=False',
    'engineering-negative-is-infrastructure-red=False',
    'automatic-runtime-equivalence-promotion=False',
    'automatic-production-runtime-recommendation=False',
    'full-domain-performance-confirmation2-authorized=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'production-repair-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '01-contract-and-provenance.txt'), $contractLines, $ascii)

$hostAudit = @(
    'status=PASS-SINGLE-HOST-INTEGRITY',
    ('host-fingerprint-sha256={0}' -f $rootFingerprint),
    'start-end-fingerprint-match=True',
    'active-power-scheme-stable=True',
    'all-exact-v9-process-contracts-match-root-host=True',
    'all-exact-v9-runtime-contexts-match-root-host=True',
    'all-ordinary-family-contracts-match-root-host=True',
    'all-replay-family-contracts-match-root-host=True',
    'all-non-vr2-family-contracts-match-root-host=True',
    'raw-system-uuid-retained=False',
    'cross-host-evidence-mixing-detected=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '03-host-consistency-audit.txt'), $hostAudit, $ascii)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '04-exact-v9-profile-summary.csv'), $exactLines, $ascii)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '05-ordinary-suite-impact-summary.csv'), $ordinaryLines, $ascii)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '06-replay-determinism-impact-summary.csv'), $replayLines, $ascii)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '07-non-vr2-performance-impact-summary.csv'), $hotPathLines, $ascii)

$ambient = $profileStats['AMBIENT-UNSET']
$explicit = $profileStats['EXPLICIT-REFERENCE-ALL-ON']
[double[]]$ambientMed = @($ambient.ProcessMedians); [double[]]$explicitMed = @($explicit.ProcessMedians)
[double[]]$ambientP95 = @($ambient.ProcessP95); [double[]]$explicitP95 = @($explicit.ProcessP95)
[Array]::Sort($ambientMed); [Array]::Sort($explicitMed); [Array]::Sort($ambientP95); [Array]::Sort($explicitP95)
$summaryLines = @(
    'status=PASS-A2-EVIDENCE-COLLECTION-COMPLETE',
    'engineering-adjudication=NOT-AUTOMATICALLY-PERFORMED',
    'runtime-configuration-impact-assessment=RETURNED-EVIDENCE-REQUIRED',
    ('ambient-median-of-process-medians-us={0}' -f (Inv (Median-Sorted $ambientMed))),
    ('explicit-median-of-process-medians-us={0}' -f (Inv (Median-Sorted $explicitMed))),
    ('ambient-median-of-process-p95-us={0}' -f (Inv (Median-Sorted $ambientP95))),
    ('explicit-median-of-process-p95-us={0}' -f (Inv (Median-Sorted $explicitP95))),
    ('ambient-calls-over-100us={0}' -f $ambient.Over100),
    ('explicit-calls-over-100us={0}' -f $explicit.Over100),
    ('ambient-calls-over-max={0}' -f $ambient.OverMax),
    ('explicit-calls-over-max={0}' -f $explicit.OverMax),
    ('ambient-reproducible-strict-owner-groups={0}' -f $reproStrict['AMBIENT-UNSET']),
    ('explicit-reproducible-strict-owner-groups={0}' -f $reproStrict['EXPLICIT-REFERENCE-ALL-ON']),
    ('ambient-ordinary-engineering-pass={0}' -f $ordinarySummaries['AMBIENT-UNSET']['engineering-pass']),
    ('explicit-ordinary-engineering-pass={0}' -f $ordinarySummaries['EXPLICIT-REFERENCE-ALL-ON']['engineering-pass']),
    ('ambient-replay-engineering-pass={0}' -f $replaySummaries['AMBIENT-UNSET']['engineering-pass']),
    ('explicit-replay-engineering-pass={0}' -f $replaySummaries['EXPLICIT-REFERENCE-ALL-ON']['engineering-pass']),
    ('ambient-hotpath-passing-processes={0}' -f $hotPathPassCounts['AMBIENT-UNSET']),
    ('explicit-hotpath-passing-processes={0}' -f $hotPathPassCounts['EXPLICIT-REFERENCE-ALL-ON']),
    'ambient-effective-default-equivalence-auto-promoted=False',
    'explicit-reference-is-production-recommendation=False',
    'cross-host-absolute-timing-comparison-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'full-domain-performance-confirmation2-authorized=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'production-repair-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False',
    'next-action=RETURN-COMPLETE-A2-ARTIFACTS-FOR-ADJUDICATION'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactRoot '08-runtime-configuration-impact-assessment1-summary.txt'), $summaryLines, $ascii)

$files = @(Get-ChildItem -LiteralPath $artifactRoot -Recurse -File)
if ($files.Count -ne 78) { throw ("A2 final evidence file count mismatch: {0} (expected 78)." -f $files.Count) }
if (Test-Path -LiteralPath (Join-Path $artifactRoot '_work')) { throw 'A2 temporary work directory was not removed.' }

Write-Host 'Runtime Configuration Impact Assessment 1 evidence integrity adjudication: PASS'
Write-Host ('Artifacts: {0}' -f $artifactRoot)
