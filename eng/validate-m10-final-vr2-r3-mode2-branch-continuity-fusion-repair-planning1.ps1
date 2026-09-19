$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require-RP1([bool]$C,[string]$M){if(-not $C){throw $M}}
function Sha-RP1([byte[]]$B){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($B))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-RP1([string]$P){Sha-RP1 ([IO.File]::ReadAllBytes($P))}
function NormSha-RP1([string]$P){$T=[IO.File]::ReadAllText($P,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-RP1 ([Text.Encoding]::UTF8.GetBytes($T))}
function TreeSha-RP1([string]$R){$RR=(Resolve-Path -LiteralPath $R).Path;$L=New-Object 'Collections.Generic.List[string]';foreach($F in @(Get-ChildItem -LiteralPath $RR -Recurse -File)){$Rel=$F.FullName.Substring($RR.Length+1).Replace('\','/');$P=$Rel.Split('/');if($P -contains 'bin' -or $P -contains 'obj'){continue};$L.Add($Rel)};$L.Sort([StringComparer]::Ordinal);$M=New-Object IO.MemoryStream;try{foreach($Rel in $L){$B=[Text.Encoding]::UTF8.GetBytes($Rel);$M.Write($B,0,$B.Length);$M.WriteByte(0);$H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $RR $Rel.Replace('/','\')));try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()};$M.Write($FH,0,$FH.Length);$M.WriteByte(10)};$M.Position=0;$TH=[Security.Cryptography.SHA256]::Create();try{$F=$TH.ComputeHash($M)}finally{$TH.Dispose()};@{Count=$L.Count;Hash=([BitConverter]::ToString($F)).Replace('-','').ToUpperInvariant()}}finally{$M.Dispose()}}
$Root=Split-Path -Parent $PSScriptRoot;Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-mode2-branch-continuity-fusion-repair-planning1-contract.json' -Raw | ConvertFrom-Json
Require-RP1 ($C.schema -eq 'm10-final-vr2-r3-mode2-branch-continuity-fusion-repair-planning1-v1') 'Schema drift.'
Require-RP1 ($C.status -eq 'PLANNING-CANDIDATE') 'Status drift.'
Require-RP1 ($C.predecessor.root_cause -eq 'H28.1-E-FUSED-PATH-NOT-MODE2-AWARE') 'Root-cause drift.'
$S=TreeSha-RP1 (Join-Path $Root 'src');Require-RP1 ($S.Count -eq [int]$C.baseline.src_file_count -and $S.Hash -eq [string]$C.baseline.src_tree_sha256) 'src tree drift.'
$T=TreeSha-RP1 (Join-Path $Root 'tests');Require-RP1 ($T.Count -eq [int]$C.baseline.tests_file_count -and $T.Hash -eq [string]$C.baseline.tests_tree_sha256) 'tests tree drift.'
foreach($P in $C.baseline.production_files.PSObject.Properties){$I=$P.Value;$Q=Join-Path $Root ([string]$I.path).Replace('/','\');Require-RP1 ((FileSha-RP1 $Q)-eq [string]$I.sha256) ("Production baseline drift: {0}" -f $I.path)}
foreach($P in $C.baseline.tests.PSObject.Properties){$I=$P.Value;$Q=Join-Path $Root ([string]$I.path).Replace('/','\');Require-RP1 ((NormSha-RP1 $Q)-eq [string]$I.normalized_sha256) ("Test baseline drift: {0}" -f $I.path)}
$ER=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_Mode2BranchContinuityFusionFailure_Diagnostic1_ReturnedArtifacts'
foreach($P in $C.predecessor.returned_diagnostic_artifacts.PSObject.Properties){$Q=Join-Path $ER $P.Name;Require-RP1 ((FileSha-RP1 $Q)-eq [string]$P.Value.sha256) ("Returned artifact drift: {0}" -f $P.Name)}
$M=[IO.File]::ReadAllText((Join-Path $ER '01-failure-state-resolution-matrix.txt'),[Text.Encoding]::UTF8)
$D=[IO.File]::ReadAllText((Join-Path $ER '03-diagnostic-summary.txt'),[Text.Encoding]::UTF8)
$R=[IO.File]::ReadAllText((Join-Path $ER '04-pre-repair-review.txt'),[Text.Encoding]::UTF8)
Require-RP1 ($M.Contains('resolver-success=True') -and $M.Contains('direct-mode2-success=True')) 'Direct mode2 evidence missing.'
Require-RP1 ($M.Contains('fused-same-instance-success=False')) 'Fused blocker evidence missing.'
Require-RP1 ($M.Contains('split-distinct-instance-success=True') -and $M.Contains('split-equals-direct=True')) 'Non-fused evidence missing.'
Require-RP1 ($D.Contains('root-cause-classification=H28.1-E-FUSED-PATH-NOT-MODE2-AWARE')) 'Root-cause evidence missing.'
Require-RP1 ($R.Contains('candidate-repair=DISABLE-H28.1-E-FUSION-FOR-MODE2-ONLY')) 'Candidate-repair evidence missing.'
Require-RP1 ($R.Contains('mode0-mode1-fused-path-must-remain-UNCHANGED')) 'Mode0/1 marker missing.'
Require-RP1 ($R.Contains('mode2-production-resolver-must-remain-ReferenceConsistentTabulatedInverseResolver')) 'Mode2 resolver marker missing.'
Require-RP1 ($C.selected_repair.name -eq 'MODE2-FUSION-ELIGIBILITY-GUARD') 'Repair design drift.'
Require-RP1 ($C.selected_repair.authorized_production_files.Count -eq 2) 'Repair file count drift.'
Require-RP1 (-not [bool]$C.authority.production_repair_authorized_now -and -not [bool]$C.authority.r4_planning_authorized) 'Authority widened.'
Require-RP1 ($C.sequencing.planning_successor -eq 'R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-IMPLEMENTATION1') 'Successor drift.'
Require-RP1 ($C.sequencing.after_implementation_pass -eq 'R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION2') 'Requalification successor drift.'
Require-RP1 ([bool]$C.sequencing.r4_requires_requalification_pass) 'R4 prerequisite lost.'
$F=Join-Path $Root ([string]$C.selected_repair.future_test).Replace('/','\');Require-RP1 (-not (Test-Path -LiteralPath $F)) 'Future implementation test exists in planning candidate.'
foreach($P in $C.documentation_files.PSObject.Properties){$I=$P.Value;$Q=Join-Path $Root ([string]$I.path).Replace('/','\');Require-RP1 ((NormSha-RP1 $Q)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)}
foreach($P in $C.gate_files.PSObject.Properties){$I=$P.Value;$Q=Join-Path $Root ([string]$I.path).Replace('/','\');Require-RP1 ((NormSha-RP1 $Q)-eq [string]$I.normalized_sha256) ("Gate hash drift: {0}" -f $I.path)}
Write-Host 'R3 mode-2 branch-continuity fusion Repair Planning 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Planning only. Production repair, R3 PASS and R4 remain unauthorized.'
