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
$Contract=Get-Content 'eng\m10-final-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2-contract.json' -Raw | ConvertFrom-Json
Require-D2 ($Contract.schema -eq 'm10-final-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2-v1') 'Schema drift.'
Require-D2 ($Contract.status -eq 'DIAGNOSTIC-CANDIDATE') 'Status drift.'
Require-D2 ($Contract.predecessor.diagnostic1_classification -eq 'SEED-INVENTORY-DIVERGENCE') 'Diagnostic 1 classification drift.'
Require-D2 ($Contract.predecessor.diagnostic1_refinement -eq 'LOGICAL-STEP0-IS-AFTER-TWO-DETERMINISTIC-SEED-PRECONDITIONING-STEPS') 'Diagnostic 1 refinement drift.'

$ReturnedRoot=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_Requalification2_InitialClosureOperatingPointDisplacement_Diagnostic1_ReturnedArtifacts'
Require-D2 (@(Get-ChildItem -LiteralPath $ReturnedRoot -File).Count -eq 5) 'Returned Diagnostic 1 artifact count drift.'
foreach($Prop in $Contract.predecessor.returned_diagnostic1_artifacts.PSObject.Properties){
    $P=Join-Path $ReturnedRoot $Prop.Name
    Require-D2 ((FileSha-D2 $P)-eq [string]$Prop.Value.sha256) ("Returned Diagnostic 1 artifact hash drift: {0}" -f $Prop.Name)
}

$Src=TreeSha-D2 (Join-Path $Root 'src')
Require-D2 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drift.'
$DiagRel=([string]$Contract.diagnostic.test_file).Substring(6)
$Tests=TreeSha-D2 (Join-Path $Root 'tests') @($DiagRel)
Require-D2 ($Tests.Count -eq [int]$Contract.baseline.historical_tests_file_count -and $Tests.Hash -eq [string]$Contract.baseline.historical_tests_tree_sha256) 'Historical tests tree drift.'
$Diag1=Join-Path $Root ([string]$Contract.baseline.diagnostic1_test).Replace('/','\')
Require-D2 ((NormSha-D2 $Diag1)-eq [string]$Contract.baseline.diagnostic1_test_normalized_sha256) 'Diagnostic 1 test drift.'
$Diag2=Join-Path $Root ([string]$Contract.diagnostic.test_file).Replace('/','\')
Require-D2 ((NormSha-D2 $Diag2)-eq [string]$Contract.diagnostic.test_normalized_sha256) 'Diagnostic 2 test drift.'

Require-D2 ([int]$Contract.diagnostic.dynamic_steps -eq 0) 'Diagnostic 2 must not run dynamics.'
Require-D2 (-not [bool]$Contract.diagnostic.deterministic_seed_preconditioning_included) 'Diagnostic 2 must exclude preconditioning.'
Require-D2 (-not [bool]$Contract.authority.production_repair_authorized) 'Production repair must remain blocked.'
Require-D2 (-not [bool]$Contract.authority.seed_repair_authorized) 'Seed repair must remain blocked.'
Require-D2 (-not [bool]$Contract.authority.new_exact_version_identity_authorized) 'New exact-version identity must remain blocked.'
Require-D2 (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 must remain blocked.'

foreach($Prop in $Contract.documentation_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-D2 ((NormSha-D2 $P)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-D2 ((NormSha-D2 $P)-eq [string]$I.normalized_sha256) ("Gate hash drift: {0}" -f $I.path)
}
Write-Host 'R3-2 authored-seed forward/inverse consistency Diagnostic 2 static audit: PASS' -ForegroundColor Green
Write-Host 'Diagnostic only. No repair or new exact-version authority.'
