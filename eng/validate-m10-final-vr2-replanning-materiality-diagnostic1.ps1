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

# This validator is launched by powershell.exe from the CMD runner. Keep the script source ASCII-only
# so Windows PowerShell 5.1 cannot reinterpret UTF-8 punctuation through the active legacy code page.
$validatorBytes = [System.IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
if ($validatorBytes | Where-Object { $_ -gt 0x7F }) {
    throw 'VR2 materiality validator source must remain ASCII-only for Windows PowerShell 5.1 compatibility.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 REPLANNING / MATERIALITY DIAGNOSTIC 1'
Write-Host '============================================================'
Write-Host 'Static fail-closed audit after returned VR2 MODEL-DISCREPANCY-BLOCKING evidence.'
Write-Host 'Observation only: no thermodynamic repair/tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$vr1Summary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR1_UserReportedValidatedSummary.txt'
Require-Text $vr1Summary 'status=VALIDATED'
Require-Text $vr1Summary 'result=PASS'

$vr2Summary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2_UserReturnedBlockingSummary.txt'
Require-Text $vr2Summary 'status=EXECUTED-RED'
Require-Text $vr2Summary 'classification=MODEL-DISCREPANCY-BLOCKING'
Require-Text $vr2Summary 'blocking-owner=INVERSE-COMPRESSED-LIQUID/resolved-pressure'
Require-Text $vr2Summary 'next-authorized-gate=VR2-REPLANNING-MATERIALITY-DIAGNOSTIC1'
Require-Text $vr2Summary 'vr3-authorized=False'
Require-Text $vr2Summary 'production-repair-authorized=False'

$attempt4Summary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt4_UserReportedFocusedSelfCheckRedSummary.txt'
Require-Text $attempt4Summary 'status=EXECUTED-RED-PRE-TRAJECTORY'
Require-Text $attempt4Summary 'failure-owner=TEST-ONLY-IF97-REGION1-INVERSE-SELF-CHECK'
Require-Text $attempt4Summary 'exact-v9-p1b-trajectory-started=False'
Require-Text $attempt4Summary 'returned-materiality-artifact-directory=EMPTY'
Require-Text $attempt4Summary 'engineering-classification=NONE'
Require-Text $attempt4Summary 'next-authorized-gate=VR2-REPLANNING-MATERIALITY-DIAGNOSTIC1-HOTFIX4'

Require-Text 'docs/PROJECT.md' 'VR2 REPLANNING / MATERIALITY DIAGNOSTIC 1'
Require-Text 'docs/PROJECT.md' 'MODEL-DISCREPANCY-BLOCKING'
Require-Text 'docs/M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md' 'only authorized next gate is **VR2 Replanning / Materiality Diagnostic 1**'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'VR2 returned `MODEL-DISCREPANCY-BLOCKING`'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'VR2 Replanning / Materiality Diagnostic 1 is the active execution candidate.'
Require-Text 'docs/M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md' 'MODEL-DISCREPANCY-BLOCKING'

$diagnosticDoc = 'docs/M10_FINAL_VR2_REPLANNING_MATERIALITY_DIAGNOSTIC1.md'
Require-Text $diagnosticDoc '**Status:** ACTIVE CANDIDATE'
Require-Text $diagnosticDoc 'observation-only impact localization'
Require-Text $diagnosticDoc 'IF97 is never committed into the runtime state'
Require-Text $diagnosticDoc 'final 1200 s loaded tail'
Require-Text $diagnosticDoc 'four contiguous 300 s windows'
Require-Text $diagnosticDoc 'impact ratio >= 1.0'
Require-Text $diagnosticDoc '0.1 <= impact ratio < 1.0'
Require-Text $diagnosticDoc 'HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $diagnosticDoc 'HYDRAULIC-MATERIALITY-NOT-EXCLUDED'
Require-Text $diagnosticDoc 'HYDRAULIC-MATERIALITY-NOT-DEMONSTRATED'
Require-Text $diagnosticDoc 'REFERENCE-INVERSION-GAP'

$helperPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/IapwsIf97Reference.cs'
Require-Text $helperPath 'internal static class IapwsIf97Reference'
Require-Text $helperPath 'TryResolveRegion1FromSpecificVolumeAndInternalEnergy'
Require-Text $helperPath 'TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy'
Require-Text $helperPath 'TryFindFirstReachableRegion1Temperature'
Require-Text $helperPath 'IsRegion1SpecificVolumeReachable'
Require-Text $helperPath 'internal readonly record struct IapwsInverseReferenceState'
Require-Text $helperPath 'SimplifiedWaterSteamThermodynamicModel and is never referenced by production code.'

$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalPhysicalReferenceVr2MaterialityDiagnosticTests.cs'
Require-Text $testPath 'Fact(Explicit = true)'
Require-Text $testPath 'NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2_MATERIALITY_DIAGNOSTIC'
Require-Text $testPath 'DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory'
Require-Text $testPath 'ExpectedLoadCommandLogicalStep'
Require-Text $testPath 'Assert.Equal(2785L, contract.ExpectedLoadCommandLogicalStep)'
Require-Text $testPath 'IapwsIf97Reference.TryResolveRegion1FromSpecificVolumeAndInternalEnergy'
Require-Text $testPath 'IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy'
Require-Text $testPath 'private static readonly string[] RequiredPathIds = ["MCP", "CHANNEL", "RETURN", "FEEDWATER-PUMP"]'
Require-Text $testPath 'private const double ConfirmedImpactRatio = 1d;'
Require-Text $testPath 'private const double NotExcludedImpactRatio = 0.1d;'
Require-Text $testPath 'private const int RequiredPersistentWindows = 3;'
Require-Text $testPath 'private const double HydraulicFlowReproductionToleranceKilogramsPerSecond = 1e-9d;'
Require-Text $testPath 'ReadLatestCanonicalSnapshot(IntegratedAutomaticOperationRuntimeEngine engine)'
Require-Text $testPath 'ReadFixedDeltaTime(IntegratedAutomaticOperationRuntimeEngine engine)'
Require-Text $testPath 'BindingFlags.Instance | BindingFlags.NonPublic'
Require-Text $testPath 'HYDRAULIC-MATERIALITY-CONFIRMED'
Require-Text $testPath 'HYDRAULIC-MATERIALITY-NOT-EXCLUDED'
Require-Text $testPath 'HYDRAULIC-MATERIALITY-NOT-DEMONSTRATED'
Require-Text $testPath 'REFERENCE-INVERSION-GAP'
Require-Text $testPath 'deterministicAnalysisRepeat'
Require-Text $testPath '01-contract-and-provenance.txt'
Require-Text $testPath '07-sentinels.txt'

$contractPath = 'eng/m10-final-physical-reference-vr2-materiality-diagnostic1-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($contract.schema -ne 'm10-final-physical-reference-vr2-materiality-diagnostic1-v1') { throw 'Unexpected VR2 materiality contract schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'VR2 materiality contract must remain CANDIDATE until returned artifacts are reviewed.' }
if ($contract.prerequisite.vr1_status -ne 'VALIDATED') { throw 'VR1 prerequisite must remain VALIDATED.' }
if ($contract.prerequisite.vr2_execution -ne 'COMPLETE') { throw 'VR2 returned execution prerequisite drifted.' }
if ($contract.prerequisite.vr2_classification -ne 'MODEL-DISCREPANCY-BLOCKING') { throw 'VR2 blocking prerequisite drifted.' }
if ($contract.prerequisite.vr2_blocking_owner -ne 'INVERSE-COMPRESSED-LIQUID/resolved-pressure') { throw 'VR2 blocking owner drifted.' }
if ($contract.reference.runtime_substitution -ne $false) { throw 'Diagnostic cannot substitute IF97 into runtime.' }
if ($contract.reference.production_model_called_by_reference -ne $false) { throw 'Independent IF97 reference cannot call production thermodynamics.' }
if ($contract.reference.inverse_selfcheck_maximum_relative_error -ne 1e-7) { throw 'Inverse-reference self-check ceiling drifted.' }
if (($contract.reference.regions -join ',') -ne '1,2,4') { throw 'Reference region list must remain 1,2,4.' }
if ($contract.execution.baseline -ne 'integrated-operations-desktop-stable@9') { throw 'Exact-v9 baseline drifted.' }
if ($contract.execution.background_seconds -ne 600) { throw 'Background duration drifted.' }
if ($contract.execution.maximum_hold_seconds_after_load -ne 3600) { throw 'P1B hold duration drifted.' }
if ($contract.execution.sample_seconds -ne 60) { throw 'Diagnostic sampling interval drifted.' }
if ($contract.execution.late_analysis_seconds -ne 1200) { throw 'Late analysis duration drifted.' }
if ($contract.execution.late_window_seconds -ne 300 -or $contract.execution.late_window_count -ne 4) { throw 'Late window structure drifted.' }
if ($contract.execution.required_persistent_windows -ne 3) { throw 'Persistent-window rule drifted.' }
if ($contract.execution.p1b_checkpoint_reproduction_required -ne $true) { throw 'P1B checkpoint reproduction must remain required.' }
if ($contract.execution.per_step_protection_and_numerical_sentinels_required -ne $true) { throw 'Per-step sentinels must remain required.' }
if (($contract.nodes -join ',') -ne 'suction,pressure,outlet,drum,feedwater-inventory') { throw 'Required-node set drifted.' }
if (($contract.hydraulic_paths -join ',') -ne 'MCP,CHANNEL,RETURN,FEEDWATER-PUMP') { throw 'Required hydraulic-path set drifted.' }
if ($contract.counterfactual.changes_runtime_state -ne $false) { throw 'Counterfactual cannot change runtime state.' }
if ($contract.counterfactual.changes_resistance -ne $false) { throw 'Counterfactual cannot change resistance.' }
if ($contract.counterfactual.changes_pump_speed -ne $false) { throw 'Counterfactual cannot change pump speed.' }
if ($contract.counterfactual.changes_pump_boost_law -ne $false) { throw 'Counterfactual cannot change pump boost law.' }
if ($contract.counterfactual.changes_topology -ne $false) { throw 'Counterfactual cannot change topology.' }
if ($contract.counterfactual.changes_energy_transport -ne $false) { throw 'Counterfactual cannot change energy transport.' }
if ($contract.counterfactual.changes_only_pressure_inputs_to_existing_quadratic_flow_law -ne $true) { throw 'Counterfactual must remain pressure-only.' }
if ($contract.counterfactual.flow_reproduction_tolerance_kg_s -ne 1e-9) { throw 'Hydraulic-law reproduction tolerance drifted.' }
if ($contract.materiality_policy.flow_comparison_floor_kg_s -ne 0.01) { throw 'Materiality flow floor drifted.' }
if ($contract.materiality_policy.confirmed_impact_ratio -ne 1.0) { throw 'Confirmed materiality ratio drifted.' }
if ($contract.materiality_policy.not_excluded_impact_ratio -ne 0.1) { throw 'Not-excluded materiality ratio drifted.' }
if ($contract.materiality_policy.overall_confirmed_requires_windows_in_same_path -ne 3) { throw 'Confirmed persistence rule drifted.' }
if ($contract.materiality_policy.overall_not_excluded_requires_windows_in_same_path -ne 3) { throw 'Not-excluded persistence rule drifted.' }
if ($contract.materiality_policy.allowed_classifications.Count -ne 4) { throw 'Expected four materiality classifications.' }
if ($contract.authority.production_src_change_authorized -ne $false) { throw 'Production source change cannot be authorized.' }
if ($contract.authority.thermodynamic_repair_authorized -ne $false) { throw 'Thermodynamic repair cannot be authorized.' }
if ($contract.authority.thermodynamic_tolerance_change_authorized -ne $false) { throw 'Thermodynamic tolerance change cannot be authorized.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'Exact-v9 change cannot be authorized.' }
if ($contract.authority.vr3_execution_authorized -ne $false) { throw 'VR3 execution cannot be authorized.' }
if ($contract.authority.p3_r1_execution_authorized -ne $false) { throw 'P3-R1 execution cannot be authorized.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long cannot be authorized.' }
if ($contract.authority.separate_engineering_decision_required_after_artifact_review -ne $true) { throw 'Separate engineering decision must remain mandatory.' }
if ($contract.artifacts.Count -ne 7) { throw 'Diagnostic must return seven artifacts.' }

Write-Host 'M10 Final VR2 Replanning / Materiality Diagnostic 1 static contract audit: PASS'
