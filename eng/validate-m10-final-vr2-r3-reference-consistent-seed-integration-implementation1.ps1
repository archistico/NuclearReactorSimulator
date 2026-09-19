$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$C,[string]$M){if(-not $C){throw $M}}
function Sha([byte[]]$B){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($B))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha([string]$P){Sha ([IO.File]::ReadAllBytes($P))}
function NormSha([string]$P){$T=[IO.File]::ReadAllText($P,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha ([Text.Encoding]::UTF8.GetBytes($T))}
function ProjectedSrcTree([string]$Root,$BaselineFiles){
    $RR=(Resolve-Path -LiteralPath $Root).Path
    $ByPath=@{};foreach($Prop in $BaselineFiles.PSObject.Properties){$ByPath[[string]$Prop.Value.path]=[string]$Prop.Value.baseline_sha256}
    $L=New-Object 'Collections.Generic.List[string]'
    foreach($F in @(Get-ChildItem -LiteralPath $RR -Recurse -File)){
        $Rel=$F.FullName.Substring($RR.Length+1).Replace('\','/');$Parts=$Rel.Split('/')
        if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue};$L.Add($Rel)
    }
    $L.Sort([StringComparer]::Ordinal);$M=New-Object IO.MemoryStream
    try{
        foreach($Rel in $L){
            $B=[Text.Encoding]::UTF8.GetBytes($Rel);$M.Write($B,0,$B.Length);$M.WriteByte(0)
            if($ByPath.ContainsKey('src/'+$Rel)){
                $Hex=$ByPath['src/'+$Rel]
                $FH=New-Object byte[] ($Hex.Length/2)
                for($K=0;$K -lt $FH.Length;$K++){$FH[$K]=[Convert]::ToByte($Hex.Substring($K*2,2),16)}
            }
            else{
                $H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $RR $Rel.Replace('/','\')))
                try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()}
            }
            $M.Write($FH,0,$FH.Length);$M.WriteByte(10)
        }
        $M.Position=0;$TH=[Security.Cryptography.SHA256]::Create();try{$Final=$TH.ComputeHash($M)}finally{$TH.Dispose()}
        @{Count=$L.Count;Hash=([BitConverter]::ToString($Final)).Replace('-','').ToUpperInvariant()}
    }finally{$M.Dispose()}
}
function Tree([string]$Root,[string[]]$Exclude=@()){
    $RR=(Resolve-Path -LiteralPath $Root).Path;$X=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach($E in $Exclude){[void]$X.Add($E.Replace('\','/'))}
    $L=New-Object 'Collections.Generic.List[string]'
    foreach($F in @(Get-ChildItem -LiteralPath $RR -Recurse -File)){
        $Rel=$F.FullName.Substring($RR.Length+1).Replace('\','/');$Parts=$Rel.Split('/')
        if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue};if(-not $X.Contains($Rel)){$L.Add($Rel)}
    }
    $L.Sort([StringComparer]::Ordinal);$M=New-Object IO.MemoryStream
    try{
        foreach($Rel in $L){$B=[Text.Encoding]::UTF8.GetBytes($Rel);$M.Write($B,0,$B.Length);$M.WriteByte(0);$H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $RR $Rel.Replace('/','\')));try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()};$M.Write($FH,0,$FH.Length);$M.WriteByte(10)}
        $M.Position=0;$TH=[Security.Cryptography.SHA256]::Create();try{$Final=$TH.ComputeHash($M)}finally{$TH.Dispose()}
        @{Count=$L.Count;Hash=([BitConverter]::ToString($Final)).Replace('-','').ToUpperInvariant()}
    }finally{$M.Dispose()}
}
$Root=Split-Path -Parent $PSScriptRoot;Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-reference-consistent-seed-integration-implementation1-contract.json' -Raw | ConvertFrom-Json
Req ($C.schema -eq 'm10-final-vr2-r3-reference-consistent-seed-integration-implementation1-v1') 'Schema drift.'
Req ($C.status -eq 'IMPLEMENTATION-CANDIDATE') 'Status drift.'
$Returned=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_ReferenceConsistentSeedIntegration_Planning1_ReturnedArtifacts'
Req (@(Get-ChildItem -LiteralPath $Returned -File).Count -eq 4) 'Planning artifact count drift.'
foreach($P in $C.prerequisite.returned_planning1_artifacts.PSObject.Properties){$Q=Join-Path $Returned $P.Name;Req ((FileSha $Q)-eq [string]$P.Value.sha256) ("Planning artifact drift: {0}" -f $P.Name)}

$CandidateReturnedRoot=Join-Path $Root ([string]$C.prerequisite.candidate_construction_returned_artifacts_root).Replace('/','\')
Req (Test-Path -LiteralPath $CandidateReturnedRoot -PathType Container) 'Frozen Candidate Construction returned-evidence root missing.'
$CandidateReturnedFiles=@(Get-ChildItem -LiteralPath $CandidateReturnedRoot -File)
Req ($CandidateReturnedFiles.Count -eq 5) ("Candidate Construction artifact count drift: expected 5, found {0}" -f $CandidateReturnedFiles.Count)
foreach($P in $C.prerequisite.candidate_construction_returned_artifacts.PSObject.Properties){
    $Q=Join-Path $CandidateReturnedRoot $P.Name
    Req (Test-Path -LiteralPath $Q -PathType Leaf) ("Candidate Construction artifact missing: {0}" -f $P.Name)
    Req ((FileSha $Q)-eq [string]$P.Value.sha256) ("Candidate Construction artifact drift: {0}" -f $P.Name)
}
$Projected=ProjectedSrcTree (Join-Path $Root 'src') $C.baseline.authorized_production_files
Req ($Projected.Count -eq [int]$C.baseline.src_file_count -and $Projected.Hash -eq [string]$C.baseline.src_tree_sha256) 'Source changes escaped the three-file implementation boundary.'
$NewRel=([string]$C.implementation.test_file).Substring(6)
$Tests=Tree (Join-Path $Root 'tests') @($NewRel)
Req ($Tests.Count -eq [int]$C.baseline.historical_tests_file_count -and $Tests.Hash -eq [string]$C.baseline.historical_tests_tree_sha256) 'Historical tests tree drift.'
$Test=Join-Path $Root ([string]$C.implementation.test_file).Replace('/','\');Req ((NormSha $Test)-eq [string]$C.implementation.test_normalized_sha256) 'Focused implementation test drift.'
$Desk=Join-Path $Root 'src\NuclearReactorSimulator.Application\Scenarios\Training\DesktopSustainedGenerationInitialConditionFactory.cs'
$Text=[IO.File]::ReadAllText($Desk,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n")
$Start='    internal static IControlRoomRuntimeEngine CreatePostMoistureEquilibriumCandidateRuntimeEngine(TimeSpan runtimeStep)'
$End='    private static IControlRoomRuntimeEngine CreateRuntimeEngine('
$I=$Text.IndexOf($Start,[StringComparison]::Ordinal);$J=$Text.IndexOf($End,$I,[StringComparison]::Ordinal)
Req ($I -ge 0 -and $J -gt $I) 'Canonical exact-v9 method segment markers missing.'
$Segment=$Text.Substring($I,$J-$I);$SegmentSha=Sha ([Text.Encoding]::UTF8.GetBytes($Segment))
Req ($SegmentSha -eq [string]$C.baseline.canonical_exact_v9_method_segment_normalized_sha256) 'Canonical exact-v9 method segment changed.'
Req ($Text.Contains('CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine')) 'New candidate factory missing.'
Req ($Text.Contains('ReferenceConsistentTabulatedInverseDomain')) 'Mode-2 candidate closure marker missing.'
Req ($C.frozen_candidate_vector.node_count -eq 12 -and $C.target_state_vector.node_count -eq 12) 'Frozen vector cardinality drift.'

# The returned Candidate Construction CSV is the authoritative decimal provenance for the 12-node vector.
# Do not round-trip these values through ConvertFrom-Json/Double in Windows PowerShell 5.1: its JSON-number
# materialization can choose an adjacent binary64 value for long decimal literals.  The frozen CSV itself is
# already SHA-256 protected by the returned-evidence contract, so compare the authored C# decimal tokens
# directly to those exact returned strings.
$FrozenVectorCsv=Join-Path $CandidateReturnedRoot '01-candidate-conserved-inventory-vector.csv'
Req (Test-Path -LiteralPath $FrozenVectorCsv -PathType Leaf) 'Authoritative returned candidate-vector CSV missing.'
$FrozenRows=@(Import-Csv -LiteralPath $FrozenVectorCsv)
Req ($FrozenRows.Count -eq 12) ("Expected 12 authoritative candidate-vector rows, found {0}" -f $FrozenRows.Count)

$AllCandidateMatches=[regex]::Matches(
    $Text,
    'new\s+OperationalFluidNodeSeed\.ConservedInventory\("([^"]+)",\s*([-+0-9.Ee]+)d,\s*([-+0-9.Ee]+)d,',
    [Text.RegularExpressions.RegexOptions]::CultureInvariant)
Req ($AllCandidateMatches.Count -eq 12) ("Expected exactly 12 ConservedInventory literals, found {0}" -f $AllCandidateMatches.Count)

$SeenNodes=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
foreach($Row in $FrozenRows){
    $Node=[string]$Row.node
    Req (-not [string]::IsNullOrWhiteSpace($Node)) 'Authoritative candidate-vector row has empty node id.'
    Req ($SeenNodes.Add($Node)) ("Duplicate authoritative candidate node: {0}" -f $Node)

    $EscapedNode=[regex]::Escape($Node)
    $Pattern='new\s+OperationalFluidNodeSeed\.ConservedInventory\("' + $EscapedNode + '",\s*([-+0-9.Ee]+)d,\s*([-+0-9.Ee]+)d,'
    $Match=[regex]::Match(
        $Text,
        $Pattern,
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    Req ($Match.Success) ("Candidate conserved-inventory literal missing for {0}" -f $Node)

    $MassSource=$Match.Groups[1].Value
    $EnergySource=$Match.Groups[2].Value
    $MassReturned=[string]$Row.mass_kg
    $EnergyReturned=[string]$Row.internal_energy_j

    Req ($MassSource -ceq $MassReturned) (
        "Candidate mass literal drift for {0}: source={1}, returned={2}" -f $Node,$MassSource,$MassReturned)
    Req ($EnergySource -ceq $EnergyReturned) (
        "Candidate energy literal drift for {0}: source={1}, returned={2}" -f $Node,$EnergySource,$EnergyReturned)
}
Req ($SeenNodes.Count -eq 12) 'Frozen candidate node uniqueness/cardinality drift.'
Req (-not [bool]$C.authority.r3_passed) 'R3 must remain RED.'
Req (-not [bool]$C.authority.r4_planning_authorized) 'R4 must remain blocked.'
foreach($P in $C.documentation_files.PSObject.Properties){$I2=$P.Value;$Q=Join-Path $Root ([string]$I2.path).Replace('/','\');Req ((NormSha $Q)-eq [string]$I2.normalized_sha256) ("Doc drift: {0}" -f $I2.path)}
foreach($P in $C.gate_files.PSObject.Properties){$I2=$P.Value;$Q=Join-Path $Root ([string]$I2.path).Replace('/','\');Req ((NormSha $Q)-eq [string]$I2.normalized_sha256) ("Gate drift: {0}" -f $I2.path)}
Write-Host 'R3 reference-consistent Seed Integration Implementation 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Implementation evidence only. R3 remains RED until Requalification 3.'
