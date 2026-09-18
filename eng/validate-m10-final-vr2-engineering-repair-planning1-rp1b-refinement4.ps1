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
    throw 'RP1B Refinement 4 validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 4'
Write-Host '============================================================'
Write-Host 'Immutable C3 R1-seam worst-case localization and reproducibility only.'
Write-Host 'No C4, RP1C selection, production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$returnedRefinement3 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement3_UserReturnedSummary.txt'
Require-Text $returnedRefinement3 'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT3'
Require-Text $returnedRefinement3 'status=VALIDATED-PERFORMANCE-ATTRIBUTION-EVIDENCE'
Require-Text $returnedRefinement3 'returned-artifact-set=COMPLETE-6-OF-6'
Require-Text $returnedRefinement3 'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $returnedRefinement3 'candidate-source-changed=False'
Require-Text $returnedRefinement3 'exact-v9-resolve-max-us=150.5'
Require-Text $returnedRefinement3 'targeted-repeat-max-us=162.7'
Require-Text $returnedRefinement3 'seam-resolve-max-us=3667.1'
Require-Text $returnedRefinement3 'r1-side-sample-count=5120'
Require-Text $returnedRefinement3 'r1-side-calls-over-max-ceiling=1'
Require-Text $returnedRefinement3 'r1-side-max-us=3667.1'
Require-Text $returnedRefinement3 'attribution=SEAM-WORST-CASE-BLOCKING'
Require-Text $returnedRefinement3 'refinement4-authorized=C3-UNCHANGED-R1-SEAM-LOCALIZATION-REPRODUCIBILITY-ONLY'
Require-Text $returnedRefinement3 'c4-authorized=False'
Require-Text $returnedRefinement3 'rp1c-selection-authorized=False'
Require-Text $returnedRefinement3 'production-src-change-authorized=False'
Require-Text $returnedRefinement3 'next-gate=RP1B-REFINEMENT4-C3-R1-SEAM-WORST-CASE-LOCALIZATION-REPRODUCIBILITY'

$frozenRefinement3 = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement3_Artifacts'
$refinement3Expected = @(
    '01-contract-and-provenance.txt',
    '02-c3-exact-v9-state-timing.csv',
    '03-c3-tail-target-repeats.csv',
    '04-c3-seam-side-timing.csv',
    '05-c3-performance-qualification.txt',
    '06-rp1b-refinement3-summary.txt'
)
foreach ($name in $refinement3Expected) { Require-File (Join-Path $frozenRefinement3 $name) }
Require-Text (Join-Path $frozenRefinement3 '06-rp1b-refinement3-summary.txt') 'status=PASS-PERFORMANCE-ATTRIBUTION-EVIDENCE-COMPLETE'
Require-Text (Join-Path $frozenRefinement3 '06-rp1b-refinement3-summary.txt') 'exact-v9-resolve-max-us=150.5'
Require-Text (Join-Path $frozenRefinement3 '06-rp1b-refinement3-summary.txt') 'seam-resolve-max-us=3667.1'
Require-Text (Join-Path $frozenRefinement3 '06-rp1b-refinement3-summary.txt') 'attribution=SEAM-WORST-CASE-BLOCKING'
Require-Text (Join-Path $frozenRefinement3 '05-c3-performance-qualification.txt') 'targeted-repeat-max-us=162.7'
Require-Text (Join-Path $frozenRefinement3 '05-c3-performance-qualification.txt') 'strict-performance-contract-met=False'

$refinement3StateRows = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement3 '02-c3-exact-v9-state-timing.csv')))
$refinement3TargetRows = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement3 '03-c3-tail-target-repeats.csv')))
$refinement3SeamRows = @((Import-Csv -LiteralPath (Join-Path $frozenRefinement3 '04-c3-seam-side-timing.csv')))
if ($refinement3StateRows.Count -ne 360) { throw 'Frozen Refinement 3 exact-v9 state timing row count drifted.' }
if ($refinement3TargetRows.Count -ne 12) { throw 'Frozen Refinement 3 targeted state count drifted.' }
if ($refinement3SeamRows.Count -ne 5) { throw 'Frozen Refinement 3 seam-side timing row count drifted.' }
$r1Historical = @($refinement3SeamRows | Where-Object { $_.probe_side -eq 'R1-SIDE' })
if ($r1Historical.Count -ne 1) { throw 'Frozen Refinement 3 R1-SIDE row count drifted.' }
if ([int]$r1Historical[0].sample_count -ne 5120) { throw 'Frozen Refinement 3 R1-SIDE sample count drifted.' }
if ([int]$r1Historical[0].calls_over_max_ceiling -ne 1) { throw 'Frozen Refinement 3 R1-SIDE exceedance count drifted.' }
Require-NearDouble $r1Historical[0].max_us 3667.1 1e-9 'Frozen Refinement 3 R1-SIDE max'
foreach ($side in @('R2-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE')) {
    $row = @($refinement3SeamRows | Where-Object { $_.probe_side -eq $side })
    if ($row.Count -ne 1 -or [int]$row[0].calls_over_max_ceiling -ne 0) { throw ("Frozen non-R1 seam exceedance drifted: {0}" -f $side) }
}

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-refinement4-contract.json'
Require-File $contractPath
$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1b-refinement4-v1') { throw 'Unexpected RP1B Refinement 4 schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'RP1B Refinement 4 contract must remain CANDIDATE before returned evidence review.' }
if ($contract.prerequisite.rp1a_status -ne 'VALIDATED' -or $contract.prerequisite.rp1b_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'RP1A/RP1B prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement1_status -ne 'VALIDATED-EVIDENCE-MATRIX' -or $contract.prerequisite.rp1b_refinement2_status -ne 'VALIDATED-EVIDENCE-MATRIX') { throw 'Earlier refinement prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement3_status -ne 'VALIDATED-PERFORMANCE-ATTRIBUTION-EVIDENCE') { throw 'Refinement 3 prerequisite drifted.' }
if ($contract.prerequisite.rp1b_refinement3_returned_artifact_set -ne 'COMPLETE-6-OF-6') { throw 'Refinement 3 artifact-set prerequisite drifted.' }
if ($contract.prerequisite.engineering_review -ne 'C3-R1-SEAM-WORST-CASE-LOCALIZATION-REQUIRED') { throw 'Refinement 4 engineering-review prerequisite drifted.' }
if ($contract.prerequisite.next_gate -ne 'RP1B-REFINEMENT4-C3-R1-SEAM-WORST-CASE-LOCALIZATION-REPRODUCIBILITY') { throw 'Refinement 4 gate identity drifted.' }
if ($contract.frozen_candidate.id -ne 'C3-VAPOR-SEAM-COMPLETE-SURROGATE') { throw 'C3 identity drifted.' }
if ($contract.frozen_candidate.source_identity -ne 'UNCHANGED-FROM-REFINEMENT2') { throw 'C3 source identity must remain frozen.' }
if ($contract.frozen_candidate.uses_direct_if97_at_resolve_time -ne $false -or $contract.frozen_candidate.c4_created -ne $false) { throw 'C3/C4 frozen candidate contract drifted.' }
Require-NearDouble $contract.frozen_candidate.refinement3_exact_v9_resolve_max_us 150.5 1e-12 'Frozen Refinement 3 exact-v9 max'
Require-NearDouble $contract.frozen_candidate.refinement3_targeted_repeat_max_us 162.7 1e-12 'Frozen Refinement 3 targeted max'
Require-NearDouble $contract.frozen_candidate.refinement3_seam_resolve_max_us 3667.1 1e-9 'Frozen Refinement 3 seam max'
if ($contract.historical_r1_observation.sample_count -ne 5120 -or $contract.historical_r1_observation.calls_over_max_ceiling -ne 1) { throw 'Historical R1 observation counts drifted.' }
Require-NearDouble $contract.historical_r1_observation.max_us 3667.1 1e-9 'Historical R1 observation max'
if ($contract.historical_r1_observation.boundary_identity_known -ne $false -or $contract.historical_r1_observation.historical_exceedance_must_be_preserved_even_if_not_reobserved -ne $true) { throw 'Historical R1 interpretation drifted.' }
if ($contract.frozen_corpus.seam_rows -ne 1280 -or $contract.frozen_corpus.r1_side_rows -ne 320 -or $contract.frozen_corpus.seam_boundary_count -ne 320) { throw 'Frozen seam corpus counts drifted.' }
Require-NearDouble $contract.frozen_corpus.seam_pressure_relative_offset 1e-5 1e-15 'Seam pressure relative offset'
Require-NearDouble $contract.frozen_corpus.seam_quality_offset 1e-6 1e-16 'Seam quality offset'
if ($contract.frozen_corpus.rp1a_regenerated -ne $false -or $contract.frozen_corpus.refinement3_regenerated -ne $false) { throw 'Frozen evidence must not be regenerated.' }
if ($contract.localization_protocol.screen_warmup_passes -ne 16 -or $contract.localization_protocol.screen_measured_passes -ne 64 -or $contract.localization_protocol.screen_rotation_stride -ne 37) { throw 'R1 screen protocol drifted.' }
if ($contract.localization_protocol.screen_measured_calls -ne 20480) { throw 'R1 screen measured-call count drifted.' }
if ($contract.localization_protocol.target_top_count -ne 12 -or $contract.localization_protocol.target_warmup_calls -ne 64) { throw 'R1 target selection/warmup protocol drifted.' }
if ($contract.localization_protocol.target_measured_blocks -ne 16 -or $contract.localization_protocol.target_calls_per_block -ne 128 -or $contract.localization_protocol.target_calls_per_boundary -ne 2048) { throw 'R1 target repeat protocol drifted.' }
if ($contract.localization_protocol.target_selection_includes_all_screen_exceeders -ne $true -or $contract.localization_protocol.target_selection_includes_top_p95 -ne $true -or $contract.localization_protocol.target_selection_includes_top_max -ne $true) { throw 'R1 target selection semantics drifted.' }
if ($contract.localization_protocol.gc_collection_deltas_are_recorded_outside_timed_region -ne $true -or $contract.localization_protocol.single_call_identity_is_recorded_for_screen -ne $true -or $contract.localization_protocol.strict_single_call_max_is_not_replaced_by_statistical_tail -ne $true) { throw 'R1 timing/attribution semantics drifted.' }
Require-NearDouble $contract.performance_ceilings.resolve_max_us 409.30666666666673 1e-12 'Maximum performance ceiling'
if ($contract.performance_ceilings.no_threshold_relaxation -ne $true) { throw 'Performance threshold relaxation is forbidden.' }
if ($contract.interpretation.clean_refinement4_run_does_not_erase_historical_refinement3_exceedance -ne $true -or $contract.interpretation.evidence_gate_may_pass_with_reproduced_exceedance -ne $true) { throw 'Refinement 4 evidence-gate semantics drifted.' }
if ($contract.interpretation.rp1c_requires_returned_artifact_review -ne $true -or $contract.interpretation.c4_requires_returned_artifact_review -ne $true) { throw 'Returned-review authority semantics drifted.' }

$expectedOutputs = @(
    '01-contract-and-provenance.txt',
    '02-c3-r1-seam-call-timing.csv',
    '03-c3-r1-boundary-summary.csv',
    '04-c3-r1-target-repeats.csv',
    '05-c3-r1-runtime-context.txt',
    '06-c3-r1-performance-attribution.txt',
    '07-rp1b-refinement4-summary.txt'
)
if (@($contract.outputs).Count -ne $expectedOutputs.Count) { throw 'Refinement 4 output artifact count drifted.' }
for ($index = 0; $index -lt $expectedOutputs.Count; $index++) {
    if ($contract.outputs[$index] -ne $expectedOutputs[$index]) { throw ("Refinement 4 output artifact drifted at index {0}." -f $index) }
}

$authority = $contract.authority
foreach ($property in @(
    'rp1c_selection_authorized_before_returned_refinement4_review',
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
foreach ($name in @('05-seam-probe-map.csv','06-performance-baseline.csv','07-rp1a-summary.txt')) {
    Require-File (Join-Path $frozenRp1a $name)
}
$seamRows = @((Import-Csv -LiteralPath (Join-Path $frozenRp1a '05-seam-probe-map.csv')))
if ($seamRows.Count -ne 1280) { throw 'Frozen RP1A seam row count drifted.' }
$r1Rows = @($seamRows | Where-Object { $_.probe_side -eq 'R1-SIDE' })
if ($r1Rows.Count -ne 320) { throw 'Frozen RP1A R1-SIDE row count drifted.' }
if (@($r1Rows | Group-Object -Property boundary_index | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Frozen R1 boundary keys are not unique.' }
if (@($r1Rows | Where-Object { $_.reference_phase -ne 'SubcooledLiquid' }).Count -ne 0) { throw 'Frozen R1 reference phase drifted.' }

$candidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement2ShadowThermodynamicCandidates.cs'
$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement4Tests.cs'
Require-Text $candidatePath 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $candidatePath 'UsesDirectIf97AtResolveTime => false'
if ((Read-Utf8Text $candidatePath).Contains('C4-')) { throw 'Refinement 4 must not introduce C4 into the frozen C3/D3 source file.' }
Require-Text $testPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement4Tests'
Require-Text $testPath 'Rp1bRefinement4_LocalizesAndTestsReproducibilityOfImmutableC3R1SeamWorstCase'
Require-Text $testPath 'ScreenMeasuredPasses = 64'
Require-Text $testPath 'TargetMeasuredBlocks = 16'
Require-Text $testPath 'TargetCallsPerBlock = 128'
Require-Text $testPath 'GC.CollectionCount(0)'
Require-Text $testPath 'historical-refinement3-exceedance-preserved=True'
Require-Text $testPath '07-rp1b-refinement4-summary.txt'
$testSource = Read-Utf8Text $testPath
if ($testSource.Contains('Assert.Single(') -and $testSource.Contains('Assert.Single(r1Rows.Where(')) {
    throw 'xUnit2031-prone Assert.Single filtering pattern reintroduced.'
}
if ($testSource.Contains('Assert.True(seamLines.Any(')) {
    throw 'xUnit2012-prone collection-existence assertion reintroduced.'
}
Require-Text $testPath 'Assert.Contains(seamLines, static line =>'

$runnerPath = 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement4.cmd'
Require-Text $runnerPath 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT4=1'
Require-Text $runnerPath 'validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement4.ps1'
Require-Text $runnerPath 'M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement4Tests.Rp1bRefinement4_LocalizesAndTestsReproducibilityOfImmutableC3R1SeamWorstCase'
Require-Text $runnerPath '--explicit only'
Require-Text $runnerPath '--parallel none'
foreach ($name in $expectedOutputs) { Require-Text $runnerPath $name }

$srcFiles = @(Get-ChildItem -LiteralPath 'src' -Recurse -File | Where-Object {
    $_.Extension -eq '.cs' -and
    $_.FullName -notmatch '[\/](bin|obj)[\/]'
})
if ($srcFiles.Count -eq 0) { throw 'Production C# source scan returned zero files.' }
foreach ($source in $srcFiles) {
    if ($source.Extension -ne '.cs' -or $source.FullName -match '[\/](bin|obj)[\/]') {
        throw ("Generated/non-C# file entered production source scan: {0}" -f $source.FullName)
    }
    $content = Read-Utf8Text $source.FullName
    if ($content.Contains('C3-VAPOR-SEAM-COMPLETE-SURROGATE') -or $content.Contains('RP1B-REFINEMENT4') -or $content.Contains('C4-')) {
        throw ("RP1B Refinement 4 identity leaked into production source: {0}" -f $source.FullName)
    }
}

$modePath = 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs'
Require-Text $modePath 'CorrelationConsistentInverseDomain = 1'
if ((Read-Utf8Text $modePath).Contains('ReferenceConsistent')) { throw 'RP1B Refinement 4 must not add a production reference-consistent closure mode.' }
$exactV9Path = 'src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.cs'
Require-Text $exactV9Path 'new("integrated-operations-desktop-stable", 9)'

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 4 static audit: PASS'
