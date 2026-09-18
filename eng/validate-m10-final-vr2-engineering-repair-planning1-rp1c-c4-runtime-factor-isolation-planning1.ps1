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
function Require-True([object]$Value,[string]$Label) { if ($Value -ne $true) { throw ("{0} must be true." -f $Label) } }
function Require-False([object]$Value,[string]$Label) { if ($Value -ne $false) { throw ("{0} must be false." -f $Label) } }
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
function Parse-InvDouble([string]$Text,[string]$Label) {
    $value = 0.0
    $style = [Globalization.NumberStyles]::Float -bor [Globalization.NumberStyles]::AllowThousands
    if (-not [double]::TryParse($Text,$style,[Globalization.CultureInfo]::InvariantCulture,[ref]$value)) { throw ("Invalid invariant double for {0}: {1}" -f $Label,$Text) }
    return $value
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation-planning1-contract.json'
$planPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION_PLANNING1.md'
$reviewPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION_PLANNING1_PREEXECUTION_REVIEW.md'
$adrPath = Join-Path $repoRoot 'docs\adr\0201-isolate-tiered-compilation-quickjit-and-loop-quickjit-before-pgo-or-rp1c-selection.md'
$adjudicationPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION1_RETURNED_EVIDENCE_ADJUDICATION.md'
$attrRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_ExactV9TailAttribution1_Artifacts'
$runSummaryPath = Join-Path $attrRoot '02-runtime-mode-run-summary.csv'
$evidenceSummaryPath = Join-Path $attrRoot '05-attribution-evidence-summary.txt'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$corpusPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'

foreach ($path in @($contractPath,$planPath,$reviewPath,$adrPath,$adjudicationPath,$runSummaryPath,$evidenceSummaryPath,$c4Path,$corpusPath)) { Require-File $path }
$contract = (Read-Utf8Text $contractPath | ConvertFrom-Json)
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation-planning1-v1') { throw 'Unexpected contract schema.' }
if ($contract.gate -ne 'RP1C-C4-RUNTIME-FACTOR-ISOLATION-PLANNING1') { throw 'Unexpected planning gate id.' }

Require-NormalizedTextSha256 $adjudicationPath 'EF00AF32658216F301C205C9C06EC474C72B19A4B1E49490A5C8043A94DA0F78' 'Attribution 1 returned adjudication'
Require-NormalizedTextSha256 $runSummaryPath '1BEEDF801287CBF1386155DCAD6FE35F93FBA41BB4366CBE59EBE307BC1B4CD7' 'Attribution 1 runtime-mode run summary'
Require-NormalizedTextSha256 $evidenceSummaryPath '10DD9D81B700200EC74391DCF22BF63D4BD77E97B5A28CABCC76A98A1783E7A2' 'Attribution 1 evidence summary'
Require-NormalizedTextSha256 $c4Path '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6' 'C4 candidate'
Require-NormalizedTextSha256 $corpusPath '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E' 'Exact-v9 corpus'

Require-Text $adjudicationPath 'controlled runtime sensitivity established'
Require-Text $adjudicationPath 'Disabling the tiering/QuickJit configuration bundle is ruled out as a repair direction'
Require-Text $adjudicationPath 'Dynamic PGO causality is not proven'
Require-Text $adjudicationPath 'Tiered compilation alone is not isolated from QuickJit'
Require-Text $evidenceSummaryPath 'TIERING-OFF: processes=5; calls=115200; calls-over-100us=91283; calls-over-max=64'
Require-Text $evidenceSummaryPath 'TIERING-ON-PGO-OFF: processes=5; calls=115200; calls-over-100us=18; calls-over-max=0'
Require-Text $evidenceSummaryPath 'TIERING-ON-PGO-ON: processes=5; calls=115200; calls-over-100us=28; calls-over-max=0'

$rows = @(Import-Csv -LiteralPath $runSummaryPath)
if ($rows.Count -ne 20) { throw 'Attribution 1 run summary must contain 20 rows.' }
$modeIds = @('AMBIENT-UNSET-CONTROL','TIERING-OFF','TIERING-ON-PGO-OFF','TIERING-ON-PGO-ON')
foreach ($mode in $modeIds) {
    $modeRows = @($rows | Where-Object { $_.mode_id -eq $mode })
    if ($modeRows.Count -ne 5) { throw ("Attribution 1 mode {0} must contain five process rows." -f $mode) }
}
$strictByMode = @{}
$over100ByMode = @{}
$maxByMode = @{}
foreach ($mode in $modeIds) {
    $modeRows = @($rows | Where-Object { $_.mode_id -eq $mode })
    $strict = 0; $over100 = 0; $max = 0.0
    foreach ($r in $modeRows) {
        $strict += [int]$r.calls_over_max_ceiling
        $over100 += [int]$r.calls_over_100us
        $m = Parse-InvDouble $r.max_us ("max_us {0}" -f $mode)
        if ($m -gt $max) { $max = $m }
        if ([int]$r.candidate_allocated_bytes -ne 0 -or [int]$r.harness_allocated_bytes -ne 0 -or [int]$r.unresolved_calls -ne 0) { throw 'Attribution 1 zero-allocation/unresolved invariant drifted.' }
        if ([int]$r.gc_gen0 -ne 0 -or [int]$r.gc_gen1 -ne 0 -or [int]$r.gc_gen2 -ne 0) { throw 'Attribution 1 GC invariant drifted.' }
    }
    $strictByMode[$mode] = $strict; $over100ByMode[$mode] = $over100; $maxByMode[$mode] = $max
}
if ($strictByMode['AMBIENT-UNSET-CONTROL'] -ne 2 -or $over100ByMode['AMBIENT-UNSET-CONTROL'] -ne 16) { throw 'Ambient returned evidence drifted.' }
if ($strictByMode['TIERING-OFF'] -ne 64 -or $over100ByMode['TIERING-OFF'] -ne 91283) { throw 'Tiering-off returned evidence drifted.' }
if ($strictByMode['TIERING-ON-PGO-OFF'] -ne 0 -or $strictByMode['TIERING-ON-PGO-ON'] -ne 0) { throw 'Tiering-on returned strict evidence drifted.' }
Require-Near $maxByMode['TIERING-OFF'] 15497.2 1.0e-9 'Tiering-off max'
Require-Near $maxByMode['TIERING-ON-PGO-OFF'] 273.9 1.0e-9 'Tiering-on PGO-off max'
Require-Near $maxByMode['TIERING-ON-PGO-ON'] 330.4 1.0e-9 'Tiering-on PGO-on max'

$p = $contract.prerequisite
if ($p.attribution1_returned_adjudication -ne 'PASS') { throw 'Returned attribution adjudication prerequisite drifted.' }
Require-True $p.runtime_configuration_sensitivity_confirmed 'Runtime sensitivity prerequisite'
Require-True $p.tiering_off_repair_direction_rejected 'Tiering-off rejected prerequisite'
Require-False $p.dynamic_pgo_causality_proven 'Dynamic PGO causality prerequisite'
Require-True $p.c4_candidate_immutable 'C4 immutable prerequisite'
Require-True $p.exact_v9_corpus_immutable 'Exact-v9 immutable prerequisite'
Require-Near ([double]$p.strict_max_us) 409.30666666666673 1.0e-12 'Strict max prerequisite'
Require-False $p.rp1c_selection_authorized 'RP1C selection prerequisite'

$f = $contract.future_gate
if ($f.id -ne 'RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1') { throw 'Unexpected future gate id.' }
Require-True $f.evidence_only 'Future gate evidence-only'
Require-False $f.automatic_causal_promotion 'Automatic causal promotion'
Require-False $f.automatic_selection_authorized 'Automatic selection authorization'
if ([int]$f.fresh_processes_per_mode -ne 5 -or [int]$f.mode_count -ne 4 -or [int]$f.total_fresh_processes -ne 20) { throw 'Future process count drifted.' }
if ([int]$f.exact_v9_rows -ne 360 -or [int]$f.warmup_passes -ne 16 -or [int]$f.measured_passes -ne 64 -or [int]$f.rotation_stride -ne 37) { throw 'Future exact-v9 protocol drifted.' }
if ([int]$f.measured_calls_per_process -ne 23040 -or [int]$f.total_measured_calls -ne 460800) { throw 'Future call count drifted.' }
Require-Near ([double]$f.strict_max_us) 409.30666666666673 1.0e-12 'Future strict max'
Require-Near ([double]$f.diagnostic_tail_floor_us) 100.0 1.0e-12 'Diagnostic tail floor'
Require-False $f.diagnostic_tail_floor_changes_qualification_threshold 'Diagnostic floor threshold authority'
Require-True $f.allocation_neutral_harness_required 'Allocation-neutral harness requirement'
Require-True $f.counterbalanced_blocked_by_run 'Counterbalanced schedule requirement'
Require-True $f.returned_evidence_adjudication_required 'Returned evidence adjudication requirement'

$expectedVars = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
$expectedModes = @(
    @('TIERING-OFF-QJ-OFF-QJFL-OFF','0','0','0','0','1'),
    @('TIERING-ON-QJ-OFF-QJFL-OFF','1','0','0','0','1'),
    @('TIERING-ON-QJ-ON-QJFL-OFF','1','0','1','0','1'),
    @('TIERING-ON-QJ-ON-QJFL-ON','1','0','1','1','1')
)
$modes = @($contract.single_factor_chain)
if ($modes.Count -ne 4) { throw 'Expected four single-factor modes.' }
for ($i=0; $i -lt 4; $i++) {
    if ([string]$modes[$i].id -ne $expectedModes[$i][0]) { throw 'Single-factor mode identity/order drifted.' }
    if ([int]$modes[$i].processes -ne 5) { throw 'Each single-factor mode must use five processes.' }
    for ($j=0; $j -lt $expectedVars.Count; $j++) {
        $actual = [string]$modes[$i].environment.PSObject.Properties[$expectedVars[$j]].Value
        $expected = [string]$expectedModes[$i][$j+1]
        if ($actual -ne $expected) { throw ("Mode {0} environment drifted for {1}." -f $modes[$i].id,$expectedVars[$j]) }
    }
}
$contrasts = @($contract.contrasts)
$expectedFactors = @('DOTNET_TieredCompilation','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops')
if ($contrasts.Count -ne 3) { throw 'Expected three single-factor contrasts.' }
for ($i=0; $i -lt 3; $i++) {
    if ([string]$contrasts[$i].left -ne $expectedModes[$i][0] -or [string]$contrasts[$i].right -ne $expectedModes[$i+1][0] -or [string]$contrasts[$i].changed_factor -ne $expectedFactors[$i]) { throw 'Single-factor contrast chain drifted.' }
}
Require-False $contract.pgo_follow_up.included_in_this_gate 'PGO comparator inclusion'
Require-True $contract.pgo_follow_up.requires_separate_planning_if_still_material 'Separate PGO planning requirement'

$envList = @($contract.caller_environment_preflight.required_unset)
if ($envList.Count -ne 5) { throw 'Caller environment variable count drifted.' }
for ($i=0; $i -lt 5; $i++) { if ([string]$envList[$i] -ne $expectedVars[$i]) { throw 'Caller environment identity/order drifted.' } }
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Parent environment fail-closed rule'

if ([int]$contract.planned_evidence.process_directory_count -ne 20 -or [int]$contract.planned_evidence.files_per_process -ne 4 -or [int]$contract.planned_evidence.aggregate_file_count -ne 6 -or [int]$contract.planned_evidence.total_required_files -ne 86) { throw 'Planned evidence tree shape drifted.' }

$test = $contract.future_test_contract
Require-True $test.explicit 'Future test explicit flag'
if ($test.parallel -ne 'none' -or $test.configuration -ne 'Release') { throw 'Future execution configuration drifted.' }
$futureTestPath = Join-Path $repoRoot $test.file
$futureRunnerPath = Join-Path $repoRoot $test.runner
if (Test-Path -LiteralPath $futureTestPath -PathType Leaf) { throw 'Future Runtime Factor Isolation test is already implemented; planning must remain planning-only.' }
if (Test-Path -LiteralPath $futureRunnerPath -PathType Leaf) { throw 'Future Runtime Factor Isolation runner is already implemented; planning must remain planning-only.' }

$a = $contract.authority
Require-False $a.runtime_factor_isolation_implementation_authorized_now 'Runtime Factor Isolation implementation authority now'
Require-True $a.runtime_factor_isolation_implementation_authorizable_after_returned_planning_adjudication 'Runtime Factor Isolation post-adjudication eligibility'
Require-False $a.pgo_comparator_implementation_authorized 'PGO comparator implementation authority'
Require-False $a.rp1c_selection_authorized 'RP1C selection authority'
Require-False $a.production_runtime_change_authorized 'Production runtime change authority'
Require-False $a.production_repair_authorized 'Production repair authority'
Require-False $a.candidate_mutation_authorized 'Candidate mutation authority'
Require-False $a.threshold_change_authorized 'Threshold change authority'
Require-False $a.exact_v9_change_authorized 'Exact-v9 change authority'
Require-False $a.vr3_authorized 'VR3 authority'
Require-False $a.p3_r1_authorized 'P3-R1 authority'
Require-False $a.second_replacement_long_authorized 'Second replacement-long authority'
if ($a.next_action_after_local_planning_pass -ne 'RETURN-COMPLETE-RUNTIME-FACTOR-ISOLATION-PLANNING-ARTIFACTS-FOR-ADJUDICATION') { throw 'Next-action contract drifted.' }

Require-Text $planPath 'TIERING-OFF-QJ-OFF-QJFL-OFF'
Require-Text $planPath 'DOTNET_TieredCompilation'
Require-Text $planPath 'DOTNET_TC_QuickJitForLoops'
Require-Text $planPath 'Dynamic PGO is deliberately not isolated here'
Require-Text $planPath '86 files total'
Require-Text $reviewPath 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $reviewPath 'Windows PowerShell compatibility'
Require-Text $adrPath 'adjacent mode pairs change only'

$artifactDir = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation-planning1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir | Out-Null
$ascii = [Text.Encoding]::ASCII

$contractLines = @(
    'status=PASS-AS-AUTHORED',
    'gate=RP1C-C4-RUNTIME-FACTOR-ISOLATION-PLANNING1',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation-planning1-v1',
    'attribution1-returned-adjudication=PASS',
    'runtime-configuration-sensitivity-confirmed=True',
    'tiering-off-repair-direction-rejected=True',
    'dynamic-pgo-causality-proven=False',
    'future-gate=RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1',
    'future-runtime-modes=4',
    'future-fresh-processes=20',
    'future-total-measured-calls=460800',
    'future-required-files=86',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-threshold=False',
    'pgo-comparator-included=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
)
[IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'),$contractLines,$ascii)

$matrixLines = @('mode_id,DOTNET_TieredCompilation,DOTNET_TieredPGO,DOTNET_TC_QuickJit,DOTNET_TC_QuickJitForLoops,DOTNET_ReadyToRun,contrast_from_previous')
$matrixLines += 'TIERING-OFF-QJ-OFF-QJFL-OFF,0,0,0,0,1,BASELINE'
$matrixLines += 'TIERING-ON-QJ-OFF-QJFL-OFF,1,0,0,0,1,DOTNET_TieredCompilation'
$matrixLines += 'TIERING-ON-QJ-ON-QJFL-OFF,1,0,1,0,1,DOTNET_TC_QuickJit'
$matrixLines += 'TIERING-ON-QJ-ON-QJFL-ON,1,0,1,1,1,DOTNET_TC_QuickJitForLoops'
[IO.File]::WriteAllLines((Join-Path $artifactDir '02-factor-isolation-matrix.csv'),$matrixLines,$ascii)

$summaryLines = @(
    'status=PASS-RUNTIME-FACTOR-ISOLATION-PLANNING-CONTRACT-FROZEN',
    'selection-result=NOT-PERFORMED',
    'runtime-factor-isolation-implementation-contained=False',
    'runtime-factor-isolation-implementation-authorized-now=False',
    'pgo-comparator-authorized=False',
    'single-factor-contrasts=3',
    'next-action=RETURN-COMPLETE-RUNTIME-FACTOR-ISOLATION-PLANNING-ARTIFACTS-FOR-ADJUDICATION',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'threshold-change-authorized=False'
)
[IO.File]::WriteAllLines((Join-Path $artifactDir '03-planning-summary.txt'),$summaryLines,$ascii)

$reviewLines = @(
    'status=STATIC-PREEXECUTION-REVIEW-PASS',
    'finding-1=ATTRIBUTION1-CONFIRMS-RUNTIME-SENSITIVITY-BUT-LARGEST-CONTRAST-IS-NOT-SINGLE-FACTOR',
    'finding-2=TIERING-OFF-REPAIR-DIRECTION-REJECTED',
    'finding-3=DYNAMIC-PGO-CAUSALITY-NOT-PROVEN',
    'correction=SINGLE-FACTOR-CHAIN-TIERING-THEN-QUICKJIT-THEN-QUICKJITFORLOOPS',
    'future-processes=4-MODES-X-5=20',
    'future-measured-calls=460800',
    'future-required-files=86',
    'pgo-follow-up=SEPARATE-PLANNING-ONLY-IF-STILL-MATERIAL',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
)
[IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'),$reviewLines,$ascii)

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 RUNTIME FACTOR ISOLATION PLANNING 1'
Write-Host '============================================================'
Write-Host 'Planning only on validated returned Attribution 1 evidence.'
Write-Host 'No Runtime Factor Isolation implementation, PGO comparator, RP1C selection or production/runtime change.'
Write-Host ''
Write-Host 'Runtime Factor Isolation Planning 1 static audit: PASS-AS-AUTHORED'
Write-Host ('Artifacts: {0}' -f $artifactDir)
