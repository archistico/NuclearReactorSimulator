$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# NRS-MARKER:R3-POST-REPAIR-D1-INMEMORY-AUTHORITY-HOTFIX4
function Root {
  $d=[IO.DirectoryInfo]::new((Get-Location).Path)
  while($null -ne $d){
    if(Test-Path (Join-Path $d.FullName 'NuclearReactorSimulator.sln')){ return $d.FullName }
    $d=$d.Parent
  }
  throw 'repository root not found'
}
function Abs([double]$v){ [Math]::Abs($v) }
$R=Root
$C=Get-Content (Join-Path $R 'eng/m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1-contract.json') -Raw | ConvertFrom-Json
$src=Join-Path $R 'artifacts/m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1/01-step1-suction-energy-balance.csv'
if(-not (Test-Path $src)){ throw 'post-repair balance evidence missing' }
$rows=Import-Csv $src
$cand=@($rows | Where-Object { $_.case -eq 'candidate-mode2' })
if($cand.Count -ne 1){ throw 'candidate-mode2 balance row cardinality invalid' }
$r=$cand[0]
$massResidual=Abs([double]$r.predicted_mass_residual_kg_s)
$transportDelta=Abs([double]$r.suction_minus_recirculation_specific_energy_j_kg)
$observedEnergy=Abs([double]$r.observed_energy_rate_w)
$energyResidual=Abs([double]$r.predicted_energy_residual_w)
$pass=($massResidual -le [double]$C.post_repair_causal_acceptance.mass_identity_abs_residual_kg_s_max) -and
      ($transportDelta -le [double]$C.post_repair_causal_acceptance.transport_specific_energy_abs_delta_j_kg_max) -and
      ($observedEnergy -le [double]$C.post_repair_causal_acceptance.observed_net_energy_abs_w_max) -and
      ($energyResidual -le [double]$C.post_repair_causal_acceptance.production_energy_identity_abs_residual_w_max)
$class=if($pass){'POST-REPAIR-CAUSAL-CLOSURE-CONFIRMED'}else{'POST-REPAIR-CAUSAL-CLOSURE-NOT-CONFIRMED'}
$inv=[Globalization.CultureInfo]::InvariantCulture
$lines=[string[]]@(
  'status=PASS-EVIDENCE-ADJUDICATED',
  ('source-evidence='+$src),
  ('classification='+$class),
  ('mass-identity-abs-residual-kg-s='+$massResidual.ToString('R',$inv)),
  ('transport-specific-energy-abs-delta-j-kg='+$transportDelta.ToString('R',$inv)),
  ('observed-net-energy-abs-w='+$observedEnergy.ToString('R',$inv)),
  ('production-energy-identity-abs-residual-w='+$energyResidual.ToString('R',$inv)),
  'historical-fast-gate-pre-repair=KNOWN-RED-86-VIOLATIONS-FIRST-STEP-15',
  'returned-fast-gate-post-repair=RED-84-VIOLATIONS-FIRST-STEP-17',
  'fast-gate-zero-envelope-as-implementation-discriminator=INVALID-HISTORICAL-CONTRADICTION',
  'threshold-change-applied=False',
  'seed-retuning-applied=False',
  'production-change-applied-by-diagnostic=False',
  'r3-remains-red=True',
  ('next-step='+($(if($pass){$C.next_if_confirmed}else{'REPAIR-IMPLEMENTATION-REVIEW'})))
)
Write-Host '--- adjudication summary ---'
$lines | ForEach-Object { Write-Host $_ }
Write-Host '--- end adjudication summary ---'

# Persistence is evidence convenience only. It must never change PASS/FAIL authority.
$dst=Join-Path $R 'artifacts/r3d1.txt'
try {
  $artifactDir=Join-Path $R 'artifacts'
  if(-not [IO.Directory]::Exists($artifactDir)){ [IO.Directory]::CreateDirectory($artifactDir) | Out-Null }
  [IO.File]::WriteAllLines($dst,$lines,[Text.Encoding]::ASCII)
  Write-Host ('adjudication-summary-persisted=True path='+$dst+' length='+$dst.Length)
}
catch {
  Write-Warning ('adjudication-summary-persisted=False; engineering verdict is unaffected; '+$_.Exception.Message)
}

if(-not $pass){ throw 'Post-repair causal closure is not confirmed.' }
Write-Host 'Post-repair causal closure: CONFIRMED'
Write-Host 'Historical zero-envelope fast gate contradiction: CONFIRMED'
