$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r,[string]$exclude=''){$rr=(Resolve-Path $r).Path;$list=New-Object 'Collections.Generic.List[string]';Get-ChildItem $rr -Recurse -File|ForEach-Object{$rel=$_.FullName.Substring($rr.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj' -or $rel -eq $exclude){return};$list.Add($rel)};$a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $a){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$s=[Security.Cryptography.SHA256]::Create();try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()};@{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
function ReadUtf8([string]$p){[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)}
function Has([string]$p,[string]$needle){(ReadUtf8 $p).Contains($needle)}
function AddMissingMarker([Collections.Generic.List[string]]$list,[string]$path,[string]$marker){if(-not (Has $path $marker)){$list.Add(($path+': '+$marker))}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-contract.json' -Raw|ConvertFrom-Json
$H=Get-Content 'eng\m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-validator-hotfix1-contract.json' -Raw|ConvertFrom-Json
$H2=Get-Content 'eng\m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-validator-hotfix2-contract.json' -Raw|ConvertFrom-Json
Req ($H.schema -eq 'm10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-validator-hotfix1-v1') 'validator hotfix schema drift'
Req ($H.defect -eq 'MARKDOWN-FORMAT-COUPLED-HOSTED-CI-HOLD-MARKER' -and $H.scope -eq 'VALIDATOR-ONLY') 'validator hotfix identity drift'
Req (-not [bool]$H.engineering_review_change -and -not [bool]$H.rev1_guard_change -and -not [bool]$H.authority_change) 'validator hotfix authority drift'
Req ($H.next_authorized_activity -eq 'IMPLEMENT-DIAGNOSTIC3-REV1-TEST-ONLY') 'validator hotfix next activity drift'
Req ($H2.schema -eq 'm10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-validator-hotfix2-v1') 'validator hotfix2 schema drift'
Req ($H2.defect -eq 'PROSE-AND-ENCODING-COUPLED-DOCUMENT-VALIDATION') 'validator hotfix2 identity drift'
Req ($H2.scope -eq 'VALIDATOR-AND-DOCUMENT-MARKER-HYGIENE-ONLY') 'validator hotfix2 scope drift'
Req (-not [bool]$H2.engineering_review_change -and -not [bool]$H2.rev1_guard_change -and -not [bool]$H2.authority_change) 'validator hotfix2 authority drift'
Req ([bool]$H2.aggregate_missing_marker_reporting) 'validator hotfix2 aggregate-reporting drift'
Req ($H2.next_authorized_activity -eq 'IMPLEMENT-DIAGNOSTIC3-REV1-TEST-ONLY') 'validator hotfix2 next activity drift'
Req ($C.schema -eq 'm10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-v1') 'planning schema drift'
Req ($C.status -eq 'PASS-WITH-PREEXECUTION-REVISION-PLANNED') 'planning status drift'
Req ($C.review_disposition -eq 'PASS-WITH-PREEXECUTION-REVISION') 'review disposition drift'
$src=Tree 'src'
Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift'
$reviewedPath='tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Tests.cs'
$hist=Tree 'tests' $reviewedPath.Substring('tests/'.Length)
Req ($hist.Count -eq [int]$C.baseline.historical_tests_file_count_excluding_reviewed_diagnostic3 -and $hist.Hash -eq [string]$C.baseline.historical_tests_tree_sha256_excluding_reviewed_diagnostic3) 'historical tests drift'
$Old=Get-Content 'eng\m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-contract.json' -Raw|ConvertFrom-Json
foreach($n in @('test','document','returned_adjudication_document','adjudicator','runner','validator')){$e=$Old.files.$n;$path=Join-Path $Root ([string]$e.path).Replace('/','\');Req ((NSha $path)-eq [string]$e.normalized_sha256) ('reviewed Diagnostic 3 drift: '+$n)}
Req ($C.review_findings.forward_inverse_architectural_seam -eq 'CONFIRMED') 'architectural-seam review drift'
Req ($C.review_findings.causal_wording -eq 'NEEDS-REVISION') 'causal wording finding drift'
Req ($C.review_findings.roadmap_consistency -eq 'NEEDS-REVISION') 'roadmap finding drift'
Req ($C.review_findings.independent_counterfactual -eq 'REQUIRED-BEFORE-CAUSAL-CLOSURE') 'counterfactual requirement drift'
Req (-not [bool]$C.rev1.production_change_allowed -and -not [bool]$C.rev1.historical_test_semantic_change_allowed) 'REV1 scope drift'
Req (-not [bool]$C.rev1.if97_production_runtime_dependency_allowed -and -not [bool]$C.rev1.c4_payload_change_allowed) 'reference/C4 scope drift'
Req (-not [bool]$C.rev1.seed_retuning_allowed -and -not [bool]$C.rev1.threshold_change_allowed -and -not [bool]$C.rev1.canonical_exact_v9_change_allowed) 'seed/threshold/exact-v9 scope drift'
Req ([int]$C.rev1.runtime_step_ms -eq 10 -and [int]$C.rev1.seed_step_count -eq 1 -and [int]$C.rev1.expected_returned_artifact_count -eq 7) 'REV1 evidence contract drift'
Req ([double]$C.rev1.guards.if97_saturation_pressure_abs_delta_ceiling_pa -eq 1.0) 'IF97 pressure guard drift'
Req ([double]$C.rev1.guards.if97_reference_vs_mode2_suction_transport_abs_delta_ceiling_j_kg -eq 0.001) 'IF97 transport guard drift'
Req ([double]$C.rev1.guards.if97_counterfactual_net_energy_abs_ceiling_w -eq 0.01) 'IF97 counterfactual rate guard drift'
Req ($C.ci.deterministic_ordinary_local -eq 'PASS' -and $C.ci.hosted_ordinary -eq 'PENDING-CONFIRMATION') 'CI checkpoint drift'
Req ([bool]$C.ci.test_only_evidence_may_proceed_while_hosted_pending) 'test-only evidence rule drift'
Req ([bool]$C.ci.production_repair_requires_hosted_green) 'hosted repair hold drift'
Req ($C.authority.next_authorized_activity -eq 'IMPLEMENT-DIAGNOSTIC3-REV1-TEST-ONLY') 'next activity drift'
Req (-not [bool]$C.authority.production_repair_authorized -and -not [bool]$C.authority.repair_planning_authorized) 'repair authority drift'
Req (-not [bool]$C.authority.r3_passed -and -not [bool]$C.authority.r3_requalification3_authorized -and -not [bool]$C.authority.r4_planning_authorized) 'R3/R4 authority drift'
$Plan='docs\M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md'
Req (Test-Path $Plan) 'review/plan document missing'
Req (Test-Path 'docs\M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1_VALIDATOR_HOTFIX1.md') 'planning validator Hotfix 1 record missing'
Req (Test-Path 'docs\M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1_VALIDATOR_HOTFIX2.md') 'planning validator Hotfix 2 record missing'
Req (Test-Path 'docs\VALIDATOR_AUTHORING_RULES.md') 'validator authoring rules missing'

# Machine semantics come from the JSON contract. Human documents expose only stable ASCII marker IDs.
$missing=New-Object 'Collections.Generic.List[string]'
foreach($m in @($H2.markers.plan)){AddMissingMarker $missing $Plan ([string]$m)}
foreach($m in @($H2.markers.project)){AddMissingMarker $missing 'docs\PROJECT.md' ([string]$m)}
foreach($m in @($H2.markers.roadmap)){AddMissingMarker $missing 'docs\ROADMAP.md' ([string]$m)}
if($missing.Count -gt 0){throw ("documentation marker contract failed; missing:`n - "+($missing -join "`n - "))}

# Stable technical identifiers are safe literal checks; editorial prose is not.
foreach($m in @(
'PASS-WITH-PREEXECUTION-REVISION',
'IMPLEMENT-DIAGNOSTIC3-REV1-TEST-ONLY',
'CAUSAL-CLOSURE-CONFIRMED',
'TRANSPORT-SEAM-LOCALIZED-COUNTERFACTUAL-NOT-CLOSED',
'IapwsIf97Reference'
)){Req (Has $Plan $m) ('review/plan technical identifier missing: '+$m)}
Req (Has 'docs\TOP_LEVEL_DOCUMENT_INDEX.md' 'M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md') 'top-level index missing review plan'
Req (Has 'docs\README.md' 'M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md') 'documentation navigation missing review plan'
Req (Has 'docs\TOP_LEVEL_DOCUMENT_INDEX.md' 'VALIDATOR_AUTHORING_RULES.md') 'top-level index missing validator rules'
Req (Has 'docs\README.md' 'VALIDATOR_AUTHORING_RULES.md') 'documentation navigation missing validator rules'

# Windows PowerShell 5.1 hygiene: this validator must remain ASCII-only or gain an explicit UTF-8 BOM.
$selfBytes=[IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
$hasUtf8Bom=($selfBytes.Length -ge 3 -and $selfBytes[0] -eq 0xEF -and $selfBytes[1] -eq 0xBB -and $selfBytes[2] -eq 0xBF)
$hasNonAscii=$false
foreach($b in $selfBytes){if($b -gt 0x7F){$hasNonAscii=$true;break}}
Req ($hasUtf8Bom -or -not $hasNonAscii) 'VALIDATOR-ENCODING-POLICY: validator must be ASCII-only or UTF-8 with BOM'
$plannedApp='tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.cs'
$plannedRef='tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactualTests.cs'
Req (-not (Test-Path $plannedApp)) 'REV1 Application test already exists in planning-only package'
Req (-not (Test-Path $plannedRef)) 'REV1 reference test already exists in planning-only package'
Write-Host 'M10 Final VR2 R3 Diagnostic 3 Deep Review & REV1 Planning 1: PASS-AS-AUTHORED' -ForegroundColor Green
