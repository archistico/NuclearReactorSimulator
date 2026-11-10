param([ValidateSet('Static','Preexecution','Adjudicate')][string]$Stage = 'Static')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = Split-Path -Parent $PSScriptRoot
$Out = Join-Path $Root 'artifacts\r3-h-feasibility1'
$ContractPath = Join-Path $PSScriptRoot 'r3-h-feasibility1-contract.json'
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Hash-File([string]$Path) {
    $stream = [IO.File]::OpenRead($Path); $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
}
function Tree([string]$Directory, [string[]]$Excluded) {
    $paths = [string[]]@([IO.Directory]::EnumerateFiles((Join-Path $Root $Directory), '*', [IO.SearchOption]::AllDirectories) |
        ForEach-Object { $_.Substring($Root.Length + 1).Replace('\','/') } |
        Where-Object { $_ -notmatch '(^|/)(bin|obj)/' -and $_ -notin $Excluded })
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $builder = New-Object Text.StringBuilder
    foreach ($path in $paths) { [void]$builder.Append($path + '|' + (Hash-File (Join-Path $Root $path)) + "`n") }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($builder.ToString()))).Replace('-','') }
    finally { $sha.Dispose() }
    return [ordered]@{ file_count = $paths.Count; tree_sha256 = $hash }
}
function Read-Json([string]$Name) { return [IO.File]::ReadAllText((Join-Path $Out $Name)) | ConvertFrom-Json }
function Write-Json([string]$Name, $Value) {
    [IO.Directory]::CreateDirectory($Out) | Out-Null
    [IO.File]::WriteAllText((Join-Path $Out $Name), ($Value | ConvertTo-Json -Depth 20) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
}
function Verify-Report([string]$Name, [string[]]$Methods) {
    [xml]$xml = [IO.File]::ReadAllText((Join-Path $Out $Name))
    $assemblies = @($xml.SelectNodes('/assemblies/assembly'))
    Require ($assemblies.Count -eq 1) 'Expected exactly one test assembly report'
    Require ([int]$assemblies[0].GetAttribute('failed') -eq 0 -and [int]$assemblies[0].GetAttribute('errors') -eq 0 -and [int]$assemblies[0].GetAttribute('skipped') -eq 0) 'Focused test report RED'
    $passed = @($xml.SelectNodes('//test[@result="Pass"]'))
    Require ($passed.Count -eq $Methods.Count) 'Focused passed-test count drift'
    Require ((@($passed | ForEach-Object { $_.GetAttribute('method') } | Sort-Object) -join '|') -eq (@($Methods | Sort-Object) -join '|')) 'Focused method identity drift'
    Require ([int]$assemblies[0].GetAttribute('passed') -eq $passed.Count) 'Focused report accounting drift'
}
$contract = [IO.File]::ReadAllText($ContractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'r3-h-conserved-inventory-feasibility1-v1') 'Implementation schema drift'
Require ($contract.implementation_files.Count -eq 3) 'Implementation surface drift'
$excluded = @($contract.implementation_files | ForEach-Object { $_.path })
foreach ($entry in @($contract.frozen_inputs) + @($contract.implementation_files)) {
    Require ((Hash-File (Join-Path $Root $entry.path)) -eq $entry.sha256) ('Frozen file drift: ' + $entry.path)
}
$baseline = [ordered]@{}
foreach ($directory in @('src','tests')) {
    $baseline[$directory] = Tree $directory $excluded
    Require ($baseline[$directory].file_count -eq $contract.baseline.$directory.file_count -and $baseline[$directory].tree_sha256 -eq $contract.baseline.$directory.tree_sha256) "$directory baseline drift"
}
$plan = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'r3-equilibrium-reconstruction-planning1-contract.json')) | ConvertFrom-Json
foreach ($key in @('search','residual_scales','rotor_torque_scale_nm','frozen_envelope','fixed_nonfluid_initial_conditions','outcomes')) {
    Require (($contract.$key | ConvertTo-Json -Depth 14 -Compress) -ceq ($plan.$key | ConvertTo-Json -Depth 14 -Compress)) "Frozen planning field drift: $key"
}
foreach ($flag in @('production_change','production_seed_installation','controller_state_change','physics_retuning','threshold_change','r3_passed','requalification3','r4')) {
    Require ($contract.authority.$flag -is [bool] -and -not $contract.authority.$flag) "Authority drift: $flag"
}
Require ($contract.authority.search_only_after_preexecution_review -is [bool] -and $contract.authority.search_only_after_preexecution_review) 'Missing preexecution hold'
[IO.Directory]::CreateDirectory($Out) | Out-Null
Write-Json '01-provenance.json' ([ordered]@{ status = 'PASS-STATIC'; baseline = $baseline; added_test_files = $excluded; contract_sha256 = Hash-File $ContractPath; production_change = $false; old_tests_changed = $false })
Write-Host 'H feasibility static audit: PASS (production and all 403 preceding tests unchanged).'
if ($Stage -eq 'Static') { exit 0 }

Verify-Report 'preexecution.xml' @(
    'Preexecution_BaselineReproducesFactoryAndFrozenResiduals',
    'Search_ConvergesOnBoundedCoupledLinearResidualAndReproducesTrace',
    'Search_CountsInvalidProbesAndStopsWithoutInventingDerivative',
    'Search_RecipeBudgetIncludesFailedAttempts',
    'Search_RejectsOutOfBoundsTrialsWithoutClippingOrInvokingPhysics',
    'Search_NormalMatrixPivotGuardRejectsUnresolvedConditioning')
$identity = Read-Json '02-baseline.json'
Require ($identity.status -eq 'PASS-BASELINE-IDENTITY' -and $identity.Observations.Count -eq 3 -and $identity.Raw.Count -eq 12) 'Baseline reconstruction not PASS'
Require (@($identity.Observations | Where-Object { -not $_.Healthy -or $_.ScaledResiduals.Count -ne 25 }).Count -eq 0) 'Baseline residual/health capture drift'
$assembly = Join-Path $Root 'tests\NuclearReactorSimulator.Application.Tests\bin\Release\net10.0\NuclearReactorSimulator.Application.Tests.dll'
if ($Stage -eq 'Preexecution') {
    Write-Json '03-preexecution-review.json' ([ordered]@{ status = 'PASS-PREEXECUTION-REVIEW'; contract_sha256 = Hash-File $ContractPath; test_assembly_sha256 = Hash-File $assembly; preexecution_report_sha256 = Hash-File (Join-Path $Out 'preexecution.xml'); baseline_state_hash = $identity.hash; focused_passed = 6; search_execution_authorized = $true; production_change = $false; r3_passed = $false })
    Write-Host 'H feasibility preexecution review: PASS. Only the frozen explicit search may now execute.'
    exit 0
}
$review = Read-Json '03-preexecution-review.json'
Require ($review.status -eq 'PASS-PREEXECUTION-REVIEW' -and $review.contract_sha256 -eq (Hash-File $ContractPath)) 'Stale preexecution authority'
Require ($review.test_assembly_sha256 -eq (Hash-File $assembly)) 'Test binary changed after review'
Verify-Report 'execution.xml' @('ExecuteFrozenHFeasibilitySearch_AfterSuccessfulPreexecutionReview')
$summary = Read-Json '07-execution-summary.json'
$result = (Read-Json '05-search-result.json').result
$confirmations = @(Read-Json '06-confirmation.json' | ForEach-Object { $_ })
$trace = @([IO.File]::ReadAllLines((Join-Path $Out '04-search-trace.jsonl')) | ForEach-Object { $_ | ConvertFrom-Json })
Require ($trace.Count -ge 1 -and $trace.Count -le 4096 -and $trace.Count -eq $result.Evaluations -and $trace.Count -eq $summary.Evaluations) 'Recipe accounting drift'
$lastAccepted = $null
$sequence = 0
foreach ($event in $trace) {
    $sequence++
    Require ($event.Id -eq $sequence -and $event.Coordinates.Count -eq 24) 'Trace identity drift'
    if ($null -ne $event.Residuals) {
        Require ($event.Residuals.Count -eq 75 -and $event.Phases.Count -eq 48) 'Trace residual/phase dimension drift'
        Require (@($event.Coordinates | Where-Object { [Math]::Abs($_) -gt 0.02 }).Count -eq 0) 'Valid trial escaped frozen bounds'
        $max = ($event.Residuals | ForEach-Object { [Math]::Abs($_) } | Measure-Object -Maximum).Maximum
        Require ([Math]::Abs($max - $event.Score) -le [Math]::Max(1e-12, [Math]::Abs($max) * 1e-12)) 'Trace objective accounting drift'
    }
    if ($event.Accepted) {
        Require ($null -ne $event.Residuals) 'Invalid trial accepted'
        if ($null -ne $lastAccepted) { Require ($event.Score -lt $lastAccepted.Score) 'Nondecreasing accepted objective' }
        $lastAccepted = $event
    }
}
Require ($null -ne $lastAccepted -and $summary.status -eq 'PASS-EVIDENCE-COMPLETE') 'Missing accepted baseline or summary'
Require ($result.Root -eq $summary.root -and $result.Stop -eq $summary.Stop) 'Result/summary drift'
if ($result.Root) {
    Require ($lastAccepted.Score -le 1 -and $confirmations.Count -eq 3) 'Root must have three independent confirmations'
    Require (@($confirmations | Select-Object -ExpandProperty signature -Unique).Count -eq 1) 'Confirmation trajectories diverged'
    $feasible = $true
    foreach ($run in $confirmations) {
        Require ($run.Raw.Count -eq 12 -and $run.observations.Count -eq 9 -and $run.health.Count -eq 100) 'Confirmation workload drift'
        foreach ($observation in $run.observations) {
            if (-not $observation.Healthy -or @($observation.ScaledResiduals | Where-Object { [Math]::Abs($_) -gt 1 }).Count -gt 0) { $feasible = $false }
        }
        foreach ($observation in $run.health) { if (-not $observation.Healthy -or -not $observation.InEnvelope) { $feasible = $false } }
    }
    $expected = if ($feasible) { 'H-FEASIBLE-FLUID-SEED' } else { 'H-CANDIDATE-NOT-QUALIFIED' }
} else {
    Require ($confirmations.Count -eq 0 -and $lastAccepted.Score -gt 1) 'Inconclusive search promoted or confirmation work invented'
    $expected = 'H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS'
}
Require ($summary.classification -eq $expected -and $summary.confirmation_runs -eq $confirmations.Count) 'Engineering classification drift'
Require ($summary.production_change -eq $false -and $summary.r3_passed -eq $false -and $summary.next_activity -eq 'RETURNED-H-EVIDENCE-REVIEW-AND-PLANNING-ONLY') 'Execution authority escaped'
$names = @('00-static.log','00-build.log','01-preexecution-tests.log','01-provenance.json','02-baseline.json','03-preexecution-review.json','preexecution.xml','04-search-trace.jsonl','05-search-result.json','06-confirmation.json','07-execution-summary.json','09-search-test.log','execution.xml')
$manifest = foreach ($name in $names) { [PSCustomObject]@{ file = $name; sha256 = Hash-File (Join-Path $Out $name) } }
$manifest | Export-Csv -LiteralPath (Join-Path $Out '08-manifest.csv') -NoTypeInformation -Encoding ASCII
Write-Host ('H feasibility adjudication: PASS-EVIDENCE-COMPLETE / ' + $expected + '. R3 remains RED.')
