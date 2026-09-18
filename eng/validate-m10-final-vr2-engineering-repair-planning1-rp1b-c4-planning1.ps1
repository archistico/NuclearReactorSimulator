$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Require-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Required directory not found: $Path"
    }
}

function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}

function Require-Text([string]$Path, [string]$Needle) {
    $content = Read-Utf8Text $Path
    if (-not $content.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

function Require-NearDouble($Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    $value = [double]$Actual
    if ([double]::IsNaN($value) -or [double]::IsInfinity($value) -or [math]::Abs($value - $Expected) -gt $Tolerance) {
        throw ("{0} drifted: actual={1:R}, expected={2:R}, tolerance={3:R}" -f $Label, $value, $Expected, $Tolerance)
    }
}

function Get-GreatestCommonDivisor([int]$A, [int]$B) {
    $aValue = [math]::Abs($A)
    $bValue = [math]::Abs($B)
    while ($bValue -ne 0) {
        $remainder = $aValue % $bValue
        $aValue = $bValue
        $bValue = $remainder
    }
    return $aValue
}

function Require-Sha256([string]$Path, [string]$ExpectedUpperHex, [string]$Label) {
    Require-File $Path
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actual -ne $ExpectedUpperHex.ToUpperInvariant()) {
        throw ("{0} SHA-256 drifted: actual={1}, expected={2}" -f $Label, $actual, $ExpectedUpperHex)
    }
}

function Require-NormalizedTextSha256([string]$Path, [string]$ExpectedUpperHex, [string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $bytes = $utf8NoBom.GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha256.ComputeHash($bytes)
    }
    finally {
        $sha256.Dispose()
    }
    $actual = ([System.BitConverter]::ToString($hashBytes)).Replace('-', '').ToUpperInvariant()
    if ($actual -ne $ExpectedUpperHex.ToUpperInvariant()) {
        throw ("{0} normalized-text SHA-256 drifted: actual={1}, expected={2}" -f $Label, $actual, $ExpectedUpperHex)
    }
}

function Get-EvidenceValue([string]$Path, [string]$Key) {
    Require-File $Path
    $prefix = $Key + '='
    $lines = [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            $inlineValue = $line.Substring($prefix.Length).Trim()
            if ($inlineValue.Length -gt 0) {
                return $inlineValue
            }
            if (($i + 1) -ge $lines.Length) {
                throw ("Evidence key has no value in {0}: {1}" -f $Path, $Key)
            }
            return $lines[$i + 1].Trim()
        }
    }
    throw ("Evidence key not found in {0}: {1}" -f $Path, $Key)
}

$validatorPath = $MyInvocation.MyCommand.Path
$validatorBytes = [System.IO.File]::ReadAllBytes($validatorPath)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'C4 Planning 1 validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

$artifactDir = 'artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4-planning1'
if (Test-Path -LiteralPath $artifactDir) {
    Remove-Item -LiteralPath $artifactDir -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B C4 PLANNING 1 REV2'
Write-Host '============================================================'
Write-Host 'Planning only. No C4 implementation is contained in this package.'
Write-Host 'No RP1C, production repair, threshold change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-c4-planning1-contract.json'
$planPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1.md'
$adrPath = 'docs/adr/0196-c4-must-remove-resolve-time-allocation-without-changing-c3-thermodynamic-semantics.md'
$adjudicationPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_RETURNED_EVIDENCE_ADJUDICATION.md'
$returnedDir = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement5_Artifacts'
$rp1aVr2Path = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts/02-vr2-reference-point-corpus.csv'
$rp1aExactV9Path = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts/03-exact-v9-node-corpus.csv'
$rp1aHydraulicPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts/04-hydraulic-context.csv'
$rp1aSeamPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts/05-seam-probe-map.csv'
$refinement2Vr2MapPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/02-candidate-vr2-error-map.csv'
$refinement2ExactV9MapPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/03-candidate-exact-v9-node-map.csv'
$refinement2SeamPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/04-candidate-seam-map.csv'
$refinement2HydraulicMapPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/05-candidate-hydraulic-replay.csv'
$refinement5TestPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.cs'
$c3Path = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement2ShadowThermodynamicCandidates.cs'
$c2Path = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement1ShadowThermodynamicCandidates.cs'
$expectedC2NormalizedSha256 = '3731100664621D2E7E396BC12790C36EF4667F1526E5AEF0186F3DD6260274C8'
$expectedC3NormalizedSha256 = 'B92302DB5B19213407C6B8A2F3A9CD33212FF55109E3087972928B4121A87064'
$expectedR5TestNormalizedSha256 = '7B3CFEA0464051D1E658978BA2ED6F7BB0B5B057D6B64F94884C800911F6A207'

foreach ($path in @($contractPath, $planPath, $adrPath, $adjudicationPath, $rp1aVr2Path, $rp1aExactV9Path, $rp1aHydraulicPath, $rp1aSeamPath, $refinement2Vr2MapPath, $refinement2ExactV9MapPath, $refinement2SeamPath, $refinement2HydraulicMapPath, $refinement5TestPath, $c3Path, $c2Path)) {
    Require-File $path
}
Require-Directory $returnedDir

Require-Text $adjudicationPath 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED'
Require-Text $adjudicationPath 'every one of the 20,480 measured C3 calls in every process reports exactly `416` allocated bytes'
Require-Text $adjudicationPath 'C4 planning = JUSTIFIED / NEXT AUTHORIZED PLANNING GATE'
Require-Text $adjudicationPath 'C4 implementation = NOT AUTHORIZED'
Require-Text $adjudicationPath 'RP1C = NOT AUTHORIZED'

Require-Text $planPath 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $planPath 'ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK'
Require-Text $planPath 'Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
Require-Text $planPath 'FamilyId = C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE'
Require-Text $planPath 'MaximumIterativeSolveIterations = 48'
Require-Text $planPath 'UsesDirectIf97AtResolveTime = false'
Require-Text $planPath 'BitConverter.DoubleToInt64Bits'
Require-Text $planPath 'total focused process invocations = 11'
Require-Text $planPath '| 1 | 11 | 41 | 73 |'
Require-Text $planPath '| 5 | 157 | 83 | 213 |'
Require-Text $planPath 'valid negative engineering evidence'
Require-Text $planPath '02-state-semantic-equivalence.csv'
Require-Text $planPath '03-hydraulic-semantic-equivalence.csv'
Require-Text $planPath '1,679 thermodynamic state observations'
Require-Text $planPath 'VR2-SAT-360C-PONLY'
Require-Text $planPath 'exactly 1,967 observations'
Require-Text $planPath 'lane-a/process-01 ... lane-a/process-05'
Require-Text $planPath 'every one of those 204,800 calls'
Require-Text $planPath 'The statistics algorithm is frozen as part of the contract'
Require-Text $planPath 'ceil(0.95 * N) - 1'
Require-Text $planPath 'fallback invocation count must be zero'
Require-Text $planPath 'whole-region current-thread allocation delta'
Require-Text $planPath 'runner performs 11 focused `dotnet test` invocations in total'
Require-Text $planPath 'measured-pass index then restarts at `0`'
Require-Text $planPath 'C4 measured resolve allocation = 0 bytes per call'
Require-Text $planPath 'must not short-circuit the ten timing processes'
Require-Text $planPath 'value-type enum/code'
Require-Text $planPath 'Refinement 5 is the frozen C3 allocation control'
Require-Text $planPath 'Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus'
Require-Text $planPath 'Rp1bC4_MeasuresOneIndependentR1LaneRun'
Require-Text $planPath 'both methods = [Fact(Explicit = true)]'
Require-Text $planPath '--parallel none'
Require-Text $planPath '--no-build'
Require-Text $planPath 'C4-QUALIFIED-ALLOCATION-TAIL-CLOSED'
Require-Text $planPath 'Return the complete planning-audit artifact folder before implementing C4.'
Require-Text $adrPath 'Boundary 3 must not receive a special branch.'
Require-Text $adrPath 'BitConverter.DoubleToInt64Bits'
Require-Text $adrPath 'eleven focused process invocations total'
Require-Text $adrPath '310 as `REGION-1-NEAR-BOUNDARY-C2` and 10 as `REGION-1-TABLE-C2`'
Require-Text $adrPath 'whole-region current-thread allocation delta minus the sum of candidate-call deltas must equal zero'

$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-c4-planning1-v4') { throw 'C4 Planning 1 schema drifted.' }
if ($contract.revision -ne 'REV2-SECOND-PREEXECUTION-HARDENING') { throw 'C4 Planning 1 revision drifted.' }
if ($contract.status -ne 'CANDIDATE' -or $contract.scope -ne 'C4-ALLOCATION-NEUTRAL-TEST-ONLY-PLANNING') { throw 'C4 Planning 1 identity drifted.' }
if ($contract.prerequisite.refinement5_classification -ne 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED') { throw 'Refinement 5 prerequisite classification drifted.' }
if ([int]$contract.prerequisite.confirmed_boundary_index -ne 3) { throw 'Confirmed boundary index drifted.' }
Require-NearDouble $contract.prerequisite.confirmed_boundary_temperature_c 47.26746165007364 1e-12 'Confirmed boundary temperature'
if ([int]$contract.prerequisite.confirmed_independent_process_runs -ne 5) { throw 'Confirmed process-run count drifted.' }
if ([int]$contract.prerequisite.c3_allocation_bytes_per_measured_call -ne 416) { throw 'Frozen C3 allocation evidence drifted.' }
if ([int]$contract.prerequisite.c3_measured_calls_total -ne 102400) { throw 'Frozen C3 measured-call total drifted.' }
if ([int]$contract.prerequisite.c3_total_calls_over_max_ceiling -ne 8) { throw 'Frozen C3 exceedance total drifted.' }
Require-NearDouble $contract.prerequisite.c3_cross_process_max_us 3598.6 1e-9 'Frozen C3 cross-process max'
if ($contract.prerequisite.repeated_exceedance_gc_correlated -ne $true) { throw 'GC correlation prerequisite drifted.' }

if ($contract.planned_candidate.candidate_id -ne 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE') { throw 'Planned C4 candidate identity drifted.' }
if ($contract.planned_candidate.test_only -ne $true -or $contract.planned_candidate.new_versioned_source_required -ne $true -or $contract.planned_candidate.c3_source_must_remain_unchanged -ne $true) { throw 'C4 source-versioning contract drifted.' }
if ($contract.planned_candidate.boundary3_special_case_allowed -ne $false -or $contract.planned_candidate.direct_if97_at_resolve_time_allowed -ne $false) { throw 'C4 bounded-scope contract drifted.' }
if ($contract.planned_candidate.scope -ne 'ALLOCATION-MECHANICS-WITH-SEMANTICALLY-IDENTICAL-C2-PREFIX-DUPLICATION') { throw 'C4 planned scope drifted.' }
if ($contract.planned_candidate.source_file -ne 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs') { throw 'C4 planned source file drifted.' }
if ($contract.planned_candidate.type_name -ne 'Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate') { throw 'C4 planned type name drifted.' }
if ($contract.planned_candidate.implementation_topology -ne 'ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK') { throw 'C4 implementation topology drifted.' }
if ($contract.planned_candidate.interface_family_id -ne 'C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE') { throw 'C4 interface FamilyId drifted.' }
if ($contract.planned_candidate.interface_initialization_reference_point_count_formula -ne '_fallback.InitializationReferencePointCount + _denseSaturation.Nodes.Count + _prefixSaturation.Nodes.Count + _prefixLiquidReferencePointCount') { throw 'C4 initialization-reference metadata formula drifted.' }
if ([int]$contract.planned_candidate.interface_maximum_iterative_solve_iterations -ne 48 -or $contract.planned_candidate.interface_uses_direct_if97_at_resolve_time -ne $false) { throw 'C4 interface runtime metadata drifted.' }
if ($contract.planned_candidate.complete_c2_clone_allowed -ne $false -or $contract.planned_candidate.historical_c2_c3_d3_edit_allowed -ne $false -or $contract.planned_candidate.new_output_region_or_phase_labels_allowed -ne $false) { throw 'C4 topology safety flags drifted.' }
$expectedPrefix = @('MIXTURE','LIQUID-TABLE','NEAR-BOUNDARY-LIQUID')
if (@($contract.planned_candidate.duplicated_c2_prefix_components).Count -ne 3) { throw 'C4 duplicated C2 prefix component count drifted.' }
for ($i = 0; $i -lt $expectedPrefix.Count; $i++) { if ($contract.planned_candidate.duplicated_c2_prefix_components[$i] -ne $expectedPrefix[$i]) { throw ('C4 duplicated prefix drifted at index {0}.' -f $i) } }
if ($contract.planned_candidate.c2_vapor_path_clone_allowed -ne $false -or [int]$contract.planned_candidate.r1_timing_fallback_invocations_required -ne 0 -or $contract.planned_candidate.resolution_path_telemetry_required -ne $true) { throw 'C4 prefix/fallback safety contract drifted.' }
if ($contract.planned_candidate.resolution_path_telemetry_storage -ne 'VALUE-TYPE-ENUM-NO-STRING-FORMATTING-IN-MEASURED-REGION') { throw 'C4 resolution-path telemetry storage contract drifted.' }
if ($contract.static_source_hypothesis.second_allocation_risk_method -ne 'TryResolveFromTable' -or $contract.static_source_hypothesis.ireadonlylist_foreach_present -ne $true -or $contract.static_source_hypothesis.ireadonlylist_foreach_allocation_causality_proven -ne $false -or $contract.static_source_hypothesis.mixture_only_pre_resolver_sufficient_for_r1 -ne $false) { throw 'C4 REV2 static source hypothesis drifted.' }
if (Test-Path -LiteralPath $contract.planned_candidate.source_file) { throw 'Planning package must not already contain the planned C4 source file.' }

if ($contract.historical_source_pins.c2_file -ne $c2Path -or $contract.historical_source_pins.c3_file -ne $c3Path) { throw 'Historical source pin paths drifted.' }
if ($contract.historical_source_pins.hash_mode -ne 'UTF8-TEXT-NORMALIZED-LF') { throw 'Historical source hash mode drifted.' }
if ($contract.historical_source_pins.c2_sha256 -ne $expectedC2NormalizedSha256 -or $contract.historical_source_pins.c3_sha256 -ne $expectedC3NormalizedSha256) { throw 'Historical source pin values drifted from validator-authoritative constants.' }
Require-NormalizedTextSha256 $c2Path $expectedC2NormalizedSha256 'Frozen C2 source'
Require-NormalizedTextSha256 $c3Path $expectedC3NormalizedSha256 'Frozen C3 source'

if ([int]$contract.semantic_equivalence.vr2_inverse_rows -ne 39 -or [int]$contract.semantic_equivalence.exact_v9_rows -ne 360 -or [int]$contract.semantic_equivalence.seam_rows -ne 1280 -or [int]$contract.semantic_equivalence.hydraulic_context_rows -ne 288) { throw 'C4 semantic-equivalence corpus shape drifted.' }
if ([int]$contract.semantic_equivalence.state_observation_rows -ne 1679 -or [int]$contract.semantic_equivalence.hydraulic_observation_rows -ne 288 -or [int]$contract.semantic_equivalence.total_observation_rows -ne 1967) { throw 'C4 semantic-equivalence typed observation totals drifted.' }
if (([int]$contract.semantic_equivalence.vr2_inverse_rows + [int]$contract.semantic_equivalence.exact_v9_rows + [int]$contract.semantic_equivalence.seam_rows) -ne [int]$contract.semantic_equivalence.state_observation_rows) { throw 'C4 state semantic total is inconsistent with component counts.' }
if (([int]$contract.semantic_equivalence.state_observation_rows + [int]$contract.semantic_equivalence.hydraulic_observation_rows) -ne [int]$contract.semantic_equivalence.total_observation_rows) { throw 'C4 total semantic count is inconsistent.' }
if ($contract.semantic_equivalence.vr2_source_file -ne $rp1aVr2Path -or $contract.semantic_equivalence.exact_v9_source_file -ne $rp1aExactV9Path -or $contract.semantic_equivalence.seam_source_file -ne $rp1aSeamPath -or $contract.semantic_equivalence.hydraulic_source_file -ne $rp1aHydraulicPath) { throw 'C4 semantic source-file contract drifted.' }
if ($contract.semantic_equivalence.vr2_applicability_rule -ne 'FINITE-POSITIVE-SPECIFIC-VOLUME-AND-FINITE-SPECIFIC-ENERGY') { throw 'C4 VR2 applicability rule drifted.' }
foreach ($property in @('hydraulic_driving_pressure_bit_exact','hydraulic_flow_bit_exact','hydraulic_sign_change_exact','hydraulic_replay_uses_state_pressures_from_same_exact_v9_semantic_evaluation')) { if ($contract.semantic_equivalence.$property -ne $true) { throw ("Hydraulic semantic-equivalence flag must remain true: {0}" -f $property) } }
foreach ($property in @('resolved_status_exact','region_exact','phase_exact','temperature_bit_exact','pressure_bit_exact','vapor_quality_bit_exact','deterministic_repeat_required')) {
    if ($contract.semantic_equivalence.$property -ne $true) { throw ("Semantic-equivalence flag must remain true: {0}" -f $property) }
}
if ($contract.semantic_equivalence.string_comparison -ne 'ORDINAL') { throw 'C4 string comparison contract drifted.' }
if ($contract.semantic_equivalence.double_bit_comparison -ne 'BITCONVERTER-DOUBLETOINT64BITS') { throw 'C4 double-bit comparison contract drifted.' }
if ([int]$contract.semantic_equivalence.minimum_c4_repeats_per_observation -lt 2) { throw 'C4 deterministic repeat count must remain at least two.' }
if ($contract.semantic_equivalence.new_region_or_phase_labels_allowed -ne $false) { throw 'C4 may not introduce new output labels.' }

$futureEvidence = $contract.future_evidence_artifact_contract
if ($futureEvidence.artifact_root -ne 'artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4') { throw 'Future C4 artifact root drifted.' }
if ($futureEvidence.test_file -ne 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests.cs' -or $futureEvidence.test_class -ne 'M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests') { throw 'Future C4 test identity drifted.' }
if ($futureEvidence.semantic_test_method -ne 'Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus' -or $futureEvidence.timing_test_method -ne 'Rp1bC4_MeasuresOneIndependentR1LaneRun') { throw 'Future C4 method identity drifted.' }
if ($futureEvidence.test_project -ne 'tests/NuclearReactorSimulator.Simulation.Tests/NuclearReactorSimulator.Simulation.Tests.csproj' -or $futureEvidence.focused_configuration -ne 'Release') { throw 'Future C4 focused project/configuration drifted.' }
if ($futureEvidence.focused_command_shape -ne 'dotnet test --project {project} --configuration Release --no-build -- --explicit only --filter-method {fully-qualified-method} --parallel none') { throw 'Future C4 focused command shape drifted.' }
if ($futureEvidence.semantic_process_lane_and_run_environment_variables_unset -ne $true -or $futureEvidence.timing_process_lane_and_run_environment_variables_required -ne $true) { throw 'Future C4 lane/run environment isolation drifted.' }
if ($futureEvidence.explicit_xunit_required -ne $true -or $futureEvidence.parallel_none_required -ne $true -or $futureEvidence.focused_processes_use_no_build -ne $true -or $futureEvidence.ordinary_gate_requires_c4_optin_unset -ne $true -or $futureEvidence.runner_setlocal_required -ne $true) { throw 'Future C4 focused-run isolation contract drifted.' }
if ($futureEvidence.ordinary_gate_command -ne 'eng\ci-ordinary.cmd') { throw 'Future C4 ordinary-gate command drifted.' }
if ($futureEvidence.runner -ne 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-c4.cmd') { throw 'Future C4 runner identity drifted.' }
if ($futureEvidence.opt_in_environment_variable -ne 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4' -or $futureEvidence.lane_environment_variable -ne 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE' -or $futureEvidence.run_index_environment_variable -ne 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX') { throw 'Future C4 environment-variable contract drifted.' }
if (@($futureEvidence.lane_values).Count -ne 2 -or $futureEvidence.lane_values[0] -ne 'A' -or $futureEvidence.lane_values[1] -ne 'B') { throw 'Future C4 lane token contract drifted.' }
if ([int]$futureEvidence.total_process_directories -ne 10 -or [int]$futureEvidence.total_required_files -ne 59) { throw 'Future C4 evidence-tree total drifted.' }
if ([int]$futureEvidence.semantic_equivalence_process_invocations -ne 1 -or [int]$futureEvidence.cross_process_timing_invocations -ne 10 -or [int]$futureEvidence.total_focused_process_invocations -ne 11) { throw 'Future C4 focused-process shape drifted.' }
if ($futureEvidence.runner_root_reset_rule -ne 'RUNNER-ONCE-BEFORE-SEMANTIC-AND-TIMING' -or $futureEvidence.process_reset_rule -ne 'OWN-LANE-PROCESS-DIRECTORY-ONLY') { throw 'Future C4 artifact reset ownership drifted.' }
if ($futureEvidence.complete_required_file_tree_for_negative_engineering_outcomes -ne $true) { throw 'Future C4 negative-outcome complete-evidence rule drifted.' }
$runnerOwned = @($futureEvidence.aggregate_file_ownership.runner_or_adjudicator)
$semanticOwned = @($futureEvidence.aggregate_file_ownership.semantic_process)
$expectedRunnerOwned = @('01-contract-and-provenance.txt','05-allocation-closure-summary.txt','06-cross-process-run-summary.csv','07-cross-process-boundary-summary.csv','08-c4-evidence-adjudication.txt','09-rp1b-c4-summary.txt')
$expectedSemanticOwned = @('02-state-semantic-equivalence.csv','03-hydraulic-semantic-equivalence.csv','04-semantic-equivalence-summary.txt')
if ($runnerOwned.Count -ne $expectedRunnerOwned.Count -or $semanticOwned.Count -ne $expectedSemanticOwned.Count) { throw 'Future C4 aggregate ownership count drifted.' }
for ($i = 0; $i -lt $expectedRunnerOwned.Count; $i++) { if ($runnerOwned[$i] -ne $expectedRunnerOwned[$i]) { throw ('Future C4 runner/adjudicator aggregate ownership drifted at index {0}.' -f $i) } }
for ($i = 0; $i -lt $expectedSemanticOwned.Count; $i++) { if ($semanticOwned[$i] -ne $expectedSemanticOwned[$i]) { throw ('Future C4 semantic aggregate ownership drifted at index {0}.' -f $i) } }
$expectedRunnerPhases = @('STATIC-CONTRACT-PREFLIGHT','ORDINARY-RELEASE-GATE-WITH-C4-OPTIN-UNSET','SEMANTIC-EQUIVALENCE-FOCUSED-PROCESS','LANE-A-FIVE-FRESH-TIMING-PROCESSES','LANE-B-FIVE-FRESH-TIMING-PROCESSES','ADJUDICATION-AND-EVIDENCE-INTEGRITY')
if (@($futureEvidence.runner_phases).Count -ne $expectedRunnerPhases.Count) { throw 'Future C4 runner phase count drifted.' }
for ($i = 0; $i -lt $expectedRunnerPhases.Count; $i++) { if ($futureEvidence.runner_phases[$i] -ne $expectedRunnerPhases[$i]) { throw ('Future C4 runner phase drifted at index {0}.' -f $i) } }
$expectedStateSemanticHeader = 'domain,observation_index,source_identity,c3_resolved,c4_resolved,c3_region,c4_region,c3_phase,c4_phase,c3_temperature_bits,c4_temperature_bits,c3_pressure_bits,c4_pressure_bits,c3_quality_has_value,c4_quality_has_value,c3_quality_bits,c4_quality_bits,c4_repeat_count,c4_repeat_deterministic,bit_equivalent'
$expectedHydraulicSemanticHeader = 'domain,observation_index,probe_id,logical_step,path_id,from_node,to_node,c3_resolved,c4_resolved,c3_driving_pressure_bits,c4_driving_pressure_bits,c3_flow_bits,c4_flow_bits,c3_sign_changed_from_production,c4_sign_changed_from_production,c4_repeat_count,c4_repeat_deterministic,bit_equivalent'
$expectedCallHeader = 'lane,run_index,boundary_index,boundary_temperature_c,pass_index,row_index,elapsed_us,allocated_bytes,resolved,phase,resolution_path,exceeded_max_ceiling,gen0_delta,gen1_delta,gen2_delta,any_gc_collection_delta'
$expectedBoundaryHeader = 'lane,run_index,boundary_index,boundary_temperature_c,sample_count,median_us,p95_us,max_us,max_pass_index,max_row_index,calls_over_max_ceiling,exceedance_fraction,calls_with_gc_activity,exceedances_with_gc_activity,nonzero_allocation_calls,fallback_calls'
if ($futureEvidence.state_semantic_csv_header -ne $expectedStateSemanticHeader -or $futureEvidence.hydraulic_semantic_csv_header -ne $expectedHydraulicSemanticHeader -or $futureEvidence.call_timing_csv_header -ne $expectedCallHeader -or $futureEvidence.boundary_summary_csv_header -ne $expectedBoundaryHeader) { throw 'Future C4 CSV schema drifted.' }
if (Test-Path -LiteralPath $futureEvidence.test_file) { throw 'Planning package must not already contain the future C4 evidence test.' }
if (Test-Path -LiteralPath $futureEvidence.runner) { throw 'Planning package must not already contain the future C4 implementation/evidence runner.' }
$expectedAggregateArtifacts = @('01-contract-and-provenance.txt','02-state-semantic-equivalence.csv','03-hydraulic-semantic-equivalence.csv','04-semantic-equivalence-summary.txt','05-allocation-closure-summary.txt','06-cross-process-run-summary.csv','07-cross-process-boundary-summary.csv','08-c4-evidence-adjudication.txt','09-rp1b-c4-summary.txt')
if (@($futureEvidence.aggregate_artifacts).Count -ne $expectedAggregateArtifacts.Count) { throw 'Future C4 aggregate artifact count drifted.' }
for ($i = 0; $i -lt $expectedAggregateArtifacts.Count; $i++) {
    if ($futureEvidence.aggregate_artifacts[$i] -ne $expectedAggregateArtifacts[$i]) { throw ("Future C4 aggregate artifact drifted at index {0}." -f $i) }
}
if ([int]$futureEvidence.semantic_state_observation_rows -ne 1679 -or [int]$futureEvidence.semantic_hydraulic_observation_rows -ne 288 -or [int]$futureEvidence.semantic_total_observation_rows -ne 1967 -or [int]$futureEvidence.process_directories_per_lane -ne 5 -or [int]$futureEvidence.call_timing_rows_per_lane_run -ne 20480 -or [int]$futureEvidence.boundary_summary_rows_per_lane_run -ne 320) { throw 'Future C4 evidence row/directory shape drifted.' }
if (@($futureEvidence.lane_directories).Count -ne 2 -or $futureEvidence.lane_directories[0] -ne 'lane-a' -or $futureEvidence.lane_directories[1] -ne 'lane-b') { throw 'Future C4 lane directory contract drifted.' }
$expectedProcessArtifacts = @('01-process-contract.txt','02-c4-r1-call-timing.csv','03-c4-r1-boundary-summary.csv','04-runtime-context.txt','05-process-summary.txt')
if (@($futureEvidence.process_artifacts).Count -ne $expectedProcessArtifacts.Count) { throw 'Future C4 process artifact count drifted.' }
for ($i = 0; $i -lt $expectedProcessArtifacts.Count; $i++) {
    if ($futureEvidence.process_artifacts[$i] -ne $expectedProcessArtifacts[$i]) { throw ("Future C4 process artifact drifted at index {0}." -f $i) }
}
if ($futureEvidence.missing_duplicate_or_extra_required_evidence_class -ne 'C4-EVIDENCE-INCOMPLETE') { throw 'Future C4 incomplete-evidence classification drifted.' }

if ([int]$contract.allocation_closure.r1_boundary_count -ne 320 -or [int]$contract.allocation_closure.warmup_passes -ne 16 -or [int]$contract.allocation_closure.measured_passes -ne 64 -or [int]$contract.allocation_closure.measured_calls_per_lane_run -ne 20480) { throw 'C4 allocation-closure measurement shape drifted.' }
if ([int]$contract.allocation_closure.c4_required_bytes_per_measured_r1_call -ne 0 -or [int]$contract.allocation_closure.c3_frozen_control_bytes_per_call -ne 416 -or $contract.allocation_closure.measurement_loop_must_not_allocate -ne $true) { throw 'C4 allocation-closure rule drifted.' }
if ([int]$contract.allocation_closure.measurement_region_harness_required_bytes -ne 0 -or $contract.allocation_closure.sample_storage_preallocated_before_full_gc -ne $true -or $contract.allocation_closure.sample_storage_value_type_required -ne $true -or $contract.allocation_closure.per_call_sample_heap_allocation_allowed -ne $false -or $contract.allocation_closure.linq_or_interface_foreach_in_measured_harness_allowed -ne $false -or $contract.allocation_closure.whole_region_allocation_reconciliation_required -ne $true -or [int]$contract.allocation_closure.r1_timing_fallback_invocations_required -ne 0) { throw 'C4 whole-region allocation-neutral harness contract drifted.' }
if ($contract.allocation_closure.resolve_time_foreach_allowed -ne $false -or $contract.allocation_closure.resolve_time_prefix_iteration -ne 'INDEXED-FOR-WHILE-ONLY') { throw 'C4 resolve-time iteration contract drifted.' }
if ($contract.allocation_closure.c3_control_inside_semantic_or_timing_process_allowed -ne $false) { throw 'C4 process-isolation C3-control rule drifted.' }

$protocol = $contract.cross_process_protocol
if ([int]$protocol.lanes -ne 2 -or [int]$protocol.logical_runs_per_lane -ne 5 -or [int]$protocol.timing_process_invocations -ne 10 -or [int]$protocol.semantic_equivalence_process_invocations -ne 1 -or [int]$protocol.total_focused_process_invocations -ne 11) { throw 'C4 cross-process/focused-process shape drifted.' }
if ($protocol.warmup_pass_index_range -ne '0..15' -or $protocol.measured_pass_index_range -ne '0..63' -or $protocol.measured_pass_index_resets_after_warmup -ne $true) { throw 'C4 pass-index reset contract drifted.' }
if ($protocol.fresh_runtime_process_per_lane_run -ne $true -or $protocol.lane_a_and_lane_b_same_process_allowed -ne $false) { throw 'C4 lane isolation contract drifted.' }
if ($protocol.historical_order_lane -ne 'ROTATION-STRIDE-37' -or $protocol.historical_order_start_formula -ne '(pass*37)%320' -or $protocol.historical_order_row_formula -ne '(start+offset)%320') { throw 'C4 historical lane contract drifted.' }
if ($protocol.decorrelated_order_lane -ne 'PRE-FROZEN-AFFINE-PERMUTATION-PER-RUN') { throw 'C4 decorrelated lane identity drifted.' }
if ([int]$protocol.warmup_passes_per_lane_run -ne 16 -or [int]$protocol.measured_passes_per_lane_run -ne 64 -or [int]$protocol.r1_boundary_count -ne 320 -or [int]$protocol.measured_calls_per_lane_run -ne 20480 -or [int]$protocol.total_measured_c4_calls -ne 204800) { throw 'C4 per-lane measurement contract drifted.' }
foreach ($property in @('record_gc_deltas','record_allocation_delta','record_boundary_identity','record_lane_identity','pre_timing_permutation_coverage_validation_required')) {
    if ($protocol.$property -ne $true) { throw ("Cross-process evidence flag must remain true: {0}" -f $property) }
}

$expectedLaneB = @(
    [pscustomobject]@{ RunIndex = 1; BaseOffset = 11; PassAdvance = 41; PermutationStride = 73 },
    [pscustomobject]@{ RunIndex = 2; BaseOffset = 47; PassAdvance = 53; PermutationStride = 107 },
    [pscustomobject]@{ RunIndex = 3; BaseOffset = 83; PassAdvance = 61; PermutationStride = 149 },
    [pscustomobject]@{ RunIndex = 4; BaseOffset = 119; PassAdvance = 71; PermutationStride = 181 },
    [pscustomobject]@{ RunIndex = 5; BaseOffset = 157; PassAdvance = 83; PermutationStride = 213 }
)
$laneBRuns = @($protocol.decorrelated_lane_runs)
if ($laneBRuns.Count -ne 5) { throw 'C4 decorrelated-lane run count drifted.' }
for ($i = 0; $i -lt $expectedLaneB.Count; $i++) {
    $expected = $expectedLaneB[$i]
    $actual = $laneBRuns[$i]
    if ([int]$actual.run_index -ne [int]$expected.RunIndex -or [int]$actual.base_offset -ne [int]$expected.BaseOffset -or [int]$actual.pass_advance -ne [int]$expected.PassAdvance -or [int]$actual.permutation_stride -ne [int]$expected.PermutationStride) {
        throw ("C4 decorrelated-lane parameters drifted at run {0}." -f ($i + 1))
    }
    if ((Get-GreatestCommonDivisor ([int]$actual.permutation_stride) 320) -ne 1) {
        throw ("C4 decorrelated permutation stride is not coprime with 320 at run {0}." -f ($i + 1))
    }
    foreach ($phase in @('warmup','measured')) {
        $passCount = if ($phase -eq 'warmup') { 16 } else { 64 }
        for ($pass = 0; $pass -lt $passCount; $pass++) {
            $seen = @{}
            $start = (([int]$actual.base_offset + ($pass * [int]$actual.pass_advance)) % 320)
            for ($offset = 0; $offset -lt 320; $offset++) {
                $rowIndex = ($start + ($offset * [int]$actual.permutation_stride)) % 320
                if ($seen.ContainsKey($rowIndex)) {
                    throw ("C4 decorrelated permutation repeated row {0} at run {1}, phase {2}, pass {3}." -f $rowIndex, ($i + 1), $phase, $pass)
                }
                $seen[$rowIndex] = $true
            }
            if ($seen.Count -ne 320) {
                throw ("C4 decorrelated permutation coverage incomplete at run {0}, phase {1}, pass {2}." -f ($i + 1), $phase, $pass)
            }
        }
    }
}

Require-NearDouble $contract.performance_ceilings.resolve_median_us 94.8 1e-12 'C4 median ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_p95_us 158.80666666666667 1e-12 'C4 p95 ceiling'
Require-NearDouble $contract.performance_ceilings.resolve_max_us 409.30666666666673 1e-12 'C4 max ceiling'
if ([int]$contract.performance_ceilings.median_allocation_bytes -ne 2816 -or $contract.performance_ceilings.strict_single_call_max_preserved -ne $true -or $contract.performance_ceilings.no_threshold_relaxation -ne $true) { throw 'C4 frozen performance contract drifted.' }

$statistics = $contract.statistics_contract
if ($statistics.qualification_distribution -ne 'EACH-LANE-RUN-ALL-20480-MEASURED-CALLS' -or $statistics.boundary_diagnostic_distribution -ne 'EACH-BOUNDARY-WITHIN-EACH-LANE-RUN-64-MEASURED-CALLS') { throw 'C4 statistics distribution scope drifted.' }
if ($statistics.sort_order -ne 'ASCENDING-ELAPSED-MICROSECONDS' -or $statistics.median_algorithm -ne 'SORTED-MIDDLE;EVEN=ARITHMETIC-MEAN-OF-TWO-CENTRAL-VALUES' -or $statistics.p95_algorithm -ne 'NEAREST-RANK-CEIL-0.95N-MINUS-1-ZERO-BASED-CLAMPED' -or $statistics.max_algorithm -ne 'MAX-ELAPSED-MICROSECONDS') { throw 'C4 statistics algorithm drifted.' }
if ([int]$statistics.qualification_sample_count_per_lane_run -ne 20480 -or [int]$statistics.boundary_sample_count_per_lane_run -ne 64) { throw 'C4 statistics sample count drifted.' }
Require-NearDouble $statistics.percentile 0.95 1e-15 'C4 p95 percentile'
if ($statistics.stopwatch_source -ne 'STOPWATCH-GETTIMESTAMP' -or $statistics.elapsed_conversion -ne 'TICKS*1000000/STOPWATCH-FREQUENCY') { throw 'C4 elapsed-time statistics source drifted.' }

$expectedClasses = @(
    'C4-QUALIFIED-ALLOCATION-TAIL-CLOSED',
    'C4-SEMANTIC-EQUIVALENCE-FAILED',
    'C4-ALLOCATION-NOT-CLOSED',
    'C4-WALL-CLOCK-TAIL-REMAINS',
    'C4-EVIDENCE-INCOMPLETE'
)
if (@($contract.evidence_classifications).Count -ne $expectedClasses.Count) { throw 'C4 evidence-classification count drifted.' }
for ($i = 0; $i -lt $expectedClasses.Count; $i++) {
    if ($contract.evidence_classifications[$i] -ne $expectedClasses[$i]) { throw ("C4 evidence classification drifted at index {0}." -f $i) }
}

if ($contract.qualification.median_p95_scope -ne 'EACH-OF-10-LANE-RUN-DISTRIBUTIONS' -or $contract.qualification.strict_max_scope -ne 'EVERY-ONE-OF-204800-MEASURED-C4-CALLS' -or $contract.qualification.zero_allocation_scope -ne 'EVERY-ONE-OF-204800-MEASURED-C4-CALLS') { throw 'C4 qualification metric scope drifted.' }
if ([int]$contract.qualification.r1_timing_fallback_invocations_required -ne 0 -or $contract.qualification.whole_measured_region_harness_allocation_neutral_required -ne $true -or $contract.qualification.semantic_process_must_be_separate_from_timing_processes -ne $true -or $contract.qualification.runner_root_reset_must_be_single_owner -ne $true) { throw 'C4 REV2 qualification-integrity contract drifted.' }
if ($contract.qualification.negative_semantic_outcome_short_circuits_timing -ne $false) { throw 'C4 semantic-negative continuation rule drifted.' }

foreach ($property in @('semantic_bit_equivalence_required','zero_r1_allocation_required','zero_single_call_max_exceedances_required','both_ordering_lanes_required','qualification_does_not_auto_authorize_rp1c','measurement_outcome_is_evidence_not_xunit_failure','focused_test_fails_only_on_harness_or_evidence_integrity','adjudication_separate_from_evidence_generation')) {
    if ($contract.qualification.$property -ne $true) { throw ("C4 qualification flag must remain true: {0}" -f $property) }
}
$expectedPrecedence = @('C4-EVIDENCE-INCOMPLETE','C4-SEMANTIC-EQUIVALENCE-FAILED','C4-ALLOCATION-NOT-CLOSED','C4-WALL-CLOCK-TAIL-REMAINS','C4-QUALIFIED-ALLOCATION-TAIL-CLOSED')
if (@($contract.qualification.classification_precedence).Count -ne $expectedPrecedence.Count) { throw 'C4 classification precedence count drifted.' }
for ($i = 0; $i -lt $expectedPrecedence.Count; $i++) {
    if ($contract.qualification.classification_precedence[$i] -ne $expectedPrecedence[$i]) { throw ("C4 classification precedence drifted at index {0}." -f $i) }
}

$expectedPlanningOutputs = @('01-contract-and-provenance.txt','02-source-attribution.txt','03-c4-planning-summary.txt','04-preexecution-hardening-review.txt')
if (@($contract.planning_audit_outputs).Count -ne $expectedPlanningOutputs.Count) { throw 'C4 planning-audit output count drifted.' }
for ($i = 0; $i -lt $expectedPlanningOutputs.Count; $i++) { if ($contract.planning_audit_outputs[$i] -ne $expectedPlanningOutputs[$i]) { throw ('C4 planning-audit output drifted at index {0}.' -f $i) } }

if ($contract.authority.c4_implementation_authorized_before_returned_planning_audit -ne $false) { throw 'C4 implementation must remain unauthorized before returned planning audit.' }
if ($contract.authority.c4_test_only_implementation_authorized_now -ne $false) { throw 'C4 test-only implementation must remain unauthorized before returned planning adjudication.' }
if ($contract.authority.c4_test_only_implementation_authorizable_after_returned_planning_adjudication -ne $true) { throw 'Returned planning adjudication must be the only gate that can authorize the separately versioned test-only C4 implementation candidate.' }
foreach ($property in @('rp1c_selection_authorized','production_src_change_authorized','thermodynamic_repair_authorized','thermodynamic_tolerance_change_authorized','performance_threshold_change_authorized','exact_v9_change_authorized','vr3_execution_authorized','p3_r1_execution_authorized','second_replacement_long_authorized')) {
    if ($contract.authority.$property -ne $false) { throw ("Authority flag must remain false: {0}" -f $property) }
}

Require-Text $c3Path 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $c3Path 'UsesDirectIf97AtResolveTime => false'
if ((Read-Utf8Text $c3Path).Contains('C4-ALLOCATION-NEUTRAL')) { throw 'Planning package must not implement C4 inside the historical C3 source.' }
Require-Text $c2Path 'internal sealed class Rp1bExtendedTabulatedReferenceSurrogateCandidate'
Require-Text $c2Path 'private static bool TryBuildReachabilityBoundary('
Require-Text $c2Path 'var fractions = new List<double>(capacity: 2);'
Require-Text $c2Path 'fractions.OrderBy(static value => value)'
Require-Text $c2Path 'private static bool TryResolveFromTable('
Require-Text $c2Path 'foreach (var row in rows)'

$rp1aVr2Rows = @(Import-Csv -LiteralPath $rp1aVr2Path)
$rp1aExactRows = @(Import-Csv -LiteralPath $rp1aExactV9Path)
$rp1aSeamRows = @(Import-Csv -LiteralPath $rp1aSeamPath)
$rp1aHydraulicRows = @(Import-Csv -LiteralPath $rp1aHydraulicPath)
if ($rp1aVr2Rows.Count -ne 40 -or $rp1aExactRows.Count -ne 360 -or $rp1aSeamRows.Count -ne 1280 -or $rp1aHydraulicRows.Count -ne 288) { throw 'Frozen RP1A semantic source corpus shape drifted.' }
$c3Vr2Applicable = @((Import-Csv -LiteralPath $refinement2Vr2MapPath) | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' -and $_.inverse_applicable -eq 'true' })
$c3ExactSemantic = @((Import-Csv -LiteralPath $refinement2ExactV9MapPath) | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' })
$c3SeamSemantic = @((Import-Csv -LiteralPath $refinement2SeamPath) | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' })
$c3HydraulicSemantic = @((Import-Csv -LiteralPath $refinement2HydraulicMapPath) | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' })
if ($c3Vr2Applicable.Count -ne 39 -or @($c3Vr2Applicable | Where-Object { $_.candidate_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen C3 VR2 inverse-applicable semantic shape drifted.' }
if ($c3ExactSemantic.Count -ne 360 -or @($c3ExactSemantic | Where-Object { $_.candidate_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen C3 exact-v9 semantic shape drifted.' }
if ($c3SeamSemantic.Count -ne 1280 -or @($c3SeamSemantic | Where-Object { $_.candidate_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen C3 seam semantic shape drifted.' }
if ($c3HydraulicSemantic.Count -ne 288 -or @($c3HydraulicSemantic | Where-Object { $_.candidate_resolved -ne 'true' }).Count -ne 0) { throw 'Frozen C3 hydraulic semantic shape drifted.' }
$nonApplicableVr2 = @((Import-Csv -LiteralPath $refinement2Vr2MapPath) | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' -and $_.inverse_applicable -ne 'true' })
if ($nonApplicableVr2.Count -ne 1 -or $nonApplicableVr2[0].point_id -ne 'VR2-SAT-360C-PONLY' -or $nonApplicableVr2[0].candidate_region -ne 'NOT-APPLICABLE' -or $nonApplicableVr2[0].candidate_phase -ne 'BOUNDARY-ONLY') { throw 'Frozen C3 VR2 boundary-only exclusion drifted.' }

$refinement2Rows = @((Import-Csv -LiteralPath $refinement2SeamPath) | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' -and $_.probe_side -eq 'R1-SIDE' })
if ($refinement2Rows.Count -ne 320) { throw 'Frozen Refinement 2 C3 R1 row count drifted.' }
if (@($refinement2Rows | Where-Object { $_.candidate_resolved -ne 'true' -or $_.candidate_phase -ne 'SubcooledLiquid' }).Count -ne 0) { throw 'Frozen C3 R1 resolution phase shape drifted.' }
$nearBoundaryCount = @($refinement2Rows | Where-Object { $_.candidate_region -eq 'REGION-1-NEAR-BOUNDARY-C2' }).Count
$tableCount = @($refinement2Rows | Where-Object { $_.candidate_region -eq 'REGION-1-TABLE-C2' }).Count
$otherR1Count = $refinement2Rows.Count - $nearBoundaryCount - $tableCount
if ($nearBoundaryCount -ne 310 -or $tableCount -ne 10 -or $otherR1Count -ne 0) { throw 'Frozen C3 R1 region-owner shape drifted; REV2 topology premise is no longer valid.' }
if ([int]$contract.frozen_r1_c3_resolution_shape.rows -ne 320 -or $contract.frozen_r1_c3_resolution_shape.phase -ne 'SubcooledLiquid' -or [int]$contract.frozen_r1_c3_resolution_shape.region1_near_boundary_c2_rows -ne 310 -or [int]$contract.frozen_r1_c3_resolution_shape.region1_table_c2_rows -ne 10 -or [int]$contract.frozen_r1_c3_resolution_shape.other_rows -ne 0) { throw 'Frozen R1 C3 resolution-shape contract drifted.' }

if ($contract.historical_measurement_harness.hash_mode -ne 'UTF8-TEXT-NORMALIZED-LF') { throw 'Historical Refinement 5 test hash mode drifted.' }
if ($contract.historical_measurement_harness.refinement5_test_sha256 -ne $expectedR5TestNormalizedSha256) { throw 'Historical Refinement 5 test pin drifted from validator-authoritative constant.' }
Require-NormalizedTextSha256 $refinement5TestPath $expectedR5TestNormalizedSha256 'Frozen Refinement 5 timing test'
Require-Text $refinement5TestPath 'var samples = new List<R1CallTimingSample>(r1Rows.Length * ScreenMeasuredPasses);'
Require-Text $refinement5TestPath 'private sealed record R1CallTimingSample('
if ($contract.historical_measurement_harness.per_call_candidate_allocation_measurement_valid -ne $true -or $contract.historical_measurement_harness.gc_correlation_valid -ne $true -or $contract.historical_measurement_harness.candidate_only_gc_causality_proven -ne $false -or $contract.historical_measurement_harness.sample_list_created_after_gc_baseline -ne $true -or $contract.historical_measurement_harness.per_call_sample_record_heap_allocation_present -ne $true -or $contract.historical_measurement_harness.whole_measured_region_harness_allocation_neutral -ne $false) { throw 'Historical R5 harness-adjudication contract drifted.' }

$csharpFiles = @(Get-ChildItem -LiteralPath 'tests' -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
foreach ($file in $csharpFiles) {
    if ((Read-Utf8Text $file.FullName).Contains('C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE')) {
        throw ("C4 implementation already exists before planning audit return: {0}" -f $file.FullName)
    }
}

$srcFiles = @(Get-ChildItem -LiteralPath 'src' -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
if ($srcFiles.Count -eq 0) { throw 'Production C# source scan returned zero files.' }
foreach ($file in $srcFiles) {
    $text = Read-Utf8Text $file.FullName
    if ($text.Contains('C4-ALLOCATION-NEUTRAL') -or $text.Contains('RP1B-C4-PLANNING1')) {
        throw ("C4 planning identity leaked into production source: {0}" -f $file.FullName)
    }
}

$rootFiles = @(Get-ChildItem -LiteralPath $returnedDir -File)
if ($rootFiles.Count -ne 5) { throw 'Returned Refinement 5 aggregate artifact count drifted.' }
foreach ($name in @('01-contract-and-provenance.txt','02-cross-process-run-summary.csv','03-cross-process-boundary-summary.csv','04-performance-reproducibility-adjudication.txt','05-rp1b-refinement5-summary.txt')) {
    Require-File (Join-Path $returnedDir $name)
}
Require-Text (Join-Path $returnedDir '04-performance-reproducibility-adjudication.txt') 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED'
Require-Text (Join-Path $returnedDir '04-performance-reproducibility-adjudication.txt') 'c4-planning-justified='
Require-Text (Join-Path $returnedDir '04-performance-reproducibility-adjudication.txt') 'c4-implementation-authorized=False'
Require-Text (Join-Path $returnedDir '04-performance-reproducibility-adjudication.txt') 'rp1c-selection-authorized=False'

$adjudicationEvidencePath = Join-Path $returnedDir '04-performance-reproducibility-adjudication.txt'
$summaryEvidencePath = Join-Path $returnedDir '05-rp1b-refinement5-summary.txt'
if ((Get-EvidenceValue $adjudicationEvidencePath 'classification') -ne 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED') { throw 'Returned Refinement 5 adjudication classification value drifted.' }
if ((Get-EvidenceValue $adjudicationEvidencePath 'process-runs-with-any-exceedance') -ne '5') { throw 'Returned Refinement 5 process-run exceedance count drifted.' }
if ((Get-EvidenceValue $adjudicationEvidencePath 'total-calls-over-max-ceiling') -ne '8') { throw 'Returned Refinement 5 total exceedance value drifted.' }
if ((Get-EvidenceValue $adjudicationEvidencePath 'same-boundary-confirmed-count') -ne '1') { throw 'Returned Refinement 5 same-boundary confirmation count drifted.' }
if ((Get-EvidenceValue $adjudicationEvidencePath 'c4-planning-justified') -ne 'True') { throw 'Returned Refinement 5 no longer authorizes C4 planning.' }
if ((Get-EvidenceValue $summaryEvidencePath 'status') -ne 'PASS-CROSS-PROCESS-REPRODUCIBILITY-EVIDENCE-COMPLETE') { throw 'Returned Refinement 5 summary status drifted.' }
if ((Get-EvidenceValue $summaryEvidencePath 'classification') -ne 'C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED') { throw 'Returned Refinement 5 summary classification drifted.' }
if ((Get-EvidenceValue $summaryEvidencePath 'c4-planning-justified') -ne 'True') { throw 'Returned Refinement 5 summary no longer justifies C4 planning.' }

$rootDirectories = @(Get-ChildItem -LiteralPath $returnedDir -Directory)
$expectedProcessDirectoryNames = 1..5 | ForEach-Object { 'process-{0:D2}' -f $_ }
if ($rootDirectories.Count -ne 5) { throw 'Returned Refinement 5 process-directory count drifted.' }
foreach ($directory in $rootDirectories) {
    if ($expectedProcessDirectoryNames -notcontains $directory.Name) { throw ("Unexpected returned Refinement 5 process directory: {0}" -f $directory.Name) }
}

$crossRunRows = @(Import-Csv -LiteralPath (Join-Path $returnedDir '02-cross-process-run-summary.csv'))
if ($crossRunRows.Count -ne 5) { throw 'Returned Refinement 5 cross-process run-summary row count drifted.' }
$crossBoundaryRows = @(Import-Csv -LiteralPath (Join-Path $returnedDir '03-cross-process-boundary-summary.csv'))
if ($crossBoundaryRows.Count -ne 320) { throw 'Returned Refinement 5 cross-process boundary-summary row count drifted.' }
$confirmedBoundaryRows = @($crossBoundaryRows | Where-Object { $_.same_boundary_slow_path_confirmed -eq 'True' })
if ($confirmedBoundaryRows.Count -ne 1) { throw 'Returned Refinement 5 confirmed-boundary row count drifted.' }
$confirmedBoundary = $confirmedBoundaryRows[0]
if ([int]$confirmedBoundary.boundary_index -ne 3 -or [int]$confirmedBoundary.process_runs_with_exceedance -ne 5 -or [int]$confirmedBoundary.total_calls_over_max_ceiling -ne 5 -or [int]$confirmedBoundary.total_exceedances_with_gc_activity -ne 5) { throw 'Returned Refinement 5 confirmed boundary aggregate shape drifted.' }
Require-NearDouble $confirmedBoundary.cross_process_max_us 3598.6 1e-9 'Returned Refinement 5 confirmed-boundary cross-process max'

$totalCalls = 0
$totalExceedances = 0
$boundary3GcExceedanceRuns = 0
$sourceLines = New-Object System.Collections.Generic.List[string]
for ($run = 1; $run -le 5; $run++) {
    $processDir = Join-Path $returnedDir ("process-{0:D2}" -f $run)
    Require-Directory $processDir
    $processFiles = @(Get-ChildItem -LiteralPath $processDir -File)
    $processDirectories = @(Get-ChildItem -LiteralPath $processDir -Directory)
    if ($processFiles.Count -ne 5 -or $processDirectories.Count -ne 0) { throw ("Refinement 5 process {0} artifact shape drifted." -f $run) }
    foreach ($name in @('01-process-contract.txt','02-c3-r1-call-timing.csv','03-c3-r1-boundary-summary.csv','04-runtime-context.txt','05-process-summary.txt')) {
        Require-File (Join-Path $processDir $name)
    }

    $rows = @((Import-Csv -LiteralPath (Join-Path $processDir '02-c3-r1-call-timing.csv')))
    if ($rows.Count -ne 20480) { throw ("Refinement 5 process {0} call count drifted." -f $run) }
    $totalCalls += $rows.Count

    $badAllocation = @($rows | Where-Object { [int]$_.allocated_bytes -ne 416 })
    if ($badAllocation.Count -ne 0) { throw ("Refinement 5 process {0} no longer has uniform 416-byte C3 allocation." -f $run) }

    $exceeders = @($rows | Where-Object { $_.exceeded_max_ceiling -eq 'True' })
    if ($exceeders.Count -lt 1) { throw ("Refinement 5 process {0} lost its returned exceedance evidence." -f $run) }
    $totalExceedances += $exceeders.Count

    $boundary3 = @($exceeders | Where-Object { [int]$_.boundary_index -eq 3 -and [int]$_.pass_index -eq 58 -and $_.any_gc_collection_delta -eq 'True' -and [int]$_.gen0_delta -eq 1 })
    if ($boundary3.Count -ne 1) { throw ("Refinement 5 process {0} boundary-3/pass-58/Gen0 recurrence drifted." -f $run) }
    $boundary3GcExceedanceRuns++

    $runtimePath = Join-Path $processDir '04-runtime-context.txt'
    Require-Text $runtimePath 'gc-gen0-collections=1'
    Require-Text $runtimePath 'gc-gen1-collections=0'
    Require-Text $runtimePath 'gc-gen2-collections=0'

    $sourceLines.Add(("process-{0:D2}: calls={1}; exceedances={2}; allocation=416B/call; boundary3-pass58-gen0=True" -f $run, $rows.Count, $exceeders.Count))
}

if ($totalCalls -ne 102400) { throw 'Returned Refinement 5 total call count drifted.' }
if ($totalExceedances -ne 8) { throw 'Returned Refinement 5 total exceedance count drifted.' }
if ($boundary3GcExceedanceRuns -ne 5) { throw 'Returned Refinement 5 cross-process boundary-3 GC recurrence drifted.' }

$contractOutput = @(
    'status=PASS-AS-AUTHORED',
    'gate=M10-FINAL-VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-C4-PLANNING1-REV2',
    'contract-schema=v4',
    'revision=REV2-SECOND-PREEXECUTION-HARDENING',
    'refinement5-classification=C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED',
    'returned-refinement5-calls=102400',
    'returned-refinement5-exceedances=8',
    'returned-refinement5-boundary3-gc-runs=5',
    'c2-c3-normalized-text-sha256-pins-verified=True',
    'c3-immutable=True',
    'c4-implementation-contained=False',
    'c4-topology=ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK',
    'c4-family-id=C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE',
    'c4-maximum-iterative-solve-iterations=48',
    'c4-uses-direct-if97-at-resolve-time=False',
    'semantic-focused-process-invocations-planned=1',
    'timing-focused-process-invocations-planned=10',
    'focused-process-invocations-planned=11',
    'future-semantic-method=Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus',
    'future-timing-method=Rp1bC4_MeasuresOneIndependentR1LaneRun',
    'future-focused-methods-explicit=True',
    'future-focused-parallel-none=True',
    'future-focused-no-build=True',
    'future-focused-configuration=Release',
    'future-semantic-lane-run-env-unset=True',
    'future-timing-lane-run-env-required=True',
    'future-ordinary-c4-optin-unset=True',
    'lane-a-lane-b-same-process-allowed=False',
    'negative-engineering-outcome-is-xunit-failure=False',
    'future-semantic-state-observation-rows=1679',
    'future-semantic-hydraulic-observation-rows=288',
    'future-semantic-total-observation-rows=1967',
    'future-aggregate-artifact-count=9',
    'future-process-directory-count=10',
    'future-required-file-count=59',
    'negative-semantic-short-circuit-timing=False',
    'aggregate-evidence-ownership-frozen=True',
    'r1-c3-subcooled-rows=320',
    'r1-c3-near-boundary-c2-rows=310',
    'r1-c3-table-c2-rows=10',
    'mixture-only-pre-resolver-sufficient=False',
    'historical-r5-whole-region-harness-allocation-neutral=False',
    'historical-r5-per-call-416-measurement-valid=True',
    'future-artifact-root=artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4',
    'c4-test-only-implementation-authorized-now=False',
    'c4-test-only-implementation-authorizable-after-returned-planning-adjudication=True',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), $contractOutput, [System.Text.Encoding]::UTF8)

$sourceOutput = New-Object System.Collections.Generic.List[string]
$sourceOutput.Add('status=STATIC-SOURCE-ATTRIBUTION-CONSISTENT')
$sourceOutput.Add('candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE')
$sourceOutput.Add('observed-allocation-bytes-per-call=416')
$sourceOutput.Add('source-hypothesis-type=Rp1bRefinementSaturationTable')
$sourceOutput.Add('source-hypothesis-method=TryBuildReachabilityBoundary')
$sourceOutput.Add('temporary-list-present=True')
$sourceOutput.Add('linq-orderby-present=True')
$sourceOutput.Add('exact-416-byte-causality-proven=False')
$sourceOutput.Add('historical-method-private=True')
$sourceOutput.Add('historical-c2-sealed=True')
$sourceOutput.Add('planned-c4-private-method-override=False')
$sourceOutput.Add('planned-c4-complete-c2-clone=False')
$sourceOutput.Add('planned-c4-mixture-pre-resolver=True')
$sourceOutput.Add('planned-c4-liquid-table-prefix=True')
$sourceOutput.Add('planned-c4-near-boundary-liquid-prefix=True')
$sourceOutput.Add('planned-c4-vapor-path-clone=False')
$sourceOutput.Add('r1-fallback-invocations-required=0')
$sourceOutput.Add('resolution-path-telemetry=VALUE-TYPE-ENUM-NO-STRING-FORMATTING-IN-MEASURED-REGION')
$sourceOutput.Add('tryresolvefromtable-interface-foreach-present=True')
$sourceOutput.Add('exact-interface-foreach-byte-causality-proven=False')
$sourceOutput.Add('r1-c3-near-boundary-c2-rows=310')
$sourceOutput.Add('r1-c3-table-c2-rows=10')
$sourceOutput.Add('boundary3-special-case-planned=False')
foreach ($line in $sourceLines) { $sourceOutput.Add($line) }
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-source-attribution.txt'), $sourceOutput, [System.Text.Encoding]::UTF8)

$summaryOutput = @(
    'status=PASS-AS-AUTHORED',
    'classification=C4-PLANNING-CONTRACT-FROZEN',
    'planned-candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
    'planned-source=Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs',
    'semantic-bit-equivalence-required=True',
    'semantic-double-comparison=BITCONVERTER-DOUBLETOINT64BITS',
    'c4-r1-allocation-required-bytes-per-call=0',
    'ordering-lanes=2',
    'logical-runs-per-lane=5',
    'fresh-process-per-lane-run=True',
    'semantic-focused-process-invocations=1',
    'timing-focused-process-invocations=10',
    'focused-process-invocations=11',
    'qualification-median-p95-scope=EACH-OF-10-LANE-RUN-DISTRIBUTIONS',
    'qualification-max-scope=EVERY-ONE-OF-204800-MEASURED-C4-CALLS',
    'qualification-zero-allocation-scope=EVERY-ONE-OF-204800-MEASURED-C4-CALLS',
    'lane-b-permutations-pre-frozen=True',
    'measured-pass-index-resets-after-warmup=True',
    'whole-measured-region-harness-allocation-neutral-required=True',
    'r1-timing-fallback-invocations-required=0',
    'runner-root-reset-owner=RUNNER-ONCE',
    'strict-resolve-max-us=409.30666666666673',
    'negative-qualification-evidence-must-not-fail-harness=True',
    'negative-semantic-outcome-short-circuits-timing=False',
    'c3-control-inside-c4-focused-processes=False',
    'c4-implementation-now-contained=False',
    'next-authorized-action=RETURN-COMPLETE-PLANNING-ARTIFACTS-FOR-ADJUDICATION',
    'return-planning-artifacts-before-implementation=True',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-c4-planning-summary.txt'), $summaryOutput, [System.Text.Encoding]::UTF8)

$reviewOutput = @(
    'status=REV2-PREEXECUTION-REVIEW-PASS',
    'finding-1=MIXTURE-ONLY-PRE-RESOLVER-STRUCTURALLY-INSUFFICIENT-FOR-R1',
    'finding-1-evidence=320-SUBCOOLED-LIQUID;310-NEAR-BOUNDARY;10-TABLE',
    'finding-2=R5-WHOLE-MEASURED-REGION-HARNESS-NOT-ALLOCATION-NEUTRAL',
    'finding-2-boundary=PER-CALL-416-MEASUREMENT-VALID;GC-CORRELATION-VALID;CANDIDATE-ONLY-GC-CAUSALITY-NOT-PROVEN',
    'correction=C3-PRECEDENCE+C2-MIXTURE-LIQUID-PREFIX+IMMUTABLE-C2-FALLBACK',
    'future-harness=PREALLOCATED-VALUE-TYPE-ZERO-HARNESS-ALLOCATION',
    'future-processes=1-SEMANTIC+10-TIMING=11-FOCUSED',
    'artifact-root-reset=RUNNER-ONLY-ONCE',
    'aggregate-ownership=SEMANTIC-02-04;RUNNER-ADJUDICATOR-01-05-09',
    'measured-pass-index=RESTARTS-AT-0-AFTER-WARMUP',
    'c4-implementation-contained=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-hardening-review.txt'), $reviewOutput, [System.Text.Encoding]::UTF8)

Write-Host 'C4 Planning 1 REV2 static audit: PASS-AS-AUTHORED'
Write-Host ("Artifacts: {0}" -f $artifactDir)
