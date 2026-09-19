$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
}
function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}
function Require-Text([string]$Path,[string]$Needle) {
    $text = Read-Utf8Text $Path
    if (-not $text.Contains($Needle)) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Require-True([object]$Value,[string]$Label) {
    if ($Value -ne $true) { throw ("{0} must be true." -f $Label) }
}
function Require-False([object]$Value,[string]$Label) {
    if ($Value -ne $false) { throw ("{0} must be false." -f $Label) }
}
function Require-Near([double]$Actual,[double]$Expected,[double]$Tolerance,[string]$Label) {
    if ([Math]::Abs($Actual-$Expected) -gt $Tolerance) { throw ("{0} drifted: actual={1}; expected={2}" -f $Label,$Actual,$Expected) }
}
function Require-AsciiSource([string]$Path,[string]$Label) {
    Require-File $Path
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    foreach ($b in $bytes) {
        if ($b -gt 127) { throw ("{0} must remain 7-bit ASCII for Windows PowerShell 5.1 source safety: {1}" -f $Label,$Path) }
    }
}
function Require-NormalizedTextSha256([string]$Path,[string]$Expected,[string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n","`n").Replace("`r","`n")
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($enc.GetBytes($normalized)) } finally { $sha.Dispose() }
    $actual = ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) { throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label,$actual,$Expected) }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$validatorPath = $MyInvocation.MyCommand.Path
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance-contract.json'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance.cmd'

$baseContract = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-contract.json'
$baseValidator = Join-Path $PSScriptRoot 'validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1.ps1'
$baseRunner = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1.cmd'
$baseDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1.md'
$baseReview = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1_PREEXECUTION_REVIEW.md'
$baseAdr = Join-Path $repoRoot 'docs\adr\0203-qualify-runtime-configuration-impact-before-rp1c-selection.md'

$returnedAdj = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md'
$amendDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1_AMENDMENT1_HOST_PROVENANCE.md'
$amendReview = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1_AMENDMENT1_HOST_PROVENANCE_PREEXECUTION_REVIEW.md'
$adr0204 = Join-Path $repoRoot 'docs\adr\0204-scope-runtime-performance-evidence-to-one-execution-host.md'

$frozenRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeConfigurationImpactAssessmentPlanning1_Artifacts'
$r01 = Join-Path $frozenRoot '01-contract-and-provenance.txt'
$r02 = Join-Path $frozenRoot '02-impact-assessment-matrix.csv'
$r03 = Join-Path $frozenRoot '03-planning-summary.txt'
$r04 = Join-Path $frozenRoot '04-preexecution-review.txt'

foreach ($path in @($contractPath,$runnerPath,$baseContract,$baseValidator,$baseRunner,$baseDoc,$baseReview,$baseAdr,$returnedAdj,$amendDoc,$amendReview,$adr0204,$r01,$r02,$r03,$r04)) {
    Require-File $path
}
Require-AsciiSource $validatorPath 'Amendment validator source'
Require-AsciiSource $runnerPath 'Amendment runner source'

$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance-v1') { throw 'Amendment schema drifted.' }
if ($contract.status -ne 'CANDIDATE-PLANNING-AMENDMENT-ONLY') { throw 'Amendment status drifted.' }
if ($contract.gate -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1-AMENDMENT1-HOST-PROVENANCE') { throw 'Amendment gate identity drifted.' }

Require-True $contract.reason.amendment_required_before_a2_implementation 'Host amendment requirement'
Require-False $contract.reason.base_planning1_host_constraint_complete 'Base Planning 1 host completeness'
if ($contract.reason.base_planning1_status -ne 'PASS-AS-AUTHORED') { throw 'Base Planning 1 returned status drifted.' }

$p = $contract.prerequisite
if ($p.base_planning_gate -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1') { throw 'Base Planning 1 gate drifted.' }
if ($p.base_planning_returned_status -ne 'PASS-AS-AUTHORED') { throw 'Base Planning 1 returned adjudication drifted.' }
if ($p.future_gate -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1') { throw 'Future A2 gate drifted.' }
if ([int]$p.future_runtime_profiles -ne 2 -or [int]$p.future_exact_v9_processes -ne 10 -or [int]$p.future_exact_v9_measured_calls -ne 230400 -or [int]$p.future_required_files -ne 78) { throw 'Future A2 frozen shape drifted.' }
Require-Near ([double]$p.strict_max_us) 409.30666666666673 0.0000000001 'Strict maximum'
Require-Near ([double]$p.diagnostic_tail_floor_us) 100.0 0.0000001 'Diagnostic tail floor'
Require-False $p.diagnostic_tail_floor_is_threshold 'Diagnostic floor threshold rule'

$files = @(Get-ChildItem -LiteralPath $frozenRoot -File)
if ($files.Count -ne 4) { throw ("Frozen returned Planning 1 artifact set must contain exactly four files; found {0}." -f $files.Count) }

$h = $contract.normalized_lf_sha256
Require-NormalizedTextSha256 $r01 $h.returned_contract 'Returned Planning 1 contract/provenance'
Require-NormalizedTextSha256 $r02 $h.returned_matrix 'Returned Planning 1 matrix'
Require-NormalizedTextSha256 $r03 $h.returned_summary 'Returned Planning 1 summary'
Require-NormalizedTextSha256 $r04 $h.returned_review 'Returned Planning 1 preexecution review'
Require-NormalizedTextSha256 $baseContract $h.base_planning_contract 'Base Planning 1 contract'
Require-NormalizedTextSha256 $baseDoc $h.base_planning_doc 'Base Planning 1 document'
Require-NormalizedTextSha256 $baseReview $h.base_planning_preexecution_review 'Base Planning 1 preexecution review'
Require-NormalizedTextSha256 $baseValidator $h.base_planning_validator 'Base Planning 1 validator'
Require-NormalizedTextSha256 $baseRunner $h.base_planning_runner 'Base Planning 1 runner'
Require-NormalizedTextSha256 $baseAdr $h.adr0203 'ADR-0203'

Require-Text $r01 'status=PASS-AS-AUTHORED'
Require-Text $r01 'future-gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1'
Require-Text $r01 'future-required-files=78'
Require-Text $r02 'AMBIENT-UNSET,unset,unset,unset,unset,unset,5,1,1,3'
Require-Text $r02 'EXPLICIT-REFERENCE-ALL-ON,1,1,1,1,1,5,1,1,3'
Require-Text $r03 'a2-implementation-authorized-now=False'
Require-Text $r04 'status=STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $returnedAdj 'PASS-AS-AUTHORED'
Require-Text $returnedAdj 'host-neutral future execution contract incomplete for implementation'
Require-Text $amendDoc 'one complete A2 execution on one physical host'
Require-Text $amendDoc 'Cross-host extrapolation of the absolute threshold is not authorized.'
Require-Text $amendDoc 'The total future A2 evidence count remains 78.'
Require-Text $amendReview 'Cross-host evidence mixing is infrastructure RED'
Require-Text $adr0204 'one physical host for all profiles and all assessment families'

$f = $contract.future_gate_unchanged
if ($f.id -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1') { throw 'Future A2 identity drifted.' }
if (@($f.profiles).Count -ne 2 -or $f.profiles[0] -ne 'AMBIENT-UNSET' -or $f.profiles[1] -ne 'EXPLICIT-REFERENCE-ALL-ON') { throw 'Future A2 profiles drifted.' }
if ([int]$f.exact_v9_processes_per_profile -ne 5 -or [int]$f.ordinary_release_suite_runs_per_profile -ne 1 -or [int]$f.replay_determinism_runs_per_profile -ne 1 -or [int]$f.non_vr2_performance_runs_per_profile -ne 3) { throw 'Future A2 run counts drifted.' }
if ([int]$f.total_required_files -ne 78) { throw 'Future A2 evidence count drifted.' }
Require-True $f.c4_immutable 'C4 immutability'
Require-True $f.exact_v9_immutable 'Exact-v9 immutability'
Require-Near ([double]$f.strict_max_us) 409.30666666666673 0.0000000001 'Future A2 strict maximum'
Require-False $f.threshold_change_authorized 'Threshold-change authority'

$hp = $contract.execution_host_policy
if ($hp.scope -ne 'ENTIRE-A2-GATE') { throw 'Host policy scope drifted.' }
Require-True $hp.single_physical_host_required 'Single-host rule'
Require-True $hp.all_profiles_same_host_required 'Same-host profiles'
Require-True $hp.all_assessment_families_same_host_required 'Same-host assessment families'
Require-False $hp.cross_host_evidence_mixing_authorized 'Cross-host mixing authority'
Require-True $hp.gate_start_host_capture_required 'Start host capture'
Require-True $hp.gate_end_host_capture_required 'End host capture'
Require-True $hp.stable_host_fingerprint_match_required 'Stable host fingerprint rule'
Require-True $hp.active_power_scheme_capture_required 'Power scheme capture'
Require-True $hp.active_power_scheme_must_not_change_during_gate 'Power scheme stability'
Require-False $hp.machine_name_written_to_evidence 'Machine-name evidence'
Require-False $hp.user_name_written_to_evidence 'User-name evidence'
Require-False $hp.raw_system_uuid_written_to_evidence 'Raw UUID evidence'
Require-True $hp.system_uuid_may_contribute_only_to_sha256_fingerprint 'UUID fingerprint-only rule'
if ($hp.fingerprint_algorithm -ne 'SHA256') { throw 'Host fingerprint algorithm drifted.' }
if (@($hp.fingerprint_inputs).Count -ne 8) { throw 'Host fingerprint input count drifted.' }
if (@($hp.recorded_nonsecret_context).Count -ne 14) { throw 'Recorded host context field count drifted.' }
Require-True $hp.child_run_contracts_must_include_host_fingerprint 'Child-run host fingerprint'
Require-True $hp.exact_v9_runtime_context_must_include_host_fingerprint 'Exact-v9 runtime host fingerprint'
if ($hp.mismatch_classification -ne 'INFRASTRUCTURE-RED-HOST-PROVENANCE-MISMATCH') { throw 'Host mismatch classification drifted.' }

$pi = $contract.performance_interpretation_policy
Require-True $pi.same_host_profile_contrast_is_primary 'Same-host contrast primary rule'
Require-Near ([double]$pi.strict_max_us_unchanged) 409.30666666666673 0.0000000001 'Unchanged strict maximum'
Require-True $pi.absolute_threshold_observation_is_host_scoped 'Host-scoped absolute observation'
Require-False $pi.cross_host_absolute_timing_comparison_authorized 'Cross-host timing comparison authority'
Require-False $pi.cross_host_threshold_extrapolation_authorized 'Cross-host threshold extrapolation authority'
Require-True $pi.host_difference_may_not_be_used_to_relax_threshold 'No host-based threshold relaxation'
Require-True $pi.host_difference_may_not_be_used_to_mutate_c4 'No host-based C4 mutation'
Require-True $pi.host_difference_may_not_be_used_to_mutate_exact_v9 'No host-based exact-v9 mutation'
Require-False $pi.cross_host_replication_in_a2 'Cross-host replication inside A2'
Require-True $pi.separate_cross_host_replication_planning_required_if_material 'Separate cross-host replication planning'
Require-False $pi.production_minimum_hardware_scope_defined 'Production minimum hardware scope'

$et = $contract.evidence_tree_amendment
if ([int]$et.total_required_files_remains -ne 78) { throw 'A2 total evidence count must remain 78.' }
Require-True $et.new_files_do_not_increase_total 'Host provenance file-count neutrality'
Require-True $et.host_provenance_uses_existing_aggregate_slots 'Host provenance aggregate-slot reuse'
$aggregate = @($et.aggregate_files)
if ($aggregate.Count -ne 8) { throw 'Future aggregate file count drifted.' }
if ($aggregate[1] -ne '02-execution-host-provenance.txt' -or $aggregate[2] -ne '03-host-consistency-audit.txt') { throw 'Future host evidence aggregate slots drifted.' }

$retro = $contract.retroactive_evidence
Require-True $retro.contexts_consistent 'Historical minimal runtime context consistency'
Require-False $retro.same_physical_host_proven_retroactively 'Retroactive same-host proof'
if ([int]$retro.runtime_factor_isolation1_minimal_context.processor_count -ne 20 -or [int]$retro.dynamic_pgo_comparator1_minimal_context.processor_count -ne 20) { throw 'Historical processor-count context drifted.' }
if ($retro.reason_not_proven -ne 'NO-STABLE-HOST-FINGERPRINT-OR-CPU-MODEL-WAS-RECORDED') { throw 'Retroactive host-proof limitation drifted.' }

$futureTest = Join-Path $repoRoot $contract.future_implementation_absence.test_file
$futureRunner = Join-Path $repoRoot $contract.future_implementation_absence.runner_file
if (Test-Path -LiteralPath $futureTest -PathType Leaf) { throw 'Future A2 test is already implemented; amendment must remain planning-only.' }
if (Test-Path -LiteralPath $futureRunner -PathType Leaf) { throw 'Future A2 runner is already implemented; amendment must remain planning-only.' }

$a = $contract.authority
Require-True $a.base_planning1_adjudicated_pass 'Base Planning 1 adjudication'
Require-False $a.base_planning1_alone_sufficient_for_a2_implementation 'Base Planning 1 standalone implementation sufficiency'
Require-True $a.amendment1_required_before_a2_implementation 'Amendment requirement'
Require-False $a.a2_implementation_authorized_now 'A2 implementation authority now'
Require-True $a.a2_implementation_authorizable_after_returned_amendment_adjudication 'A2 post-amendment eligibility'
Require-False $a.full_domain_performance_confirmation2_authorized 'FDPC2 authority'
Require-False $a.rp1c_selection_authorized 'RP1C selection authority'
Require-False $a.production_runtime_change_authorized 'Production runtime authority'
Require-False $a.production_repair_authorized 'Production repair authority'
Require-False $a.candidate_mutation_authorized 'Candidate mutation authority'
Require-False $a.threshold_change_authorized 'Threshold authority'
Require-False $a.exact_v9_change_authorized 'Exact-v9 authority'
Require-False $a.vr3_authorized 'VR3 authority'
Require-False $a.p3_r1_authorized 'P3-R1 authority'
Require-False $a.second_replacement_long_authorized 'Second replacement-long authority'

$artifactDir = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding -ArgumentList $false

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), @(
    'status=PASS-AS-AUTHORED',
    'gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1-AMENDMENT1-HOST-PROVENANCE',
    'base-planning1-returned-adjudication=PASS-AS-AUTHORED',
    'future-gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1',
    'single-physical-host-required=True',
    'all-profiles-same-host-required=True',
    'all-assessment-families-same-host-required=True',
    'cross-host-evidence-mixing-authorized=False',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'threshold-change-authorized=False',
    'future-required-files=78',
    'a2-implementation-authorized-now=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
), $utf8)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-host-provenance-policy.txt'), @(
    'status=PASS-HOST-PROVENANCE-POLICY-FROZEN',
    'host-scope=ENTIRE-A2-GATE',
    'host-fingerprint=SHA256-PRIVACY-PRESERVING',
    'raw-system-uuid-retained=False',
    'machine-name-retained=False',
    'user-name-retained=False',
    'start-end-fingerprint-match-required=True',
    'active-power-scheme-capture-required=True',
    'active-power-scheme-stability-required=True',
    'absolute-threshold-observation-host-scoped=True',
    'cross-host-absolute-timing-comparison-authorized=False',
    'cross-host-threshold-extrapolation-authorized=False',
    'cross-host-replication-in-a2=False'
), $utf8)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-planning-amendment-summary.txt'), @(
    'status=PASS-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1-AMENDMENT1-CONTRACT-FROZEN',
    'base-planning1-status=PASS-AS-AUTHORED',
    'base-planning1-alone-sufficient-for-a2-implementation=False',
    'future-a2-profiles-unchanged=True',
    'future-a2-run-counts-unchanged=True',
    'future-a2-required-files=78',
    'c4-immutable=True',
    'exact-v9-immutable=True',
    'strict-threshold-unchanged=True',
    'a2-implementation-authorized-now=False',
    'full-domain-performance-confirmation2-authorized=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'next-action=RETURN-COMPLETE-HOST-PROVENANCE-AMENDMENT-ARTIFACTS-FOR-ADJUDICATION'
), $utf8)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), @(
    'status=STATIC-PREEXECUTION-REVIEW-PASS',
    'finding-1=BASE-PLANNING1-RETURNED-PASS-AS-AUTHORED',
    'finding-2=MULTI-HOST-EXECUTION-IS-MATERIAL-PERFORMANCE-CONFOUND',
    'finding-3=SINGLE-HOST-PER-A2-GATE-REQUIRED',
    'finding-4=CROSS-HOST-EVIDENCE-MIXING-IS-INFRASTRUCTURE-RED',
    'finding-5=ABSOLUTE-WALL-CLOCK-RESULTS-ARE-HOST-SCOPED',
    'finding-6=HISTORICAL-RFI1-PGO-CONTEXT-CONSISTENT-BUT-PHYSICAL-HOST-NOT-PROVEN',
    'future-a2-required-files=78',
    'threshold-change-authorized=False',
    'a2-implementation-authorized=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
), $utf8)

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 RUNTIME CONFIGURATION IMPACT ASSESSMENT'
Write-Host 'PLANNING 1 AMENDMENT 1 - EXECUTION HOST PROVENANCE'
Write-Host '============================================================'
Write-Host 'Planning amendment only. Freezes one-host-per-A2-gate provenance and interpretation rules.'
Write-Host 'No A2 implementation, FDPC2, RP1C selection or production/runtime change authorization.'
Write-Host ''
Write-Host 'Runtime Configuration Impact Assessment Planning 1 Amendment 1 static audit: PASS-AS-AUTHORED'
Write-Host ("Artifacts: {0}" -f $artifactDir)
