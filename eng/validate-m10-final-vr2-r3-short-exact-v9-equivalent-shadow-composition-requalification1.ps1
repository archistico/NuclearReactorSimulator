$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-R3([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Require-R3Text([string]$Path,[string]$Needle){
    Require-R3 (Test-Path -LiteralPath $Path -PathType Leaf) ("Missing file: {0}" -f $Path)
    $Text=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8)
    Require-R3 ($Text.IndexOf($Needle,[StringComparison]::Ordinal)-ge 0) ("Missing marker in {0}: {1}" -f $Path,$Needle)
}
function Sha-R3([byte[]]$Bytes){$Sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($Sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant()}finally{$Sha.Dispose()}}
function FileSha-R3([string]$Path){Sha-R3 ([IO.File]::ReadAllBytes($Path))}
function NormSha-R3([string]$Path){$Text=[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n");Sha-R3 ([Text.Encoding]::UTF8.GetBytes($Text))}
function TreeSha-R3([string]$Root,[string[]]$ExcludeRel=@()){
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
$Contract=Get-Content 'eng\m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1-contract.json' -Raw | ConvertFrom-Json

Require-R3 ($Contract.schema -eq 'm10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1-v1') 'R3 execution schema mismatch.'
Require-R3 ($Contract.status -eq 'EXECUTION-CANDIDATE') 'R3 execution status mismatch.'
Require-R3 ($Contract.prerequisite.r2_returned_adjudication -eq 'PASS') 'R2 returned adjudication prerequisite drift.'
Require-R3 ($Contract.prerequisite.r3_planning_gate -eq 'R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1') 'R3 planning gate identity drift.'
Require-R3 ($Contract.prerequisite.canonical_exact_v9 -eq 'integrated-operations-desktop-stable@9') 'Canonical exact-v9 identity drift.'
Require-R3 ($Contract.prerequisite.canonical_closure_mode -eq 'CorrelationConsistentInverseDomain') 'Canonical closure-mode identity drift.'
Require-R3 ($Contract.prerequisite.shadow_candidate_closure_mode -eq 'ReferenceConsistentTabulatedInverseDomain') 'Shadow closure-mode identity drift.'
Require-R3 ($Contract.prerequisite.planning_returned_adjudication -eq 'PASS') 'R3 Planning 1 returned adjudication missing.'
Require-R3 ([bool]$Contract.authority.r3_execution_authorized_now) 'R3 execution authority missing.'
foreach($Name in @('production_default_switch_authorized','production_runtime_change_authorized','exact_v9_change_authorized','new_exact_version_authorized','threshold_change_authorized','r4_planning_authorized_now','vr3_authorized','p3_r1_authorized','second_replacement_long_authorized')){
    Require-R3 (-not [bool]$Contract.authority.$Name) ("Unauthorized authority true: {0}" -f $Name)
}

$PlanningContractPath=Join-Path $Root 'eng\m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification-planning1-contract.json'
Require-R3 ((FileSha-R3 $PlanningContractPath)-eq [string]$Contract.prerequisite.planning_contract_sha256) 'R3 Planning 1 contract hash drift.'
$Audit=Join-Path $Root $Contract.prerequisite.planning_returned_audit.Replace('/','\')
Require-R3 ((FileSha-R3 $Audit)-eq [string]$Contract.prerequisite.planning_returned_audit_sha256) 'R3 planning returned-audit hash drift.'
Require-R3Text $Audit 'status=PASS'
Require-R3Text $Audit 'r3-execution-authorized=True'
Require-R3Text (Join-Path $Root 'docs\M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md') 'PASS-AS-AUTHORED / RETURNED-EVIDENCE ADJUDICATED'
Require-R3Text (Join-Path $Root 'docs\PROJECT.md') '.\scripts\run-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1.cmd'
Require-R3Text (Join-Path $Root 'docs\PROJECT.md') 'R3 execution CURRENT'

$PlanningRoot=Join-Path $Root $Contract.planning_returned_artifacts.root.Replace('/','\')
$PlanningProps=@($Contract.planning_returned_artifacts.sha256.PSObject.Properties)
Require-R3 ($PlanningProps.Count -eq 4) 'R3 planning artifact manifest count drift.'
Require-R3 (@(Get-ChildItem -LiteralPath $PlanningRoot -File).Count -eq 4) 'R3 frozen planning artifact count drift.'
foreach($Prop in $PlanningProps){
    $File=Join-Path $PlanningRoot $Prop.Name
    Require-R3 ((FileSha-R3 $File)-eq [string]$Prop.Value) ("R3 planning artifact hash drift: {0}" -f $Prop.Name)
}
Require-R3Text (Join-Path $PlanningRoot '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-R3Text (Join-Path $PlanningRoot '03-acceptance-and-successor-summary.txt') 'future-gate=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION1'
Require-R3Text (Join-Path $PlanningRoot '04-preexecution-review.txt') 'finding-4=MODE2-IS-THE-ONLY-SCORED-FACTOR-CHANGE'

$Src=TreeSha-R3 (Join-Path $Root 'src')
Require-R3 ($Src.Count -eq [int]$Contract.baseline.src_file_count -and $Src.Hash -eq [string]$Contract.baseline.src_tree_sha256) 'Production src tree drift.'

$TestRel=[string]$Contract.execution.new_test_file
$Historical=TreeSha-R3 (Join-Path $Root 'tests') @($TestRel.Substring('tests/'.Length))
Require-R3 ($Historical.Count -eq [int]$Contract.baseline.tests_file_count -and $Historical.Hash -eq [string]$Contract.baseline.tests_tree_sha256) 'Historical tests tree drift.'

$Test=Join-Path $Root $TestRel.Replace('/','\')
Require-R3 ((NormSha-R3 $Test)-eq [string]$Contract.execution.new_test_normalized_sha256) 'R3 focused test hash drift.'
Require-R3Text $Test 'CreateExactV9EquivalentShadow'
Require-R3Text $Test 'WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain'
Require-R3Text $Test 'WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain'
Require-R3Text $Test 'BaselineEquivalenceSteps = 128'
Require-R3Text $Test 'HealthSteps = 12_000'
Require-R3Text $Test 'DeterminismSteps = 128'
Require-R3Text $Test 'TimeSpan.FromMilliseconds(10d)'
Require-R3Text $Test 'ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed'
Require-R3Text $Test 'new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine()'
Require-R3Text $Test 'initialRequestedElectricalPowerMegawatts: 5d'
Require-R3Text $Test 'useFourNodeBranchContinuityCorrectedCommitOptIn: true'
Require-R3Text $Test 'turbineMoistureDrainNodeId: "hotwell"'
Require-R3Text $Test 'r4-long-materiality-executed=False'
$RunnerPath=Join-Path $Root ([string]$Contract.gate_files.runner.path).Replace('/','\')
Require-R3Text $RunnerPath 'if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"'
Require-R3Text $RunnerPath 'if exist "%REPORT_DIR%" goto :artifact_cleanup_fail'
Require-R3Text $RunnerPath 'if not exist "%REPORT_DIR%" goto :artifact_create_fail'

foreach($Prop in $Contract.baseline.exact_v9_provenance_files_sha256.PSObject.Properties){
    $Path=Join-Path $Root ([string]$Prop.Value.path).Replace('/','\')
    Require-R3 ((FileSha-R3 $Path)-eq [string]$Prop.Value.sha256) ("Exact-v9 provenance hash drift: {0}" -f $Prop.Name)
}

$Env=$Contract.execution.inherited_exact_v9_health_envelope
Require-R3 ([int]$Contract.execution.baseline_equivalence_steps -eq 128) 'Baseline equivalence step count drift.'
Require-R3 ([int]$Contract.execution.mode2_shadow_steps -eq 12000) 'Mode2 shadow step count drift.'
Require-R3 ([double]$Contract.execution.simulated_seconds -eq 120.0) 'Mode2 shadow duration drift.'
Require-R3 ([double]$Contract.execution.fixed_timestep_ms -eq 10.0) 'Fixed timestep drift.'
Require-R3 ([bool]$Contract.execution.ordinary_release_suite_required) 'Ordinary Release requirement drift.'
Require-R3 ([bool]$Contract.execution.forced_focused_rebuild_required) 'Forced focused rebuild requirement drift.'
Require-R3 ($Contract.execution.single_factor_change -eq 'thermodynamicClosureMode:1->2') 'Single-factor change contract drift.'
Require-R3 ([int]$Contract.execution.trajectory_stride_steps -eq 100) 'Trajectory stride drift.'
Require-R3 ([int]$Contract.execution.mode2_deterministic_repeat_steps -eq 128) 'Mode2 deterministic-repeat step count drift.'
Require-R3 (-not [bool]$Contract.execution.r4_long_materiality_execution_allowed) 'R4 long materiality must remain forbidden in R3.'
Require-R3 (-not [bool]$Contract.execution.canonical_exact_v9_change_allowed) 'Canonical exact-v9 mutation must remain forbidden in R3.'
Require-R3 (-not [bool]$Contract.execution.default_mode2_activation_allowed) 'Default mode2 activation must remain forbidden in R3.'
Require-R3 (-not [bool]$Contract.execution.new_exact_version_allowed) 'New exact-version identity must remain forbidden in R3.'
Require-R3 ([int]$Contract.execution.required_files -eq 7) 'R3 required artifact count drift.'
$ExpectedOutputs=@(
    '01-contract-and-provenance.txt',
    '02-shadow-baseline-equivalence.csv',
    '03-mode2-shadow-health-trajectory.csv',
    '04-ownership-conservation-summary.txt',
    '05-deterministic-repeat.txt',
    '06-r3-requalification-summary.txt',
    '07-prequalification-review.txt'
)
Require-R3 (@($Contract.execution.outputs).Count -eq $ExpectedOutputs.Count) 'R3 output manifest count drift.'
for($Index=0;$Index -lt $ExpectedOutputs.Count;$Index++){
    Require-R3 ([string]$Contract.execution.outputs[$Index] -eq $ExpectedOutputs[$Index]) ("R3 output manifest drift at index {0}." -f $Index)
}
Require-R3 ($Contract.execution.classification_on_pass -eq 'PASS-R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFIED') 'R3 PASS classification drift.'
Require-R3 ($Contract.execution.classification_on_failure -eq 'R3-SHADOW-COMPOSITION-BLOCKING') 'R3 failure classification drift.'
Require-R3 ([bool]$Contract.execution.returned_adjudication_required_before_r4_planning) 'Returned R3 adjudication requirement drift.'
Require-R3 ($Contract.execution.post_successor -eq 'R4-P1B-EQUIVALENT-LONG-MATERIALITY-RECHECK-PLANNING1') 'R3 post-successor drift.'
Require-R3 ([double]$Env.electrical_export_mwe.minimum -eq 4.99 -and [double]$Env.electrical_export_mwe.maximum -eq 5.01) 'Electrical envelope drift.'
Require-R3 ([double]$Env.primary_pump_mass_flow_kg_s.minimum -eq 99.9 -and [double]$Env.primary_pump_mass_flow_kg_s.maximum -eq 100.1) 'Primary-flow envelope drift.'
Require-R3 ([double]$Env.drum_level_fraction.minimum -eq .49 -and [double]$Env.drum_level_fraction.maximum -eq .51) 'Drum-level envelope drift.'
Require-R3 ([double]$Env.governor_output_percent.minimum -eq 29.27 -and [double]$Env.governor_output_percent.maximum -eq 29.30) 'Governor envelope drift.'
Require-R3 ([double]$Env.maximum_commanded_transfer_mismatch_kg_s -eq 1e-8) 'Transfer ceiling drift.'
Require-R3 ([double]$Env.maximum_stage_energy_ownership_residual_w -eq 1e-3) 'Ownership ceiling drift.'
Require-R3 ([double]$Env.maximum_mass_closure_residual_kg -eq 1e-6) 'Mass closure ceiling drift.'
Require-R3 ([double]$Env.maximum_full_energy_closure_residual_j -eq 1e-2) 'Energy closure ceiling drift.'
Require-R3 ([double]$Env.maximum_balance_mass_rate_residual_kg_s -eq 1e-8) 'Balance mass-rate ceiling drift.'
Require-R3 ([double]$Env.maximum_balance_power_residual_w -eq 1e-3) 'Balance power ceiling drift.'

foreach($Prop in $Contract.documentation_files.PSObject.Properties){
    $Path=Join-Path $Root ([string]$Prop.Value.path).Replace('/','\')
    Require-R3 ((NormSha-R3 $Path)-eq [string]$Prop.Value.normalized_sha256) ("R3 execution doc hash drift: {0}" -f $Prop.Name)
}
foreach($Prop in $Contract.gate_files.PSObject.Properties){
    $Path=Join-Path $Root ([string]$Prop.Value.path).Replace('/','\')
    Require-R3 ((NormSha-R3 $Path)-eq [string]$Prop.Value.normalized_sha256) ("R3 execution gate hash drift: {0}" -f $Prop.Name)
}

Write-Host 'R3 short exact-v9-equivalent shadow/composition execution static audit: PASS' -ForegroundColor Green
Write-Host 'No default/exact-v9/R4 authority is granted.'
