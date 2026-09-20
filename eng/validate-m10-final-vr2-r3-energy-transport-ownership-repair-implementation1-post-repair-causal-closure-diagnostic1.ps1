$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$Problems = New-Object System.Collections.Generic.List[string]
function Add-Problem([string]$m){ $Problems.Add($m) }
function Req([bool]$c,[string]$m){ if(-not $c){ Add-Problem $m } }
function Sha([string]$p){
  $s=[IO.File]::OpenRead($p)
  try { $h=[Security.Cryptography.SHA256]::Create(); try { return ([BitConverter]::ToString($h.ComputeHash($s))).Replace('-','') } finally { $h.Dispose() } }
  finally { $s.Dispose() }
}
function Root {
  $d=[IO.DirectoryInfo]::new((Get-Location).Path)
  while($null -ne $d){ if(Test-Path (Join-Path $d.FullName 'NuclearReactorSimulator.sln')){ return $d.FullName }; $d=$d.Parent }
  throw 'repository root not found'
}
function Count-Bad($rows,[scriptblock]$ok){ return @($rows | Where-Object { -not (& $ok $_) }).Count }
function First-Bad($rows,[scriptblock]$ok){ $r=@($rows | Where-Object { -not (& $ok $_) } | Select-Object -First 1); if($r.Count -eq 0){return 0}; return [int]$r[0].step }
$R=Root
$C=Get-Content (Join-Path $R 'eng/m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1-contract.json') -Raw | ConvertFrom-Json
Req ($C.gate_id -eq 'R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1-POST-REPAIR-CAUSAL-CLOSURE-DIAGNOSTIC1') 'gate id drift'
Req (-not [bool]$C.authority.production_change_authorized) 'production authority drift'
Req (-not [bool]$C.authority.threshold_change_authorized) 'threshold authority drift'
Req ($C.r3_state -eq 'RED') 'R3 state drift'
foreach($p in $C.production_surface_sha256.PSObject.Properties){ $f=Join-Path $R ($p.Name -replace '/', [IO.Path]::DirectorySeparatorChar); Req (Test-Path $f) ('missing production path '+$p.Name); if(Test-Path $f){ Req ((Sha $f) -eq ([string]$p.Value).ToUpperInvariant()) ('production drift '+$p.Name) } }
$tf=Join-Path $R ($C.existing_runtime_evidence_test.path -replace '/', [IO.Path]::DirectorySeparatorChar)
Req (Test-Path $tf) 'runtime evidence test missing'
if(Test-Path $tf){ Req ((Sha $tf) -eq ([string]$C.existing_runtime_evidence_test.sha256).ToUpperInvariant()) 'runtime evidence test drift' }
$oldPath=Join-Path $R ($C.frozen_fast_gate_evidence.historical_pre_repair.path -replace '/', [IO.Path]::DirectorySeparatorChar)
$newPath=Join-Path $R ($C.frozen_fast_gate_evidence.returned_post_repair.path -replace '/', [IO.Path]::DirectorySeparatorChar)
Req (Test-Path $oldPath) 'historical fast gate evidence missing'; Req (Test-Path $newPath) 'post-repair fast gate evidence missing'
if(Test-Path $oldPath){ Req ((Sha $oldPath) -eq ([string]$C.frozen_fast_gate_evidence.historical_pre_repair.sha256).ToUpperInvariant()) 'historical fast gate evidence drift' }
if(Test-Path $newPath){ Req ((Sha $newPath) -eq ([string]$C.frozen_fast_gate_evidence.returned_post_repair.sha256).ToUpperInvariant()) 'post-repair fast gate evidence drift' }
if((Test-Path $oldPath) -and (Test-Path $newPath)){
  $old=Import-Csv $oldPath; $new=Import-Csv $newPath
  $envOk={ param($r) $r.in_envelope -eq 'true' }
  $primaryOk={ param($r) ([double]$r.primary_pump_kg_s -ge 99.9) -and ([double]$r.primary_pump_kg_s -le 100.1) }
  $govOk={ param($r) ([double]$r.governor_percent -ge 29.27) -and ([double]$r.governor_percent -le 29.30) }
  $elecOk={ param($r) ([double]$r.electrical_mwe -ge 4.99) -and ([double]$r.electrical_mwe -le 5.01) }
  $levelOk={ param($r) ([double]$r.drum_level -ge 0.49) -and ([double]$r.drum_level -le 0.51) }
  Req ((Count-Bad $old $envOk) -eq 86) 'historical total violations drift'
  Req ((First-Bad $old $envOk) -eq 15) 'historical first violation drift'
  Req ((Count-Bad $old $primaryOk) -eq 86) 'historical primary violations drift'
  Req ((First-Bad $old $primaryOk) -eq 15) 'historical primary first violation drift'
  Req ((Count-Bad $old $govOk) -eq 84) 'historical governor violations drift'
  Req ((First-Bad $old $govOk) -eq 17) 'historical governor first violation drift'
  Req ((Count-Bad $new $envOk) -eq 84) 'post-repair total violations drift'
  Req ((First-Bad $new $envOk) -eq 17) 'post-repair first violation drift'
  Req ((Count-Bad $new $primaryOk) -eq 79) 'post-repair primary violations drift'
  Req ((First-Bad $new $primaryOk) -eq 22) 'post-repair primary first violation drift'
  Req ((Count-Bad $new $govOk) -eq 84) 'post-repair governor violations drift'
  Req ((First-Bad $new $govOk) -eq 17) 'post-repair governor first violation drift'
  Req ((Count-Bad $new $elecOk) -eq 0) 'post-repair electrical violations drift'
  Req ((Count-Bad $new $levelOk) -eq 0) 'post-repair level violations drift'
  Req (@($new | Where-Object { $_.trip_active -ne 'false' }).Count -eq 0) 'post-repair trip drift'
  Req (@($new | Where-Object { $_.breaker_closed -ne 'true' }).Count -eq 0) 'post-repair breaker drift'
  Req (@($new | Where-Object { [long]$_.rollbacks -ne 0 }).Count -eq 0) 'post-repair rollback drift'
  Req (@($new | Where-Object { $_.finite -ne 'true' }).Count -eq 0) 'post-repair finite drift'
  $maxGov=0.0
  for($i=0;$i -lt [Math]::Min($old.Count,$new.Count);$i++){ $d=[Math]::Abs(([double]$new[$i].governor_percent)-([double]$old[$i].governor_percent)); if($d -gt $maxGov){$maxGov=$d} }
  Req ($maxGov -le [double]$C.frozen_fast_gate_evidence.common_mode_governor_max_abs_delta_percent) 'governor is not common-mode within frozen comparison bound'
}
$adjPath=Join-Path $R 'eng/adjudicate-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1.ps1'
Req (Test-Path $adjPath) 'adjudicator missing'
if(Test-Path $adjPath){
  $adjLines=[IO.File]::ReadAllLines($adjPath)
  $adjMarker='# NRS-MARKER:R3-POST-REPAIR-D1-INMEMORY-AUTHORITY-HOTFIX4'
  Req (@($adjLines | Where-Object { $_ -eq $adjMarker }).Count -eq 1) 'adjudicator Hotfix4 marker cardinality drift'
  $adjText=[IO.File]::ReadAllText($adjPath)
  Req (-not [Text.RegularExpressions.Regex]::IsMatch($adjText,'(?m)\b(Set-Content|Copy-Item|Out-File|Export-Csv)\b')) 'adjudicator forbidden path-provider writer reintroduced'
  Req ([Text.RegularExpressions.Regex]::Matches($adjText,[Text.RegularExpressions.Regex]::Escape('[IO.File]::WriteAllLines')).Count -eq 1) 'adjudicator optional writer cardinality drift'
  Req ([Text.RegularExpressions.Regex]::Matches($adjText,[Text.RegularExpressions.Regex]::Escape("'artifacts/r3d1.txt'")).Count -eq 1) 'adjudicator short summary path drift'
  Req ([Text.RegularExpressions.Regex]::Matches($adjText,[Text.RegularExpressions.Regex]::Escape('engineering verdict is unaffected')).Count -eq 1) 'adjudicator persistence isolation marker drift'
}
if($Problems.Count -gt 0){ Write-Host 'M10 Final VR2 R3 Post-Repair Causal-Closure Diagnostic 1 validator: FAIL'; $Problems | ForEach-Object { Write-Host (' - '+$_) }; throw ('validator found {0} problem(s)' -f $Problems.Count) }
Write-Host 'M10 Final VR2 R3 Post-Repair Causal-Closure Diagnostic 1 validator: PASS'
Write-Host 'Known-RED historical fast gate is frozen as diagnostic evidence; no threshold is changed.'
