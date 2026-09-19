$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Need-I1([bool]$C,[string]$M){if(-not $C){throw $M}}
function Sha-I1([byte[]]$B){$S=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($S.ComputeHash($B))).Replace('-','').ToUpperInvariant()}finally{$S.Dispose()}}
function FileSha-I1([string]$P){Sha-I1 ([IO.File]::ReadAllBytes($P))}
function NormSha-I1([string]$P){$T=[IO.File]::ReadAllText($P,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-I1 ([Text.Encoding]::UTF8.GetBytes($T))}
function Tree-I1([string]$Root,[string[]]$Exclude=@()){$R=(Resolve-Path -LiteralPath $Root).Path;$E=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase);foreach($X in $Exclude){[void]$E.Add($X.Replace('\','/'))};$L=New-Object 'Collections.Generic.List[string]';foreach($F in @(Get-ChildItem -LiteralPath $R -Recurse -File)){$Rel=$F.FullName.Substring($R.Length+1).Replace('\','/');$P=$Rel.Split('/');if($P -contains 'bin' -or $P -contains 'obj'){continue};if(-not $E.Contains($Rel)){$L.Add($Rel)}};$L.Sort([StringComparer]::Ordinal);$M=New-Object IO.MemoryStream;try{foreach($Rel in $L){$B=[Text.Encoding]::UTF8.GetBytes($Rel);$M.Write($B,0,$B.Length);$M.WriteByte(0);$H=[Security.Cryptography.SHA256]::Create();$Fs=[IO.File]::OpenRead((Join-Path $R $Rel.Replace('/','\')));try{$FH=$H.ComputeHash($Fs)}finally{$Fs.Dispose();$H.Dispose()};$M.Write($FH,0,$FH.Length);$M.WriteByte(10)};$M.Position=0;$TH=[Security.Cryptography.SHA256]::Create();try{$Z=$TH.ComputeHash($M)}finally{$TH.Dispose()};@{Count=$L.Count;Hash=([BitConverter]::ToString($Z)).Replace('-','').ToUpperInvariant()}}finally{$M.Dispose()}}
function RemoveOnce-I1([string]$Text,[string]$Fragment,[string]$Label){$I=$Text.IndexOf($Fragment,[StringComparison]::Ordinal);Need-I1 ($I -ge 0) ("Missing projection fragment: {0}" -f $Label);$J=$Text.IndexOf($Fragment,$I+$Fragment.Length,[StringComparison]::Ordinal);Need-I1 ($J -lt 0) ("Duplicated projection fragment: {0}" -f $Label);$Text.Remove($I,$Fragment.Length)}
$Root=Split-Path -Parent $PSScriptRoot;Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-mode2-branch-continuity-fusion-repair-implementation1-contract.json' -Raw | ConvertFrom-Json
Need-I1 ($C.status -eq 'IMPLEMENTATION-EVIDENCE-GATE') 'Implementation contract status drift.'
Need-I1 ($C.prerequisite.planning_status -eq 'PASS-AS-AUTHORED') 'Planning returned status drift.'
$PR=Join-Path $Root 'eng\frozen-evidence\ordinary\M10FinalVR2_R3_Mode2BranchContinuityFusionRepair_Planning1_ReturnedArtifacts'
foreach($P in $C.prerequisite.planning_artifacts.PSObject.Properties){$Q=Join-Path $PR $P.Name;Need-I1 (Test-Path -LiteralPath $Q -PathType Leaf) ("Missing planning artifact: {0}" -f $P.Name);Need-I1 ((FileSha-I1 $Q)-eq [string]$P.Value.sha256) ("Planning artifact hash drift: {0}" -f $P.Name)}
$Model=Join-Path $Root $C.implementation_files.model.path.Replace('/','\');$Wrapper=Join-Path $Root $C.implementation_files.wrapper.path.Replace('/','\');$Test=Join-Path $Root $C.implementation_files.focused_test.path.Replace('/','\')
Need-I1 ((NormSha-I1 $Model)-eq [string]$C.implementation_files.model.normalized_sha256) 'Implementation model hash drift.'
Need-I1 ((NormSha-I1 $Wrapper)-eq [string]$C.implementation_files.wrapper.normalized_sha256) 'Implementation wrapper hash drift.'
Need-I1 ((NormSha-I1 $Test)-eq [string]$C.implementation_files.focused_test.normalized_sha256) 'Focused repair test hash drift.'
$ModelText=[IO.File]::ReadAllText($Model,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n")
$WrapperText=[IO.File]::ReadAllText($Wrapper,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n")
$MF="    internal bool IsLegacyBranchContinuityFusionEligible`n        => _closureMode != WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain;`n`n"
$WF="`n            && optimizedProvider.IsLegacyBranchContinuityFusionEligible)"
$ModelProjection=RemoveOnce-I1 $ModelText $MF 'mode eligibility capability'
$WI=$WrapperText.IndexOf($WF,[StringComparison]::Ordinal);Need-I1 ($WI -ge 0) 'Missing H.28.1-E eligibility guard';$WJ=$WrapperText.IndexOf($WF,$WI+$WF.Length,[StringComparison]::Ordinal);Need-I1 ($WJ -lt 0) 'Duplicated H.28.1-E eligibility guard';$WrapperProjection=$WrapperText.Remove($WI,$WF.Length).Insert($WI,')')
Need-I1 ((Sha-I1 ([Text.Encoding]::UTF8.GetBytes($ModelProjection)))-eq [string]$C.baseline.model_normalized_sha256) 'Model legacy projection differs from Planning 1 baseline.'
Need-I1 ((Sha-I1 ([Text.Encoding]::UTF8.GetBytes($WrapperProjection)))-eq [string]$C.baseline.wrapper_normalized_sha256) 'Wrapper legacy projection differs from Planning 1 baseline.'
Need-I1 ((FileSha-I1 'src\NuclearReactorSimulator.Simulation\Physics\Fluids\ReferenceConsistentTabulatedInverseResolver.cs')-eq [string]$C.baseline.resolver_sha256) 'Mode2 resolver changed.'
Need-I1 ((FileSha-I1 'src\NuclearReactorSimulator.Simulation\Physics\Fluids\ReferenceData\NRSVR2C4.v1.bin')-eq [string]$C.baseline.payload_sha256) 'C4 payload changed.'
Need-I1 ((FileSha-I1 'src\NuclearReactorSimulator.Simulation\Physics\Fluids\WaterSteamThermodynamicClosureMode.cs')-eq [string]$C.baseline.closure_mode_sha256) 'Closure mode enum/default changed.'
$S=Tree-I1 (Join-Path $Root 'src') @('NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs','NuclearReactorSimulator.Simulation/Physics/Fluids/ThermodynamicBranchContinuityModel.cs')
Need-I1 ($S.Count -eq [int]$C.baseline.src_excluding_authorized_count -and $S.Hash -eq [string]$C.baseline.src_excluding_authorized_tree_sha256) 'Unauthorized src change detected.'
$T=Tree-I1 (Join-Path $Root 'tests') @('NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2R3Mode2BranchContinuityFusionRepairImplementation1Tests.cs')
Need-I1 ($T.Count -eq [int]$C.baseline.tests_before_new_count -and $T.Hash -eq [string]$C.baseline.tests_before_new_tree_sha256) 'Historical tests changed.'
Need-I1 ($C.implementation.authorized_production_files.Count -eq 2) 'Production scope count drift.'
Need-I1 (-not [bool]$C.authority.r3_passed -and -not [bool]$C.authority.r4_planning_authorized) 'Downstream authority drift.'
foreach($P in $C.documentation_files.PSObject.Properties){$I=$P.Value;$Q=Join-Path $Root ([string]$I.path).Replace('/','\');Need-I1 ((NormSha-I1 $Q)-eq [string]$I.normalized_sha256) ("Documentation hash drift: {0}" -f $I.path)}
foreach($P in $C.gate_files.PSObject.Properties){$I=$P.Value;$Q=Join-Path $Root ([string]$I.path).Replace('/','\');Need-I1 ((NormSha-I1 $Q)-eq [string]$I.normalized_sha256) ("Gate file hash drift: {0}" -f $I.path)}
Write-Host 'R3 mode-2 branch-continuity fusion Repair Implementation 1 static audit: PASS' -ForegroundColor Green
Write-Host 'Implementation evidence only. R3 remains RED until Short Requalification 2 passes.'
