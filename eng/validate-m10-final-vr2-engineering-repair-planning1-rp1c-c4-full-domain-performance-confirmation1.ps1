$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Fail([string]$Message) {
    Write-Host "RP1C C4 Full-Domain Performance Confirmation 1 static audit: FAIL - $Message" -ForegroundColor Red
    exit 2
}
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }
function Read-All([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) "missing file: $Path"
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}
function Normalized-Sha256([string]$Path) {
    $text = Read-All $Path
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}
function Require-Contains([string]$Text, [string]$Needle, [string]$Context) {
    Require ($Text.Contains($Needle)) "$Context missing marker: $Needle"
}

$contractPath = 'eng\m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1-contract.json'
$contract = (Read-All $contractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1-v1') 'contract schema mismatch'
Require ($contract.status -eq 'CANDIDATE') 'contract status mismatch'
Require ([bool]$contract.authority.confirmation_implementation_authorized) 'confirmation implementation is not authorized'
Require (-not [bool]$contract.authority.rp1c_selection_authorized) 'RP1C selection must remain unauthorized'
Require (-not [bool]$contract.authority.production_repair_authorized) 'production repair must remain unauthorized'
Require ([int]$contract.protocol.fresh_process_count -eq 5) 'fresh process count drift'
Require ([int]$contract.protocol.total_measured_calls -eq 217600) 'total measured call count drift'
Require ([int]$contract.evidence.total_required_files -eq 30) 'evidence file count drift'
Require ([double]$contract.corrected_performance_predicate.exact_v9_resolve_median_us_max -eq 94.8) 'median ceiling drift'
Require ([Math]::Abs(([double]$contract.corrected_performance_predicate.exact_v9_resolve_p95_us_max) - 158.80666666666667) -lt 1e-12) 'p95 ceiling drift'
Require ([Math]::Abs(([double]$contract.corrected_performance_predicate.exact_v9_single_call_max_us_max) - 409.30666666666673) -lt 1e-12) 'max ceiling drift'
Require ([double]$contract.corrected_performance_predicate.exact_v9_median_candidate_allocation_bytes_max -eq 2816) 'allocation ceiling drift'

$planningRoot = 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_Planning1_Artifacts'
$planning = @{
    '01-contract-and-provenance.txt' = '9A93B80B211002AEC9390F957B3A00F8643D60FA38191DEBB772E22492E11ACA'
    '02-selection-readiness-matrix.csv' = 'A96E66F4A7CE4F376547E454288BF5FCEBA1831FED39B66005B897BD64133BEB'
    '03-selection-policy-summary.txt' = '64AA0BA7378430AD5381A8C80A22C8D6AE6A8F93DB5B25C11EE1A5A271B044AD'
    '04-preexecution-review.txt' = '0FF168162E6050DC246D69E9B1FD80AA6CB027B4CB722E478E3FF954C18EA2CE'
}
foreach ($name in $planning.Keys) {
    $path = Join-Path $planningRoot $name
    Require ((Normalized-Sha256 $path) -eq $planning[$name]) "returned planning artifact hash mismatch: $name"
}
$planningContract = Read-All (Join-Path $planningRoot '01-contract-and-provenance.txt')
$planningSummary = Read-All (Join-Path $planningRoot '03-selection-policy-summary.txt')
$planningReview = Read-All (Join-Path $planningRoot '04-preexecution-review.txt')
Require-Contains $planningContract 'status=PASS-AS-AUTHORED' 'planning contract'
Require-Contains $planningContract 'c4-selection-ready-now=False' 'planning contract'
Require-Contains $planningContract 'selection-ready-count-now=0' 'planning contract'
Require-Contains $planningContract 'next-confirmation-gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1' 'planning contract'
Require-Contains $planningContract 'confirmation-fresh-processes=5' 'planning contract'
Require-Contains $planningContract 'confirmation-total-measured-calls=217600' 'planning contract'
Require-Contains $planningContract 'rp1c-selection-authorized=False' 'planning contract'
Require-Contains $planningSummary 'selection-result=NOT-PERFORMED' 'planning summary'
Require-Contains $planningSummary 'future-selection-decision-space=SELECT-C4|SELECT-NONE' 'planning summary'
Require-Contains $planningReview 'future-confirmation-implementation-contained=False' 'planning review'
Require-Contains $planningReview 'returned-planning-adjudication-required-before-confirmation=True' 'planning review'

$adjudicationDoc = Read-All 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md'
Require-Contains $adjudicationDoc 'Returned RP1C Planning 1 evidence is adjudicated **PASS**.' 'returned planning adjudication'
Require-Contains $adjudicationDoc 'RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1' 'returned planning adjudication'
Require-Contains $adjudicationDoc 'does **not** authorize candidate mutation, RP1C selection, production repair' 'returned planning adjudication'

$pins = @{
    'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs' = '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv' = '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\05-seam-probe-map.csv' = '9035396FDB5A1F6AD89F0B9D28A26992D577906812DE412EFA2AABA98FEBE47D'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv' = '91776709A34142E9312A092F3B19EAFCF0FCA576D561F296CC9272FCDC4F7DEB'
}
foreach ($path in $pins.Keys) { Require ((Normalized-Sha256 $path) -eq $pins[$path]) "immutable candidate/corpus hash mismatch: $path" }

$testPath = 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation1Tests.cs'
$testText = Read-All $testPath
Require-Contains $testText '[Fact(Explicit = true)]' 'confirmation test'
Require-Contains $testText 'Rp1cC4FullDomainPerformanceConfirmation1_MeasuresOneFreshProcess' 'confirmation test'
Require-Contains $testText 'ExactMeasuredCalls = ExactRowCount * ExactMeasuredPasses' 'confirmation test'
Require-Contains $testText 'SeamMeasuredCalls = SeamRowCount * SeamMeasuredPasses' 'confirmation test'
Require-Contains $testText 'PrimeTimingHarness' 'confirmation test'
Require-Contains $testText 'harnessAllocatedBytes' 'confirmation test'
Require-Contains $testText 'engineering-negative-outcome-is-xunit-failure=False' 'confirmation test'
Require (-not $testText.Contains('Assert.True(strictMet')) 'negative engineering result must not fail xUnit'

$runnerPath = 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1.cmd'
$adjudicatorPath = 'eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1.ps1'
Require (Test-Path -LiteralPath $runnerPath -PathType Leaf) 'runner missing'
Require (Test-Path -LiteralPath $adjudicatorPath -PathType Leaf) 'adjudicator missing'
$runnerText = Read-All $runnerPath
Require-Contains $runnerText 'for /L %%R in (1,1,5)' 'runner'
Require-Contains $runnerText '--explicit only' 'runner'
Require-Contains $runnerText '--parallel none' 'runner'
Require-Contains $runnerText 'eng\ci-ordinary.cmd' 'runner'

Write-Host 'RP1C C4 Full-Domain Performance Confirmation 1 static audit: PASS-AS-AUTHORED' -ForegroundColor Green
exit 0
