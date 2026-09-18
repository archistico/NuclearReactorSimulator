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

function Require-AsciiSource([string]$Path, [string]$Label) {
    Require-File $Path
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -gt 127) {
            throw ("{0} must remain 7-bit ASCII for Windows PowerShell 5.1 source-decoding safety; non-ASCII byte at offset {1}." -f $Label, $i)
        }
    }
}

function Require-True([object]$Value, [string]$Label) {
    if ($Value -ne $true) { throw ("{0} must be true." -f $Label) }
}

function Require-False([object]$Value, [string]$Label) {
    if ($Value -ne $false) { throw ("{0} must be false." -f $Label) }
}

function Require-Near([double]$Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    if ([Math]::Abs($Actual - $Expected) -gt $Tolerance) {
        throw ("{0} drifted: actual={1}; expected={2}" -f $Label, $Actual, $Expected)
    }
}

function Require-NormalizedTextSha256([string]$Path, [string]$Expected, [string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($enc.GetBytes($normalized))
    }
    finally {
        $sha.Dispose()
    }
    $actual = ([BitConverter]::ToString($hash)).Replace('-', '').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label, $actual, $Expected)
    }
}

function Require-EnvironmentValue([object]$Mode, [string]$Name, [string]$Expected, [string]$Label) {
    $property = $Mode.environment.PSObject.Properties | Where-Object { $_.Name -eq $Name } | Select-Object -First 1
    if ($null -eq $property) { throw ("Missing environment field for {0}: {1}" -f $Label, $Name) }
    $actual = [string]$property.Value
    if ($actual -ne $Expected) {
        throw ("{0} drifted for {1}: actual={2}; expected={3}" -f $Label, $Name, $actual, $Expected)
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$validatorPath = $MyInvocation.MyCommand.Path
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation1-contract.json'
$planningDir = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeFactorIsolationPlanning1_Artifacts'
$planning01 = Join-Path $planningDir '01-contract-and-provenance.txt'
$planning02 = Join-Path $planningDir '02-factor-isolation-matrix.csv'
$planning03 = Join-Path $planningDir '03-planning-summary.txt'
$planning04 = Join-Path $planningDir '04-preexecution-review.txt'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
$testPath = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1cC4RuntimeFactorIsolation1Tests.cs'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation1.cmd'
$adjudicatorPath = Join-Path $PSScriptRoot 'adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation1.ps1'
$implementationDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION1.md'
$reviewDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION1_PREEXECUTION_REVIEW.md'

foreach ($path in @(
    $contractPath,$planning01,$planning02,$planning03,$planning04,$c4Path,$exactPath,$performancePath,
    $testPath,$runnerPath,$adjudicatorPath,$implementationDoc,$reviewDoc
)) {
    Require-File $path
}

# Windows PowerShell 5.1 parses UTF-8-without-BOM script source through the legacy code page.
# Keep executable gate sources strictly 7-bit ASCII so source literals cannot be mojibaked before execution.
Require-AsciiSource $validatorPath 'Static validator source'
Require-AsciiSource $adjudicatorPath 'Adjudicator source'
Require-AsciiSource $runnerPath 'Runner source'

$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation1-v1') { throw 'Contract schema drifted.' }
if ($contract.status -ne 'CANDIDATE-EVIDENCE-ONLY') { throw 'Contract status drifted.' }
if ($contract.gate -ne 'RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1') { throw 'Contract gate drifted.' }

$p = $contract.prerequisite
if ($p.planning1_returned_adjudication -ne 'PASS' -or $p.planning1_status -ne 'PASS-AS-AUTHORED') { throw 'Returned Planning 1 prerequisite drifted.' }
if ($p.attribution1_returned_adjudication -ne 'PASS') { throw 'Attribution 1 returned adjudication prerequisite drifted.' }
Require-True $p.runtime_configuration_sensitivity_confirmed 'Runtime sensitivity prerequisite'
Require-True $p.tiering_off_repair_direction_rejected 'Tiering-off rejection prerequisite'
Require-False $p.dynamic_pgo_causality_proven 'Dynamic PGO causality prerequisite'
Require-True $p.c4_candidate_immutable 'C4 immutability prerequisite'
Require-True $p.exact_v9_corpus_immutable 'Exact-v9 immutability prerequisite'
Require-True $p.caller_runtime_environment_must_be_clean 'Caller runtime clean prerequisite'
Require-False $p.rp1c_selection_authorized 'RP1C selection prerequisite'

$protocol = $contract.protocol
Require-True $protocol.evidence_only 'Evidence-only protocol'
Require-True $protocol.exact_v9_only 'Exact-v9-only protocol'
if ([int]$protocol.mode_count -ne 4 -or [int]$protocol.fresh_processes_per_mode -ne 5 -or [int]$protocol.total_fresh_processes -ne 20) { throw 'Runtime process protocol drifted.' }
if ([int]$protocol.rows -ne 360 -or [int]$protocol.warmup_passes -ne 16 -or [int]$protocol.measured_passes -ne 64 -or [int]$protocol.rotation_stride -ne 37) { throw 'Exact-v9 process shape drifted.' }
if ([int]$protocol.measured_calls_per_process -ne 23040 -or [int]$protocol.total_measured_calls -ne 460800) { throw 'Measured-call count drifted.' }
Require-Near ([double]$protocol.strict_max_us) 409.30666666666673 1.0e-12 'Strict max ceiling'
Require-Near ([double]$protocol.diagnostic_tail_floor_us) 100.0 1.0e-12 'Diagnostic tail floor'
Require-False $protocol.diagnostic_tail_floor_is_qualification_threshold '100 us threshold authority'
Require-True $protocol.allocation_neutral_harness_required 'Allocation-neutral harness'
Require-False $protocol.negative_or_inconclusive_result_is_harness_failure 'Negative engineering result xUnit policy'
Require-False $protocol.engineering_negative_or_inconclusive_is_infrastructure_red 'Engineering/infrastructure classification policy'
Require-False $protocol.automatic_causal_promotion 'Automatic causal promotion'
Require-False $protocol.automatic_selection_authorized 'Automatic selection authorization'
if ($protocol.execution_order_strategy -ne 'COUNTERBALANCED-BLOCKED-BY-RUN') { throw 'Execution-order strategy drifted.' }
$expectedSchedule = @('A1','B1','C1','D1','B2','C2','D2','A2','C3','D3','A3','B3','D4','A4','B4','C4','A5','C5','B5','D5')
$schedule = @($protocol.execution_schedule)
if ($schedule.Count -ne $expectedSchedule.Count) { throw 'Execution schedule length drifted.' }
for ($i = 0; $i -lt $expectedSchedule.Count; $i++) {
    if ([string]$schedule[$i] -ne $expectedSchedule[$i]) { throw ("Execution schedule drifted at index {0}." -f $i) }
}
Require-True $protocol.full_exact_matrix_identity_verified_by_adjudicator 'Full exact matrix integrity policy'
Require-True $protocol.frozen_performance_baseline_pinned 'Frozen performance baseline policy'

$expectedVars = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
$expectedModes = @(
    @('TIERING-OFF-QJ-OFF-QJFL-OFF','A','0','0','0','0','1'),
    @('TIERING-ON-QJ-OFF-QJFL-OFF','B','1','0','0','0','1'),
    @('TIERING-ON-QJ-ON-QJFL-OFF','C','1','0','1','0','1'),
    @('TIERING-ON-QJ-ON-QJFL-ON','D','1','0','1','1','1')
)
$modes = @($contract.runtime_modes)
if ($modes.Count -ne 4) { throw 'Expected four runtime modes.' }
for ($i = 0; $i -lt 4; $i++) {
    if ([string]$modes[$i].id -ne $expectedModes[$i][0]) { throw 'Runtime mode identity/order drifted.' }
    if ([string]$modes[$i].letter -ne $expectedModes[$i][1]) { throw 'Runtime mode letter drifted.' }
    if ([int]$modes[$i].processes -ne 5) { throw 'Each runtime mode must use five fresh processes.' }
    for ($j = 0; $j -lt $expectedVars.Count; $j++) {
        Require-EnvironmentValue $modes[$i] $expectedVars[$j] $expectedModes[$i][$j + 2] $modes[$i].id
    }
}

$expectedFactors = @('DOTNET_TieredCompilation','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops')
$contrasts = @($contract.contrasts)
if ($contrasts.Count -ne 3) { throw 'Expected exactly three single-factor contrasts.' }
for ($i = 0; $i -lt 3; $i++) {
    if ([string]$contrasts[$i].id -ne @('A-B','B-C','C-D')[$i]) { throw 'Contrast identity drifted.' }
    if ([string]$contrasts[$i].left -ne $expectedModes[$i][0] -or [string]$contrasts[$i].right -ne $expectedModes[$i + 1][0]) { throw 'Contrast adjacency drifted.' }
    if ([string]$contrasts[$i].changed_factor -ne $expectedFactors[$i]) { throw 'Single-factor identity drifted.' }
}
Require-False $contract.pgo_follow_up.included_in_this_gate 'PGO comparator inclusion'
Require-True $contract.pgo_follow_up.requires_separate_planning_if_still_material 'Separate PGO planning requirement'

$preflight = @($contract.caller_environment_preflight.required_unset)
if ($preflight.Count -ne 5) { throw 'Caller environment variable count drifted.' }
for ($i = 0; $i -lt 5; $i++) {
    if ([string]$preflight[$i] -ne $expectedVars[$i]) { throw 'Caller environment identity/order drifted.' }
}
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Parent environment fail-closed rule'

$evidence = $contract.evidence
if ([int]$evidence.process_directory_count -ne 20 -or [int]$evidence.files_per_process -ne 4 -or [int]$evidence.aggregate_file_count -ne 6 -or [int]$evidence.total_required_files -ne 86) { throw 'Evidence-tree shape drifted.' }
if ([int]$evidence.contrast_summary_count -ne 3) { throw 'Contrast-summary count drifted.' }
Require-True $evidence.run_summary_includes_sequence_index 'Run summary sequence-index policy'
Require-True $evidence.per_mode_aggregate_in_evidence_summary 'Per-mode aggregate policy'
Require-True $evidence.tail_row_summary_includes_run_and_run_pass_pairs 'Tail row provenance policy'

$execution = $contract.execution
Require-True $execution.explicit 'Focused test explicit flag'
if ($execution.parallel -ne 'none' -or $execution.configuration -ne 'Release') { throw 'Execution configuration drifted.' }
if ($execution.test_class -ne 'M10FinalVr2EngineeringRepairPlanning1Rp1cC4RuntimeFactorIsolation1Tests') { throw 'Focused test class drifted.' }
if ($execution.test_method -ne 'Rp1cC4RuntimeFactorIsolation1_MeasuresOneFreshSingleFactorModeProcess') { throw 'Focused test method drifted.' }

$expectedHashes = @{
    planning_artifact_01 = 'BCAE4A1170AB442C54248669374893CDE07730EF4354F41997FE81BF05EE59BB'
    planning_artifact_02 = '3E03AF2415524C547DB70F7324409278ED15DC81DA143CA498A60AFB1298367F'
    planning_artifact_03 = 'B618418EF13840C1548FB151D00F150ADF903F8630FA5EF705BBD667B671054E'
    planning_artifact_04 = '1DDA64D0A141E86F960EF47771A1F148B016CF23EB3BFDB8B36A1D98D0B8C178'
    c4_candidate = '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6'
    rp1a_exact_v9_corpus = '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E'
    rp1a_performance_baseline = '91776709A34142E9312A092F3B19EAFCF0FCA576D561F296CC9272FCDC4F7DEB'
    focused_test = '7B3560EFA247A21618179DC26A8A7B8A40770D27C77C356E5CB99040CE3EC9A5'
    runner = '25E3CC1FB96492F62F4F3B745606E17E19C51817BDF599D25E1CDAB08FC81992'
    adjudicator = 'F8DE6CCC9C9D45ECE36231434144CF01C7BE59E361E40A7D72BFD386101D2A6C'
}
$hashes = $contract.normalized_lf_sha256
foreach ($name in $expectedHashes.Keys) {
    $contractValue = [string]$hashes.PSObject.Properties[$name].Value
    if ($contractValue.ToUpperInvariant() -ne $expectedHashes[$name]) {
        throw ("Contract hash drifted for {0}." -f $name)
    }
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
Require-Text $planning01 'future-gate=RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1'
Require-Text $planning02 'TIERING-ON-QJ-ON-QJFL-ON,1,0,1,1,1,DOTNET_TC_QuickJitForLoops'
Require-Text $planning03 'single-factor-contrasts=3'
Require-Text $planning04 'correction=SINGLE-FACTOR-CHAIN-TIERING-THEN-QUICKJIT-THEN-QUICKJITFORLOOPS'

$testText = Read-Utf8Text $testPath
foreach ($marker in @(
    '[Fact(Explicit = true)]',
    'Rp1cC4RuntimeFactorIsolation1_MeasuresOneFreshSingleFactorModeProcess',
    'TIERING-OFF-QJ-OFF-QJFL-OFF',
    'TIERING-ON-QJ-OFF-QJFL-OFF',
    'TIERING-ON-QJ-ON-QJFL-OFF',
    'TIERING-ON-QJ-ON-QJFL-ON',
    'DiagnosticTailFloorMicroseconds = 100d',
    'MeasuredPasses = 64',
    'WarmupPasses = 16',
    'strictMaximumMicroseconds',
    'process-id=',
    'server-gc=',
    'production-runtime-change-authorized=False',
    'automatic-causal-promotion=False',
    'rp1c-selection-authorized=False'
)) {
    if (-not $testText.Contains($marker)) { throw ("Focused test marker missing: {0}" -f $marker) }
}
if ($testText.Contains('409.30666666666673d')) { throw 'Focused test must use the frozen performance baseline rather than duplicate the strict max literal.' }
if ($testText.Contains('Assert.True(strict')) { throw 'Focused test must not convert a negative performance observation into xUnit failure.' }

$runnerText = Read-Utf8Text $runnerPath
foreach ($name in $expectedVars) {
    if (-not $runnerText.Contains(("if defined {0}" -f $name))) { throw ("Runner caller-environment preflight missing: {0}" -f $name) }
}
foreach ($mode in $expectedModes) {
    if (-not $runnerText.Contains($mode[0])) { throw ("Runner mode missing: {0}" -f $mode[0]) }
}
foreach ($marker in @(
    'Block/run 1: A B C D',
    'Block/run 2: B C D A',
    'Block/run 3: C D A B',
    'Block/run 4: D A B C',
    'Block/run 5: A C B D',
    'call :run_one TIERING-OFF-QJ-OFF-QJFL-OFF 1 0 0 0 0 1',
    'call :run_one TIERING-ON-QJ-OFF-QJFL-OFF 1 1 0 0 0 1',
    'call :run_one TIERING-ON-QJ-ON-QJFL-OFF 1 1 0 1 0 1',
    'call :run_one TIERING-ON-QJ-ON-QJFL-ON 1 1 0 1 1 1',
    '02-runtime-factor-run-summary.csv',
    '03-single-factor-contrast-summary.csv',
    'set "DOTNET_TieredCompilation="',
    'set "DOTNET_TC_QuickJitForLoops="',
    '--parallel none',
    '--no-build'
)) {
    if (-not $runnerText.Contains($marker)) { throw ("Runner hardening marker missing: {0}" -f $marker) }
}

$adjudicatorText = Read-Utf8Text $adjudicatorPath
foreach ($marker in @(
    '02-runtime-factor-run-summary.csv',
    '03-single-factor-contrast-summary.csv',
    '04-tail-row-path-summary.csv',
    'sequence_index',
    'run_pass_pairs',
    'Timing row/order',
    'Timing probe identity',
    'Runtime allocation accounting',
    "'TIERING-OFF-QJ-OFF-QJFL-OFF' = @('0','0','0','0','1')",
    "'TIERING-ON-QJ-OFF-QJFL-OFF' = @('1','0','0','0','1')",
    "'TIERING-ON-QJ-ON-QJFL-OFF' = @('1','0','1','0','1')",
    "'TIERING-ON-QJ-ON-QJFL-ON' = @('1','0','1','1','1')",
    'DOTNET_TieredCompilation',
    'DOTNET_TC_QuickJit',
    'DOTNET_TC_QuickJitForLoops',
    'engineering-negative-or-inconclusive-is-infrastructure-red=False',
    'automatic-causal-promotion=False',
    'production-runtime-change-authorized=False',
    'rp1c-selection-authorized=False',
    'exactly 86 files'
)) {
    if (-not $adjudicatorText.Contains($marker)) { throw ("Adjudicator marker missing: {0}" -f $marker) }
}
if ($adjudicatorText -match '\$[A-Za-z_][A-Za-z0-9_]*:') { throw 'Adjudicator contains a PowerShell variable-colon interpolation hazard.' }
if ($adjudicatorText.Contains('StringComparison')) { throw 'Adjudicator must remain compatible with Windows PowerShell 5.1.' }

$a = $contract.authority
Require-True $a.runtime_factor_isolation_gate_implementation_authorized 'Runtime Factor Isolation implementation authority'
Require-True $a.runtime_factor_isolation_result_is_evidence_only 'Runtime Factor Isolation evidence-only authority'
Require-True $a.returned_evidence_adjudication_required 'Returned evidence adjudication authority'
Require-False $a.pgo_comparator_implementation_authorized 'PGO comparator implementation authority'
Require-False $a.rp1c_selection_authorized 'RP1C selection authority'
Require-False $a.production_runtime_change_authorized 'Production runtime change authority'
Require-False $a.production_repair_authorized 'Production repair authority'
Require-False $a.candidate_mutation_authorized 'Candidate mutation authority'
Require-False $a.threshold_change_authorized 'Threshold change authority'
Require-False $a.exact_v9_change_authorized 'Exact-v9 authority'
Require-False $a.vr3_authorized 'VR3 authority'
Require-False $a.p3_r1_authorized 'P3-R1 authority'
Require-False $a.second_replacement_long_authorized 'Second replacement-long authority'
if ($a.next_action_after_local_completion -ne 'RETURN-COMPLETE-RUNTIME-FACTOR-ISOLATION1-ARTIFACTS-FOR-ADJUDICATION') { throw 'Next-action contract drifted.' }

Require-Text $implementationDoc 'RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1'
Require-Text $implementationDoc '460,800 measured calls total'
Require-Text $implementationDoc 'exactly 86 files'
Require-Text $implementationDoc 'engineering-negative-or-inconclusive-is-infrastructure-red=False'
Require-Text $reviewDoc 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $reviewDoc 'Windows PowerShell 5.1 compatibility review'
Require-Text $reviewDoc '`Require-Text` anti-false-RED review'
Require-Text $reviewDoc ('all `64 ' + [char]0x00D7 + ' 360` call identities per process')

Write-Host 'Exact-v9 Runtime Factor Isolation 1 static audit: PASS-AS-AUTHORED'
