$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
$Inv=[Globalization.CultureInfo]::InvariantCulture
function D([object]$v){[double]::Parse([string]$v,$Inv)}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$A=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3'
$balance=@(Import-Csv (Join-Path $A '01-step1-suction-energy-balance.csv'))
$paths=@(Import-Csv (Join-Path $A '02-mode2-suction-inverse-path.csv'))
$provider=@(Import-Csv (Join-Path $A '03-forward-saturation-provider-seam.csv'))
Req ($balance.Count -eq 2) 'balance row cardinality drift'
Req ($paths.Count -eq 2) 'resolver-path row cardinality drift'
Req ($provider.Count -eq 1) 'forward-provider row cardinality drift'
$mode1=@($balance|Where-Object case -eq 'mode1')
$cand=@($balance|Where-Object case -eq 'candidate-mode2')
Req ($mode1.Count -eq 1 -and $cand.Count -eq 1) 'balance case identity drift'
$raw=@($paths|Where-Object checkpoint -eq 'raw')
$step1=@($paths|Where-Object checkpoint -eq 'seed-step1')
Req ($raw.Count -eq 1 -and $step1.Count -eq 1) 'resolver checkpoint identity drift'
$p=$provider[0]
$mode1=$mode1[0];$cand=$cand[0];$raw=$raw[0];$step1=$step1[0]
Req ([Math]::Abs((D $mode1.observed_mass_rate_kg_s)) -le 1e-9) 'mode1 suction mass balance moved'
Req ([Math]::Abs((D $cand.observed_mass_rate_kg_s)) -le 1e-9) 'candidate suction mass balance moved'
Req ([Math]::Abs((D $mode1.observed_energy_rate_w)) -le 0.01) 'mode1 suction energy baseline moved'
$obs=D $cand.observed_energy_rate_w
Req ($obs -ge -5217000 -and $obs -le -5215000) 'candidate suction energy-rate localization drift'
Req ([Math]::Abs((D $cand.predicted_energy_residual_w)) -le 0.001) 'candidate energy identity does not close'
Req ([Math]::Abs((D $cand.predicted_mass_residual_kg_s)) -le 1e-9) 'candidate mass identity does not close'
$du=D $cand.suction_minus_recirculation_specific_energy_j_kg
Req ($du -ge 52100 -and $du -le 52250) 'specific-energy seam magnitude drift'
Req ($raw.resolved_phase -eq 'SubcooledLiquid') 'raw mode2 suction phase drift'
Req ($step1.resolved_phase -eq 'SaturatedMixture') 'step1 mode2 suction phase drift'
Req ($p.bitwise_identical_across_modes -eq 'true') 'forward saturation provider is no longer common across closure modes'
$pred=D $cand.predicted_energy_rate_w
$res=D $cand.predicted_energy_residual_w
$flow=D $cand.pump_flow_kg_s
$recirc=D $cand.recirculation_flow_kg_s
$quality=[string]$cand.step1_quality
$classification='MODE2-FORWARD-INVERSE-ADVECTED-ENERGY-CLOSURE-GAP'
$summary=@(
'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
('classification='+$classification),
'first-causal-checkpoint=seed-step1',
'first-causal-node=suction',
('candidate-observed-energy-rate-w='+$obs.ToString('R',$Inv)),
('candidate-predicted-energy-rate-w='+$pred.ToString('R',$Inv)),
('candidate-energy-identity-residual-w='+$res.ToString('R',$Inv)),
('candidate-suction-minus-recirculation-specific-energy-j-kg='+$du.ToString('R',$Inv)),
('candidate-pump-flow-kg-s='+$flow.ToString('R',$Inv)),
('candidate-recirculation-flow-kg-s='+$recirc.ToString('R',$Inv)),
('mode2-raw-resolver-path='+$raw.resolution_path),
('mode2-step1-resolver-path='+$step1.resolution_path),
('mode2-step1-phase='+$step1.resolved_phase),
('mode2-step1-quality='+$quality),
'forward-saturation-provider-bitwise-identical-across-modes=true',
'governor-initiation-supported=false',
'production-repair-authorized=false',
'seed-retuning-authorized=false',
'threshold-change-authorized=false',
'canonical-exact-v9-change-authorized=false',
'r3-passed=false',
'r3-requalification3-authorized=false',
'r4-planning-authorized=false',
'next-step=RETURN-ARTIFACTS-FOR-INDEPENDENT-ADJUDICATION'
)
[IO.File]::WriteAllLines((Join-Path $A '04-diagnostic-summary.txt'),$summary,(New-Object Text.UTF8Encoding($false)))
$review=@(
'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
'engineering-decision=CAUSAL-SEAM-LOCALIZED-NO-REPAIR-SELECTION',
'causal-owner=STEAM-DRUM-LIQUID-RECIRCULATION-FORWARD-PROVIDER-VS-MODE2-SUCTION-INVERSE-INVENTORY',
'evidence-meaning=The first-step mode2 suction energy loss is reconstructed from existing production source/sink terms to bookkeeping residual while mass remains balanced.',
'evidence-meaning-2=The common forward saturation provider supplies the drum liquid transport state while the mode2 conserved suction inventory is interpreted by the reference-consistent inverse resolver.',
'evidence-meaning-3=The resulting selected-specific-energy gap precedes the observed seed-step1 phase-path transition.',
'not-authorized=production repair; seed retuning; transport-mode change; C4/payload change; threshold change; canonical exact-v9 change; R3 Requalification 3; R4 Planning 1',
'next-required-action=Return artifacts 01-05 for independent adjudication and repair-planning authorization decision.'
)
[IO.File]::WriteAllLines((Join-Path $A '05-pre-repair-review.txt'),$review,(New-Object Text.UTF8Encoding($false)))
Write-Host 'R3 Suction Energy-Transport Causal-Seam Diagnostic 3 adjudication: PASS' -ForegroundColor Green
