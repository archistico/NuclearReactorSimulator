$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([IO.File]::ReadAllBytes($p)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r,[string]$exclude=''){$rr=(Resolve-Path $r).Path;$list=New-Object 'Collections.Generic.List[string]';Get-ChildItem $rr -Recurse -File|ForEach-Object{$rel=$_.FullName.Substring($rr.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj' -or $rel -eq $exclude){return};$list.Add($rel)};$a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $a){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$s=[Security.Cryptography.SHA256]::Create();try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()};@{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-contract.json' -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-v1') 'schema drift'
Req ($C.status -eq 'DIAGNOSTIC-CANDIDATE') 'candidate status drift'
$pred=Join-Path $Root ([string]$C.prerequisite.diagnostic1_manifest.path).Replace('/','\')
Req ((Sha $pred)-eq [string]$C.prerequisite.diagnostic1_manifest.sha256) 'Diagnostic 1 returned manifest drift'
$P=Get-Content $pred -Raw|ConvertFrom-Json
Req ($P.status -eq 'PASS-DIAGNOSTIC-EVIDENCE-COMPLETE') 'Diagnostic 1 predecessor status drift'
Req ($P.engineering_classification -eq 'PRECONDITIONING-INDUCED-PRIMARY-HYDRAULIC-OPERATING-POINT-DIVERGENCE') 'Diagnostic 1 engineering classification drift'
Req ($P.authority.next_authorized_activity -eq 'R3-SEED-INTEGRATION-TWO-SEED-STEP-PRECONDITIONING-DIVERGENCE-DIAGNOSTIC2') 'Diagnostic 2 authority missing'
foreach($p in $C.prerequisite.raw_evidence.PSObject.Properties){
    $path=Join-Path $Root ([string]$p.Value.path).Replace('/','\')
    Req ((Sha $path)-eq [string]$p.Value.sha256) ("raw prerequisite drift: "+$p.Name)
}
$src=Tree 'src'
Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift'
$testRel=[string]$C.files.test.path
Req ($testRel.StartsWith('tests/',[StringComparison]::Ordinal)) 'diagnostic test path must remain under tests/'
$testUnderTests=$testRel.Substring('tests/'.Length)
$hist=Tree 'tests' $testUnderTests
Req ($hist.Count -eq [int]$C.baseline.historical_tests_file_count -and $hist.Hash -eq [string]$C.baseline.historical_tests_tree_sha256) 'historical tests drift'
Req ((NSha (Join-Path $Root $testRel.Replace('/','\')))-eq [string]$C.files.test.normalized_sha256) 'Diagnostic 2 test drift'
Req ((NSha (Join-Path $Root ([string]$C.files.document.path).Replace('/','\')))-eq [string]$C.files.document.normalized_sha256) 'Diagnostic 2 document drift'
Req ((NSha (Join-Path $Root ([string]$C.files.adjudicator.path).Replace('/','\')))-eq [string]$C.files.adjudicator.normalized_sha256) 'Diagnostic 2 adjudicator drift'
Req ((NSha (Join-Path $Root ([string]$C.files.runner.path).Replace('/','\')))-eq [string]$C.files.runner.normalized_sha256) 'Diagnostic 2 runner drift'
Req ((NSha (Join-Path $Root ([string]$C.files.validator.path).Replace('/','\')))-eq [string]$C.files.validator.normalized_sha256) 'Diagnostic 2 validator drift'
Req ([int]$C.diagnostic.runtime_step_ms -eq 10) 'runtime-step drift'
Req ($C.diagnostic.raw_checkpoint_source -eq 'FROZEN-RETURNED-EVIDENCE') 'raw checkpoint provenance drift'
Req ([int]$C.diagnostic.seed_step1_count -eq 1 -and [int]$C.diagnostic.seed_step2_count -eq 2) 'seed checkpoint drift'
Req ([int]$C.diagnostic.raw_node_rows -eq 12 -and [int]$C.diagnostic.seed_node_rows_each -eq 12) 'node cardinality contract drift'
Req ([int]$C.diagnostic.hydraulic_paths_each -eq 8 -and [int]$C.diagnostic.flow_signals_each -eq 12) 'hydraulic/flow cardinality contract drift'
Req ([int]$C.diagnostic.controller_rows -eq 2) 'controller cardinality contract drift'
Req (-not [bool]$C.diagnostic.production_change_allowed -and -not [bool]$C.diagnostic.threshold_change_allowed) 'diagnostic scope drift'
Req (-not [bool]$C.authority.production_repair_authorized) 'production repair must remain blocked'
Req (-not [bool]$C.authority.seed_retuning_authorized) 'seed retuning must remain blocked'
Req (-not [bool]$C.authority.threshold_change_authorized) 'threshold change must remain blocked'
Req (-not [bool]$C.authority.c4_change_authorized) 'C4 must remain frozen'
Req (-not [bool]$C.authority.canonical_exact_v9_change_authorized) 'canonical exact-v9 must remain frozen'
Req (-not [bool]$C.authority.new_exact_version_authorized) 'new exact version must remain blocked'
Req (-not [bool]$C.authority.r3_passed) 'R3 must remain RED'
Req (-not [bool]$C.authority.r3_requalification3_authorized) 'R3 Short Requalification 3 must remain blocked'
Req (-not [bool]$C.authority.r4_planning_authorized) 'R4 must remain blocked'
Req ($C.authority.next_step_after_execution -eq 'RETURN-ARTIFACTS-FOR-ADJUDICATION') 'post-execution authority drift'
Write-Host 'R3 two-seed-step preconditioning Diagnostic 2 static audit: PASS' -ForegroundColor Green
