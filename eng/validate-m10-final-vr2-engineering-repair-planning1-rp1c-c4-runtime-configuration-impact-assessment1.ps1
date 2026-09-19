$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
}

function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}

function Require-Text([string]$Path, [string]$Needle) {
    $text = Read-Utf8Text $Path
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

function Require-AsciiSource([string]$Path, [string]$Label) {
    Require-File $Path
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    foreach ($byte in $bytes) {
        if ($byte -gt 127) { throw ("{0} is not 7-bit ASCII: {1}" -f $Label, $Path) }
    }
    $text = [System.Text.Encoding]::ASCII.GetString($bytes)
    if ($text -match '(?<!\r)\n') { throw ("{0} is not CRLF-normalized: {1}" -f $Label, $Path) }
}

function Get-NormalizedTextSha256([string]$Path) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $enc = New-Object System.Text.UTF8Encoding($false)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($enc.GetBytes($normalized)))).Replace('-','').ToUpperInvariant() }
    finally { $sha.Dispose() }
}

function Get-FileSha256([string]$Path) {
    Require-File $Path
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToUpperInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}

function Get-TreeSha256([string]$Root, [string[]]$ExcludeRelative = @(), [string[]]$ExcludeDirectoryNames = @()) {
    $resolved = (Resolve-Path -LiteralPath $Root).Path
    $exclude = @{}
    foreach ($item in $ExcludeRelative) { $exclude[$item.Replace('\','/')] = $true }
    $excludedDirectoryNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($name in $ExcludeDirectoryNames) { [void]$excludedDirectoryNames.Add($name) }
    $relativePaths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in @(Get-ChildItem -LiteralPath $resolved -Recurse -File)) {
        $rel = $file.FullName.Substring($resolved.Length + 1).Replace('\','/')
        $segments = @($rel.Split('/'))
        $underExcludedDirectory = $false
        for ($i = 0; $i -lt ($segments.Count - 1); $i++) {
            if ($excludedDirectoryNames.Contains($segments[$i])) { $underExcludedDirectory = $true; break }
        }
        if (-not $underExcludedDirectory -and -not $exclude.ContainsKey($rel)) { $relativePaths.Add($rel) }
    }
    $relativePaths.Sort([System.StringComparer]::Ordinal)
    $ms = New-Object System.IO.MemoryStream
    try {
        foreach ($rel in $relativePaths) {
            $fullPath = Join-Path $resolved $rel.Replace('/','\')
            $relBytes = [System.Text.Encoding]::UTF8.GetBytes($rel)
            $ms.Write($relBytes,0,$relBytes.Length); $ms.WriteByte(0)
            $fileSha = [System.Security.Cryptography.SHA256]::Create()
            $fs = [System.IO.File]::OpenRead($fullPath)
            try { $fileHash = $fileSha.ComputeHash($fs) }
            finally { $fs.Dispose(); $fileSha.Dispose() }
            $ms.Write($fileHash,0,$fileHash.Length); $ms.WriteByte(10)
        }
        $ms.Position = 0
        $treeSha = [System.Security.Cryptography.SHA256]::Create()
        try { $treeHash = $treeSha.ComputeHash($ms) }
        finally { $treeSha.Dispose() }
        return @{ Hash = ([BitConverter]::ToString($treeHash)).Replace('-','').ToUpperInvariant(); Count = $relativePaths.Count }
    }
    finally { $ms.Dispose() }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1-contract.json'
$testRelative = 'NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1cC4RuntimeConfigurationImpactAssessment1Tests.cs'
$testPath = Join-Path $repoRoot ('tests\' + $testRelative.Replace('/','\'))
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1.cmd'
$orchestratorPath = Join-Path $PSScriptRoot 'invoke-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1.ps1'
$adjudicatorPath = Join-Path $PSScriptRoot 'adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1.ps1'
$mainDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1.md'
$reviewDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1_PREEXECUTION_REVIEW.md'
$returnDoc = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1_RETURNED_EVIDENCE_ADJUDICATION.md'
$adr = Join-Path $repoRoot 'docs\adr\0206-execute-runtime-configuration-impact-assessment-on-one-host-before-fdpc2.md'
$projectDoc = Join-Path $repoRoot 'docs\PROJECT.md'
$roadmapDoc = Join-Path $repoRoot 'docs\ROADMAP.md'
$topIndex = Join-Path $repoRoot 'docs\TOP_LEVEL_DOCUMENT_INDEX.md'
$detailedPlanDoc = Join-Path $repoRoot 'docs\M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md'
$adrIndex = Join-Path $repoRoot 'docs\adr\README.md'
$compactionContract = Join-Path $PSScriptRoot 'm10-final-frozen-evidence-ordinary-compaction1-contract.json'
$compactionArtifacts = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalFrozenEvidenceOrdinaryCompaction1_Artifacts'
$planningArtifacts = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeConfigurationImpactAssessmentPlanning1_Artifacts'
$amendArtifacts = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeConfigurationImpactAssessmentPlanning1Amendment1HostProvenance_Artifacts'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'

foreach ($p in @($contractPath,$testPath,$runnerPath,$orchestratorPath,$adjudicatorPath,$mainDoc,$reviewDoc,$returnDoc,$adr,$projectDoc,$roadmapDoc,$detailedPlanDoc,$topIndex,$adrIndex,$compactionContract,$c4Path,$exactPath,$performancePath)) { Require-File $p }
Require-AsciiSource $MyInvocation.MyCommand.Path 'A2 validator'
Require-AsciiSource $orchestratorPath 'A2 orchestrator'
Require-AsciiSource $adjudicatorPath 'A2 adjudicator'
Require-AsciiSource $runnerPath 'A2 runner'

$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1-v1') { throw 'A2 contract schema drifted.' }
if ($contract.status -ne 'CANDIDATE-EVIDENCE-ONLY') { throw 'A2 status drifted.' }
if ($contract.gate -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1') { throw 'A2 gate identity drifted.' }
if ([int]$contract.protocol.profile_count -ne 2 -or [int]$contract.protocol.exact_v9_total_processes -ne 10 -or [int]$contract.protocol.total_exact_v9_measured_calls -ne 230400 -or [int]$contract.evidence.total_required_files -ne 78) { throw 'A2 frozen count contract drifted.' }
if ([double]$contract.protocol.strict_max_us -ne 409.30666666666673) { throw 'A2 strict max drifted.' }
if ([double]$contract.protocol.diagnostic_tail_floor_us -ne 100.0 -or $contract.protocol.diagnostic_tail_floor_is_threshold -ne $false) { throw 'A2 diagnostic floor authority drifted.' }
if ([string]$contract.protocol.source_tree_identity_scope -ne 'CANONICAL-SOURCE-EXCLUDING-BIN-OBJ' -or [string]$contract.protocol.runtime_transient_directory_policy -ne 'ALLOW-LOCAL-GENERATED-OUTPUT-NOT-IN-TREE-IDENTITY' -or [string]$contract.protocol.preexisting_a2_artifact_policy -ne 'ORCHESTRATOR-DELETE-AND-RECREATE-BEFORE-EVIDENCE') { throw 'A2 source-tree / working-copy identity policy drifted.' }
if ($contract.host_policy.single_physical_host_required -ne $true -or $contract.host_policy.cross_host_evidence_mixing_authorized -ne $false -or $contract.host_policy.active_power_scheme_stability_required -ne $true) { throw 'A2 host policy drifted.' }
if ([string]$contract.protocol.xunit_executed_formula -ne 'passed+failed+skipped' -or [string]$contract.protocol.xunit_reported_total_accounting -ne 'ACCEPT-EXECUTED-OR-EXECUTED-PLUS-NOT-RUN' -or [string]$contract.protocol.xunit_execution_cardinality_source -ne 'executed') { throw 'A2 structured xUnit accounting contract drifted.' }
foreach ($name in @('full_domain_performance_confirmation2_authorized','rp1c_selection_authorized','production_runtime_change_authorized','production_repair_authorized','threshold_change_authorized','exact_v9_change_authorized','vr3_authorized','p3_r1_authorized','second_replacement_long_authorized')) {
    if ($contract.authority.$name -ne $false) { throw ("A2 authority drifted: {0}" -f $name) }
}

$sets = @(
    @{ Root=$planningArtifacts; Hashes=$contract.normalized_lf_sha256.planning_returned; Label='Planning 1' },
    @{ Root=$amendArtifacts; Hashes=$contract.normalized_lf_sha256.host_amendment_returned; Label='Host Amendment 1' },
    @{ Root=$compactionArtifacts; Hashes=$contract.normalized_lf_sha256.compaction_returned; Label='Compaction 1' }
)
foreach ($set in $sets) {
    $files = @(Get-ChildItem -LiteralPath $set.Root -File)
    if ($files.Count -ne 4) { throw ("{0} returned artifact count drifted: {1}" -f $set.Label,$files.Count) }
    foreach ($file in $files) {
        $prop = $set.Hashes.PSObject.Properties[$file.Name]
        if ($null -eq $prop) { throw ("Unexpected {0} returned artifact: {1}" -f $set.Label,$file.Name) }
        if ((Get-NormalizedTextSha256 $file.FullName) -ne ([string]$prop.Value).ToUpperInvariant()) { throw ("{0} returned artifact hash drifted: {1}" -f $set.Label,$file.Name) }
    }
}
Require-Text (Join-Path $planningArtifacts '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $amendArtifacts '01-contract-and-provenance.txt') 'single-physical-host-required=True'
Require-Text (Join-Path $compactionArtifacts '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $compactionArtifacts '03-compaction-summary.txt') 'expanded-large-payloads-present=0'

if ((Get-NormalizedTextSha256 $c4Path) -ne $contract.normalized_lf_sha256.c4_candidate) { throw 'C4 candidate hash drifted.' }
if ((Get-NormalizedTextSha256 $exactPath) -ne $contract.normalized_lf_sha256.exact_v9_corpus) { throw 'Exact-v9 corpus hash drifted.' }
if ((Get-NormalizedTextSha256 $performancePath) -ne $contract.normalized_lf_sha256.performance_baseline) { throw 'Performance baseline hash drifted.' }

$srcTree = Get-TreeSha256 -Root (Join-Path $repoRoot 'src') -ExcludeDirectoryNames @('bin','obj')
if ($srcTree.Count -ne 959 -or $srcTree.Hash -ne '4A8904F573157AC1D5B58E4CBD58E94FAB09C28CA9A73D2E2330147BBCF9AF9E') { throw 'src tree drifted from Compaction 1.' }
$testsBaseTree = Get-TreeSha256 -Root (Join-Path $repoRoot 'tests') -ExcludeRelative @($testRelative) -ExcludeDirectoryNames @('bin','obj')
if ($testsBaseTree.Count -ne 381 -or $testsBaseTree.Hash -ne '6DD38EC9C89D55691420D65E39BB3C3E74966ED484CD5090211E4263201DDBAE') { throw 'Pre-existing tests drifted from Compaction 1.' }

$hashChecks = @{
    focused_test=$testPath; runner=$runnerPath; orchestrator=$orchestratorPath; adjudicator=$adjudicatorPath
    main_doc=$mainDoc; preexecution_review=$reviewDoc; returned_evidence_adjudication=$returnDoc; adr0206=$adr
}
foreach ($name in $hashChecks.Keys) {
    $expected = [string]$contract.normalized_lf_sha256.implementation.PSObject.Properties[$name].Value
    if ((Get-NormalizedTextSha256 $hashChecks[$name]) -ne $expected.ToUpperInvariant()) { throw ("A2 implementation hash drifted: {0}" -f $name) }
}

Require-Text $testPath '[Fact(Explicit = true)]'
Require-Text $testPath 'AMBIENT-UNSET'
Require-Text $testPath 'EXPLICIT-REFERENCE-ALL-ON'
Require-Text $testPath 'host-fingerprint-sha256='
Require-Text $testPath 'measured-passes=64'
Require-Text $runnerPath 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN'
Require-Text $runnerPath 'COMPlus_TieredCompilation'
Require-Text $runnerPath 'COMPlus_ReadyToRun'
Require-Text $runnerPath 'Engineering-negative outcomes are retained as evidence'
Require-Text $orchestratorPath '--report-xunit-filename'
Require-Text $orchestratorPath "'--results-directory', (Quote-Arg `$WorkDirectory), '--minimum-expected-tests', '1', '--'"
Require-Text $orchestratorPath 'console-language-independent=True'
Require-Text $orchestratorPath "`$assembly.'not-run'"
Require-Text $orchestratorPath 'TOTAL-INCLUDES-NOT-RUN'
Require-Text $orchestratorPath 'TOTAL-EQUALS-EXECUTED'
Require-Text $orchestratorPath '$counts.Executed'
Require-Text $orchestratorPath 'run-count accounting is inconsistent'
Require-Text $orchestratorPath 'discovered no executed tests'
Require-Text $orchestratorPath 'COMPlus_TieredPGO'
Require-Text $orchestratorPath 'NRS_A2_HOST_FINGERPRINT_SHA256'
Require-Text $orchestratorPath 'INFRASTRUCTURE-RED-HOST-PROVENANCE-MISMATCH'
Require-Text $adjudicatorPath 'A2 final evidence file count mismatch'
Require-Text $adjudicatorPath "`$run['test-executed']"
Require-Text $adjudicatorPath "['reported-total']"
Require-Text $adjudicatorPath 'engineering-adjudication=NOT-AUTOMATICALLY-PERFORMED'
Require-Text $mainDoc 'exactly 78 files'
Require-Text $reviewDoc 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $projectDoc 'BRANCH A2 RUNTIME CONFIGURATION IMPACT ASSESSMENT 1 evidence candidate'
Require-Text $roadmapDoc 'Branch A2 execution checkpoint'
Require-Text $detailedPlanDoc 'Live checkpoint override'
Require-Text $roadmapDoc 'Frozen Evidence Ordinary Compaction 1 returned / A2 implementation'
Require-Text $topIndex 'M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1.md'
Require-Text $adrIndex 'ADR 0206'

$ordinaryRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary'
$tooLarge = @(Get-ChildItem -LiteralPath $ordinaryRoot -Recurse -File | Where-Object { $_.Length -gt 1048576 })
if ($tooLarge.Count -ne 0) { throw ("Compact ordinary boundary regressed; direct files >1 MiB: {0}" -f $tooLarge.Count) }
$compaction = Read-Utf8Text $compactionContract | ConvertFrom-Json
foreach ($pack in @($compaction.archive_packs)) {
    $path = Join-Path $repoRoot ([string]$pack.path).Replace('/','\')
    Require-File $path
    if ((Get-FileSha256 $path) -ne ([string]$pack.sha256).ToUpperInvariant()) { throw ("Frozen archive pack hash drifted: {0}" -f $pack.path) }
    if ((Get-Item -LiteralPath $path).Length -ne [long]$pack.bytes) { throw ("Frozen archive pack size drifted: {0}" -f $pack.path) }
}

# A previous failed A2 attempt may legitimately leave partial artifacts in a developer working copy.
# The orchestrator deletes and recreates its owned A2 artifact root before collecting new evidence.
# Candidate-package cleanliness is a packaging-time invariant, not a runtime working-copy invariant.
# Local bin/obj/TestResults directories may likewise exist after previous restore/build/test runs.

Write-Host 'Runtime Configuration Impact Assessment 1 static audit: PASS-AS-AUTHORED'
Write-Host 'A2 implementation is evidence-only; FDPC2/RP1C/production runtime authority remains false.'
