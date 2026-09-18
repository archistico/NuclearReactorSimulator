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
    throw 'RP1B Refinement 2 validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 2'
Write-Host '============================================================'
Write-Host 'Test-only C3/D3 vapor-side seam completion against immutable RP1A and frozen RP1B evidence.'
Write-Host 'No RP1C selection, production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$returnedRefinement1 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_UserReturnedSummary.txt'
Require-Text $returnedRefinement1 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT1'
Require-Text $returnedRefinement1 'status=VALIDATED-EVIDENCE-MATRIX'
Require-Text $returnedRefinement1 'returned-artifact-set=COMPLETE-9-OF-9'
Require-Text $returnedRefinement1 'c2-vr2-unresolved=0'
Require-Text $returnedRefinement1 'c2-exact-v9-unresolved=0'
Require-Text $returnedRefinement1 'c2-seam-unresolved=260'
Require-Text $returnedRefinement1 'c2-seam-phase-mismatch=55'
Require-Text $returnedRefinement1 'c2-r4-vapor-side-unresolved=2'
Require-Text $returnedRefinement1 'c2-r2-side-unresolved=258'
Require-Text $returnedRefinement1 'c2-r2-side-phase-mismatch=55'
Require-Text $returnedRefinement1 'd2-vr2-unresolved=0'
Require-Text $returnedRefinement1 'd2-exact-v9-unresolved=0'
Require-Text $returnedRefinement1 'd2-seam-unresolved=310'
Require-Text $returnedRefinement1 'd2-seam-phase-mismatch=0'
Require-Text $returnedRefinement1 'd2-r4-vapor-side-unresolved=310'
Require-Text $returnedRefinement1 'engineering-review=VAPOR-SIDE-SEAM-COMPLETION-REQUIRED'
Require-Text $returnedRefinement1 'refinement2-authorized=C3-D3-TEST-ONLY'
Require-Text $returnedRefinement1 'rp1c-selection-authorized=False'
Require-Text $returnedRefinement1 'production-src-change-authorized=False'
Require-Text $returnedRefinement1 'next-gate=RP1B-REFINEMENT2-C3-D3-VAPOR-SIDE-SEAM-COMPLETION'

$frozenRefinement1 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_Artifacts'
$refinement1Expected = @(
    '01-contract-and-provenance.txt',
    '02-candidate-vr2-error-map.csv',
    '03-candidate-exact-v9-node-map.csv',
    '04-candidate-seam-map.csv',
    '05-candidate-hydraulic-replay.csv',
    '06-candidate-performance.csv',
    '07-candidate-complexity.csv',
    '08-candidate-summary.csv',
    '09-rp1b-refinement1-summary.txt'
)
foreach ($name in $refinement1Expected) { Require-File (Join-Path $frozenRefinement1 $name) }
Require-Text (Join-Path $frozenRefinement1 '09-rp1b-refinement1-summary.txt') 'status=PASS-EVIDENCE-MATRIX-COMPLETE'
Require-Text (Join-Path $frozenRefinement1 '09-rp1b-refinement1-summary.txt') 'c2-extended-tabulated-surrogate-rp1c-selection-eligible=False'
Require-Text (Join-Path $frozenRefinement1 '09-rp1b-refinement1-summary.txt') 'd2-seam-complete-if97-comparator-rp1c-selection-eligible=False'
Require-Text (Join-Path $frozenRefinement1 '09-rp1b-refinement1-summary.txt') 'rp1c-selection-performed=False'

$refVr2 = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '02-candidate-vr2-error-map.csv')))
$refNodes = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '03-candidate-exact-v9-node-map.csv')))
$refSeams = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '04-candidate-seam-map.csv')))
$refHydraulics = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '05-candidate-hydraulic-replay.csv')))
$refPerformance = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '06-candidate-performance.csv')))
$refComplexity = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '07-candidate-complexity.csv')))
$refSummary = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement1 '08-candidate-summary.csv')))
if ($refVr2.Count -ne 80) { throw 'Frozen Refinement 1 VR2 row count drifted.' }
if ($refNodes.Count -ne 720) { throw 'Frozen Refinement 1 node row count drifted.' }
if ($refSeams.Count -ne 2560) { throw 'Frozen Refinement 1 seam row count drifted.' }
if ($refHydraulics.Count -ne 576) { throw 'Frozen Refinement 1 hydraulic row count drifted.' }
if ($refPerformance.Count -ne 2 -or $refComplexity.Count -ne 2 -or $refSummary.Count -ne 2) { throw 'Frozen Refinement 1 summary/performance/complexity count drifted.' }
$c2 = @($refSummary | Where-Object { $_.candidate_id -eq 'C2-EXTENDED-TABULATED-SURROGATE' })
$d2 = @($refSummary | Where-Object { $_.candidate_id -eq 'D2-SEAM-COMPLETE-IF97-COMPARATOR' })
if ($c2.Count -ne 1 -or $d2.Count -ne 1) { throw 'Frozen Refinement 1 candidate identities drifted.' }
if ([int]$c2[0].vr2_unresolved -ne 0 -or [int]$c2[0].exact_v9_unresolved -ne 0 -or [int]$c2[0].seam_unresolved -ne 260 -or [int]$c2[0].seam_phase_mismatch -ne 55) { throw 'Frozen C2 result drifted.' }
if ([int]$d2[0].vr2_unresolved -ne 0 -or [int]$d2[0].exact_v9_unresolved -ne 0 -or [int]$d2[0].seam_unresolved -ne 310 -or [int]$d2[0].seam_phase_mismatch -ne 0) { throw 'Frozen D2 result drifted.' }
if ($c2[0].planning_target_met -ne 'true' -or $c2[0].performance_ceiling_met -ne 'true' -or $c2[0].rp1c_selection_eligible -ne 'false') { throw 'Frozen C2 qualification drifted.' }
if ($d2[0].planning_target_met -ne 'true' -or $d2[0].performance_ceiling_met -ne 'true' -or $d2[0].rp1c_selection_eligible -ne 'false') { throw 'Frozen D2 qualification drifted.' }

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-refinement2-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-refinement2-v1') { throw 'Unexpected RP1B Refinement 2 schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'RP1B Refinement 2 contract must remain CANDIDATE before returned evidence review.' }
if ($contract.prerequisite.rp1a_status -ne 'VALIDATED') { throw 'RP1A prerequisite drifted.' }
if ($contract.prerequisite.rp1b_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'RP1B prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement1_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'Refinement 1 prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement1_returned_artifact_set -ne 'COMPLETE-9-OF-9') { throw 'Refinement 1 artifact-set prerequisite drifted.' }
if ($contract.prerequisite.engineering_review -ne 'VAPOR-SIDE-SEAM-COMPLETION-REQUIRED') { throw 'Refinement 2 engineering-review prerequisite drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1B-REFINEMENT2-C3-D3-VAPOR-SIDE-SEAM-COMPLETION') { throw 'Refinement 2 gate identity drifted.' }
if ($contract.frozen_corpus.vr2_rows -ne 40 -or $contract.frozen_corpus.vr2_inverse_applicable_rows -ne 39 -or $contract.frozen_corpus.vr2_boundary_only_rows -ne 1) { throw 'Frozen VR2 corpus count drifted.' }
if ($contract.frozen_corpus.exact_v9_node_rows -ne 360 -or $contract.frozen_corpus.hydraulic_rows -ne 288 -or $contract.frozen_corpus.seam_rows -ne 1280 -or $contract.frozen_corpus.seam_boundary_count -ne 320) { throw 'Frozen RP1A corpus count drifted.' }
Require-NearDouble $contract.frozen_corpus.seam_pressure_relative_offset 1e-5 1e-15 'Seam pressure relative offset'
Require-NearDouble $contract.frozen_corpus.seam_quality_offset 1e-6 1e-16 'Seam quality offset'
if ($contract.frozen_corpus.seam_offsets_changed -ne $false -or $contract.frozen_corpus.rp1a_regenerated -ne $false) { throw 'Frozen corpus must remain immutable.' }
if (@($contract.candidates).Count -ne 2) { throw 'Refinement 2 candidate count drifted.' }
if ($contract.candidates[0].id -ne 'C3-VAPOR-SEAM-COMPLETE-SURROGATE') { throw 'C3 identity drifted.' }
if ($contract.candidates[1].id -ne 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR') { throw 'D3 identity drifted.' }
if (@($contract.candidates | Where-Object { $_.implementation_scope -ne 'TEST-ONLY' }).Count -ne 0) { throw 'C3/D3 must remain TEST-ONLY.' }
if ($contract.candidates[0].uses_direct_if97_at_resolve_time -ne $false) { throw 'C3 must not use direct IF97 at resolve time.' }
if ($contract.candidates[1].uses_direct_if97_at_resolve_time -ne $true) { throw 'D3 direct IF97 comparator identity drifted.' }
if ($contract.candidates[0].retunes_c2_in_place -ne $false -or $contract.candidates[1].retunes_d2_in_place -ne $false) { throw 'C2/D2 must remain immutable evidence.' }
Require-NearDouble $contract.comparison.existing_vr2_blocking_ceiling_fraction 0.25 1e-12 'VR2 blocking ceiling'
Require-NearDouble $contract.comparison.planning_target_fraction 0.1 1e-12 'Planning target'
Require-NearDouble $contract.comparison.exact_v9_phase_agreement_target_percent 100 1e-12 'Exact-v9 phase agreement target'
Require-NearDouble $contract.comparison.hydraulic_replay_frozen_law_self_check_tolerance_kg_s 1e-9 1e-18 'Hydraulic replay self-check tolerance'
if ($contract.comparison.candidate_warmup_passes -ne 2 -or $contract.comparison.candidate_measured_passes -ne 8) { throw 'Refinement 2 performance protocol drifted.' }
if ($contract.comparison.all_1280_seam_probes_must_resolve_for_selection_eligibility -ne $true) { throw 'All-seam resolution selection requirement drifted.' }
if ($contract.comparison.zero_seam_phase_mismatch_required_for_selection_eligibility -ne $true) { throw 'Zero seam phase mismatch selection requirement drifted.' }
if ($contract.comparison.seam_coordinates_and_offsets_are_immutable -ne $true) { throw 'Seam coordinates must remain immutable.' }
if ($contract.comparison.first_generation_and_refinement1_results_are_frozen_not_recomputed -ne $true) { throw 'Prior evidence must remain frozen.' }
Require-NearDouble $contract.performance_ceilings.resolve_median_us 94.8 1e-12 'Median performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_p95_us 158.80666666666667 1e-12 'P95 performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_max_us 409.30666666666673 1e-12 'Maximum performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_median_allocated_bytes 2816 1e-9 'Median allocation ceiling'

$expectedOutputs = @(
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
if (@($contract.outputs).Count -ne $expectedOutputs.Count) { throw 'Refinement 2 output artifact count drifted.' }
for ($index = 0; $index -lt $expectedOutputs.Count; $index++) {
    if ($contract.outputs[$index] -ne $expectedOutputs[$index]) { throw ("Refinement 2 output artifact drifted at index {0}." -f $index) }
}

$authority = $contract.authority
foreach ($property in @(
    'rp1c_selection_authorized_before_returned_refinement2_review',
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
foreach ($name in @('02-vr2-reference-point-corpus.csv','03-exact-v9-node-corpus.csv','04-hydraulic-context.csv','05-seam-probe-map.csv','06-performance-baseline.csv','07-rp1a-summary.txt')) {
    Require-File (Join-Path $frozenRp1a $name)
}
Require-Text (Join-Path $frozenRp1a '07-rp1a-summary.txt') 'status=PASS'
Require-Text (Join-Path $frozenRp1a '07-rp1a-summary.txt') 'candidate-timing-inspected=False'
$vr2Rows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '02-vr2-reference-point-corpus.csv')))
$nodeRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '03-exact-v9-node-corpus.csv')))
$pathRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '04-hydraulic-context.csv')))
$seamRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '05-seam-probe-map.csv')))
if ($vr2Rows.Count -ne 40 -or $nodeRows.Count -ne 360 -or $pathRows.Count -ne 288 -or $seamRows.Count -ne 1280) { throw 'Frozen RP1A corpus row count drifted.' }
if (@($seamRows | Group-Object -Property boundary_index).Count -ne 320) { throw 'Frozen seam boundary count drifted.' }
foreach ($side in @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')) {
    if (@($seamRows | Where-Object { $_.probe_side -eq $side }).Count -ne 320) { throw ("Frozen seam-side count drifted: {0}" -f $side) }
}
$boundaryOnly = @($vr2Rows | Where-Object { [string]::IsNullOrWhiteSpace($_.specific_volume_m3_kg) -or [string]::IsNullOrWhiteSpace($_.specific_u_j_kg) })
if ($boundaryOnly.Count -ne 1 -or $boundaryOnly[0].point_id -ne 'VR2-SAT-360C-PONLY') { throw 'Frozen VR2 boundary-only row drifted.' }
if (@($nodeRows | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen node corpus contains unresolved reference rows.' }
if (@($pathRows | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen hydraulic corpus contains unresolved reference rows.' }
if (@($nodeRows | Group-Object -Property probe_id,logical_step,node_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen node keys are not unique.' }
if (@($pathRows | Group-Object -Property probe_id,logical_step,path_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen hydraulic keys are not unique.' }
if (@($seamRows | Group-Object -Property boundary_index,probe_side | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen seam keys are not unique.' }

$refinement1CandidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement1ShadowThermodynamicCandidates.cs'
$refinement2CandidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement2ShadowThermodynamicCandidates.cs'
$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement2Tests.cs'
Require-Text $refinement1CandidatePath 'C2-EXTENDED-TABULATED-SURROGATE'
Require-Text $refinement1CandidatePath 'D2-SEAM-COMPLETE-IF97-COMPARATOR'
if ((Read-Utf8Text $refinement1CandidatePath).Contains('C3-VAPOR-SEAM-COMPLETE-SURROGATE') -or (Read-Utf8Text $refinement1CandidatePath).Contains('D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR')) { throw 'Refinement 1 candidate file must remain immutable.' }
Require-Text $refinement2CandidatePath 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $refinement2CandidatePath 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR'
Require-Text $refinement2CandidatePath 'DenseSaturationStepKelvins = 0.02d'
Require-Text $refinement2CandidatePath 'UsesDirectIf97AtResolveTime => false'
Require-Text $refinement2CandidatePath 'UsesDirectIf97AtResolveTime => true'
Require-Text $refinement2CandidatePath 'requireSuperheatedSide: true'
Require-Text $refinement2CandidatePath 'requireSuperheatedSide: false'
Require-Text $refinement2CandidatePath 'TryResolveNearVaporMixture'
Require-Text $refinement2CandidatePath 'MaximumIterativeSolveIterations => 1_700'
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement2Tests'
Require-Text $testPath 'Rp1bRefinement2_ProducesC3D3ShadowEvidenceAgainstFrozenRp1aCorpus'
Require-Text $testPath 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $testPath 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR'
Require-Text $testPath 'vaporSeamCompletionMet = seamUnresolved == 0 && seamPhaseMismatch == 0'
Require-Text $testPath 'VaporSeamCompletionMet'
Require-Text $testPath 'ValidateFrozenHydraulicReplayLaw(nodeRows, hydraulicRows)'
Require-Text $testPath 'maximumFlowError <= 1e-9'
Require-Text $testPath '09-rp1b-refinement2-summary.txt'
Require-Text $testPath 'rp1c-selection-performed=False'
if ((Read-Utf8Text $testPath).Contains('Assert.Single(vr2Rows.Where(')) { throw 'xUnit2031-prone Assert.Single filtering pattern reintroduced.' }

$runnerPath = 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement2.cmd'
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT2=1'
Require-Text $runnerPath 'validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement2.ps1'
Require-Text $runnerPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement2Tests.Rp1bRefinement2_ProducesC3D3ShadowEvidenceAgainstFrozenRp1aCorpus'
Require-Text $runnerPath '--explicit only'
Require-Text $runnerPath '--parallel none'
foreach ($name in $expectedOutputs) { Require-Text $runnerPath $name }

$srcFiles = Get-ChildItem -LiteralPath 'src' -Recurse -File -Include *.cs
foreach ($source in $srcFiles) {
    $content = Read-Utf8Text $source.FullName
    if ($content.Contains('C3-VAPOR-SEAM-COMPLETE-SURROGATE') -or $content.Contains('D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR')) {
        throw ("RP1B Refinement 2 candidate identity leaked into production source: {0}" -f $source.FullName)
    }
}

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Read-Utf8Text $modePath).Contains('ReferenceConsistent')) { throw 'RP1B Refinement 2 must not add a production reference-consistent closure mode.' }
$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 2 static audit: PASS'
