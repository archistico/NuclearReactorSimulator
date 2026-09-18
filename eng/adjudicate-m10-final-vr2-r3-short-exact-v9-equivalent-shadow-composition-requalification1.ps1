$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Invariant=[Globalization.CultureInfo]::InvariantCulture

function Require-R3([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Read-R3Kv([string]$Path){
    Require-R3 (Test-Path -LiteralPath $Path -PathType Leaf) ("Missing file {0}" -f $Path)
    $Map=@{}
    foreach($Raw in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path))){
        $Line=$Raw.TrimStart([char]0xFEFF)
        $Index=$Line.IndexOf('=')
        if($Index -gt 0){
            $Key=$Line.Substring(0,$Index)
            Require-R3 (-not $Map.ContainsKey($Key)) ("Duplicate key in key-value evidence: {0}" -f $Key)
            $Map[$Key]=$Line.Substring($Index+1)
        }
    }
    $Map
}
function Parse-R3Double([string]$Value){[double]::Parse($Value,$Invariant)}
function Parse-R3FiniteDouble([string]$Value,[string]$Label){
    $Parsed=Parse-R3Double $Value
    Require-R3 (-not [double]::IsNaN($Parsed) -and -not [double]::IsInfinity($Parsed)) ("Non-finite numeric evidence: {0}" -f $Label)
    $Parsed
}
function Write-R3Lines([string]$Path,[string[]]$Lines){[IO.File]::WriteAllLines($Path,$Lines,(New-Object Text.UTF8Encoding($false)))}
function Sha-R3([byte[]]$Bytes){$Sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($Sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant()}finally{$Sha.Dispose()}}
function FileSha-R3([string]$Path){Sha-R3 ([IO.File]::ReadAllBytes($Path))}
function NormSha-R3([string]$Path){$Text=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-R3 ([Text.Encoding]::UTF8.GetBytes($Text))}
function TreeSha-R3([string]$TreeRoot,[string[]]$ExcludeRel=@()){
    $Resolved=(Resolve-Path -LiteralPath $TreeRoot).Path
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
function Require-R3KvKey([hashtable]$Map,[string]$Key){Require-R3 ($Map.ContainsKey($Key)) ("Missing key in key-value evidence: {0}" -f $Key)}

$Contract=Get-Content 'eng\m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1-contract.json' -Raw | ConvertFrom-Json
$ArtifactRoot=Join-Path $Root ([string]$Contract.execution.artifact_root).Replace('/','\')
Require-R3 (Test-Path -LiteralPath $ArtifactRoot -PathType Container) 'R3 artifact root missing.'
Require-R3 ($env:NRS_M10_FINAL_VR2_R3_REQUALIFICATION1_ORDINARY_PASS -eq '1') 'Ordinary Release PASS marker missing.'

$TestRel=[string]$Contract.execution.new_test_file
$Src=TreeSha-R3 (Join-Path $Root 'src')
Require-R3 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drifted after static audit.'
$Historical=TreeSha-R3 (Join-Path $Root 'tests') @($TestRel.Substring('tests/'.Length))
Require-R3 ($Historical.Count -eq [int]$Contract.baseline.tests_file_count -and $Historical.Hash -eq [string]$Contract.baseline.tests_tree_sha256) 'Historical tests tree drifted after static audit.'
$FocusedTestPath=Join-Path $Root $TestRel.Replace('/','\')
Require-R3 ((NormSha-R3 $FocusedTestPath)-eq [string]$Contract.execution.new_test_normalized_sha256) 'Focused R3 test drifted after static audit.'
foreach($Prop in $Contract.baseline.exact_v9_provenance_files_sha256.PSObject.Properties){
    $Path=Join-Path $Root ([string]$Prop.Value.path).Replace('/','\')
    Require-R3 ((FileSha-R3 $Path)-eq [string]$Prop.Value.sha256) ("Exact-v9 provenance drifted after static audit: {0}" -f $Prop.Name)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $Path=Join-Path $Root ([string]$Prop.Value.path).Replace('/','\')
    Require-R3 ((NormSha-R3 $Path)-eq [string]$Prop.Value.normalized_sha256) ("R3 gate file drifted after static audit: {0}" -f $Prop.Name)
}

$BaselinePath=Join-Path $ArtifactRoot '02-shadow-baseline-equivalence.csv'
$BaselineRaw=[IO.File]::ReadAllLines((Resolve-Path -LiteralPath $BaselinePath))
Require-R3 ($BaselineRaw.Count -ge 1 -and $BaselineRaw[0] -eq 'step,canonical_fingerprint,shadow_baseline_fingerprint,match') 'R3 baseline equivalence CSV header mismatch.'
$Baseline=@(Import-Csv -LiteralPath $BaselinePath)
Require-R3 ($Baseline.Count -eq 128) 'R3 baseline equivalence row count mismatch.'
Require-R3 (@($Baseline | Where-Object {
    $_.match -ne 'true'
    -or $_.canonical_fingerprint -ne $_.shadow_baseline_fingerprint
    -or $_.canonical_fingerprint -notmatch '^[0-9a-f]{64}$'
    -or $_.shadow_baseline_fingerprint -notmatch '^[0-9a-f]{64}$'
}).Count -eq 0) 'Canonical exact-v9 and mode-1 shadow fingerprint mismatch.'
Require-R3 ([int]$Baseline[0].step -eq 1 -and [int]$Baseline[-1].step -eq 128) 'Baseline equivalence step range mismatch.'
for($Index=0;$Index -lt $Baseline.Count;$Index++){
    Require-R3 ([int]$Baseline[$Index].step -eq ($Index+1)) ("Baseline equivalence step sequence mismatch at row {0}." -f ($Index+1))
}

$TrajectoryPath=Join-Path $ArtifactRoot '03-mode2-shadow-health-trajectory.csv'
$TrajectoryRaw=[IO.File]::ReadAllLines((Resolve-Path -LiteralPath $TrajectoryPath))
$ExpectedTrajectoryHeader='step,seconds,electrical_mwe,primary_pump_kg_s,drum_level,governor_output_percent,moisture_drain_kg_s,transfer_mismatch_kg_s,stage_energy_ownership_residual_w,mass_closure_kg,full_energy_closure_j,balance_mass_rate_kg_s,balance_power_w,trip_active,breaker_closed,rollbacks,fallback_commit_violations,unsafe_commit_violations,untargeted_branch_disagreements,finite,in_envelope'
Require-R3 ($TrajectoryRaw.Count -ge 1 -and $TrajectoryRaw[0] -eq $ExpectedTrajectoryHeader) 'R3 sampled health trajectory CSV header mismatch.'
$Trajectory=@(Import-Csv -LiteralPath $TrajectoryPath)
Require-R3 ($Trajectory.Count -eq 120) 'R3 sampled health trajectory row count mismatch.'
Require-R3 ([int]$Trajectory[0].step -eq 100 -and [int]$Trajectory[-1].step -eq 12000) 'R3 sampled health trajectory step range mismatch.'
for($Index=0;$Index -lt $Trajectory.Count;$Index++){
    $Row=$Trajectory[$Index]
    $ExpectedStep=($Index+1)*100
    Require-R3 ([int]$Row.step -eq $ExpectedStep) ("R3 sampled trajectory step sequence mismatch at row {0}." -f ($Index+1))
    $Seconds=Parse-R3FiniteDouble $Row.seconds ("trajectory seconds row {0}" -f ($Index+1))
    Require-R3 ([Math]::Abs($Seconds-($ExpectedStep*0.01)) -le 1e-12) ("R3 sampled trajectory time mismatch at row {0}." -f ($Index+1))

    $Electrical=Parse-R3FiniteDouble $Row.electrical_mwe ("trajectory electrical row {0}" -f ($Index+1))
    $PrimaryPump=Parse-R3FiniteDouble $Row.primary_pump_kg_s ("trajectory primary pump row {0}" -f ($Index+1))
    $DrumLevel=Parse-R3FiniteDouble $Row.drum_level ("trajectory drum level row {0}" -f ($Index+1))
    $Governor=Parse-R3FiniteDouble $Row.governor_output_percent ("trajectory governor row {0}" -f ($Index+1))
    $Moisture=Parse-R3FiniteDouble $Row.moisture_drain_kg_s ("trajectory moisture drain row {0}" -f ($Index+1))
    $Transfer=Parse-R3FiniteDouble $Row.transfer_mismatch_kg_s ("trajectory transfer mismatch row {0}" -f ($Index+1))
    $Ownership=Parse-R3FiniteDouble $Row.stage_energy_ownership_residual_w ("trajectory ownership residual row {0}" -f ($Index+1))
    $MassClosure=Parse-R3FiniteDouble $Row.mass_closure_kg ("trajectory mass closure row {0}" -f ($Index+1))
    $EnergyClosure=Parse-R3FiniteDouble $Row.full_energy_closure_j ("trajectory energy closure row {0}" -f ($Index+1))
    $BalanceMass=Parse-R3FiniteDouble $Row.balance_mass_rate_kg_s ("trajectory balance mass row {0}" -f ($Index+1))
    $BalancePower=Parse-R3FiniteDouble $Row.balance_power_w ("trajectory balance power row {0}" -f ($Index+1))

    Require-R3 ($Electrical -ge 4.99 -and $Electrical -le 5.01) ("Sampled electrical envelope exceeded at row {0}." -f ($Index+1))
    Require-R3 ($PrimaryPump -ge 99.9 -and $PrimaryPump -le 100.1) ("Sampled primary-flow envelope exceeded at row {0}." -f ($Index+1))
    Require-R3 ($DrumLevel -ge .49 -and $DrumLevel -le .51) ("Sampled drum-level envelope exceeded at row {0}." -f ($Index+1))
    Require-R3 ($Governor -ge 29.27 -and $Governor -le 29.30) ("Sampled governor envelope exceeded at row {0}." -f ($Index+1))
    Require-R3 ($Moisture -gt 0.0) ("Sampled moisture-drain ownership failed at row {0}." -f ($Index+1))
    Require-R3 ($Transfer -ge 0.0 -and $Transfer -le 1e-8) ("Sampled transfer mismatch ceiling exceeded at row {0}." -f ($Index+1))
    Require-R3 ($Ownership -ge 0.0 -and $Ownership -le 1e-3) ("Sampled ownership residual ceiling exceeded at row {0}." -f ($Index+1))
    Require-R3 ($MassClosure -ge 0.0 -and $MassClosure -le 1e-6) ("Sampled mass closure ceiling exceeded at row {0}." -f ($Index+1))
    Require-R3 ($EnergyClosure -ge 0.0 -and $EnergyClosure -le 1e-2) ("Sampled energy closure ceiling exceeded at row {0}." -f ($Index+1))
    Require-R3 ($BalanceMass -ge 0.0 -and $BalanceMass -le 1e-8) ("Sampled balance mass-rate ceiling exceeded at row {0}." -f ($Index+1))
    Require-R3 ($BalancePower -ge 0.0 -and $BalancePower -le 1e-3) ("Sampled balance power ceiling exceeded at row {0}." -f ($Index+1))
    Require-R3 ($Row.trip_active -eq 'false' -and $Row.breaker_closed -eq 'true') ("Sampled R3 trip/breaker violation at row {0}." -f ($Index+1))
    Require-R3 ([long]$Row.rollbacks -eq 0 -and [long]$Row.fallback_commit_violations -eq 0 -and [long]$Row.unsafe_commit_violations -eq 0 -and [long]$Row.untargeted_branch_disagreements -eq 0) ("Sampled R3 numerical ownership violation at row {0}." -f ($Index+1))
    Require-R3 ($Row.finite -eq 'true' -and $Row.in_envelope -eq 'true') ("Sampled R3 derived health flags disagree at row {0}." -f ($Index+1))
}

$Health=Read-R3Kv (Join-Path $ArtifactRoot '04-ownership-conservation-summary.txt')
foreach($Key in @(
    'status','steps','simulated-seconds','fixed-timestep-ms',
    'non-finite-steps','health-envelope-violation-steps','trip-steps','breaker-open-steps',
    'rollbacks','fallback-commit-violations','unsafe-commit-violations','untargeted-branch-disagreements',
    'electrical-range-mwe','min-electrical-mwe','max-electrical-mwe',
    'primary-pump-range-kg-s','min-primary-pump-kg-s','max-primary-pump-kg-s',
    'drum-level-range','min-drum-level','max-drum-level',
    'governor-output-range-percent','min-governor-output-percent','max-governor-output-percent',
    'minimum-moisture-drain-kg-s','max-commanded-transfer-mismatch-kg-s',
    'max-stage-energy-ownership-residual-w','max-network-mass-closure-kg',
    'max-network-energy-closure-j','max-network-balance-mass-rate-kg-s','max-network-balance-power-w',
    'canonical-exact-v9-change','mode2-default-activation','r4-long-materiality-executed'
)){Require-R3KvKey $Health $Key}
Require-R3 ($Health['status'] -eq 'MODE2-SHADOW-HEALTH-EVIDENCE-WRITTEN') 'R3 health summary status mismatch.'
Require-R3 ([int]$Health['steps'] -eq 12000) 'R3 health summary step count mismatch.'
Require-R3 ((Parse-R3Double $Health['simulated-seconds']) -eq 120.0) 'R3 simulated duration mismatch.'
Require-R3 ((Parse-R3Double $Health['fixed-timestep-ms']) -eq 10.0) 'R3 fixed timestep mismatch.'
foreach($Key in @('non-finite-steps','health-envelope-violation-steps','trip-steps','breaker-open-steps','rollbacks','fallback-commit-violations','unsafe-commit-violations','untargeted-branch-disagreements')){
    Require-R3 ([long]$Health[$Key] -eq 0) ("R3 zero-event contract failed: {0}" -f $Key)
}

$MinElectrical=Parse-R3FiniteDouble $Health['min-electrical-mwe'] 'summary min electrical'
$MaxElectrical=Parse-R3FiniteDouble $Health['max-electrical-mwe'] 'summary max electrical'
$MinPrimaryPump=Parse-R3FiniteDouble $Health['min-primary-pump-kg-s'] 'summary min primary pump'
$MaxPrimaryPump=Parse-R3FiniteDouble $Health['max-primary-pump-kg-s'] 'summary max primary pump'
$MinDrumLevel=Parse-R3FiniteDouble $Health['min-drum-level'] 'summary min drum level'
$MaxDrumLevel=Parse-R3FiniteDouble $Health['max-drum-level'] 'summary max drum level'
$MinGovernor=Parse-R3FiniteDouble $Health['min-governor-output-percent'] 'summary min governor'
$MaxGovernor=Parse-R3FiniteDouble $Health['max-governor-output-percent'] 'summary max governor'
$MinMoisture=Parse-R3FiniteDouble $Health['minimum-moisture-drain-kg-s'] 'summary minimum moisture drain'
$MaxTransfer=Parse-R3FiniteDouble $Health['max-commanded-transfer-mismatch-kg-s'] 'summary max transfer mismatch'
$MaxOwnership=Parse-R3FiniteDouble $Health['max-stage-energy-ownership-residual-w'] 'summary max ownership residual'
$MaxMassClosure=Parse-R3FiniteDouble $Health['max-network-mass-closure-kg'] 'summary max mass closure'
$MaxEnergyClosure=Parse-R3FiniteDouble $Health['max-network-energy-closure-j'] 'summary max energy closure'
$MaxBalanceMass=Parse-R3FiniteDouble $Health['max-network-balance-mass-rate-kg-s'] 'summary max balance mass'
$MaxBalancePower=Parse-R3FiniteDouble $Health['max-network-balance-power-w'] 'summary max balance power'

Require-R3 ($MinElectrical -ge 4.99 -and $MaxElectrical -le 5.01 -and $MinElectrical -le $MaxElectrical) 'Electrical envelope exceeded.'
Require-R3 ($MinPrimaryPump -ge 99.9 -and $MaxPrimaryPump -le 100.1 -and $MinPrimaryPump -le $MaxPrimaryPump) 'Primary flow envelope exceeded.'
Require-R3 ($MinDrumLevel -ge .49 -and $MaxDrumLevel -le .51 -and $MinDrumLevel -le $MaxDrumLevel) 'Drum level envelope exceeded.'
Require-R3 ($MinGovernor -ge 29.27 -and $MaxGovernor -le 29.30 -and $MinGovernor -le $MaxGovernor) 'Governor envelope exceeded.'
Require-R3 ($MinMoisture -gt 0.0) 'Moisture drain ownership failed.'
Require-R3 ($MaxTransfer -ge 0.0 -and $MaxTransfer -le 1e-8) 'Transfer mismatch ceiling exceeded.'
Require-R3 ($MaxOwnership -ge 0.0 -and $MaxOwnership -le 1e-3) 'Stage energy ownership ceiling exceeded.'
Require-R3 ($MaxMassClosure -ge 0.0 -and $MaxMassClosure -le 1e-6) 'Mass closure ceiling exceeded.'
Require-R3 ($MaxEnergyClosure -ge 0.0 -and $MaxEnergyClosure -le 1e-2) 'Energy closure ceiling exceeded.'
Require-R3 ($MaxBalanceMass -ge 0.0 -and $MaxBalanceMass -le 1e-8) 'Balance mass-rate ceiling exceeded.'
Require-R3 ($MaxBalancePower -ge 0.0 -and $MaxBalancePower -le 1e-3) 'Balance power ceiling exceeded.'
Require-R3 ($Health['electrical-range-mwe'] -eq ($Health['min-electrical-mwe']+'..'+$Health['max-electrical-mwe'])) 'Electrical range summary is inconsistent with min/max.'
Require-R3 ($Health['primary-pump-range-kg-s'] -eq ($Health['min-primary-pump-kg-s']+'..'+$Health['max-primary-pump-kg-s'])) 'Primary-pump range summary is inconsistent with min/max.'
Require-R3 ($Health['drum-level-range'] -eq ($Health['min-drum-level']+'..'+$Health['max-drum-level'])) 'Drum-level range summary is inconsistent with min/max.'
Require-R3 ($Health['governor-output-range-percent'] -eq ($Health['min-governor-output-percent']+'..'+$Health['max-governor-output-percent'])) 'Governor range summary is inconsistent with min/max.'
Require-R3 ($Health['canonical-exact-v9-change'] -eq 'False') 'Canonical exact-v9 mutation detected.'
Require-R3 ($Health['mode2-default-activation'] -eq 'False') 'Mode2 default activation detected.'
Require-R3 ($Health['r4-long-materiality-executed'] -eq 'False') 'Forbidden R4 long materiality detected.'
$FinalTrajectory=$Trajectory[-1]
Require-R3 ([long]$FinalTrajectory.rollbacks -eq [long]$Health['rollbacks']) 'Final sampled rollback count disagrees with summary.'
Require-R3 ([long]$FinalTrajectory.fallback_commit_violations -eq [long]$Health['fallback-commit-violations']) 'Final sampled fallback count disagrees with summary.'
Require-R3 ([long]$FinalTrajectory.unsafe_commit_violations -eq [long]$Health['unsafe-commit-violations']) 'Final sampled unsafe-commit count disagrees with summary.'
Require-R3 ([long]$FinalTrajectory.untargeted_branch_disagreements -eq [long]$Health['untargeted-branch-disagreements']) 'Final sampled untargeted-disagreement count disagrees with summary.'

$Repeat=Read-R3Kv (Join-Path $ArtifactRoot '05-deterministic-repeat.txt')
foreach($Key in @('status','steps','run-a-fingerprint','run-b-fingerprint','fingerprint-match','historical-exact-v9-fingerprint-equality-required')){Require-R3KvKey $Repeat $Key}
Require-R3 ($Repeat['status'] -eq 'MODE2-SHADOW-DETERMINISTIC-REPEAT-EVIDENCE-WRITTEN') 'R3 deterministic repeat status mismatch.'
Require-R3 ([int]$Repeat['steps'] -eq 128) 'R3 deterministic repeat step count mismatch.'
Require-R3 ($Repeat['fingerprint-match'] -eq 'True') 'R3 mode2 deterministic repeat mismatch.'
Require-R3 ($Repeat['run-a-fingerprint'] -eq $Repeat['run-b-fingerprint']) 'R3 mode2 repeat fingerprints differ.'
Require-R3 ($Repeat['historical-exact-v9-fingerprint-equality-required'] -eq 'False') 'R3 incorrectly required mode2 to equal historical exact-v9.'

Write-R3Lines (Join-Path $ArtifactRoot '01-contract-and-provenance.txt') @(
    'status=PASS-R3-EVIDENCE-COMPLETE',
    'gate=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION1',
    'r2-returned-adjudication=PASS',
    'r3-planning1-returned-adjudication=PASS',
    'canonical-exact-v9=integrated-operations-desktop-stable@9',
    'canonical-closure-mode=CorrelationConsistentInverseDomain',
    'shadow-candidate-closure-mode=ReferenceConsistentTabulatedInverseDomain',
    'single-factor-change=thermodynamicClosureMode:1->2',
    'ordinary-release-suite=PASS',
    ('execution-contract-sha256='+(FileSha-R3 (Join-Path $Root 'eng\m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1-contract.json'))),
    ('focused-test-normalized-sha256='+(NormSha-R3 $FocusedTestPath)),
    ('production-src-tree-sha256='+$Src.Hash),
    ('historical-tests-tree-sha256='+$Historical.Hash),
    ('planning-contract-sha256='+[string]$Contract.prerequisite.planning_contract_sha256),
    ('planning-returned-audit-sha256='+[string]$Contract.prerequisite.planning_returned_audit_sha256),
    'production-src-change=False',
    'historical-tests-change=False',
    'default-activation=False',
    'exact-v9-change=False',
    'new-exact-version=False',
    'r4-planning-authorized-now=False'
)

Write-R3Lines (Join-Path $ArtifactRoot '06-r3-requalification-summary.txt') @(
    'status=PASS-R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFIED',
    'classification=PASS-R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFIED',
    'ordinary-release-suite=PASS',
    'baseline-equivalence-steps=128',
    'baseline-fingerprint-mismatches=0',
    'mode2-shadow-steps=12000',
    'mode2-shadow-simulated-seconds=120',
    ('electrical-range-mwe='+$Health['electrical-range-mwe']),
    ('primary-pump-range-kg-s='+$Health['primary-pump-range-kg-s']),
    ('drum-level-range='+$Health['drum-level-range']),
    ('governor-output-range-percent='+$Health['governor-output-range-percent']),
    ('minimum-moisture-drain-kg-s='+$Health['minimum-moisture-drain-kg-s']),
    ('max-commanded-transfer-mismatch-kg-s='+$Health['max-commanded-transfer-mismatch-kg-s']),
    ('max-stage-energy-ownership-residual-w='+$Health['max-stage-energy-ownership-residual-w']),
    ('max-network-mass-closure-kg='+$Health['max-network-mass-closure-kg']),
    ('max-network-energy-closure-j='+$Health['max-network-energy-closure-j']),
    ('max-network-balance-mass-rate-kg-s='+$Health['max-network-balance-mass-rate-kg-s']),
    ('max-network-balance-power-w='+$Health['max-network-balance-power-w']),
    'mode2-deterministic-repeat=True',
    'r4-planning-authorized-now=False',
    'next-action=RETURN-COMPLETE-R3-EVIDENCE-FOR-ADJUDICATION'
)

Write-R3Lines (Join-Path $ArtifactRoot '07-prequalification-review.txt') @(
    'status=R3-PREQUALIFICATION-REVIEW-PASS',
    'finding-1=R2-RETURNED-EVIDENCE-PASS',
    'finding-2=R3-PLANNING1-RETURNED-PASS-AS-AUTHORED',
    'finding-3=CANONICAL-EXACT-V9-UNCHANGED',
    'finding-4=MODE1-SHADOW-128-OF-128-FINGERPRINT-EQUIVALENT',
    'finding-5=MODE2-ONLY-SCORED-FACTOR-CHANGE',
    'finding-6=MODE2-SHADOW-120S-12000-STEPS-HEALTH-ENVELOPE-PASS',
    'finding-7=CONSERVATION-AND-MOISTURE-OWNERSHIP-PASS',
    'finding-8=MODE2-DETERMINISTIC-REPEAT-PASS',
    'finding-9=ORDINARY-RELEASE-PASS',
    'finding-10=R4-LONG-MATERIALITY-NOT-EXECUTED',
    'default-activation-authorized=False',
    'exact-v9-change-authorized=False',
    'r4-planning-authorized=False',
    'returned-r3-adjudication-required=True'
)

$Files=@(Get-ChildItem -LiteralPath $ArtifactRoot -File)
Require-R3 ($Files.Count -eq 7) 'R3 evidence set must contain exactly seven files.'
Write-Host 'R3 short exact-v9-equivalent shadow/composition requalification adjudication: PASS' -ForegroundColor Green
