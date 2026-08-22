param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10 final V&V matrix validation failed: $Message" }
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

$matrixPath = Join-Path $RepositoryRoot 'eng\m10-final-vv-matrix.json'
$acceptancePath = Join-Path $RepositoryRoot 'eng\m10985-manual-acceptance-record.json'
Require (Test-Path -LiteralPath $matrixPath -PathType Leaf) 'eng/m10-final-vv-matrix.json is missing.'
Require (Test-Path -LiteralPath $acceptancePath -PathType Leaf) 'M10.9.8.5 manual acceptance record is missing.'

$m = Get-Content -LiteralPath $matrixPath -Raw | ConvertFrom-Json
Require ($m.schema -eq 'm10-final-vv-matrix-v1') 'matrix schema mismatch.'
Require ($m.status -eq 'FROZEN-PRE-LONG') 'matrix must be FROZEN-PRE-LONG before the cumulative gate.'
$rows = @($m.rows)
Require ($rows.Count -eq 27) 'exactly 27 V&V rows are required.'
Require (@($rows.id | Select-Object -Unique).Count -eq 27) 'V&V row IDs must be unique.'
Require (@($m.authoritative_exact_v4_reference.frozen_i3_budgets).Count -eq 19) 'exactly 19 frozen I.3 budgets are required.'
Require ($m.authoritative_exact_v4_reference.fixed_step_ms -eq 10) 'authoritative fixed step must remain 10 ms.'
Require ($m.authoritative_exact_v4_reference.instantaneous_conservation_ceilings.mass_closure_residual_kg -eq 0.000001) 'mass closure ceiling changed.'
Require ($m.authoritative_exact_v4_reference.instantaneous_conservation_ceilings.energy_closure_residual_J -eq 0.01) 'energy closure ceiling changed.'
Require ($m.authoritative_exact_v4_reference.instantaneous_conservation_ceilings.balance_mass_rate_residual_kg_s -eq 0.00000001) 'balance mass-rate ceiling changed.'
Require ($m.authoritative_exact_v4_reference.instantaneous_conservation_ceilings.balance_power_residual_W -eq 0.001) 'balance power ceiling changed.'

$hmi = @($rows | Where-Object { $_.id -eq 'HMI-OPS-01' })
Require ($hmi.Count -eq 1) 'HMI-OPS-01 missing or duplicated.'
Require ($hmi[0].closure_status -eq 'ACCEPTED-M10985') 'HMI-OPS-01 must record accepted M10.9.8.5 manual evidence.'
$long = @($rows | Where-Object { $_.id -eq 'LONG-SOAK-01' })
Require ($long.Count -eq 1) 'LONG-SOAK-01 missing or duplicated.'
Require ($long[0].closure_status -eq 'PENDING-LONG-GATE') 'LONG-SOAK-01 must remain pending until the long gate runs.'

$a = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
Require ($a.status -eq 'VALIDATED') 'M10.9.8.5 manual acceptance record is not VALIDATED.'
Require ($a.acceptanceText -eq 'M10.9.8.5 manual integrated HMI acceptance OK') 'manual acceptance text mismatch.'
Require ($a.m1098Status -eq 'VALIDATED/CLOSED') 'M10.9.8 must be recorded as VALIDATED/CLOSED.'
Require ($a.m10Status -eq 'OPEN-PENDING-FINAL-CUMULATIVE-AND-LONG') 'M10 must remain open before the final gates.'

foreach ($row in $rows) {
    Require (-not [string]::IsNullOrWhiteSpace($row.phenomenon)) "$($row.id) has no phenomenon description."
    Require (@($row.acceptance_criteria).Count -gt 0) "$($row.id) has no acceptance criteria."
    foreach ($e in @($row.evidence_references)) {
        if ($e.type -eq 'external-review-artifact') { continue }
        $p = [string]$e.path
        if ([string]::IsNullOrWhiteSpace($p)) { continue }
        $full = Join-Path $RepositoryRoot ($p.Replace('/','\'))
        Require (Test-Path -LiteralPath $full) "$($row.id) evidence path is missing: $p"
    }
    foreach ($route in @($row.final_cumulative_required_routes)) {
        if ($route -eq 'dotnet test') { continue }
        $p = [string]$route
        $full = Join-Path $RepositoryRoot ($p.Replace('/','\'))
        Require (Test-Path -LiteralPath $full) "$($row.id) cumulative route is missing: $p"
    }
}


# The final cumulative gate reuses selected historical focused scripts only for their
# still-current functional owner tests. Their exact-candidate ApplicationDescriptor tests
# were intentionally removed/superseded from the current test surface, so historical reuse
# must skip those descriptor-only filters without weakening standalone historical gates.
$finalScriptPath = Join-Path $RepositoryRoot 'scripts\run-m10-final-validation.cmd'
Require (Test-Path -LiteralPath $finalScriptPath -PathType Leaf) 'final cumulative script is missing.'
$finalScript = Get-Content -LiteralPath $finalScriptPath -Raw
$historicalReuseRoutes = @(
    @{ Script='scripts\run-m10972-domain-definition-invariant-closure-audit.cmd'; Call='call scripts\run-m10972-domain-definition-invariant-closure-audit.cmd --historical-reuse'; Descriptor='Current_DescribesM10972Hotfix1Rev1DomainDefinitionInvariantClosureCandidate' },
    @{ Script='scripts\run-m10973-desktop-host-session-integrity-audit.cmd'; Call='call scripts\run-m10973-desktop-host-session-integrity-audit.cmd --historical-reuse'; Descriptor='Current_DescribesM10973Hotfix2Rev2DesktopHostSessionIntegrityCandidate' },
    @{ Script='scripts\run-m10974-mission-performance-timeline-audit.cmd'; Call='call scripts\run-m10974-mission-performance-timeline-audit.cmd --historical-reuse'; Descriptor='Current_DescribesM10974DeterministicMissionTimelineCandidate' }
)
foreach ($route in $historicalReuseRoutes) {
    Require ($finalScript.Contains($route.Call)) ('final cumulative gate does not use historical-reuse mode for ' + $route.Script)
    $routePath = Join-Path $RepositoryRoot $route.Script
    Require (Test-Path -LiteralPath $routePath -PathType Leaf) ('historical focused route is missing: ' + $route.Script)
    $routeText = Get-Content -LiteralPath $routePath -Raw
    Require ($routeText.Contains('if /I "%~1"=="--historical-reuse" set "HISTORICAL_REUSE=1"')) ('historical-reuse switch missing from ' + $route.Script)
    Require ($routeText.Contains('if "%HISTORICAL_REUSE%"=="1"')) ('historical-reuse branch missing from ' + $route.Script)
    Require ($routeText.Contains($route.Descriptor)) ('standalone exact-candidate descriptor guard unexpectedly removed from ' + $route.Script)
}

# Current ApplicationDescriptor test surface intentionally keeps only the current M10.9.7.5
# descriptor contract; this assertion prevents accidental reintroduction of stale exact-candidate
# filters into the cumulative route without an explicit historical-reuse boundary.
$descriptorTestsPath = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Application.Tests\ApplicationDescriptorTests.cs'
Require (Test-Path -LiteralPath $descriptorTestsPath -PathType Leaf) 'ApplicationDescriptorTests.cs is missing.'
$descriptorTests = Get-Content -LiteralPath $descriptorTestsPath -Raw
Require ($descriptorTests.Contains('Current_DescribesM10975MissionPerformanceClosureCandidate')) 'current M10.9.7.5 descriptor contract is missing.'


# Fail fast on stale focused-test filters before running the expensive cumulative sequence.
# This is a source-level existence check, not a replacement for executing the tests.
$testSourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'tests') -Recurse -File -Filter '*.cs')
$testSourceCorpus = [string]::Join("`n", @($testSourceFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }))
$allowedSkippedMethods = @(
    'Current_DescribesM10972Hotfix1Rev1DomainDefinitionInvariantClosureCandidate',
    'Current_DescribesM10973Hotfix2Rev2DesktopHostSessionIntegrityCandidate',
    'Current_DescribesM10974DeterministicMissionTimelineCandidate'
)
$calledScriptMatches = [regex]::Matches($finalScript, '(?im)^call\s+([^\r\n]+?\.cmd)(?:\s+--historical-reuse)?\s*$')
foreach ($calledMatch in $calledScriptMatches) {
    $calledRelative = $calledMatch.Groups[1].Value.Trim()
    if (-not ($calledRelative -like 'scripts\*.cmd')) { continue }
    $calledPath = Join-Path $RepositoryRoot $calledRelative
    Require (Test-Path -LiteralPath $calledPath -PathType Leaf) ('called focused script is missing: ' + $calledRelative)
    $calledText = Get-Content -LiteralPath $calledPath -Raw
    foreach ($match in [regex]::Matches($calledText, '--filter-class\s+"([^"]+)"')) {
        $fq = $match.Groups[1].Value
        $className = ($fq -split '\.')[-1]
        Require ([regex]::IsMatch($testSourceCorpus, ('\bclass\s+' + [regex]::Escape($className) + '\b'))) ('focused --filter-class resolves no current source class: ' + $fq + ' in ' + $calledRelative)
    }
    foreach ($match in [regex]::Matches($calledText, '--filter-method\s+"([^"]+)"')) {
        $fq = $match.Groups[1].Value
        $methodName = ($fq -split '\.')[-1]
        if ($allowedSkippedMethods -contains $methodName) {
            Require ($calledMatch.Value.Contains('--historical-reuse')) ('stale exact-candidate method is only legal behind historical reuse: ' + $fq)
            continue
        }
        Require ([regex]::IsMatch($testSourceCorpus, ('\b' + [regex]::Escape($methodName) + '\s*\('))) ('focused --filter-method resolves no current source method: ' + $fq + ' in ' + $calledRelative)
    }
}

# Preserve the accepted M10.9.8 contracts byte-for-byte.
$expected = @{
    'eng\m1098-integrated-human-automation-hmi-matrix.json'='272e4eb2c958254c18cf19c1818006325ea0363c4f76eae7d8432fdb42d6da4e';
    'eng\m1098-integrated-human-automation-hmi-matrix-v2.json'='218d341111e4fa273643dce7dc9a18a6b3285bc498869cd12784f0a3d51c3223';
    'eng\m10983-degraded-fault-protection-takeover-matrix.json'='3e5e4a2622cf8f445b1ad44901f0825d7d43ef50abfcecd764735b19aaa1ebf0';
    'eng\m10984-replay-checkpoint-same-seed-integrity-matrix.json'='def8d36e26973b2bdac8046f5f3fe3991dfcf6106ea19937866ab3c4ba86e7d3'
}
foreach ($rel in $expected.Keys) {
    $p = Join-Path $RepositoryRoot $rel
    Require (Test-Path -LiteralPath $p -PathType Leaf) "frozen contract missing: $rel"
    Require ((Get-Sha256Hex $p) -eq $expected[$rel]) "frozen contract changed: $rel"
}

Write-Host ('M10 final V&V matrix validation passed. rows=27; matrix-sha256=' + (Get-Sha256Hex $matrixPath))
