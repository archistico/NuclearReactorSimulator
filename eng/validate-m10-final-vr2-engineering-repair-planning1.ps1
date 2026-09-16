$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    Require-File $Path
    $content = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if (-not $content.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

$validatorPath = $MyInvocation.MyCommand.Path
$validatorBytes = [System.IO.File]::ReadAllBytes($validatorPath)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'Planning validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1'
Write-Host '============================================================'
Write-Host 'Static fail-closed planning audit only.'
Write-Host 'No production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$returnedSummary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_ReturnedEvidenceAdjudicationSummary.txt'
Require-Text $returnedSummary 'gate=VR2-MATERIALITY-DIAGNOSTIC1-RETURNED-EVIDENCE-ADJUDICATION'
Require-Text $returnedSummary 'returned-artifact-set=COMPLETE-7-OF-7'
Require-Text $returnedSummary 'engineering-classification=HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $returnedSummary 'p1b-checkpoints-matched=3/3'
Require-Text $returnedSummary 'node-row-count=360'
Require-Text $returnedSummary 'unresolved-node-row-count=0'
Require-Text $returnedSummary 'path-row-count=288'
Require-Text $returnedSummary 'unresolved-path-row-count=0'
Require-Text $returnedSummary 'pressure-production-subcooled-reference-region4-mixture-count=72'
Require-Text $returnedSummary 'suction-production-subcooled-reference-region4-mixture-count=55'
Require-Text $returnedSummary 'production-repair-authorized=False'
Require-Text $returnedSummary 'thermodynamic-tolerance-change-authorized=False'
Require-Text $returnedSummary 'exact-v9-change-authorized=False'
Require-Text $returnedSummary 'vr3-authorized=False'

$productionModel = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs'
Require-Text $productionModel 'private const double LiquidSpecificHeatJoulesPerKilogramKelvin = 4_200d;'
Require-Text $productionModel 'private const double LiquidBulkModulusPascals = 2.2e9d;'
Require-Text $productionModel 'TryResolveSaturatedMixture(specificVolume, specificInternalEnergy'
Require-Text $productionModel 'TryResolveSubcooledLiquid(specificVolume, specificInternalEnergy'
Require-Text $productionModel 'TryResolveSuperheatedVapor(specificVolume, specificInternalEnergy'

$closureMode = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $closureMode 'HistoricalCorrelationTopology = 0'
Require-Text $closureMode 'CorrelationConsistentInverseDomain = 1'

Require-Text 'docs/adr/0165-stage-correlation-consistent-water-steam-inverse-domain-repair-before-production-activation.md' 'Introduce'
Require-Text 'docs/adr/0166-requalify-repaired-thermodynamic-closure-before-versioned-activation.md' 'Requalify the repaired thermodynamic closure before versioned activation'
Require-Text 'docs/adr/0167-activate-repaired-exact-v4-with-historical-version-preservation.md' 'Old exact-version saves and scenarios remain reproducible rather than being migrated implicitly to new thermodynamics.'

$planPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md'
Require-Text $planPath 'VALIDATED / CLOSED'
Require-Text $planPath 'HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $planPath 'Family A'
Require-Text $planPath 'local compressed-liquid coefficient retune'
Require-Text $planPath 'do not advance this family as a standalone repair candidate.'
Require-Text $planPath 'Family B'
Require-Text $planPath 'piecewise reference-consistent reduced inverse closure'
Require-Text $planPath 'Family C'
Require-Text $planPath 'bounded IF97-derived table/interpolation surrogate'
Require-Text $planPath 'Family D'
Require-Text $planPath 'bounded production IF97 subset'
Require-Text $planPath 'RP1A'
Require-Text $planPath 'Reference Domain Corpus & Seam Map Freeze'
Require-Text $planPath 'RP1B'
Require-Text $planPath 'Test-Only Shadow Candidate Matrix'
Require-Text $planPath 'RP1C'
Require-Text $planPath 'Engineering Repair Selection Gate'
Require-Text $planPath 'exact-v9 must not be silently reinterpreted'
Require-Text $planPath 'new opt-in closure mode'
Require-Text $planPath 'new exact-version identity'
Require-Text $planPath 'Do not widen VR2 claim bands.'
Require-Text $planPath 'performance ceiling must then be frozen'

$adr194 = 'docs/adr/0194-preserve-exact-v9-and-stage-reference-consistent-thermodynamic-repair-behind-new-closure-mode.md'
Require-Text $adr194 'Accepted after the returned VR2 Engineering Repair Planning 1 static audit completed'
Require-Text $adr194 'do not change the semantics of `WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain` in place;'
Require-Text $adr194 'do not reinterpret `integrated-operations-desktop-stable@9` through new thermodynamic physics;'
Require-Text $adr194 'introduce a new opt-in thermodynamic closure mode'

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-v1') { throw 'Unexpected Planning 1 contract schema.' }
if ($contract.status -ne 'VALIDATED') { throw 'Planning 1 contract must be VALIDATED after returned planning audit review.' }
if ($contract.prerequisite.engineering_classification -ne 'HYDRAULIC-MATERIALITY-CONFIRMED') { throw 'Materiality prerequisite drifted.' }
if ($contract.prerequisite.node_row_count -ne 360 -or $contract.prerequisite.unresolved_node_row_count -ne 0) { throw 'Frozen node evidence drifted.' }
if ($contract.prerequisite.path_row_count -ne 288 -or $contract.prerequisite.unresolved_path_row_count -ne 0) { throw 'Frozen path evidence drifted.' }
if ($contract.prerequisite.pressure_production_subcooled_reference_region4_mixture_count -ne 72) { throw 'Pressure phase-boundary evidence drifted.' }
if ($contract.prerequisite.suction_production_subcooled_reference_region4_mixture_count -ne 55) { throw 'Suction phase-boundary evidence drifted.' }
if ($contract.problem_scope.not_just_compressed_liquid_coefficient_tuning -ne $true) { throw 'Planning scope cannot collapse to coefficient tuning.' }
if ($contract.historical_semantics.correlation_consistent_inverse_domain_must_remain_immutable -ne $true) { throw 'Existing closure semantics must remain immutable.' }
if ($contract.historical_semantics.exact_v9_must_remain_immutable -ne $true) { throw 'Exact-v9 must remain immutable.' }
if ($contract.historical_semantics.new_opt_in_closure_mode_required_for_any_authorized_repair -ne $true) { throw 'Any future repair must use a new opt-in closure mode.' }
if ($contract.historical_semantics.new_exact_version_required_before_any_repaired_production_activation -ne $true) { throw 'Any future activation must use a new exact version.' }

$familyA = $contract.repair_families | Where-Object { $_.id -eq 'A-LOCAL-COEFFICIENT-RETUNE' }
$familyB = $contract.repair_families | Where-Object { $_.id -eq 'B-PIECEWISE-REFERENCE-CONSISTENT-REDUCED-CLOSURE' }
$familyC = $contract.repair_families | Where-Object { $_.id -eq 'C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE' }
$familyD = $contract.repair_families | Where-Object { $_.id -eq 'D-BOUNDED-PRODUCTION-IF97-SUBSET' }
if ($familyA.planning_status -ne 'DO-NOT-ADVANCE-AS-STANDALONE') { throw 'Local coefficient retune cannot advance standalone.' }
if ($familyB.planning_status -ne 'ADVANCE-TO-TEST-ONLY-SHADOW-STUDY') { throw 'Family B status drifted.' }
if ($familyC.planning_status -ne 'ADVANCE-TO-TEST-ONLY-SHADOW-STUDY') { throw 'Family C status drifted.' }
if ($familyD.planning_status -ne 'ADVANCE-AS-REFERENCE-FIDELITY-COST-COMPARATOR') { throw 'Family D status drifted.' }

if ($contract.selection_policy.existing_vr2_blocking_ceiling_percent -ne 25.0) { throw 'Existing VR2 blocking ceiling drifted.' }
if ($contract.selection_policy.planning_target_m10_core_percent -ne 10.0) { throw 'Planning target drifted.' }
if ($contract.selection_policy.planning_target_is_not_a_replacement_vr2_tolerance -ne $true) { throw 'Planning target cannot replace VR2 tolerance.' }
if ($contract.selection_policy.phase_agreement_target_on_reference_classifiable_exact_v9_corpus_percent -ne 100.0) { throw 'Phase agreement target drifted.' }
if ($contract.selection_policy.inventory_clamping_allowed -ne $false) { throw 'Inventory clamping must remain forbidden.' }
if ($contract.selection_policy.node_id_special_cases_allowed -ne $false) { throw 'Node-id special cases must remain forbidden.' }
if ($contract.selection_policy.fail_closed_required -ne $true -or $contract.selection_policy.deterministic_repeat_required -ne $true) { throw 'Fail-closed/deterministic requirements drifted.' }
if ($contract.selection_policy.performance_ceiling_must_be_frozen_in_rp1a_before_rp1b_timing_results -ne $true) { throw 'Performance ceiling timing rule drifted.' }

$expectedSequence = @(
    'RP1A-REFERENCE-DOMAIN-CORPUS-AND-SEAM-MAP-FREEZE',
    'RP1B-TEST-ONLY-SHADOW-CANDIDATE-MATRIX',
    'RP1C-ENGINEERING-REPAIR-SELECTION-GATE'
)
if (($contract.sequence -join '|') -ne ($expectedSequence -join '|')) { throw 'Planning sequence drifted.' }

if ($contract.authority.production_src_change_authorized -ne $false) { throw 'Production source change cannot be authorized.' }
if ($contract.authority.thermodynamic_repair_authorized -ne $false) { throw 'Thermodynamic repair cannot be authorized.' }
if ($contract.authority.thermodynamic_tolerance_change_authorized -ne $false) { throw 'Thermodynamic tolerance change cannot be authorized.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'Exact-v9 change cannot be authorized.' }
if ($contract.authority.existing_closure_mode_reinterpretation_authorized -ne $false) { throw 'Existing closure reinterpretation cannot be authorized.' }
if ($contract.authority.new_exact_version_activation_authorized -ne $false) { throw 'New exact activation cannot be authorized.' }
if ($contract.authority.vr3_execution_authorized -ne $false) { throw 'VR3 cannot be authorized.' }
if ($contract.authority.p3_r1_execution_authorized -ne $false) { throw 'P3-R1 cannot be authorized.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long cannot be authorized.' }
if ($contract.authority.rp1a_implementation_authorized_before_returned_planning_audit_review -ne $false) { throw 'RP1A cannot start before returned Planning 1 audit review.' }

Require-Text 'docs/PROJECT.md' 'VR2 ENGINEERING REPAIR PLANNING 1'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'VR2 Engineering Repair Planning 1'
Require-Text 'docs/M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md' 'VR2 Engineering Repair Planning 1'
Require-Text 'docs/KNOWN_MODEL_LIMITATIONS.md' 'HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text 'docs/README.md' 'M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md'
Require-Text 'docs/adr/README.md' '0194-preserve-exact-v9-and-stage-reference-consistent-thermodynamic-repair-behind-new-closure-mode.md'

$artifactDir = 'artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$artifactPath = Join-Path $artifactDir '01-planning-audit.txt'
@(
    'gate=VR2-ENGINEERING-REPAIR-PLANNING1',
    'status=PASS-AS-AUTHORED',
    'prerequisite-engineering-classification=HYDRAULIC-MATERIALITY-CONFIRMED',
    'repair-problem-scope=(v,u)->phase,T,p,quality',
    'family-a-local-coefficient-retune=DO-NOT-ADVANCE-AS-STANDALONE',
    'family-b-piecewise-reduced-closure=ADVANCE-TO-TEST-ONLY-SHADOW-STUDY',
    'family-c-tabulated-reference-surrogate=ADVANCE-TO-TEST-ONLY-SHADOW-STUDY',
    'family-d-bounded-if97-subset=ADVANCE-AS-REFERENCE-FIDELITY-COST-COMPARATOR',
    'exact-v9-immutable=True',
    'existing-correlation-consistent-closure-immutable=True',
    'new-opt-in-closure-required-for-future-repair=True',
    'new-exact-version-required-before-future-activation=True',
    'next-gate=RP1A-REFERENCE-DOMAIN-CORPUS-AND-SEAM-MAP-FREEZE',
    'production-src-change-authorized=False',
    'thermodynamic-repair-authorized=False',
    'thermodynamic-tolerance-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False',
    'rp1a-authorized-before-returned-planning-audit-review=False'
) | Set-Content -LiteralPath $artifactPath -Encoding UTF8

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 static audit: PASS'
Write-Host ("Artifact written: {0}" -f $artifactPath)
