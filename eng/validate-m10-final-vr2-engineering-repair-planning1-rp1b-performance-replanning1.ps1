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
        return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
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

function Require-NearDouble($Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    $value = [double]$Actual
    if ([double]::IsNaN($value) -or [double]::IsInfinity($value) -or [math]::Abs($value - $Expected) -gt $Tolerance) {
        throw ("{0} drifted: actual={1:R}, expected={2:R}, tolerance={3:R}" -f $Label, $value, $Expected, $Tolerance)
    }
}

$validatorPath = $MyInvocation.MyCommand.Path
$validatorBytes = [System.IO.File]::ReadAllBytes($validatorPath)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'Performance replanning validator source must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B PERFORMANCE REPLANNING 1'
Write-Host '============================================================'
Write-Host 'Planning/adjudication only on immutable C3 and returned R3/R4 evidence.'
Write-Host 'No C4, RP1C selection, production repair, tolerance change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1b-performance-replanning1-contract.json'
$planPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_PERFORMANCE_REPLANNING1.md'
$adrPath = 'docs/adr/0195-separate-c3-algorithmic-performance-from-managed-runtime-wall-clock-tail-before-repair-selection.md'
$returnedSummary = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement4_UserReturnedSummary.txt'
$attempt1Summary = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_PerformanceReplanning1_Attempt1Summary.txt'
$returnedDir = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement4_Artifacts'

Require-File $contractPath
Require-File $planPath
Require-File $adrPath
Require-File $returnedSummary
Require-File $attempt1Summary
foreach ($name in @(
    '01-contract-and-provenance.txt',
    '02-c3-r1-seam-call-timing.csv',
    '03-c3-r1-boundary-summary.csv',
    '04-c3-r1-target-repeats.csv',
    '05-c3-r1-runtime-context.txt',
    '06-c3-r1-performance-attribution.txt',
    '07-rp1b-refinement4-summary.txt')) {
    Require-File (Join-Path $returnedDir $name)
}

Require-Text $returnedSummary 'status=PASS-R1-SEAM-LOCALIZATION-EVIDENCE-COMPLETE'
Require-Text $returnedSummary 'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE'
Require-Text $returnedSummary 'candidate-source-changed=False'
Require-Text $returnedSummary 'c4-created=False'
Require-Text $returnedSummary 'screen-worst-boundary-index=3'
Require-Text $returnedSummary 'screen-worst-call-gc-activity=True'
Require-Text $returnedSummary 'targeted-worst-boundary-index=191'
Require-Text $returnedSummary 'targeted-worst-call-gc-activity=False'
Require-Text $returnedSummary 'same-boundary-reproduction=False'
Require-Text $returnedSummary 'rp1c-selection-authorized=False'
Require-Text $returnedSummary 'c4-implementation-authorized=False'
Require-Text $attempt1Summary 'status=STATIC-PREFLIGHT-RED'
Require-Text $attempt1Summary 'failure-owner=VALIDATOR-HUMAN-DOC-MACHINE-MARKER-MISMATCH'
Require-Text $attempt1Summary 'contract-rule-present=True'
Require-Text $attempt1Summary 'human-plan-semantic-rule-present=True'

$runtimeContext = Join-Path $returnedDir '05-c3-r1-runtime-context.txt'
Require-Text $runtimeContext 'screen-exceedances-with-gc-activity=1'
Require-Text $runtimeContext 'target-exceedances-with-gc-activity=0'

$attributionPath = Join-Path $returnedDir '06-c3-r1-performance-attribution.txt'
Require-Text $attributionPath 'screen-measured-calls=20480'
Require-Text $attributionPath 'screen-max-us=2620.6'
Require-Text $attributionPath 'screen-calls-over-max-ceiling=1'
Require-Text $attributionPath 'targeted-max-us=853.5'
Require-Text $attributionPath 'targeted-calls-over-max-ceiling=1'
Require-Text $attributionPath 'attribution=R1-SEAM-TARGETED-ISOLATED-EXCEEDANCE'
Require-Text $attributionPath 'current-run-strict-r1-seam-contract-met=False'

$screenRows = @((Import-Csv -LiteralPath (Join-Path $returnedDir '02-c3-r1-seam-call-timing.csv')))
if ($screenRows.Count -ne 20480) { throw 'Returned Refinement 4 screen call count drifted.' }
$screenExceeders = @($screenRows | Where-Object { $_.exceeded_max_ceiling -eq 'True' })
if ($screenExceeders.Count -ne 1) { throw 'Returned Refinement 4 screen exceedance count drifted.' }
$screenEx = $screenExceeders[0]
if ([int]$screenEx.boundary_index -ne 3) { throw 'Returned Refinement 4 screen exceedance boundary drifted.' }
Require-NearDouble $screenEx.boundary_temperature_c 47.26746165007364 1e-9 'Screen exceedance boundary temperature'
Require-NearDouble $screenEx.elapsed_us 2620.6 1e-9 'Screen exceedance elapsed time'
if ($screenEx.any_gc_collection_delta -ne 'True' -or [int]$screenEx.gc_gen0_delta -ne 1) { throw 'Screen exceedance GC attribution drifted.' }

$boundaryRows = @((Import-Csv -LiteralPath (Join-Path $returnedDir '03-c3-r1-boundary-summary.csv')))
if ($boundaryRows.Count -ne 320) { throw 'Returned Refinement 4 boundary summary count drifted.' }
if (@($boundaryRows | Group-Object -Property boundary_index | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw 'Returned Refinement 4 boundary keys are not unique.' }
$boundary3 = @($boundaryRows | Where-Object { [int]$_.boundary_index -eq 3 })
if ($boundary3.Count -ne 1 -or [int]$boundary3[0].calls_over_max_ceiling -ne 1) { throw 'Boundary 3 screen summary drifted.' }
Require-NearDouble $boundary3[0].median_us 8.55 1e-12 'Boundary 3 screen median'
Require-NearDouble $boundary3[0].p95_us 11.1 1e-12 'Boundary 3 screen p95'

$targetRows = @((Import-Csv -LiteralPath (Join-Path $returnedDir '04-c3-r1-target-repeats.csv')))
if ($targetRows.Count -ne 23) { throw 'Returned Refinement 4 targeted boundary count drifted.' }
$targetExceeders = @($targetRows | Where-Object { [int]$_.calls_over_max_ceiling -gt 0 })
if ($targetExceeders.Count -ne 1) { throw 'Returned Refinement 4 targeted exceedance owner count drifted.' }
$targetEx = $targetExceeders[0]
if ([int]$targetEx.boundary_index -ne 191) { throw 'Returned Refinement 4 targeted exceedance boundary drifted.' }
Require-NearDouble $targetEx.boundary_temperature_c 270.3515844941138 1e-9 'Targeted exceedance boundary temperature'
Require-NearDouble $targetEx.target_max_us 853.5 1e-9 'Targeted exceedance maximum'
Require-NearDouble $targetEx.target_median_us 7.5 1e-12 'Targeted exceedance boundary median'
Require-NearDouble $targetEx.target_p95_us 9.6 1e-12 'Targeted exceedance boundary p95'
if ([int]$targetEx.exceedances_with_gc_activity -ne 0) { throw 'Targeted exceedance GC attribution drifted.' }
if ([int]$targetEx.screen_calls_over_max_ceiling -ne 0) { throw 'Boundary 191 unexpectedly exceeded during the screen.' }
if ($targetEx.target_classification -ne 'TARGETED-ISOLATED-EXCEEDANCE') { throw 'Targeted exceedance classification drifted.' }

if ([int]$targetEx.boundary_index -eq [int]$screenEx.boundary_index) { throw 'Refinement 4 unexpectedly reproduced the same exceedance boundary.' }

$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
if ($contract.gate -ne 'VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-PERFORMANCE-REPLANNING1') { throw 'Performance replanning gate identity drifted.' }
if ($contract.scope -ne 'C3-MANAGED-RUNTIME-WALL-CLOCK-TAIL-ADJUDICATION-AND-CROSS-PROCESS-PLAN') { throw 'Performance replanning scope drifted.' }
if ($contract.prerequisite.candidate -ne 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' -or $contract.prerequisite.candidate_source_unchanged -ne $true) { throw 'C3 prerequisite drifted.' }
if ($contract.prerequisite.c4_created -ne $false) { throw 'C4 must not exist in performance replanning.' }
Require-NearDouble $contract.frozen_performance.resolve_max_ceiling_us 409.30666666666673 1e-12 'Resolve maximum ceiling'
Require-NearDouble $contract.frozen_performance.historical_refinement3_r1_max_us 3667.1 1e-9 'Historical R3 R1 maximum'
Require-NearDouble $contract.frozen_performance.refinement4_screen_max_us 2620.6 1e-9 'R4 screen maximum'
Require-NearDouble $contract.frozen_performance.refinement4_targeted_max_us 853.5 1e-9 'R4 targeted maximum'
if ($contract.frozen_performance.refinement4_screen_worst_boundary_index -ne 3 -or $contract.frozen_performance.refinement4_targeted_worst_boundary_index -ne 191) { throw 'Frozen R4 boundary identities drifted.' }
if ($contract.frozen_performance.same_boundary_reproduction -ne $false) { throw 'Same-boundary reproduction must remain false.' }
if ($contract.engineering_decision.c4_justified_now -ne $false -or $contract.engineering_decision.rp1c_authorized_now -ne $false) { throw 'C4/RP1C cannot be authorized by this planning gate.' }
if ($contract.engineering_decision.strict_single_call_max_erased -ne $false -or $contract.engineering_decision.strict_single_call_max_relaxed -ne $false -or $contract.engineering_decision.performance_threshold_changed -ne $false) { throw 'Performance replanning cannot erase, relax or change the frozen ceiling.' }
if ($contract.engineering_decision.next_gate -ne 'RP1B-REFINEMENT5-C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY') { throw 'Next gate drifted.' }
if ($contract.refinement5_plan.independent_process_runs -ne 5 -or $contract.refinement5_plan.r1_boundary_count -ne 320) { throw 'Refinement 5 independent-process plan drifted.' }
if ($contract.refinement5_plan.warmup_passes_per_process -ne 16 -or $contract.refinement5_plan.measured_passes_per_process -ne 64 -or $contract.refinement5_plan.measured_r1_calls_per_process -ne 20480) { throw 'Refinement 5 per-process measurement protocol drifted.' }
Require-NearDouble $contract.refinement5_plan.same_resolve_max_ceiling_us 409.30666666666673 1e-12 'Refinement 5 maximum ceiling'
if ($contract.refinement5_plan.candidate_specific_slow_path_rule -ne 'SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS') { throw 'Candidate-specific slow-path rule drifted.' }
if ($contract.refinement5_plan.c4_authorization_rule -ne 'ONLY-AFTER-CANDIDATE-SPECIFIC-SLOW-PATH-CONFIRMED') { throw 'C4 authorization rule drifted.' }
if ($contract.refinement5_plan.rp1c_selection_performed -ne $false) { throw 'RP1C selection cannot be performed by performance replanning.' }

foreach ($property in @(
    'production_src_change_authorized',
    'thermodynamic_repair_authorized',
    'thermodynamic_tolerance_change_authorized',
    'exact_v9_change_authorized',
    'rp1c_selection_authorized',
    'c4_implementation_authorized',
    'vr3_authorized',
    'p3_r1_authorized',
    'second_replacement_long_authorized')) {
    if ($contract.authority.$property -ne $false) { throw ("Authority flag must remain false: {0}" -f $property) }
}

Require-Text $planPath 'C4 is **not authorized** from Refinement 4 alone;'
Require-Text $planPath 'five independent focused-test process runs'
Require-Text $planPath 'same boundary identity exceeds the unchanged maximum ceiling in at least two independent process runs'
Require-Text $planPath '409.30666666666673 us'
Require-Text $planPath 'C3 remains `C3-VAPOR-SEAM-COMPLETE-SURROGATE` byte-for-byte;'
Require-Text $adrPath 'do not authorize C4 unless a same-boundary ceiling exceedance is confirmed in at least two independent focused-test process runs'
Require-Text $adrPath 'keep C3 byte-for-byte immutable;'

$candidatePath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bRefinement2ShadowThermodynamicCandidates.cs'
Require-Text $candidatePath 'C3-VAPOR-SEAM-COMPLETE-SURROGATE'
if ((Read-Utf8Text $candidatePath).Contains('C4-')) { throw 'Performance replanning must not introduce C4.' }

$srcFiles = @(Get-ChildItem -LiteralPath 'src' -Recurse -File | Where-Object {
    $_.Extension -eq '.cs' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
})
if ($srcFiles.Count -eq 0) { throw 'Production C# source scan returned zero files.' }
foreach ($source in $srcFiles) {
    $content = Read-Utf8Text $source.FullName
    if ($content.Contains('C3-VAPOR-SEAM-COMPLETE-SURROGATE') -or $content.Contains('RP1B-PERFORMANCE-REPLANNING1') -or $content.Contains('C4-')) {
        throw ("Performance replanning identity leaked into production source: {0}" -f $source.FullName)
    }
}

$artifactDir = 'artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-performance-replanning1'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$artifactPath = Join-Path $artifactDir '01-performance-replanning-audit.txt'
@(
    'gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-PERFORMANCE-REPLANNING1',
    'status=PASS-AS-AUTHORED',
    'candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE',
    'candidate-source-changed=False',
    'refinement3-r1-historical-max-us=3667.1',
    'refinement4-screen-max-us=2620.6',
    'refinement4-screen-exceedance-boundary=3',
    'refinement4-screen-exceedance-gc-activity=True',
    'refinement4-targeted-max-us=853.5',
    'refinement4-targeted-exceedance-boundary=191',
    'refinement4-targeted-exceedance-gc-activity=False',
    'same-boundary-reproduction=False',
    'c4-justified-now=False',
    'rp1c-authorized-now=False',
    'strict-single-call-max-relaxed=False',
    'performance-threshold-changed=False',
    'next-gate=RP1B-REFINEMENT5-C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY',
    'refinement5-independent-process-runs=5',
    'refinement5-candidate-specific-slow-path-rule=SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS',
    'production-src-change-authorized=False',
    'thermodynamic-repair-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False'
) | Set-Content -LiteralPath $artifactPath -Encoding ASCII

Write-Host 'M10 Final VR2 Engineering Repair Planning 1 RP1B Performance Measurement Replanning 1 static audit: PASS'
Write-Host ("Artifact written: {0}" -f $artifactPath)
