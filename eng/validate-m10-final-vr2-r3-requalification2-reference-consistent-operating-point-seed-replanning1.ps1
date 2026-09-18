$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-RP1([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Sha-RP1([byte[]]$Bytes){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-RP1([string]$Path){Sha-RP1 ([IO.File]::ReadAllBytes($Path))}
function NormSha-RP1([string]$Path){$T=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-RP1 ([Text.Encoding]::UTF8.GetBytes($T))}
function TreeSha-RP1([string]$Root){
    $Resolved=(Resolve-Path -LiteralPath $Root).Path
    $List=New-Object 'Collections.Generic.List[string]'
    foreach($F in @(Get-ChildItem -LiteralPath $Resolved -Recurse -File)){
        $Rel=$F.FullName.Substring($Resolved.Length+1).Replace('\','/')
        $Parts=$Rel.Split('/')
        if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue}
        $List.Add($Rel)
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
$Contract=Get-Content 'eng\m10-final-vr2-r3-requalification2-reference-consistent-operating-point-seed-replanning1-contract.json' -Raw | ConvertFrom-Json

Require-RP1 ($Contract.schema -eq 'm10-final-vr2-r3-requalification2-reference-consistent-operating-point-seed-replanning1-v1') 'Schema drift.'
Require-RP1 ($Contract.status -eq 'PLANNING-CANDIDATE') 'Status drift.'
Require-RP1 ($Contract.predecessor.diagnostic2_classification -eq 'LEGACY-FORWARD-MODE2-INVERSE-SEED-CONSISTENCY-GAP') 'Diagnostic 2 classification drift.'

$ReturnedRoot=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_Requalification2_AuthoredSeedForwardInverseConsistency_Diagnostic2_ReturnedArtifacts'
Require-RP1 (@(Get-ChildItem -LiteralPath $ReturnedRoot -File).Count -eq 4) 'Returned Diagnostic 2 artifact count drift.'
foreach($Prop in $Contract.predecessor.returned_diagnostic2_artifacts.PSObject.Properties){
    $P=Join-Path $ReturnedRoot $Prop.Name
    Require-RP1 (Test-Path -LiteralPath $P -PathType Leaf) ("Missing returned Diagnostic 2 artifact: {0}" -f $Prop.Name)
    Require-RP1 ((FileSha-RP1 $P)-eq [string]$Prop.Value.sha256) ("Returned Diagnostic 2 artifact hash drift: {0}" -f $Prop.Name)
}

$Summary=[IO.File]::ReadAllText((Join-Path $ReturnedRoot '03-diagnostic-summary.txt'),[Text.Encoding]::UTF8)
Require-RP1 ($Summary.Contains('classification=LEGACY-FORWARD-MODE2-INVERSE-SEED-CONSISTENCY-GAP')) 'Returned classification evidence missing.'
Require-RP1 ($Summary.Contains('forward-property-providers-bitwise-equal=True')) 'Forward provider equality evidence missing.'
Require-RP1 ($Summary.Contains('raw-seed-mode1-mode2-inverse-difference-nodes=12')) '12-node inverse divergence evidence missing.'
Require-RP1 ($Summary.Contains('raw-seed-phase-difference-nodes=2')) 'Phase divergence evidence missing.'

$Src=TreeSha-RP1 (Join-Path $Root 'src')
Require-RP1 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drift.'
$Tests=TreeSha-RP1 (Join-Path $Root 'tests')
Require-RP1 ($Tests.Count -eq [int]$Contract.baseline.tests_file_count -and $Tests.Hash -eq [string]$Contract.baseline.tests_tree_sha256) 'Tests tree drift.'

Require-RP1 ([int]$Contract.target_state_vector.node_count -eq 12) 'Target node count drift.'
Require-RP1 ($Contract.target_state_vector.nodes.Count -eq 12) 'Target vector cardinality drift.'
$Ids=@($Contract.target_state_vector.nodes | ForEach-Object { [string]$_.node })
Require-RP1 (($Ids | Sort-Object -Unique).Count -eq 12) 'Target node IDs are not unique.'

Require-RP1 ($Contract.selected_next_gate.id -eq 'R3-REFERENCE-CONSISTENT-RAW-SEED-CANDIDATE-CONSTRUCTION1') 'Next gate drift.'
Require-RP1 ($Contract.selected_next_gate.scope -eq 'TEST-ONLY-EVIDENCE-CONSTRUCTION') 'Next gate scope drift.'
Require-RP1 ($Contract.selected_next_gate.language -eq 'C#') 'Candidate construction language drift.'
Require-RP1 ([int]$Contract.selected_next_gate.runtime_dynamic_steps -eq 0) 'Candidate Construction 1 must not run dynamics.'
Require-RP1 (-not [bool]$Contract.selected_next_gate.production_source_changes_allowed) 'Production source changes must remain blocked.'
Require-RP1 (-not [bool]$Contract.selected_next_gate.c4_resolver_change_allowed) 'C4 resolver changes must remain blocked.'
Require-RP1 (-not [bool]$Contract.selected_next_gate.c4_payload_change_allowed) 'C4 payload changes must remain blocked.'
Require-RP1 (-not [bool]$Contract.selected_next_gate.canonical_exact_v9_change_allowed) 'Canonical exact-v9 changes must remain blocked.'
Require-RP1 (-not [bool]$Contract.selected_next_gate.new_exact_version_identity_allowed) 'New exact-version identity must remain blocked.'
Require-RP1 (-not [bool]$Contract.selected_next_gate.threshold_change_allowed) 'Threshold changes must remain blocked.'
Require-RP1 (-not [bool]$Contract.authority.production_seed_repair_authorized) 'Production seed repair must remain blocked.'
Require-RP1 (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 must remain blocked.'

foreach($Prop in $Contract.documentation_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-RP1 ((NormSha-RP1 $P)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-RP1 ((NormSha-RP1 $P)-eq [string]$I.normalized_sha256) ("Gate hash drift: {0}" -f $I.path)
}

Write-Host 'R3-2 reference-consistent operating-point seed Replanning 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Planning only. Candidate Construction 1 remains test-only; no production seed repair or R4 authority.'
