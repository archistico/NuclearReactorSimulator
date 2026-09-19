$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([IO.File]::ReadAllBytes($p)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r,[string]$exclude=''){$rr=(Resolve-Path $r).Path;$list=New-Object 'Collections.Generic.List[string]';Get-ChildItem $rr -Recurse -File|ForEach-Object{$rel=$_.FullName.Substring($rr.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj' -or $rel -eq $exclude){return};$list.Add($rel)};$a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $a){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$s=[Security.Cryptography.SHA256]::Create();try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()};@{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-contract.json' -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-v1') 'schema drift'
Req ($C.status -eq 'DIAGNOSTIC-CANDIDATE') 'candidate status drift'
$pred=Join-Path $Root ([string]$C.prerequisite.returned_diagnostic2_manifest.path).Replace('/','\')
Req ((Sha $pred)-eq [string]$C.prerequisite.returned_diagnostic2_manifest.sha256) 'Diagnostic 2 returned manifest drift'
$P=Get-Content $pred -Raw|ConvertFrom-Json
Req ($P.status -eq 'PASS-DIAGNOSTIC-EVIDENCE-COMPLETE') 'Diagnostic 2 predecessor status drift'
Req ($P.engineering_classification -eq 'SEED-STEP1-SUCTION-PHASE-BOUNDARY-HYDRAULIC-DIVERGENCE-LOCALIZED') 'Diagnostic 2 classification drift'
Req ($P.authority.next_authorized_activity -eq 'R3-SEED-INTEGRATION-SUCTION-ENERGY-TRANSPORT-CAUSAL-SEAM-DIAGNOSTIC3') 'Diagnostic 3 authority missing'
foreach($f in $C.prerequisite.returned_diagnostic2_artifacts.PSObject.Properties){$path=Join-Path $Root (([string]$C.prerequisite.returned_diagnostic2_directory)+'/'+$f.Name).Replace('/','\');Req ((Sha $path)-eq [string]$f.Value) ('Diagnostic 2 frozen artifact drift: '+$f.Name)}
$src=Tree 'src';Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift'
$testRel=[string]$C.files.test.path;$under=$testRel.Substring('tests/'.Length);$hist=Tree 'tests' $under
Req ($hist.Count -eq [int]$C.baseline.historical_tests_file_count -and $hist.Hash -eq [string]$C.baseline.historical_tests_tree_sha256) 'historical tests drift'
foreach($n in @('test','document','returned_adjudication_document','adjudicator','runner','validator')){$e=$C.files.$n;$path=Join-Path $Root ([string]$e.path).Replace('/','\');Req ((NSha $path)-eq [string]$e.normalized_sha256) ('Diagnostic 3 file drift: '+$n)}
Req ([int]$C.diagnostic.runtime_step_ms -eq 10 -and [int]$C.diagnostic.seed_step_count -eq 1) 'runtime checkpoint drift'
Req ([int]$C.diagnostic.balance_rows -eq 2 -and [int]$C.diagnostic.resolver_path_rows -eq 2 -and [int]$C.diagnostic.forward_provider_rows -eq 1) 'evidence cardinality drift'
Req (-not [bool]$C.diagnostic.production_change_allowed -and -not [bool]$C.diagnostic.threshold_change_allowed) 'diagnostic scope drift'
Req (-not [bool]$C.authority.production_repair_authorized -and -not [bool]$C.authority.seed_retuning_authorized -and -not [bool]$C.authority.threshold_change_authorized) 'repair authority drift'
Req (-not [bool]$C.authority.c4_change_authorized -and -not [bool]$C.authority.canonical_exact_v9_change_authorized) 'thermodynamic/exact-v9 authority drift'
Req (-not [bool]$C.authority.r3_passed -and -not [bool]$C.authority.r3_requalification3_authorized -and -not [bool]$C.authority.r4_planning_authorized) 'R3/R4 authority drift'
$A=Join-Path $Root 'artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3'
foreach($f in @('01-step1-suction-energy-balance.csv','02-mode2-suction-inverse-path.csv','03-forward-saturation-provider-seam.csv','04-diagnostic-summary.txt','05-pre-repair-review.txt')){Req (-not (Test-Path (Join-Path $A $f))) ('pre-existing Diagnostic 3 artifact: '+$f)}
Write-Host 'R3 Suction Energy-Transport Causal-Seam Diagnostic 3 static audit: PASS' -ForegroundColor Green
