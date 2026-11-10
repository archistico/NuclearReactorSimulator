param([switch]$Adjudicate)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = Split-Path -Parent $PSScriptRoot
$Contract = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'r3-post-repair-dynamic-equilibrium-residual-diagnostic1-contract.json') -Raw | ConvertFrom-Json
$Out = Join-Path $Root 'artifacts\r3-dyn-eq-d1'

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Hash-File([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
}
function Tree([string]$Directory, [string]$Exclude = '') {
    $paths = [string[]]@([IO.Directory]::EnumerateFiles((Join-Path $Root $Directory), '*', [IO.SearchOption]::AllDirectories) |
        ForEach-Object { $_.Substring($Root.Length + 1).Replace('\', '/') } |
        Where-Object { $_ -notmatch '(^|/)(bin|obj)/' -and $_ -ne $Exclude })
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $text = New-Object Text.StringBuilder
    foreach ($path in $paths) { [void]$text.Append($path + '|' + (Hash-File (Join-Path $Root $path)) + "`n") }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text.ToString()))).Replace('-', '') }
    finally { $sha.Dispose() }
    return @{ Count = $paths.Count; Hash = $hash }
}

Require ($Contract.schema -eq 'r3-post-repair-dynamic-equilibrium-residual-diagnostic1-v1') 'schema drift'
foreach ($flag in @('production_change','retuning','threshold_change','reconstruction_family_selected','r3_passed','requalification3_authorized','r4_authorized')) {
    Require ($Contract.authority.$flag -is [bool] -and -not $Contract.authority.$flag) "authority drift: $flag"
}
Require ($Contract.authority.next_activity -eq 'REVIEW-DIAGNOSTIC1-EQUILIBRIUM-RECONSTRUCTION-PLANNING-ONLY') 'next activity drift'
Require ($Contract.workload.timestep_ms -eq 10 -and $Contract.workload.running_steps_per_mode -eq 100) 'workload drift'
Require (($Contract.workload.checkpoints -join ',') -eq '0,1,2,5,10,20,50,100') 'checkpoint drift'
Require (($Contract.workload.modes -join ',') -eq 'mode1,candidate-mode2') 'mode drift'
foreach ($directory in @('src', 'tests')) {
    $excluded = if ($directory -eq 'tests') { [string]$Contract.diagnostic_test.path } else { '' }
    $tree = Tree $directory $excluded
    Require ($tree.Count -eq $Contract.baseline.$directory.file_count) "$directory count drift"
    Require ($tree.Hash -eq $Contract.baseline.$directory.tree_sha256) "$directory frozen content drift"
}
foreach ($file in @($Contract.frozen_inputs) + @($Contract.diagnostic_test)) {
    Require ((Hash-File (Join-Path $Root $file.path)) -eq $file.sha256) ('frozen file drift: ' + $file.path)
}
Write-Host 'R3 dynamic-equilibrium residual diagnostic: static PASS (production and prior tests unchanged).'
if (-not $Adjudicate) { exit 0 }

function Read-Rows([string]$Name, [int]$Expected) {
    $path = Join-Path $Out $Name
    $expectedHeader = [string]$Contract.evidence_headers.$Name
    Require (([IO.File]::ReadLines($path) | Select-Object -First 1) -eq $expectedHeader) "$Name schema drift"
    $rows = @(Import-Csv -LiteralPath $path)
    Require ($rows.Count -eq $Expected) "$Name row count drift"
    foreach ($row in $rows) {
        foreach ($field in $row.PSObject.Properties) {
            if ($field.Name -in @('mode','checkpoint','node','phase','step')) { continue }
            if ($field.Name -in @('trip','breaker_closed','finite','in_frozen_envelope')) {
                Require ($field.Value -in @('true','false')) "$Name invalid boolean: $($field.Name)"
                continue
            }
            if ($field.Value -eq 'null') {
                Require ($field.Name -eq 'quality' -or
                    ($Name -eq '01-node-residuals.csv' -and $row.checkpoint -eq 'raw' -and
                     $field.Name -in @('mass_storage_residual_kg_s','energy_storage_residual_w','pressure_step_delta_pa'))) "$Name unexpected null: $($field.Name)"
                continue
            }
            $value = [double]::Parse($field.Value, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture)
            Require (-not [double]::IsNaN($value) -and -not [double]::IsInfinity($value)) "$Name non-finite field"
        }
    }
    return $rows
}
function Number($Row, [string]$Key) {
    return [double]::Parse([string]$Row.$Key, [Globalization.CultureInfo]::InvariantCulture)
}
function Select-One($Rows, [string]$Mode, [string]$Checkpoint) {
    $matched = @($Rows | Where-Object { $_.mode -eq $Mode -and $_.checkpoint -eq $Checkpoint })
    Require ($matched.Count -eq 1) "missing or duplicate $Mode/$Checkpoint"
    return $matched[0]
}
$nodes = Read-Rows '01-node-residuals.csv' 240
$hydraulics = Read-Rows '02-hydraulics-drum.csv' 18
$controllers = Read-Rows '03-controller-rotor.csv' 18
$balances = Read-Rows '04-conservation.csv' 18
$health = Read-Rows '05-health.csv' 204
$nodeIds = @('control-out','drum','exhaust','feedwater-inventory','header','hotwell','outlet','pressure','steam','stop-out','suction','turbine-inlet')
$checkpoints = @('seed-step1','post-seed','1','2','5','10','20','50','100')
foreach ($mode in @('mode1','candidate-mode2')) {
    foreach ($checkpoint in @('raw') + $checkpoints) {
        $rows = @($nodes | Where-Object { $_.mode -eq $mode -and $_.checkpoint -eq $checkpoint })
        Require ($rows.Count -eq 12) "node checkpoint count: $mode/$checkpoint"
        Require ((@($rows.node | Sort-Object) -join '|') -eq ($nodeIds -join '|')) "node identity drift: $mode/$checkpoint"
    }
    foreach ($checkpoint in $checkpoints) {
        foreach ($rows in @(@{ Data = $hydraulics }, @{ Data = $controllers }, @{ Data = $balances })) {
            $null = Select-One $rows.Data $mode $checkpoint
        }
    }
    $steps = @('seed-step1','post-seed') + @(1..100 | ForEach-Object { [string]$_ })
    foreach ($step in $steps) {
        $rows = @($health | Where-Object { $_.mode -eq $mode -and $_.step -eq $step })
        Require ($rows.Count -eq 1) "health checkpoint drift: $mode/$step"
        Require ($rows[0].finite -eq 'true' -and $rows[0].trip -eq 'false' -and $rows[0].breaker_closed -eq 'true' -and $rows[0].rollbacks -eq '0') "health sentinel RED: $mode/$step"
    }
}

$c0 = Select-One $controllers 'candidate-mode2' 'seed-step1'
$c100 = Select-One $controllers 'candidate-mode2' '100'
$c1 = Select-One $controllers 'candidate-mode2' '1'
$h1 = Select-One $hydraulics 'candidate-mode2' '1'
$h100 = Select-One $hydraulics 'candidate-mode2' '100'
$legacy = Select-One $controllers 'mode1' '100'
$preconditioning = @($nodes | Where-Object { $_.mode -eq 'candidate-mode2' -and $_.checkpoint -eq 'seed-step1' })
$largest = $preconditioning | Sort-Object { [Math]::Abs((Number $_ 'energy_storage_residual_w')) } -Descending | Select-Object -First 1
$historical = Get-Content -LiteralPath (Join-Path $Root 'eng\m10-final-vr2-r3-post-repair-dynamic-equilibrium-replanning1-contract.json') -Raw | ConvertFrom-Json
$governorDelta = (Number $c100 'output_percent') - (Number $c1 'output_percent')
$summary = [ordered]@{
    status = 'PASS-DIAGNOSTIC-EVIDENCE-COMPLETE'
    diagnostic_id = 'R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1'
    production_change = $false
    prior_test_change = $false
    seed_step2_matches_factory = $true
    health_rows = $health.Count
    candidate_envelope_violation_steps = @($health | Where-Object { $_.mode -eq 'candidate-mode2' -and $_.step -match '^\d+$' -and $_.in_frozen_envelope -eq 'false' }).Count
    largest_seed_step1_storage_node = $largest.node
    largest_seed_step1_storage_rate_w = Number $largest 'energy_storage_residual_w'
    candidate_primary_flow_step1_kg_s = Number $h1 'pump_kg_s'
    candidate_primary_flow_step100_kg_s = Number $h100 'pump_kg_s'
    candidate_governor_delta_percent = $governorDelta
    governor_delta_post_minus_pre_repair_percent = $governorDelta - [double]$historical.returned_metrics.historical_fast_gate.governor_delta_percent
    candidate_initial_net_torque_nm = Number $c0 'net_torque_nm'
    candidate_final_net_torque_nm = Number $c100 'net_torque_nm'
    candidate_final_rotor_rpm = Number $c100 'rotor_final_rpm'
    canonical_final_rotor_rpm = Number $legacy 'rotor_final_rpm'
    candidate_integral_delta = (Number $c100 'integral') - (Number $c0 'integral')
    candidate_governor_delta_from_seed_percent = (Number $c100 'output_percent') - (Number $c0 'output_percent')
    max_suction_mass_identity_abs_kg_s = ($balances | ForEach-Object { [Math]::Abs((Number $_ 'suction_mass_identity_residual_kg_s')) } | Measure-Object -Maximum).Maximum
    max_suction_energy_identity_abs_w = ($balances | ForEach-Object { [Math]::Abs((Number $_ 'suction_energy_identity_residual_w')) } | Measure-Object -Maximum).Maximum
    max_network_mass_closure_abs_kg = ($balances | ForEach-Object { [Math]::Abs((Number $_ 'network_mass_closure_kg')) } | Measure-Object -Maximum).Maximum
    max_network_energy_closure_abs_j = ($balances | ForEach-Object { [Math]::Abs((Number $_ 'network_energy_closure_j')) } | Measure-Object -Maximum).Maximum
    selected_reconstruction_family = 'UNSELECTED'
    r3_passed = $false
    requalification3_authorized = $false
    r4_authorized = $false
    next_activity = 'REVIEW-DIAGNOSTIC1-EQUILIBRIUM-RECONSTRUCTION-PLANNING-ONLY'
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Out '06-diagnostic-summary.json') -Encoding UTF8
$manifest = foreach ($name in @('00-static-audit.txt','00-build.txt','00-test.txt','01-node-residuals.csv','02-hydraulics-drum.csv','03-controller-rotor.csv','04-conservation.csv','05-health.csv','06-diagnostic-summary.json')) {
    $path = Join-Path $Out $name
    Require ([IO.File]::Exists($path)) "missing execution provenance: $name"
    [PSCustomObject]@{ file = $name; sha256 = Hash-File $path }
}
$manifest | Export-Csv -LiteralPath (Join-Path $Out '07-manifest.csv') -NoTypeInformation -Encoding ASCII
Write-Host 'R3 dynamic-equilibrium residual diagnostic: PASS-DIAGNOSTIC-EVIDENCE-COMPLETE. R3 remains RED.'
