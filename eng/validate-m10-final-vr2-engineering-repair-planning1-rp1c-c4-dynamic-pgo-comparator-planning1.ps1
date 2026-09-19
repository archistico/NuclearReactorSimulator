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
    if ([Math]::Abs($Actual-$Expected) -gt $Tolerance) {
        throw ("{0} drifted: actual={1}; expected={2}" -f $Label,$Actual,$Expected)
    }
}
function Parse-InvDouble([string]$Text,[string]$Label) {
    $value = 0.0
    $style = [Globalization.NumberStyles]::Float -bor [Globalization.NumberStyles]::AllowThousands
    if (-not [double]::TryParse($Text,$style,[Globalization.CultureInfo]::InvariantCulture,[ref]$value)) {
        throw ("Invalid invariant double for {0}: {1}" -f $Label,$Text)
    }
    return $value
}
function Require-NormalizedTextSha256([string]$Path,[string]$Expected,[string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n","`n").Replace("`r","`n")
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($enc.GetBytes($normalized)) } finally { $sha.Dispose() }
    $actual = ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label,$actual,$Expected)
    }
}
function Require-AsciiSource([string]$Path,[string]$Label) {
    Require-File $Path
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    foreach ($b in $bytes) {
        if ($b -gt 127) { throw ("{0} must remain 7-bit ASCII for Windows PowerShell 5.1 source safety: {1}" -f $Label,$Path) }
    }
}
function Get-ByteSha256Hex([string]$Path) {
    Require-File $Path
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash([System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)) } finally { $sha.Dispose() }
    return ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
}
function Require-ByteTreeManifestSha256([string]$Root,[string]$Expected,[string]$Label) {
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { throw ("Required evidence root not found: {0}" -f $Root) }
    $rootFull = (Resolve-Path -LiteralPath $Root).Path
    $separator = [System.IO.Path]::DirectorySeparatorChar.ToString()
    $rootPrefix = $rootFull
    if (-not $rootPrefix.EndsWith($separator)) { $rootPrefix += $separator }
    $paths = [string[]]@(Get-ChildItem -LiteralPath $Root -Recurse -File | ForEach-Object { $_.FullName })
    [Array]::Sort($paths,[StringComparer]::Ordinal)
    $builder = New-Object System.Text.StringBuilder
    foreach ($fullPath in $paths) {
        if (-not $fullPath.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase)) { throw ("Evidence path escaped root: {0}" -f $fullPath) }
        $relative = $fullPath.Substring($rootPrefix.Length).Replace('\','/')
        $fileHash = Get-ByteSha256Hex $fullPath
        [void]$builder.Append($fileHash).Append('  ').Append($relative).Append("`n")
    }
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $manifestHash = $sha.ComputeHash($enc.GetBytes($builder.ToString())) } finally { $sha.Dispose() }
    $actual = ([BitConverter]::ToString($manifestHash)).Replace('-','').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw ("{0} byte-manifest SHA-256 drifted: actual={1}; expected={2}" -f $Label,$actual,$Expected)
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$validatorPath = $MyInvocation.MyCommand.Path
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator-planning1-contract.json'
$adjudicationPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION1_RETURNED_EVIDENCE_ADJUDICATION.md'
$planPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR_PLANNING1.md'
$reviewPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR_PLANNING1_PREEXECUTION_REVIEW.md'
$adrPath = Join-Path $repoRoot 'docs\adr\0202-isolate-dynamic-pgo-with-qjfl-enabled-before-runtime-impact-assessment.md'
$frozenRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeFactorIsolation1_Artifacts'
$rfi01 = Join-Path $frozenRoot '01-contract-and-provenance.txt'
$rfi02 = Join-Path $frozenRoot '02-runtime-factor-run-summary.csv'
$rfi03 = Join-Path $frozenRoot '03-single-factor-contrast-summary.csv'
$rfi05 = Join-Path $frozenRoot '05-runtime-factor-evidence-summary.txt'
$rfi06 = Join-Path $frozenRoot '06-runtime-factor-isolation1-summary.txt'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator-planning1.cmd'

foreach ($path in @($contractPath,$adjudicationPath,$planPath,$reviewPath,$adrPath,$rfi01,$rfi02,$rfi03,$rfi05,$rfi06,$c4Path,$exactPath,$performancePath,$runnerPath)) {
    Require-File $path
}

Require-AsciiSource $validatorPath 'Planning validator source'
Require-AsciiSource $runnerPath 'Planning runner source'

$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator-planning1-v1') { throw 'Contract schema drifted.' }
if ($contract.status -ne 'CANDIDATE-PLANNING-ONLY') { throw 'Planning status drifted.' }
if ($contract.gate -ne 'RP1C-C4-DYNAMIC-PGO-COMPARATOR-PLANNING1') { throw 'Planning gate identity drifted.' }

$p = $contract.prerequisite
if ($p.runtime_factor_isolation1_returned_adjudication -ne 'PASS') { throw 'Runtime Factor Isolation 1 returned adjudication must be PASS.' }
if ($p.branch -ne 'A-RUNTIME-SENSITIVE') { throw 'Roadmap branch drifted.' }
Require-True $p.tiered_compilation_material_single_factor_effect 'TieredCompilation material single-factor effect'
Require-False $p.quickjit_rare_tail_causality_promoted 'QuickJit rare-tail causal promotion'
Require-False $p.quickjit_for_loops_material_tail_benefit 'QuickJitForLoops material tail benefit'
Require-False $p.dynamic_pgo_qjfl_on_resolved 'Dynamic PGO QJFL-on resolution'
Require-True $p.dynamic_pgo_comparator_material 'Dynamic PGO comparator materiality'
Require-True $p.c4_candidate_immutable 'C4 immutability'
Require-True $p.exact_v9_corpus_immutable 'Exact-v9 immutability'
Require-Near ([double]$p.strict_max_us) 409.30666666666673 0.0000000001 'Strict maximum'
Require-False $p.rp1c_selection_authorized 'RP1C selection prerequisite'

$files = @(Get-ChildItem -LiteralPath $frozenRoot -Recurse -File)
if ($files.Count -ne 86) { throw ("Frozen Runtime Factor Isolation evidence must contain exactly 86 files; found {0}." -f $files.Count) }
$processDirs = @(Get-ChildItem -LiteralPath $frozenRoot -Directory | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Directory })
if ($processDirs.Count -ne 20) { throw ("Frozen Runtime Factor Isolation evidence must contain 20 process directories; found {0}." -f $processDirs.Count) }
foreach ($dir in $processDirs) {
    $processFiles = @(Get-ChildItem -LiteralPath $dir.FullName -File)
    if ($processFiles.Count -ne 4) { throw ("Each frozen process directory must contain four files: {0}" -f $dir.FullName) }
}
Require-ByteTreeManifestSha256 $frozenRoot $contract.returned_evidence_tree_byte_manifest_sha256 'Frozen Runtime Factor Isolation evidence tree'

$h = $contract.normalized_lf_sha256
Require-NormalizedTextSha256 $adjudicationPath $h.runtime_factor_isolation1_returned_adjudication_doc 'Returned adjudication document'
Require-NormalizedTextSha256 $rfi01 $h.rfi1_contract_and_provenance 'RFI1 contract/provenance'
Require-NormalizedTextSha256 $rfi02 $h.rfi1_run_summary 'RFI1 run summary'
Require-NormalizedTextSha256 $rfi03 $h.rfi1_contrast_summary 'RFI1 contrast summary'
Require-NormalizedTextSha256 $rfi05 $h.rfi1_evidence_summary 'RFI1 evidence summary'
Require-NormalizedTextSha256 $rfi06 $h.rfi1_gate_summary 'RFI1 gate summary'
Require-NormalizedTextSha256 $c4Path $h.c4_candidate 'C4 candidate'
Require-NormalizedTextSha256 $exactPath $h.rp1a_exact_v9_corpus 'RP1A exact-v9 corpus'
Require-NormalizedTextSha256 $performancePath $h.rp1a_performance_baseline 'RP1A performance baseline'

Require-Text $rfi01 'status=PASS-RUNTIME-FACTOR-ISOLATION-EVIDENCE-INTEGRITY'
Require-Text $rfi01 'total-measured-calls=460800'
Require-Text $rfi05 'TIERING-OFF-QJ-OFF-QJFL-OFF: processes=5; calls=115200; calls-over-100us=92203; calls-over-max=20'
Require-Text $rfi05 'TIERING-ON-QJ-OFF-QJFL-OFF: processes=5; calls=115200; calls-over-100us=36; calls-over-max=1'
Require-Text $rfi05 'TIERING-ON-QJ-ON-QJFL-OFF: processes=5; calls=115200; calls-over-100us=21; calls-over-max=0'
Require-Text $rfi05 'TIERING-ON-QJ-ON-QJFL-ON: processes=5; calls=115200; calls-over-100us=21; calls-over-max=0'
Require-Text $rfi06 'classification=RUNTIME-FACTOR-ISOLATION-EVIDENCE-COMPLETE-NO-CAUSAL-PROMOTION'

$runRows = @(Import-Csv -LiteralPath $rfi02)
if ($runRows.Count -ne 20) { throw 'RFI1 run summary must contain 20 rows.' }
$expectedModes = @(
    'TIERING-OFF-QJ-OFF-QJFL-OFF',
    'TIERING-ON-QJ-OFF-QJFL-OFF',
    'TIERING-ON-QJ-ON-QJFL-OFF',
    'TIERING-ON-QJ-ON-QJFL-ON'
)
foreach ($mode in $expectedModes) {
    $rows = @($runRows | Where-Object { $_.mode_id -eq $mode })
    if ($rows.Count -ne 5) { throw ("RFI1 mode {0} must have five process rows." -f $mode) }
    foreach ($row in $rows) {
        if ([int]$row.measured_calls -ne 23040) { throw 'RFI1 measured-call count drifted.' }
        if ([int]$row.unresolved_calls -ne 0) { throw 'RFI1 unresolved calls must be zero.' }
        if ([int]$row.candidate_allocated_bytes -ne 0 -or [int]$row.harness_allocated_bytes -ne 0) { throw 'RFI1 allocation neutrality drifted.' }
        if ([int]$row.gc_gen0 -ne 0 -or [int]$row.gc_gen1 -ne 0 -or [int]$row.gc_gen2 -ne 0) { throw 'RFI1 measured-region GC drifted.' }
    }
}

$contrastRows = @(Import-Csv -LiteralPath $rfi03)
if ($contrastRows.Count -ne 3) { throw 'RFI1 contrast summary must contain exactly three rows.' }
$ab = @($contrastRows | Where-Object { $_.contrast_id -eq 'A-B' })[0]
$bc = @($contrastRows | Where-Object { $_.contrast_id -eq 'B-C' })[0]
$cd = @($contrastRows | Where-Object { $_.contrast_id -eq 'C-D' })[0]
if ($null -eq $ab -or $null -eq $bc -or $null -eq $cd) { throw 'RFI1 contrast identity is incomplete.' }
if ($ab.changed_factor -ne 'DOTNET_TieredCompilation') { throw 'A-B factor drifted.' }
if ([int]$ab.left_calls_over_100us -ne 92203 -or [int]$ab.right_calls_over_100us -ne 36 -or [int]$ab.left_calls_over_max -ne 20 -or [int]$ab.right_calls_over_max -ne 1) { throw 'A-B aggregate evidence drifted.' }
if ($bc.changed_factor -ne 'DOTNET_TC_QuickJit') { throw 'B-C factor drifted.' }
if ([int]$bc.left_calls_over_100us -ne 36 -or [int]$bc.right_calls_over_100us -ne 21 -or [int]$bc.left_calls_over_max -ne 1 -or [int]$bc.right_calls_over_max -ne 0) { throw 'B-C aggregate evidence drifted.' }
if ($cd.changed_factor -ne 'DOTNET_TC_QuickJitForLoops') { throw 'C-D factor drifted.' }
if ([int]$cd.left_calls_over_100us -ne 21 -or [int]$cd.right_calls_over_100us -ne 21 -or [int]$cd.left_calls_over_max -ne 0 -or [int]$cd.right_calls_over_max -ne 0) { throw 'C-D aggregate evidence drifted.' }

$f = $contract.future_gate
if ($f.id -ne 'RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1') { throw 'Future gate identity drifted.' }
Require-True $f.evidence_only 'Future gate evidence-only rule'
Require-False $f.automatic_causal_promotion 'Automatic causal promotion'
Require-False $f.automatic_selection_authorized 'Automatic selection'
if ([int]$f.fresh_processes_per_mode -ne 5 -or [int]$f.mode_count -ne 2 -or [int]$f.total_fresh_processes -ne 10) { throw 'Future process contract drifted.' }
if ([int]$f.exact_v9_rows -ne 360 -or [int]$f.warmup_passes -ne 16 -or [int]$f.measured_passes -ne 64 -or [int]$f.rotation_stride -ne 37) { throw 'Future matrix contract drifted.' }
if ([int]$f.measured_calls_per_process -ne 23040 -or [int]$f.total_measured_calls -ne 230400) { throw 'Future measured-call contract drifted.' }
Require-Near ([double]$f.strict_max_us) 409.30666666666673 0.0000000001 'Future strict maximum'
Require-Near ([double]$f.diagnostic_tail_floor_us) 100.0 0.0000001 'Diagnostic tail floor'
Require-False $f.diagnostic_tail_floor_changes_qualification_threshold 'Diagnostic floor qualification rule'
Require-True $f.allocation_neutral_harness_required 'Allocation-neutral harness'
Require-True $f.counterbalanced_blocked_by_run 'Counterbalanced order'
Require-True $f.returned_evidence_adjudication_required 'Returned adjudication requirement'

$modes = @($contract.runtime_modes)
if ($modes.Count -ne 2) { throw 'Expected exactly two PGO modes.' }
$expectedVars = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
$expected = @(
    @('PGO-OFF-QJFL-ON','1','0','1','1','1'),
    @('PGO-ON-QJFL-ON','1','1','1','1','1')
)
for ($i=0; $i -lt 2; $i++) {
    if ([string]$modes[$i].id -ne $expected[$i][0]) { throw 'PGO mode identity/order drifted.' }
    if ([int]$modes[$i].processes -ne 5) { throw 'Each PGO mode must use five processes.' }
    for ($j=0; $j -lt 5; $j++) {
        $actual = [string]$modes[$i].environment.PSObject.Properties[$expectedVars[$j]].Value
        if ($actual -ne $expected[$i][$j+1]) { throw ("PGO mode environment drifted for {0}." -f $expectedVars[$j]) }
    }
}
if ($contract.contrast.left -ne 'PGO-OFF-QJFL-ON' -or $contract.contrast.right -ne 'PGO-ON-QJFL-ON' -or $contract.contrast.changed_factor -ne 'DOTNET_TieredPGO') {
    throw 'PGO single-factor contrast drifted.'
}
$schedule = @($contract.execution_schedule)
$expectedSchedule = @('OFF1','ON1','ON2','OFF2','OFF3','ON3','ON4','OFF4','OFF5','ON5')
if ($schedule.Count -ne $expectedSchedule.Count) { throw 'PGO execution schedule length drifted.' }
for ($i=0; $i -lt $schedule.Count; $i++) {
    if ([string]$schedule[$i] -ne $expectedSchedule[$i]) { throw 'PGO execution schedule drifted.' }
}

Require-False $contract.deferred_questions.ambient_unset_included 'Ambient mode inclusion'
if ($contract.deferred_questions.effective_default_equivalence_deferred_to -ne 'BRANCH-A2-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT') { throw 'Effective-default deferral drifted.' }
Require-False $contract.deferred_questions.production_runtime_setting_selection_performed 'Runtime setting selection'

$envList = @($contract.caller_environment_preflight.required_unset)
if ($envList.Count -ne 5) { throw 'Caller environment variable count drifted.' }
for ($i=0; $i -lt 5; $i++) {
    if ([string]$envList[$i] -ne $expectedVars[$i]) { throw 'Caller environment identity/order drifted.' }
}
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Parent environment fail-closed rule'

if ([int]$contract.planned_evidence.process_directory_count -ne 10 -or [int]$contract.planned_evidence.files_per_process -ne 4 -or [int]$contract.planned_evidence.aggregate_file_count -ne 6 -or [int]$contract.planned_evidence.total_required_files -ne 46) {
    throw 'Planned evidence tree shape drifted.'
}

$futureTestPath = Join-Path $repoRoot $contract.future_test_contract.file
$futureRunnerPath = Join-Path $repoRoot $contract.future_test_contract.runner
if (Test-Path -LiteralPath $futureTestPath -PathType Leaf) { throw 'Future Dynamic PGO Comparator test is already implemented; planning must remain planning-only.' }
if (Test-Path -LiteralPath $futureRunnerPath -PathType Leaf) { throw 'Future Dynamic PGO Comparator runner is already implemented; planning must remain planning-only.' }
Require-True $contract.future_test_contract.explicit 'Future test explicit flag'
if ($contract.future_test_contract.parallel -ne 'none' -or $contract.future_test_contract.configuration -ne 'Release') { throw 'Future execution configuration drifted.' }

$a = $contract.authority
Require-False $a.dynamic_pgo_comparator_implementation_authorized_now 'Dynamic PGO comparator authority now'
Require-True $a.dynamic_pgo_comparator_implementation_authorizable_after_returned_planning_adjudication 'Dynamic PGO comparator post-adjudication eligibility'
Require-False $a.runtime_configuration_impact_assessment_authorized 'Runtime Configuration Impact Assessment authority'
Require-False $a.rp1c_selection_authorized 'RP1C selection authority'
Require-False $a.production_runtime_change_authorized 'Production runtime change authority'
Require-False $a.production_repair_authorized 'Production repair authority'
Require-False $a.candidate_mutation_authorized 'Candidate mutation authority'
Require-False $a.threshold_change_authorized 'Threshold change authority'
Require-False $a.exact_v9_change_authorized 'Exact-v9 change authority'
Require-False $a.vr3_authorized 'VR3 authority'
Require-False $a.p3_r1_authorized 'P3-R1 authority'
Require-False $a.second_replacement_long_authorized 'Second replacement-long authority'
if ($a.next_action_after_local_planning_pass -ne 'RETURN-COMPLETE-DYNAMIC-PGO-COMPARATOR-PLANNING-ARTIFACTS-FOR-ADJUDICATION') { throw 'Next-action contract drifted.' }

Require-Text $adjudicationPath 'Dynamic PGO remains a'
Require-Text $adjudicationPath 'RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1'
Require-Text $planPath 'PGO-OFF-QJFL-ON'
Require-Text $planPath 'PGO-ON-QJFL-ON'
Require-Text $planPath '230,400 measured calls total'
Require-Text $planPath 'exactly 46 files'
Require-Text $planPath 'Effective-default equivalence belongs to the later Runtime Configuration Impact Assessment'
Require-Text $reviewPath 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $reviewPath 'Windows PowerShell 5.1 compatibility review'
Require-Text $adrPath 'Ambient/unset mode is excluded from this gate'

$artifactDir = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator-planning1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir | Out-Null
$ascii = [Text.Encoding]::ASCII

$contractLines = @(
    'status=PASS-AS-AUTHORED',
    'gate=RP1C-C4-DYNAMIC-PGO-COMPARATOR-PLANNING1',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator-planning1-v1',
    'runtime-factor-isolation1-returned-adjudication=PASS',
    'roadmap-branch=A-RUNTIME-SENSITIVE',
    'tiered-compilation-material-single-factor-effect=True',
    'quickjit-rare-tail-causality-promoted=False',
    'quickjit-for-loops-material-tail-benefit=False',
    'dynamic-pgo-comparator-material=True',
    'future-gate=RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1',
    'future-runtime-modes=2',
    'future-fresh-processes=10',
    'future-total-measured-calls=230400',
    'future-required-files=46',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-threshold=False',
    'ambient-mode-included=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
)
[IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'),$contractLines,$ascii)

$matrixLines = @('mode_id,DOTNET_TieredCompilation,DOTNET_TieredPGO,DOTNET_TC_QuickJit,DOTNET_TC_QuickJitForLoops,DOTNET_ReadyToRun,contrast_from_previous')
$matrixLines += 'PGO-OFF-QJFL-ON,1,0,1,1,1,BASELINE'
$matrixLines += 'PGO-ON-QJFL-ON,1,1,1,1,1,DOTNET_TieredPGO'
[IO.File]::WriteAllLines((Join-Path $artifactDir '02-pgo-comparator-matrix.csv'),$matrixLines,$ascii)

$summaryLines = @(
    'status=PASS-DYNAMIC-PGO-COMPARATOR-PLANNING-CONTRACT-FROZEN',
    'selection-result=NOT-PERFORMED',
    'dynamic-pgo-comparator-implementation-contained=False',
    'dynamic-pgo-comparator-implementation-authorized-now=False',
    'runtime-configuration-impact-assessment-authorized=False',
    'single-factor-contrasts=1',
    'next-action=RETURN-COMPLETE-DYNAMIC-PGO-COMPARATOR-PLANNING-ARTIFACTS-FOR-ADJUDICATION',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'threshold-change-authorized=False'
)
[IO.File]::WriteAllLines((Join-Path $artifactDir '03-planning-summary.txt'),$summaryLines,$ascii)

$reviewLines = @(
    'status=STATIC-PREEXECUTION-REVIEW-PASS',
    'finding-1=RUNTIME-FACTOR-ISOLATION1-EVIDENCE-COMPLETE',
    'finding-2=TIERED-COMPILATION-MATERIAL-SINGLE-FACTOR-EFFECT',
    'finding-3=QUICKJIT-RARE-TAIL-CAUSALITY-NOT-PROMOTED',
    'finding-4=QUICKJITFORLOOPS-NO-MATERIAL-TAIL-BENEFIT',
    'remaining-hypothesis=DYNAMIC-PGO-WITH-QJFL-ON',
    'future-processes=2-MODES-X-5=10',
    'future-measured-calls=230400',
    'future-required-files=46',
    'ambient-effective-default=DEFERRED-TO-BRANCH-A2',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
)
[IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'),$reviewLines,$ascii)

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 DYNAMIC PGO COMPARATOR PLANNING 1'
Write-Host '============================================================'
Write-Host 'Planning only on adjudicated Runtime Factor Isolation 1 evidence.'
Write-Host 'No PGO comparator implementation, Runtime Configuration Impact Assessment, RP1C selection or production/runtime change.'
Write-Host ''
Write-Host 'Dynamic PGO Comparator Planning 1 static audit: PASS-AS-AUTHORED'
Write-Host ('Artifacts: {0}' -f $artifactDir)
