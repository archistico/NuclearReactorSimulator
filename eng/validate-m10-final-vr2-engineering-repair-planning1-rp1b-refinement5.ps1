$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Read-Utf8Text([string]$Path) {
    Require-File $Path
    try {
        $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
        $content = [System.IO.File]::ReadAllText($resolvedPath, [System.Text.Encoding]::UTF8)
        if ($null -eq $content) { throw 'ReadAllText returned null.' }
        return [string]$content
    }
    catch {
        throw ("Failed to read UTF-8 text file {0}: {1}" -f $Path, $_.Exception.Message)
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    $content = Read-Utf8Text $Path
    if (-not $content.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

function Require-NearDouble([object]$Actual, [double]$Expected, [double]$Tolerance, [string]$Name) {
    $actualValue = [double]$Actual
    if ([double]::IsNaN($actualValue) -or [double]::IsInfinity($actualValue)) {
        throw ("{0} is not finite." -f $Name)
    }
    if ([Math]::Abs($actualValue - $Expected) -gt $Tolerance) {
        throw ("{0} drifted. Actual={1:R}; Expected={2:R}; Tolerance={3:R}." -f $Name, $actualValue, $Expected, $Tolerance)
    }
}

$validatorBytes = [System.IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'RP1B Refinement 5 validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 5'
Write-Host '============================================================'
Write-Host 'Immutable C3 cross-process R1-seam wall-clock tail reproducibility only.'
Write-Host 'Five independent test processes; no C4 implementation, RP1C selection or production repair.'
Write-Host ''

$returnedAudit = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_PerformanceReplanning1_UserReturnedAudit.txt'
Require-Text $returnedAudit 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-PERFORMANCE-REPLANNING1'
Require-Text $returnedAudit 'status=PASS-AS-AUTHORED'
Require-Text $returnedAudit 'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $returnedAudit 'candidate-source-changed=False'
Require-Text $returnedAudit 'same-boundary-reproduction=False'
Require-Text $returnedAudit 'c4-justified-now=False'
Require-Text $returnedAudit 'rp1c-authorized-now=False'
Require-Text $returnedAudit 'strict-single-call-max-relaxed=False'
Require-Text $returnedAudit 'performance-threshold-changed=False'
Require-Text $returnedAudit 'next-gate=RP1B-REFINEMENT5-C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY'
Require-Text $returnedAudit 'refinement5-independent-process-runs=5'
Require-Text $returnedAudit 'refinement5-candidate-specific-slow-path-rule=SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS'
Require-Text $returnedAudit 'production-src-change-authorized=False'
Require-Text $returnedAudit 'thermodynamic-repair-authorized=False'
Require-Text $returnedAudit 'exact-v9-change-authorized=False'

$planningDoc = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_PERFORMANCE_REPLANNING1.md'
Require-Text $planningDoc 'five independent focused-test process runs'
Require-Text $planningDoc 'same boundary identity exceeds the unchanged maximum ceiling in at least two independent process runs'
Require-Text $planningDoc 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED'
Require-Text $planningDoc 'C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL'
Require-Text $planningDoc 'C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED'

$refinementDoc = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_C3_CROSS_PROCESS_TAIL.md'
Require-Text $refinementDoc 'C3 Cross-Process Wall-Clock Tail Reproducibility'
Require-Text $refinementDoc '5 independent focused-test processes'
Require-Text $refinementDoc '20,480 measured calls per process'
Require-Text $refinementDoc 'SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS'
Require-Text $refinementDoc 'C4 implementation remains unauthorized'
Require-Text $refinementDoc 'RP1C remains unauthorized'

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-refinement5-contract.json'
Require-File $contractPath
$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-refinement5-v1') { throw 'Refinement 5 contract schema drifted.' }
if ($contract.status -ne 'CANDIDATE') { throw 'Refinement 5 contract status drifted.' }
if ($contract.scope -ne 'C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY') { throw 'Refinement 5 scope drifted.' }
if ($contract.prerequisite.performance_replanning1_status -ne 'PASS-AS-AUTHORED') { throw 'Performance replanning prerequisite drifted.' }
if ($contract.prerequisite.candidate -ne 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' -or $contract.prerequisite.candidate_source_unchanged -ne $true) { throw 'C3 identity/source contract drifted.' }
if ($contract.prerequisite.c4_created -ne $false -or $contract.prerequisite.same_boundary_reproduction_before_refinement5 -ne $false) { throw 'Pre-Refinement5 C4/reproduction state drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1B-REFINEMENT5-C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY') { throw 'Refinement 5 gate identity drifted.' }
if ($contract.frozen_corpus.r1_boundary_count -ne 320 -or $contract.frozen_corpus.warmup_passes_per_process -ne 16 -or $contract.frozen_corpus.measured_passes_per_process -ne 64 -or $contract.frozen_corpus.measured_calls_per_process -ne 20480 -or $contract.frozen_corpus.rotation_stride -ne 37) { throw 'Refinement 5 frozen measurement protocol drifted.' }
if ($contract.frozen_corpus.rp1a_regenerated -ne $false -or $contract.frozen_corpus.c3_regenerated -ne $false) { throw 'Refinement 5 must not regenerate RP1A or C3.' }
if ($contract.cross_process_protocol.independent_process_runs -ne 5 -or $contract.cross_process_protocol.fresh_runtime_process_per_run -ne $true -or $contract.cross_process_protocol.runner_owns_process_repetition -ne $true) { throw 'Independent process contract drifted.' }
if ($contract.cross_process_protocol.process_run_index_environment_variable -ne 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX') { throw 'Run-index environment variable drifted.' }
if ($contract.cross_process_protocol.candidate_specific_slow_path_rule -ne 'SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS' -or $contract.cross_process_protocol.same_boundary_minimum_independent_process_runs -ne 2) { throw 'Candidate-specific slow-path rule drifted.' }
if ($contract.cross_process_protocol.record_per_call_boundary_identity -ne $true -or $contract.cross_process_protocol.record_gc_deltas -ne $true -or $contract.cross_process_protocol.record_process_level_max_and_boundary -ne $true -or $contract.cross_process_protocol.evidence_gate_may_pass_with_exceedances -ne $true) { throw 'Refinement 5 evidence semantics drifted.' }
Require-NearDouble $contract.performance_ceilings.resolve_max_us 409.30666666666673 1e-12 'Refinement 5 maximum ceiling'
if ($contract.performance_ceilings.strict_single_call_max_preserved -ne $true -or $contract.performance_ceilings.no_threshold_relaxation -ne $true) { throw 'Strict performance ceiling semantics drifted.' }
Require-NearDouble $contract.historical_evidence.refinement3_r1_max_us 3667.1 1e-9 'Historical Refinement 3 max'
Require-NearDouble $contract.historical_evidence.refinement4_screen_max_us 2620.6 1e-9 'Historical Refinement 4 screen max'
if ($contract.historical_evidence.refinement4_screen_boundary_index -ne 3) { throw 'Historical Refinement 4 screen boundary drifted.' }
Require-NearDouble $contract.historical_evidence.refinement4_targeted_max_us 853.5 1e-9 'Historical Refinement 4 targeted max'
if ($contract.historical_evidence.refinement4_targeted_boundary_index -ne 191 -or $contract.historical_evidence.historical_exceedances_preserved -ne $true) { throw 'Historical Refinement 4 targeted evidence drifted.' }
if ($contract.adjudication.same_boundary_confirmed_classification -ne 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED') { throw 'Same-boundary classification drifted.' }
if ($contract.adjudication.cross_process_isolated_classification -ne 'C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL') { throw 'Isolated-tail classification drifted.' }
if ($contract.adjudication.no_exceedance_classification -ne 'C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED') { throw 'No-exceedance classification drifted.' }
if ($contract.adjudication.c4_planning_justified_only_for_same_boundary_confirmed -ne $true -or $contract.adjudication.c4_implementation_authorized -ne $false -or $contract.adjudication.rp1c_selection_authorized -ne $false) { throw 'Refinement 5 adjudication authority drifted.' }

$expectedProcessOutputs = @('01-process-contract.txt','02-c3-r1-call-timing.csv','03-c3-r1-boundary-summary.csv','04-runtime-context.txt','05-process-summary.txt')
$expectedAggregateOutputs = @('01-contract-and-provenance.txt','02-cross-process-run-summary.csv','03-cross-process-boundary-summary.csv','04-performance-reproducibility-adjudication.txt','05-rp1b-refinement5-summary.txt')
if (@($contract.process_outputs).Count -ne $expectedProcessOutputs.Count) { throw 'Process output count drifted.' }
if (@($contract.aggregate_outputs).Count -ne $expectedAggregateOutputs.Count) { throw 'Aggregate output count drifted.' }
for ($index = 0; $index -lt $expectedProcessOutputs.Count; $index++) {
    if ($contract.process_outputs[$index] -ne $expectedProcessOutputs[$index]) { throw ("Process output drifted at index {0}." -f $index) }
}
for ($index = 0; $index -lt $expectedAggregateOutputs.Count; $index++) {
    if ($contract.aggregate_outputs[$index] -ne $expectedAggregateOutputs[$index]) { throw ("Aggregate output drifted at index {0}." -f $index) }
}

foreach ($property in @('c4_planning_authorized_before_returned_refinement5_review','c4_implementation_authorized','rp1c_selection_authorized','production_src_change_authorized','thermodynamic_repair_authorized','thermodynamic_tolerance_change_authorized','exact_v9_change_authorized','vr3_execution_authorized','p3_r1_execution_authorized','second_replacement_long_authorized')) {
    if ($contract.authority.$property -ne $false) { throw ("Authority flag must remain false: {0}" -f $property) }
}

$frozenRp1a = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts'
foreach ($name in @('05-seam-probe-map.csv','06-performance-baseline.csv','07-rp1a-summary.txt')) { Require-File (Join-Path $frozenRp1a $name) }
$seamRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '05-seam-probe-map.csv')))
if ($seamRows.Count -ne 1280) { throw 'Frozen RP1A seam row count drifted.' }
$r1Rows = @($seamRows | Where-Object { $_.probe_side -eq 'R1-SIDE' })
if ($r1Rows.Count -ne 320) { throw 'Frozen RP1A R1-side row count drifted.' }
if (@($r1Rows | Group-Object -Property boundary_index | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen R1 boundary identities are not unique.' }

$candidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement2ShadowThermodynamicCandidates.cs'
$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.cs'
Require-Text $candidatePath 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $candidatePath 'UsesDirectIf97AtResolveTime => false'
if ((Read-Utf8Text $candidatePath).Contains('C4-')) { throw 'Refinement 5 must not introduce C4 into the frozen C3/D3 source file.' }
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests'
Require-Text $testPath 'Rp1bRefinement5_MeasuresImmutableC3R1SeamAcrossOneIndependentProcessRun'
Require-Text $testPath 'ScreenWarmupPasses = 16'
Require-Text $testPath 'ScreenMeasuredPasses = 64'
Require-Text $testPath 'RotationStride = 37'
Require-Text $testPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX'
Require-Text $testPath 'GC.CollectionCount(0)'
Require-Text $testPath '20_480'
$testSource = Read-Utf8Text $testPath
if ($testSource.Contains('Assert.Single(r1Rows.Where(')) { throw 'xUnit2031-prone Assert.Single filtering pattern found.' }
if ($testSource.Contains('Assert.True(seamLines.Any(')) { throw 'xUnit2012-prone collection existence assertion found.' }

$runnerPath = 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement5.cmd'
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5=1'
Require-Text $runnerPath 'for /L %%R in (1,1,5) do ('
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX=%%R'
Require-Text $runnerPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.Rp1bRefinement5_MeasuresImmutableC3R1SeamAcrossOneIndependentProcessRun'
Require-Text $runnerPath '--explicit only'
Require-Text $runnerPath '--parallel none'
Require-Text $runnerPath 'adjudicate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement5.ps1'
foreach ($name in $expectedProcessOutputs) { Require-Text $runnerPath $name }
foreach ($name in $expectedAggregateOutputs) { Require-Text $runnerPath $name }

$adjudicatorPath = 'eng/adjudicate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement5.ps1'
Require-File $adjudicatorPath
$adjudicatorBytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $adjudicatorPath).Path)
if ($adjudicatorBytes | Where-Object { $_ -gt 127 }) { throw 'Refinement 5 adjudicator source must remain ASCII-only.' }
Require-Text $adjudicatorPath 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED'
Require-Text $adjudicatorPath 'C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL'
Require-Text $adjudicatorPath 'C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED'
Require-Text $adjudicatorPath '$confirmed = $runsWithExceedance -ge 2'
Require-Text $adjudicatorPath 'c4-implementation-authorized=False'
Require-Text $adjudicatorPath 'rp1c-selection-authorized=False'

$srcFiles = @(Get-ChildItem -LiteralPath 'src' -Recurse -File | Where-Object {
    $_.Extension -eq '.cs' -and
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
})
if ($srcFiles.Count -eq 0) { throw 'Production C# source scan returned zero files.' }
foreach ($source in $srcFiles) {
    if ($source.Extension -ne '.cs' -or $source.FullName -match '[\\/](bin|obj)[\\/]') {
        throw ("Generated/non-C# file entered production source scan: {0}" -f $source.FullName)
    }
    $content = Read-Utf8Text $source.FullName
    if ($content.Contains('C3-VAPOR-SEAM-COMPLETE-SURROGATE') -or $content.Contains('RP1B-REFINEMENT5') -or $content.Contains('C4-')) {
        throw ("RP1B Refinement 5 identity leaked into production source: {0}" -f $source.FullName)
    }
}

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Read-Utf8Text $modePath).Contains('ReferenceConsistent')) { throw 'Refinement 5 must not add a production reference-consistent closure mode.' }
$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 5 static audit: PASS'
