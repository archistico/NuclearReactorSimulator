param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10.9.8.4 replay/checkpoint/same-seed validation failed: $Message" }
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
$m84Path = Join-Path $RepositoryRoot 'eng\m10984-replay-checkpoint-same-seed-integrity-matrix.json'
Require (Test-Path -LiteralPath $v1Path -PathType Leaf) 'Accepted M10.9.8.1 matrix v1 is missing.'
Require (Test-Path -LiteralPath $v2Path -PathType Leaf) 'Validated M10.9.8.2 matrix v2 is missing.'
Require (Test-Path -LiteralPath $m83Path -PathType Leaf) 'Validated M10.9.8.3 matrix is missing.'
Require (Test-Path -LiteralPath $m84Path -PathType Leaf) 'M10.9.8.4 integrity matrix is missing.'
Require ((Get-Sha256Hex $v1Path) -eq '272e4eb2c958254c18cf19c1818006325ea0363c4f76eae7d8432fdb42d6da4e') 'Accepted matrix v1 changed.'
Require ((Get-Sha256Hex $v2Path) -eq '218d341111e4fa273643dce7dc9a18a6b3285bc498869cd12784f0a3d51c3223') 'Validated M10.9.8.2 matrix v2 changed.'
Require ((Get-Sha256Hex $m83Path) -eq '3e5e4a2622cf8f445b1ad44901f0825d7d43ef50abfcecd764735b19aaa1ebf0') 'Validated M10.9.8.3 matrix changed.'

$m = Get-Content -LiteralPath $m84Path -Raw | ConvertFrom-Json
Require ($m.schemaVersion -eq 1) 'schemaVersion must remain 1.'
Require ($m.milestone -eq 'M10.9.8.4') 'milestone mismatch.'
Require ($m.matrixId -eq 'm10984-replay-checkpoint-same-seed-v1') 'matrixId mismatch.'
Require ($m.baseline -eq 'M10.9.8.3 VALIDATED') 'baseline mismatch.'
Require ($m.productionRuntimeChanged -eq $false) 'M10.9.8.4 must not change production runtime.'
Require ($m.simulationPhysicsChanged -eq $false) 'M10.9.8.4 must not change Simulation physics.'
Require ($m.archiveSchemaChanged -eq $false) 'Archive schema must remain unchanged.'
Require ($m.fingerprintAlgorithmChanged -eq $false) 'Fingerprint algorithm must remain unchanged.'
Require ($m.opaquePhysicalCheckpointStateAdded -eq $false) 'Opaque physical checkpoint state is forbidden.'
Require ($m.opaqueChallengeStateAdded -eq $false) 'Opaque challenge checkpoint state is forbidden.'
Require (@($m.requiredEquivalenceModes).Count -eq 4) 'Exactly four equivalence modes are required.'
Require (@($m.requiredEquivalenceModes | Select-Object -Unique).Count -eq 4) 'Equivalence modes must be unique.'
foreach ($required in @('same-seed-fresh-session-repeat','full-replay','checkpoint-prefix-live-continuation','challenge-replay-projection')) {
    Require (@($m.requiredEquivalenceModes) -contains $required) "Required equivalence mode '$required' is missing."
}
Require (@($m.rows).Count -eq 4) 'Exactly four representative state classes are required.'
Require (@($m.rows.rowId | Select-Object -Unique).Count -eq 4) 'Row IDs must be unique.'
for ($i = 1; $i -le 4; $i++) {
    $id = 'RCI-{0:D2}' -f $i
    Require (@($m.rows | Where-Object { $_.rowId -eq $id }).Count -eq 1) "$id missing or duplicated."
}

$testPath = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Application.Tests\ControlRoom\Automation\M10984ReplayCheckpointSameSeedIntegrityTests.cs'
Require (Test-Path -LiteralPath $testPath -PathType Leaf) 'M10.9.8.4 integration test is missing.'
$test = Get-Content -LiteralPath $testPath -Raw
Require ($test.Contains('AssertSameSeedEquivalent')) 'Same-seed fresh-session comparison is missing.'
Require ($test.Contains('ReplayAndVerify')) 'Full replay verification is missing.'
Require ($test.Contains('SeekAndVerify')) 'Checkpoint-prefix restoration is missing.'
Require ($test.Contains('new ScenarioRecorder(restored.Session, restored.ReplayedRecording)')) 'Replay-backed live continuation is missing.'
Require ($test.Contains('OperationalChallengeRecordingProjector.Project')) 'Challenge replay projection verification is missing.'
Require ($test.Contains('ScenarioSessionArchive.CurrentSchemaVersion')) 'Archive schema-v1 preservation assertion is missing.'
Require ($test.Contains('m10984-power-unavailable')) 'Degraded measurement validation composition is missing.'
Require ($test.Contains('PlantControlAuthorityHealth.SuspendedByProtection')) 'Protection-state replay assertion is missing.'
Require ($test.Contains('AdvanceAuthorityAfterProtectionCommit')) 'Protection authority must be observed on the deterministic tick after the SCRAM commit before the protection checkpoint is captured.'
Require ($test.Contains('Assert.Equal("NONE"')) 'Manual takeover stale-objective clearing assertion is missing.'


$scriptPath = Join-Path $RepositoryRoot 'scripts\run-m10984-replay-checkpoint-same-seed-integrity-audit.cmd'
Require (Test-Path -LiteralPath $scriptPath -PathType Leaf) 'M10.9.8.4 focused audit script is missing.'
$script = Get-Content -LiteralPath $scriptPath -Raw
Require (-not $script.Contains(('Get-' + 'FileHash'))) 'Focused audit must remain compatible with the validated legacy Windows PowerShell host.'
Require (-not $script.Contains('call :')) 'Focused audit must not reintroduce batch-label subroutines.'
foreach ($className in @(
    'M10984ReplayCheckpointSameSeedIntegrityTests',
    'M10983DegradedFaultProtectionTakeoverMatrixTests',
    'M10982HealthyAssistanceAuthorityMatrixTests',
    'ScenarioRecorderReplayTests',
    'ScenarioSessionArchiveReplayTests',
    'ScenarioAutomationReplayTests',
    'M10965ChallengeReplayCheckpointClosureTests',
    'M10961ChallengeLifecycleContractTests',
    'M10974MissionPerformanceTimelineContractTests',
    'M10974FingerprintV1SchemaAnchorTests',
    'OperatorComputerM10954ObservedResponseEvidenceTests',
    'AlarmTrendTimelinePresentationTests',
    'M10974MissionPerformanceArchiveRestoreTests'
)) {
    Require ($script.Contains($className)) "Focused owner rerun '$className' is missing."
}

Write-Host 'M10.9.8.4 replay/checkpoint/same-seed matrix contract validation passed.'
