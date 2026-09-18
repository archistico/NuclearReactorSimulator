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
    throw 'RP1B Refinement 3 validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 3'
Write-Host '============================================================'
Write-Host 'C3 unchanged performance-tail attribution and full worst-case qualification.'
Write-Host 'No C4, RP1C selection, production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$returnedRefinement2 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_UserReturnedSummary.txt'
Require-Text $returnedRefinement2 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT2'
Require-Text $returnedRefinement2 'status=VALIDATED-EVIDENCE-MATRIX'
Require-Text $returnedRefinement2 'returned-artifact-set=COMPLETE-9-OF-9'
Require-Text $returnedRefinement2 'c3-all-frozen-points-resolved=True'
Require-Text $returnedRefinement2 'c3-exact-v9-phase-agreement-percent=100'
Require-Text $returnedRefinement2 'c3-seam-unresolved=0'
Require-Text $returnedRefinement2 'c3-seam-phase-mismatch=0'
Require-Text $returnedRefinement2 'c3-resolve-max-us=823.4'
Require-Text $returnedRefinement2 'c3-seam-max-us=175.1'
Require-Text $returnedRefinement2 'c3-performance-ceiling-met=False'
Require-Text $returnedRefinement2 'd3-resolve-max-us=208.5'
Require-Text $returnedRefinement2 'd3-seam-max-us=16504.9'
Require-Text $returnedRefinement2 'engineering-review=D3-RECORDED-ELIGIBILITY-INCOMPLETE-WORST-CASE-PREDICATE-C3-PERFORMANCE-TAIL-ATTRIBUTION-REQUIRED'
Require-Text $returnedRefinement2 'refinement3-authorized=C3-UNCHANGED-PERFORMANCE-TAIL-ATTRIBUTION-ONLY'
Require-Text $returnedRefinement2 'c4-authorized=False'
Require-Text $returnedRefinement2 'rp1c-selection-authorized=False'
Require-Text $returnedRefinement2 'production-src-change-authorized=False'
Require-Text $returnedRefinement2 'next-gate=RP1B-REFINEMENT3-C3-PERFORMANCE-TAIL-ATTRIBUTION-QUALIFICATION'

$frozenRefinement2 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts'
$refinement2Expected = @(
    '01-contract-and-provenance.txt',
    '02-candidate-vr2-error-map.csv',
    '03-candidate-exact-v9-node-map.csv',
    '04-candidate-seam-map.csv',
    '05-candidate-hydraulic-replay.csv',
    '06-candidate-performance.csv',
    '07-candidate-complexity.csv',
    '08-candidate-summary.csv',
    '09-rp1b-refinement2-summary.txt'
)
foreach ($name in $refinement2Expected) { Require-File (Join-Path $frozenRefinement2 $name) }
Require-Text (Join-Path $frozenRefinement2 '09-rp1b-refinement2-summary.txt') 'status=PASS-EVIDENCE-MATRIX-COMPLETE'
Require-Text (Join-Path $frozenRefinement2 '09-rp1b-refinement2-summary.txt') 'c3-vapor-seam-complete-surrogate-rp1c-selection-eligible=False'
Require-Text (Join-Path $frozenRefinement2 '09-rp1b-refinement2-summary.txt') 'd3-vapor-seam-complete-if97-comparator-rp1c-selection-eligible=True'
Require-Text (Join-Path $frozenRefinement2 '09-rp1b-refinement2-summary.txt') 'rp1c-selection-performed=False'

$refVr2 = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '02-candidate-vr2-error-map.csv')))
$refNodes = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '03-candidate-exact-v9-node-map.csv')))
$refSeams = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '04-candidate-seam-map.csv')))
$refHydraulics = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '05-candidate-hydraulic-replay.csv')))
$refPerformance = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '06-candidate-performance.csv')))
$refComplexity = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '07-candidate-complexity.csv')))
$refSummary = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement2 '08-candidate-summary.csv')))
if ($refVr2.Count -ne 80) { throw 'Frozen Refinement 2 VR2 row count drifted.' }
if ($refNodes.Count -ne 720) { throw 'Frozen Refinement 2 node row count drifted.' }
if ($refSeams.Count -ne 2560) { throw 'Frozen Refinement 2 seam row count drifted.' }
if ($refHydraulics.Count -ne 576) { throw 'Frozen Refinement 2 hydraulic row count drifted.' }
if ($refPerformance.Count -ne 2 -or $refComplexity.Count -ne 2 -or $refSummary.Count -ne 2) { throw 'Frozen Refinement 2 summary/performance/complexity count drifted.' }

$c3Performance = @($refPerformance | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' })
$d3Performance = @($refPerformance | Where-Object { $_.candidate_id -eq 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR' })
if ($c3Performance.Count -ne 1 -or $d3Performance.Count -ne 1) { throw 'Frozen Refinement 2 performance candidate identities drifted.' }
Require-NearDouble $c3Performance[0].resolve_median_us 7.2 1e-12 'Frozen C3 median'
Require-NearDouble $c3Performance[0].resolve_p95_us 124.6 1e-12 'Frozen C3 p95'
Require-NearDouble $c3Performance[0].resolve_max_us 823.4 1e-12 'Frozen C3 max'
Require-NearDouble $c3Performance[0].seam_max_us 175.1 1e-12 'Frozen C3 seam max'
Require-NearDouble $d3Performance[0].resolve_max_us 208.5 1e-12 'Frozen D3 max'
Require-NearDouble $d3Performance[0].seam_max_us 16504.9 1e-9 'Frozen D3 seam max'
if ($c3Performance[0].within_frozen_ceilings -ne 'false') { throw 'Frozen C3 performance disposition drifted.' }
if ($d3Performance[0].within_frozen_ceilings -ne 'true') { throw 'Frozen D3 recorded performance disposition drifted.' }

$c3Summary = @($refSummary | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' })
$d3Summary = @($refSummary | Where-Object { $_.candidate_id -eq 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR' })
if ($c3Summary.Count -ne 1 -or $d3Summary.Count -ne 1) { throw 'Frozen Refinement 2 summary candidate identities drifted.' }
if ($c3Summary[0].all_frozen_points_resolved -ne 'true' -or $c3Summary[0].seam_unresolved -ne '0' -or $c3Summary[0].seam_phase_mismatch -ne '0') { throw 'Frozen C3 physical completion drifted.' }
if ($d3Summary[0].all_frozen_points_resolved -ne 'true' -or $d3Summary[0].seam_unresolved -ne '0' -or $d3Summary[0].seam_phase_mismatch -ne '0') { throw 'Frozen D3 physical completion drifted.' }

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-refinement3-contract.json'
Require-File $contractPath
$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-refinement3-v1') { throw 'Unexpected RP1B Refinement 3 schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'RP1B Refinement 3 contract must remain CANDIDATE before returned evidence review.' }
if ($contract.prerequisite.rp1a_status -ne 'VALIDATED' -or $contract.prerequisite.rp1b_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'RP1A/RP1B prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement1_status -ne 'VALIDATED-EVIDENCE-MATRIX' -or $contract.prerequisite.rp1b_refinement2_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'Refinement prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement2_returned_artifact_set -ne 'COMPLETE-9-OF-9') { throw 'Refinement 2 artifact-set prerequisite drifted.' }
if ($contract.prerequisite.engineering_review -ne 'C3-PERFORMANCE-TAIL-ATTRIBUTION-REQUIRED') { throw 'Refinement 3 engineering-review prerequisite drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1B-REFINEMENT3-C3-PERFORMANCE-TAIL-ATTRIBUTION-QUALIFICATION') { throw 'Refinement 3 gate identity drifted.' }
if ($contract.frozen_candidate.id -ne 'C3-VAPOR-SEAM-COMPLETE-SURROGATE') { throw 'C3 identity drifted.' }
if ($contract.frozen_candidate.source_identity -ne 'UNCHANGED-FROM-REFINEMENT2') { throw 'C3 source identity must remain frozen.' }
if ($contract.frozen_candidate.c4_created -ne $false) { throw 'Refinement 3 must not create C4.' }
if ($contract.frozen_candidate.uses_direct_if97_at_resolve_time -ne $false) { throw 'C3 must remain table-only at resolve time.' }
Require-NearDouble $contract.frozen_candidate.refinement2_resolve_max_us 823.4 1e-12 'Contract frozen C3 max'
Require-NearDouble $contract.frozen_candidate.refinement2_seam_max_us 175.1 1e-12 'Contract frozen C3 seam max'
Require-NearDouble $contract.frozen_comparator_observation.refinement2_seam_max_us 16504.9 1e-9 'Contract frozen D3 seam max'
if ($contract.frozen_comparator_observation.d3_is_not_remeasured_by_refinement3 -ne $true) { throw 'D3 must remain frozen observation only.' }
if ($contract.frozen_corpus.exact_v9_node_rows -ne 360 -or $contract.frozen_corpus.seam_rows -ne 1280 -or $contract.frozen_corpus.seam_boundary_count -ne 320) { throw 'Frozen corpus counts drifted.' }
Require-NearDouble $contract.frozen_corpus.seam_pressure_relative_offset 1e-5 1e-15 'Seam pressure relative offset'
Require-NearDouble $contract.frozen_corpus.seam_quality_offset 1e-6 1e-16 'Seam quality offset'
if ($contract.frozen_corpus.rp1a_regenerated -ne $false -or $contract.frozen_corpus.refinement2_regenerated -ne $false) { throw 'Frozen evidence must not be regenerated.' }
if ($contract.performance_protocol.full_corpus_warmup_passes -ne 16 -or $contract.performance_protocol.full_corpus_measured_passes -ne 64) { throw 'Full-corpus timing protocol drifted.' }
if ($contract.performance_protocol.rotation_stride -ne 37) { throw 'Timing rotation stride drifted.' }
if ($contract.performance_protocol.tail_top_state_count -ne 12 -or $contract.performance_protocol.tail_warmup_calls_per_state -ne 32) { throw 'Tail target protocol drifted.' }
if ($contract.performance_protocol.tail_measured_blocks -ne 8 -or $contract.performance_protocol.tail_calls_per_block -ne 64) { throw 'Tail repeat protocol drifted.' }
if ($contract.performance_protocol.seam_warmup_passes -ne 4 -or $contract.performance_protocol.seam_measured_passes -ne 16) { throw 'Seam timing protocol drifted.' }
if ($contract.performance_protocol.strict_single_call_max_is_not_replaced_by_statistical_tail -ne $true -or $contract.performance_protocol.targeted_repeats_are_additional_strict_measurement -ne $true -or $contract.performance_protocol.targeted_repeats_cannot_relax_prior_or_screen_max -ne $true) { throw 'Strict max semantics drifted.' }
if ($contract.performance_protocol.seam_max_is_part_of_full_performance_contract -ne $true) { throw 'Seam max must be part of full performance contract.' }
Require-NearDouble $contract.performance_ceilings.resolve_median_us 94.8 1e-12 'Median performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_p95_us 158.80666666666667 1e-12 'P95 performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_max_us 409.30666666666673 1e-12 'Maximum performance ceiling'
Require-NearDouble $contract.performance_ceilings.seam_max_us 409.30666666666673 1e-12 'Seam maximum performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_median_allocated_bytes 2816 1e-9 'Median allocation ceiling'
if ($contract.full_performance_predicate.exact_v9_single_call_max_must_meet_ceiling -ne $true -or $contract.full_performance_predicate.seam_single_call_max_must_meet_same_max_ceiling -ne $true -or $contract.full_performance_predicate.targeted_repeat_single_call_max_must_meet_same_max_ceiling -ne $true) { throw 'Full worst-case predicate drifted.' }
if ($contract.full_performance_predicate.no_threshold_relaxation -ne $true) { throw 'Performance threshold relaxation is forbidden.' }

$expectedOutputs = @(
    '01-contract-and-provenance.txt',
    '02-c3-exact-v9-state-timing.csv',
    '03-c3-tail-target-repeats.csv',
    '04-c3-seam-side-timing.csv',
    '05-c3-performance-qualification.txt',
    '06-rp1b-refinement3-summary.txt'
)
if (@($contract.outputs).Count -ne $expectedOutputs.Count) { throw 'Refinement 3 output artifact count drifted.' }
for ($index = 0; $index -lt $expectedOutputs.Count; $index++) {
    if ($contract.outputs[$index] -ne $expectedOutputs[$index]) { throw ("Refinement 3 output artifact drifted at index {0}." -f $index) }
}

$authority = $contract.authority
foreach ($property in @(
    'rp1c_selection_authorized_before_returned_refinement3_review',
    'c4_implementation_authorized',
    'production_src_change_authorized',
    'thermodynamic_repair_authorized',
    'thermodynamic_tolerance_change_authorized',
    'exact_v9_change_authorized',
    'existing_closure_mode_reinterpretation_authorized',
    'new_exact_version_activation_authorized',
    'vr3_execution_authorized',
    'p3_r1_execution_authorized',
    'second_replacement_long_authorized')) {
    if ($authority.$property -ne $false) { throw ("Authority flag must remain false: {0}" -f $property) }
}

$frozenRp1a = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts'
foreach ($name in @('03-exact-v9-node-corpus.csv','05-seam-probe-map.csv','06-performance-baseline.csv','07-rp1a-summary.txt')) {
    Require-File (Join-Path $frozenRp1a $name)
}
$nodeRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '03-exact-v9-node-corpus.csv')))
$seamRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '05-seam-probe-map.csv')))
if ($nodeRows.Count -ne 360 -or $seamRows.Count -ne 1280) { throw 'Frozen RP1A timing corpus row count drifted.' }
foreach ($side in @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')) {
    if (@($seamRows | Where-Object { $_.probe_side -eq $side }).Count -ne 320) { throw ("Frozen seam-side count drifted: {0}" -f $side) }
}
if (@($nodeRows | Group-Object -Property probe_id,logical_step,node_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen node keys are not unique.' }
if (@($seamRows | Group-Object -Property boundary_index,probe_side | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen seam keys are not unique.' }

$refinement2CandidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement2ShadowThermodynamicCandidates.cs'
$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement3Tests.cs'
Require-Text $refinement2CandidatePath 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $refinement2CandidatePath 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR'
Require-Text $refinement2CandidatePath 'UsesDirectIf97AtResolveTime => false'
if ((Read-Utf8Text $refinement2CandidatePath).Contains('C4-')) { throw 'Refinement 3 must not introduce C4 into the frozen C3/D3 source file.' }
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement3Tests'
Require-Text $testPath 'Rp1bRefinement3_AttributesAndQualifiesImmutableC3PerformanceTail'
Require-Text $testPath 'FullCorpusMeasuredPasses = 64'
Require-Text $testPath 'TailMeasuredBlocks = 8'
Require-Text $testPath 'TailCallsPerBlock = 64'
Require-Text $testPath 'SeamMeasuredPasses = 16'
Require-Text $testPath 'seamMaximum <= ceilings.ResolveMaximumMicroseconds'
Require-Text $testPath 'targetedMaximum <= ceilings.ResolveMaximumMicroseconds'
Require-Text $testPath 'strict-performance-contract-met='
Require-Text $testPath 'strict-max-not-replaced-by-statistical-tail=True'
Require-Text $testPath '06-rp1b-refinement3-summary.txt'
if ((Read-Utf8Text $testPath).Contains('Assert.Single(') -and (Read-Utf8Text $testPath).Contains('.Where(')) {
    if ((Read-Utf8Text $testPath).Contains('Assert.Single(nodeRows.Where(') -or (Read-Utf8Text $testPath).Contains('Assert.Single(seamRows.Where(')) {
        throw 'xUnit2031-prone Assert.Single filtering pattern reintroduced.'
    }
}

$runnerPath = 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement3.cmd'
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT3=1'
Require-Text $runnerPath 'validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement3.ps1'
Require-Text $runnerPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement3Tests.Rp1bRefinement3_AttributesAndQualifiesImmutableC3PerformanceTail'
Require-Text $runnerPath '--explicit only'
Require-Text $runnerPath '--parallel none'
foreach ($name in $expectedOutputs) { Require-Text $runnerPath $name }

# Enumerate production C# source explicitly. Do not rely on -Include with -LiteralPath:
# Windows PowerShell 5.1 can allow generated/non-C# descendants through that combination.
# Build outputs under bin/obj are repository-local generated artifacts and are not production source.
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
    if ($content.Contains('C3-VAPOR-SEAM-COMPLETE-SURROGATE') -or $content.Contains('RP1B-REFINEMENT3') -or $content.Contains('C4-')) {
        throw ("RP1B Refinement 3 identity leaked into production source: {0}" -f $source.FullName)
    }
}

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Read-Utf8Text $modePath).Contains('ReferenceConsistent')) { throw 'RP1B Refinement 3 must not add a production reference-consistent closure mode.' }
$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 3 static audit: PASS'
