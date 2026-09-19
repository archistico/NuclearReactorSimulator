$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
}
function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}
function Require-Text([string]$Path,[string]$Needle) {
    $text = Read-Utf8Text $Path
    if (-not $text.Contains($Needle)) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Require-True([object]$Value,[string]$Label) {
    if ($Value -ne $true) { throw ("{0} must be true." -f $Label) }
}
function Require-False([object]$Value,[string]$Label) {
    if ($Value -ne $false) { throw ("{0} must be false." -f $Label) }
}
function Require-Near([double]$Actual,[double]$Expected,[double]$Tolerance,[string]$Label) {
    if ([Math]::Abs($Actual-$Expected) -gt $Tolerance) { throw ("{0} drifted: actual={1}; expected={2}" -f $Label,$Actual,$Expected) }
}
function Require-NormalizedTextSha256([string]$Path,[string]$Expected,[string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n","`n").Replace("`r","`n")
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($enc.GetBytes($normalized)) } finally { $sha.Dispose() }
    $actual = ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) { throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label,$actual,$Expected) }
}
function Require-AsciiSource([string]$Path,[string]$Label) {
    Require-File $Path
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    for ($i=0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -gt 127) { throw ("{0} must remain 7-bit ASCII for Windows PowerShell 5.1 source safety; non-ASCII byte at offset {1}." -f $Label,$i) }
    }
}
function Require-EnvironmentValue([object]$Mode,[string]$Name,[string]$Expected,[string]$Label) {
    $property = $Mode.environment.PSObject.Properties | Where-Object { $_.Name -eq $Name } | Select-Object -First 1
    if ($null -eq $property) { throw ("Missing environment field for {0}: {1}" -f $Label,$Name) }
    if ([string]$property.Value -ne $Expected) { throw ("{0} drifted for {1}." -f $Label,$Name) }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$validatorPath = $MyInvocation.MyCommand.Path
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1-contract.json'
$planningDir = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_DynamicPgoComparatorPlanning1_Artifacts'
$planning01 = Join-Path $planningDir '01-contract-and-provenance.txt'
$planning02 = Join-Path $planningDir '02-pgo-comparator-matrix.csv'
$planning03 = Join-Path $planningDir '03-planning-summary.txt'
$planning04 = Join-Path $planningDir '04-preexecution-review.txt'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
$testPath = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1cC4DynamicPgoComparator1Tests.cs'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1.cmd'
$adjudicatorPath = Join-Path $PSScriptRoot 'adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1.ps1'
$planningAdjDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md'
$implementationDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR1.md'
$reviewDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR1_PREEXECUTION_REVIEW.md'

foreach ($path in @($contractPath,$planning01,$planning02,$planning03,$planning04,$c4Path,$exactPath,$performancePath,$testPath,$runnerPath,$adjudicatorPath,$planningAdjDoc,$implementationDoc,$reviewDoc)) { Require-File $path }

Require-AsciiSource $validatorPath 'Static validator source'
Require-AsciiSource $adjudicatorPath 'Adjudicator source'
Require-AsciiSource $runnerPath 'Runner source'

$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1-v1') { throw 'Contract schema drifted.' }
if ($contract.status -ne 'CANDIDATE-EVIDENCE-ONLY') { throw 'Contract status drifted.' }
if ($contract.gate -ne 'RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1') { throw 'Contract gate drifted.' }

$p = $contract.prerequisite
if ($p.planning1_returned_adjudication -ne 'PASS' -or $p.planning1_status -ne 'PASS-AS-AUTHORED') { throw 'Returned Planning 1 prerequisite drifted.' }
if ($p.runtime_factor_isolation1_returned_adjudication -ne 'PASS') { throw 'Runtime Factor Isolation 1 prerequisite drifted.' }
if ($p.roadmap_branch -ne 'A-RUNTIME-SENSITIVE') { throw 'Runtime-sensitive branch prerequisite drifted.' }
Require-True $p.tiered_compilation_material_single_factor_effect 'TieredCompilation material-effect prerequisite'
Require-False $p.quickjit_rare_tail_causality_promoted 'QuickJit causal promotion prerequisite'
Require-False $p.quickjit_for_loops_material_tail_benefit 'QuickJitForLoops material-benefit prerequisite'
Require-True $p.dynamic_pgo_comparator_material 'Dynamic PGO comparator materiality prerequisite'
Require-True $p.c4_candidate_immutable 'C4 immutability prerequisite'
Require-True $p.exact_v9_corpus_immutable 'Exact-v9 immutability prerequisite'
Require-True $p.caller_runtime_environment_must_be_clean 'Caller environment prerequisite'
Require-False $p.runtime_configuration_impact_assessment_authorized 'Runtime Configuration Impact Assessment prerequisite authority'
Require-False $p.rp1c_selection_authorized 'RP1C selection prerequisite authority'

$protocol = $contract.protocol
Require-True $protocol.evidence_only 'Evidence-only protocol'
Require-True $protocol.exact_v9_only 'Exact-v9-only protocol'
if ([int]$protocol.mode_count -ne 2 -or [int]$protocol.fresh_processes_per_mode -ne 5 -or [int]$protocol.total_fresh_processes -ne 10) { throw 'Process protocol drifted.' }
if ([int]$protocol.rows -ne 360 -or [int]$protocol.warmup_passes -ne 16 -or [int]$protocol.measured_passes -ne 64 -or [int]$protocol.rotation_stride -ne 37) { throw 'Exact-v9 process shape drifted.' }
if ([int]$protocol.measured_calls_per_process -ne 23040 -or [int]$protocol.total_measured_calls -ne 230400) { throw 'Measured-call count drifted.' }
Require-Near ([double]$protocol.strict_max_us) 409.30666666666673 1.0e-12 'Strict max ceiling'
Require-Near ([double]$protocol.diagnostic_tail_floor_us) 100.0 1.0e-12 'Diagnostic tail floor'
Require-False $protocol.diagnostic_tail_floor_is_qualification_threshold '100 us threshold authority'
Require-True $protocol.allocation_neutral_harness_required 'Allocation-neutral harness'
Require-False $protocol.negative_or_inconclusive_result_is_harness_failure 'Negative engineering result xUnit policy'
Require-False $protocol.engineering_negative_or_inconclusive_is_infrastructure_red 'Engineering/infrastructure classification policy'
Require-False $protocol.automatic_causal_promotion 'Automatic PGO causal promotion'
Require-False $protocol.automatic_selection_authorized 'Automatic selection authority'
if ($protocol.execution_order_strategy -ne 'COUNTERBALANCED-BLOCKED-BY-RUN') { throw 'Execution-order strategy drifted.' }
$expectedSchedule = @('OFF1','ON1','ON2','OFF2','OFF3','ON3','ON4','OFF4','OFF5','ON5')
$schedule = @($protocol.execution_schedule)
if ($schedule.Count -ne 10) { throw 'Execution schedule length drifted.' }
for ($i=0; $i -lt 10; $i++) { if ([string]$schedule[$i] -ne $expectedSchedule[$i]) { throw ("Execution schedule drifted at index {0}." -f $i) } }
Require-True $protocol.full_exact_matrix_identity_verified_by_adjudicator 'Full matrix identity policy'
Require-True $protocol.frozen_performance_baseline_pinned 'Frozen performance baseline policy'
Require-False $protocol.ambient_unset_included 'Ambient/unset inclusion'

$expectedVars = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
$expectedModes = @(
    @('PGO-OFF-QJFL-ON','OFF','1','0','1','1','1'),
    @('PGO-ON-QJFL-ON','ON','1','1','1','1','1')
)
$modes = @($contract.runtime_modes)
if ($modes.Count -ne 2) { throw 'Expected two PGO runtime modes.' }
for ($i=0; $i -lt 2; $i++) {
    if ([string]$modes[$i].id -ne $expectedModes[$i][0] -or [string]$modes[$i].letter -ne $expectedModes[$i][1]) { throw 'Runtime mode identity/order drifted.' }
    if ([int]$modes[$i].processes -ne 5) { throw 'Each PGO mode must use five fresh processes.' }
    for ($j=0; $j -lt 5; $j++) { Require-EnvironmentValue $modes[$i] $expectedVars[$j] $expectedModes[$i][$j+2] $modes[$i].id }
}
if ($contract.contrast.id -ne 'PGO-OFF-ON' -or $contract.contrast.left -ne 'PGO-OFF-QJFL-ON' -or $contract.contrast.right -ne 'PGO-ON-QJFL-ON' -or $contract.contrast.changed_factor -ne 'DOTNET_TieredPGO') { throw 'PGO single-factor contrast drifted.' }

$preflight = @($contract.caller_environment_preflight.required_unset)
if ($preflight.Count -ne 5) { throw 'Caller environment variable count drifted.' }
for ($i=0; $i -lt 5; $i++) { if ([string]$preflight[$i] -ne $expectedVars[$i]) { throw 'Caller environment identity/order drifted.' } }
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Parent environment fail-closed rule'

$e = $contract.evidence
if ([int]$e.process_directory_count -ne 10 -or [int]$e.files_per_process -ne 4 -or [int]$e.aggregate_file_count -ne 6 -or [int]$e.total_required_files -ne 46) { throw 'Evidence-tree shape drifted.' }
if ([int]$e.contrast_summary_count -ne 1) { throw 'Contrast-summary count drifted.' }
Require-True $e.run_summary_includes_sequence_index 'Run summary sequence-index policy'
Require-True $e.per_mode_aggregate_in_evidence_summary 'Per-mode aggregate policy'
Require-True $e.tail_row_summary_includes_run_and_run_pass_pairs 'Tail provenance policy'

$x = $contract.execution
Require-True $x.explicit 'Focused test explicit flag'
if ($x.parallel -ne 'none' -or $x.configuration -ne 'Release') { throw 'Execution configuration drifted.' }
if ($x.test_class -ne 'M10FinalVr2EngineeringRepairPlanning1Rp1cC4DynamicPgoComparator1Tests' -or $x.test_method -ne 'Rp1cC4DynamicPgoComparator1_MeasuresOneFreshPgoModeProcess') { throw 'Focused test identity drifted.' }

$expectedHashes = @{
    planning_artifact_01 = '1C6D97C475F0270FE50C85B2B1CA9231D6D105655D0205F3D44748C1B604C25B'
    planning_artifact_02 = '1F21DEBD7B9DFB661D7590E50DCA57BFEB2B78C63B5B0A37BC444C8F412A4D55'
    planning_artifact_03 = '032204D6DFA148E2E8D44A89107BB05BAF7CCC71EEAD0D303BFEE449A817D2C6'
    planning_artifact_04 = 'E08A2129BF4827D128AB6C291908961AC74444DB011FB814A9DA190799DE0A96'
    c4_candidate = '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6'
    rp1a_exact_v9_corpus = '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E'
    rp1a_performance_baseline = '91776709A34142E9312A092F3B19EAFCF0FCA576D561F296CC9272FCDC4F7DEB'
    focused_test = '2ADA9DE54C61B65EA1EB4D0EE7BDDD14B7ACFE035EAF70971BC86A95F0E7E5B4'
    runner = '73D1903AD2C782C26A651E4F16C3118C7B9D7EC2EC03C215524E42324DEF0386'
    adjudicator = '8CDE9C9F73F5048BCB02EB487A5DD28CBFAAD2A9D639ACFC993922161572A078'
}
$hashes = $contract.normalized_lf_sha256
foreach ($name in $expectedHashes.Keys) {
    $contractValue = [string]$hashes.PSObject.Properties[$name].Value
    if ($contractValue.ToUpperInvariant() -ne $expectedHashes[$name]) { throw ("Contract hash drifted for {0}." -f $name) }
}
Require-NormalizedTextSha256 $planning01 $expectedHashes['planning_artifact_01'] 'Returned planning artifact 01'
Require-NormalizedTextSha256 $planning02 $expectedHashes['planning_artifact_02'] 'Returned planning artifact 02'
Require-NormalizedTextSha256 $planning03 $expectedHashes['planning_artifact_03'] 'Returned planning artifact 03'
Require-NormalizedTextSha256 $planning04 $expectedHashes['planning_artifact_04'] 'Returned planning artifact 04'
Require-NormalizedTextSha256 $c4Path $expectedHashes['c4_candidate'] 'Immutable C4 candidate'
Require-NormalizedTextSha256 $exactPath $expectedHashes['rp1a_exact_v9_corpus'] 'Frozen exact-v9 corpus'
Require-NormalizedTextSha256 $performancePath $expectedHashes['rp1a_performance_baseline'] 'Frozen performance baseline'
Require-NormalizedTextSha256 $testPath $expectedHashes['focused_test'] 'Focused test'
Require-NormalizedTextSha256 $runnerPath $expectedHashes['runner'] 'Runner'
Require-NormalizedTextSha256 $adjudicatorPath $expectedHashes['adjudicator'] 'Adjudicator'

Require-Text $planning01 'status=PASS-AS-AUTHORED'
Require-Text $planning01 'future-gate=RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1'
Require-Text $planning01 'future-total-measured-calls=230400'
Require-Text $planning02 'PGO-OFF-QJFL-ON,1,0,1,1,1,BASELINE'
Require-Text $planning02 'PGO-ON-QJFL-ON,1,1,1,1,1,DOTNET_TieredPGO'
Require-Text $planning03 'dynamic-pgo-comparator-implementation-authorized-now=False'
Require-Text $planning03 'runtime-configuration-impact-assessment-authorized=False'
Require-Text $planning04 'remaining-hypothesis=DYNAMIC-PGO-WITH-QJFL-ON'
Require-Text $planning04 'future-processes=2-MODES-X-5=10'

$testText = Read-Utf8Text $testPath
foreach ($marker in @('[Fact(Explicit = true)]','Rp1cC4DynamicPgoComparator1_MeasuresOneFreshPgoModeProcess','PGO-OFF-QJFL-ON','PGO-ON-QJFL-ON','DiagnosticTailFloorMicroseconds = 100d','MeasuredPasses = 64','WarmupPasses = 16','dynamic-pgo-single-factor-only=True','process-id=','server-gc=','production-runtime-change-authorized=False','automatic-causal-promotion=False','rp1c-selection-authorized=False')) {
    if (-not $testText.Contains($marker)) { throw ("Focused test marker missing: {0}" -f $marker) }
}
if ($testText.Contains('409.30666666666673d')) { throw 'Focused test must use the frozen performance baseline rather than duplicate the strict max literal.' }
if ($testText.Contains('Assert.True(strict')) { throw 'Focused test must not convert a negative performance observation into xUnit failure.' }

$runnerText = Read-Utf8Text $runnerPath
foreach ($name in $expectedVars) { if (-not $runnerText.Contains(("if defined {0}" -f $name))) { throw ("Runner caller-environment preflight missing: {0}" -f $name) } }
foreach ($marker in @('Block 1: OFF ON','Block 2: ON OFF','Block 3: OFF ON','Block 4: ON OFF','Block 5: OFF ON','call :run_one PGO-OFF-QJFL-ON 1 0','call :run_one PGO-ON-QJFL-ON 1 1','set "DOTNET_TieredCompilation=1"','set "DOTNET_TC_QuickJit=1"','set "DOTNET_TC_QuickJitForLoops=1"','set "DOTNET_ReadyToRun=1"','02-pgo-run-summary.csv','03-pgo-contrast-summary.csv','--parallel none','--no-build')) {
    if (-not $runnerText.Contains($marker)) { throw ("Runner hardening marker missing: {0}" -f $marker) }
}

$adjudicatorText = Read-Utf8Text $adjudicatorPath
foreach ($marker in @('02-pgo-run-summary.csv','03-pgo-contrast-summary.csv','04-tail-row-path-summary.csv','sequence_index','run_pass_pairs','Timing row/order','Timing probe identity','Runtime allocation accounting',"'PGO-OFF-QJFL-ON' = @('1','0','1','1','1')","'PGO-ON-QJFL-ON'  = @('1','1','1','1','1')",'DOTNET_TieredPGO','engineering-negative-or-inconclusive-is-infrastructure-red=False','automatic-causal-promotion=False','runtime-configuration-impact-assessment-authorized=False','production-runtime-change-authorized=False','rp1c-selection-authorized=False','exactly 46 files')) {
    if (-not $adjudicatorText.Contains($marker)) { throw ("Adjudicator marker missing: {0}" -f $marker) }
}
if ($adjudicatorText -match '\$[A-Za-z_][A-Za-z0-9_]*:') { throw 'Adjudicator contains a PowerShell variable-colon interpolation hazard.' }
if ($adjudicatorText.Contains('StringComparison')) { throw 'Adjudicator must remain compatible with Windows PowerShell 5.1.' }

$a = $contract.authority
Require-True $a.dynamic_pgo_comparator_gate_implementation_authorized 'Dynamic PGO Comparator implementation authority'
Require-True $a.dynamic_pgo_comparator_result_is_evidence_only 'Dynamic PGO Comparator evidence-only authority'
Require-True $a.returned_evidence_adjudication_required 'Returned evidence adjudication authority'
Require-False $a.runtime_configuration_impact_assessment_authorized 'Runtime Configuration Impact Assessment authority'
Require-False $a.rp1c_selection_authorized 'RP1C selection authority'
Require-False $a.production_runtime_change_authorized 'Production runtime change authority'
Require-False $a.production_repair_authorized 'Production repair authority'
Require-False $a.candidate_mutation_authorized 'Candidate mutation authority'
Require-False $a.threshold_change_authorized 'Threshold change authority'
Require-False $a.exact_v9_change_authorized 'Exact-v9 authority'
Require-False $a.vr3_authorized 'VR3 authority'
Require-False $a.p3_r1_authorized 'P3-R1 authority'
Require-False $a.second_replacement_long_authorized 'Second replacement-long authority'
if ($a.next_action_after_local_completion -ne 'RETURN-COMPLETE-DYNAMIC-PGO-COMPARATOR1-ARTIFACTS-FOR-ADJUDICATION') { throw 'Next-action contract drifted.' }

Require-Text $planningAdjDoc 'PASS - RETURNED PLANNING EVIDENCE ACCEPTED AS AUTHORED.'
Require-Text $planningAdjDoc 'The only changed factor is `DOTNET_TieredPGO`.'
Require-Text $implementationDoc '230,400'
Require-Text $implementationDoc 'exactly 46 files'
Require-Text $implementationDoc 'Negative, neutral or inconclusive engineering evidence is not infrastructure RED.'
Require-Text $reviewDoc 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $reviewDoc 'Windows PowerShell 5.1 compatibility review'
Require-Text $reviewDoc '`Require-Text` anti-false-RED review'
Require-Text $reviewDoc 'all 64 x 360 call identities/order'

Write-Host 'Exact-v9 Dynamic PGO Comparator 1 static audit: PASS-AS-AUTHORED'
