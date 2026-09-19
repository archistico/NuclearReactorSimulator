$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
$Inv=[Globalization.CultureInfo]::InvariantCulture
function D([object]$v){[double]::Parse([string]$v,$Inv)}
function Sha([string]$p){$rp=(Resolve-Path $p).Path;$stream=[IO.File]::OpenRead($rp);$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash($stream))).Replace('-','').ToUpperInvariant()}finally{$stream.Dispose();$s.Dispose()}}
function AddIssue([Collections.Generic.List[string]]$list,[string]$id,[bool]$condition){if(-not $condition){$list.Add($id)}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1-contract.json' -Raw|ConvertFrom-Json
$G=$C.diagnostic.guards
$A=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1'

$balance=@(Import-Csv (Join-Path $A '01-step1-suction-energy-balance.csv'))
$paths=@(Import-Csv (Join-Path $A '02-mode2-suction-inverse-path.csv'))
$transport=@(Import-Csv (Join-Path $A '03-forward-transport-decomposition.csv'))
$referenceInput=@(Import-Csv (Join-Path $A '04-reference-counterfactual-input.csv'))
$reference=@(Import-Csv (Join-Path $A '05-if97-reference-counterfactual.csv'))

Req ($balance.Count -eq 2) 'balance row cardinality drift'
Req ($paths.Count -eq 2) 'resolver-path row cardinality drift'
Req ($transport.Count -eq 1) 'transport row cardinality drift'
Req ($referenceInput.Count -eq 1) 'reference-input row cardinality drift'
Req ($reference.Count -eq 1) 'reference row cardinality drift'

$mode1=@($balance|Where-Object case -eq 'mode1')
$cand=@($balance|Where-Object case -eq 'candidate-mode2')
$raw=@($paths|Where-Object checkpoint -eq 'raw')
$step1=@($paths|Where-Object checkpoint -eq 'seed-step1')
Req ($mode1.Count -eq 1 -and $cand.Count -eq 1) 'balance case identity drift'
Req ($raw.Count -eq 1 -and $step1.Count -eq 1) 'resolver checkpoint identity drift'
$mode1=$mode1[0]
$cand=$cand[0]
$raw=$raw[0]
$step1=$step1[0]
$t=$transport[0]
$ri=$referenceInput[0]
$r=$reference[0]

$identity=New-Object 'Collections.Generic.List[string]'
AddIssue $identity 'MODE1-CHECKPOINT-MASS' ((D $mode1.checkpoint_mass_delta_kg) -eq 0.0)
AddIssue $identity 'MODE1-CHECKPOINT-ENERGY' ((D $mode1.checkpoint_energy_delta_j) -eq 0.0)
AddIssue $identity 'MODE2-CHECKPOINT-MASS' ((D $cand.checkpoint_mass_delta_kg) -eq 0.0)
AddIssue $identity 'MODE2-CHECKPOINT-ENERGY' ((D $cand.checkpoint_energy_delta_j) -eq 0.0)
AddIssue $identity 'MODE1-MASS-RATE' ([Math]::Abs((D $mode1.observed_mass_rate_kg_s)) -le [double]$G.mass_identity_abs_residual_ceiling_kg_s)
AddIssue $identity 'MODE2-MASS-RATE' ([Math]::Abs((D $cand.observed_mass_rate_kg_s)) -le [double]$G.mass_identity_abs_residual_ceiling_kg_s)
AddIssue $identity 'MODE1-ENERGY-BASELINE' ([Math]::Abs((D $mode1.observed_energy_rate_w)) -le [double]$G.mode1_energy_abs_ceiling_w)
$obs=D $cand.observed_energy_rate_w
AddIssue $identity 'MODE2-ENERGY-LOCALIZATION' ($obs -ge [double]$G.candidate_energy_rate_min_w -and $obs -le [double]$G.candidate_energy_rate_max_w)
AddIssue $identity 'MODE2-MASS-IDENTITY' ([Math]::Abs((D $cand.predicted_mass_residual_kg_s)) -le [double]$G.mass_identity_abs_residual_ceiling_kg_s)
AddIssue $identity 'MODE2-ENERGY-IDENTITY' ([Math]::Abs((D $cand.predicted_energy_residual_w)) -le [double]$G.production_energy_identity_abs_residual_ceiling_w)
AddIssue $identity 'MODE2-RECIRCULATION-NOT-LIMITED' ([string]$cand.recirculation_inventory_limited -eq 'false')
AddIssue $identity 'MODE2-RECIRCULATION-DEMAND-MATCH' ([Math]::Abs((D $cand.requested_recirculation_flow_kg_s)-(D $cand.recirculation_flow_kg_s)) -le [double]$G.mass_identity_abs_residual_ceiling_kg_s)
$runtimeGap=D $cand.suction_minus_recirculation_specific_energy_j_kg
AddIssue $identity 'MODE2-RUNTIME-TRANSPORT-GAP' ($runtimeGap -ge [double]$G.production_transport_gap_min_j_kg -and $runtimeGap -le [double]$G.production_transport_gap_max_j_kg)
AddIssue $identity 'MODE2-RAW-RESOLVED' ([string]$raw.resolved -eq 'true')
AddIssue $identity 'MODE2-STEP1-RESOLVED' ([string]$step1.resolved -eq 'true')
AddIssue $identity 'MODE2-RAW-PHASE' ([string]$raw.resolved_phase -eq 'SubcooledLiquid')
AddIssue $identity 'MODE2-STEP1-PHASE' ([string]$step1.resolved_phase -eq 'SaturatedMixture')
AddIssue $identity 'RESOLVER-EVIDENCE-SEMANTICS' ([string]$raw.evidence_semantics -eq 'RECONSTRUCTED-FROM-EXACT-FROZEN-CONSERVED-INVENTORY' -and [string]$step1.evidence_semantics -eq 'RECONSTRUCTED-FROM-EXACT-FROZEN-CONSERVED-INVENTORY')
AddIssue $identity 'DRUM-CHECKPOINT-TEMPERATURE' ((D $t.drum_temperature_delta_k) -eq 0.0)
AddIssue $identity 'DRUM-CHECKPOINT-PRESSURE' ((D $t.drum_pressure_delta_pa) -eq 0.0)
AddIssue $identity 'DRUM-PHASE' ([string]$t.drum_phase -eq 'SaturatedMixture')
AddIssue $identity 'DRUM-TRANSPORT-MODE' ([string]$t.energy_transport_mode -eq 'SpecificEnthalpy')
AddIssue $identity 'FORWARD-PROVIDER-BIT-EQUALITY' ([string]$t.forward_properties_all_bits_equal -eq 'true')
AddIssue $identity 'FLOW-WORK-IDENTITY' ([Math]::Abs((D $t.flow_work_identity_residual_j_kg)) -le [double]$G.transport_identity_abs_residual_ceiling_j_kg)
AddIssue $identity 'ENTHALPY-IDENTITY' ([Math]::Abs((D $t.enthalpy_identity_residual_j_kg)) -le [double]$G.transport_identity_abs_residual_ceiling_j_kg)
AddIssue $identity 'SELECTED-TRANSPORT-IDENTITY' ([Math]::Abs((D $t.selected_transport_identity_residual_j_kg)) -le [double]$G.transport_identity_abs_residual_ceiling_j_kg)
AddIssue $identity 'LIQUID-ENERGY-RATE-IDENTITY' ([Math]::Abs((D $t.liquid_energy_rate_identity_residual_w)) -le [double]$G.production_energy_identity_abs_residual_ceiling_w)

$referenceSetup=New-Object 'Collections.Generic.List[string]'
$inputPath=Join-Path $A '04-reference-counterfactual-input.csv'
AddIssue $referenceSetup 'REFERENCE-INPUT-SHA256' ([string]$r.input_sha256 -eq (Sha $inputPath))
AddIssue $referenceSetup 'REFERENCE-CALCULATION-VALID' ([string]$r.reference_calculation_valid -eq 'true')
$satDelta=[Math]::Abs((D $r.saturation_pressure_delta_pa))
AddIssue $referenceSetup 'IF97-SATURATION-POINT' ($satDelta -le [double]$G.if97_saturation_pressure_abs_delta_ceiling_pa)

$counterfactual=New-Object 'Collections.Generic.List[string]'
$referenceVsSuction=[Math]::Abs((D $r.if97_minus_mode2_suction_transport_j_kg))
AddIssue $counterfactual 'IF97-VS-MODE2-SUCTION' ($referenceVsSuction -le [double]$G.if97_reference_vs_mode2_suction_transport_abs_delta_ceiling_j_kg)
$referenceTransport=D $r.if97_region1_transport_j_kg
$suctionTransport=D $r.mode2_raw_suction_transport_j_kg
$recirculationFlow=D $r.recirculation_flow_kg_s
$pumpFlow=D $r.pump_flow_kg_s
$counterSigned=D $r.counterfactual_net_energy_rate_w
$counterNet=[Math]::Abs($counterSigned)
$counterIdentityExpected=(($referenceTransport-$suctionTransport)*$recirculationFlow)+($suctionTransport*($recirculationFlow-$pumpFlow))
$counterIdentityResidual=[Math]::Abs($counterSigned-$counterIdentityExpected)
AddIssue $counterfactual 'IF97-COUNTERFACTUAL-RATE-IDENTITY' ($counterIdentityResidual -le [double]$G.counterfactual_rate_identity_abs_residual_ceiling_w)
$counterBudget=([double]$G.if97_reference_vs_mode2_suction_transport_abs_delta_ceiling_j_kg*[Math]::Abs($recirculationFlow))+([double]$G.mass_identity_abs_residual_ceiling_kg_s*[Math]::Max([Math]::Abs($referenceTransport),[Math]::Abs($suctionTransport)))+[double]$G.counterfactual_budget_roundoff_w
AddIssue $counterfactual 'IF97-COUNTERFACTUAL-NET-RATE' ($counterNet -le $counterBudget)
$referenceGap=D $r.if97_minus_production_transport_j_kg
AddIssue $counterfactual 'IF97-VS-PRODUCTION-GAP' ($referenceGap -ge [double]$G.production_transport_gap_min_j_kg -and $referenceGap -le [double]$G.production_transport_gap_max_j_kg)

if($identity.Count -gt 0){
    $classification='DIAGNOSTIC-IDENTITY-RED'
}elseif($referenceSetup.Count -gt 0){
    $classification='REFERENCE-COUNTERFACTUAL-RED'
}elseif($counterfactual.Count -gt 0){
    $classification='TRANSPORT-SEAM-LOCALIZED-COUNTERFACTUAL-NOT-CLOSED'
}else{
    $classification='CAUSAL-CLOSURE-CONFIRMED'
}

Req (@($C.diagnostic.adjudication_classes) -contains $classification) 'unplanned adjudication class'
$allIssues=New-Object 'Collections.Generic.List[string]'
foreach($x in $identity){$allIssues.Add($x)}
foreach($x in $referenceSetup){$allIssues.Add($x)}
foreach($x in $counterfactual){$allIssues.Add($x)}
$issueText=if($allIssues.Count -eq 0){'none'}else{$allIssues -join ';'}

$summary=@(
'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
('classification='+$classification),
'first-causal-checkpoint=seed-step1',
'first-causal-node=suction',
'localized-seam=STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT',
'repair-owner=UNSELECTED',
('diagnostic-issues='+$issueText),
('candidate-observed-energy-rate-w='+$obs.ToString('R',$Inv)),
('runtime-suction-minus-production-transport-j-kg='+$runtimeGap.ToString('R',$Inv)),
('if97-saturation-pressure-abs-delta-pa='+$satDelta.ToString('R',$Inv)),
('if97-reference-vs-mode2-suction-abs-delta-j-kg='+$referenceVsSuction.ToString('R',$Inv)),
('if97-minus-production-transport-j-kg='+$referenceGap.ToString('R',$Inv)),
('if97-counterfactual-net-energy-abs-w='+$counterNet.ToString('R',$Inv)),
('if97-counterfactual-derived-budget-w='+$counterBudget.ToString('R',$Inv)),
('if97-counterfactual-rate-identity-residual-w='+$counterIdentityResidual.ToString('R',$Inv)),
'counterfactual-budget-mode=DERIVED-FROM-COMPONENT-GUARDS',
('mode2-raw-resolver-path='+[string]$raw.resolution_path),
('mode2-step1-resolver-path='+[string]$step1.resolution_path),
'resolver-evidence-semantics=RECONSTRUCTED-FROM-EXACT-FROZEN-CONSERVED-INVENTORY',
('forward-provider-all-ieee754-bits-identical='+[string]$t.forward_properties_all_bits_equal),
'hosted-ordinary-ci=PENDING-CONFIRMATION',
'production-repair-authorized=false',
'repair-planning-authorized=false',
'seed-retuning-authorized=false',
'threshold-change-authorized=false',
'c4-change-authorized=false',
'canonical-exact-v9-change-authorized=false',
'r3-passed=false',
'r3-requalification3-authorized=false',
'r4-planning-authorized=false',
'next-step=RETURN-ARTIFACTS-01-07-FOR-INDEPENDENT-ADJUDICATION'
)
[IO.File]::WriteAllLines((Join-Path $A '06-diagnostic-summary.txt'),$summary,(New-Object Text.UTF8Encoding($false)))

$closure=($classification -eq 'CAUSAL-CLOSURE-CONFIRMED')
$review=@(
'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
('engineering-decision='+$classification),
('causal-closure-confirmed='+($(if($closure){'true'}else{'false'}))),
'localized-seam=STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT',
'repair-owner=UNSELECTED',
'evidence-meaning=REV1 separates production runtime bookkeeping from an independent test-only IAPWS-IF97 transport counterfactual.',
'evidence-meaning-2=A causal-closure classification confirms the seam for planning purposes only; it does not select a production ownership design.',
'hosted-ci-hold=Production repair planning and implementation remain blocked until hosted ordinary-ci is GREEN.',
'not-authorized=production repair; repair planning; seed retuning; threshold change; C4/payload change; canonical exact-v9 change; R3 PASS; R3 Requalification 3; R4 Planning 1',
'next-required-action=Return artifacts 01-07 for independent adjudication. Combine causal closure with hosted ordinary-ci GREEN only in a later returned-evidence gate.'
)
[IO.File]::WriteAllLines((Join-Path $A '07-pre-repair-review.txt'),$review,(New-Object Text.UTF8Encoding($false)))

Write-Host ('R3 Suction Energy-Transport Causal-Seam Diagnostic 3 REV1 adjudication: '+$classification) -ForegroundColor Green
