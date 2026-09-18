$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Require-Text([string]$Path,[string]$Needle) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
    $text=[System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8)
    if ($text.IndexOf($Needle,[System.StringComparison]::Ordinal) -lt 0) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Get-Sha256([byte[]]$Bytes) {
    $sha=[System.Security.Cryptography.SHA256]::Create(); try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant() } finally { $sha.Dispose() }
}
function Get-TreeSha256([string]$Root, [string[]]$ExcludeDirectoryNames = @()) {
    $resolved=(Resolve-Path -LiteralPath $Root).Path
    $excluded=New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach($name in $ExcludeDirectoryNames) { [void]$excluded.Add($name) }
    $paths=New-Object 'System.Collections.Generic.List[string]'
    foreach($file in @(Get-ChildItem -LiteralPath $resolved -Recurse -File)) {
        $rel=$file.FullName.Substring($resolved.Length + 1).Replace('\','/')
        $segments=@($rel.Split('/'))
        $skip=$false
        for($i=0;$i -lt ($segments.Count - 1);$i++) { if($excluded.Contains($segments[$i])) { $skip=$true; break } }
        if(-not $skip) { $paths.Add($rel) }
    }
    $paths.Sort([System.StringComparer]::Ordinal)
    $ms=New-Object System.IO.MemoryStream
    try {
        foreach($rel in $paths) {
            $full=Join-Path $resolved $rel.Replace('/','\')
            $relBytes=[System.Text.Encoding]::UTF8.GetBytes($rel)
            $ms.Write($relBytes,0,$relBytes.Length); $ms.WriteByte(0)
            $sha=[System.Security.Cryptography.SHA256]::Create(); $fs=[System.IO.File]::OpenRead($full)
            try { $fh=$sha.ComputeHash($fs) } finally { $fs.Dispose(); $sha.Dispose() }
            $ms.Write($fh,0,$fh.Length); $ms.WriteByte(10)
        }
        $ms.Position=0
        $sha2=[System.Security.Cryptography.SHA256]::Create()
        try { $th=$sha2.ComputeHash($ms) } finally { $sha2.Dispose() }
        return @{ Hash=([BitConverter]::ToString($th)).Replace('-','').ToUpperInvariant(); Count=$paths.Count }
    } finally { $ms.Dispose() }
}

function Get-NormalizedSha256([string]$Path) {
    $text=[System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n")
    return Get-Sha256 ([System.Text.Encoding]::UTF8.GetBytes($text))
}

$repoRoot=Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$contractPath=Join-Path $repoRoot 'eng\m10-final-vr2-engineering-repair-planning1-rp1c-selection1-contract.json'
$contract=Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-rp1c-selection1-v1') 'RP1C Selection 1 schema mismatch.'
Require ($contract.status -eq 'DECISION-ONLY') 'RP1C Selection 1 status mismatch.'
$decisions=@($contract.decision_space)
Require ($decisions.Count -eq 2) 'Selection decision space must contain exactly two entries.'
Require ($decisions[0] -eq 'SELECT-C4' -and $decisions[1] -eq 'SELECT-NONE') 'Selection decision space/order drift.'
$srcTree=Get-TreeSha256 (Join-Path $repoRoot 'src') @('bin','obj')
$testsTree=Get-TreeSha256 (Join-Path $repoRoot 'tests') @('bin','obj')
Require ([int]$srcTree.Count -eq [int]$contract.repository_identity.src_count) 'Canonical src file count drifted.'
Require ($srcTree.Hash -eq $contract.repository_identity.src_tree_sha256) 'Canonical src tree drifted.'
Require ([int]$testsTree.Count -eq [int]$contract.repository_identity.tests_count) 'Canonical tests file count drifted.'
Require ($testsTree.Hash -eq $contract.repository_identity.tests_tree_sha256) 'Canonical tests tree drifted.'
Require ($contract.decision.selection_result -eq 'SELECT-C4') 'Authored selection result must be SELECT-C4.'
Require ($contract.decision.selection_mode -eq 'AUTHORED-ENGINEERING-DECISION') 'Selection mode drift.'
Require (-not [bool]$contract.decision.automatic_selection) 'Automatic selection must remain false.'
Require ([bool]$contract.decision.select_none_preserved) 'SELECT-NONE must remain preserved.'
Require ([bool]$contract.decision.select_none_considered) 'SELECT-NONE must be considered.'
Require ($contract.decision.selected_candidate -eq 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE') 'Selected candidate drift.'
Require ($contract.decision.selected_runtime_profile -eq 'AMBIENT-UNSET') 'Selected runtime profile drift.'
Require ([bool]$contract.readiness.c4_selection_ready) 'C4 must remain selection-ready.'
Require (-not [bool]$contract.readiness.d3_selection_ready) 'D3 must remain blocked.'
Require ([int]$contract.readiness.selection_ready_count -eq 1) 'Selection-ready count must remain one.'
Require (-not [bool]$contract.execution.new_measurement_allowed) 'New measurement must remain forbidden.'
Require (-not [bool]$contract.execution.candidate_mutation_allowed) 'Candidate mutation must remain forbidden.'
Require (-not [bool]$contract.execution.runtime_change_allowed) 'Runtime change must remain forbidden.'
Require (-not [bool]$contract.execution.threshold_change_allowed) 'Threshold change must remain forbidden.'
Require (-not [bool]$contract.execution.exact_v9_change_allowed) 'Exact-v9 change must remain forbidden.'
Require (-not [bool]$contract.execution.seam_corpus_change_allowed) 'Seam corpus change must remain forbidden.'
Require ([int]$contract.execution.required_files -eq 4) 'Selection output file count contract drift.'
$outputs=@($contract.execution.outputs)
Require ($outputs.Count -eq 4) 'Selection output list must contain four files.'
Require ($outputs[0] -eq '01-selection-decision-and-provenance.txt') 'Selection output 01 drift.'
Require ($outputs[1] -eq '02-selection-readiness-matrix.csv') 'Selection output 02 drift.'
Require ($outputs[2] -eq '03-selection-decision-summary.txt') 'Selection output 03 drift.'
Require ($outputs[3] -eq '04-preexecution-review.txt') 'Selection output 04 drift.'
Require ($contract.post_selection.select_c4_successor -eq 'R1-IMPLEMENTATION-PLANNING-ONLY') 'Post-selection boundary drift.'
Require ([bool]$contract.post_selection.returned_selection_adjudication_required) 'Returned selection adjudication must be required.'
foreach($p in $contract.authority.PSObject.Properties) { Require (-not [bool]$p.Value) ("Downstream authority must remain false: {0}" -f $p.Name) }

$planningRoot=Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_SelectionPlanning1_Artifacts'
foreach($row in $contract.prerequisite.selection_planning1_artifacts.PSObject.Properties) {
    $p=Join-Path $planningRoot $row.Name
    Require (Test-Path -LiteralPath $p -PathType Leaf) ("Missing returned Selection Planning 1 artifact: {0}" -f $row.Name)
    $bytes=[System.IO.File]::ReadAllBytes($p)
    Require ($bytes.Length -eq [int64]$row.Value.bytes) ("Selection Planning 1 artifact size mismatch: {0}" -f $row.Name)
    Require ((Get-Sha256 $bytes) -eq $row.Value.sha256.ToUpperInvariant()) ("Selection Planning 1 artifact hash mismatch: {0}" -f $row.Name)
}
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $planningRoot '03-selection-policy-summary.txt') 'future-selection-decision-space=SELECT-C4|SELECT-NONE'
Require-Text (Join-Path $planningRoot '03-selection-policy-summary.txt') 'automatic-selection=False'
Require-Text (Join-Path $planningRoot '03-selection-policy-summary.txt') 'selection-result=NOT-PERFORMED'
Require-Text (Join-Path $planningRoot '04-preexecution-review.txt') 'finding-2=C4-AMBIENT-PAIR-SELECTION-READY'
Require-Text (Join-Path $planningRoot '04-preexecution-review.txt') 'finding-3=D3-REMAINS-BLOCKED-BY-FROZEN-SEAM-MAX'

$fdpcRoot=Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_FullDomainPerformanceConfirmation2_Artifacts'
Require-Text (Join-Path $fdpcRoot '06-confirmation-evidence-adjudication.txt') 'classification=C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED'
Require-Text (Join-Path $fdpcRoot '06-confirmation-evidence-adjudication.txt') 'all-five-processes-meet-corrected-performance-predicate=True'
Require-Text (Join-Path $fdpcRoot '07-rp1c-c4-full-domain-performance-confirmation2-summary.txt') 'fdpc2-selection-ready=True'

foreach($row in $contract.implementation_hashes.PSObject.Properties) {
    $p=Join-Path $repoRoot ($row.Value.path.Replace('/','\'))
    Require (Test-Path -LiteralPath $p -PathType Leaf) ("Implementation file missing: {0}" -f $row.Value.path)
    Require ((Get-NormalizedSha256 $p) -eq $row.Value.normalized_sha256.ToUpperInvariant()) ("Selection implementation hash drifted: {0}" -f $row.Name)
}

$main=Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_SELECTION1.md'
$pre=Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_SELECTION1_PREEXECUTION_REVIEW.md'
$ret=Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_SELECTION1_RETURNED_EVIDENCE_ADJUDICATION.md'
$adr=Join-Path $repoRoot 'docs\adr\0210-select-c4-for-r1-planning-after-green-fdpc2-without-production-activation.md'
Require-Text $main 'The authored decision is:'
Require-Text $main 'SELECT-C4'
Require-Text $main 'SELECT-NONE'
Require-Text $pre 'The authored selection is `SELECT-C4`'
Require-Text $ret 'selection-result=SELECT-C4'
Require-Text $adr 'Author the RP1C decision as `SELECT-C4`'

$roadmap=Join-Path $repoRoot 'docs\ROADMAP.md'
$detailed=Join-Path $repoRoot 'docs\M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md'
$project=Join-Path $repoRoot 'docs\PROJECT.md'
$docsReadme=Join-Path $repoRoot 'docs\README.md'
Require-Text $roadmap 'The live gate is `RP1C-ENGINEERING-REPAIR-SELECTION1`'
Require-Text $detailed 'The next and only authorized activity is decision-only `RP1C-ENGINEERING-REPAIR-SELECTION1`.'
Require-Text $project 'The only live gate is now decision-only `RP1C-ENGINEERING-REPAIR-SELECTION1`.'
Require-Text $docsReadme 'The only live gate is decision-only `RP1C-ENGINEERING-REPAIR-SELECTION1`.'

$artifactDir=Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-selection1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$utf8=New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $true
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-selection-decision-and-provenance.txt'), @(
 'status=PASS-RP1C-SELECTION-EVIDENCE-COMPLETE',
 'gate=RP1C-ENGINEERING-REPAIR-SELECTION1',
 'selection-result=SELECT-C4',
 'selection-mode=AUTHORED-ENGINEERING-DECISION',
 'automatic-selection=False',
 'select-none-preserved=True',
 'select-none-considered=True',
 'selected-candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
 'selected-runtime-profile=AMBIENT-UNSET',
 'selection-planning1-returned=PASS-AS-AUTHORED',
 'fdpc2-returned-adjudication=PASS',
 'new-measurement-performed=False',
 'candidate-mutation-performed=False',
 'r1-implementation-planning-authorized-now=False',
 'production-repair-authorized=False'
),$utf8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-selection-readiness-matrix.csv'), @(
 'candidate_id,runtime_profile,selection_ready,selected,blocker_or_basis',
 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE,AMBIENT-UNSET,True,True,FDPC2-FIVE-OF-FIVE-PASS-CORRECTED-PREDICATE',
 'D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR,N/A,False,False,SEAM-MAX-16504.9-US-EXCEEDS-409.30666666666673-US',
 'SELECT-NONE,N/A,N/A,False,PRESERVED-MANDATORY-ALTERNATIVE-CONSIDERED-NOT-SELECTED'
),$utf8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-selection-decision-summary.txt'), @(
 'status=PASS-RP1C-SELECTION-DECISION-RECORDED',
 'selection-result=SELECT-C4',
 'selection-ready-count=1',
 'select-none-mandatory=True',
 'select-none-selected=False',
 'automatic-selection=False',
 'decision-basis=C4-ONLY-SELECTION-READY-AFTER-GREEN-FDPC2-D3-BLOCKED',
 'post-selection-successor=R1-IMPLEMENTATION-PLANNING-ONLY',
 'returned-selection-adjudication-required=True',
 'r1-implementation-planning-authorized-now=False',
 'production-repair-authorized=False',
 'next-action=RETURN-COMPLETE-SELECTION-ARTIFACTS-FOR-ADJUDICATION'
),$utf8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), @(
 'status=STATIC-SELECTION-REVIEW-PASS',
 'finding-1=SELECTION-PLANNING1-RETURNED-PASS-AS-AUTHORED',
 'finding-2=C4-AMBIENT-SELECTION-READY',
 'finding-3=D3-NOT-SELECTION-READY',
 'finding-4=SELECT-NONE-PRESERVED-AND-CONSIDERED',
 'finding-5=SELECT-C4-IS-AUTHORED-NOT-AUTOMATIC',
 'finding-6=NO-NEW-MEASUREMENT-OR-MUTATION',
 'selection-result=SELECT-C4',
 'r1-implementation-planning-authorized-now=False',
 'production-repair-authorized=False'
),$utf8)
$files=@(Get-ChildItem -LiteralPath $artifactDir -File)
Require ($files.Count -eq 4) 'RP1C Selection 1 must write exactly four artifacts.'
Write-Host 'RP1C Engineering Repair Selection 1 static decision audit: PASS' -ForegroundColor Green
Write-Host 'Selection result: SELECT-C4' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f $artifactDir)
