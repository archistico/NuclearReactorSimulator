$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-D2([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Sha-D2([byte[]]$Bytes){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-D2([string]$Path){Sha-D2 ([IO.File]::ReadAllBytes($Path))}
function NormSha-D2([string]$Path){$T=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-D2 ([Text.Encoding]::UTF8.GetBytes($T))}
function TreeSha-D2([string]$Root,[string[]]$ExcludeRel=@()){
    $Resolved=(Resolve-Path -LiteralPath $Root).Path
    $Ex=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach($E in $ExcludeRel){[void]$Ex.Add($E.Replace('\','/'))}
    $List=New-Object 'Collections.Generic.List[string]'
    foreach($F in @(Get-ChildItem -LiteralPath $Resolved -Recurse -File)){
        $Rel=$F.FullName.Substring($Resolved.Length+1).Replace('\','/')
        $Parts=$Rel.Split('/')
        if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue}
        if(-not $Ex.Contains($Rel)){$List.Add($Rel)}
    }
    $List.Sort([StringComparer]::Ordinal)
    $Ms=New-Object IO.MemoryStream
    try{
        foreach($Rel in $List){
            $B=[Text.Encoding]::UTF8.GetBytes($Rel);$Ms.Write($B,0,$B.Length);$Ms.WriteByte(0)
            $H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $Resolved $Rel.Replace('/','\')))
            try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()}
            $Ms.Write($FH,0,$FH.Length);$Ms.WriteByte(10)
        }
        $Ms.Position=0;$TH=[Security.Cryptography.SHA256]::Create()
        try{$Final=$TH.ComputeHash($Ms)}finally{$TH.Dispose()}
        @{Count=$List.Count;Hash=([BitConverter]::ToString($Final)).Replace('-','').ToUpperInvariant()}
    }finally{$Ms.Dispose()}
}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1-contract.json' -Raw | ConvertFrom-Json

Require-D2 ($Contract.schema -eq 'm10-final-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1-v1') 'Diagnostic schema drift.'
Require-D2 ($Contract.status -eq 'DIAGNOSTIC-CANDIDATE') 'Diagnostic status drift.'
Require-D2 ($Contract.returned_r3_requalification2.classification -eq 'R3-SHADOW-COMPOSITION-BLOCKING') 'Returned R3-2 classification drift.'
Require-D2 ([int]$Contract.returned_r3_requalification2.health_envelope_violation_steps -eq 12000) 'Returned health violation count drift.'
Require-D2 ([int]$Contract.returned_r3_requalification2.non_finite_steps -eq 0) 'Returned non-finite count drift.'
Require-D2 ([bool]$Contract.returned_r3_requalification2.deterministic_repeat_pass) 'Returned deterministic-repeat PASS missing.'

$ReturnedRoot=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_Requalification2_FailureArtifacts'
$ReturnedFiles=@(Get-ChildItem -LiteralPath $ReturnedRoot -File)
Require-D2 ($ReturnedFiles.Count -eq 4) 'Returned R3-2 failure artifact count drift.'
foreach($Prop in $Contract.returned_r3_requalification2.returned_artifacts.PSObject.Properties){
    $P=Join-Path $ReturnedRoot $Prop.Name
    Require-D2 (Test-Path -LiteralPath $P -PathType Leaf) ("Missing returned R3-2 artifact: {0}" -f $Prop.Name)
    Require-D2 ((FileSha-D2 $P)-eq [string]$Prop.Value.sha256) ("Returned R3-2 artifact hash drift: {0}" -f $Prop.Name)
}

$BaselineCsv=Import-Csv -LiteralPath (Join-Path $ReturnedRoot '02-shadow-baseline-equivalence.csv')
Require-D2 ($BaselineCsv.Count -eq 128) 'Returned baseline row count drift.'
Require-D2 (@($BaselineCsv | Where-Object { $_.match -ne 'true' }).Count -eq 0) 'Returned canonical-v9 baseline mismatch.'
$Trajectory=Import-Csv -LiteralPath (Join-Path $ReturnedRoot '03-mode2-shadow-health-trajectory.csv')
Require-D2 ($Trajectory.Count -eq 120) 'Returned trajectory row count drift.'
Require-D2 (@($Trajectory | Where-Object { $_.finite -ne 'true' }).Count -eq 0) 'Returned trajectory contains non-finite sample.'
Require-D2 (@($Trajectory | Where-Object { $_.in_envelope -eq 'true' }).Count -eq 0) 'Returned trajectory unexpectedly contains an in-envelope sample.'

$Src=TreeSha-D2 (Join-Path $Root 'src')
Require-D2 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drift.'

$DiagRel=([string]$Contract.diagnostic.test_file).Substring(6)
$Tests=TreeSha-D2 (Join-Path $Root 'tests') @($DiagRel)
Require-D2 ($Tests.Count -eq [int]$Contract.baseline.historical_tests_file_count -and $Tests.Hash -eq [string]$Contract.baseline.historical_tests_tree_sha256) 'Historical tests tree drift.'

$R3Test=Join-Path $Root ([string]$Contract.baseline.r3_requalification2_test).Replace('/','\')
Require-D2 ((NormSha-D2 $R3Test)-eq [string]$Contract.baseline.r3_requalification2_test_normalized_sha256) 'R3 Requalification 2 focused test drift.'
$DiagTest=Join-Path $Root ([string]$Contract.diagnostic.test_file).Replace('/','\')
Require-D2 ((NormSha-D2 $DiagTest)-eq [string]$Contract.diagnostic.test_normalized_sha256) 'Diagnostic test drift.'

Require-D2 (-not [bool]$Contract.authority.production_repair_authorized) 'Diagnostic must not authorize production repair.'
Require-D2 (-not [bool]$Contract.authority.threshold_change_authorized) 'Diagnostic must not authorize threshold changes.'
Require-D2 (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 must remain blocked.'

foreach($Prop in $Contract.documentation_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-D2 ((NormSha-D2 $P)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-D2 ((NormSha-D2 $P)-eq [string]$I.normalized_sha256) ("Gate hash drift: {0}" -f $I.path)
}

Write-Host 'R3-2 initial closure / operating-point displacement Diagnostic 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Diagnostic only. R3 remains RED and R4 remains blocked.'
