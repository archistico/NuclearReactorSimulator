$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function D([string]$v){[double]::Parse($v,[Globalization.CultureInfo]::InvariantCulture)}
function F([double]$v){$v.ToString('R',[Globalization.CultureInfo]::InvariantCulture)}
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function MaxAbs($rows,[string]$column){
    $values=@($rows|ForEach-Object{[Math]::Abs((D ([string]$_.$column)))})
    if($values.Count -eq 0){throw "No values for $column"}
    return [double](($values|Measure-Object -Maximum).Maximum)
}
function PhaseNodes($rows){@($rows|Where-Object phase_match -ne 'true'|ForEach-Object node)}
$Root=Split-Path -Parent $PSScriptRoot
$Droot=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2'
$raw=@(Import-Csv (Join-Path $Droot '01-raw-checkpoint-node-comparison.csv'))
$s1=@(Import-Csv (Join-Path $Droot '02-seed-step1-node-comparison.csv'))
$s2=@(Import-Csv (Join-Path $Droot '03-seed-step2-node-comparison.csv'))
$heads=@(Import-Csv (Join-Path $Droot '04-seed-step-hydraulic-heads.csv'))
$flows=@(Import-Csv (Join-Path $Droot '05-seed-step-flow-comparison.csv'))
$ctl=@(Import-Csv (Join-Path $Droot '06-seed-step-controller-turbine.csv'))
Req ($raw.Count -eq 12 -and $s1.Count -eq 12 -and $s2.Count -eq 12) 'node evidence cardinality drift'
Req ($heads.Count -eq 16) 'hydraulic-head evidence cardinality drift'
Req ($flows.Count -eq 24) 'flow evidence cardinality drift'
Req ($ctl.Count -eq 2) 'controller evidence cardinality drift'
Req (@($raw|Where-Object checkpoint -ne 'raw').Count -eq 0) 'raw checkpoint label drift'
Req (@($s1|Where-Object checkpoint -ne 'seed-step1').Count -eq 0) 'seed-step1 checkpoint label drift'
Req (@($s2|Where-Object checkpoint -ne 'seed-step2').Count -eq 0) 'seed-step2 checkpoint label drift'
Req (@($raw.node|Sort-Object -Unique).Count -eq 12 -and @($s1.node|Sort-Object -Unique).Count -eq 12 -and @($s2.node|Sort-Object -Unique).Count -eq 12) 'node identity cardinality drift'
Req (@($heads|Where-Object checkpoint -eq 'seed-step1').Count -eq 8 -and @($heads|Where-Object checkpoint -eq 'seed-step2').Count -eq 8) 'hydraulic checkpoint cardinality drift'
Req (@($flows|Where-Object checkpoint -eq 'seed-step1').Count -eq 12 -and @($flows|Where-Object checkpoint -eq 'seed-step2').Count -eq 12) 'flow checkpoint cardinality drift'
Req (@($ctl|Where-Object checkpoint -eq 'seed-step1').Count -eq 1 -and @($ctl|Where-Object checkpoint -eq 'seed-step2').Count -eq 1) 'controller checkpoint cardinality drift'
if(@($raw|Where-Object phase_match -ne 'true').Count -ne 0){throw 'frozen raw candidate phase qualification drift'}
# HOTFIX 1: force array capture at the function call boundary. In Windows PowerShell,
# a function that emits zero pipeline objects assigns $null, and StrictMode then makes
# .Count a PropertyNotFoundStrict error. @(...) preserves a stable 0/1/N collection.
$rawPhase=@(PhaseNodes $raw)
$s1Phase=@(PhaseNodes $s1)
$s2Phase=@(PhaseNodes $s2)
$firstPhase=if($rawPhase.Count -gt 0){'raw'}elseif($s1Phase.Count -gt 0){'seed-step1'}elseif($s2Phase.Count -gt 0){'seed-step2'}else{'none'}
$h1=@($heads|Where-Object checkpoint -eq 'seed-step1');$h2=@($heads|Where-Object checkpoint -eq 'seed-step2')
$f1=@($flows|Where-Object checkpoint -eq 'seed-step1');$f2=@($flows|Where-Object checkpoint -eq 'seed-step2')
$c1=$ctl|Where-Object checkpoint -eq 'seed-step1'|Select-Object -First 1
$c2=$ctl|Where-Object checkpoint -eq 'seed-step2'|Select-Object -First 1
if($null -eq $c1 -or $null -eq $c2){throw 'controller checkpoint drift'}
$rSuction=$raw|Where-Object node -eq 'suction'|Select-Object -First 1
$s1Suction=$s1|Where-Object node -eq 'suction'|Select-Object -First 1
$s2Suction=$s2|Where-Object node -eq 'suction'|Select-Object -First 1
if($null -eq $rSuction -or $null -eq $s1Suction -or $null -eq $s2Suction){throw 'suction evidence missing'}
$sum=@(
'status=DIAGNOSTIC-EVIDENCE-WRITTEN',
'classification=TWO-SEED-STEP-PRECONDITIONING-DIVERGENCE-LOCALIZATION-EVIDENCE',
('raw-phase-mismatch-count={0}' -f $rawPhase.Count),
('raw-phase-mismatch-nodes={0}' -f (($rawPhase -join ';'))),
('seed-step1-phase-mismatch-count={0}' -f $s1Phase.Count),
('seed-step1-phase-mismatch-nodes={0}' -f (($s1Phase -join ';'))),
('seed-step2-phase-mismatch-count={0}' -f $s2Phase.Count),
('seed-step2-phase-mismatch-nodes={0}' -f (($s2Phase -join ';'))),
('first-phase-divergence-checkpoint={0}' -f $firstPhase),
('raw-max-abs-pressure-delta-pa={0}' -f (F (MaxAbs $raw 'pressure_delta_pa'))),
('seed-step1-max-abs-pressure-delta-pa={0}' -f (F (MaxAbs $s1 'pressure_delta_pa'))),
('seed-step2-max-abs-pressure-delta-pa={0}' -f (F (MaxAbs $s2 'pressure_delta_pa'))),
('seed-step1-max-abs-hydraulic-head-delta-pa={0}' -f (F (MaxAbs $h1 'head_delta_pa'))),
('seed-step2-max-abs-hydraulic-head-delta-pa={0}' -f (F (MaxAbs $h2 'head_delta_pa'))),
('seed-step1-max-abs-flow-delta-kg-s={0}' -f (F (MaxAbs $f1 'flow_delta_kg_s'))),
('seed-step2-max-abs-flow-delta-kg-s={0}' -f (F (MaxAbs $f2 'flow_delta_kg_s'))),
('raw-suction-mode1-phase={0}' -f $rSuction.mode1_phase),
('raw-suction-candidate-phase={0}' -f $rSuction.candidate_phase),
('seed-step1-suction-mode1-phase={0}' -f $s1Suction.mode1_phase),
('seed-step1-suction-candidate-phase={0}' -f $s1Suction.candidate_phase),
('seed-step2-suction-mode1-phase={0}' -f $s2Suction.mode1_phase),
('seed-step2-suction-candidate-phase={0}' -f $s2Suction.candidate_phase),
('seed-step1-governor-delta={0}' -f (F ((D $c1.candidate_governor)-(D $c1.mode1_governor)))),
('seed-step2-governor-delta={0}' -f (F ((D $c2.candidate_governor)-(D $c2.mode1_governor)))),
('seed-step1-speed-error-delta={0}' -f (F ((D $c1.candidate_speed_error)-(D $c1.mode1_speed_error)))),
('seed-step2-speed-error-delta={0}' -f (F ((D $c2.candidate_speed_error)-(D $c2.mode1_speed_error)))),
('seed-step1-speed-integral-delta={0}' -f (F ((D $c1.candidate_speed_integral)-(D $c1.mode1_speed_integral)))),
('seed-step2-speed-integral-delta={0}' -f (F ((D $c2.candidate_speed_integral)-(D $c2.mode1_speed_integral)))),
'production-repair-applied=False',
'seed-retuning-applied=False',
'threshold-change-applied=False',
'c4-change-applied=False',
'canonical-exact-v9-change-applied=False',
'r3-remains-red=True',
'requalification3-authorized=False',
'r4-planning-authorized=False')
[IO.File]::WriteAllLines((Join-Path $Droot '07-diagnostic-summary.txt'),$sum,(New-Object Text.UTF8Encoding($false)))
$review=@(
'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
'next-step=RETURN-ARTIFACTS-FOR-ADJUDICATION',
'production-repair-authorized=False',
'seed-retuning-authorized=False',
'threshold-change-authorized=False',
'c4-change-authorized=False',
'canonical-exact-v9-change-authorized=False',
'r3-remains-red=True',
'requalification3-authorized=False',
'r4-remains-blocked=True')
[IO.File]::WriteAllLines((Join-Path $Droot '08-pre-repair-review.txt'),$review,(New-Object Text.UTF8Encoding($false)))
$hotfix=@(
'status=PASS-ADJUDICATOR-HOTFIX1',
'original-failure=PropertyNotFoundStrict-on-empty-rawPhase-Count',
'root-cause=ZERO-PIPELINE-OUTPUT-ASSIGNED-AS-NULL-UNDER-STRICTMODE',
'fix=EXPLICIT-ARRAY-CAPTURE-AT-PHASE-NODE-CALL-SITES',
'dynamic-evidence-reexecuted=False',
'returned-evidence-reused=True',
'evidence-files-reused=01-06',
'r3-remains-red=True',
'production-repair-authorized=False',
'r4-remains-blocked=True')
[IO.File]::WriteAllLines((Join-Path $Droot '09-adjudicator-hotfix1-record.txt'),$hotfix,(New-Object Text.UTF8Encoding($false)))
Write-Host 'Two-seed-step preconditioning Diagnostic 2 evidence adjudicated with Hotfix 1.' -ForegroundColor Green
