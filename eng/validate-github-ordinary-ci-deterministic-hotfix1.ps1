$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function Sha([string]$p){$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([IO.File]::ReadAllBytes($p)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function NSha([string]$p){$t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");$s=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($t)))).Replace('-','').ToUpperInvariant()}finally{$s.Dispose()}}
function Tree([string]$r){
    $rr=(Resolve-Path $r).Path
    $list=New-Object 'Collections.Generic.List[string]'
    Get-ChildItem $rr -Recurse -File|ForEach-Object{
        $rel=$_.FullName.Substring($rr.Length+1).Replace('\','/')
        $parts=$rel.Split('/')
        if($parts -contains 'bin' -or $parts -contains 'obj'){return}
        $list.Add($rel)
    }
    $a=$list.ToArray();[Array]::Sort($a,[StringComparer]::Ordinal)
    $ms=New-Object IO.MemoryStream
    try{
        foreach($rel in $a){
            $b=[Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($b,0,$b.Length);$ms.WriteByte(0)
            $h=[Security.Cryptography.SHA256]::Create()
            try{$fh=$h.ComputeHash([IO.File]::ReadAllBytes((Join-Path $rr $rel.Replace('/','\'))))}finally{$h.Dispose()}
            $ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)
        }
        $ms.Position=0;$s=[Security.Cryptography.SHA256]::Create()
        try{$x=$s.ComputeHash($ms)}finally{$s.Dispose()}
        @{Count=$a.Count;Hash=([BitConverter]::ToString($x)).Replace('-','').ToUpperInvariant()}
    }finally{$ms.Dispose()}
}
function ReadKv([string]$p){
    $d=@{}
    foreach($line in [IO.File]::ReadAllLines($p,[Text.Encoding]::UTF8)){
        if([string]::IsNullOrWhiteSpace($line) -or -not $line.Contains('=')){continue}
        $i=$line.IndexOf('=');$d[$line.Substring(0,$i)]=$line.Substring($i+1)
    }
    $d
}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ContractPath=Join-Path $Root 'eng\github-ordinary-ci-deterministic-hotfix1-contract.json'
$C=Get-Content $ContractPath -Raw|ConvertFrom-Json
Req ($C.schema -eq 'github-ordinary-ci-deterministic-serialization-hotfix1-v1') 'CI hotfix schema drift'
Req ($C.status -eq 'EXECUTION-CANDIDATE') 'CI hotfix status drift'

$src=Tree 'src'
Req ($src.Count -eq [int]$C.baseline.src_file_count -and $src.Hash -eq [string]$C.baseline.src_tree_sha256) 'src drift: CI hotfix must not change production source'
$tests=Tree 'tests'
Req ($tests.Count -eq [int]$C.baseline.tests_file_count -and $tests.Hash -eq [string]$C.baseline.tests_tree_sha256) 'tests drift: CI hotfix must not change test source'

foreach($name in @('ci_ordinary','workflow','document','runner')){
    $f=$C.files.$name
    $path=Join-Path $Root ([string]$f.path).Replace('/','\')
    Req (Test-Path $path -PathType Leaf) ("required file missing: "+$name)
    Req ((NSha $path)-eq [string]$f.normalized_sha256) ("required file drift: "+$name)
}

$ci=[IO.File]::ReadAllText((Join-Path $Root 'eng\ci-ordinary.cmd'),[Text.Encoding]::UTF8)
$expected=[string]$C.deterministic_ci.expected_dotnet_test_command
Req ($ci.Contains('set "CI=true"')) 'CI=true must be intrinsic to ci-ordinary.cmd'
Req ($ci.Contains($expected)) 'deterministic dotnet test command drift'
Req ($ci.Contains('validate-github-ordinary-ci-deterministic-hotfix1.ps1')) 'CI hotfix validator must run from ci-ordinary.cmd'
Req ($ci.Contains('dotnet restore || exit /b 1')) 'restore fail-closed contract drift'
Req ($ci.Contains('dotnet build --configuration Release --no-restore || exit /b 1')) 'build fail-closed contract drift'
Req ($ci.Contains('call eng\ci-current-evidence.cmd || exit /b 1')) 'current evidence fail-closed contract drift'
Req (-not $ci.Contains('--filter')) 'test filter is forbidden in ordinary CI hotfix'
Req (-not $ci.Contains('continue-on-error')) 'continue-on-error is forbidden in ordinary CI hotfix'
Req (-not $ci.Contains('|| ver >nul')) 'failure suppression is forbidden in ordinary CI hotfix'
Req (-not $ci.Contains('retry')) 'retry semantics are forbidden in ordinary CI hotfix'

$wf=[IO.File]::ReadAllText((Join-Path $Root '.github\workflows\ordinary-ci.yml'),[Text.Encoding]::UTF8)
Req ($wf.Contains('runs-on: windows-latest')) 'hosted runner drift'
Req ($wf.Contains("CI: 'true'")) 'hosted CI environment drift'
Req ($wf.Contains('uses: actions/checkout@v7')) 'checkout action drift'
Req ($wf.Contains('uses: actions/setup-dotnet@v6')) 'setup-dotnet action drift'
Req ($wf.Contains('global-json-file: global.json')) 'global.json workflow binding drift'
Req ($wf.Contains('run: eng\ci-ordinary.cmd')) 'workflow ordinary entry point drift'
Req (-not $wf.Contains('continue-on-error: true')) 'hosted continue-on-error forbidden'

Req ([int]$C.observed_hosted_failure.total_tests -eq 1525) 'observed hosted total drift'
Req ([int]$C.observed_hosted_failure.failed_tests -eq 1) 'observed hosted failure-count drift'
Req ($C.observed_hosted_failure.failed_assembly -eq 'NuclearReactorSimulator.Application.Tests') 'observed hosted failing assembly drift'
Req (-not [bool]$C.observed_hosted_failure.exact_failed_test_known) 'exact failing test must remain unknown until returned detailed evidence names it'
Req (-not [bool]$C.observed_hosted_failure.root_cause_proven) 'parallelism must not be recorded as proven root cause'

$FRoot=Join-Path $Root ([string]$C.diagnostic2_returned_evidence.frozen_path).Replace('/','\')
Req (Test-Path $FRoot -PathType Container) 'Diagnostic 2 returned frozen-evidence directory missing'
foreach($p in $C.diagnostic2_returned_evidence.files.PSObject.Properties){
    $path=Join-Path $FRoot $p.Name
    Req (Test-Path $path -PathType Leaf) ("Diagnostic 2 returned evidence missing: "+$p.Name)
    Req ((Sha $path)-eq [string]$p.Value.sha256) ("Diagnostic 2 returned evidence drift: "+$p.Name)
    Req ((Get-Item $path).Length -eq [long]$p.Value.bytes) ("Diagnostic 2 returned evidence length drift: "+$p.Name)
}
$S7=ReadKv (Join-Path $FRoot '07-diagnostic-summary.txt')
$S8=ReadKv (Join-Path $FRoot '08-pre-repair-review.txt')
$S9=ReadKv (Join-Path $FRoot '09-adjudicator-hotfix1-record.txt')
Req ($S7['status'] -eq [string]$C.diagnostic2_returned_evidence.status_07) 'Diagnostic 2 summary status drift'
Req ($S8['status'] -eq [string]$C.diagnostic2_returned_evidence.status_08) 'Diagnostic 2 review status drift'
Req ($S9['status'] -eq [string]$C.diagnostic2_returned_evidence.status_09) 'Diagnostic 2 hotfix record status drift'
Req ($S7['first-phase-divergence-checkpoint'] -eq 'seed-step1') 'Diagnostic 2 first phase divergence checkpoint drift'
Req ($S7['seed-step1-phase-mismatch-nodes'] -eq 'suction') 'Diagnostic 2 first phase divergence node drift'
Req ($S7['r3-remains-red'] -eq 'True') 'R3 evidence status drift'
Req ($S8['production-repair-authorized'] -eq 'False') 'Diagnostic 2 must not authorize production repair'
Req ($S8['r4-remains-blocked'] -eq 'True') 'Diagnostic 2 must keep R4 blocked'

Req (-not [bool]$C.authority.r3_passed) 'R3 must remain RED'
Req (-not [bool]$C.authority.production_repair_authorized) 'production repair must remain blocked'
Req (-not [bool]$C.authority.seed_retuning_authorized) 'seed retuning must remain blocked'
Req (-not [bool]$C.authority.threshold_change_authorized) 'threshold changes must remain blocked'
Req (-not [bool]$C.authority.c4_change_authorized) 'C4 must remain frozen'
Req (-not [bool]$C.authority.canonical_exact_v9_change_authorized) 'canonical exact-v9 must remain frozen'
Req (-not [bool]$C.authority.r3_requalification3_authorized) 'R3 Requalification 3 must remain blocked'
Req (-not [bool]$C.authority.r4_planning_authorized) 'R4 must remain blocked'
Write-Host 'GitHub Ordinary CI Deterministic Serialization Hotfix 1 static audit: PASS' -ForegroundColor Green
