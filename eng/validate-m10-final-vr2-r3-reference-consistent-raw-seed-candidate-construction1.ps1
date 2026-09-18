$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-C1([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Sha-C1([byte[]]$Bytes){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-C1([string]$Path){Sha-C1 ([IO.File]::ReadAllBytes($Path))}
function NormSha-C1([string]$Path){$T=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-C1 ([Text.Encoding]::UTF8.GetBytes($T))}
function TreeSha-C1([string]$Root,[string[]]$ExcludeRel=@()){
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
$Contract=Get-Content 'eng\m10-final-vr2-r3-reference-consistent-raw-seed-candidate-construction1-contract.json' -Raw | ConvertFrom-Json

Require-C1 ($Contract.schema -eq 'm10-final-vr2-r3-reference-consistent-raw-seed-candidate-construction1-v1') 'Schema drift.'
Require-C1 ($Contract.status -eq 'EXECUTION-CANDIDATE') 'Status drift.'
Require-C1 ($Contract.prerequisite.replanning1_classification -eq 'PASS-AS-AUTHORED') 'Replanning 1 authority missing.'

$ReturnedRoot=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_ReferenceConsistentOperatingPointSeed_Replanning1_ReturnedArtifacts'
Require-C1 (@(Get-ChildItem -LiteralPath $ReturnedRoot -File).Count -eq 4) 'Returned Replanning 1 artifact count drift.'
foreach($Prop in $Contract.prerequisite.returned_replanning1_artifacts.PSObject.Properties){
    $P=Join-Path $ReturnedRoot $Prop.Name
    Require-C1 ((FileSha-C1 $P)-eq [string]$Prop.Value.sha256) ("Returned Replanning 1 artifact hash drift: {0}" -f $Prop.Name)
}
$Summary=[IO.File]::ReadAllText((Join-Path $ReturnedRoot '03-replanning-summary.txt'),[Text.Encoding]::UTF8)
Require-C1 ($Summary.Contains('next-authorized-gate=R3-REFERENCE-CONSISTENT-RAW-SEED-CANDIDATE-CONSTRUCTION1')) 'Candidate Construction 1 authority missing.'

$Src=TreeSha-C1 (Join-Path $Root 'src')
Require-C1 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drift.'
$NewRel=([string]$Contract.candidate_construction.test_file).Substring(6)
$Tests=TreeSha-C1 (Join-Path $Root 'tests') @($NewRel)
Require-C1 ($Tests.Count -eq [int]$Contract.baseline.historical_tests_file_count -and $Tests.Hash -eq [string]$Contract.baseline.historical_tests_tree_sha256) 'Historical tests tree drift.'

foreach($Name in @('cold_shutdown_factory','desktop_sustained_factory','mode2_resolver','c4_payload')){
    $Item=$Contract.baseline.$Name
    $P=Join-Path $Root ([string]$Item.path).Replace('/','\')
    Require-C1 ((FileSha-C1 $P)-eq [string]$Item.sha256) ("Frozen baseline file drift: {0}" -f $Item.path)
}

$Cold=[IO.File]::ReadAllText((Join-Path $Root ([string]$Contract.baseline.cold_shutdown_factory.path).Replace('/','\')),[Text.Encoding]::UTF8)
$Desktop=[IO.File]::ReadAllText((Join-Path $Root ([string]$Contract.baseline.desktop_sustained_factory.path).Replace('/','\')),[Text.Encoding]::UTF8)
Require-C1 ($Cold.Contains('FluidNodeDefinition Node(string id, double volumeCubicMetres = 10d)')) 'Default 10 m3 node-volume provenance marker missing.'
Require-C1 ($Desktop.Contains('exhaustSteamSpaceVolumeCubicMetres: 1_000d')) 'Exact-v9 exhaust volume marker missing.'
Require-C1 ($Desktop.Contains('pressurizedSteamPathNodeVolumeCubicMetres: 100d')) 'Exact-v9 pressurized steam-path volume marker missing.'

Require-C1 ([int]$Contract.target_state_vector.node_count -eq 12 -and $Contract.target_state_vector.nodes.Count -eq 12) 'Target-state vector cardinality drift.'
Require-C1 ([int]$Contract.candidate_construction.required_candidate_nodes -eq 12) 'Candidate-node contract drift.'
Require-C1 ([int]$Contract.candidate_construction.required_hydraulic_heads -eq 8) 'Hydraulic-head contract drift.'
Require-C1 ([int]$Contract.candidate_construction.runtime_preconditioning_steps -eq 0) 'Runtime preconditioning must remain zero.'
Require-C1 ([int]$Contract.candidate_construction.runtime_dynamic_steps -eq 0) 'Runtime dynamics must remain zero.'
Require-C1 (-not [bool]$Contract.candidate_construction.residual_acceptance_thresholds_defined) 'Candidate Construction 1 must not invent residual acceptance thresholds.'

$Test=Join-Path $Root ([string]$Contract.candidate_construction.test_file).Replace('/','\')
Require-C1 ((NormSha-C1 $Test)-eq [string]$Contract.candidate_construction.test_normalized_sha256) 'Candidate Construction 1 test drift.'

Require-C1 (-not [bool]$Contract.authority.production_source_change_authorized) 'Production source changes must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.production_seed_integration_authorized) 'Production seed integration must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.c4_resolver_change_authorized) 'C4 resolver changes must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.c4_payload_change_authorized) 'C4 payload changes must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.canonical_exact_v9_change_authorized) 'Canonical exact-v9 changes must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.new_exact_version_identity_authorized) 'New exact-version identity must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.threshold_change_authorized) 'Threshold changes must remain blocked.'
Require-C1 (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 must remain blocked.'

foreach($Prop in $Contract.documentation_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-C1 ((NormSha-C1 $P)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-C1 ((NormSha-C1 $P)-eq [string]$I.normalized_sha256) ("Gate hash drift: {0}" -f $I.path)
}

Write-Host 'R3 reference-consistent raw-seed Candidate Construction 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Test-only construction. No production integration or R4 authority.'
