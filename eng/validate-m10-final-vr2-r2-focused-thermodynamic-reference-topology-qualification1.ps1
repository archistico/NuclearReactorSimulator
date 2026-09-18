$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require([bool]$c,[string]$m){if(-not $c){throw $m}}
function Require-Text([string]$p,[string]$n){Require (Test-Path -LiteralPath $p -PathType Leaf) ("Missing file: {0}" -f $p);$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8);Require ($t.IndexOf($n,[StringComparison]::Ordinal)-ge 0) ("Missing marker in {0}: {1}" -f $p,$n)}
function ShaBytes([byte[]]$b){$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash($b))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function FileSha([string]$p){ShaBytes ([IO.File]::ReadAllBytes($p))}
function NormSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");ShaBytes ([Text.Encoding]::UTF8.GetBytes($t))}
function TreeSha([string]$root,[string[]]$excludeRel=@()){$r=(Resolve-Path $root).Path;$x=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase);foreach($e in $excludeRel){[void]$x.Add($e.Replace('\','/'))};$l=New-Object 'Collections.Generic.List[string]';foreach($f in @(Get-ChildItem $r -Recurse -File)){ $rel=$f.FullName.Substring($r.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj'){continue};if(-not $x.Contains($rel)){$l.Add($rel)}};$l.Sort([StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $l){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();$fs=[IO.File]::OpenRead((Join-Path $r $rel.Replace('/','\')));try{$fh=$h.ComputeHash($fs)}finally{$fs.Dispose();$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$h2=[Security.Cryptography.SHA256]::Create();try{$th=$h2.ComputeHash($ms)}finally{$h2.Dispose()};@{Count=$l.Count;Hash=([BitConverter]::ToString($th)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
$root=Split-Path -Parent $PSScriptRoot;Set-Location $root
$c=Get-Content 'eng\m10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification1-contract.json' -Raw|ConvertFrom-Json
Require ($c.schema -eq 'm10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification1-v1') 'R2 execution schema mismatch.'
Require ($c.status -eq 'EXECUTION-CANDIDATE') 'R2 execution status mismatch.'
Require ($c.prerequisite.planning_returned_adjudication -eq 'PASS') 'R2 Planning 1 returned adjudication missing.'
Require ([bool]$c.authority.r2_execution_authorized_now) 'R2 execution authority missing.'
foreach($p in @('production_default_switch_authorized','production_runtime_change_authorized','exact_v9_change_authorized','new_exact_version_authorized','threshold_change_authorized','r3_planning_authorized_now','vr3_authorized','p3_r1_authorized','second_replacement_long_authorized')){Require (-not [bool]$c.authority.$p) ("Unauthorized authority true: {0}" -f $p)}
$ra=Join-Path $root $c.prerequisite.planning_returned_audit.Replace('/','\');Require ((FileSha $ra)-eq $c.prerequisite.planning_returned_audit_sha256) 'Planning returned-audit hash drift.';Require-Text $ra 'status=PASS';Require-Text $ra 'r2-execution-authorized=True'
$pr=Join-Path $root $c.planning_returned_artifacts.root.Replace('/','\');$props=@($c.planning_returned_artifacts.sha256.PSObject.Properties);Require ($props.Count -eq 4) 'Planning artifact manifest count drift.';foreach($p in $props){$f=Join-Path $pr $p.Name;Require ((FileSha $f)-eq [string]$p.Value) ("Planning artifact hash drift: {0}" -f $p.Name)}
$src=TreeSha (Join-Path $root 'src');Require ($src.Count -eq [int]$c.baseline.src_file_count -and $src.Hash -eq $c.baseline.src_tree_sha256) 'Production src tree drift.'
$testRel=[string]$c.execution.new_test_file;$hist=TreeSha (Join-Path $root 'tests') @($testRel.Substring('tests/'.Length));Require ($hist.Count -eq [int]$c.baseline.tests_file_count -and $hist.Hash -eq $c.baseline.tests_tree_sha256) 'Historical tests tree drift.'
$test=Join-Path $root $testRel.Replace('/','\');Require ((NormSha $test)-eq $c.execution.new_test_normalized_sha256) 'R2 focused test hash drift.'
Require-Text $test 'ReferenceConsistentTabulatedInverseDomain'
Require-Text $test 'IapwsIf97Reference'
Require-Text $test 'Assert.Equal(40, vr2Rows.Count)'
Require-Text $test 'Assert.Equal(360, exactRows.Count)'
Require-Text $test 'Assert.Equal(1_280, seamRows.Count)'
Require-Text $test 'ExistingVr2BlockingCeilingFraction = 0.25d'
Require-Text $test 'PlanningTargetFraction = 0.10d'
Require-Text $test 'exact-v9-composition-executed=False'
Require (-not ([IO.File]::ReadAllText($test).Contains('Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate'))) 'C4 shadow may not be R2 oracle.'
$prod=@{simulation_project='src\NuclearReactorSimulator.Simulation\NuclearReactorSimulator.Simulation.csproj';closure_mode='src\NuclearReactorSimulator.Simulation\Physics\Fluids\WaterSteamThermodynamicClosureMode.cs';production_model='src\NuclearReactorSimulator.Simulation\Physics\Fluids\SimplifiedWaterSteamThermodynamicModel.cs';mode2_resolver='src\NuclearReactorSimulator.Simulation\Physics\Fluids\ReferenceConsistentTabulatedInverseResolver.cs';payload='src\NuclearReactorSimulator.Simulation\Physics\Fluids\ReferenceData\NRSVR2C4.v1.bin'};foreach($k in $prod.Keys){Require ((FileSha (Join-Path $root $prod[$k]))-eq [string]$c.baseline.production_files_sha256.$k) ("Production hash drift: {0}" -f $k)}
$ref=Join-Path $root $c.reference_contract.helper.Replace('/','\');Require ((FileSha $ref)-eq $c.baseline.reference_helper_sha256) 'IF97 helper drift.'
foreach($x in @(@($c.frozen_corpora.vr2_reference.path,$c.frozen_corpora.vr2_reference.sha256),@($c.frozen_corpora.exact_v9_nodes.path,$c.frozen_corpora.exact_v9_nodes.sha256),@($c.frozen_corpora.seam_map.path,$c.frozen_corpora.seam_map.sha256),@($c.frozen_corpora.hydraulic_context.path,$c.frozen_corpora.hydraulic_context.sha256))){Require ((FileSha (Join-Path $root ([string]$x[0]).Replace('/','\')))-eq [string]$x[1]) 'Frozen corpus hash drift.'}
Require ([double]$c.execution.reference_selfcheck_max_relative_error -eq 1e-8) 'Self-check threshold drift.';Require ([double]$c.execution.vr2_blocking_max_relative_error -eq .25) '25% ceiling drift.';Require ([double]$c.execution.planning1_pressure_target_max_relative_error -eq .10) '10% target drift.'
Require (-not [bool]$c.execution.scenario_execution_allowed -and -not [bool]$c.execution.exact_v9_composition_allowed -and -not [bool]$c.execution.hydraulic_long_materiality_execution_allowed) 'Execution boundary drift.'
foreach($p in $c.documentation_files.PSObject.Properties){$f=Join-Path $root ([string]$p.Value.path).Replace('/','\');Require ((NormSha $f)-eq [string]$p.Value.normalized_sha256) ("R2 execution doc hash drift: {0}" -f $p.Name)}
foreach($p in $c.gate_files.PSObject.Properties){$f=Join-Path $root ([string]$p.Value.path).Replace('/','\');Require ((NormSha $f)-eq [string]$p.Value.normalized_sha256) ("R2 execution gate hash drift: {0}" -f $p.Name)}
Write-Host 'R2 focused thermodynamic/reference/topology execution static audit: PASS' -ForegroundColor Green
Write-Host 'No default/exact-v9/R3 authority is granted.'
