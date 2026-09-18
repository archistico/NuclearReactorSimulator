$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}

function Require-Text([string]$Path, [string]$Needle) {
    $text = Read-Utf8Text $Path
    if (-not $text.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

function Require-NormalizedTextSha256([string]$Path, [string]$Expected, [string]$Label) {
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
    $actual = ([System.BitConverter]::ToString($hashBytes)).Replace("-", "").ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label, $actual, $Expected)
    }
}

function Parse-InvDouble([string]$Text, [string]$Label) {
    $value = 0.0
    $style = [System.Globalization.NumberStyles]::Float -bor [System.Globalization.NumberStyles]::AllowThousands
    if (-not [double]::TryParse($Text, $style, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw ("{0} is not an invariant finite double: {1}" -f $Label, $Text)
    }
    if ([double]::IsNaN($value) -or [double]::IsInfinity($value)) {
        throw ("{0} is non-finite: {1}" -f $Label, $Text)
    }
    return $value
}

function Require-Near([double]$Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    if ([double]::IsNaN($Actual) -or [double]::IsInfinity($Actual) -or [math]::Abs($Actual - $Expected) -gt $Tolerance) {
        throw ("{0} drifted: actual={1:R}; expected={2:R}; tolerance={3:R}" -f $Label, $Actual, $Expected, $Tolerance)
    }
}

function Require-TrueString([string]$Actual, [string]$Label) {
    if (-not $Actual.Equals('true', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ("{0} expected true, actual={1}" -f $Label, $Actual)
    }
}

$validatorPath = $MyInvocation.MyCommand.Path
$validatorBytes = [System.IO.File]::ReadAllBytes($validatorPath)
if ($validatorBytes | Where-Object { $_ -gt 127 }) {
    throw 'RP1C Planning 1 validator must remain ASCII-only for Windows PowerShell 5.1 stability.'
}

$artifactDir = 'artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-planning1'
if (Test-Path -LiteralPath $artifactDir) {
    Remove-Item -LiteralPath $artifactDir -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C PLANNING 1'
Write-Host '============================================================'
Write-Host 'Planning only on validated returned C4 evidence.'
Write-Host 'No RP1C selection, production repair, threshold change, exact-v9 change, VR3, P3-R1 or second-long authorization.'
Write-Host ''

$contractPath = 'eng/m10-final-vr2-engineering-repair-planning1-rp1c-planning1-contract.json'
$planPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1.md'
$adrPath = 'docs/adr/0197-rp1c-selection-uses-corrected-full-performance-predicate-and-preserves-no-selection.md'
$mainPlanPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md'
$c4AdjudicationPath = 'docs/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_RETURNED_EVIDENCE_ADJUDICATION.md'
$d3SummaryPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/08-candidate-summary.csv'
$d3PerformancePath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/06-candidate-performance.csv'
$d3ComplexityPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts/07-candidate-complexity.csv'
$c4SemanticPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Artifacts/04-semantic-equivalence-summary.txt'
$c4AllocationPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Artifacts/05-allocation-closure-summary.txt'
$c4RunsPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Artifacts/06-cross-process-run-summary.csv'
$c4SummaryPath = 'eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Artifacts/09-rp1b-c4-summary.txt'

foreach ($path in @($contractPath, $planPath, $adrPath, $mainPlanPath, $c4AdjudicationPath, $d3SummaryPath, $d3PerformancePath, $d3ComplexityPath, $c4SemanticPath, $c4AllocationPath, $c4RunsPath, $c4SummaryPath)) {
    Require-File $path
}

$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-planning1-v1') { throw 'Unexpected RP1C Planning 1 contract schema.' }
if ($contract.status -ne 'PLANNING-ONLY') { throw 'RP1C Planning 1 must remain planning-only.' }
if ($contract.prerequisite.c4_returned_evidence_adjudication -ne 'PASS') { throw 'Returned C4 adjudication must be PASS.' }
if ($contract.prerequisite.c4_classification -ne 'C4-QUALIFIED-ALLOCATION-TAIL-CLOSED') { throw 'Unexpected returned C4 classification.' }
if ($contract.prerequisite.rp1c_planning_authorized -ne $true) { throw 'RP1C planning must be authorized.' }
if ($contract.prerequisite.rp1c_selection_authorized_before_returned_planning_adjudication -ne $false) { throw 'RP1C selection cannot be authorized before returned planning adjudication.' }

if ($contract.authority.rp1c_selection_authorized_now -ne $false) { throw 'RP1C selection must remain unauthorized now.' }
if ($contract.authority.production_repair_authorized -ne $false) { throw 'Production repair must remain unauthorized.' }
if ($contract.authority.threshold_change_authorized -ne $false) { throw 'Threshold change must remain unauthorized.' }
if ($contract.authority.exact_v9_change_authorized -ne $false) { throw 'exact-v9 change must remain unauthorized.' }
if ($contract.authority.vr3_authorized -ne $false) { throw 'VR3 must remain unauthorized.' }
if ($contract.authority.p3_r1_authorized -ne $false) { throw 'P3-R1 must remain unauthorized.' }
if ($contract.authority.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long must remain unauthorized.' }
if ($contract.authority.c4_full_domain_confirmation_authorizable_after_returned_planning_adjudication -ne $true) { throw 'Returned planning adjudication must be the only authority path to C4 full-domain confirmation.' }
if ($contract.frozen_evidence_sha256_normalized_lf.hash_mode -ne 'UTF8-TEXT-NORMALIZED-LF') { throw 'Frozen evidence hash mode must be normalized UTF-8 LF.' }

Require-Text $c4AdjudicationPath 'RETURNED-EVIDENCE ADJUDICATION: PASS'
Require-Text $c4AdjudicationPath 'C4-QUALIFIED-ALLOCATION-TAIL-CLOSED'
Require-Text $c4AdjudicationPath 'RP1C-PLANNING-AUTHORIZED=True'
Require-Text $c4AdjudicationPath 'RP1C-SELECTION-AUTHORIZED=False'
Require-Text $mainPlanPath "D3's recorded eligibility omitted seam worst-case timing"
Require-Text $planPath 'C4 full-domain performance confirmation = incomplete'
Require-Text $planPath '217,600 calls'
Require-Text $planPath 'SELECT-C4'
Require-Text $planPath 'SELECT-NONE'
Require-Text $planPath 'R1-IMPLEMENTATION-PLANNING-ONLY'
Require-Text $adrPath 'semantic bit-equivalence does not by itself prove identical timing'
Require-Text $adrPath 'current selection-ready count is zero'

$expectedHashes = @{
    d3_candidate_summary = '5080CEF4D6EB360C7650143F5889FA73815B1A1EB9BD1F7C60A27CFFC2860208'
    d3_performance = '700954A17F6A18D92256CC6F76B44A37AF201A05070DD57C5969872A354E4E64'
    d3_complexity = '47361EB6F89148B3EDBE83708F3580F20EBD5055C9BF4884E754870D6C5AA541'
    c4_semantic_summary = 'AE952F5D25DD16763DF8FAE91B354A905E13252547BEEFF89D5A80A5CDB41826'
    c4_allocation_summary = 'BBFB1983CB426E147DCCB1830E36F3A3A94E28162CC99EB4D3003BA62040B0BB'
    c4_run_summary = 'E870E2000693068332954F113A485ACF92F34BC36D3708B9BDB556A6E8284511'
    c4_summary = '01BD52A390763671231F386DBB74888FF33E37B2CCD0588DADE04E3C59F619F2'
    c4_returned_adjudication = '370FAB7AA3EF3E2F0DF4BC4839E56F6F917D53C05CA934A331078B0CF702FE10'
}

Require-NormalizedTextSha256 $d3SummaryPath $expectedHashes['d3_candidate_summary'] 'D3 candidate summary'
Require-NormalizedTextSha256 $d3PerformancePath $expectedHashes['d3_performance'] 'D3 performance'
Require-NormalizedTextSha256 $d3ComplexityPath $expectedHashes['d3_complexity'] 'D3 complexity'
Require-NormalizedTextSha256 $c4SemanticPath $expectedHashes['c4_semantic_summary'] 'C4 semantic summary'
Require-NormalizedTextSha256 $c4AllocationPath $expectedHashes['c4_allocation_summary'] 'C4 allocation summary'
Require-NormalizedTextSha256 $c4RunsPath $expectedHashes['c4_run_summary'] 'C4 run summary'
Require-NormalizedTextSha256 $c4SummaryPath $expectedHashes['c4_summary'] 'C4 summary'
Require-NormalizedTextSha256 $c4AdjudicationPath $expectedHashes['c4_returned_adjudication'] 'C4 returned adjudication'

foreach ($name in $expectedHashes.Keys) {
    $property = $contract.frozen_evidence_sha256_normalized_lf.PSObject.Properties[$name]
    if ($null -eq $property) { throw ("Contract evidence hash missing for {0}." -f $name) }
    $contractValue = [string]$property.Value
    if ($contractValue.ToUpperInvariant() -ne $expectedHashes[$name]) {
        throw ("Contract evidence hash drifted for {0}." -f $name)
    }
}

$summaryRows = @(Import-Csv -LiteralPath $d3SummaryPath)
$c3Row = $summaryRows | Where-Object { $_.candidate_id -eq 'C3-VAPOR-SEAM-COMPLETE-SURROGATE' }
$d3Row = $summaryRows | Where-Object { $_.candidate_id -eq 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR' }
if (@($c3Row).Count -ne 1 -or @($d3Row).Count -ne 1) { throw 'Expected exactly one C3 row and one D3 row.' }

foreach ($row in @($c3Row, $d3Row)) {
    Require-TrueString ([string]$row.all_frozen_points_resolved) ($row.candidate_id + ' all_frozen_points_resolved')
    Require-TrueString ([string]$row.no_core_wrong_phase) ($row.candidate_id + ' no_core_wrong_phase')
    Require-TrueString ([string]$row.vr2_blocking_ceiling_met) ($row.candidate_id + ' vr2_blocking_ceiling_met')
    Require-TrueString ([string]$row.planning_target_met) ($row.candidate_id + ' planning_target_met')
    Require-TrueString ([string]$row.deterministic_repeat) ($row.candidate_id + ' deterministic_repeat')
    Require-TrueString ([string]$row.vapor_seam_completion_met) ($row.candidate_id + ' vapor_seam_completion_met')
    if ([int]$row.seam_unresolved -ne 0) { throw ($row.candidate_id + ' seam unresolved must be zero.') }
    if ([int]$row.seam_phase_mismatch -ne 0) { throw ($row.candidate_id + ' seam phase mismatch must be zero.') }
    $phaseAgreement = Parse-InvDouble ([string]$row.exact_v9_phase_agreement_percent) ($row.candidate_id + ' exact-v9 phase agreement')
    Require-Near $phaseAgreement 100.0 1.0e-12 ($row.candidate_id + ' exact-v9 phase agreement')
}

$c3HotCore = Parse-InvDouble ([string]$c3Row.max_hot_core_pressure_relative_error) 'C3 hot-core pressure error'
Require-Near $c3HotCore 0.031463842982651896 1.0e-15 'C3 hot-core pressure error'
if ($c3HotCore -gt 0.1) { throw 'C3/C4 semantic basis must meet the 10 percent planning target.' }

$d3PerformanceRows = @(Import-Csv -LiteralPath $d3PerformancePath)
$d3Perf = $d3PerformanceRows | Where-Object { $_.candidate_id -eq 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR' }
if (@($d3Perf).Count -ne 1) { throw 'Expected exactly one D3 performance row.' }
$d3Median = Parse-InvDouble ([string]$d3Perf.resolve_median_us) 'D3 resolve median'
$d3P95 = Parse-InvDouble ([string]$d3Perf.resolve_p95_us) 'D3 resolve p95'
$d3ResolveMax = Parse-InvDouble ([string]$d3Perf.resolve_max_us) 'D3 resolve max'
$d3SeamMax = Parse-InvDouble ([string]$d3Perf.seam_max_us) 'D3 seam max'
Require-Near $d3Median 14.7 1.0e-12 'D3 resolve median'
Require-Near $d3P95 15.3 1.0e-12 'D3 resolve p95'
Require-Near $d3ResolveMax 208.5 1.0e-12 'D3 resolve max'
Require-Near $d3SeamMax 16504.9 1.0e-9 'D3 seam max'
$strictMax = [double]$contract.corrected_full_selection_predicate.single_call_max_us_max
Require-Near $strictMax 409.30666666666673 1.0e-12 'Strict max ceiling'
if ($d3SeamMax -le $strictMax) { throw 'D3 must remain blocked by its frozen seam max.' }

$d3ComplexityRows = @(Import-Csv -LiteralPath $d3ComplexityPath)
$d3Complexity = $d3ComplexityRows | Where-Object { $_.candidate_id -eq 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR' }
if (@($d3Complexity).Count -ne 1) { throw 'Expected exactly one D3 complexity row.' }
Require-TrueString ([string]$d3Complexity.direct_if97_at_resolve_time) 'D3 direct IF97 flag'
if ([int]$d3Complexity.max_iterative_solve_iterations -ne 1700) { throw 'D3 iterative ceiling drifted.' }

Require-Text $c4SemanticPath 'total-observation-count=1967'
Require-Text $c4SemanticPath 'state-bit-mismatch-count=0'
Require-Text $c4SemanticPath 'hydraulic-bit-mismatch-count=0'
Require-Text $c4SemanticPath 'repeat-mismatch-count=0'
Require-Text $c4SemanticPath 'semantic-bit-equivalent=true'
Require-Text $c4AllocationPath 'measured-calls=204800'
Require-Text $c4AllocationPath 'nonzero-allocation-calls=0'
Require-Text $c4AllocationPath 'candidate-call-allocated-bytes-sum=0'
Require-Text $c4AllocationPath 'harness-allocated-bytes-sum=0'
Require-Text $c4AllocationPath 'fallback-calls=0'
Require-Text $c4AllocationPath 'allocation-closure-met=True'
Require-Text $c4SummaryPath 'classification=C4-QUALIFIED-ALLOCATION-TAIL-CLOSED'
Require-Text $c4SummaryPath 'semantic-bit-equivalent=True'
Require-Text $c4SummaryPath 'allocation-closure-met=True'
Require-Text $c4SummaryPath 'wall-clock-tail-closed=True'

$c4Runs = @(Import-Csv -LiteralPath $c4RunsPath)
if ($c4Runs.Count -ne 10) { throw 'C4 run summary must contain exactly 10 rows.' }
$c4WorstMax = 0.0
foreach ($row in $c4Runs) {
    if ([int]$row.measured_calls -ne 20480) { throw 'Each C4 run must contain 20,480 measured calls.' }
    if ([int]$row.calls_over_max_ceiling -ne 0) { throw 'C4 R1 run contains a strict max exceedance.' }
    if ([int]$row.unresolved_calls -ne 0) { throw 'C4 R1 run contains unresolved calls.' }
    if ([int]$row.nonzero_allocation_calls -ne 0) { throw 'C4 R1 run contains nonzero candidate allocation.' }
    if ([long]$row.candidate_allocated_bytes -ne 0) { throw 'C4 R1 candidate allocated bytes must be zero.' }
    if ([long]$row.harness_allocated_bytes -ne 0) { throw 'C4 R1 harness allocated bytes must be zero.' }
    if ([int]$row.fallback_calls -ne 0) { throw 'C4 R1 fallback calls must be zero.' }
    Require-TrueString ([string]$row.median_within_ceiling) 'C4 R1 median within ceiling'
    Require-TrueString ([string]$row.p95_within_ceiling) 'C4 R1 p95 within ceiling'
    Require-TrueString ([string]$row.max_within_ceiling) 'C4 R1 max within ceiling'
    $max = Parse-InvDouble ([string]$row.max_us) 'C4 R1 max_us'
    if ($max -gt $c4WorstMax) { $c4WorstMax = $max }
}
Require-Near $c4WorstMax 329.9 1.0e-12 'C4 worst returned R1 max'
if ($c4WorstMax -gt $strictMax) { throw 'C4 worst returned R1 max exceeds strict ceiling.' }

if ($contract.current_readiness_expectation.c4_physical_and_r1_qualification_complete -ne $true) { throw 'C4 physical/R1 qualification must be complete.' }
if ($contract.current_readiness_expectation.c4_full_domain_performance_confirmation_complete -ne $false) { throw 'C4 full-domain confirmation must remain incomplete.' }
if ($contract.current_readiness_expectation.c4_selection_ready_now -ne $false) { throw 'C4 must not be selection-ready before full-domain confirmation.' }
if ($contract.current_readiness_expectation.d3_selection_ready_now -ne $false) { throw 'D3 must remain not selection-ready.' }
if ([int]$contract.current_readiness_expectation.selection_ready_count_now -ne 0) { throw 'Current selection-ready count must be zero.' }
if ($contract.current_readiness_expectation.automatic_selection_authorized -ne $false) { throw 'Automatic selection must remain false.' }

$confirm = $contract.next_confirmation_gate
if ($confirm.id -ne 'RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1') { throw 'Unexpected next confirmation gate id.' }
if ([int]$confirm.fresh_process_count -ne 5) { throw 'C4 full-domain confirmation must use five fresh processes.' }
if ([int]$confirm.exact_v9_warmup_passes -ne 16 -or [int]$confirm.exact_v9_measured_passes -ne 64) { throw 'Exact-v9 warmup/measured pass contract drifted.' }
if ([int]$confirm.seam_warmup_passes -ne 4 -or [int]$confirm.seam_measured_passes -ne 16) { throw 'Seam warmup/measured pass contract drifted.' }
if ([int]$confirm.exact_v9_rows -ne 360 -or [int]$confirm.seam_rows -ne 1280) { throw 'Full-domain confirmation corpus drifted.' }
if ([int]$confirm.exact_v9_measured_calls_per_process -ne 23040) { throw 'Exact-v9 measured call count drifted.' }
if ([int]$confirm.seam_measured_calls_per_process -ne 20480) { throw 'Seam measured call count drifted.' }
if ([int]$confirm.total_measured_calls_per_process -ne 43520) { throw 'Per-process full-domain call count drifted.' }
if ([int]$confirm.total_measured_calls -ne 217600) { throw 'Total full-domain call count drifted.' }
if ($confirm.candidate_mutation_allowed -ne $false -or $confirm.threshold_change_allowed -ne $false -or $confirm.corpus_regeneration_allowed -ne $false) { throw 'Confirmation gate must preserve candidate, thresholds and corpus.' }
if ($confirm.all_five_processes_must_meet_corrected_performance_predicate -ne $true) { throw 'Every confirmation process must meet the corrected predicate.' }
if ($confirm.zero_candidate_allocation_required -ne $false) { throw 'Full-domain candidate allocation must not be forced to zero on every path.' }
if ([int]$confirm.exact_v9_median_candidate_allocation_ceiling_bytes -ne 2816) { throw 'Full-domain candidate allocation ceiling must remain 2816 B median.' }
if ($confirm.allocation_neutral_harness_required -ne $true) { throw 'Full-domain measurement harness must remain allocation-neutral.' }
if ($confirm.r1_zero_allocation_evidence_preserved -ne $true) { throw 'R1 zero-allocation evidence must remain preserved.' }
if ($confirm.fallback_zero_required_full_domain -ne $false) { throw 'Full-domain confirmation must not require zero fallback on unrelated paths.' }

$futureTest = [string]$confirm.test_file
$futureRunner = [string]$confirm.runner
if ($futureTest -ne 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation1Tests.cs') { throw 'Future confirmation test path drifted.' }
if ([string]$confirm.test_class -ne 'M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation1Tests') { throw 'Future confirmation test class drifted.' }
if ([string]$confirm.test_method -ne 'Rp1cC4FullDomainPerformanceConfirmation1_MeasuresOneFreshProcess') { throw 'Future confirmation test method drifted.' }
if ($futureRunner -ne 'scripts/run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1.cmd') { throw 'Future confirmation runner path drifted.' }
$runValues = @($confirm.run_index_values)
if ($runValues.Count -ne 5) { throw 'Future confirmation run-index list must contain five values.' }
for ($i = 0; $i -lt 5; $i++) { if ([int]$runValues[$i] -ne ($i + 1)) { throw 'Future confirmation run-index values must be exactly 1..5.' } }
if (Test-Path -LiteralPath $futureTest) { throw 'C4 full-domain confirmation test must not already exist in the planning package.' }
if (Test-Path -LiteralPath $futureRunner) { throw 'C4 full-domain confirmation runner must not already exist in the planning package.' }

$selection = $contract.future_rp1c_selection_gate_after_confirmation
if (@($selection.decision_space).Count -ne 2) { throw 'Future selection decision space must have exactly two entries.' }
if (-not (@($selection.decision_space) -contains 'SELECT-C4')) { throw 'SELECT-C4 missing from future decision space.' }
if (-not (@($selection.decision_space) -contains 'SELECT-NONE')) { throw 'SELECT-NONE missing from future decision space.' }
if ($selection.select_c4_requires_full_domain_confirmation_pass -ne $true) { throw 'SELECT-C4 must require returned full-domain confirmation PASS.' }
if ($selection.d3_selectable_without_new_corrected_performance_evidence -ne $false) { throw 'D3 cannot be selectable under current evidence.' }
if ($selection.new_measurement_allowed -ne $false) { throw 'Selection gate itself must not add new measurements after confirmation.' }
if ($selection.post_selection_next_action_if_c4_selected -ne 'R1-IMPLEMENTATION-PLANNING-ONLY') { throw 'Unexpected post-selection authority boundary.' }

$inv = [System.Globalization.CultureInfo]::InvariantCulture
$contractArtifact = @(
    'status=PASS-AS-AUTHORED',
    'gate=M10-FINAL-VR2-ENGINEERING-REPAIR-PLANNING1-RP1C-PLANNING1',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-planning1-v1',
    'c4-returned-classification=C4-QUALIFIED-ALLOCATION-TAIL-CLOSED',
    'corrected-strict-max-us=' + $strictMax.ToString('R', $inv),
    'c4-physical-and-r1-qualification-complete=True',
    'c4-full-domain-performance-confirmation-complete=False',
    'c4-selection-ready-now=False',
    'd3-selection-ready-now=False',
    'selection-ready-count-now=0',
    'next-confirmation-gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1',
    'confirmation-fresh-processes=5',
    'confirmation-total-measured-calls=217600',
    'automatic-selection-authorized=False',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'exact-v9-change-authorized=False',
    'vr3-authorized=False',
    'p3-r1-authorized=False',
    'second-replacement-long-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), $contractArtifact, [System.Text.Encoding]::UTF8)

$matrix = @(
    'candidate_id,family_id,role,physical_complete,planning_target_met,seam_complete,deterministic_repeat,r1_performance_closed,full_domain_candidate_performance_confirmed,selection_ready,selection_blocker',
    'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE,C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE,ACTIVE-SELECTION-CANDIDATE,True,True,True,True,True,False,False,FULL-DOMAIN-C4-PERFORMANCE-CONFIRMATION-REQUIRED',
    'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR,D-BOUNDED-PRODUCTION-IF97-SUBSET-COMPARATOR,ACTIVE-COMPARATOR,True,True,True,True,False,True,False,SEAM-MAX-EXCEEDS-STRICT-CEILING'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-selection-readiness-matrix.csv'), $matrix, [System.Text.Encoding]::UTF8)

$policy = @(
    'status=PASS-RP1C-PLANNING-CONTRACT-FROZEN',
    'selection-result=NOT-PERFORMED',
    'selection-ready-count-now=0',
    'c4-current-state=PHYSICAL-AND-R1-QUALIFIED-FULL-DOMAIN-TIMING-PENDING',
    'd3-current-blocker=SEAM-MAX-16504.9-US-GT-409.30666666666673-US',
    'no-selection-option-preserved=True',
    'next-evidence-gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1',
    'next-evidence-fresh-processes=5',
    'next-evidence-total-measured-calls=217600',
    'future-selection-decision-space=SELECT-C4|SELECT-NONE',
    'candidate-mutation-authorized=False',
    'threshold-change-authorized=False',
    'next-action=RETURN-COMPLETE-RP1C-PLANNING-ARTIFACTS-FOR-ADJUDICATION',
    'rp1c-selection-authorized=False',
    'production-repair-authorized=False'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-selection-policy-summary.txt'), $policy, [System.Text.Encoding]::UTF8)

$review = @(
    'status=STATIC-PREEXECUTION-REVIEW-PASS',
    'finding-1=D3-REFINEMENT2-ELIGIBILITY-OMITTED-SEAM-WORST-CASE',
    'finding-1-frozen-seam-max-us=16504.9',
    'finding-1-strict-max-us=' + $strictMax.ToString('R', $inv),
    'finding-2=C4-R1-TAIL-CLOSED-BUT-FULL-DOMAIN-C4-TIMING-NOT-YET-MEASURED',
    'finding-2-c4-worst-returned-r1-max-us=' + $c4WorstMax.ToString('R', $inv),
    'finding-3=BIT-EQUIVALENCE-DOES-NOT-PROVE-WALL-CLOCK-EQUIVALENCE',
    'finding-4=SELECTION-READY-COUNT-REMAINS-ZERO-BEFORE-CONFIRMATION',
    'future-confirmation-implementation-contained=False',
    'returned-planning-adjudication-required-before-confirmation=True',
    'post-selection-if-c4=R1-IMPLEMENTATION-PLANNING-ONLY'
)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), $review, [System.Text.Encoding]::UTF8)

$actualOutputs = @(Get-ChildItem -LiteralPath $artifactDir -File | Sort-Object Name)
if ($actualOutputs.Count -ne 4) { throw 'RP1C Planning 1 must write exactly four artifacts.' }
foreach ($required in @($contract.planning_outputs)) {
    if (-not (Test-Path -LiteralPath (Join-Path $artifactDir $required) -PathType Leaf)) {
        throw ("Missing planning artifact: {0}" -f $required)
    }
}

Write-Host 'RP1C Planning 1 static audit: PASS-AS-AUTHORED'
Write-Host ("Artifacts: {0}" -f $artifactDir)
