$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}

function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}

function Require-Text([string]$Path, [string]$Needle) {
    $text = Read-Utf8Text $Path
    if (-not $text.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

function Require-NormalizedTextSha256([string]$Path, [string]$Expected, [string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $bytes = $utf8NoBom.GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha256.ComputeHash($bytes)
    }
    finally {
        $sha256.Dispose()
    }
    $actual = ([System.BitConverter]::ToString($hashBytes)).Replace("-", "").ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label, $actual, $Expected)
    }
}

function Parse-InvDouble([string]$Text, [string]$Label) {
    $value = 0.0
    $style = [System.Globalization.NumberStyles]::Float -bor [System.Globalization.NumberStyles]::AllowThousands
    if (-not [double]::TryParse($Text, $style, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw ("Invalid invariant double for {0}: {1}" -f $Label, $Text)
    }
    return $value
}

function Require-Near([double]$Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    if ([Math]::Abs($Actual - $Expected) -gt $Tolerance) {
        throw ("{0} drifted: actual={1}; expected={2}; tolerance={3}" -f $Label, $Actual, $Expected, $Tolerance)
    }
}

function Require-False([object]$Value, [string]$Label) {
    if ($Value -ne $false) { throw ("{0} must be false." -f $Label) }
}

function Require-True([object]$Value, [string]$Label) {
    if ($Value -ne $true) { throw ("{0} must be true." -f $Label) }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1-contract.json'
$planPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION_PLANNING1.md'
$reviewPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION_PLANNING1_PREEXECUTION_REVIEW.md'
$adrPath = Join-Path $repoRoot 'docs\adr\0199-attribute-rare-exact-v9-wall-clock-tail-before-rp1c-selection-or-candidate-mutation.md'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactCorpusPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$fdpcRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_FullDomainPerformanceConfirmation1_Artifacts'
$fdpcSummaryPath = Join-Path $fdpcRoot '05-rp1c-c4-full-domain-performance-confirmation1-summary.txt'
$fdpcRunSummaryPath = Join-Path $fdpcRoot '02-cross-process-run-summary.csv'
$fdpcAdjDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_RETURNED_EVIDENCE_ADJUDICATION.md'

foreach ($path in @($contractPath, $planPath, $reviewPath, $adrPath, $c4Path, $exactCorpusPath, $fdpcSummaryPath, $fdpcRunSummaryPath, $fdpcAdjDoc)) {
    Require-File $path
}

$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1-v1') { throw 'Unexpected planning contract schema.' }
if ($contract.gate -ne 'RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION-PLANNING1') { throw 'Unexpected planning gate id.' }
if ($contract.status -ne 'CANDIDATE-PLANNING-ONLY') { throw 'Planning contract status drifted.' }

$expectedHashes = @{
    fdpc1_returned_adjudication_doc = '6DA20524DAD8D1AFBBCFC0646049839B4FE862B4A46D95F4266BB315DB19C04D'
    fdpc1_returned_summary = '1E93B9C678C03CE98F00816F47E552044680E7DA6A35DA6DC4F144B6BBCF621D'
    fdpc1_cross_process_run_summary = '2DE372394E6ABD3458AD74A5484A2A7014E919C7F069B0BB7E3EF1628E45CDD3'
    c4_candidate = '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6'
    rp1a_exact_v9_corpus = '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E'
}

Require-NormalizedTextSha256 $fdpcAdjDoc $expectedHashes.fdpc1_returned_adjudication_doc 'FDPC1 returned adjudication document'
Require-NormalizedTextSha256 $fdpcSummaryPath $expectedHashes.fdpc1_returned_summary 'FDPC1 returned summary'
Require-NormalizedTextSha256 $fdpcRunSummaryPath $expectedHashes.fdpc1_cross_process_run_summary 'FDPC1 cross-process run summary'
Require-NormalizedTextSha256 $c4Path $expectedHashes.c4_candidate 'Immutable C4 candidate'
Require-NormalizedTextSha256 $exactCorpusPath $expectedHashes.rp1a_exact_v9_corpus 'Frozen exact-v9 corpus'

foreach ($name in $expectedHashes.Keys) {
    $contractValue = [string]$contract.normalized_lf_sha256.$name
    if ($contractValue.ToUpperInvariant() -ne $expectedHashes[$name]) {
        throw ("Contract normalized hash drifted for {0}." -f $name)
    }
}
if ($contract.normalized_lf_sha256.hash_mode -ne 'UTF8-TEXT-NORMALIZED-LF') { throw 'Hash mode drifted.' }

Require-Text $fdpcSummaryPath 'classification=C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED'
Require-Text $fdpcSummaryPath 'rp1c-selection-authorized=False'
Require-Text $fdpcAdjDoc 'RETURNED-FDPC1-EVIDENCE-ADJUDICATION=PASS'
Require-Text $fdpcAdjDoc 'runtime/tiering attribution hypothesis'

$allFdpcFiles = @(Get-ChildItem -LiteralPath $fdpcRoot -File -Recurse)
if ($allFdpcFiles.Count -ne 30) { throw ("Frozen FDPC1 tree must contain exactly 30 files; found {0}." -f $allFdpcFiles.Count) }

$runRows = @(Import-Csv -LiteralPath $fdpcRunSummaryPath)
if ($runRows.Count -ne 5) { throw 'FDPC1 cross-process summary must contain five runs.' }
$failRuns = @()
$totalCalls = 0
$totalExact = 0
$totalSeam = 0
foreach ($row in $runRows) {
    $runIndex = [int]$row.run_index
    $exactCalls = [int]$row.exact_v9_calls
    $seamCalls = [int]$row.seam_calls
    $totalExact += $exactCalls
    $totalSeam += $seamCalls
    $totalCalls += ($exactCalls + $seamCalls)
    if ($exactCalls -ne 23040 -or $seamCalls -ne 20480) { throw ("FDPC1 run {0} call count drifted." -f $runIndex) }
    if ([int]$row.exact_v9_unresolved -ne 0 -or [int]$row.seam_unresolved -ne 0) { throw ("FDPC1 run {0} contains unresolved calls." -f $runIndex) }
    if ([long]$row.harness_allocated_bytes -ne 0) { throw ("FDPC1 run {0} harness allocation drifted." -f $runIndex) }
    if ([int]$row.seam_calls_over_max_ceiling -ne 0) { throw ("FDPC1 run {0} seam exceedance drifted." -f $runIndex) }
    if ([string]$row.strict_corrected_predicate_met -eq 'False') { $failRuns += $runIndex }
}
if ($totalCalls -ne 217600 -or $totalExact -ne 115200 -or $totalSeam -ne 102400) { throw 'Frozen FDPC1 aggregate call counts drifted.' }
if ($failRuns.Count -ne 2 -or $failRuns[0] -ne 4 -or $failRuns[1] -ne 5) { throw 'FDPC1 failing run set must remain exactly runs 4 and 5.' }

$tailRows = @()
$strictRows = @()
$strictMax = [double]$contract.prerequisite.strict_max_us
$tailFloor = [double]$contract.future_attribution_gate.diagnostic_tail_floor_us
for ($run = 1; $run -le 5; $run++) {
    $timingPath = Join-Path $fdpcRoot ("process-{0:D2}\02-exact-v9-call-timing.csv" -f $run)
    Require-File $timingPath
    $rows = @(Import-Csv -LiteralPath $timingPath)
    if ($rows.Count -ne 23040) { throw ("FDPC1 process {0} exact-v9 row count drifted." -f $run) }
    foreach ($row in $rows) {
        $elapsed = Parse-InvDouble ([string]$row.elapsed_us) ("FDPC1 run {0} elapsed_us" -f $run)
        if ($elapsed -gt $tailFloor) { $tailRows += $row }
        if ($elapsed -gt $strictMax) { $strictRows += $row }
    }
    $runtimePath = Join-Path $fdpcRoot ("process-{0:D2}\04-runtime-context.txt" -f $run)
    Require-Text $runtimePath 'harness-allocated-bytes=0'
    Require-Text $runtimePath 'gc-gen0-collections-during-measured-region=0'
    Require-Text $runtimePath 'gc-gen1-collections-during-measured-region=0'
    Require-Text $runtimePath 'gc-gen2-collections-during-measured-region=0'
}

if ($tailRows.Count -ne 5) { throw ("Expected exactly five exact-v9 calls above diagnostic 100 us; found {0}." -f $tailRows.Count) }
if ($strictRows.Count -ne 2) { throw ("Expected exactly two exact-v9 strict exceedances; found {0}." -f $strictRows.Count) }

$tailPasses = @($tailRows | ForEach-Object { [int]$_.pass_index } | Sort-Object)
$expectedPasses = @(15,17,17,18,18)
for ($i = 0; $i -lt $expectedPasses.Count; $i++) {
    if ($tailPasses[$i] -ne $expectedPasses[$i]) { throw 'Returned >100 us pass-position signature drifted.' }
}
foreach ($row in $tailRows) {
    if ([string]$row.resolution_path -ne 'C2-MIXTURE-PREFIX') { throw 'Every returned >100 us exact-v9 call must remain on C2-MIXTURE-PREFIX.' }
    if ([long]$row.allocated_bytes -ne 0) { throw 'Returned >100 us exact-v9 call unexpectedly allocates.' }
}
$strictValues = @($strictRows | ForEach-Object { Parse-InvDouble ([string]$_.elapsed_us) 'strict exceedance elapsed' } | Sort-Object -Descending)
Require-Near $strictValues[0] 1027.1 1.0e-12 'FDPC1 largest strict exceedance'
Require-Near $strictValues[1] 796.1 1.0e-12 'FDPC1 second strict exceedance'

Require-True $contract.prerequisite.c4_candidate_immutable 'C4 immutable prerequisite'
Require-True $contract.prerequisite.exact_v9_corpus_immutable 'Exact-v9 immutable prerequisite'
Require-False $contract.prerequisite.rp1c_selection_authorized 'RP1C selection prerequisite'
Require-False $contract.planning_hypothesis.thermodynamic_slow_path_claimed 'Thermodynamic slow path claim'
Require-False $contract.planning_hypothesis.runtime_tiering_claimed 'Runtime tiering claim'
Require-False $contract.planning_hypothesis.threshold_change_allowed 'Threshold change planning hypothesis'
Require-False $contract.planning_hypothesis.candidate_mutation_allowed 'Candidate mutation planning hypothesis'

$future = $contract.future_attribution_gate
if ($future.id -ne 'RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1') { throw 'Unexpected future attribution gate id.' }
Require-True $future.evidence_only 'Future gate evidence-only'
Require-False $future.automatic_causal_promotion 'Automatic causal promotion'
Require-False $future.automatic_selection_authorized 'Automatic selection authorization'
Require-True $future.exact_v9_only 'Exact-v9-only scope'
Require-False $future.seam_remeasurement_required 'Seam remeasurement requirement'
if ([int]$future.fresh_processes_per_mode -ne 5 -or [int]$future.mode_count -ne 4 -or [int]$future.total_fresh_processes -ne 20) { throw 'Future runtime-mode process contract drifted.' }
if ([int]$future.exact_v9_rows -ne 360 -or [int]$future.warmup_passes -ne 16 -or [int]$future.measured_passes -ne 64) { throw 'Future exact-v9 protocol drifted.' }
if ([int]$future.measured_calls_per_process -ne 23040 -or [int]$future.total_measured_calls -ne 460800) { throw 'Future exact-v9 call count drifted.' }
Require-Near ([double]$future.strict_max_us) 409.30666666666673 1.0e-12 'Future strict max'
Require-Near ([double]$future.diagnostic_tail_floor_us) 100.0 1.0e-12 'Diagnostic tail floor'
Require-False $future.diagnostic_tail_floor_changes_qualification_threshold 'Diagnostic floor threshold authority'
Require-True $future.allocation_neutral_harness_required 'Allocation-neutral harness'
Require-True $future.returned_evidence_adjudication_required 'Returned attribution adjudication requirement'

$expectedEnv = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
$actualEnv = @($contract.caller_environment_preflight.required_unset)
if ($actualEnv.Count -ne $expectedEnv.Count) { throw 'Caller environment preflight variable count drifted.' }
for ($i = 0; $i -lt $expectedEnv.Count; $i++) {
    if ([string]$actualEnv[$i] -ne $expectedEnv[$i]) { throw 'Caller environment preflight variable ordering/identity drifted.' }
}
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Parent environment fail-closed rule'

$modes = @($contract.runtime_modes)
if ($modes.Count -ne 4) { throw 'Expected exactly four runtime modes.' }
$modeIds = @($modes | ForEach-Object { [string]$_.id })
$expectedModeIds = @('AMBIENT-UNSET-CONTROL','TIERING-OFF','TIERING-ON-PGO-OFF','TIERING-ON-PGO-ON')
for ($i = 0; $i -lt $expectedModeIds.Count; $i++) {
    if ($modeIds[$i] -ne $expectedModeIds[$i]) { throw 'Runtime mode ordering/identity drifted.' }
    if ([int]$modes[$i].processes -ne 5) { throw 'Each runtime mode must use five fresh processes.' }
}
foreach ($name in $expectedEnv) {
    if ($null -ne $modes[0].environment.$name) { throw ("Ambient control must leave {0} unset." -f $name) }
}
$expectedModeValues = @(
    @('0','0','0','0','1'),
    @('1','0','1','0','1'),
    @('1','1','1','0','1')
)
for ($modeOffset = 0; $modeOffset -lt 3; $modeOffset++) {
    $mode = $modes[$modeOffset + 1]
    for ($envIndex = 0; $envIndex -lt $expectedEnv.Count; $envIndex++) {
        $actual = [string]$mode.environment.($expectedEnv[$envIndex])
        $expected = [string]$expectedModeValues[$modeOffset][$envIndex]
        if ($actual -ne $expected) { throw ("Runtime mode {0} value drifted for {1}: actual={2}; expected={3}" -f $mode.id, $expectedEnv[$envIndex], $actual, $expected) }
    }
}

if ([int]$contract.planned_evidence.process_directory_count -ne 20 -or [int]$contract.planned_evidence.files_per_process -ne 4 -or [int]$contract.planned_evidence.aggregate_file_count -ne 6 -or [int]$contract.planned_evidence.total_required_files -ne 86) { throw 'Future evidence tree shape drifted.' }
if (@($contract.planned_evidence.process_files).Count -ne 4 -or @($contract.planned_evidence.aggregate_files).Count -ne 6) { throw 'Future evidence file-name count drifted.' }

$futureTest = $contract.future_test_contract
if ($futureTest.file -ne 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests.cs') { throw 'Future test file identity drifted.' }
if ($futureTest.class -ne 'M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests') { throw 'Future test class identity drifted.' }
if ($futureTest.method -ne 'Rp1cC4ExactV9TailAttribution1_MeasuresOneFreshRuntimeModeProcess') { throw 'Future test method identity drifted.' }
Require-True $futureTest.explicit 'Future attribution test explicit flag'
if ($futureTest.parallel -ne 'none' -or $futureTest.configuration -ne 'Release') { throw 'Future focused execution configuration drifted.' }
if ($futureTest.runner -ne 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1.cmd') { throw 'Future attribution runner identity drifted.' }

Require-False $contract.authority.attribution_gate_implementation_authorized_now 'Attribution implementation authorization now'
Require-True $contract.authority.attribution_gate_implementation_authorizable_after_returned_planning_adjudication 'Attribution implementation post-adjudication eligibility'
Require-False $contract.authority.rp1c_selection_authorized 'RP1C selection authority'
Require-False $contract.authority.production_repair_authorized 'Production repair authority'
Require-False $contract.authority.threshold_change_authorized 'Threshold change authority'
Require-False $contract.authority.exact_v9_change_authorized 'Exact-v9 authority'
Require-False $contract.authority.vr3_authorized 'VR3 authority'
Require-False $contract.authority.p3_r1_authorized 'P3-R1 authority'
Require-False $contract.authority.second_replacement_long_authorized 'Second replacement-long authority'
if ($contract.authority.next_action_after_local_planning_pass -ne 'RETURN-COMPLETE-TAIL-ATTRIBUTION-PLANNING-ARTIFACTS-FOR-ADJUDICATION') { throw 'Next action after planning pass drifted.' }

Require-Text $planPath '100 us'
Require-Text $planPath 'not a new acceptance threshold'
Require-Text $planPath 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN'
Require-Text $planPath '4 modes x 5 fresh processes = 20 fresh processes'
Require-Text $planPath '20 x 23,040 = 460,800'
Require-Text $planPath 'No automatic causal verdict'
Require-Text $reviewPath 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $reviewPath 'Windows PowerShell compatibility'
Require-Text $adrPath 'evidence-only and cannot automatically prove causality or authorize selection'

$futureTestPath = Join-Path $repoRoot $contract.future_test_contract.file
if (Test-Path -LiteralPath $futureTestPath -PathType Leaf) { throw 'Future attribution test is already implemented; planning package must remain planning-only.' }
$futureRunnerPath = Join-Path $repoRoot $contract.future_test_contract.runner
if (Test-Path -LiteralPath $futureRunnerPath -PathType Leaf) { throw 'Future attribution runner is already implemented; planning package must remain planning-only.' }

$artifactDir = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir | Out-Null
$ascii = [System.Text.Encoding]::ASCII

$contractLines = @(
    'status=PASS-AS-AUTHORED',
    'gate=RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION-PLANNING1',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1-v1',
    'fdpc1-returned-adjudication=PASS',
    'fdpc1-classification=C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED',
    'fdpc1-exact-v9-strict-exceedances=2',
    'fdpc1-exact-v9-calls-over-100us=5',
    'fdpc1-tail-passes=15|17|17|18|18',
    'fdpc1-tail-resolution-path=C2-MIXTURE-PREFIX',
    'runtime-tiering-causality-proven=False',
    'future-attribution-gate=RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1',
    'future-runtime-modes=4',
    'future-processes-per-mode=5',
    'future-total-processes=20',
    'future-total-measured-calls=460800',
    'future-required-files=86',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-qualification-threshold=False',
    'attribution-gate-implementation-authorized-now=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), $contractLines, $ascii)

$summaryLines = @(
    'status=PASS-TAIL-ATTRIBUTION-PLANNING-CONTRACT-FROZEN',
    'classification=PLANNING-ONLY-NO-CAUSAL-PROMOTION',
    'open-owner=RARE-EXACT-V9-WALL-CLOCK-TAIL',
    'frozen-tail-path=C2-MIXTURE-PREFIX',
    'frozen-tail-pass-signature=15|17|17|18|18',
    'frozen-strict-exceedances=2',
    'seam-remeasurement-required=False',
    'c4-mutation-authorized=False',
    'runtime-tiering-causality-proven=False',
    'caller-environment-must-be-clean=True',
    'automatic-causal-promotion=False',
    'automatic-selection-authorized=False',
    'next-action=RETURN-COMPLETE-TAIL-ATTRIBUTION-PLANNING-ARTIFACTS-FOR-ADJUDICATION'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-tail-attribution-plan-summary.txt'), $summaryLines, $ascii)

$modeLines = @(
    'mode_id,processes,DOTNET_TieredCompilation,DOTNET_TieredPGO,DOTNET_TC_QuickJit,DOTNET_TC_QuickJitForLoops,DOTNET_ReadyToRun,purpose',
    'AMBIENT-UNSET-CONTROL,5,UNSET,UNSET,UNSET,UNSET,UNSET,REPRODUCE-FDPC1-STYLE-AMBIENT-RUNTIME',
    'TIERING-OFF,5,0,0,0,0,1,TEST-TIERED-COMPILATION-SENSITIVITY',
    'TIERING-ON-PGO-OFF,5,1,0,1,0,1,SEPARATE-DYNAMIC-PGO-FROM-TIERING',
    'TIERING-ON-PGO-ON,5,1,1,1,0,1,EXPLICIT-TIERING-PGO-COMPARATOR'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-runtime-mode-matrix.csv'), $modeLines, $ascii)

$reviewLines = @(
    'status=STATIC-PREEXECUTION-REVIEW-PASS',
    'finding-1=FDPC1-NEGATIVE-RESULT-OWNED-BY-TWO-RARE-EXACT-V9-SINGLE-CALL-MAXIMA',
    'finding-2=FIVE-GT-100US-CALLS-AT-PASSES-15-17-17-18-18-ALL-C2-MIXTURE-PREFIX',
    'finding-3=NO-MEASURED-REGION-GC-AND-TAIL-CALL-ALLOCATION-ZERO',
    'finding-4=RUNTIME-TIERING-HYPOTHESIS-JUSTIFIED-BUT-NOT-PROVEN',
    'finding-5=CALLER-DOTNET-COMPILATION-ENVIRONMENT-MUST-FAIL-CLOSED-IF-PRESET',
    'finding-6=FOUR-MODE-TWENTY-PROCESS-EXACT-V9-ONLY-EVIDENCE-GATE',
    'finding-7=100US-DIAGNOSTIC-FLOOR-DOES-NOT-CHANGE-409.30666666666673US-CEILING',
    'finding-8=NO-AUTOMATIC-CAUSAL-OR-SELECTION-PROMOTION',
    'windows-powershell-compatibility=PASS-AS-AUTHORED',
    'future-implementation-contained=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), $reviewLines, $ascii)

$writtenArtifacts = @(Get-ChildItem -LiteralPath $artifactDir -File)
if ($writtenArtifacts.Count -ne 4) { throw ("Planning artifact count must be exactly 4; found {0}." -f $writtenArtifacts.Count) }

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 EXACT-V9 TAIL ATTRIBUTION PLANNING 1'
Write-Host '============================================================'
Write-Host 'Planning only on validated returned FDPC1 evidence.'
Write-Host 'No attribution implementation, RP1C selection, production repair, threshold change,'
Write-Host 'exact-v9 change, VR3, P3-R1 or second replacement-long authorization.'
Write-Host ''
Write-Host 'Exact-v9 Tail Attribution Planning 1 static audit: PASS-AS-AUTHORED'
Write-Host 'Artifacts: artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1'
