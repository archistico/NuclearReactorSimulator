param([string]$ContractPath = '', [string]$ArtifactDirectory = '')

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = Split-Path -Parent $PSScriptRoot
if (-not $ContractPath) { $ContractPath = Join-Path $PSScriptRoot 'r3-equilibrium-reconstruction-planning1-contract.json' }
if (-not $ArtifactDirectory) { $ArtifactDirectory = Join-Path $Root 'artifacts\r3-eq-h-plan1' }
$ArtifactDirectory = [IO.Path]::GetFullPath($ArtifactDirectory)
$artifactPrefix = (Join-Path $Root 'artifacts').TrimEnd('\') + '\'
if (-not $ArtifactDirectory.StartsWith($artifactPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Planning output must be inside repository artifacts.' }
[IO.Directory]::CreateDirectory($ArtifactDirectory) | Out-Null
foreach ($name in @('01-provenance.json','02-returned-review.json','03-residual-scales.csv','04-planning-summary.json','05-manifest.csv')) {
    $path = Join-Path $ArtifactDirectory $name
    if ([IO.File]::Exists($path)) { [IO.File]::Delete($path) }
}

function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Hash-File([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
}
function Tree([string]$Directory) {
    $paths = [string[]]@([IO.Directory]::EnumerateFiles((Join-Path $Root $Directory), '*', [IO.SearchOption]::AllDirectories) |
        ForEach-Object { $_.Substring($Root.Length + 1).Replace('\', '/') } |
        Where-Object { $_ -notmatch '(^|/)(bin|obj)/' })
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $builder = New-Object Text.StringBuilder
    foreach ($path in $paths) { [void]$builder.Append($path + '|' + (Hash-File (Join-Path $Root $path)) + "`n") }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($builder.ToString()))).Replace('-', '') }
    finally { $sha.Dispose() }
    return [ordered]@{ file_count = $paths.Count; tree_sha256 = $hash }
}
function D($Row, [string]$Key) {
    $value = [double]::Parse([string]$Row.$Key, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture)
    Require (-not [double]::IsNaN($value) -and -not [double]::IsInfinity($value)) "Non-finite $Key"
    return $value
}
function F([double]$Value) { return $Value.ToString('R', [Globalization.CultureInfo]::InvariantCulture) }
function Same([double]$Left, [double]$Right, [string]$Message) {
    Require ([BitConverter]::DoubleToInt64Bits($Left) -eq [BitConverter]::DoubleToInt64Bits($Right)) $Message
}
function ULP([double]$Value) {
    $exponent = ([BitConverter]::DoubleToInt64Bits([Math]::Abs($Value)) -shr 52) -band 2047
    if ($exponent -eq 0) { return [double]::Epsilon }
    return [Math]::Pow(2.0, $exponent - 1023 - 52)
}
function Maximum($Rows, [string]$Key) { return ($Rows | ForEach-Object { [Math]::Abs((D $_ $Key)) } | Measure-Object -Maximum).Maximum }
function One($Rows, [string]$Mode, [string]$Checkpoint) {
    $items = @($Rows | Where-Object { $_.mode -eq $Mode -and $_.checkpoint -eq $Checkpoint })
    Require ($items.Count -eq 1) "Checkpoint identity drift: $Mode/$Checkpoint"
    return $items[0]
}
function Write-Json([string]$Name, $Value) {
    [IO.File]::WriteAllText((Join-Path $ArtifactDirectory $Name), ($Value | ConvertTo-Json -Depth 12) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
}

$contract = [IO.File]::ReadAllText($ContractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'r3-equilibrium-reconstruction-planning1-v1') 'Planning schema drift'
$trees = [ordered]@{}
foreach ($directory in @('src','tests')) {
    $trees[$directory] = Tree $directory
    Require ($trees[$directory].file_count -eq $contract.baseline.$directory.file_count) "$directory count drift"
    Require ($trees[$directory].tree_sha256 -eq $contract.baseline.$directory.tree_sha256) "$directory content drift"
}
foreach ($entry in $contract.frozen_inputs) {
    Require ((Hash-File (Join-Path $Root $entry.path)) -eq $entry.sha256) ('Frozen input drift: ' + $entry.path)
}
foreach ($entry in $contract.document_markers.PSObject.Properties) {
    $lines = [IO.File]::ReadAllLines((Join-Path $Root $entry.Name))
    foreach ($marker in $entry.Value) {
        Require (@($lines | Where-Object { $_.Trim() -ceq $marker }).Count -eq 1) "Marker cardinality drift: $marker"
    }
}
foreach ($flag in @('production_change','test_source_change','candidate_search_execution','production_seed_installation','controller_initial_state_change','physics_retuning','threshold_change','requalification3','r3_passed','r4')) {
    Require ($contract.authority.$flag -is [bool] -and -not $contract.authority.$flag) "Authority drift: $flag"
}
Require ($contract.authority.test_only_h_implementation -is [bool] -and $contract.authority.test_only_h_implementation) 'Missing test-only H implementation authority'
Require ($contract.authority.next_activity -eq 'R3-H-CONSERVED-INVENTORY-FEASIBILITY1-IMPLEMENTATION-AND-PREEXECUTION-REVIEW') 'Next activity drift'
Require ($contract.selection -eq 'H-TEST-ONLY-FEASIBILITY-FIRST') 'Family selection drift'
Require (($contract.unselected_families -join ',') -eq 'C,J') 'C/J must remain unselected'
Require (($contract.search.variable_order -join ',') -eq 'control-out,drum,exhaust,feedwater-inventory,header,hotwell,outlet,pressure,steam,stop-out,suction,turbine-inlet') 'Variable identity/order drift'
Require (($contract.search.variables_per_node -join ',') -eq 'mass_kg,internal_energy_j') 'Unknown surface drift'
Require ($contract.search.fractional_bound -eq 0.02 -and $contract.search.maximum_iterations -eq 64 -and $contract.search.maximum_recipe_evaluations -eq 4096) 'Search bound drift'
Require ($contract.search.central_difference_step -eq 0.000001 -and $contract.search.initial_damping -eq 0.001) 'Search initialization drift'
Require ($contract.search.maximum_line_halvings -eq 12 -and $contract.search.maximum_damping_attempts -eq 6) 'Search retry budget drift'
Require ($contract.search.damping_min -eq 1e-12 -and $contract.search.damping_max -eq 1e12 -and $contract.search.damping_factor -eq 10 -and $contract.search.pivot_floor -eq 1e-14) 'Linear solve/damping drift'
Require ($contract.search.method -eq 'DAMPED-LEAST-SQUARES-NORMAL-EQUATIONS-ORDINAL-PARTIAL-PIVOT') 'Solver method drift'
Require ($contract.search.acceptance -eq 'STRICT-MAX-ABS-SCALED-RESIDUAL-DECREASE' -and $contract.search.stop_at_first_scaled_root -eq $true) 'Search acceptance drift'
Require ($contract.search.residual_count -eq 75 -and $contract.search.scaled_root_max -eq 1 -and $contract.search.initial_guess -eq 'FROZEN-MODE2-RAW-VECTOR') 'Residual dimension/root/start drift'
Require ($contract.search.count_failed_recipes -eq $true -and $contract.search.clip_out_of_bounds -eq $false -and $contract.search.restart_after_failed_confirmation -eq $false) 'Search accounting/continuation drift'
Require ($contract.search.scale_ulp_factor -eq 64 -and $contract.search.timestep_seconds -eq 0.01) 'Scale construction drift'
Require ($contract.search.seed_steps -eq 2 -and $contract.search.running_evaluation_steps -eq 1 -and $contract.search.confirmation_runs -eq 3 -and $contract.search.confirmation_running_steps -eq 100) 'Evaluation/confirmation drift'
Require (($contract.search.evaluation_checkpoints -join ',') -eq 'seed-step1,post-seed,1') 'Residual checkpoint drift'
Require (($contract.outcomes -join ',') -eq 'H-FEASIBLE-FLUID-SEED,H-CANDIDATE-NOT-QUALIFIED,H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS,HARNESS-FAIL') 'Outcome drift'
Require (($contract.fixed_nonfluid_initial_conditions -join ',') -eq 'thermal-body-states,neutrons-precursors-poison-decay,rod-positions,pump-valve-biases,controller-memory-and-modes,rotor-speed,generator-breaker-load-grid,geometry-resistances-capacities,control-gains,protection,thermodynamic-transport-laws') 'Fixed non-fluid surface drift'
foreach ($entry in @(
    @{ Key = 'electrical_mwe'; Low = 4.99; High = 5.01 },
    @{ Key = 'primary_pump_kg_s'; Low = 99.9; High = 100.1 },
    @{ Key = 'drum_level'; Low = 0.49; High = 0.51 },
    @{ Key = 'governor_percent'; Low = 29.27; High = 29.30 })) {
    $bounds = @($contract.frozen_envelope.($entry.Key))
    Require ($bounds.Count -eq 2) ('Envelope shape drift: ' + $entry.Key)
    Same $bounds[0] $entry.Low ('Envelope lower drift: ' + $entry.Key)
    Same $bounds[1] $entry.High ('Envelope upper drift: ' + $entry.Key)
}
Require ($contract.frozen_envelope.trip_allowed -eq $false -and $contract.frozen_envelope.breaker_closed -eq $true -and $contract.frozen_envelope.rollback_allowed -eq $false) 'Health envelope drift'

$evidence = Join-Path $Root 'eng\frozen-evidence\r3-dyn-eq-d1'
$manifest = @(Import-Csv (Join-Path $evidence '07-manifest.csv'))
Require ($manifest.Count -eq 9) 'Returned manifest count drift'
foreach ($entry in $manifest) { Require ((Hash-File (Join-Path $evidence $entry.file)) -eq $entry.sha256) ('Returned payload drift: ' + $entry.file) }
$nodes = @(Import-Csv (Join-Path $evidence '01-node-residuals.csv'))
$hydraulics = @(Import-Csv (Join-Path $evidence '02-hydraulics-drum.csv'))
$controllers = @(Import-Csv (Join-Path $evidence '03-controller-rotor.csv'))
$balances = @(Import-Csv (Join-Path $evidence '04-conservation.csv'))
$health = @(Import-Csv (Join-Path $evidence '05-health.csv'))
Require ($nodes.Count -eq 240 -and $hydraulics.Count -eq 18 -and $controllers.Count -eq 18 -and $balances.Count -eq 18 -and $health.Count -eq 204) 'Returned CSV cardinality drift'
$checkpoints = @('seed-step1','post-seed','1','2','5','10','20','50','100')
foreach ($mode in @('mode1','candidate-mode2')) {
    foreach ($checkpoint in @('raw') + $checkpoints) {
        $rows = @($nodes | Where-Object { $_.mode -eq $mode -and $_.checkpoint -eq $checkpoint })
        Require ($rows.Count -eq 12 -and (@($rows.node | Sort-Object) -join ',') -eq ($contract.search.variable_order -join ',')) 'Returned node identity drift'
    }
    foreach ($checkpoint in $checkpoints) {
        $null = One $hydraulics $mode $checkpoint
        $null = One $controllers $mode $checkpoint
        $null = One $balances $mode $checkpoint
    }
    foreach ($node in $contract.search.variable_order) {
        $nodeRows = @($nodes | Where-Object { $_.mode -eq $mode -and $_.node -eq $node })
        foreach ($pair in @(@('raw','seed-step1'),@('seed-step1','post-seed'),@('post-seed','1'),@('1','2'))) {
            $before = One $nodeRows $mode $pair[0]
            $after = One $nodeRows $mode $pair[1]
            Same (((D $after 'mass_kg') - (D $before 'mass_kg')) / 0.01) (D $after 'mass_storage_residual_kg_s') "Mass rate identity drift: $mode/$node/$($pair[1])"
            Same (((D $after 'energy_j') - (D $before 'energy_j')) / 0.01) (D $after 'energy_storage_residual_w') "Energy rate identity drift: $mode/$node/$($pair[1])"
        }
    }
    foreach ($step in @('seed-step1','post-seed') + @(1..100 | ForEach-Object { [string]$_ })) {
        $rows = @($health | Where-Object { $_.mode -eq $mode -and $_.step -eq $step })
        Require ($rows.Count -eq 1) 'Returned health identity drift'
        Require ($rows[0].finite -eq 'true' -and $rows[0].trip -eq 'false' -and $rows[0].breaker_closed -eq 'true' -and $rows[0].rollbacks -eq '0') 'Returned health sentinel RED'
    }
}

$scales = @()
foreach ($node in $contract.search.variable_order) {
    $canonical = @($nodes | Where-Object { $_.mode -eq 'mode1' -and $_.node -eq $node })
    $rates = @($canonical | Where-Object { $_.checkpoint -ne 'raw' })
    $massMax = Maximum $rates 'mass_storage_residual_kg_s'
    $energyMax = Maximum $rates 'energy_storage_residual_w'
    $massUlp = 64 * (ULP (Maximum $canonical 'mass_kg')) / 0.01
    $energyUlp = 64 * (ULP (Maximum $canonical 'energy_j')) / 0.01
    $massScale = [Math]::Max($massMax, $massUlp)
    $energyScale = [Math]::Max($energyMax, $energyUlp)
    $frozen = @($contract.residual_scales | Where-Object { $_.node -eq $node })
    Require ($frozen.Count -eq 1) "Missing scale: $node"
    Same $massScale (D $frozen[0] 'mass_scale_kg_s') "Mass scale drift: $node"
    Same $energyScale (D $frozen[0] 'energy_scale_w') "Energy scale drift: $node"
    $scales += [PSCustomObject][ordered]@{ node = $node; canonical_mass_rate_max = F $massMax; canonical_energy_rate_max = F $energyMax; mass_ulp_floor = F $massUlp; energy_ulp_floor = F $energyUlp; mass_scale_kg_s = F $massScale; energy_scale_w = F $energyScale }
}
Require ($contract.residual_scales.Count -eq 12) 'Residual scale count drift'
$canonicalControl = @($controllers | Where-Object { $_.mode -eq 'mode1' })
$canonicalTorqueMagnitude = [Math]::Max((Maximum $canonicalControl 'turbine_torque_nm'), [Math]::Max((Maximum $canonicalControl 'effective_load_torque_nm'), (Maximum $canonicalControl 'passive_loss_torque_nm')))
$torqueScale = [Math]::Max((Maximum $canonicalControl 'net_torque_nm'), (64 * (ULP $canonicalTorqueMagnitude)))
Same $torqueScale (D $contract 'rotor_torque_scale_nm') 'Rotor torque scale drift'

$a = One $controllers 'mode1' 'seed-step1'
$b = One $controllers 'candidate-mode2' 'seed-step1'
foreach ($key in @('setpoint_rpm','measurement_rpm','error_rpm','integral','output_percent','rotor_initial_rpm','effective_load_torque_nm','control_valve_fraction')) {
    Same (D $a $key) (D $b $key) "Initial control/rotor/load mismatch: $key"
}
$candidateSeed = @($nodes | Where-Object { $_.mode -eq 'candidate-mode2' -and $_.checkpoint -eq 'seed-step1' })
$c100 = One $controllers 'candidate-mode2' '100'
$c1 = One $controllers 'candidate-mode2' '1'
$postSeed = One $controllers 'candidate-mode2' 'post-seed'
$outlet = @($candidateSeed | Where-Object { $_.node -eq 'outlet' })[0]
$inlet = @($candidateSeed | Where-Object { $_.node -eq 'turbine-inlet' })[0]
Require ((D $outlet 'energy_storage_residual_w') -lt 0 -and (D $inlet 'energy_storage_residual_w') -gt 0) 'Energy residual direction drift'
Require ([Math]::Abs((D $postSeed 'net_torque_nm')) -gt [Math]::Abs((D $b 'net_torque_nm'))) 'Preconditioning torque deficit not established'
$governorDelta = (D $c100 'output_percent') - (D $c1 'output_percent')
$returnedSummary = [IO.File]::ReadAllText((Join-Path $evidence '06-diagnostic-summary.json')) | ConvertFrom-Json
Same $governorDelta (D $returnedSummary 'candidate_governor_delta_percent') 'Returned governor summary drift'
Same (D $outlet 'energy_storage_residual_w') (D $returnedSummary 'largest_seed_step1_storage_rate_w') 'Returned storage summary drift'
$violations = @($health | Where-Object { $_.mode -eq 'candidate-mode2' -and $_.step -match '^\d+$' -and $_.in_frozen_envelope -eq 'false' }).Count
Require ($violations -eq 84 -and $violations -eq $returnedSummary.candidate_envelope_violation_steps) 'Returned envelope accounting drift'
$regression = [IO.File]::ReadAllText((Join-Path $evidence '08-regression-summary.json')) | ConvertFrom-Json
Require ($regression.status -eq 'PASS' -and $regression.ordinary_failed -eq 0 -and $regression.current_evidence -eq 'PASS' -and $regression.authoritative_exact_v9 -eq 'PASS') 'Prerequisite regression not PASS'
$regressionLog = Join-Path $Root $regression.log_path
$regressionLogVerified = [IO.File]::Exists($regressionLog)
if ($regressionLogVerified) { Require ((Hash-File $regressionLog) -eq $regression.log_sha256) 'Inherited raw regression log drift' }

Write-Json '01-provenance.json' ([ordered]@{ status = 'PASS-AS-AUTHORED'; planning_contract_sha256 = Hash-File $ContractPath; baseline = $trees; production_change = $false; test_source_change = $false; returned_manifest_sha256 = Hash-File (Join-Path $evidence '07-manifest.csv'); inherited_regression = $regression; inherited_raw_log_present_and_verified = $regressionLogVerified; ordinary_suite_rerun = $false })
Write-Json '02-returned-review.json' ([ordered]@{ status = 'PASS-RETURNED-EVIDENCE-REVIEW'; selection = $contract.selection; outlet_seed_step1_energy_rate_w = F (D $outlet 'energy_storage_residual_w'); turbine_inlet_seed_step1_energy_rate_w = F (D $inlet 'energy_storage_residual_w'); initial_control_actuator_rotor_load_equal = $true; candidate_post_seed_net_torque_nm = F (D $postSeed 'net_torque_nm'); candidate_final_rotor_rpm = F (D $c100 'rotor_final_rpm'); governor_delta_percent = F $governorDelta; envelope_violations = $violations; independent_controller_defect_proven = $false; h_sufficiency_proven = $false; max_suction_energy_identity_abs_w = F (Maximum $balances 'suction_energy_identity_residual_w'); max_network_energy_closure_abs_j = F (Maximum $balances 'network_energy_closure_j') })
$scales | Export-Csv -LiteralPath (Join-Path $ArtifactDirectory '03-residual-scales.csv') -NoTypeInformation -Encoding ASCII
Write-Json '04-planning-summary.json' ([ordered]@{ status = 'PASS-AS-AUTHORED'; selection = $contract.selection; unknown_count = 24; rotor_torque_scale_nm = F $torqueScale; search = $contract.search; authority = $contract.authority; outcomes = $contract.outcomes; ordinary_suite_rerun = $false })
$outputManifest = foreach ($name in @('01-provenance.json','02-returned-review.json','03-residual-scales.csv','04-planning-summary.json')) {
    [PSCustomObject]@{ file = $name; sha256 = Hash-File (Join-Path $ArtifactDirectory $name) }
}
$outputManifest | Export-Csv -LiteralPath (Join-Path $ArtifactDirectory '05-manifest.csv') -NoTypeInformation -Encoding ASCII
Write-Host 'R3 Equilibrium Reconstruction Planning 1: PASS-AS-AUTHORED.'
Write-Host 'H test-only feasibility implementation/preexecution review is next. R3 remains RED; no production seed or controller change.'
