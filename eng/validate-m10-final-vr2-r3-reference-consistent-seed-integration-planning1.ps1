$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require-SIP1([bool]$C,[string]$M){if(-not $C){throw $M}}
function Sha-SIP1([byte[]]$B){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($B))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-SIP1([string]$P){Sha-SIP1 ([IO.File]::ReadAllBytes($P))}
function NormSha-SIP1([string]$P){$T=[IO.File]::ReadAllText($P,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-SIP1 ([Text.Encoding]::UTF8.GetBytes($T))}
function TreeSha-SIP1([string]$R){
    $RR=(Resolve-Path -LiteralPath $R).Path;$L=New-Object 'Collections.Generic.List[string]'
    foreach($F in @(Get-ChildItem -LiteralPath $RR -Recurse -File)){
        $Rel=$F.FullName.Substring($RR.Length+1).Replace('\','/');$Parts=$Rel.Split('/')
        if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue};$L.Add($Rel)
    }
    $L.Sort([StringComparer]::Ordinal);$M=New-Object IO.MemoryStream
    try{
        foreach($Rel in $L){
            $B=[Text.Encoding]::UTF8.GetBytes($Rel);$M.Write($B,0,$B.Length);$M.WriteByte(0)
            $H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $RR $Rel.Replace('/','\')))
            try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()}
            $M.Write($FH,0,$FH.Length);$M.WriteByte(10)
        }
        $M.Position=0;$TH=[Security.Cryptography.SHA256]::Create()
        try{$Final=$TH.ComputeHash($M)}finally{$TH.Dispose()}
        @{Count=$L.Count;Hash=([BitConverter]::ToString($Final)).Replace('-','').ToUpperInvariant()}
    }finally{$M.Dispose()}
}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-reference-consistent-seed-integration-planning1-contract.json' -Raw | ConvertFrom-Json
Require-SIP1 ($Contract.schema -eq 'm10-final-vr2-r3-reference-consistent-seed-integration-planning1-v1') 'Schema drift.'
Require-SIP1 ($Contract.status -eq 'PLANNING-CANDIDATE') 'Status drift.'
Require-SIP1 ($Contract.predecessor.candidate_construction_classification -eq 'PASS-CANDIDATE-CONSTRUCTION-EVIDENCE-COMPLETE') 'Candidate Construction PASS missing.'

$ReturnedRoot=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_ReferenceConsistentRawSeed_CandidateConstruction1_ReturnedArtifacts'
Require-SIP1 (@(Get-ChildItem -LiteralPath $ReturnedRoot -File).Count -eq 5) 'Returned Candidate Construction artifact count drift.'
foreach($Prop in $Contract.predecessor.returned_candidate_construction_artifacts.PSObject.Properties){
    $P=Join-Path $ReturnedRoot $Prop.Name
    Require-SIP1 ((FileSha-SIP1 $P)-eq [string]$Prop.Value.sha256) ("Returned Candidate Construction artifact drift: {0}" -f $Prop.Name)
}
$Review=[IO.File]::ReadAllText((Join-Path $ReturnedRoot '05-pre-integration-review.txt'),[Text.Encoding]::UTF8)
Require-SIP1 ($Review.Contains('status=PASS-CANDIDATE-CONSTRUCTION-EVIDENCE-COMPLETE')) 'Candidate Construction PASS marker missing.'
Require-SIP1 ($Review.Contains('resolved-node-count=12')) '12-node resolved evidence missing.'
Require-SIP1 ($Review.Contains('phase-match-node-count=12')) '12-node phase-match evidence missing.'

$Src=TreeSha-SIP1 (Join-Path $Root 'src')
Require-SIP1 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'src baseline drift.'
$Tests=TreeSha-SIP1 (Join-Path $Root 'tests')
Require-SIP1 ($Tests.Count -eq [int]$Contract.baseline.tests_file_count -and $Tests.Hash -eq [string]$Contract.baseline.tests_tree_sha256) 'tests baseline drift.'

foreach($Prop in $Contract.baseline.production_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-SIP1 ((FileSha-SIP1 $P)-eq [string]$I.sha256) ("Frozen production file drift: {0}" -f $I.path)
}
$ExactTest=Join-Path $Root ([string]$Contract.baseline.canonical_exact_v9_test.path).Replace('/','\')
Require-SIP1 ((NormSha-SIP1 $ExactTest)-eq [string]$Contract.baseline.canonical_exact_v9_test.normalized_sha256) 'Canonical exact-v9 historical test drift.'

Require-SIP1 ([int]$Contract.frozen_candidate_vector.node_count -eq 12 -and $Contract.frozen_candidate_vector.nodes.Count -eq 12) 'Frozen candidate-vector cardinality drift.'
Require-SIP1 ($Contract.selected_integration_design.id -eq 'OPT-IN-CONSERVED-INVENTORY-SEED-SEAM') 'Integration design drift.'
Require-SIP1 ($Contract.selected_integration_design.authorized_production_files.Count -eq 3) 'Authorized production-file count drift.'
Require-SIP1 ($Contract.selected_integration_design.new_seed_subtype -eq 'OperationalFluidNodeSeed.ConservedInventory') 'ConservedInventory seam drift.'
Require-SIP1 (-not [bool]$Contract.selected_integration_design.canonical_exact_v9_method_body_change_allowed) 'Canonical exact-v9 method body must remain frozen.'
Require-SIP1 ([bool]$Contract.selected_integration_design.new_opt_in_candidate_factory_required) 'Opt-in candidate factory requirement lost.'
Require-SIP1 ($Contract.selected_integration_design.candidate_closure_mode -eq 'ReferenceConsistentTabulatedInverseDomain') 'Candidate closure mode drift.'
Require-SIP1 ([int]$Contract.selected_integration_design.candidate_fast_running_steps -eq 100) 'Fast-gate step count drift.'
Require-SIP1 ([int]$Contract.selected_integration_design.candidate_runtime_step_ms -eq 10) 'Fast-gate timestep drift.'
Require-SIP1 (-not [bool]$Contract.selected_integration_design.c4_resolver_change_allowed) 'C4 resolver changes must remain blocked.'
Require-SIP1 (-not [bool]$Contract.selected_integration_design.c4_payload_change_allowed) 'C4 payload changes must remain blocked.'
Require-SIP1 (-not [bool]$Contract.selected_integration_design.threshold_change_allowed) 'Threshold changes must remain blocked.'
Require-SIP1 (-not [bool]$Contract.authority.production_seed_integration_authorized_now) 'Production seed integration must not be pre-authorized.'
Require-SIP1 (-not [bool]$Contract.authority.canonical_exact_v9_change_authorized) 'Canonical exact-v9 changes remain blocked.'
Require-SIP1 (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 remains blocked.'
Require-SIP1 ($Contract.sequencing.planning_successor -eq 'R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-IMPLEMENTATION1') 'Planning successor drift.'
Require-SIP1 ($Contract.sequencing.after_implementation_pass -eq 'R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION3') 'Post-implementation successor drift.'

foreach($Prop in $Contract.documentation_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-SIP1 ((NormSha-SIP1 $P)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $I=$Prop.Value;$P=Join-Path $Root ([string]$I.path).Replace('/','\')
    Require-SIP1 ((NormSha-SIP1 $P)-eq [string]$I.normalized_sha256) ("Gate hash drift: {0}" -f $I.path)
}

Write-Host 'R3 reference-consistent Seed Integration Planning 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Planning only. Implementation, R3 PASS and R4 remain unauthorized.'
