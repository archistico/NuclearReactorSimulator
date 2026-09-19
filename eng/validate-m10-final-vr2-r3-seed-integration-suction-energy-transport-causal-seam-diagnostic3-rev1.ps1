$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$rp=(Resolve-Path $p).Path;$stream=[IO.File]::OpenRead($rp);$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash($stream))).Replace('-','').ToUpperInvariant()}finally{$stream.Dispose();$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r,[string[]]$excludes=@()){$rr=(Resolve-Path $r).Path;$list=New-Object 'Collections.Generic.List[string]';Get-ChildItem $rr -Recurse -File|ForEach-Object{$rel=$_.FullName.Substring($rr.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj' -or $excludes -contains $rel){return};$list.Add($rel)};$a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $a){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$s=[Security.Cryptography.SHA256]::Create();try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()};@{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
function ReadUtf8([string]$p){[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)}
function Has([string]$p,[string]$needle){(ReadUtf8 $p).Contains($needle)}
function AddMarkerIssue([Collections.Generic.List[string]]$list,[string]$path,[string]$marker){$text=ReadUtf8 $path;$count=([regex]::Matches($text,[regex]::Escape($marker))).Count;if($count -ne 1){$list.Add(($path+': '+$marker+' expected exactly once; found '+$count))}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1-contract.json' -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1-v1') 'REV1 schema drift'
Req ($C.status -eq 'IMPLEMENTED-NOT-EXECUTED') 'REV1 status drift'

$P=$C.prerequisite.planning_contract
Req ((Sha ([string]$P.path)) -eq [string]$P.sha256) 'planning contract drift'
$Plan=Get-Content ([string]$P.path) -Raw|ConvertFrom-Json
Req ($Plan.status -eq [string]$P.required_status) 'planning status drift'
Req ($Plan.authority.next_authorized_activity -eq [string]$P.required_next_authority) 'planning authority drift'
$PH=$C.prerequisite.planning_validator_hotfix2_contract
Req ((Sha ([string]$PH.path)) -eq [string]$PH.sha256) 'planning validator Hotfix 2 contract drift'
$AM=$C.prerequisite.preexecution_audit_planning_amendment1_contract
Req ((Sha ([string]$AM.path)) -eq [string]$AM.sha256) 'REV1 preexecution Amendment 1 contract drift'
$AMD=Get-Content ([string]$AM.path) -Raw|ConvertFrom-Json
Req ($AMD.status -eq [string]$AM.required_status) 'REV1 preexecution Amendment 1 status drift'
Req ($AMD.authority.next_authorized_activity -eq [string]$AM.required_next_authority) 'REV1 preexecution Amendment 1 authority drift'
$VH=$C.prerequisite.validator_contract_hygiene_hotfix1_contract
Req ((Sha ([string]$VH.path)) -eq [string]$VH.sha256) 'REV1 validator contract-hygiene Hotfix 1 contract drift'
$VHD=Get-Content ([string]$VH.path) -Raw|ConvertFrom-Json
Req ($VHD.status -eq [string]$VH.required_status) 'REV1 validator contract-hygiene Hotfix 1 status drift'
Req ($VHD.authority.next_authorized_activity -eq [string]$VH.required_next_authority) 'REV1 validator contract-hygiene Hotfix 1 authority drift'
Req ($VHD.scope -eq 'ACTIVE-REV1-VALIDATOR-DOCUMENT-CONTRACT-HYGIENE-ONLY') 'REV1 validator contract-hygiene Hotfix 1 scope drift'
Req (-not [bool]$VHD.invariants.production_change -and -not [bool]$VHD.invariants.guard_change -and -not [bool]$VHD.invariants.authority_change) 'REV1 validator contract-hygiene Hotfix 1 invariant drift'
$OldRef=$C.prerequisite.reviewed_diagnostic3_contract
Req ((Sha ([string]$OldRef.path)) -eq [string]$OldRef.sha256) 'reviewed Diagnostic 3 contract drift'
$Old=Get-Content ([string]$OldRef.path) -Raw|ConvertFrom-Json
foreach($n in @('test','document','returned_adjudication_document','adjudicator','runner','validator')){$e=$Old.files.$n;$path=Join-Path $Root ([string]$e.path).Replace('/','\');Req ((NSha $path)-eq [string]$e.normalized_sha256) ('reviewed Diagnostic 3 drift: '+$n)}

$D2=[string]$C.prerequisite.returned_diagnostic2_directory
foreach($f in $C.prerequisite.returned_diagnostic2_artifacts.PSObject.Properties){$path=Join-Path $Root (($D2+'/'+$f.Name).Replace('/','\'));Req ((Sha $path)-eq [string]$f.Value) ('Diagnostic 2 frozen artifact drift: '+$f.Name)}

$src=Tree 'src'
Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift'
$exclude=@(
'NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Tests.cs',
'NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.cs',
'NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactualTests.cs'
)
$hist=Tree 'tests' $exclude
Req ($hist.Count -eq [int]$C.baseline.historical_tests_file_count_excluding_reviewed_diagnostic3_and_rev1 -and $hist.Hash -eq [string]$C.baseline.historical_tests_tree_sha256_excluding_reviewed_diagnostic3_and_rev1) 'historical tests drift'
Req ((Sha 'eng\ci-ordinary.cmd') -eq [string]$C.baseline.ordinary_ci_cmd_sha256) 'ordinary CI command drift'
Req ((Sha '.github\workflows\ordinary-ci.yml') -eq [string]$C.baseline.ordinary_ci_workflow_sha256) 'ordinary CI workflow drift'

foreach($n in @('application_test','simulation_reference_test','document','adjudicator','runner','validator')){$e=$C.files.$n;$path=Join-Path $Root ([string]$e.path).Replace('/','\');Req (Test-Path $path) ('REV1 file missing: '+$n);Req ((NSha $path)-eq [string]$e.normalized_sha256) ('REV1 file drift: '+$n)}

Req ([int]$C.diagnostic.runtime_step_ms -eq 10 -and [int]$C.diagnostic.seed_step_count -eq 1) 'runtime checkpoint drift'
Req ([int]$C.diagnostic.expected_returned_artifact_count -eq 7) 'artifact-count drift'
Req (-not [bool]$C.diagnostic.production_change_allowed -and -not [bool]$C.diagnostic.historical_test_semantic_change_allowed) 'diagnostic scope drift'
Req (-not [bool]$C.diagnostic.if97_production_runtime_dependency_allowed -and -not [bool]$C.diagnostic.c4_payload_change_allowed) 'reference/C4 scope drift'
Req (-not [bool]$C.diagnostic.seed_retuning_allowed -and -not [bool]$C.diagnostic.threshold_change_allowed -and -not [bool]$C.diagnostic.canonical_exact_v9_change_allowed) 'seed/threshold/exact-v9 scope drift'
Req ([double]$C.diagnostic.guards.if97_saturation_pressure_abs_delta_ceiling_pa -eq 1.0) 'IF97 pressure guard drift'
Req ([double]$C.diagnostic.guards.if97_reference_vs_mode2_suction_transport_abs_delta_ceiling_j_kg -eq 0.001) 'IF97 transport guard drift'
Req ([double]$C.diagnostic.guards.superseded_fixed_counterfactual_net_energy_abs_ceiling_w -eq 0.01) 'superseded IF97 counterfactual guard provenance drift'
Req ($C.diagnostic.guards.counterfactual_net_budget_mode -eq 'DERIVED-FROM-COMPONENT-GUARDS') 'counterfactual budget mode drift'
Req ([double]$C.diagnostic.guards.counterfactual_rate_identity_abs_residual_ceiling_w -eq 0.000001) 'counterfactual rate identity guard drift'
Req ([double]$C.diagnostic.guards.counterfactual_budget_roundoff_w -eq 0.001) 'counterfactual roundoff budget drift'
Req (@($C.diagnostic.adjudication_classes).Count -eq 4) 'adjudication-class cardinality drift'
foreach($class in @('CAUSAL-CLOSURE-CONFIRMED','TRANSPORT-SEAM-LOCALIZED-COUNTERFACTUAL-NOT-CLOSED','DIAGNOSTIC-IDENTITY-RED','REFERENCE-COUNTERFACTUAL-RED')){Req (@($C.diagnostic.adjudication_classes) -contains $class) ('adjudication class missing: '+$class)}
Req ($C.ci.deterministic_ordinary_local -eq 'PASS' -and $C.ci.hosted_ordinary -eq 'PENDING-CONFIRMATION') 'CI checkpoint drift'
Req ([bool]$C.ci.test_only_evidence_may_proceed_while_hosted_pending) 'test-only evidence rule drift'
Req ([bool]$C.ci.production_repair_planning_requires_hosted_green) 'hosted CI repair hold drift'
Req ($C.authority.next_authorized_activity -eq 'EXECUTE-DIAGNOSTIC3-REV1-TEST-ONLY') 'next activity drift'
Req (-not [bool]$C.authority.production_repair_authorized -and -not [bool]$C.authority.repair_planning_authorized) 'repair authority drift'
Req (-not [bool]$C.authority.seed_retuning_authorized -and -not [bool]$C.authority.threshold_change_authorized -and -not [bool]$C.authority.c4_change_authorized) 'repair-scope authority drift'
Req (-not [bool]$C.authority.canonical_exact_v9_change_authorized -and -not [bool]$C.authority.r3_passed -and -not [bool]$C.authority.r3_requalification3_authorized -and -not [bool]$C.authority.r4_planning_authorized) 'R3/R4 authority drift'

Req ($C.powershell_compatibility.hash_backend -eq 'DOTNET-SHA256-STREAM') 'PowerShell hash backend drift'
Req (-not [bool]$C.powershell_compatibility.get_file_hash_dependency_allowed) 'PowerShell hash dependency policy drift'
$legacyHashCmd='Get-'+'FileHash'
Req (-not (Has $MyInvocation.MyCommand.Path $legacyHashCmd)) 'validator uses unsupported hash cmdlet'
Req (-not (Has ([string]$C.files.adjudicator.path) $legacyHashCmd)) 'adjudicator uses unsupported hash cmdlet'

$app=[string]$C.files.application_test.path
$ref=[string]$C.files.simulation_reference_test.path
Req (-not (Has $app 'IapwsIf97Reference')) 'Application runtime test must not use IapwsIf97Reference'
Req (Has $ref 'IapwsIf97Reference') 'Simulation reference test must use IapwsIf97Reference'
$prodIf97=Get-ChildItem 'src' -Recurse -File -Filter '*.cs'|Select-String -SimpleMatch 'IapwsIf97Reference'
Req (@($prodIf97).Count -eq 0) 'IapwsIf97Reference leaked into production src'

Req ($C.document_validation_policy.mode -eq 'JSON-DECLARED-ASCII-MARKERS-ONLY') 'document validation policy drift'
Req ($C.document_validation_policy.marker_cardinality -eq 'EXACTLY-ONE') 'document marker cardinality policy drift'
Req (-not [bool]$C.document_validation_policy.direct_markdown_technical_identifier_presence_checks_allowed) 'direct Markdown identifier checks unexpectedly authorized'
Req (-not [bool]$C.document_validation_policy.navigation_filename_literal_presence_checks_allowed) 'navigation filename literal checks unexpectedly authorized'
$markerIssues=New-Object 'Collections.Generic.List[string]'
foreach($entry in @($C.documentation_marker_files)){
    $path=[string]$entry.path
    Req (Test-Path $path) ('documentation marker file missing: '+$path)
    foreach($m in @($entry.markers)){AddMarkerIssue $markerIssues $path ([string]$m)}
}
if($markerIssues.Count -gt 0){throw ("documentation marker contract failed:`n - "+($markerIssues -join "`n - "))}

$A=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1'
foreach($f in @($C.diagnostic.artifact_names)){Req (-not (Test-Path (Join-Path $A ([string]$f)))) ('pre-existing Diagnostic 3 REV1 artifact: '+[string]$f)}

$selfBytes=[IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
$hasUtf8Bom=($selfBytes.Length -ge 3 -and $selfBytes[0] -eq 0xEF -and $selfBytes[1] -eq 0xBB -and $selfBytes[2] -eq 0xBF)
$hasNonAscii=$false
foreach($b in $selfBytes){if($b -gt 0x7F){$hasNonAscii=$true;break}}
Req ($hasUtf8Bom -or -not $hasNonAscii) 'VALIDATOR-ENCODING-POLICY: validator must be ASCII-only or UTF-8 with BOM'
$adjBytes=[IO.File]::ReadAllBytes((Resolve-Path ([string]$C.files.adjudicator.path)).Path)
$adjHasUtf8Bom=($adjBytes.Length -ge 3 -and $adjBytes[0] -eq 0xEF -and $adjBytes[1] -eq 0xBB -and $adjBytes[2] -eq 0xBF)
$adjHasNonAscii=$false
foreach($b in $adjBytes){if($b -gt 0x7F){$adjHasNonAscii=$true;break}}
Req ($adjHasUtf8Bom -or -not $adjHasNonAscii) 'ADJUDICATOR-ENCODING-POLICY: adjudicator must be ASCII-only or UTF-8 with BOM'
Write-Host 'R3 Suction Energy-Transport Causal-Seam Diagnostic 3 REV1 static audit: PASS' -ForegroundColor Green
