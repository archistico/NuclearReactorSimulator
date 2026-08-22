param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10.9.8.3 degraded/fault/protection/takeover matrix validation failed: $Message" }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }
function Get-Sha256Hex([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try { $bytes = $sha256.ComputeHash($stream) }
        finally { if ($null -ne $sha256) { $sha256.Dispose() } }
    }
    finally { if ($null -ne $stream) { $stream.Dispose() } }
    return ([System.BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

$v1Path = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix.json'
$v2Path = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix-v2.json'
$m83Path = Join-Path $RepositoryRoot 'eng\m10983-degraded-fault-protection-takeover-matrix.json'
Require (Test-Path -LiteralPath $v1Path -PathType Leaf) 'Accepted matrix v1 is missing.'
Require (Test-Path -LiteralPath $v2Path -PathType Leaf) 'M10.9.8.2 execution matrix v2 is missing.'
Require (Test-Path -LiteralPath $m83Path -PathType Leaf) 'M10.9.8.3 execution matrix is missing.'
Require ((Get-Sha256Hex $v1Path) -eq '272e4eb2c958254c18cf19c1818006325ea0363c4f76eae7d8432fdb42d6da4e') 'Accepted matrix v1 changed.'
Require ((Get-Sha256Hex $v2Path) -eq '218d341111e4fa273643dce7dc9a18a6b3285bc498869cd12784f0a3d51c3223') 'Validated M10.9.8.2 matrix v2 changed.'

$m = Get-Content -LiteralPath $m83Path -Raw | ConvertFrom-Json
Require ($m.schemaVersion -eq 1) 'schemaVersion must remain 1.'
Require ($m.milestone -eq 'M10.9.8.3') 'milestone mismatch.'
Require ($m.matrixId -eq 'm10983-degraded-fault-protection-takeover-v1') 'matrixId mismatch.'
Require ($m.baseline -eq 'M10.9.8.2 Hotfix 1 REV5 VALIDATED') 'baseline mismatch.'
Require ($m.productionRuntimeChanged -eq $false) 'M10.9.8.3 must not change production runtime.'
Require ($m.simulationPhysicsChanged -eq $false) 'M10.9.8.3 must not change Simulation physics.'
Require ($m.newProductionScenarioRegistration -eq $false) 'Validation-only compositions must not register production scenarios.'
Require ($m.newFaultTypeRegistration -eq $false) 'M10.9.8.3 must not add fault types.'
Require ($m.replayCheckpointOwnedByNextMilestone -eq 'M10.9.8.4') 'Replay/checkpoint ownership boundary mismatch.'
Require (@($m.requiredCases).Count -eq 11) 'Exactly eleven required cases must be frozen.'
Require (@($m.requiredCases | Select-Object -Unique).Count -eq 11) 'Required case IDs must be unique.'
Require (@($m.rows).Count -eq 11) 'Exactly eleven execution rows are required.'
Require (@($m.rows.rowId | Select-Object -Unique).Count -eq 11) 'Execution row IDs must be unique.'
for ($i = 1; $i -le 11; $i++) {
    $id = 'DFP-{0:D2}' -f $i
    Require (@($m.rows | Where-Object { $_.rowId -eq $id }).Count -eq 1) "$id missing or duplicated."
}
foreach ($case in @($m.requiredCases)) {
    Require (@($m.rows | Where-Object { $_.case -eq $case }).Count -eq 1) "Required case '$case' must map to exactly one row."
}
$dfp1 = @($m.rows | Where-Object { $_.rowId -eq 'DFP-01' })[0]
Require ($dfp1.scenarioKind -eq 'validation-only exact-v4 composition') 'DFP-01 must remain validation-only exact-v4.'
$dfp7 = @($m.rows | Where-Object { $_.rowId -eq 'DFP-07' })[0]
Require ($dfp7.scenarioKind -eq 'canonical M4.5 generator close-check owner') 'DFP-07 must use the real canonical permissive owner.'
$dfp11 = @($m.rows | Where-Object { $_.rowId -eq 'DFP-11' })[0]
Require (@($dfp11.sourceMatrixRows) -contains 'INT-17') 'DFP-11 must include active challenge ownership.'

$testPath = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Application.Tests\ControlRoom\Automation\M10983DegradedFaultProtectionTakeoverMatrixTests.cs'
Require (Test-Path -LiteralPath $testPath -PathType Leaf) 'M10.9.8.3 integration test is missing.'
$test = Get-Content -LiteralPath $testPath -Raw
Require ($test.Contains('m10983-bounded-demand-supervisory-degraded-validation')) 'Validation-only degraded challenge composition missing.'
Require ($test.Contains('ScenarioFaultTriggerDefinition.AtLogicalStep(2)')) 'Required measurement degradation activation step missing.'
Require ($test.Contains('ScenarioFaultTriggerDefinition.AtLogicalStep(5)')) 'Required measurement recovery/deactivation step missing.'
Require ($test.Contains('PlantControlAuthorityHealth.SuspendedByProtection')) 'Protection suspension assertion missing.'
Require ($test.Contains('ChallengeLifecycleState.Failed')) 'Challenge/protection failure-boundary assertion missing.'

Write-Host 'M10.9.8.3 degraded/fault/protection/takeover matrix contract validation passed.'
