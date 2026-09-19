$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Read-Utf8Text([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path), [System.Text.Encoding]::UTF8)
}
function Normalized-Sha256([string]$Path) {
    $text = Read-Utf8Text $Path
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToUpperInvariant() }
    finally { $sha.Dispose() }
}
function Require-Text([string]$Path, [string]$Needle) {
    $text = Read-Utf8Text $Path
    Require ($text.Contains($Needle)) ("Required marker not found in {0}: {1}" -f $Path,$Needle)
}
function Require-Ascii([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    $bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path))
    foreach ($b in $bytes) { Require ($b -le 127) ("Executable source contains non-ASCII byte: {0}" -f $Path) }
}

$contractPath = 'eng\m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-contract.json'
$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-v1') 'FDPC2 contract schema mismatch.'
Require ($contract.status -eq 'CANDIDATE-EVIDENCE-ONLY') 'FDPC2 contract status mismatch.'
Require ([bool]$contract.authority.fdpc2_implementation_authorized) 'FDPC2 implementation must be authorized by returned planning adjudication.'
Require (-not [bool]$contract.authority.rp1c_selection_authorized) 'RP1C selection must remain unauthorized.'
Require (-not [bool]$contract.authority.production_runtime_change_authorized) 'Production runtime change must remain unauthorized.'
Require (-not [bool]$contract.authority.production_repair_authorized) 'Production repair must remain unauthorized.'
Require (-not [bool]$contract.authority.threshold_change_authorized) 'Threshold change must remain unauthorized.'
Require (-not [bool]$contract.authority.exact_v9_change_authorized) 'Exact-v9 change must remain unauthorized.'
Require ($contract.protocol.runtime_profile -eq 'AMBIENT-UNSET') 'FDPC2 runtime profile drift.'
Require ([bool]$contract.protocol.same_physical_host_as_a2_required) 'FDPC2 same-host requirement drift.'
Require ($contract.protocol.required_host_fingerprint_sha256 -eq '931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336') 'FDPC2 host fingerprint drift.'
Require ($contract.protocol.required_active_power_scheme_guid -eq '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c') 'FDPC2 power scheme drift.'
Require ([int]$contract.protocol.fresh_process_count -eq 5) 'FDPC2 process count drift.'
Require ([int]$contract.protocol.total_measured_calls -eq 217600) 'FDPC2 measured-call count drift.'
Require ([int]$contract.protocol.total_required_files -eq 32) 'FDPC2 required-file count drift.'
Require (-not [bool]$contract.protocol.engineering_negative_is_infrastructure_red) 'Engineering-negative evidence must not become infrastructure RED.'
Require ([Math]::Abs(([double]$contract.corrected_performance_predicate.single_call_max_us_max) - 409.30666666666673) -lt 1e-12) 'Strict max drift.'

$controlled = @(
 'DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun',
 'COMPlus_TieredCompilation','COMPlus_TieredPGO','COMPlus_TC_QuickJit','COMPlus_TC_QuickJitForLoops','COMPlus_ReadyToRun'
)
foreach ($name in $controlled) {
    $value = [Environment]::GetEnvironmentVariable($name, 'Process')
    Require ([string]::IsNullOrEmpty($value)) ("FDPC2 AMBIENT-UNSET caller contamination: {0}={1}" -f $name,$value)
}

$planningRoot = 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_FullDomainPerformanceConfirmation2_Planning1_Artifacts'
$planningHashes = @{
 '01-contract-and-provenance.txt'='9444C475EBB5E92D2446ABFE0ADD379266D5EA6825549184B0F1D46AC897552E';
 '02-fdpc2-design-matrix.csv'='3EBF332A614C65B0A2817E4534B6D5756C301224C8F7994B71F5F616BCF73A9C';
 '03-planning-summary.txt'='159D001835BEFFA6845F66AA34F550FF4562FD5AD6901A9E5F98527DB3B529D8';
 '04-preexecution-review.txt'='CBB81DF8C48CCFB94AEAD70749BCF3C44333C84630A9624CB2B2E364EBECB5F8'
}
foreach ($name in $planningHashes.Keys) {
    Require ((Normalized-Sha256 (Join-Path $planningRoot $name)) -eq $planningHashes[$name]) ("Returned FDPC2 Planning 1 artifact hash mismatch: {0}" -f $name)
}
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $planningRoot '03-planning-summary.txt') 'future-fdpc2-profile=AMBIENT-UNSET'
Require-Text (Join-Path $planningRoot '04-preexecution-review.txt') 'finding-5=FDPC2-MUST-USE-SAME-A2-HOST-AND-POWER-SCHEME'

Require ((Normalized-Sha256 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs') -eq $contract.normalized_lf_sha256.c4_candidate) 'C4 candidate drift.'
Require ((Normalized-Sha256 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv') -eq $contract.normalized_lf_sha256.rp1a_exact_v9_corpus) 'Exact-v9 corpus drift.'
Require ((Normalized-Sha256 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\05-seam-probe-map.csv') -eq $contract.normalized_lf_sha256.rp1a_seam_corpus) 'Seam corpus drift.'
Require ((Normalized-Sha256 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv') -eq $contract.normalized_lf_sha256.rp1a_performance_baseline) 'Performance baseline drift.'

$implementation = @{
 'focused_test'='tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation2Tests.cs';
 'orchestrator'='eng\invoke-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.ps1';
 'adjudicator'='eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.ps1';
 'runner'='scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.cmd';
 'main_doc'='docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2.md';
 'preexec_doc'='docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PREEXECUTION_REVIEW.md';
 'returned_doc'='docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_RETURNED_EVIDENCE_ADJUDICATION.md';
 'planning_returned_doc'='docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md';
 'adr_0208'='docs\adr\0208-execute-fdpc2-under-ambient-runtime-on-frozen-a2-host.md'
}
foreach ($name in $implementation.Keys) {
    $expected = [string]$contract.normalized_lf_sha256.$name
    Require (-not [string]::IsNullOrWhiteSpace($expected)) ("FDPC2 contract missing implementation hash: {0}" -f $name)
    Require ((Normalized-Sha256 $implementation[$name]) -eq $expected.ToUpperInvariant()) ("FDPC2 implementation hash drifted: {0}" -f $name)
}

Require-Text $implementation['focused_test'] 'runtime-profile=AMBIENT-UNSET'
Require-Text $implementation['focused_test'] 'host-fingerprint-sha256='
Require-Text $implementation['orchestrator'] '$requiredFingerprint'
Require-Text $implementation['orchestrator'] '$runtimeVariables'
Require-Text $implementation['adjudicator'] 'Require-CompleteMatrix'
Require-Text $implementation['adjudicator'] 'negative-performance-outcome-is-runner-failure=False'
Require-Text $implementation['runner'] '[3/4] Same-host ambient/unset FDPC2 evidence collection'
Require-Text $implementation['main_doc'] '217,600 measured calls'
Require-Text $implementation['preexec_doc'] 'STATIC PRE-EXECUTION REVIEW: PASS'
Require-Text $implementation['planning_returned_doc'] 'FDPC2-IMPLEMENTATION-AUTHORIZED=True'

foreach ($path in @(
    'eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.ps1',
    $implementation['orchestrator'],
    $implementation['adjudicator'],
    $implementation['runner']
)) { Require-Ascii $path }

Write-Host 'FDPC2 static contract audit: PASS-AS-AUTHORED' -ForegroundColor Green
exit 0
