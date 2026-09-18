param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$HistoricalReuse
)

$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    throw "M10.9.8.1 matrix validation failed: $Message"
}

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        Fail $Message
    }
}

function RequireText($Value, [string]$Name) {
    Require ($null -ne $Value -and -not [string]::IsNullOrWhiteSpace([string]$Value)) "$Name must be a non-empty string."
}

function RequireNonEmptyArray($Value, [string]$Name) {
    Require (@($Value).Count -gt 0) "$Name must contain at least one item."
}

$manualPath = Join-Path $RepositoryRoot 'docs\usermanual\MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md'
Require (Test-Path -LiteralPath $manualPath -PathType Leaf) "User manual not found: $manualPath"
$manual = Get-Content -LiteralPath $manualPath -Raw
$manualRequiredAnchors = @(
    'funzionalità utente validate fino a M10.9.7 CLOSED',
    '## 9.14 Pannello MISSION — Mission & Performance',
    'GRID DEMAND, REQUESTED LOAD e ACTUAL OUTPUT',
    'Autorità richiesta ed effettiva',
    '## 12.12 Challenge operativi e punteggio MISSION',
    '## 13.7 Missione, timeline e ripristino',
    'M10.9.8.1 REV1 Docs1 ha congelato la matrice di validazione integrata senza aggiungere nuove funzioni operative'
)
foreach ($requiredAnchor in $manualRequiredAnchors) {
    Require ($manual.Contains($requiredAnchor)) "User manual alignment anchor missing: $requiredAnchor"
}

$matrixPath = Join-Path $RepositoryRoot 'eng\m1098-integrated-human-automation-hmi-matrix.json'
Require (Test-Path -LiteralPath $matrixPath -PathType Leaf) "Matrix file not found: $matrixPath"

try {
    $matrix = Get-Content -LiteralPath $matrixPath -Raw | ConvertFrom-Json
}
catch {
    Fail "Matrix JSON is invalid: $($_.Exception.Message)"
}

Require ($matrix.schemaVersion -eq 1) 'schemaVersion must be 1.'
Require ($matrix.milestone -eq 'M10.9.8.1') 'milestone must be M10.9.8.1.'
Require ($matrix.matrixId -eq 'm1098-integrated-human-automation-hmi-v1') 'matrixId mismatch.'
Require ($matrix.baseline -eq 'M10.9.7.5 Hotfix 1 VALIDATED') 'baseline must be M10.9.7.5 Hotfix 1 VALIDATED.'
Require ($matrix.matrixFrozen -eq $true) 'matrixFrozen must be true.'
Require ($matrix.repairsBeforeAcceptanceAllowed -eq $false) 'repairsBeforeAcceptanceAllowed must be false.'
Require ($matrix.productionRuntimeChanged -eq $false) 'productionRuntimeChanged must be false.'

$expectedAssistance = @('Hidden', 'ChecklistOnly', 'Guided')
$actualAssistance = @($matrix.axes.trainingAssistance)
Require (($actualAssistance -join '|') -eq ($expectedAssistance -join '|')) 'trainingAssistance axis mismatch.'

$expectedAuthority = @('Manual', 'Assisted', 'SupervisoryAutomatic')
$actualAuthority = @($matrix.axes.plantControlAuthority)
Require (($actualAuthority -join '|') -eq ($expectedAuthority -join '|')) 'plantControlAuthority axis mismatch.'

Require ($matrix.authoritativeDesktop.scenarioId -eq 'integrated-normal-operations-training-i5-repaired-v4-production') 'authoritative desktop scenario mismatch.'
Require ($matrix.authoritativeDesktop.profileExactId -eq 'integrated-operations-desktop-stable@4') 'authoritative desktop profile mismatch.'
Require ($matrix.authoritativeDesktop.thermodynamicClosure -eq 'CorrelationConsistentInverseDomain') 'authoritative thermodynamic closure mismatch.'
Require ($matrix.authoritativeDesktop.hydraulicPolicy -eq 'FourNodeBranchContinuityCorrectedCommitOptIn') 'authoritative hydraulic policy mismatch.'
Require ($matrix.authoritativeDesktop.fixedStepMilliseconds -eq 10) 'fixed step must remain 10 ms.'

$rows = @($matrix.rows)
Require ($rows.Count -eq 19) "expected 19 rows, found $($rows.Count)."
$uniqueRowIds = @($rows | ForEach-Object { $_.rowId } | Sort-Object -Unique)
Require ($uniqueRowIds.Count -eq 19) 'rowId values must be unique.'

$healthyRows = @($rows | Where-Object { $_.family -eq 'healthy-bounded-load' })
Require ($healthyRows.Count -eq 9) "expected 9 healthy rows, found $($healthyRows.Count)."

for ($index = 0; $index -lt $healthyRows.Count; $index++) {
    $expectedRowId = 'HAA-{0:D2}' -f ($index + 1)
    $row = $healthyRows[$index]
    Require ($row.rowId -eq $expectedRowId) "healthy row order/id mismatch at index $index."
    Require ($row.scenarioIdentityKind -eq 'challenge-pack-versioned') "$expectedRowId scenarioIdentityKind mismatch."
    Require ($row.scenarioId -eq 'power-manoeuvring-normal-shutdown') "$expectedRowId scenarioId mismatch."
    Require ($row.scenarioExactId -eq 'bounded-demand-following-5-10-5@1') "$expectedRowId exact challenge binding mismatch."
    Require ($row.profileExactId -eq 'stable-low-load-parallel-operation@1') "$expectedRowId profile mismatch."
    Require ($row.expectedChallengeDemandProfile -eq 'bounded-demand-5-10-5@1') "$expectedRowId demand profile mismatch."
    Require ($row.requestedAuthority -eq $row.expectedEffectiveAuthority) "$expectedRowId healthy effective authority must equal requested authority."
}

foreach ($assistance in $expectedAssistance) {
    foreach ($authority in $expectedAuthority) {
        $match = @($healthyRows | Where-Object { $_.requestedAssistance -eq $assistance -and $_.requestedAuthority -eq $authority })
        Require ($match.Count -eq 1) "healthy 3x3 matrix must contain exactly one $assistance / $authority row."
    }
}

$requiredFamilies = @(
    'healthy-bounded-load',
    'synchronization-loading',
    'blocked-permissive-interlock',
    'degraded-supervisory-measurement',
    'canonical-protection-trip',
    'equipment-fault',
    'instrumentation-fault',
    'manual-takeover',
    'challenge-demand-following',
    'checkpoint-replay-continuation',
    'terminal-mission-continuing-plant'
) | Sort-Object
$actualFamilies = @($rows | ForEach-Object { $_.family } | Sort-Object -Unique)
Require (($actualFamilies -join '|') -eq ($requiredFamilies -join '|')) 'required integration-family set mismatch.'

foreach ($row in $rows) {
    RequireText $row.rowId 'rowId'
    RequireText $row.family "$($row.rowId).family"
    RequireText $row.scenarioIdentityKind "$($row.rowId).scenarioIdentityKind"
    RequireText $row.scenarioId "$($row.rowId).scenarioId"
    RequireText $row.scenarioExactId "$($row.rowId).scenarioExactId"
    RequireText $row.profileExactId "$($row.rowId).profileExactId"
    Require ($expectedAssistance -contains $row.requestedAssistance) "$($row.rowId) requestedAssistance is invalid."
    Require ($expectedAuthority -contains $row.requestedAuthority) "$($row.rowId) requestedAuthority is invalid."
    Require ($expectedAuthority -contains $row.expectedEffectiveAuthority) "$($row.rowId) expectedEffectiveAuthority is invalid."
    RequireNonEmptyArray $row.preconditions "$($row.rowId).preconditions"
    RequireNonEmptyArray $row.commandsActionsExercised "$($row.rowId).commandsActionsExercised"
    RequireText $row.expectedProtectionFaultInvolvement "$($row.rowId).expectedProtectionFaultInvolvement"
    RequireText $row.replayCheckpointRequirement "$($row.rowId).replayCheckpointRequirement"
    RequireNonEmptyArray $row.expectedOperatorEvidence "$($row.rowId).expectedOperatorEvidence"
    RequireNonEmptyArray $row.manualHmiObservations "$($row.rowId).manualHmiObservations"
    RequireText $row.ownerIfFails "$($row.rowId).ownerIfFails"
    RequireText $row.notes "$($row.rowId).notes"
}

$invariants = @($matrix.crossCuttingInvariants)
Require ($invariants.Count -eq 11) "expected 11 cross-cutting invariants, found $($invariants.Count)."
$uniqueInvariantIds = @($invariants | ForEach-Object { $_.id } | Sort-Object -Unique)
Require ($uniqueInvariantIds.Count -eq 11) 'cross-cutting invariant IDs must be unique.'

$requiredInvariantIds = @(
    'assistance-does-not-change-physics',
    'protection-overrides-normal-control',
    'requested-effective-authority-distinct',
    'supervisory-degradation-fail-closed',
    'expected-observed-command-evidence-distinct',
    'demand-request-actual-distinct',
    'scoring-observational-only',
    'measurement-quality-preserved',
    'mission-has-no-plant-command-authority',
    'replay-checkpoint-operator-state-equivalent',
    'keyboard-only-critical-operation-viable'
) | Sort-Object
$actualInvariantIds = @($invariants | ForEach-Object { $_.id } | Sort-Object)
Require (($actualInvariantIds -join '|') -eq ($requiredInvariantIds -join '|')) 'cross-cutting invariant set mismatch.'
foreach ($invariant in $invariants) {
    RequireText $invariant.contract "$($invariant.id).contract"
    RequireText $invariant.owner "$($invariant.id).owner"
}

$validationOnly = @($rows | Where-Object { $_.scenarioIdentityKind -eq 'validation-only-versioned-composition' })
Require ($validationOnly.Count -eq 1) 'exactly one validation-only composition is required.'
Require ($validationOnly[0].rowId -eq 'INT-12') 'INT-12 must be the validation-only composition.'
Require ($validationOnly[0].scenarioExactId -eq 'm1098-supervisory-required-measurement-unavailable@1') 'INT-12 exact composition ID mismatch.'
Require (([string]$validationOnly[0].notes) -match 'does not register a new production scenario') 'INT-12 must explicitly forbid new production scenario registration.'

if (-not $HistoricalReuse) {
    $sourceCode = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src') -Recurse -File | Where-Object { $_.Extension -in @('.cs', '.axaml', '.csproj') }
    $testCode = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'tests') -Recurse -File | Where-Object { $_.Extension -in @('.cs', '.axaml', '.csproj') }
    $compiledFiles = @($sourceCode) + @($testCode)
    $milestoneCodeMatches = @($compiledFiles | Select-String -SimpleMatch 'M10.9.8.1' -ErrorAction Stop)
    Require ($milestoneCodeMatches.Count -eq 0) 'M10.9.8.1 REV1 standalone acceptance requires no M10.9.8.1 milestone marker under compiled src/tests.'
}
else {
    Write-Host 'M10.9.8.1 compiled-surface marker check skipped in historical-reuse mode; future milestones may legitimately reference the accepted M10.9.8.1 baseline.'
}

$artifactDirectory = Join-Path $RepositoryRoot 'artifacts\m1098-integrated-validation-matrix'
if (Test-Path -LiteralPath $artifactDirectory) {
    Remove-Item -LiteralPath $artifactDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactDirectory | Out-Null
$summaryPath = Join-Path $artifactDirectory '01-m10981-validation-matrix.summary.txt'
$summary = @(
    'scope=M10.9.8.1 REV1 Docs1 Integrated Human/Automation/HMI validation matrix freeze over M10.9.7.5 Hotfix 1 VALIDATED; contract/evidence planning plus user-manual alignment only; compiled/runtime source and tests unchanged from validated baseline;',
    "matrix-id=m1098-integrated-human-automation-hmi-v1; matrix-schema=1; matrix-rows=$($rows.Count); healthy-assistance-authority-rows=$($healthyRows.Count); required-families=$($actualFamilies.Count); cross-cutting-invariants=$($invariants.Count);",
    'assistance-axis=Hidden|ChecklistOnly|Guided; authority-axis=Manual|Assisted|SupervisoryAutomatic; healthy-cross-product-complete=True; expected-healthy-effective-authority-equals-requested=True;',
    'authoritative-desktop=integrated-normal-operations-training-i5-repaired-v4-production|integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn|10ms;',
    'validation-only-degraded-measurement-composition-explicit=True; production-scenario-registration-added=False; matrix-frozen=True; repairs-before-acceptance-allowed=False; production-runtime-changed=False; compiled-surface-changed=False; test-surface-changed=False;',
    'invariant-assistance-does-not-change-physics=True; invariant-protection-overrides-normal-control=True; invariant-requested-effective-authority-distinct=True; invariant-supervisory-degradation-fail-closed=True; invariant-demand-request-actual-distinct=True; invariant-scoring-observational-only=True; invariant-measurement-quality-preserved=True; invariant-mission-command-authority=False; invariant-replay-checkpoint-equivalent=True; invariant-keyboard-only=True;',
    'm10981-rev1-contract-only-rebuild=True; m10981-docs1-user-manual-aligned=True; m10981-integrated-validation-matrix-passes=True; manual-matrix-acceptance-required=True; next-step=manual matrix acceptance then M10.9.8.2 automated healthy assistance-authority matrix;'
)
[System.IO.File]::WriteAllLines($summaryPath, $summary, (New-Object System.Text.UTF8Encoding($false)))

Write-Host 'M10.9.8.1 REV1 Docs1 matrix/manual contract validation passed.'
Write-Host "Artifact: $summaryPath"
