$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$rp=(Resolve-Path $p).Path;$stream=[IO.File]::OpenRead($rp);$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash($stream))).Replace('-','').ToUpperInvariant()}finally{$stream.Dispose();$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r,[string[]]$excludes=@()){$rr=(Resolve-Path $r).Path;$list=New-Object 'Collections.Generic.List[string]';Get-ChildItem $rr -Recurse -File|ForEach-Object{$rel=$_.FullName.Substring($rr.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj' -or $excludes -contains $rel){return};$list.Add($rel)};$a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $a){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$s=[Security.Cryptography.SHA256]::Create();try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()};@{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
function ReadUtf8([string]$p){[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)}
function AddMarkerIssue([Collections.Generic.List[string]]$list,[string]$path,[string]$marker){$text=ReadUtf8 $path;$count=([regex]::Matches($text,[regex]::Escape($marker))).Count;if($count -ne 1){$list.Add(($path+': '+$marker+' expected exactly once; found '+$count))}}
function D([string]$s){[double]::Parse($s,[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture)}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-diagnostic3-rev1-returned-evidence-adjudication1-contract.json' -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-diagnostic3-rev1-returned-evidence-adjudication1-v1') 'schema drift'
Req ($C.status -eq 'PASS-AS-AUTHORED-READY-FOR-AUDIT') 'status drift'
Req ($C.engineering_classification -eq 'CAUSAL-CLOSURE-CONFIRMED') 'classification drift'
Req ($C.findings.localized_seam -eq 'STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT') 'localized seam drift'
Req ($C.findings.repair_owner -eq 'UNSELECTED') 'repair owner drift'

$src=Tree 'src'
Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift'
$exclude=@(
'NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Tests.cs',
'NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.cs',
'NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactualTests.cs'
)
$hist=Tree 'tests' $exclude
Req ($hist.Count -eq [int]$C.baseline.historical_tests_file_count -and $hist.Hash -eq [string]$C.baseline.historical_tests_tree_sha256) 'historical tests drift'
Req ((Sha 'eng\ci-ordinary.cmd') -eq [string]$C.baseline.ordinary_ci_cmd_sha256) 'ordinary CI command drift'
Req ((Sha '.github\workflows\ordinary-ci.yml') -eq [string]$C.baseline.ordinary_ci_workflow_sha256) 'ordinary CI workflow drift'

$F=[string]$C.returned_evidence.directory
Req (Test-Path $F) 'returned evidence directory missing'
foreach($p in $C.returned_evidence.files.PSObject.Properties){
    $path=Join-Path $Root (($F+'/'+$p.Name).Replace('/','\'))
    Req (Test-Path $path) ('returned artifact missing: '+$p.Name)
    Req ((Sha $path) -eq [string]$p.Value) ('returned artifact hash drift: '+$p.Name)
}

$balance=@(Import-Csv (Join-Path $F '01-step1-suction-energy-balance.csv'))
$paths=@(Import-Csv (Join-Path $F '02-mode2-suction-inverse-path.csv'))
$transport=@(Import-Csv (Join-Path $F '03-forward-transport-decomposition.csv'))
$reference=@(Import-Csv (Join-Path $F '05-if97-reference-counterfactual.csv'))
Req ($balance.Count -eq 2 -and $paths.Count -eq 2 -and $transport.Count -eq 1 -and $reference.Count -eq 1) 'returned artifact cardinality drift'
$cand=@($balance|Where-Object case -eq 'candidate-mode2')
$raw=@($paths|Where-Object checkpoint -eq 'raw')
$step1=@($paths|Where-Object checkpoint -eq 'seed-step1')
Req ($cand.Count -eq 1 -and $raw.Count -eq 1 -and $step1.Count -eq 1) 'returned evidence identity drift'
$cand=$cand[0];$raw=$raw[0];$step1=$step1[0];$t=$transport[0];$r=$reference[0]
Req ([Math]::Abs((D $cand.predicted_energy_residual_w)) -le [double]$C.guards.production_energy_identity_abs_residual_ceiling_w) 'production energy identity RED'
Req ((D $cand.observed_mass_rate_kg_s) -eq 0.0) 'candidate mass balance RED'
Req ([string]$raw.resolved_phase -eq 'SubcooledLiquid') 'raw phase drift'
Req ([string]$step1.resolved_phase -eq 'SaturatedMixture') 'step1 phase drift'
Req ([string]$t.forward_properties_all_bits_equal -eq 'true') 'forward provider bit identity RED'
Req ([Math]::Abs((D $t.flow_work_identity_residual_j_kg)) -le [double]$C.guards.transport_identity_abs_residual_ceiling_j_kg) 'flow work identity RED'
Req ([Math]::Abs((D $t.enthalpy_identity_residual_j_kg)) -le [double]$C.guards.transport_identity_abs_residual_ceiling_j_kg) 'enthalpy identity RED'
Req ([Math]::Abs((D $t.selected_transport_identity_residual_j_kg)) -le [double]$C.guards.transport_identity_abs_residual_ceiling_j_kg) 'selected transport identity RED'
Req ([Math]::Abs((D $t.liquid_energy_rate_identity_residual_w)) -le [double]$C.guards.production_energy_identity_abs_residual_ceiling_w) 'liquid energy rate identity RED'
Req ([string]$r.reference_calculation_valid -eq 'true') 'IF97 reference invalid'
Req ([Math]::Abs((D $r.saturation_pressure_delta_pa)) -le [double]$C.guards.if97_saturation_pressure_abs_delta_ceiling_pa) 'IF97 saturation pressure RED'
Req ([Math]::Abs((D $r.if97_minus_mode2_suction_transport_j_kg)) -le [double]$C.guards.if97_reference_vs_mode2_suction_transport_abs_delta_ceiling_j_kg) 'IF97 vs mode2 suction transport RED'
$refGap=D $r.if97_minus_production_transport_j_kg
Req ($refGap -ge [double]$C.guards.production_transport_gap_min_j_kg -and $refGap -le [double]$C.guards.production_transport_gap_max_j_kg) 'IF97 vs production transport gap RED'
$referenceTransport=D $r.if97_region1_transport_j_kg
$suctionTransport=D $r.mode2_raw_suction_transport_j_kg
$recirc=D $r.recirculation_flow_kg_s
$pump=D $r.pump_flow_kg_s
$net=D $r.counterfactual_net_energy_rate_w
$identityExpected=(($referenceTransport-$suctionTransport)*$recirc)+($suctionTransport*($recirc-$pump))
$identityResidual=[Math]::Abs($net-$identityExpected)
Req ($identityResidual -le [double]$C.guards.counterfactual_rate_identity_abs_residual_ceiling_w) 'counterfactual rate identity RED'
$budget=([double]$C.guards.if97_reference_vs_mode2_suction_transport_abs_delta_ceiling_j_kg*[Math]::Abs($recirc))+([double]$C.guards.mass_identity_abs_residual_ceiling_kg_s*[Math]::Max([Math]::Abs($referenceTransport),[Math]::Abs($suctionTransport)))+[double]$C.guards.counterfactual_budget_roundoff_w
Req ([Math]::Abs($net) -le $budget) 'counterfactual net energy RED'

Req ($C.ci.deterministic_ordinary_local -eq 'PASS') 'local ordinary CI state drift'
Req ($C.ci.hosted_ordinary -eq 'PENDING-CONFIRMATION') 'hosted ordinary CI state drift'
Req ($C.authority.next_authorized_activity -eq 'CONFIRM-HOSTED-ORDINARY-CI-GREEN') 'next authority drift'
Req (-not [bool]$C.authority.production_repair_authorized -and -not [bool]$C.authority.repair_planning_authorized) 'repair authority drift'
Req (-not [bool]$C.authority.seed_retuning_authorized -and -not [bool]$C.authority.threshold_change_authorized -and -not [bool]$C.authority.c4_change_authorized) 'scope authority drift'
Req (-not [bool]$C.authority.canonical_exact_v9_change_authorized -and -not [bool]$C.authority.r3_passed -and -not [bool]$C.authority.r3_requalification3_authorized -and -not [bool]$C.authority.r4_planning_authorized) 'R3/R4 authority drift'

foreach($n in @('document','validator','runner')){
    $e=$C.files.$n
    Req (Test-Path ([string]$e.path)) ('file missing: '+$n)
    Req ((NSha ([string]$e.path)) -eq [string]$e.normalized_sha256) ('file drift: '+$n)
}
$markerIssues=New-Object 'Collections.Generic.List[string]'
foreach($entry in @($C.documentation_marker_files)){
    $path=[string]$entry.path
    Req (Test-Path $path) ('documentation marker file missing: '+$path)
    foreach($m in @($entry.markers)){AddMarkerIssue $markerIssues $path ([string]$m)}
}
if($markerIssues.Count -gt 0){throw ("documentation marker contract failed:`n - "+($markerIssues -join "`n - "))}

$selfBytes=[IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
$hasUtf8Bom=($selfBytes.Length -ge 3 -and $selfBytes[0] -eq 0xEF -and $selfBytes[1] -eq 0xBB -and $selfBytes[2] -eq 0xBF)
$hasNonAscii=$false
foreach($b in $selfBytes){if($b -gt 0x7F){$hasNonAscii=$true;break}}
Req ($hasUtf8Bom -or -not $hasNonAscii) 'VALIDATOR-ENCODING-POLICY'
Write-Host 'M10 Final VR2 R3 Diagnostic 3 REV1 Returned-Evidence Adjudication 1: PASS-AS-AUTHORED' -ForegroundColor Green
