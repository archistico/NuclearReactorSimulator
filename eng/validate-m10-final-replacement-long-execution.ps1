param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "M10 Final replacement-long execution preflight failed: $Message" }
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
function Read-Manifest([string]$Path) {
    $rows = @(Get-Content -LiteralPath $Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $map = @{}
    foreach ($line in $rows) {
        if ($line -notmatch '^([0-9a-f]{64}) \*(.+)$') { Fail "invalid manifest row: $line" }
        $relative = $matches[2].Replace('/','\')
        Require (-not $map.ContainsKey($relative)) "duplicate manifest path: $relative"
        $map[$relative] = $matches[1]
    }
    return $map
}
function Validate-Frozen-Tree([string]$TreeName, [string]$ManifestPath, [int]$ExpectedCount) {
    $expected = Read-Manifest $ManifestPath
    Require ($expected.Count -eq $ExpectedCount) "$TreeName manifest file count mismatch."
    $treeRoot = Join-Path $RepositoryRoot $TreeName
    $actual = @(Get-ChildItem -LiteralPath $treeRoot -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
    Require ($actual.Count -eq $expected.Count) "$TreeName file count differs from frozen manifest."
    foreach ($file in $actual) {
        $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\','/')
        Require ($expected.ContainsKey($relative)) "unexpected frozen $TreeName file: $relative"
        Require ((Get-Sha256Hex $file.FullName) -eq $expected[$relative]) "$TreeName file changed after freeze: $relative"
    }
}

$contractPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-validation-contract.json'
$freezeRecordPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-baseline-freeze-record.json'
$activationRecordPath = Join-Path $RepositoryRoot 'eng\m10-final-v9-production-activation-decision-record.json'
$srcManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-v9-baseline-src.sha256'
$testsManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-replacement-long-v9-baseline-tests.sha256'
$oldSrcManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-long-baseline-src.sha256'
$oldTestsManifestPath = Join-Path $RepositoryRoot 'eng\m10-final-long-baseline-tests.sha256'
$newTestRelative = 'tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\M10FinalReplacementLongValidationTests.cs'
$newTestPath = Join-Path $RepositoryRoot $newTestRelative

foreach ($path in @($contractPath,$freezeRecordPath,$activationRecordPath,$srcManifestPath,$testsManifestPath,$oldSrcManifestPath,$oldTestsManifestPath,$newTestPath)) {
    Require (Test-Path -LiteralPath $path -PathType Leaf) "required execution file missing: $path"
}

$c = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$f = Get-Content -LiteralPath $freezeRecordPath -Raw | ConvertFrom-Json
$a = Get-Content -LiteralPath $activationRecordPath -Raw | ConvertFrom-Json

Require ($f.status -eq 'VALIDATED') 'baseline-freeze record is not validated.'
Require ($f.replacementLongAuthorized -eq $true) 'replacement long is not authorized by the returned freeze gate.'
Require ($f.replacementLongExecuted -eq $false) 'freeze record already claims the replacement long executed.'
Require ($f.m10ClosureEligible -eq $false) 'freeze record must not pre-authorize M10 closure.'
Require ($f.authoritativeDefault -eq 'integrated-operations-desktop-stable@9') 'freeze authoritative default changed.'
Require ($f.productionMissionPack -eq 'bounded-demand-following-5-10-5@3') 'freeze production mission changed.'
Require ($f.activationFingerprint -eq '7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418') 'activation fingerprint changed.'
Require ($f.replacementAuthoredSeconds -eq 1920 -and $f.replacementAuthoredSteps -eq 192000 -and $f.replacementLegCount -eq 5) 'frozen workload identity changed.'
Require ($f.hardWallCapMinutes -eq 60) 'frozen wall cap changed.'
Require ($f.sourceManifestFiles -eq 959 -and $f.testsManifestFiles -eq 351) 'freeze manifest file counts changed.'
Require ($f.executionCandidateAllowedNewTestFiles -eq 1) 'freeze does not authorize exactly one new test file.'
Require ((Get-Sha256Hex $srcManifestPath) -eq $f.sourceManifestSha256) 'replacement source manifest file changed after freeze.'
Require ((Get-Sha256Hex $testsManifestPath) -eq $f.testsManifestSha256) 'replacement tests manifest file changed after freeze.'
Require ((Get-Sha256Hex $contractPath) -eq $f.contractSha256) 'replacement workload contract changed after freeze.'

Require ($a.status -eq 'VALIDATED' -and $a.productionActivation -eq $true) 'exact-v9 activation record is no longer valid.'
Require ($a.authoritativeDefault -eq 'integrated-operations-desktop-stable@9') 'activation authoritative identity changed.'
Require ($a.productionMissionPack -eq 'bounded-demand-following-5-10-5@3') 'activation mission identity changed.'
Require ($a.fingerprint -eq $f.activationFingerprint) 'activation/freeze fingerprint mismatch.'

Require ($c.total_simulated_seconds -eq 1920 -and $c.total_logical_steps -eq 192000) 'execution workload differs from frozen contract.'
Require (@($c.legs).Count -eq 5) 'execution contract must retain five legs.'
Require ($c.wall_clock_policy.hard_campaign_cap_minutes -eq 60) 'execution hard wall cap differs from freeze.'
Require ($c.replacement_baseline_manifests.execution_candidate_allowed_test_additions -eq 1) 'contract no longer authorizes exactly one test addition.'
Require ($c.replacement_baseline_manifests.production_src_changes_after_freeze_allowed -eq $false) 'contract unexpectedly permits source changes.'

Validate-Frozen-Tree 'src' $srcManifestPath 959

$expectedTests = Read-Manifest $testsManifestPath
Require ($expectedTests.Count -eq 351) 'frozen tests manifest count mismatch.'
$actualTests = @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'tests') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
Require ($actualTests.Count -eq 352) 'execution candidate must contain exactly 351 frozen tests plus one new test file.'
$extra = New-Object System.Collections.Generic.List[string]
foreach ($file in $actualTests) {
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\','/')
    if ($expectedTests.ContainsKey($relative)) {
        Require ((Get-Sha256Hex $file.FullName) -eq $expectedTests[$relative]) "pre-existing test changed after freeze: $relative"
    }
    else {
        $extra.Add($relative)
    }
}
Require ($extra.Count -eq 1) 'execution candidate contains more than one non-frozen test file.'
Require ($extra[0] -eq $newTestRelative) "unexpected execution test addition: $($extra[0])"

$newTestText = Get-Content -LiteralPath $newTestPath -Raw
Require ($newTestText.Contains('[Fact(Explicit = true)]')) 'replacement-long test must remain explicit.'
Require ($newTestText.Contains('NRS_M10_FINAL_REPLACEMENT_LONG')) 'replacement-long opt-in environment seam missing.'
Require ($newTestText.Contains('HealthySteps = 90_000')) 'RL-H1 frozen step count not present in execution test.'
Require ($newTestText.Contains('MissionSteps = 48_000')) 'RL-M1 frozen step count not present in execution test.'
Require ($newTestText.Contains('DegradedSteps = 30_000')) 'RL-D1 frozen step count not present in execution test.'
Require ($newTestText.Contains('ProtectionSteps = 18_000')) 'RL-P1 frozen step count not present in execution test.'
Require ($newTestText.Contains('ReplaySteps = 6_000')) 'RL-R1 frozen step count not present in execution test.'
Require ($newTestText.Contains('HardCampaignCapMinutes = 60d')) 'hard wall-cap enforcement seam missing from execution test.'
Require ($newTestText.Contains('bounded-demand-following-5-10-5@3')) 'production mission @3 binding missing from execution test.'
Require ($newTestText.Contains('integrated-operations-desktop-stable", 9')) 'exact-v9 binding missing from execution test.'

Require ((Get-Sha256Hex $oldSrcManifestPath) -eq $c.historical_reference.old_src_manifest_file_sha256) 'failed exact-v4 source manifest was rewritten.'
Require ((Get-Sha256Hex $oldTestsManifestPath) -eq $c.historical_reference.old_tests_manifest_file_sha256) 'failed exact-v4 tests manifest was rewritten.'
Require ($c.historical_reference.reuse_authorized -eq $false) 'failed exact-v4 manifest reuse became authorized.'

Write-Host 'm10-final-replacement-long-freeze-authorization-recorded=True'
Write-Host 'm10-final-replacement-long-src-manifest-matches=True'
Write-Host 'm10-final-replacement-long-preexisting-tests-manifest-matches=True'
Write-Host 'm10-final-replacement-long-single-test-addition-valid=True'
Write-Host 'm10-final-replacement-long-workload-contract-unchanged=True'
Write-Host 'm10-final-replacement-long-execution-preflight-passes=True'
