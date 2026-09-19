$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Require-Text([string]$Path,[string]$Needle) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
    $text = [System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8)
    if ($text.IndexOf($Needle,[System.StringComparison]::Ordinal) -lt 0) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Get-Sha256([byte[]]$Bytes) {
    $sha=[System.Security.Cryptography.SHA256]::Create(); try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant() } finally { $sha.Dispose() }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$contractPath = Join-Path $repoRoot 'eng\m10-final-vr2-engineering-repair-planning1-rp1c-selection-planning1-contract.json'
$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-rp1c-selection-planning1-v1') 'Selection Planning 1 schema mismatch.'
Require ($contract.status -eq 'PLANNING-ONLY') 'Selection Planning 1 status mismatch.'
Require ([bool]$contract.readiness.c4_selection_ready) 'C4 must be selection-ready after returned FDPC2.'
Require (-not [bool]$contract.readiness.d3_selection_ready) 'D3 must remain blocked.'
Require ([int]$contract.readiness.selection_ready_count -eq 1) 'Selection-ready count must be exactly one.'
Require ($contract.readiness.c4_runtime_profile -eq 'AMBIENT-UNSET') 'C4 runtime profile drift.'
Require ([bool]$contract.readiness.c4_fdpc1_negative_history_preserved) 'FDPC1 negative history must remain preserved.'
Require ($contract.future_selection_gate.id -eq 'RP1C-ENGINEERING-REPAIR-SELECTION1') 'Future selection gate id drift.'
$decisions=@($contract.future_selection_gate.decision_space)
Require ($decisions.Count -eq 2) 'Decision space must contain exactly two entries.'
Require ($decisions -contains 'SELECT-C4') 'SELECT-C4 missing.'
Require ($decisions -contains 'SELECT-NONE') 'SELECT-NONE missing.'
Require ([bool]$contract.future_selection_gate.select_none_mandatory_option) 'SELECT-NONE must remain mandatory.'
Require (-not [bool]$contract.future_selection_gate.new_measurement_allowed) 'Selection gate must not add measurements.'
Require (-not [bool]$contract.future_selection_gate.candidate_mutation_allowed) 'Candidate mutation must remain false.'
Require (-not [bool]$contract.future_selection_gate.threshold_change_allowed) 'Threshold change must remain false.'
Require (-not [bool]$contract.future_selection_gate.runtime_change_allowed) 'Runtime change must remain false.'
Require (-not [bool]$contract.future_selection_gate.automatic_selection_allowed) 'Automatic selection must remain false.'
Require ($contract.future_selection_gate.post_select_c4 -eq 'R1-IMPLEMENTATION-PLANNING-ONLY') 'Post-C4 boundary drift.'
foreach ($p in $contract.authority.PSObject.Properties) { Require (-not [bool]$p.Value) ("Authority must remain false: {0}" -f $p.Name) }

$fdpcRoot=Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_FullDomainPerformanceConfirmation2_Artifacts'
Require-Text (Join-Path $fdpcRoot '01-contract-and-provenance.txt') 'status=PASS-EVIDENCE-COMPLETE'
Require-Text (Join-Path $fdpcRoot '06-confirmation-evidence-adjudication.txt') 'classification=C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED'
Require-Text (Join-Path $fdpcRoot '06-confirmation-evidence-adjudication.txt') 'all-five-processes-meet-corrected-performance-predicate=True'
Require-Text (Join-Path $fdpcRoot '06-confirmation-evidence-adjudication.txt') 'exact-v9-calls-over-max=0'
Require-Text (Join-Path $fdpcRoot '06-confirmation-evidence-adjudication.txt') 'seam-calls-over-max=0'
Require-Text (Join-Path $fdpcRoot '07-rp1c-c4-full-domain-performance-confirmation2-summary.txt') 'fdpc2-selection-ready=True'

$treeManifest=Import-Csv -LiteralPath (Join-Path $fdpcRoot '08-returned-tree-manifest.csv')
Require ($treeManifest.Count -eq 32) 'Returned FDPC2 manifest must contain 32 rows.'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath=Join-Path $repoRoot 'eng\frozen-evidence\archive\rp1c-c4-fulldomainperformanceconfirmation2-large-payloads.zip'
$zip=[System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
  foreach($row in $treeManifest) {
    if ($row.storage -eq 'direct') {
      $p=Join-Path $fdpcRoot ($row.relative_path.Replace('/','\'))
      Require (Test-Path -LiteralPath $p -PathType Leaf) ("Missing direct FDPC2 evidence: {0}" -f $row.relative_path)
      $bytes=[System.IO.File]::ReadAllBytes($p)
      Require ($bytes.Length -eq [int64]$row.bytes) ("FDPC2 direct size mismatch: {0}" -f $row.relative_path)
      Require ((Get-Sha256 $bytes) -eq $row.sha256.ToUpperInvariant()) ("FDPC2 direct hash mismatch: {0}" -f $row.relative_path)
    } elseif ($row.storage -eq 'archive') {
      $logical='M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_FullDomainPerformanceConfirmation2_Artifacts/' + $row.relative_path.Replace('\','/')
      $entry=$zip.GetEntry($logical); Require ($null -ne $entry) ("Missing archived FDPC2 evidence: {0}" -f $row.relative_path)
      Require ($entry.Length -eq [int64]$row.bytes) ("FDPC2 archive size mismatch: {0}" -f $row.relative_path)
      $s=$entry.Open(); try { $ms=New-Object System.IO.MemoryStream; $s.CopyTo($ms); $bytes=$ms.ToArray(); $ms.Dispose() } finally { $s.Dispose() }
      Require ((Get-Sha256 $bytes) -eq $row.sha256.ToUpperInvariant()) ("FDPC2 archive hash mismatch: {0}" -f $row.relative_path)
    } else { throw ("Unknown FDPC2 storage: {0}" -f $row.storage) }
  }
} finally { $zip.Dispose() }

$main=Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_SELECTION_PLANNING1.md'
$pre=Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_SELECTION_PLANNING1_PREEXECUTION_REVIEW.md'
$adr=Join-Path $repoRoot 'docs\adr\0209-open-rp1c-selection-only-after-green-fdpc2-and-preserve-select-none.md'
Require-Text $main 'selection-ready count = 1'
Require-Text $main 'SELECT-C4'
Require-Text $main 'SELECT-NONE'
Require-Text $pre 'RP1C selection is not performed by this planning gate.'
Require-Text $adr 'SELECT-C4 | SELECT-NONE'

$roadmap=Join-Path $repoRoot 'docs\ROADMAP.md'
$detailed=Join-Path $repoRoot 'docs\M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md'
$docsReadme=Join-Path $repoRoot 'docs\README.md'
$parentPlan=Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md'
Require-Text $roadmap 'The only live successor is planning-only `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`.'
Require-Text $detailed 'The next and only authorized activity is `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`.'
Require-Text $docsReadme 'The only live gate is planning-only `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`.'
Require-Text $parentPlan 'The live successor is planning-only `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`'
Require (-not [bool]$contract.future_selection_gate.selection_decision_frozen_by_planning) 'Planning must not preselect C4.'
Require ([bool]$contract.future_selection_gate.returned_selection_adjudication_required_before_r1_planning) 'Returned selection adjudication must precede R1 planning.'
foreach($futurePath in @($contract.future_selection_gate.contract,$contract.future_selection_gate.validator,$contract.future_selection_gate.runner)) {
    Require (-not (Test-Path -LiteralPath (Join-Path $repoRoot ($futurePath.Replace('/','\'))))) ("Future selection implementation must not be contained: {0}" -f $futurePath)
}

$artifactDir=Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-selection-planning1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$inv=[System.Globalization.CultureInfo]::InvariantCulture
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), @(
 'status=PASS-AS-AUTHORED',
 'gate=RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1',
 'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-selection-planning1-v1',
 'fdpc2-returned-adjudication=PASS',
 'c4-selection-ready=True',
 'c4-runtime-profile=AMBIENT-UNSET',
 'd3-selection-ready=False',
 'selection-ready-count=1',
 'future-gate=RP1C-ENGINEERING-REPAIR-SELECTION1',
 'rp1c-selection-authorized=False',
 'production-repair-authorized=False'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-selection-readiness-matrix.csv'), @(
 'candidate_id,runtime_profile,selection_ready,blocker_or_basis',
 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE,AMBIENT-UNSET,True,FDPC2-FIVE-OF-FIVE-PASS-CORRECTED-PREDICATE',
 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR,N/A,False,SEAM-MAX-16504.9-US-EXCEEDS-409.30666666666673-US'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-selection-policy-summary.txt'), @(
 'status=PASS-RP1C-SELECTION-PLANNING-CONTRACT-FROZEN',
 'selection-result=NOT-PERFORMED',
 'selection-ready-count=1',
 'future-selection-decision-space=SELECT-C4|SELECT-NONE',
 'select-none-mandatory=True',
 'new-measurement-in-selection-gate=False',
 'automatic-selection=False',
 'post-select-c4=R1-IMPLEMENTATION-PLANNING-ONLY',
 'rp1c-selection-authorized=False',
 'production-repair-authorized=False',
 'next-action=RETURN-COMPLETE-SELECTION-PLANNING-ARTIFACTS-FOR-ADJUDICATION'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), @(
 'status=STATIC-PREEXECUTION-REVIEW-PASS',
 'finding-1=FDPC2-EVIDENCE-COMPLETE-32-FILES-217600-CALLS',
 'finding-2=C4-AMBIENT-PAIR-SELECTION-READY',
 'finding-3=D3-REMAINS-BLOCKED-BY-FROZEN-SEAM-MAX',
 'finding-4=SELECT-NONE-REMAINS-MANDATORY',
 'finding-5=SELECTION-GATE-MUST-NOT-MEASURE-OR-MUTATE',
 'future-selection-implementation-contained=False',
 'rp1c-selection-authorized=False',
 'production-repair-authorized=False'
),[System.Text.Encoding]::UTF8)
$files=@(Get-ChildItem -LiteralPath $artifactDir -File)
Require ($files.Count -eq 4) 'Selection Planning 1 must write exactly four artifacts.'
Write-Host 'RP1C Selection Planning 1 static audit: PASS-AS-AUTHORED' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f $artifactDir)
