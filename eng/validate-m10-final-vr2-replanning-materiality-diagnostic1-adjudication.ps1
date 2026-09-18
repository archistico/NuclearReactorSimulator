$ErrorActionPreference = 'Stop'

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    Require-File $Path
    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if (-not $text.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

$validatorBytes = [System.IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
if ($validatorBytes | Where-Object { $_ -gt 0x7F }) {
    throw 'VR2 materiality adjudication validator source must remain ASCII-only for Windows PowerShell 5.1 compatibility.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 MATERIALITY DIAGNOSTIC 1 RETURNED EVIDENCE ADJUDICATION'
Write-Host '============================================================'
Write-Host 'Static fail-closed audit of the complete returned Attempt-5 materiality evidence.'
Write-Host 'No 3600 s replay; no production repair/tolerance/exact-v9/VR3 authority.'
Write-Host ''

$attempt5Summary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_UserReturnedPostEvidenceFlowReproductionRedSummary.txt'
Require-Text $attempt5Summary 'status=EXECUTED-RED-POST-EVIDENCE'
Require-Text $attempt5Summary 'artifact-set-returned=COMPLETE-7-OF-7'
Require-Text $attempt5Summary 'p1b-checkpoints-matched=3/3'
Require-Text $attempt5Summary 'artifact-summary-classification=HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $attempt5Summary 'artifact-summary-max-hydraulic-flow-reproduction-error-kg-s=0.009952798974779853'
Require-Text $attempt5Summary 'frozen-original-reproduction-tolerance-kg-s=1e-9'
Require-Text $attempt5Summary 'authoritative-exact-v9-h22-fixed-point-flow-tolerance-kg-s=1e-2'
Require-Text $attempt5Summary 'production-repair-authorized=False'
Require-Text $attempt5Summary 'vr3-authorized=False'
Require-Text $attempt5Summary 'next-authorized-gate=VR2-MATERIALITY-DIAGNOSTIC1-RETURNED-EVIDENCE-ADJUDICATION'

$frozenDir = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts'
foreach ($name in @(
    '01-contract-and-provenance.txt',
    '02-p1b-checkpoint-reproduction.csv',
    '03-node-if97-inverse-map.csv',
    '04-hydraulic-path-counterfactual.csv',
    '05-late-window-materiality.csv',
    '06-materiality-summary.txt',
    '07-sentinels.txt')) {
    Require-File (Join-Path $frozenDir $name)
}

Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'execution-pass=True'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'classification=HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'p1b-checkpoints-matched=3/3'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'node-row-count=360'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'path-row-count=288'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'confirmed-window-count=8'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'not-excluded-window-count=8'
Require-Text (Join-Path $frozenDir '06-materiality-summary.txt') 'deterministic-analysis-repeat=True'

$couplingPath = 'src/NuclearReactorSimulator.Domain/Plant/HydraulicNumericalCouplingDefinition.cs'
Require-Text $couplingPath 'H22FourNodeBranchContinuityCorrectedCommitOptIn'
Require-Text $couplingPath '1e-2d);'

$historicalTest = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalPhysicalReferenceVr2MaterialityDiagnosticTests.cs'
Require-Text $historicalTest 'private const double HydraulicFlowReproductionToleranceKilogramsPerSecond = 1e-9d;'
Require-Text $historicalTest 'HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $historicalTest 'private const double ConfirmedImpactRatio = 1d;'
Require-Text $historicalTest 'private const double NotExcludedImpactRatio = 0.1d;'

$adjudicationTest = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalPhysicalReferenceVr2MaterialityReturnedEvidenceAdjudicationTests.cs'
Require-Text $adjudicationTest 'Fact(Explicit = true)'
Require-Text $adjudicationTest 'NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2_MATERIALITY_ADJUDICATION'
Require-Text $adjudicationTest 'H22FourNodeBranchContinuityCorrectedCommitOptIn'
Require-Text $adjudicationTest 'FrozenOriginalReproductionToleranceKilogramsPerSecond = 1e-9d;'
Require-Text $adjudicationTest 'Assert.Equal(1e-2d, exactV9FixedPointTolerance);'
Require-Text $adjudicationTest 'pressureProductionSubcooledReferenceMixture'
Require-Text $adjudicationTest 'suctionProductionSubcooledReferenceMixture'
Require-Text $adjudicationTest 'ConservativeImpactRatio'
Require-Text $adjudicationTest 'engineering-classification=HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $adjudicationTest 'production-repair-authorized=False'
Require-Text $adjudicationTest 'vr3-authorized=False'

$contractPath = 'eng/m10-final-physical-reference-vr2-materiality-diagnostic1-returned-evidence-adjudication-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-physical-reference-vr2-materiality-diagnostic1-returned-evidence-adjudication-v1') { throw 'Unexpected adjudication contract schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'Adjudication contract must remain CANDIDATE until returned adjudication artifact review.' }
if ($contract.source_attempt.attempt -ne 5) { throw 'Source attempt must remain 5.' }
if ($contract.source_attempt.status -ne 'EXECUTED-RED-POST-EVIDENCE') { throw 'Source attempt status drifted.' }
if ($contract.source_attempt.returned_artifact_set_complete -ne $true -or $contract.source_attempt.artifact_count -ne 7) { throw 'Returned Attempt-5 evidence set must remain complete.' }
if ($contract.source_attempt.artifact_summary_classification -ne 'HYDRAULIC-MATERIALITY-CONFIRMED') { throw 'Returned materiality classification drifted.' }
if ($contract.historical_contract.original_flow_reproduction_tolerance_kg_s -ne 1e-9) { throw 'Historical 1e-9 assertion must remain frozen.' }
if ($contract.historical_contract.original_contract_is_rewritten -ne $false) { throw 'Historical contract cannot be rewritten.' }
if ($contract.historical_contract.original_long_diagnostic_is_rerun -ne $false) { throw 'Adjudication must not replay the long diagnostic.' }
if ($contract.authoritative_exact_v9_numerics.absolute_flow_fixed_point_tolerance_kg_s -ne 0.01) { throw 'Authoritative exact-v9 H.22 flow residual bound drifted.' }
if ($contract.returned_evidence.p1b_checkpoints_matched -ne '3/3') { throw 'P1B checkpoint evidence drifted.' }
if ($contract.returned_evidence.node_row_count -ne 360 -or $contract.returned_evidence.unresolved_node_row_count -ne 0) { throw 'Node evidence counts drifted.' }
if ($contract.returned_evidence.path_row_count -ne 288 -or $contract.returned_evidence.unresolved_path_row_count -ne 0) { throw 'Path evidence counts drifted.' }
if ($contract.returned_evidence.confirmed_window_count -ne 8 -or $contract.returned_evidence.not_excluded_window_count -ne 8) { throw 'Window evidence counts drifted.' }
if ($contract.returned_evidence.maximum_flow_reproduction_error_kg_s -ge $contract.authoritative_exact_v9_numerics.absolute_flow_fixed_point_tolerance_kg_s) { throw 'Returned flow reproduction error must remain inside the authoritative fixed-point bound.' }
if ($contract.returned_evidence.pressure_production_subcooled_reference_region4_mixture_count -ne 72) { throw 'Pressure-node phase reinterpretation count drifted.' }
if ($contract.returned_evidence.suction_production_subcooled_reference_region4_mixture_count -ne 55) { throw 'Suction-node phase reinterpretation count drifted.' }
if ($contract.robustness_rule.confirmed_impact_ratio -ne 1.0 -or $contract.robustness_rule.not_excluded_impact_ratio -ne 0.1) { throw 'Materiality thresholds cannot change.' }
if ($contract.robustness_rule.required_persistent_windows -ne 3) { throw 'Persistence threshold cannot change.' }
if ($contract.robustness_rule.materiality_thresholds_changed -ne $false) { throw 'Materiality thresholds must remain unchanged.' }
if ($contract.authority.production_src_change_authorized -ne $false) { throw 'Production source change cannot be authorized.' }
if ($contract.authority.thermodynamic_repair_authorized -ne $false) { throw 'Thermodynamic repair cannot be authorized.' }
if ($contract.authority.thermodynamic_tolerance_change_authorized -ne $false) { throw 'Thermodynamic tolerance change cannot be authorized.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'Exact-v9 change cannot be authorized.' }
if ($contract.authority.vr3_execution_authorized -ne $false) { throw 'VR3 cannot be authorized.' }
if ($contract.authority.p3_r1_execution_authorized -ne $false) { throw 'P3-R1 cannot be authorized.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long cannot be authorized.' }
if ($contract.authority.separate_engineering_repair_planning_decision_required -ne $true) { throw 'Separate repair-planning decision must remain mandatory.' }

Require-Text 'docs/PROJECT.md' 'VR2 MATERIALITY DIAGNOSTIC 1 RETURNED-EVIDENCE ADJUDICATION'
Require-Text 'docs/M10_FINAL_VR2_REPLANNING_MATERIALITY_DIAGNOSTIC1.md' 'Attempt 5 returned evidence'
Require-Text 'docs/M10_FINAL_VR2_REPLANNING_MATERIALITY_DIAGNOSTIC1.md' '0.009952798974779853 kg/s'
Require-Text 'docs/M10_FINAL_VR2_REPLANNING_MATERIALITY_DIAGNOSTIC1.md' 'H22'
Require-Text 'docs/M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md' 'returned-evidence adjudication'

Require-Text 'docs/M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md' 'ACTIVE CANDIDATE'
Require-Text 'docs/M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md' '0.009952798974779853 kg/s'
Require-Text 'docs/M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md' '72 sampled `pressure` states'
Require-Text 'docs/M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md' '55 of 72 `suction` samples'

Write-Host 'M10 Final VR2 Materiality Diagnostic 1 returned-evidence adjudication static audit: PASS'
