$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-D1([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Sha-D1([byte[]]$Bytes){$Sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($Sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant()}finally{$Sha.Dispose()}}
function FileSha-D1([string]$Path){Sha-D1 ([IO.File]::ReadAllBytes($Path))}
function NormSha-D1([string]$Path){$Text=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-D1 ([Text.Encoding]::UTF8.GetBytes($Text))}
function TreeSha-D1([string]$Root,[string[]]$ExcludeRel=@()){
    $Resolved=(Resolve-Path -LiteralPath $Root).Path
    $Exclude=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach($Entry in $ExcludeRel){[void]$Exclude.Add($Entry.Replace('\','/'))}
    $List=New-Object 'Collections.Generic.List[string]'
    foreach($File in @(Get-ChildItem -LiteralPath $Resolved -Recurse -File)){
        $Rel=$File.FullName.Substring($Resolved.Length+1).Replace('\','/')
        $Parts=$Rel.Split('/')
        if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue}
        if(-not $Exclude.Contains($Rel)){$List.Add($Rel)}
    }
    $List.Sort([StringComparer]::Ordinal)
    $Ms=New-Object IO.MemoryStream
    try{
        foreach($Rel in $List){
            $Bytes=[Text.Encoding]::UTF8.GetBytes($Rel);$Ms.Write($Bytes,0,$Bytes.Length);$Ms.WriteByte(0)
            $Hash=[Security.Cryptography.SHA256]::Create()
            $Fs=[IO.File]::OpenRead((Join-Path $Resolved $Rel.Replace('/','\')))
            try{$FileHash=$Hash.ComputeHash($Fs)}finally{$Fs.Dispose();$Hash.Dispose()}
            $Ms.Write($FileHash,0,$FileHash.Length);$Ms.WriteByte(10)
        }
        $Ms.Position=0
        $TreeHash=[Security.Cryptography.SHA256]::Create()
        try{$Final=$TreeHash.ComputeHash($Ms)}finally{$TreeHash.Dispose()}
        @{Count=$List.Count;Hash=([BitConverter]::ToString($Final)).Replace('-','').ToUpperInvariant()}
    }finally{$Ms.Dispose()}
}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1-contract.json' -Raw | ConvertFrom-Json

Require-D1 ($Contract.schema -eq 'm10-final-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1-v1') 'Diagnostic schema mismatch.'
Require-D1 ($Contract.status -eq 'FAILURE-DIAGNOSTIC-CANDIDATE') 'Diagnostic status mismatch.'
Require-D1 ($Contract.returned_failure.classification -eq 'R3-SHADOW-COMPOSITION-BLOCKING') 'Returned R3 failure classification drift.'
Require-D1 ($Contract.returned_failure.node_id -eq 'turbine-inlet') 'Returned failure node drift.'
Require-D1 ([double]$Contract.returned_failure.specific_volume_m3_kg -eq 0.048580627845180926) 'Returned failure specific volume drift.'
Require-D1 ([double]$Contract.returned_failure.specific_internal_energy_j_kg -eq 2525533.2846314958) 'Returned failure specific energy drift.'
Require-D1 (-not [bool]$Contract.diagnostic.production_repair_allowed) 'Diagnostic must not authorize production repair.'
Require-D1 (-not [bool]$Contract.authority.production_change_authorized) 'Production change authority must remain false.'
Require-D1 (-not [bool]$Contract.authority.r4_planning_authorized) 'R4 planning must remain blocked.'

$FailureCsv=Join-Path $Root ([string]$Contract.returned_failure.baseline_equivalence_artifact).Replace('/','\')
Require-D1 ((FileSha-D1 $FailureCsv)-eq [string]$Contract.returned_failure.baseline_equivalence_sha256) 'Returned R3 baseline evidence hash drift.'
$Rows=@(Import-Csv -LiteralPath $FailureCsv)
Require-D1 ($Rows.Count -eq 128) 'Returned baseline row count drift.'
for($i=0;$i -lt 128;$i++){
    Require-D1 ([int]$Rows[$i].step -eq ($i+1)) ("Returned baseline step sequence drift at index {0}." -f $i)
    Require-D1 ($Rows[$i].match -eq 'true') ("Returned baseline mismatch at step {0}." -f ($i+1))
    Require-D1 ($Rows[$i].canonical_fingerprint -eq $Rows[$i].shadow_baseline_fingerprint) ("Returned baseline fingerprint inequality at step {0}." -f ($i+1))
}

$Src=TreeSha-D1 (Join-Path $Root 'src')
Require-D1 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drift.'

$R3Rel=[string]$Contract.baseline.existing_r3_focused_test
$DiagRel=[string]$Contract.diagnostic.test_file
$Historical=TreeSha-D1 (Join-Path $Root 'tests') @($R3Rel.Substring(6),$DiagRel.Substring(6))
Require-D1 ($Historical.Count -eq [int]$Contract.baseline.historical_tests_file_count -and $Historical.Hash -eq [string]$Contract.baseline.historical_tests_tree_sha256) 'Historical tests tree drift.'

$R3Test=Join-Path $Root $R3Rel.Replace('/','\')
$DiagTest=Join-Path $Root $DiagRel.Replace('/','\')
Require-D1 ((NormSha-D1 $R3Test)-eq [string]$Contract.baseline.existing_r3_focused_test_normalized_sha256) 'Existing R3 focused test drift.'
Require-D1 ((NormSha-D1 $DiagTest)-eq [string]$Contract.diagnostic.test_normalized_sha256) 'Diagnostic test hash drift.'

$Wrapper=Join-Path $Root ([string]$Contract.baseline.wrapper_path).Replace('/','\')
$Model=Join-Path $Root ([string]$Contract.baseline.simplified_model_path).Replace('/','\')
Require-D1 ((FileSha-D1 $Wrapper)-eq [string]$Contract.baseline.wrapper_sha256) 'ThermodynamicBranchContinuityModel source drift.'
Require-D1 ((FileSha-D1 $Model)-eq [string]$Contract.baseline.simplified_model_sha256) 'SimplifiedWaterSteamThermodynamicModel source drift.'
$WrapperText=[IO.File]::ReadAllText($Wrapper,[Text.Encoding]::UTF8)
$ModelText=[IO.File]::ReadAllText($Model,[Text.Encoding]::UTF8)
Require-D1 ($WrapperText.Contains('ReferenceEquals(_productionModel, _diagnosticProvider)')) 'H.28.1-E same-instance fusion marker missing.'
Require-D1 ($WrapperText.Contains('optimizedProvider.EvaluateBranchContinuity(definition, inventory, previousState)')) 'Fused EvaluateBranchContinuity call marker missing.'
Require-D1 ($WrapperText.Contains('_productionModel.Resolve(definition, inventory, previousState)')) 'Non-fused production Resolve marker missing.'
Require-D1 ($ModelText.Contains('_referenceConsistentTabulatedInverseResolver.TryResolve(')) 'Mode-2 direct resolver marker missing.'
Require-D1 ($ModelText.Contains('internal WaterSteamBranchContinuityEvaluation EvaluateBranchContinuity(')) 'Branch continuity evaluation marker missing.'

Write-Host 'R3 mode-2 branch-continuity fusion failure diagnostic static audit: PASS' -ForegroundColor Green
Write-Host 'No production repair or R4 authority is granted.'
