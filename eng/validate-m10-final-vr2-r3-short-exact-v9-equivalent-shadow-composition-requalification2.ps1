$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require-R32([bool]$C,[string]$M){if(-not $C){throw $M}}
function Sha-R32([byte[]]$B){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($B))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-R32([string]$P){Sha-R32 ([IO.File]::ReadAllBytes($P))}
function NormSha-R32([string]$P){$T=[IO.File]::ReadAllText($P,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-R32 ([Text.Encoding]::UTF8.GetBytes($T))}
function TreeSha-R32([string]$R,[string[]]$ExcludeRel=@()){
 $RR=(Resolve-Path -LiteralPath $R).Path
 $E=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
 foreach($X in $ExcludeRel){[void]$E.Add($X.Replace('\','/'))}
 $L=New-Object 'Collections.Generic.List[string]'
 foreach($F in @(Get-ChildItem -LiteralPath $RR -Recurse -File)){
   $Rel=$F.FullName.Substring($RR.Length+1).Replace('\','/');$Parts=$Rel.Split('/')
   if($Parts -contains 'bin' -or $Parts -contains 'obj'){continue}
   if(-not $E.Contains($Rel)){$L.Add($Rel)}
 }
 $L.Sort([StringComparer]::Ordinal);$M=New-Object IO.MemoryStream
 try{
   foreach($Rel in $L){$B=[Text.Encoding]::UTF8.GetBytes($Rel);$M.Write($B,0,$B.Length);$M.WriteByte(0);$H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $RR $Rel.Replace('/','\')));try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()};$M.Write($FH,0,$FH.Length);$M.WriteByte(10)}
   $M.Position=0;$TH=[Security.Cryptography.SHA256]::Create();try{$Final=$TH.ComputeHash($M)}finally{$TH.Dispose()}
   @{Count=$L.Count;Hash=([BitConverter]::ToString($Final)).Replace('-','').ToUpperInvariant()}
 }finally{$M.Dispose()}
}
$Root=Split-Path -Parent $PSScriptRoot;Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification2-contract.json' -Raw | ConvertFrom-Json
Require-R32 ($C.schema -eq 'm10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification2-v1') 'R3-2 schema drift.'
Require-R32 ($C.status -eq 'EXECUTION-CANDIDATE') 'R3-2 status drift.'
Require-R32 ($C.prerequisite.repair_implementation1_returned_adjudication -eq 'PASS') 'Repair Implementation 1 returned PASS missing.'
Require-R32 ($C.prerequisite.repair_implementation1_classification -eq 'PASS-R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-IMPLEMENTATION1') 'Repair Implementation 1 classification drift.'
Require-R32 ($C.execution.single_factor_change -eq 'thermodynamicClosureMode:1->2') 'Single-factor contract drift.'
Require-R32 ([int]$C.execution.baseline_equivalence_steps -eq 128) 'Baseline step count drift.'
Require-R32 ([int]$C.execution.mode2_shadow_steps -eq 12000) 'Mode2 step count drift.'
Require-R32 ([double]$C.execution.simulated_seconds -eq 120.0) 'Simulated seconds drift.'
Require-R32 ([double]$C.execution.fixed_timestep_ms -eq 10.0) 'Fixed timestep drift.'
Require-R32 ([int]$C.execution.trajectory_stride_steps -eq 100) 'Trajectory stride drift.'
Require-R32 ([int]$C.execution.mode2_deterministic_repeat_steps -eq 128) 'Determinism count drift.'
Require-R32 ([bool]$C.execution.ordinary_release_suite_required) 'Ordinary suite requirement lost.'
Require-R32 ([bool]$C.execution.forced_preordinary_full_rebuild_required) 'Preordinary rebuild requirement lost.'
Require-R32 ([bool]$C.execution.forced_focused_rebuild_required) 'Focused rebuild requirement lost.'
Require-R32 (-not [bool]$C.execution.r4_long_materiality_execution_allowed) 'R4 execution illegally enabled.'
Require-R32 (-not [bool]$C.execution.canonical_exact_v9_change_allowed) 'Canonical exact-v9 mutation illegally enabled.'
Require-R32 (-not [bool]$C.execution.default_mode2_activation_allowed) 'Default mode2 illegally enabled.'
$RepairRoot=Join-Path $Root $C.repair_implementation1_returned_artifacts.root.Replace('/','\')
Require-R32 (Test-Path -LiteralPath $RepairRoot -PathType Container) 'Repair Implementation 1 artifact root missing.'
Require-R32 (@(Get-ChildItem -LiteralPath $RepairRoot -File).Count -eq 6) 'Repair Implementation 1 artifact count drift.'
foreach($P in $C.repair_implementation1_returned_artifacts.sha256.PSObject.Properties){$Q=Join-Path $RepairRoot $P.Name;Require-R32 ((FileSha-R32 $Q)-eq [string]$P.Value) ("Repair artifact hash drift: {0}" -f $P.Name)}
$RepairSummary=[IO.File]::ReadAllText((Join-Path $RepairRoot '05-implementation-summary.txt'),[Text.Encoding]::UTF8)
Require-R32 ($RepairSummary.Contains('status=PASS-R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-IMPLEMENTATION1')) 'Repair PASS summary missing.'
Require-R32 ($RepairSummary.Contains('ordinary-release-suite=PASS')) 'Repair ordinary PASS missing.'
foreach($P in $C.baseline.repaired_production_normalized_sha256.PSObject.Properties){$Q=Join-Path $Root $P.Name.Replace('/','\');Require-R32 ((NormSha-R32 $Q)-eq [string]$P.Value) ("Repaired production drift: {0}" -f $P.Name)}
$Src=TreeSha-R32 (Join-Path $Root 'src')
Require-R32 ($Src.Count -eq [int]$C.baseline.src_file_count -and $Src.Hash -eq [string]$C.baseline.src_tree_sha256) 'Repaired src tree drift.'
$TestRel=[string]$C.execution.new_test_file
$Hist=TreeSha-R32 (Join-Path $Root 'tests') @($TestRel.Substring('tests/'.Length))
Require-R32 ($Hist.Count -eq [int]$C.baseline.tests_file_count -and $Hist.Hash -eq [string]$C.baseline.tests_tree_sha256) 'Historical tests tree drift.'
$Focused=Join-Path $Root $TestRel.Replace('/','\')
Require-R32 ((NormSha-R32 $Focused)-eq [string]$C.execution.new_test_normalized_sha256) 'R3-2 focused test drift.'
foreach($P in $C.baseline.exact_v9_provenance_files_sha256.PSObject.Properties){$Q=Join-Path $Root ([string]$P.Value.path).Replace('/','\');Require-R32 ((FileSha-R32 $Q)-eq [string]$P.Value.sha256) ("Exact-v9 provenance drift: {0}" -f $P.Name)}
foreach($P in $C.documentation_files.PSObject.Properties){$Q=Join-Path $Root ([string]$P.Value.path).Replace('/','\');Require-R32 ((NormSha-R32 $Q)-eq [string]$P.Value.normalized_sha256) ("Documentation hash drift: {0}" -f $P.Name)}
foreach($P in $C.gate_files.PSObject.Properties){$Q=Join-Path $Root ([string]$P.Value.path).Replace('/','\');Require-R32 ((NormSha-R32 $Q)-eq [string]$P.Value.normalized_sha256) ("Gate-file hash drift: {0}" -f $P.Name)}
Write-Host 'R3 Short Requalification 2 static audit: PASS' -ForegroundColor Green
Write-Host 'No R4, default mode2 or canonical exact-v9 authority is granted.'
