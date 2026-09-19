$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
function F([double]$v){$v.ToString('R',[Globalization.CultureInfo]::InvariantCulture)}
$Root=Split-Path -Parent $PSScriptRoot;$D=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-fast-gate-dynamic-equilibrium-diagnostic1'
$k=@(Import-Csv (Join-Path $D '02-first-100-step-key-dynamics.csv'));$h=@(Import-Csv (Join-Path $D '03-first-100-step-hydraulic-heads.csv'));$c=@(Import-Csv (Join-Path $D '04-first-100-step-controller-turbine.csv'))
if($k.Count -ne 100 -or $h.Count -ne 800 -or $c.Count -ne 100){throw 'diagnostic evidence cardinality drift'}
$m1bad=@($k|Where-Object mode1_envelope -ne 'true').Count;$c2bad=@($k|Where-Object candidate_envelope -ne 'true').Count
$ff=($k|Where-Object {[double]::Parse($_.candidate_primary_pump_kg_s,[Globalization.CultureInfo]::InvariantCulture)-lt 99.9}|Select-Object -First 1).step
$fg=($k|Where-Object {[double]::Parse($_.candidate_governor,[Globalization.CultureInfo]::InvariantCulture)-gt 29.30}|Select-Object -First 1).step
$last=$k[-1];$lastCtl=$c[-1]
$sum=@(
'status=DIAGNOSTIC-EVIDENCE-WRITTEN',
'classification=POST-SEED-DYNAMIC-EQUILIBRIUM-DIVERGENCE',
("mode1-envelope-violation-steps={0}" -f $m1bad),
("candidate-envelope-violation-steps={0}" -f $c2bad),
("first-candidate-primary-flow-violation-step={0}" -f $ff),
("first-candidate-governor-violation-step={0}" -f $fg),
("step100-primary-flow-delta-kg-s={0}" -f (F ([double]::Parse($last.candidate_primary_pump_kg_s,[Globalization.CultureInfo]::InvariantCulture)-[double]::Parse($last.mode1_primary_pump_kg_s,[Globalization.CultureInfo]::InvariantCulture)))),
("step100-governor-delta={0}" -f (F ([double]::Parse($last.candidate_governor,[Globalization.CultureInfo]::InvariantCulture)-[double]::Parse($last.mode1_governor,[Globalization.CultureInfo]::InvariantCulture)))),
("step100-speed-error-delta={0}" -f (F ([double]::Parse($lastCtl.candidate_speed_error,[Globalization.CultureInfo]::InvariantCulture)-[double]::Parse($lastCtl.mode1_speed_error,[Globalization.CultureInfo]::InvariantCulture)))),
("step100-speed-integral-delta={0}" -f (F ([double]::Parse($lastCtl.candidate_speed_integral,[Globalization.CultureInfo]::InvariantCulture)-[double]::Parse($lastCtl.mode1_speed_integral,[Globalization.CultureInfo]::InvariantCulture)))),
'production-repair-applied=False','threshold-change-applied=False','r3-remains-red=True','requalification3-authorized=False','r4-planning-authorized=False')
[IO.File]::WriteAllLines((Join-Path $D '05-diagnostic-summary.txt'),$sum,(New-Object Text.UTF8Encoding($false)))
$review=@('status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE','next-step=RETURN-ARTIFACTS-FOR-ADJUDICATION','production-repair-authorized=False','threshold-change-authorized=False','r3-remains-red=True','r4-remains-blocked=True')
[IO.File]::WriteAllLines((Join-Path $D '06-pre-repair-review.txt'),$review,(New-Object Text.UTF8Encoding($false)))
Write-Host 'Dynamic-equilibrium diagnostic evidence adjudicated.' -ForegroundColor Green
