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

$validatorBytes = [System.IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'RP1A validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1A'
Write-Host '============================================================'
Write-Host 'Reference-domain corpus, seam-map and pre-candidate performance freeze.'
Write-Host 'No production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$returnedPlanning = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_UserReturnedAudit.txt'
Require-Text $returnedPlanning 'gate=VR2-ENGINEERING-REPAIR-PLANNING1'
Require-Text $returnedPlanning 'status=PASS-AS-AUTHORED'
Require-Text $returnedPlanning 'next-gate=RP1A-REFERENCE-DOMAIN-CORPUS-AND-SEAM-MAP-FREEZE'
Require-Text $returnedPlanning 'production-src-change-authorized=False'
Require-Text $returnedPlanning 'thermodynamic-repair-authorized=False'
Require-Text $returnedPlanning 'exact-v9-change-authorized=False'
Require-Text $returnedPlanning 'vr3-authorized=False'

$planningContractPath = 'eng/m10-final-vr2-engineering-repair-planning1-contract.json'
Require-File $planningContractPath
$planning = Get-Content -LiteralPath $planningContractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($planning.schema -ne 'm10-final-vr2-engineering-repair-planning1-v1') { throw 'Unexpected Planning 1 schema.' }
if ($planning.status -ne 'VALIDATED') { throw 'Planning 1 must be VALIDATED before RP1A.' }
if ($planning.prerequisite.engineering_classification -ne 'HYDRAULIC-MATERIALITY-CONFIRMED') { throw 'Materiality prerequisite drifted.' }
if ($planning.historical_semantics.exact_v9_must_remain_immutable -ne $true) { throw 'Exact-v9 immutability drifted.' }
if ($planning.historical_semantics.correlation_consistent_inverse_domain_must_remain_immutable -ne $true) { throw 'Existing closure immutability drifted.' }

$rp1aContractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1a-contract.json'
Require-File $rp1aContractPath
$contract = Get-Content -LiteralPath $rp1aContractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1a-v1') { throw 'Unexpected RP1A schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'RP1A contract must remain CANDIDATE before returned evidence review.' }
if ($contract.pre_execution_review.revision -ne 1 -or $contract.pre_execution_review.status -ne 'STATIC-HARDENED') { throw 'RP1A pre-execution review metadata drifted.' }
if ($contract.pre_execution_review.original_candidate_executed -ne $false) { throw 'The superseded original RP1A candidate must remain recorded as unexecuted.' }
if ($contract.pre_execution_review.reference_resolved_zero_based_column_index -ne 10 -or $contract.pre_execution_review.reference_region_zero_based_column_index -ne 11) { throw 'Frozen node CSV column binding drifted.' }
if ($contract.pre_execution_review.windows_powershell_51_validator_ascii_only -ne $true) { throw 'Windows PowerShell 5.1 ASCII validator invariant drifted.' }
if ($contract.prerequisite.returned_planning_status -ne 'PASS-AS-AUTHORED') { throw 'Returned Planning 1 prerequisite drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1A-REFERENCE-DOMAIN-CORPUS-AND-SEAM-MAP-FREEZE') { throw 'RP1A gate identity drifted.' }
if ($contract.corpus.frozen_exact_v9_node_rows -ne 360) { throw 'Frozen node corpus count drifted.' }
if ($contract.corpus.frozen_exact_v9_reference_region4_rows -ne 348) { throw 'Frozen Region-4 row count drifted.' }
if ($contract.corpus.frozen_exact_v9_reference_region1_rows -ne 12) { throw 'Frozen Region-1 row count drifted.' }
if ($contract.corpus.frozen_hydraulic_context_rows -ne 288) { throw 'Frozen hydraulic context count drifted.' }
if ($contract.corpus.expected_vr2_reference_corpus_rows -ne 40) { throw 'VR2 derived inverse corpus row count drifted.' }
if ($contract.seam_map.pressure_relative_offset -ne 1e-5) { throw 'Seam pressure offset drifted.' }
if ($contract.seam_map.quality_offset -ne 1e-6) { throw 'Seam quality offset drifted.' }
if ($contract.seam_map.maximum_region1_region2_boundary_temperature_k -ne 623.15) { throw 'Region-1/2 seam temperature ceiling drifted.' }
if (@($contract.seam_map.probe_sides).Count -ne 4) { throw 'Seam probe-side count drifted.' }
if ($contract.seam_map.probe_sides[0] -ne 'R1-SIDE' -or $contract.seam_map.probe_sides[1] -ne 'R4-LIQUID-SIDE' -or $contract.seam_map.probe_sides[2] -ne 'R4-VAPOR-SIDE' -or $contract.seam_map.probe_sides[3] -ne 'R2-SIDE') { throw 'Seam probe-side ordering or identity drifted.' }
if ($contract.seam_map.production_unresolved_rows_are_evidence_not_auto_failure -ne $true) { throw 'Production unresolved seam rows must remain evidence, not an RP1A auto-failure.' }
if ($contract.seam_map.production_phase_mismatch_rows_are_evidence_not_auto_failure -ne $true) { throw 'Production phase mismatches must remain evidence, not an RP1A auto-failure.' }
if ($contract.seam_map.deterministic_repeat_required -ne $true) { throw 'Seam deterministic repeat must remain required.' }
if ($contract.seam_map.expected_boundary_count -ne 320) { throw 'Seam boundary count drifted.' }
if ($contract.seam_map.expected_probe_rows -ne 1280) { throw 'Seam probe row count drifted.' }
if ($contract.performance.resolve_warmup_passes -ne 8 -or $contract.performance.resolve_measured_passes -ne 64) { throw 'Resolve performance protocol drifted.' }
if ($contract.performance.whole_step_warmup_steps -ne 128 -or $contract.performance.whole_step_measured_steps -ne 512) { throw 'Whole-step performance protocol drifted.' }
if ($contract.performance.historical_h28_median_wall_ratio_limit -ne 8.0) { throw 'H28 median wall ratio drifted.' }
if ($contract.performance.historical_h28_p95_wall_ratio_limit -ne 12.0) { throw 'H28 p95 wall ratio drifted.' }
if ($contract.performance.historical_h28_median_allocation_ratio_limit -ne 16.0) { throw 'H28 allocation ratio drifted.' }
if ($contract.performance.fixed_numerical_step_microseconds -ne 10000) { throw 'Fixed 10 ms numerical step metadata drifted.' }
if ($contract.performance.whole_step_baseline_is_context_not_rp1b_runtime_qualification -ne $true) { throw 'Whole-step RP1A baseline must remain contextual evidence only.' }
if ($contract.performance.candidate_timing_must_not_be_inspected_before_rp1a_ceiling_freeze -ne $true) { throw 'Pre-candidate timing freeze rule drifted.' }

if ($contract.authority.production_src_change_authorized -ne $false) { throw 'Production source change cannot be authorized by RP1A.' }
if ($contract.authority.thermodynamic_repair_authorized -ne $false) { throw 'Thermodynamic repair cannot be authorized by RP1A.' }
if ($contract.authority.thermodynamic_tolerance_change_authorized -ne $false) { throw 'Thermodynamic tolerance change cannot be authorized by RP1A.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'Exact-v9 change cannot be authorized by RP1A.' }
if ($contract.authority.rp1b_implementation_authorized_before_returned_rp1a_review -ne $false) { throw 'RP1B cannot be authorized before returned RP1A review.' }
if ($contract.authority.vr3_execution_authorized -ne $false) { throw 'VR3 cannot be authorized by RP1A.' }
if ($contract.authority.p3_r1_execution_authorized -ne $false) { throw 'P3-R1 cannot be authorized by RP1A.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long cannot be authorized by RP1A.' }

$frozenAttempt5 = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts'
Require-File (Join-Path $frozenAttempt5 '03-node-if97-inverse-map.csv')
Require-File (Join-Path $frozenAttempt5 '04-hydraulic-path-counterfactual.csv')

$nodePath = Join-Path $frozenAttempt5 '03-node-if97-inverse-map.csv'
$hydraulicPath = Join-Path $frozenAttempt5 '04-hydraulic-path-counterfactual.csv'
$expectedNodeHeader = 'probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa'
$expectedHydraulicHeader = 'probe_id,logical_step,elapsed_s,path_id,from_node,to_node,resistance_pa_s2_kg2,active_boost_pa,production_driving_pa,if97_driving_pa,canonical_flow_kg_s,production_formula_flow_kg_s,if97_pressure_only_counterfactual_flow_kg_s,reference_resolved,driving_pressure_sign_changed,abs_counterfactual_flow_shift_kg_s'
$nodeLines = @(Get-Content -LiteralPath $nodePath -Encoding UTF8)
$hydraulicLines = @(Get-Content -LiteralPath $hydraulicPath -Encoding UTF8)
if ($nodeLines.Count -lt 1 -or $nodeLines[0] -ne $expectedNodeHeader) { throw 'Frozen node corpus CSV header drifted.' }
if ($hydraulicLines.Count -lt 1 -or $hydraulicLines[0] -ne $expectedHydraulicHeader) { throw 'Frozen hydraulic corpus CSV header drifted.' }
$nodeRows = @($nodeLines | Select-Object -Skip 1 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$pathRows = @($hydraulicLines | Select-Object -Skip 1 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($nodeRows.Count -ne 360) { throw ("Frozen node corpus has {0} rows, expected 360." -f $nodeRows.Count) }
if ($pathRows.Count -ne 288) { throw ("Frozen hydraulic corpus has {0} rows, expected 288." -f $pathRows.Count) }
$nodeObjects = @($nodeLines | ConvertFrom-Csv)
if (@($nodeObjects | Where-Object { $_.reference_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen node corpus contains unresolved IF97 reference rows.' }
if (@($nodeObjects | Where-Object { $_.reference_region -eq 'REGION-4-MIXTURE' }).Count -ne 348) { throw 'Frozen node corpus Region-4 row count drifted.' }
if (@($nodeObjects | Where-Object { $_.reference_region -eq 'REGION-1' }).Count -ne 12) { throw 'Frozen node corpus Region-1 row count drifted.' }
$region4UniqueTemperatureCount = @($nodeObjects | Where-Object { $_.reference_region -eq 'REGION-4-MIXTURE' } | Group-Object -Property reference_temperature_c).Count
if ($region4UniqueTemperatureCount -ne 310) { throw ("Frozen node corpus has {0} unique Region-4 reference temperatures, expected 310." -f $region4UniqueTemperatureCount) }
if (($region4UniqueTemperatureCount + 10) -ne $contract.seam_map.expected_boundary_count) { throw 'Data-derived RP1A seam-boundary count no longer matches the frozen contract.' }

$rp1aDoc = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1A_REFERENCE_DOMAIN_CORPUS_SEAM_MAP_FREEZE.md'
Require-Text $rp1aDoc 'EXECUTION CANDIDATE'
Require-Text $rp1aDoc '360 frozen exact-v9/P1B Attempt-5 node observations'
Require-Text $rp1aDoc '288 frozen hydraulic-path rows'
Require-Text $rp1aDoc 'R1-SIDE'
Require-Text $rp1aDoc 'R4-LIQUID'
Require-Text $rp1aDoc 'R4-VAPOR'
Require-Text $rp1aDoc 'R2-SIDE'
Require-Text $rp1aDoc 'candidate resolve median wall ceiling = RP1A baseline median * 8'
Require-Text $rp1aDoc 'candidate resolve p95 wall ceiling    = RP1A baseline p95 * 12'
Require-Text $rp1aDoc 'candidate median allocation ceiling   = RP1A baseline median allocation * 16'
Require-Text $rp1aDoc 'rp1b-authorized=False'

$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1aTests.cs'
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1a'
Require-Text $testPath 'CorrelationConsistentInverseDomain'
Require-Text $testPath 'BoundaryPressureRelativeOffset = 1e-5d'
Require-Text $testPath 'BoundaryQualityOffset = 1e-6d'
Require-Text $testPath 'ResolveMeasuredPasses = 64'
Require-Text $testPath 'WholeStepMeasuredSteps = 512'
Require-Text $testPath 'CandidateResolveP95CeilingMicroseconds'
Require-Text $testPath 'ExpectedVr2ReferenceCorpusRows = 40'
Require-Text $testPath 'ExpectedSeamBoundaryCount = 320'
Require-Text $testPath 'ExpectedSeamProbeRows = 1_280'
Require-Text $testPath 'r1SideCount == ExpectedSeamBoundaryCount'
Require-Text $testPath 'r4LiquidSideCount == ExpectedSeamBoundaryCount'
Require-Text $testPath 'r4VaporSideCount == ExpectedSeamBoundaryCount'
Require-Text $testPath 'r2SideCount == ExpectedSeamBoundaryCount'
Require-Text $testPath 'seamTemperaturesInsideRegion12Boundary'
Require-Text $testPath 'Reference.InitialConditionId'
Require-Text $testPath 'Reference.Version'
Require-Text $testPath 'FrozenNodeCsvHeader'
Require-Text $testPath 'FrozenHydraulicCsvHeader'
Require-Text $testPath 'bool.Parse(parts[10])'
if ((Get-Content -LiteralPath $testPath -Raw -Encoding UTF8).Contains('bool.Parse(parts[11])')) { throw 'RP1A frozen-node parser must read reference_resolved from CSV column 10, not reference_region column 11.' }

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Get-Content -LiteralPath $modePath -Raw -Encoding UTF8).Contains('ReferenceConsistent')) {
    throw 'RP1A must not add a production reference-consistent closure mode.'
}

$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1A static audit: PASS'
