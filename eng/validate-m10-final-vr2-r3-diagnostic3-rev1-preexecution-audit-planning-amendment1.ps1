$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$rp=(Resolve-Path $p).Path;$stream=[IO.File]::OpenRead($rp);$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash($stream))).Replace('-','').ToUpperInvariant()}finally{$stream.Dispose();$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function ReadUtf8([string]$p){[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)}
function Has([string]$p,[string]$needle){(ReadUtf8 $p).Contains($needle)}
function D([object]$v){[double]::Parse([string]$v,[Globalization.CultureInfo]::InvariantCulture)}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$A=Get-Content 'eng\m10-final-vr2-r3-diagnostic3-rev1-preexecution-audit-planning-amendment1-contract.json' -Raw|ConvertFrom-Json
$R=Get-Content 'eng\m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1-contract.json' -Raw|ConvertFrom-Json
$P=Get-Content 'eng\m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1-contract.json' -Raw|ConvertFrom-Json
Req ($A.schema -eq 'm10-final-vr2-r3-diagnostic3-rev1-preexecution-audit-planning-amendment1-v1') 'amendment schema drift'
Req ($A.status -eq 'PASS-AS-AUTHORED-READY-FOR-AUDIT') 'amendment status drift'
Req ($P.status -eq 'PASS-WITH-PREEXECUTION-REVISION-PLANNED') 'prerequisite planning status drift'
Req ($A.scope -eq 'TEST-ONLY-HARNESS-PORTABILITY-AND-COUNTERFACTUAL-GUARD-CONSISTENCY') 'amendment scope drift'
Req ($A.findings.powershell_hashing -eq 'GET-FILEHASH-UNAVAILABLE-IN-PROVEN-USER-ENVIRONMENT') 'hashing finding drift'
Req ($A.findings.counterfactual_guard -eq 'FIXED-NET-RATE-GUARD-INCONSISTENT-WITH-SPECIFIC-ENERGY-GUARD-AT-100KGPS') 'counterfactual finding drift'
Req ($A.changes.hash_backend -eq 'DOTNET-SHA256-STREAM') 'portable hash backend drift'
Req (-not [bool]$A.changes.get_file_hash_dependency_allowed) 'unsupported hash cmdlet policy drift'
Req ([double]$A.changes.superseded_fixed_counterfactual_net_energy_abs_ceiling_w -eq 0.01) 'superseded guard provenance drift'
Req ($A.changes.counterfactual_net_budget_mode -eq 'DERIVED-FROM-COMPONENT-GUARDS') 'derived budget mode drift'
Req ([double]$A.changes.counterfactual_rate_identity_abs_residual_ceiling_w -eq 0.000001) 'rate identity guard drift'
Req ([double]$A.changes.counterfactual_budget_roundoff_w -eq 0.001) 'roundoff budget drift'
$pre=$A.analytical_preflight_from_frozen_raw_evidence
$delta=[Math]::Abs((D $pre.if97_minus_mode2_suction_transport_j_kg))
$pred=[Math]::Abs((D $pre.predicted_counterfactual_net_if_flows_equal_w))
$old=[double]$A.changes.superseded_fixed_counterfactual_net_energy_abs_ceiling_w
$budget=D $pre.derived_guard_budget_at_representative_flow_w
Req ($delta -le 0.001) 'analytical preflight violates specific-energy guard'
Req ($pred -gt $old) 'analytical preflight does not demonstrate fixed-net inconsistency'
Req ($pred -le $budget) 'analytical preflight violates derived component budget'
Req ($budget -gt 0.1 -and $budget -lt 0.11) 'derived representative budget drift'
Req ($R.diagnostic.guards.counterfactual_net_budget_mode -eq 'DERIVED-FROM-COMPONENT-GUARDS') 'REV1 contract did not adopt amended budget mode'
Req ([double]$R.diagnostic.guards.superseded_fixed_counterfactual_net_energy_abs_ceiling_w -eq 0.01) 'REV1 superseded guard provenance drift'
Req ($R.powershell_compatibility.hash_backend -eq 'DOTNET-SHA256-STREAM') 'REV1 PowerShell backend drift'
Req (-not [bool]$R.powershell_compatibility.get_file_hash_dependency_allowed) 'REV1 unsupported hash dependency drift'
$legacyHashCmd='Get-'+'FileHash'
foreach($path in @([string]$R.files.validator.path,[string]$R.files.adjudicator.path)){Req (-not (Has $path $legacyHashCmd)) ('unsupported hash cmdlet remains: '+$path)}
foreach($path in @([string]$R.files.validator.path,[string]$R.files.adjudicator.path)){$bytes=[IO.File]::ReadAllBytes((Resolve-Path $path).Path);$bom=($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF);$non=$false;foreach($b in $bytes){if($b -gt 0x7F){$non=$true;break}};Req ($bom -or -not $non) ('PowerShell encoding policy failed: '+$path)}
Req (-not [bool]$A.invariants.production_change -and -not [bool]$A.invariants.historical_test_semantic_change) 'amendment scope escaped test-only harness'
Req (-not [bool]$A.invariants.threshold_or_model_acceptance_change -and -not [bool]$A.invariants.c4_or_payload_change -and -not [bool]$A.invariants.canonical_exact_v9_change) 'model authority drift'
Req (-not [bool]$A.invariants.repair_owner_selected -and -not [bool]$A.invariants.r3_passed -and -not [bool]$A.invariants.r4_authorized) 'R3/R4 authority drift'
Req ($A.authority.next_authorized_activity -eq 'EXECUTE-DIAGNOSTIC3-REV1-TEST-ONLY') 'amendment next authority drift'
foreach($n in @('document','validator','runner')){$e=$A.files.$n;Req (Test-Path ([string]$e.path)) ('amendment file missing: '+$n);Req ((NSha ([string]$e.path)) -eq [string]$e.normalized_sha256) ('amendment file drift: '+$n)}
$doc='docs\M10_FINAL_VR2_R3_DIAGNOSTIC3_REV1_PREEXECUTION_AUDIT_PLANNING_AMENDMENT1.md'
foreach($m in @('NRS-MARKER:DIAG3-REV1-AMENDMENT1-SCOPE','NRS-MARKER:DIAG3-REV1-AMENDMENT1-GUARD','NRS-MARKER:DIAG3-REV1-AMENDMENT1-POWERSHELL','NRS-MARKER:DIAG3-REV1-AMENDMENT1-AUTHORITY')){Req (Has $doc $m) ('amendment document marker missing: '+$m)}
Req (Has 'docs\VALIDATOR_AUTHORING_RULES.md' 'Prefer runtime-portable primitives over convenience cmdlets') 'validator portability rule missing'
Write-Host 'M10 Final VR2 R3 Diagnostic 3 REV1 Preexecution Audit / Planning Amendment 1: PASS-AS-AUTHORED' -ForegroundColor Green
