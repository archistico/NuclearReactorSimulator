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
        if ($null -eq $content) {
            throw 'ReadAllText returned null.'
        }
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
    throw 'RP1B Refinement 1 validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 1'
Write-Host '============================================================'
Write-Host 'Test-only C2/D2 refinement against immutable RP1A and frozen RP1B evidence.'
Write-Host 'No RP1C selection, production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$attempt1 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_Attempt1_PreflightRedSummary.txt'
Require-Text $attempt1 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT1'
Require-Text $attempt1 'attempt=1'
Require-Text $attempt1 'status=STATIC-PREFLIGHT-RED'
Require-Text $attempt1 'ordinary-build-started=False'
Require-Text $attempt1 'focused-refinement1-started=False'
Require-Text $attempt1 'candidate-evidence-produced=False'
Require-Text $attempt1 'failure-owner=VALIDATOR-EXACT-FLOAT-COMPARISON'
Require-Text $attempt1 'rp1c-selection-authorized=False'
Require-Text $attempt1 'production-src-change-authorized=False'

$returnedRp1b = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_UserReturnedSummary.txt'
Require-Text $returnedRp1b 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B'
Require-Text $returnedRp1b 'status=VALIDATED-EVIDENCE-MATRIX'
Require-Text $returnedRp1b 'returned-artifact-set=COMPLETE-9-OF-9'
Require-Text $returnedRp1b 'engineering-review=NO-FIRST-GENERATION-CANDIDATE-SELECTABLE'
Require-Text $returnedRp1b 'refinement-authorized=C2-D2-TEST-ONLY'
Require-Text $returnedRp1b 'c1-exact-v9-unresolved=12'
Require-Text $returnedRp1b 'c1-unresolved-owner=feedwater-inventory'
Require-Text $returnedRp1b 'd1-planning-target-met=True'
Require-Text $returnedRp1b 'd1-performance-ceiling-met=False'
Require-Text $returnedRp1b 'rp1c-selection-authorized=False'
Require-Text $returnedRp1b 'production-src-change-authorized=False'
Require-Text $returnedRp1b 'thermodynamic-repair-authorized=False'
Require-Text $returnedRp1b 'thermodynamic-tolerance-change-authorized=False'
Require-Text $returnedRp1b 'exact-v9-change-authorized=False'
Require-Text $returnedRp1b 'vr3-authorized=False'
Require-Text $returnedRp1b 'p3-r1-authorized=False'
Require-Text $returnedRp1b 'second-replacement-long-authorized=False'
Require-Text $returnedRp1b 'next-gate=RP1B-REFINEMENT1-C2-D2-SHADOW-MATRIX'

$frozenRp1b = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts'
$rp1bExpected = @(
    '01-contract-and-provenance.txt',
    '02-candidate-vr2-error-map.csv',
    '03-candidate-exact-v9-node-map.csv',
    '04-candidate-seam-map.csv',
    '05-candidate-hydraulic-replay.csv',
    '06-candidate-performance.csv',
    '07-candidate-complexity.csv',
    '08-candidate-summary.csv',
    '09-rp1b-summary.txt'
)
foreach ($name in $rp1bExpected) { Require-File (Join-Path $frozenRp1b $name) }
Require-Text (Join-Path $frozenRp1b '09-rp1b-summary.txt') 'status=PASS-EVIDENCE-MATRIX-COMPLETE'
Require-Text (Join-Path $frozenRp1b '09-rp1b-summary.txt') 'c1-tabulated-surrogate-rp1c-selection-eligible=False'
Require-Text (Join-Path $frozenRp1b '09-rp1b-summary.txt') 'd1-bounded-if97-subset-rp1c-selection-eligible=False'
Require-Text (Join-Path $frozenRp1b '09-rp1b-summary.txt') 'rp1c-selection-performed=False'
Require-Text (Join-Path $frozenRp1b '09-rp1b-summary.txt') 'production-src-change-authorized=False'

$frozenRp1bVr2 = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '02-candidate-vr2-error-map.csv')))
$frozenRp1bNodes = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '03-candidate-exact-v9-node-map.csv')))
$frozenRp1bSeams = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '04-candidate-seam-map.csv')))
$frozenRp1bHydraulics = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '05-candidate-hydraulic-replay.csv')))
$frozenRp1bPerformance = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '06-candidate-performance.csv')))
$frozenRp1bComplexity = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '07-candidate-complexity.csv')))
if ($frozenRp1bVr2.Count -ne 120) { throw 'Frozen RP1B VR2 matrix row count drifted.' }
if ($frozenRp1bNodes.Count -ne 1080) { throw 'Frozen RP1B node matrix row count drifted.' }
if ($frozenRp1bSeams.Count -ne 3840) { throw 'Frozen RP1B seam matrix row count drifted.' }
if ($frozenRp1bHydraulics.Count -ne 864) { throw 'Frozen RP1B hydraulic matrix row count drifted.' }
if ($frozenRp1bPerformance.Count -ne 3 -or $frozenRp1bComplexity.Count -ne 3) { throw 'Frozen RP1B performance/complexity row count drifted.' }
$expectedFirstGenerationIds = @('B1-PIECEWISE-REDUCED','C1-TABULATED-SURROGATE','D1-BOUNDED-IF97-SUBSET')
foreach ($candidateId in $expectedFirstGenerationIds) {
    if (@($frozenRp1bVr2 | Where-Object { $_.candidate_id -eq $candidateId }).Count -ne 40) { throw ("Frozen RP1B VR2 rows drifted for {0}." -f $candidateId) }
    if (@($frozenRp1bNodes | Where-Object { $_.candidate_id -eq $candidateId }).Count -ne 360) { throw ("Frozen RP1B node rows drifted for {0}." -f $candidateId) }
    if (@($frozenRp1bSeams | Where-Object { $_.candidate_id -eq $candidateId }).Count -ne 1280) { throw ("Frozen RP1B seam rows drifted for {0}." -f $candidateId) }
    if (@($frozenRp1bHydraulics | Where-Object { $_.candidate_id -eq $candidateId }).Count -ne 288) { throw ("Frozen RP1B hydraulic rows drifted for {0}." -f $candidateId) }
}

$firstGenerationSummary = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '08-candidate-summary.csv')))
if ($firstGenerationSummary.Count -ne 3) { throw 'Frozen RP1B candidate-summary row count drifted.' }
$c1 = @($firstGenerationSummary | Where-Object { $_.candidate_id -eq 'C1-TABULATED-SURROGATE' })
$d1 = @($firstGenerationSummary | Where-Object { $_.candidate_id -eq 'D1-BOUNDED-IF97-SUBSET' })
if ($c1.Count -ne 1 -or $d1.Count -ne 1) { throw 'Frozen RP1B C1/D1 summary identities drifted.' }
if ([int]$c1[0].exact_v9_unresolved -ne 12) { throw 'Frozen C1 exact-v9 unresolved count drifted.' }
if ($c1[0].performance_ceiling_met -ne 'true') { throw 'Frozen C1 performance result drifted.' }
if ($c1[0].rp1c_selection_eligible -ne 'false') { throw 'Frozen C1 selection result drifted.' }
if ($d1[0].planning_target_met -ne 'true') { throw 'Frozen D1 planning-target result drifted.' }
if ($d1[0].performance_ceiling_met -ne 'false') { throw 'Frozen D1 performance result drifted.' }
if ($d1[0].rp1c_selection_eligible -ne 'false') { throw 'Frozen D1 selection result drifted.' }

$frozenC1Nodes = @((Import-Csv -LiteralPath (Join-Path $frozenRp1b '03-candidate-exact-v9-node-map.csv')) | Where-Object { $_.candidate_id -eq 'C1-TABULATED-SURROGATE' })
$c1Unresolved = @($frozenC1Nodes | Where-Object { $_.candidate_resolved -ne 'true' })
if ($frozenC1Nodes.Count -ne 360 -or $c1Unresolved.Count -ne 12) { throw 'Frozen C1 node evidence drifted.' }
if (@($c1Unresolved | Where-Object { $_.node_id -ne 'feedwater-inventory' }).Count -ne 0) { throw 'Frozen C1 unresolved ownership drifted away from feedwater-inventory.' }

$rp1aContractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1a-contract.json'
Require-File $rp1aContractPath
$rp1a = Get-Content -LiteralPath $rp1aContractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($rp1a.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1a-v1') { throw 'Unexpected RP1A schema.' }
if ($rp1a.status -ne 'VALIDATED') { throw 'RP1A must remain VALIDATED.' }
if ($rp1a.returned_review.status -ne 'VALIDATED') { throw 'Returned RP1A review status drifted.' }
if ($rp1a.returned_review.returned_artifact_set -ne 'COMPLETE-7-OF-7') { throw 'Returned RP1A artifact-set status drifted.' }
if ($rp1a.returned_review.rp1b_test_only_shadow_candidate_matrix_authorized -ne $true) { throw 'Returned RP1A RP1B authorization drifted.' }
if ($rp1a.returned_review.rp1c_selection_authorized -ne $false) { throw 'Returned RP1A must not authorize RP1C.' }
if ($rp1a.returned_review.production_src_change_authorized -ne $false) { throw 'Returned RP1A must not authorize production changes.' }

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-refinement1-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-refinement1-v1') { throw 'Unexpected RP1B Refinement 1 schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'RP1B Refinement 1 contract must remain CANDIDATE before returned evidence review.' }
if ($contract.prerequisite.rp1a_status -ne 'VALIDATED') { throw 'RP1A prerequisite status drifted.' }
if ($contract.prerequisite.rp1b_returned_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'Returned RP1B prerequisite drifted.' }
if ($contract.prerequisite.rp1b_returned_artifact_set -ne 'COMPLETE-9-OF-9') { throw 'Returned RP1B artifact-set prerequisite drifted.' }
if ($contract.prerequisite.first_generation_selection_result -ne 'NO-CANDIDATE-SELECTABLE') { throw 'First-generation selection result drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1B-REFINEMENT1-C2-D2-SHADOW-MATRIX') { throw 'RP1B Refinement 1 gate identity drifted.' }
if ($contract.frozen_corpus.vr2_rows -ne 40) { throw 'VR2 row count drifted.' }
if ($contract.frozen_corpus.vr2_inverse_applicable_rows -ne 39) { throw 'VR2 inverse-applicable count drifted.' }
if ($contract.frozen_corpus.vr2_boundary_only_rows -ne 1) { throw 'VR2 boundary-only count drifted.' }
if ($contract.frozen_corpus.exact_v9_node_rows -ne 360) { throw 'Exact-v9 node count drifted.' }
if ($contract.frozen_corpus.hydraulic_rows -ne 288) { throw 'Hydraulic row count drifted.' }
if ($contract.frozen_corpus.seam_rows -ne 1280) { throw 'Seam row count drifted.' }
if ($contract.frozen_corpus.seam_boundary_count -ne 320) { throw 'Seam boundary count drifted.' }
if ($contract.frozen_corpus.seam_offsets_changed -ne $false) { throw 'Seam offsets must remain immutable.' }
if ($contract.frozen_corpus.rp1a_regenerated -ne $false) { throw 'RP1A corpus must not be regenerated by Refinement 1.' }
if (@($contract.candidates).Count -ne 2) { throw 'RP1B Refinement 1 candidate count drifted.' }
if ($contract.candidates[0].id -ne 'C2-EXTENDED-TABULATED-SURROGATE') { throw 'C2 identity drifted.' }
if ($contract.candidates[1].id -ne 'D2-SEAM-COMPLETE-IF97-COMPARATOR') { throw 'D2 identity drifted.' }
if (@($contract.candidates | Where-Object { $_.implementation_scope -ne 'TEST-ONLY' }).Count -ne 0) { throw 'C2/D2 must remain TEST-ONLY.' }
if ($contract.candidates[0].uses_direct_if97_at_resolve_time -ne $false) { throw 'C2 must not use direct IF97 at resolve time.' }
if ($contract.candidates[1].uses_direct_if97_at_resolve_time -ne $true) { throw 'D2 direct IF97 comparator identity drifted.' }
if ($contract.candidates[0].retunes_c1_in_place -ne $false) { throw 'C2 must not retune C1 in place.' }
if ($contract.candidates[1].retunes_d1_in_place -ne $false) { throw 'D2 must not retune D1 in place.' }
if ($contract.frozen_first_generation.b1_selection_eligible -ne $false) { throw 'Frozen B1 selection result drifted.' }
if ($contract.frozen_first_generation.c1_selection_eligible -ne $false) { throw 'Frozen C1 selection result drifted.' }
if ($contract.frozen_first_generation.c1_exact_v9_unresolved_rows -ne 12) { throw 'Frozen C1 unresolved-row count drifted.' }
if ($contract.frozen_first_generation.c1_unresolved_owner -ne 'feedwater-inventory') { throw 'Frozen C1 unresolved owner drifted.' }
if ($contract.frozen_first_generation.d1_selection_eligible -ne $false) { throw 'Frozen D1 selection result drifted.' }
if ($contract.frozen_first_generation.d1_planning_target_met -ne $true) { throw 'Frozen D1 planning-target result drifted.' }
if ($contract.frozen_first_generation.d1_performance_ceiling_met -ne $false) { throw 'Frozen D1 performance result drifted.' }
if ($contract.frozen_first_generation.retuning_in_place_allowed -ne $false) { throw 'First-generation in-place retuning must remain forbidden.' }
Require-NearDouble $contract.comparison.planning_target_fraction 0.1 1e-15 'Planning target fraction'
Require-NearDouble $contract.comparison.existing_vr2_blocking_ceiling_fraction 0.25 1e-15 'VR2 blocking ceiling fraction'
if ($contract.comparison.planning_target_is_not_a_replacement_vr2_tolerance -ne $true) { throw 'Planning target must not replace the VR2 tolerance.' }
Require-NearDouble $contract.comparison.exact_v9_phase_agreement_target_percent 100 1e-12 'Exact-v9 phase agreement target percent'
if ($contract.comparison.candidate_warmup_passes -ne 2 -or $contract.comparison.candidate_measured_passes -ne 8) { throw 'Refinement 1 performance protocol drifted.' }
if ($contract.comparison.candidate_qualification_is_evidence_not_refinement_pass_criterion -ne $true) { throw 'Candidate qualification/evidence separation drifted.' }
if ($contract.comparison.deterministic_repeat_required_for_evidence_completion -ne $true) { throw 'Deterministic-repeat contract drifted.' }
if ($contract.comparison.hydraulic_replay_uses_frozen_attempt5_path_rows -ne $true) { throw 'Frozen hydraulic replay contract drifted.' }
if ($contract.comparison.seam_continuity_uses_all_1280_frozen_probes -ne $true) { throw 'Frozen seam-continuity corpus contract drifted.' }
if ($contract.comparison.seam_coordinates_and_offsets_are_immutable -ne $true) { throw 'Seam coordinates/offsets must remain immutable.' }
if ($contract.comparison.vr2_boundary_only_rows_are_preserved_but_not_scored_as_inverse_closure -ne $true) { throw 'VR2 boundary-only handling contract drifted.' }
Require-NearDouble $contract.comparison.hydraulic_replay_frozen_law_self_check_tolerance_kg_s 1e-9 1e-18 'Hydraulic replay self-check tolerance'
if ($contract.comparison.region2_reference_domain_respects_b23_boundary -ne $true) { throw 'Region-2 B23 domain guard drifted.' }
if ($contract.comparison.first_generation_results_are_frozen_not_recomputed -ne $true) { throw 'First-generation evidence freeze contract drifted.' }
Require-NearDouble $contract.performance_ceilings.resolve_median_us 94.8 1e-12 'Median performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_p95_us 158.80666666666667 1e-12 'P95 performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_max_us 409.30666666666673 1e-12 'Max performance ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_median_allocated_bytes 2816 1e-9 'Allocation ceiling'
if ($contract.performance_ceilings.source -ne 'RP1A-returned-machine-local-freeze') { throw 'Performance ceiling provenance drifted.' }
if ($contract.authority.rp1c_selection_authorized_before_returned_refinement_review -ne $false) { throw 'RP1C cannot be authorized by this candidate.' }
if ($contract.authority.production_src_change_authorized -ne $false) { throw 'Production source changes cannot be authorized.' }
if ($contract.authority.thermodynamic_repair_authorized -ne $false) { throw 'Thermodynamic repair cannot be authorized.' }
if ($contract.authority.thermodynamic_tolerance_change_authorized -ne $false) { throw 'Thermodynamic tolerance change cannot be authorized.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'Exact-v9 cannot be authorized for change.' }
if ($contract.authority.existing_closure_mode_reinterpretation_authorized -ne $false) { throw 'Existing closure reinterpretation cannot be authorized.' }
if ($contract.authority.new_exact_version_activation_authorized -ne $false) { throw 'New exact-version activation cannot be authorized.' }
if ($contract.authority.vr3_execution_authorized -ne $false) { throw 'VR3 cannot be authorized.' }
if ($contract.authority.p3_r1_execution_authorized -ne $false) { throw 'P3-R1 cannot be authorized.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long cannot be authorized.' }
$expectedOutputs = @(
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
if (@($contract.outputs).Count -ne $expectedOutputs.Count) { throw 'Refinement 1 output artifact count drifted.' }
for ($index = 0; $index -lt $expectedOutputs.Count; $index++) {
    if ($contract.outputs[$index] -ne $expectedOutputs[$index]) { throw ("Refinement 1 output artifact drifted at index {0}." -f $index) }
}

$frozen = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts'
foreach ($name in @(
    '01-contract-and-provenance.txt',
    '02-vr2-reference-point-corpus.csv',
    '03-exact-v9-node-corpus.csv',
    '04-hydraulic-context.csv',
    '05-seam-probe-map.csv',
    '06-performance-baseline.csv',
    '07-rp1a-summary.txt')) {
    Require-File (Join-Path $frozen $name)
}

Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'status=PASS'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'vr2-reference-corpus-rows=40'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'exact-v9-node-corpus-rows=360'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'hydraulic-context-rows=288'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'seam-probe-rows=1280'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'seam-boundary-count=320'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'candidate-timing-inspected=False'
Require-Text (Join-Path $frozen '06-performance-baseline.csv') 'rp1b_candidate_resolve_median_ceiling_us,94.8'
Require-Text (Join-Path $frozen '06-performance-baseline.csv') 'rp1b_candidate_resolve_p95_ceiling_us,158.80666666666667'
Require-Text (Join-Path $frozen '06-performance-baseline.csv') 'rp1b_candidate_resolve_max_ceiling_us,409.30666666666673'
Require-Text (Join-Path $frozen '06-performance-baseline.csv') 'rp1b_candidate_resolve_median_allocated_bytes_ceiling,2816'

$expectedVr2Header = 'point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa'
$expectedNodeHeader = 'probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa'
$expectedHydraulicHeader = 'probe_id,logical_step,elapsed_s,path_id,from_node,to_node,resistance_pa_s2_kg2,active_boost_pa,production_driving_pa,if97_driving_pa,canonical_flow_kg_s,production_formula_flow_kg_s,if97_pressure_only_counterfactual_flow_kg_s,reference_resolved,driving_pressure_sign_changed,abs_counterfactual_flow_shift_kg_s'
$expectedSeamHeader = 'boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa'
if ((Get-Content -LiteralPath (Join-Path $frozen '02-vr2-reference-point-corpus.csv') -TotalCount 1 -Encoding UTF8) -ne $expectedVr2Header) { throw 'Frozen RP1A VR2 CSV header drifted.' }
if ((Get-Content -LiteralPath (Join-Path $frozen '03-exact-v9-node-corpus.csv') -TotalCount 1 -Encoding UTF8) -ne $expectedNodeHeader) { throw 'Frozen RP1A node CSV header drifted.' }
if ((Get-Content -LiteralPath (Join-Path $frozen '04-hydraulic-context.csv') -TotalCount 1 -Encoding UTF8) -ne $expectedHydraulicHeader) { throw 'Frozen RP1A hydraulic CSV header drifted.' }
if ((Get-Content -LiteralPath (Join-Path $frozen '05-seam-probe-map.csv') -TotalCount 1 -Encoding UTF8) -ne $expectedSeamHeader) { throw 'Frozen RP1A seam CSV header drifted.' }

$vr2Rows = @((Import-Csv -LiteralPath (Join-Path $frozen '02-vr2-reference-point-corpus.csv')))
$nodeRows = @((Import-Csv -LiteralPath (Join-Path $frozen '03-exact-v9-node-corpus.csv')))
$pathRows = @((Import-Csv -LiteralPath (Join-Path $frozen '04-hydraulic-context.csv')))
$seamRows = @((Import-Csv -LiteralPath (Join-Path $frozen '05-seam-probe-map.csv')))
if ($vr2Rows.Count -ne 40) { throw 'Frozen RP1A VR2 row count drifted.' }
if (@($vr2Rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.specific_volume_m3_kg) -and -not [string]::IsNullOrWhiteSpace($_.specific_u_j_kg) }).Count -ne 39) { throw 'Frozen RP1A inverse-applicable row count drifted.' }
if ($nodeRows.Count -ne 360) { throw 'Frozen RP1A node row count drifted.' }
if ($pathRows.Count -ne 288) { throw 'Frozen RP1A hydraulic row count drifted.' }
if ($seamRows.Count -ne 1280) { throw 'Frozen RP1A seam row count drifted.' }
if (@($seamRows | Group-Object -Property boundary_index).Count -ne 320) { throw 'Frozen RP1A seam boundary count drifted.' }
foreach ($side in @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')) {
    if (@($seamRows | Where-Object { $_.probe_side -eq $side }).Count -ne 320) { throw ("Frozen RP1A seam-side count drifted: {0}" -f $side) }
}
$boundaryOnly = @($vr2Rows | Where-Object { [string]::IsNullOrWhiteSpace($_.specific_volume_m3_kg) -or [string]::IsNullOrWhiteSpace($_.specific_u_j_kg) })
if ($boundaryOnly.Count -ne 1 -or $boundaryOnly[0].point_id -ne 'VR2-SAT-360C-PONLY' -or $boundaryOnly[0].source_family -ne 'SATURATION-PRESSURE-ONLY') { throw 'Frozen RP1A boundary-only VR2 row drifted.' }
if (@($nodeRows | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen RP1A node corpus contains unresolved reference rows.' }
if (@($pathRows | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen RP1A hydraulic corpus contains unresolved reference rows.' }
if (@($nodeRows | Group-Object -Property probe_id,logical_step,node_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen RP1A node keys are not unique on probe_id + logical_step + node_id.' }
if (@($pathRows | Group-Object -Property probe_id,logical_step,path_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen RP1A hydraulic keys are not unique on probe_id + logical_step + path_id.' }
if (@($seamRows | Group-Object -Property boundary_index,probe_side | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen RP1A seam keys are not unique.' }

$oldCandidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bShadowThermodynamicCandidates.cs'
$newCandidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement1ShadowThermodynamicCandidates.cs'
$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement1Tests.cs'
Require-Text $oldCandidatePath 'C1-TABULATED-SURROGATE'
Require-Text $oldCandidatePath 'D1-BOUNDED-IF97-SUBSET'
if ((Read-Utf8Text $oldCandidatePath).Contains('C2-EXTENDED-TABULATED-SURROGATE') -or (Read-Utf8Text $oldCandidatePath).Contains('D2-SEAM-COMPLETE-IF97-COMPARATOR')) {
    throw 'First-generation RP1B candidate file must remain immutable.'
}
Require-Text $newCandidatePath 'C2-EXTENDED-TABULATED-SURROGATE'
Require-Text $newCandidatePath 'D2-SEAM-COMPLETE-IF97-COMPARATOR'
Require-Text $newCandidatePath 'UsesDirectIf97AtResolveTime => false'
Require-Text $newCandidatePath 'UsesDirectIf97AtResolveTime => true'
Require-Text $newCandidatePath 'stepKelvins: 0.5'
Require-Text $newCandidatePath 'TryBuildReachabilityBoundary'
Require-Text $newCandidatePath 'LiquidBoundaryOffsetsMegapascals'
Require-Text $newCandidatePath 'VaporBoundaryFractions'
Require-Text $newCandidatePath 'Region2MaximumPressureMegapascals'
Require-Text $newCandidatePath 'TryResolveRegion2Fallback'
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement1Tests'
Require-Text $testPath 'Rp1bRefinement1_ProducesC2D2ShadowEvidenceAgainstFrozenRp1aCorpus'
Require-Text $testPath 'CandidateWarmupPasses = 2'
Require-Text $testPath 'CandidateMeasuredPasses = 8'
Require-Text $testPath 'C2-EXTENDED-TABULATED-SURROGATE'
Require-Text $testPath 'D2-SEAM-COMPLETE-IF97-COMPARATOR'
Require-Text $testPath 'ValidateFrozenHydraulicReplayLaw(nodeRows, hydraulicRows)'
Require-Text $testPath 'maximumFlowError <= 1e-9'
Require-Text $testPath 'candidate-count=2'
Require-Text $testPath '09-rp1b-refinement1-summary.txt'
Require-Text $testPath 'rp1c-selection-performed=False'
Require-Text $testPath 'production-src-change-authorized=False'
if ((Read-Utf8Text $testPath).Contains('Assert.Single(vr2Rows.Where(')) { throw 'xUnit2031-prone Assert.Single filtering pattern reintroduced.' }
Require-Text $newCandidatePath 'MaximumIterativeSolveIterations => 48'
Require-Text $newCandidatePath 'MaximumIterativeSolveIterations => 1_500'

$runnerPath = 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement1.cmd'
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT1=1'
Require-Text $runnerPath 'validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement1.ps1'
Require-Text $runnerPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement1Tests.Rp1bRefinement1_ProducesC2D2ShadowEvidenceAgainstFrozenRp1aCorpus'
Require-Text $runnerPath '--explicit only'
Require-Text $runnerPath '--parallel none'
foreach ($name in $expectedOutputs) { Require-Text $runnerPath $name }

$srcFiles = Get-ChildItem -LiteralPath 'src' -Recurse -File -Include *.cs
foreach ($source in $srcFiles) {
    $content = Read-Utf8Text $source.FullName
    if ($content.Contains('C2-EXTENDED-TABULATED-SURROGATE') -or $content.Contains('D2-SEAM-COMPLETE-IF97-COMPARATOR')) {
        throw ("RP1B Refinement 1 candidate identity leaked into production source: {0}" -f $source.FullName)
    }
}

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Read-Utf8Text $modePath).Contains('ReferenceConsistent')) {
    throw 'RP1B Refinement 1 must not add a production reference-consistent closure mode.'
}

$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 1 static audit: PASS'
