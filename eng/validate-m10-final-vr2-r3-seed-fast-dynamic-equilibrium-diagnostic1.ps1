$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([IO.File]::ReadAllBytes($p)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r,[string]$exclude=''){$rr=(Resolve-Path $r).Path;$list=New-Object 'Collections.Generic.List[string]';Get-ChildItem $rr -Recurse -File|ForEach-Object{$rel=$_.FullName.Substring($rr.Length+1).Replace('\','/');$parts=$rel.Split('/');if($parts -contains 'bin' -or $parts -contains 'obj' -or $rel -eq $exclude){return};$list.Add($rel)};$a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal);$ms=New-Object IO.MemoryStream;try{foreach($rel in $a){$b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0);$h=[Security.Cryptography.SHA256]::Create();try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()};$ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)};$ms.Position=0;$s=[Security.Cryptography.SHA256]::Create();try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()};@{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}}finally{$ms.Dispose()}}
$Root=Split-Path -Parent $PSScriptRoot;Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-seed-fast-dynamic-equilibrium-diagnostic1-contract.json' -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-seed-fast-dynamic-equilibrium-diagnostic1-v1') 'schema drift'
$fr=Join-Path $Root ([string]$C.prerequisite.failed_fast_evidence_root).Replace('/','\');Req (@(Get-ChildItem $fr -File).Count -eq 2) 'failed fast evidence count drift'
foreach($p in $C.prerequisite.failed_fast_evidence.PSObject.Properties){Req ((Sha (Join-Path $fr $p.Name))-eq [string]$p.Value.sha256) ("failed evidence drift: "+$p.Name)}
$raw=@(Import-Csv (Join-Path $fr '03-raw-seed-roundtrip.csv'));$h=@(Import-Csv (Join-Path $fr '04-fast-100-step-health.csv'))
Req ($raw.Count -eq 12) 'raw row count drift';Req (@($raw|Where-Object phase_match -ne 'true').Count -eq 0) 'raw phase mismatch';Req ($h.Count -eq 100) 'health row count drift';Req (@($h|Where-Object in_envelope -eq 'false').Count -eq 86) '86-step RED evidence drift'
$src=Tree 'src';Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift'
$testRel=[string]$C.files.test.path
Req ($testRel.StartsWith('tests/',[StringComparison]::Ordinal)) 'diagnostic test path must remain under tests/'
$testRelUnderTests=$testRel.Substring('tests/'.Length)
$hist=Tree 'tests' $testRelUnderTests
Req ($hist.Count -eq [int]$C.baseline.historical_tests_file_count -and $hist.Hash -eq [string]$C.baseline.historical_tests_tree_sha256) 'historical tests drift'
Req ((NSha (Join-Path $Root $testRel.Replace('/','\')))-eq [string]$C.files.test.normalized_sha256) 'diagnostic test drift'
Req (-not [bool]$C.diagnostic.production_change_allowed) 'production must remain frozen';Req (-not [bool]$C.diagnostic.threshold_change_allowed) 'thresholds must remain frozen';Req (-not [bool]$C.diagnostic.requalification3_authorized) 'R3-3 must remain blocked';Req (-not [bool]$C.diagnostic.r4_authorized) 'R4 must remain blocked'
Write-Host 'R3 seed fast dynamic-equilibrium diagnostic static audit: PASS' -ForegroundColor Green
