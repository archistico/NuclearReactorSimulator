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

function Normalized-TextSha256([string]$Path) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $bytes = $utf8NoBom.GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try { $hashBytes = $sha256.ComputeHash($bytes) }
    finally { $sha256.Dispose() }
    return ([System.BitConverter]::ToString($hashBytes)).Replace("-", "").ToUpperInvariant()
}

function Require-NormalizedTextSha256([string]$Path, [string]$Expected, [string]$Label) {
    $actual = Normalized-TextSha256 $Path
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label, $actual, $Expected)
    }
}

function Require-True([object]$Value, [string]$Label) {
    if ($Value -ne $true) { throw ("{0} must be true." -f $Label) }
}

function Require-False([object]$Value, [string]$Label) {
    if ($Value -ne $false) { throw ("{0} must be false." -f $Label) }
}

function Require-EnvValue([object]$Mode, [string]$Name, [object]$Expected, [string]$Label) {
    $actual = $Mode.environment.$Name
    if ($null -eq $Expected) {
        if ($null -ne $actual) { throw ("{0} must be null/unset." -f $Label) }
    }
    elseif ([string]$actual -ne [string]$Expected) {
        throw ("{0} drifted: actual={1}; expected={2}" -f $Label, $actual, $Expected)
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1-contract.json'
$testPath = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests.cs'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
$planningRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_ExactV9TailAttributionPlanning1_Artifacts'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1.cmd'
$adjudicatorPath = Join-Path $PSScriptRoot 'adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1.ps1'
$implementationDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION1.md'
$reviewDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION1_PREEXECUTION_REVIEW.md'

foreach ($path in @($contractPath,$testPath,$c4Path,$exactPath,$performancePath,$runnerPath,$adjudicatorPath,$implementationDoc,$reviewDoc)) {
    Require-File $path
}
foreach ($file in @('01-contract-and-provenance.txt','02-tail-attribution-plan-summary.txt','03-runtime-mode-matrix.csv','04-preexecution-review.txt')) {
    Require-File (Join-Path $planningRoot $file)
}

$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1-v2') { throw 'Unexpected attribution implementation contract schema.' }
if ($contract.status -ne 'CANDIDATE-EVIDENCE-ONLY-REV1') { throw 'Unexpected attribution implementation contract status.' }
if ($contract.revision -ne 'REV1-CONCLUSIVE-REVIEW-HARDENING') { throw 'Unexpected attribution implementation revision.' }
if ($contract.gate -ne 'RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1') { throw 'Unexpected attribution gate id.' }

$hashes = @{
    planning_artifact_01 = '977A6B5678EC918A1CCA373B3E938D34B2BE3EDD8411BF2178B8C044199FF535'
    planning_artifact_02 = '732AD55B65D9DBFB70CE169570FDDFA7E00D6B719AC85707C598CD4FCCB0B53E'
    planning_artifact_03 = '7F832A67F874F2768314EB14875BA14FFD87749E1676A33E8CF76CF6F1A4AE5F'
    planning_artifact_04 = '9AAC5C8452FB3887F4D5D68DD629E313935FAA6B09726797AA98ED15A6904F5D'
    c4_candidate = '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6'
    rp1a_exact_v9_corpus = '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E'
    rp1a_performance_baseline = '91776709A34142E9312A092F3B19EAFCF0FCA576D561F296CC9272FCDC4F7DEB'
    focused_test = '135D3EDE0F3793CBAE811D07C539245A1A54179D41ECD41B4414713E2427C146'
    runner = 'D01FDD880938A9A9F383DADC62A00EB50FFF90DFFC0AAEDCD75111852CD519BE'
    adjudicator = '581F48F8E05EBEED88E986D9142616F96CE1E0AFB09603D292668FE2546DB52A'
}

Require-NormalizedTextSha256 (Join-Path $planningRoot '01-contract-and-provenance.txt') $hashes.planning_artifact_01 'Returned planning artifact 01'
Require-NormalizedTextSha256 (Join-Path $planningRoot '02-tail-attribution-plan-summary.txt') $hashes.planning_artifact_02 'Returned planning artifact 02'
Require-NormalizedTextSha256 (Join-Path $planningRoot '03-runtime-mode-matrix.csv') $hashes.planning_artifact_03 'Returned planning artifact 03'
Require-NormalizedTextSha256 (Join-Path $planningRoot '04-preexecution-review.txt') $hashes.planning_artifact_04 'Returned planning artifact 04'
Require-NormalizedTextSha256 $c4Path $hashes.c4_candidate 'Immutable C4 candidate'
Require-NormalizedTextSha256 $exactPath $hashes.rp1a_exact_v9_corpus 'Frozen RP1A exact-v9 corpus'
Require-NormalizedTextSha256 $performancePath $hashes.rp1a_performance_baseline 'Frozen RP1A performance baseline'
Require-NormalizedTextSha256 $testPath $hashes.focused_test 'Focused attribution test'
Require-NormalizedTextSha256 $runnerPath $hashes.runner 'Attribution runner'
Require-NormalizedTextSha256 $adjudicatorPath $hashes.adjudicator 'Attribution adjudicator'

foreach ($name in $hashes.Keys) {
    $actual = [string]$contract.normalized_lf_sha256.$name
    if ($actual.ToUpperInvariant() -ne $hashes[$name]) { throw ("Contract hash drifted for {0}." -f $name) }
}
if ($contract.normalized_lf_sha256.hash_mode -ne 'UTF8-TEXT-NORMALIZED-LF') { throw 'Hash mode drifted.' }

Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'future-total-processes=20'
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'future-total-measured-calls=460800'
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'future-required-files=86'
Require-Text (Join-Path $planningRoot '02-tail-attribution-plan-summary.txt') 'runtime-tiering-causality-proven=False'
Require-Text (Join-Path $planningRoot '02-tail-attribution-plan-summary.txt') 'automatic-causal-promotion=False'
Require-Text (Join-Path $planningRoot '04-preexecution-review.txt') 'windows-powershell-compatibility=PASS-AS-AUTHORED'

Require-True $contract.prerequisite.c4_candidate_immutable 'C4 candidate immutability'
Require-True $contract.prerequisite.exact_v9_corpus_immutable 'Exact-v9 corpus immutability'
Require-True $contract.prerequisite.caller_runtime_environment_must_be_clean 'Clean caller environment requirement'
Require-False $contract.prerequisite.rp1c_selection_authorized 'RP1C selection prerequisite'

$protocol = $contract.protocol
Require-True $protocol.evidence_only 'Evidence-only protocol'
Require-True $protocol.exact_v9_only 'Exact-v9-only protocol'
if ([int]$protocol.mode_count -ne 4 -or [int]$protocol.fresh_processes_per_mode -ne 5 -or [int]$protocol.total_fresh_processes -ne 20) { throw 'Runtime process protocol drifted.' }
if ([int]$protocol.rows -ne 360 -or [int]$protocol.warmup_passes -ne 16 -or [int]$protocol.measured_passes -ne 64) { throw 'Exact-v9 process shape drifted.' }
if ([int]$protocol.measured_calls_per_process -ne 23040 -or [int]$protocol.total_measured_calls -ne 460800) { throw 'Measured-call count drifted.' }
if ([double]$protocol.strict_max_us -ne 409.30666666666673) { throw 'Strict max ceiling drifted.' }
if ([double]$protocol.diagnostic_tail_floor_us -ne 100.0) { throw 'Diagnostic tail floor drifted.' }
Require-False $protocol.diagnostic_tail_floor_is_qualification_threshold '100 us threshold authority'
Require-True $protocol.allocation_neutral_harness_required 'Allocation-neutral harness'
Require-False $protocol.negative_or_inconclusive_result_is_harness_failure 'Negative evidence xUnit policy'
Require-False $protocol.automatic_causal_promotion 'Automatic causal promotion'
Require-False $protocol.automatic_selection_authorized 'Automatic selection authorization'
if ($protocol.execution_order_strategy -ne 'COUNTERBALANCED-BLOCKED-BY-RUN') { throw 'Execution-order strategy drifted.' }
$expectedSchedule = @('A1','B1','C1','D1','B2','C2','D2','A2','C3','D3','A3','B3','D4','A4','B4','C4','A5','C5','B5','D5')
$schedule = @($protocol.execution_schedule)
if ($schedule.Count -ne $expectedSchedule.Count) { throw 'Execution schedule length drifted.' }
for ($i=0; $i -lt $expectedSchedule.Count; $i++) {
    if ([string]$schedule[$i] -ne $expectedSchedule[$i]) { throw ("Execution schedule drifted at index {0}." -f $i) }
}
Require-True $protocol.full_exact_matrix_identity_verified_by_adjudicator 'Full exact matrix integrity policy'
Require-True $protocol.frozen_performance_baseline_pinned 'Frozen performance baseline pin policy'
Require-True $contract.interpretation_limits.qjfl_zero_is_frozen_by_returned_planning 'QJFL=0 planning freeze must be explicit'
Require-True $contract.interpretation_limits.c2_mixture_prefix_target_contains_loop 'Loop-bearing C2 mixture-prefix limitation must be explicit'
Require-True $contract.interpretation_limits.qjfl_zero_can_reduce_dynamic_pgo_sensitivity_for_loop_methods 'QJFL/Dynamic-PGO sensitivity limitation must be explicit'
Require-False $contract.interpretation_limits.pgo_on_vs_off_null_result_excludes_dynamic_pgo 'Null PGO comparison exclusion policy'
Require-False $contract.interpretation_limits.runtime_mode_difference_proves_single_cause 'Single-cause auto-attribution policy'
Require-False $contract.interpretation_limits.low_level_jit_event_trace_collected 'Low-level JIT trace claim'
Require-True $contract.interpretation_limits.result_requires_returned_evidence_adjudication 'Returned-evidence adjudication requirement'

if ([int]$contract.evidence.process_directory_count -ne 20 -or [int]$contract.evidence.files_per_process -ne 4 -or [int]$contract.evidence.aggregate_file_count -ne 6 -or [int]$contract.evidence.total_required_files -ne 86) { throw 'Evidence-tree contract drifted.' }
Require-True $contract.evidence.run_summary_includes_sequence_index 'Run summary sequence-index policy'
Require-True $contract.evidence.tail_row_summary_includes_run_and_run_pass_pairs 'Tail row run provenance policy'

$expectedModes = @('AMBIENT-UNSET-CONTROL','TIERING-OFF','TIERING-ON-PGO-OFF','TIERING-ON-PGO-ON')
$modes = @($contract.runtime_modes)
if ($modes.Count -ne 4) { throw 'Expected four runtime modes.' }
for ($i=0; $i -lt 4; $i++) {
    if ([string]$modes[$i].id -ne $expectedModes[$i]) { throw 'Runtime mode identity/order drifted.' }
    if ([int]$modes[$i].processes -ne 5) { throw 'Each runtime mode must use five processes.' }
}
Require-EnvValue $modes[0] 'DOTNET_TieredCompilation' $null 'Ambient TieredCompilation'
Require-EnvValue $modes[0] 'DOTNET_TieredPGO' $null 'Ambient TieredPGO'
Require-EnvValue $modes[0] 'DOTNET_TC_QuickJit' $null 'Ambient QuickJit'
Require-EnvValue $modes[0] 'DOTNET_TC_QuickJitForLoops' $null 'Ambient QuickJitForLoops'
Require-EnvValue $modes[0] 'DOTNET_ReadyToRun' $null 'Ambient ReadyToRun'
Require-EnvValue $modes[1] 'DOTNET_TieredCompilation' '0' 'Tiering-off TieredCompilation'
Require-EnvValue $modes[1] 'DOTNET_TieredPGO' '0' 'Tiering-off TieredPGO'
Require-EnvValue $modes[1] 'DOTNET_TC_QuickJit' '0' 'Tiering-off QuickJit'
Require-EnvValue $modes[1] 'DOTNET_TC_QuickJitForLoops' '0' 'Tiering-off QuickJitForLoops'
Require-EnvValue $modes[1] 'DOTNET_ReadyToRun' '1' 'Tiering-off ReadyToRun'
Require-EnvValue $modes[2] 'DOTNET_TieredCompilation' '1' 'PGO-off TieredCompilation'
Require-EnvValue $modes[2] 'DOTNET_TieredPGO' '0' 'PGO-off TieredPGO'
Require-EnvValue $modes[2] 'DOTNET_TC_QuickJit' '1' 'PGO-off QuickJit'
Require-EnvValue $modes[2] 'DOTNET_TC_QuickJitForLoops' '0' 'PGO-off QuickJitForLoops'
Require-EnvValue $modes[2] 'DOTNET_ReadyToRun' '1' 'PGO-off ReadyToRun'
Require-EnvValue $modes[3] 'DOTNET_TieredCompilation' '1' 'PGO-on TieredCompilation'
Require-EnvValue $modes[3] 'DOTNET_TieredPGO' '1' 'PGO-on TieredPGO'
Require-EnvValue $modes[3] 'DOTNET_TC_QuickJit' '1' 'PGO-on QuickJit'
Require-EnvValue $modes[3] 'DOTNET_TC_QuickJitForLoops' '0' 'PGO-on QuickJitForLoops'
Require-EnvValue $modes[3] 'DOTNET_ReadyToRun' '1' 'PGO-on ReadyToRun'

$expectedEnv = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
$preflightEnv = @($contract.caller_environment_preflight.required_unset)
if ($preflightEnv.Count -ne 5) { throw 'Caller environment preflight count drifted.' }
for ($i=0; $i -lt 5; $i++) {
    if ([string]$preflightEnv[$i] -ne $expectedEnv[$i]) { throw 'Caller environment preflight identity/order drifted.' }
}
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Fail-closed caller environment policy'

$testText = Read-Utf8Text $testPath
foreach ($marker in @(
    '[Fact(Explicit = true)]',
    'Rp1cC4ExactV9TailAttribution1_MeasuresOneFreshRuntimeModeProcess',
    'AMBIENT-UNSET-CONTROL',
    'TIERING-OFF',
    'TIERING-ON-PGO-OFF',
    'TIERING-ON-PGO-ON',
    'DiagnosticTailFloorMicroseconds = 100d',
    'MeasuredPasses = 64',
    'WarmupPasses = 16',
    'strictMaximumMicroseconds',
    'process-id=',
    'server-gc=',
    'automatic-causal-promotion=False',
    'rp1c-selection-authorized=False'
)) {
    if (-not $testText.Contains($marker)) { throw ("Focused test marker missing: {0}" -f $marker) }
}
if ($testText.Contains('409.30666666666673d')) { throw 'Focused test must use the pinned frozen performance ceiling rather than duplicate a hard-coded max.' }
if ($testText.Contains('Assert.True(strict')) { throw 'Focused test must not convert negative performance evidence into xUnit failure.' }

$runnerText = Read-Utf8Text $runnerPath
foreach ($name in $expectedEnv) {
    if (-not $runnerText.Contains(("if defined {0}" -f $name))) { throw ("Runner caller-environment preflight missing: {0}" -f $name) }
}
foreach ($mode in $expectedModes) {
    if (-not $runnerText.Contains($mode)) { throw ("Runner runtime mode missing: {0}" -f $mode) }
}
foreach ($marker in @('Block/run 1: A B C D','Block/run 2: B C D A','Block/run 3: C D A B','Block/run 4: D A B C','Block/run 5: A C B D','--parallel none','--no-build')) {
    if (-not $runnerText.Contains($marker)) { throw ("Runner hardening marker missing: {0}" -f $marker) }
}

$adjudicatorText = Read-Utf8Text $adjudicatorPath
foreach ($marker in @('02-runtime-mode-run-summary.csv','03-tail-pass-window-summary.csv','04-tail-row-path-summary.csv','sequence_index','run_pass_pairs','Timing row/order','Timing probe identity','Runtime allocation accounting','automatic-causal-promotion=False','rp1c-selection-authorized=False')) {
    if (-not $adjudicatorText.Contains($marker)) { throw ("Adjudicator marker missing: {0}" -f $marker) }
}
if ($adjudicatorText -match '\$[A-Za-z_][A-Za-z0-9_]*:') { throw 'Adjudicator contains a PowerShell variable-colon interpolation hazard.' }
if ($adjudicatorText.Contains('StringComparison')) { throw 'Adjudicator must remain compatible with Windows PowerShell 5.1.' }

Require-True $contract.authority.attribution_gate_implementation_authorized 'Attribution implementation authority'
Require-True $contract.authority.attribution_result_is_evidence_only 'Attribution result evidence-only authority'
Require-True $contract.authority.returned_evidence_adjudication_required 'Returned evidence adjudication authority'
Require-False $contract.authority.rp1c_selection_authorized 'RP1C selection authority'
Require-False $contract.authority.production_repair_authorized 'Production repair authority'
Require-False $contract.authority.threshold_change_authorized 'Threshold change authority'
Require-False $contract.authority.exact_v9_change_authorized 'Exact-v9 authority'
Require-False $contract.authority.vr3_authorized 'VR3 authority'
Require-False $contract.authority.p3_r1_authorized 'P3-R1 authority'
Require-False $contract.authority.second_replacement_long_authorized 'Second replacement-long authority'

Require-Text $implementationDoc 'evidence-only'
Require-Text $implementationDoc '460,800'
Require-Text $implementationDoc '86'
Require-Text $implementationDoc 'counterbalanced'
Require-Text $reviewDoc 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $reviewDoc 'Windows PowerShell 5.1'
Require-Text $reviewDoc 'full exact-v9 row identity'

Write-Host 'Exact-v9 Tail Attribution 1 REV1 static audit: PASS-AS-AUTHORED'
