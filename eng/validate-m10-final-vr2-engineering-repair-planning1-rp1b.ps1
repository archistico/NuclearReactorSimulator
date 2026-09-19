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

$validatorBytes = [System.IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'RP1B validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B'
Write-Host '============================================================'
Write-Host 'Test-only shadow candidate matrix against the immutable RP1A corpus.'
Write-Host 'No production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$attempt1 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Attempt1_PreflightRedSummary.txt'
Require-Text $attempt1 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B'
Require-Text $attempt1 'attempt=1'
Require-Text $attempt1 'status=STATIC-PREFLIGHT-RED'
Require-Text $attempt1 'ordinary-build-started=False'
Require-Text $attempt1 'focused-rp1b-started=False'
Require-Text $attempt1 'candidate-evidence-produced=False'
Require-Text $attempt1 'failure-owner=VALIDATOR-NULL-UNSAFE-SOURCE-SCAN'
Require-Text $attempt1 'rp1c-selection-authorized=False'
Require-Text $attempt1 'production-src-change-authorized=False'

$attempt2 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Attempt2_BuildRedSummary.txt'
Require-Text $attempt2 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B'
Require-Text $attempt2 'attempt=2'
Require-Text $attempt2 'status=ORDINARY-BUILD-RED'
Require-Text $attempt2 'static-audit-completed=True'
Require-Text $attempt2 'ordinary-build-started=True'
Require-Text $attempt2 'focused-rp1b-started=False'
Require-Text $attempt2 'candidate-evidence-produced=False'
Require-Text $attempt2 'failure-owner=TEST-ONLY-XUNIT2031-ASSERT-SINGLE-FILTER-CONTRACT'
Require-Text $attempt2 'rp1c-selection-authorized=False'
Require-Text $attempt2 'production-src-change-authorized=False'

$returnedRp1a = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_UserReturnedSummary.txt'
Require-Text $returnedRp1a 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1A'
Require-Text $returnedRp1a 'status=VALIDATED'
Require-Text $returnedRp1a 'returned-artifact-set=COMPLETE-7-OF-7'
Require-Text $returnedRp1a 'vr2-inverse-applicable-rows=39'
Require-Text $returnedRp1a 'vr2-boundary-only-rows=1'
Require-Text $returnedRp1a 'vr2-boundary-only-point=VR2-SAT-360C-PONLY'
Require-Text $returnedRp1a 'rp1b-test-only-shadow-candidate-matrix-authorized=True'
Require-Text $returnedRp1a 'rp1c-selection-authorized=False'
Require-Text $returnedRp1a 'production-src-change-authorized=False'
Require-Text $returnedRp1a 'thermodynamic-repair-authorized=False'
Require-Text $returnedRp1a 'exact-v9-change-authorized=False'
Require-Text $returnedRp1a 'vr3-authorized=False'
Require-Text $returnedRp1a 'next-gate=RP1B-TEST-ONLY-SHADOW-CANDIDATE-MATRIX'

$rp1aContractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1a-contract.json'
Require-File $rp1aContractPath
$rp1a = Get-Content -LiteralPath $rp1aContractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($rp1a.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1a-v1') { throw 'Unexpected RP1A schema.' }
if ($rp1a.status -ne 'VALIDATED') { throw 'RP1A must be VALIDATED before RP1B.' }
if ($rp1a.returned_review.status -ne 'VALIDATED') { throw 'Returned RP1A review status drifted.' }
if ($rp1a.returned_review.rp1b_test_only_shadow_candidate_matrix_authorized -ne $true) { throw 'RP1B authorization missing from returned RP1A review.' }
if ($rp1a.returned_review.production_src_change_authorized -ne $false) { throw 'RP1A cannot authorize production source changes.' }

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-v1') { throw 'Unexpected RP1B schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'RP1B contract must remain CANDIDATE before returned evidence review.' }
if ($contract.prerequisite.returned_status -ne 'VALIDATED') { throw 'RP1A returned prerequisite drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1B-TEST-ONLY-SHADOW-CANDIDATE-MATRIX') { throw 'RP1B gate identity drifted.' }
if ($contract.frozen_corpus.vr2_rows -ne 40) { throw 'RP1B VR2 row count drifted.' }
if ($contract.frozen_corpus.exact_v9_node_rows -ne 360) { throw 'RP1B exact-v9 node count drifted.' }
if ($contract.frozen_corpus.hydraulic_rows -ne 288) { throw 'RP1B hydraulic row count drifted.' }
if ($contract.frozen_corpus.seam_rows -ne 1280) { throw 'RP1B seam row count drifted.' }
if ($contract.frozen_corpus.seam_boundary_count -ne 320) { throw 'RP1B seam boundary count drifted.' }
if (@($contract.candidates).Count -ne 3) { throw 'RP1B candidate count drifted.' }
if ($contract.candidates[0].id -ne 'B1-PIECEWISE-REDUCED') { throw 'B1 identity drifted.' }
if ($contract.candidates[1].id -ne 'C1-TABULATED-SURROGATE') { throw 'C1 identity drifted.' }
if ($contract.candidates[2].id -ne 'D1-BOUNDED-IF97-SUBSET') { throw 'D1 identity drifted.' }
if (@($contract.candidates | Where-Object { $_.implementation_scope -ne 'TEST-ONLY' }).Count -ne 0) { throw 'All RP1B candidates must remain TEST-ONLY.' }
if (@($contract.candidates | Where-Object { $_.final_result_retuning_in_place_allowed -ne $false }).Count -ne 0) { throw 'In-place candidate retuning must remain forbidden.' }
if ($contract.frozen_corpus.vr2_rows -ne 40) { throw 'RP1A VR2 corpus row count drifted.' }
if ($contract.frozen_corpus.vr2_inverse_applicable_rows -ne 39) { throw 'RP1A inverse-applicable VR2 row count drifted.' }
if ($contract.frozen_corpus.vr2_boundary_only_rows -ne 1) { throw 'RP1A VR2 boundary-only row count drifted.' }
if ($contract.frozen_corpus.exact_v9_node_rows -ne 360) { throw 'RP1A exact-v9 node count drifted.' }
if ($contract.frozen_corpus.hydraulic_rows -ne 288) { throw 'RP1A hydraulic row count drifted.' }
if ($contract.frozen_corpus.seam_rows -ne 1280 -or $contract.frozen_corpus.seam_boundary_count -ne 320) { throw 'RP1A seam corpus shape drifted.' }
if ($contract.comparison.vr2_boundary_only_rows_are_preserved_but_not_scored_as_inverse_closure -ne $true) { throw 'Boundary-only VR2 handling contract drifted.' }
if ($contract.comparison.existing_vr2_blocking_ceiling_fraction -ne 0.25) { throw 'Existing VR2 blocking ceiling drifted.' }
if ($contract.comparison.planning_target_fraction -ne 0.10) { throw 'RP1B planning target drifted.' }
if ($contract.comparison.planning_target_is_not_a_replacement_vr2_tolerance -ne $true) { throw 'Planning target must not become a VR2 tolerance.' }
if ($contract.comparison.candidate_warmup_passes -ne 2 -or $contract.comparison.candidate_measured_passes -ne 8) { throw 'RP1B performance protocol drifted.' }
if ($contract.comparison.candidate_qualification_is_evidence_not_rp1b_pass_criterion -ne $true) { throw 'RP1B evidence/pass separation drifted.' }
if ([Math]::Abs($contract.comparison.hydraulic_replay_frozen_law_self_check_tolerance_kg_s - 1e-9) -gt 1e-18) { throw 'RP1B hydraulic replay self-check tolerance drifted.' }
if ($contract.comparison.region2_reference_domain_respects_b23_boundary -ne $true) { throw 'RP1B Region-2 B23 domain guard drifted.' }
if ($contract.comparison.complexity_iteration_count_is_conservative_ceiling -ne $true) { throw 'RP1B complexity reporting contract drifted.' }
if ($contract.performance_ceilings.resolve_median_us -ne 94.8) { throw 'RP1A median ceiling drifted.' }
if ([Math]::Abs($contract.performance_ceilings.resolve_p95_us - 158.80666666666667) -gt 1e-12) { throw 'RP1A p95 ceiling drifted.' }
if ([Math]::Abs($contract.performance_ceilings.resolve_max_us - 409.30666666666673) -gt 1e-12) { throw 'RP1A max ceiling drifted.' }
if ($contract.performance_ceilings.resolve_median_allocated_bytes -ne 2816.0) { throw 'RP1A allocation ceiling drifted.' }
if ($contract.authority.rp1c_selection_authorized_before_returned_rp1b_review -ne $false) { throw 'RP1C cannot be authorized before returned RP1B review.' }
if ($contract.authority.production_src_change_authorized -ne $false) { throw 'Production source change cannot be authorized by RP1B.' }
if ($contract.authority.thermodynamic_repair_authorized -ne $false) { throw 'Thermodynamic repair cannot be authorized by RP1B.' }
if ($contract.authority.thermodynamic_tolerance_change_authorized -ne $false) { throw 'Thermodynamic tolerance change cannot be authorized by RP1B.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'Exact-v9 change cannot be authorized by RP1B.' }
if ($contract.authority.vr3_execution_authorized -ne $false) { throw 'VR3 cannot be authorized by RP1B.' }
if ($contract.authority.p3_r1_execution_authorized -ne $false) { throw 'P3-R1 cannot be authorized by RP1B.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long cannot be authorized by RP1B.' }
$expectedOutputs = @(
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
if (@($contract.outputs).Count -ne $expectedOutputs.Count) { throw 'RP1B output artifact count drifted.' }
for ($index = 0; $index -lt $expectedOutputs.Count; $index++) {
    if ($contract.outputs[$index] -ne $expectedOutputs[$index]) { throw ("RP1B output artifact drifted at index {0}." -f $index) }
}


$frozen = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts'
$expectedFiles = @(
    '01-contract-and-provenance.txt',
    '02-vr2-reference-point-corpus.csv',
    '03-exact-v9-node-corpus.csv',
    '04-hydraulic-context.csv',
    '05-seam-probe-map.csv',
    '06-performance-baseline.csv',
    '07-rp1a-summary.txt'
)
foreach ($name in $expectedFiles) { Require-File (Join-Path $frozen $name) }

Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'status=PASS'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'vr2-reference-corpus-rows=40'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'exact-v9-node-corpus-rows=360'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'hydraulic-context-rows=288'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'seam-probe-rows=1280'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'seam-boundary-count=320'
Require-Text (Join-Path $frozen '07-rp1a-summary.txt') 'candidate-timing-inspected=False'
Require-Text (Join-Path $frozen '06-performance-baseline.csv') 'rp1b_candidate_resolve_median_ceiling_us,94.8'
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
if ($vr2Rows.Count -ne 40) { throw 'Frozen RP1A VR2 corpus row count drifted.' }
if (@($vr2Rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.specific_volume_m3_kg) -and -not [string]::IsNullOrWhiteSpace($_.specific_u_j_kg) }).Count -ne 39) { throw 'Frozen RP1A inverse-applicable VR2 row count drifted.' }
$boundaryOnly = @($vr2Rows | Where-Object { [string]::IsNullOrWhiteSpace($_.specific_volume_m3_kg) -or [string]::IsNullOrWhiteSpace($_.specific_u_j_kg) })
if ($boundaryOnly.Count -ne 1 -or $boundaryOnly[0].point_id -ne 'VR2-SAT-360C-PONLY' -or $boundaryOnly[0].source_family -ne 'SATURATION-PRESSURE-ONLY') { throw 'Frozen RP1A boundary-only VR2 row drifted.' }
if ($nodeRows.Count -ne 360) { throw 'Frozen RP1A node corpus row count drifted.' }
if ($pathRows.Count -ne 288) { throw 'Frozen RP1A hydraulic row count drifted.' }
if ($seamRows.Count -ne 1280) { throw 'Frozen RP1A seam row count drifted.' }
if (@($nodeRows | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen RP1A node corpus contains unresolved reference rows.' }
if (@($pathRows | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen RP1A hydraulic corpus contains unresolved reference rows.' }
if (@($seamRows | Group-Object -Property boundary_index).Count -ne 320) { throw 'Frozen RP1A seam boundary count drifted.' }
foreach ($side in @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')) {
    if (@($seamRows | Where-Object { $_.probe_side -eq $side }).Count -ne 320) { throw ("Frozen RP1A seam-side count drifted: {0}" -f $side) }
}
if (@($nodeRows | Group-Object -Property probe_id,logical_step,node_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen RP1A node keys are not unique on probe_id + logical_step + node_id.' }
if (@($pathRows | Group-Object -Property probe_id,logical_step,path_id | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen RP1A hydraulic keys are not unique on probe_id + logical_step + path_id.' }
if (@($seamRows | Group-Object -Property boundary_index,probe_side | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen RP1A seam keys are not unique.' }

$candidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bShadowThermodynamicCandidates.cs'
$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bTests.cs'
Require-Text $candidatePath 'internal interface IRp1bShadowThermodynamicCandidate'
Require-Text $candidatePath 'Rp1bPiecewiseReducedCandidate'
Require-Text $candidatePath 'Rp1bTabulatedReferenceSurrogateCandidate'
Require-Text $candidatePath 'Rp1bBoundedIf97SubsetCandidate'
Require-Text $candidatePath 'UsesDirectIf97AtResolveTime'
Require-Text $candidatePath 'MaximumIterativeSolveIterations => 32'
Require-Text $candidatePath 'MaximumIterativeSolveIterations => 1_200'
Require-Text $candidatePath 'Region2MaximumPressureMegapascals'
Require-Text $candidatePath '348.05185628969d'
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bTests'
Require-Text $testPath 'CandidateWarmupPasses = 2'
Require-Text $testPath 'CandidateMeasuredPasses = 8'
Require-Text $testPath 'VR2-SAT-360C-PONLY'
Require-Text $testPath 'Assert.Single(vr2Rows, static row => !row.InverseApplicable)'
Require-Text $testPath 'InverseApplicable'
Require-Text $testPath 'var specificVolume = DN(parts[6])'
Require-Text $testPath 'var specificEnergy = DN(parts[7])'
Require-Text $testPath '(row.ProbeId, row.LogicalStep, row.NodeId)'
Require-Text $testPath '(row.ProbeId, row.LogicalStep, row.FromNodeId)'
Require-Text $testPath 'ValidateFrozenHydraulicReplayLaw(nodeRows, hydraulicRows)'
Require-Text $testPath 'maximumFlowError <= 1e-9d'
Require-Text $testPath 'HasFrozenCheckValve(row.PathId)'
Require-Text $testPath 'vr2_inverse_applicable_rows'
Require-Text $testPath 'candidate-qualification-is-evidence-not-rp1b-pass-criterion=True'
Require-Text $testPath '09-rp1b-summary.txt'
Require-Text $testPath 'rp1c-selection-performed=False'
Require-Text $testPath 'production-src-change-authorized=False'

$runnerPath = 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b.cmd'
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B=1'
Require-Text $runnerPath 'validate-m10-final-vr2-engineering-repair-planning1-rp1b.ps1'
Require-Text $runnerPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bTests.Rp1b_ProducesTestOnlyShadowCandidateMatrixAgainstFrozenRp1aCorpus'
Require-Text $runnerPath '--explicit only'
Require-Text $runnerPath '--parallel none'
foreach ($name in $expectedOutputs) { Require-Text $runnerPath $name }

$srcFiles = Get-ChildItem -LiteralPath 'src' -Recurse -File -Include *.cs
foreach ($source in $srcFiles) {
    $content = Read-Utf8Text $source.FullName
    if ($content.Contains('B1-PIECEWISE-REDUCED') -or $content.Contains('C1-TABULATED-SURROGATE') -or $content.Contains('D1-BOUNDED-IF97-SUBSET')) {
        throw ("RP1B candidate identity leaked into production source: {0}" -f $source.FullName)
    }
}

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Read-Utf8Text $modePath).Contains('ReferenceConsistent')) {
    throw 'RP1B must not add a production reference-consistent closure mode.'
}

$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B static audit: PASS'
