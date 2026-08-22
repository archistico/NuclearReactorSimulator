param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10.9.8.2 Hotfix 1 matrix-v2 validation failed: $Message" }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }

$v1Path = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix.json'
$v2Path = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix-v2.json'
Require (Test-Path -LiteralPath $v1Path -PathType Leaf) 'Accepted matrix v1 is missing.'
Require (Test-Path -LiteralPath $v2Path -PathType Leaf) 'Execution matrix v2 is missing.'

function Get-Sha256Hex([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash($stream)
        }
        finally {
            if ($null -ne $sha256) { $sha256.Dispose() }
        }
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
    }

    return ([System.BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

$v1Hash = Get-Sha256Hex $v1Path
Require ($v1Hash -eq '272e4eb2c958254c18cf19c1818006325ea0363c4f76eae7d8432fdb42d6da4e') 'Accepted matrix v1 was modified; exact frozen bytes must remain unchanged.'

$v1 = Get-Content -LiteralPath $v1Path -Raw | ConvertFrom-Json
$v2 = Get-Content -LiteralPath $v2Path -Raw | ConvertFrom-Json
Require ($v1.matrixId -eq 'm1098-integrated-human-automation-hmi-v1') 'Matrix v1 id mismatch.'
Require ($v2.schemaVersion -eq 1) 'Matrix v2 schemaVersion must remain 1.'
Require ($v2.milestone -eq 'M10.9.8.2 Hotfix 1') 'Matrix v2 milestone mismatch.'
Require ($v2.matrixId -eq 'm1098-integrated-human-automation-hmi-v2') 'Matrix v2 id mismatch.'
Require ($v2.supersedesMatrixId -eq $v1.matrixId) 'Matrix v2 must explicitly supersede v1 for execution.'
Require ($v2.matrixFrozen -eq $true) 'Matrix v2 must be frozen.'
Require ($v2.repairsBeforeAcceptanceAllowed -eq $false) 'Matrix v2 must remain fail-closed to silent repair.'
Require ($v2.productionRuntimeChanged -eq $true) 'Matrix v2 must disclose the narrow production App/Application hotfix.'
Require ($v2.healthyExecutionPhase -eq 'active-bounded-demand-control-axis') 'Matrix v2 must disclose the active bounded-demand HAA execution phase.'
Require ($v2.healthyTargetWindowStartOffsetSteps -eq 4000) 'Matrix v2 must preserve the +4000 target-window start offset from activation.'
Require ($v2.healthyTargetWindowEndOffsetSteps -eq 8000) 'Matrix v2 must preserve the +8000 target-window end offset from activation.'
Require ($v2.healthyTargetWindowSemantics -eq 'offsets-from-activation; observational completion target only; does not delay activation or external-demand publication') 'Matrix v2 target-window semantics mismatch.'
Require (@($v2.rows).Count -eq 19) 'Matrix v2 must retain 19 rows.'
Require (@($v2.crossCuttingInvariants).Count -eq 11) 'Matrix v2 must retain 11 cross-cutting invariants.'

$expectedAssistance = @('Hidden','ChecklistOnly','Guided')
$expectedAuthority = @('Manual','Assisted','SupervisoryAutomatic')
$healthy = @($v2.rows | Where-Object { $_.family -eq 'healthy-bounded-load' })
Require ($healthy.Count -eq 9) 'Matrix v2 must retain the nine-row healthy cross-product.'
for ($i = 0; $i -lt 9; $i++) {
    $row = $healthy[$i]
    $id = 'HAA-{0:D2}' -f ($i + 1)
    Require ($row.rowId -eq $id) "$id row order mismatch."
    Require ($row.scenarioId -eq 'integrated-normal-operations-training-i5-repaired-v4-production') "$id production scenario mismatch."
    Require ($row.scenarioExactId -eq 'bounded-demand-following-5-10-5@2') "$id production pack mismatch."
    Require ($row.profileExactId -eq 'integrated-operations-desktop-stable@4') "$id production profile mismatch."
    Require ($row.expectedChallengeDemandProfile -eq 'bounded-demand-5-10-5@1') "$id demand owner must remain exact-v1."
    Require (@($row.preconditions) -contains 'production bounded-demand @2 exact pack bound; healthy activation condition satisfied on the exact-v4 baseline') "$id must disclose canonical healthy activation."
    Require (@($row.preconditions) -contains 'target completion window remains +4000..+8000 logical steps from activation and is observational only') "$id target-window semantics mismatch."
    Require ($row.replayCheckpointRequirement -eq 'full replay + checkpoint prefix + live continuation equivalence') "$id replay/checkpoint requirement mismatch."
    Require (@($row.expectedOperatorEvidence) -contains 'MISSION exact pack/lifecycle/objective/score context visible; external demand is active after canonical challenge activation') "$id must expose active external-demand evidence after canonical activation."
    Require (@($row.expectedOperatorEvidence) -contains 'GRID DEMAND / REQUESTED LOAD / ACTUAL OUTPUT remain distinct under the M10.9.6.2 owner contract') "$id demand/request/actual owner contract mismatch."
    Require ($row.requestedAuthority -eq $row.expectedEffectiveAuthority) "$id healthy effective authority mismatch."
}
foreach ($a in $expectedAssistance) {
    foreach ($u in $expectedAuthority) {
        Require (@($healthy | Where-Object { $_.requestedAssistance -eq $a -and $_.requestedAuthority -eq $u }).Count -eq 1) "Missing/duplicate healthy row for $a / $u."
    }
}

foreach ($id in @('INT-17','INT-18','INT-19')) {
    $row = @($v2.rows | Where-Object { $_.rowId -eq $id })
    Require ($row.Count -eq 1) "$id missing."
    Require ($row[0].scenarioExactId -eq 'bounded-demand-following-5-10-5@2') "$id must follow production bounded-demand @2."
    Require ($row[0].profileExactId -eq 'integrated-operations-desktop-stable@4') "$id must use production exact-v4."
}

$allowedChanged = @('HAA-01','HAA-02','HAA-03','HAA-04','HAA-05','HAA-06','HAA-07','HAA-08','HAA-09','INT-17','INT-18','INT-19')
foreach ($oldRow in @($v1.rows)) {
    if ($allowedChanged -contains $oldRow.rowId) { continue }
    $newRow = @($v2.rows | Where-Object { $_.rowId -eq $oldRow.rowId })
    Require ($newRow.Count -eq 1) "$($oldRow.rowId) missing from matrix v2."
    Require (($oldRow | ConvertTo-Json -Depth 20 -Compress) -eq ($newRow[0] | ConvertTo-Json -Depth 20 -Compress)) "$($oldRow.rowId) changed outside the bounded-demand revision scope."
}

$auditScriptPath = Join-Path $RepositoryRoot 'scripts\run-m10982-healthy-assistance-authority-matrix-audit.cmd'
Require (Test-Path -LiteralPath $auditScriptPath -PathType Leaf) 'Focused audit script is missing.'
$auditScript = Get-Content -LiteralPath $auditScriptPath -Raw
$historicalReuseInvocation = 'validate-m10981-integrated-validation-matrix.ps1" -RepositoryRoot "%CD%" -HistoricalReuse'
Require ($auditScript.Contains($historicalReuseInvocation)) 'Focused audit must invoke the accepted M10.9.8.1 validator in historical-reuse mode.'

Write-Host 'M10.9.8.2 Hotfix 1 matrix-v2 contract validation passed.'
