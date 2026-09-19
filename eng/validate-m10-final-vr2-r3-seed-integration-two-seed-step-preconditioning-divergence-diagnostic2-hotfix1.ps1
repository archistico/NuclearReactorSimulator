$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([IO.File]::ReadAllBytes($p)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$BaseValidator=Join-Path $Root 'eng\validate-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2.ps1'
& $BaseValidator
$C=Get-Content 'eng\m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-hotfix1-contract.json' -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-hotfix1-v1') 'hotfix schema drift'
Req ($C.status -eq 'RETURNED-EVIDENCE-ADJUDICATOR-HOTFIX1') 'hotfix status drift'
Req ((Sha (Join-Path $Root ([string]$C.original_candidate.contract_path).Replace('/','\')))-eq [string]$C.original_candidate.contract_sha256) 'original Diagnostic 2 contract drift'
Req ((NSha (Join-Path $Root ([string]$C.original_candidate.adjudicator_path).Replace('/','\')))-eq [string]$C.original_candidate.adjudicator_normalized_sha256) 'original Diagnostic 2 adjudicator drift'
$Droot=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2'
foreach($p in $C.returned_evidence.PSObject.Properties){
    $path=Join-Path $Droot ([string]$p.Value.file)
    Req (Test-Path $path -PathType Leaf) ("returned evidence missing: "+$p.Name)
    Req ((Sha $path)-eq [string]$p.Value.sha256) ("returned evidence drift: "+$p.Name)
}
Req ((NSha (Join-Path $Root ([string]$C.hotfix.adjudicator_path).Replace('/','\')))-eq [string]$C.hotfix.adjudicator_normalized_sha256) 'Hotfix 1 adjudicator drift'
Req ((NSha (Join-Path $Root ([string]$C.hotfix.document_path).Replace('/','\')))-eq [string]$C.hotfix.document_normalized_sha256) 'Hotfix 1 document drift'
Req ((NSha (Join-Path $Root ([string]$C.hotfix.runner_path).Replace('/','\')))-eq [string]$C.hotfix.runner_normalized_sha256) 'Hotfix 1 runner drift'
Req ($C.failure.stage -eq '[4/4] Evidence adjudication') 'failure stage drift'
Req ($C.failure.error_id -eq 'PropertyNotFoundStrict') 'failure identity drift'
Req ($C.failure.root_cause -eq 'ZERO-PIPELINE-OUTPUT-ASSIGNED-AS-NULL-UNDER-STRICTMODE') 'root-cause drift'
Req ($C.hotfix.dynamic_evidence_reexecuted -eq $false) 'dynamic rerun must remain false'
Req (-not [bool]$C.authority.production_repair_authorized) 'production repair must remain blocked'
Req (-not [bool]$C.authority.seed_retuning_authorized) 'seed retuning must remain blocked'
Req (-not [bool]$C.authority.threshold_change_authorized) 'threshold change must remain blocked'
Req (-not [bool]$C.authority.c4_change_authorized) 'C4 must remain frozen'
Req (-not [bool]$C.authority.canonical_exact_v9_change_authorized) 'canonical exact-v9 must remain frozen'
Req (-not [bool]$C.authority.r3_passed) 'R3 must remain RED'
Req (-not [bool]$C.authority.r4_planning_authorized) 'R4 must remain blocked'
Write-Host 'R3 Diagnostic 2 Adjudicator Hotfix 1 static returned-evidence audit: PASS' -ForegroundColor Green
