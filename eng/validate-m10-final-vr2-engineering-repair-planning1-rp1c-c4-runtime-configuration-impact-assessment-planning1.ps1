$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
}
function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}
function Require-Text([string]$Path,[string]$Needle) {
    $text = Read-Utf8Text $Path
    if (-not $text.Contains($Needle)) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Require-True([object]$Value,[string]$Label) {
    if ($Value -ne $true) { throw ("{0} must be true." -f $Label) }
}
function Require-False([object]$Value,[string]$Label) {
    if ($Value -ne $false) { throw ("{0} must be false." -f $Label) }
}
function Require-Near([double]$Actual,[double]$Expected,[double]$Tolerance,[string]$Label) {
    if ([Math]::Abs($Actual-$Expected) -gt $Tolerance) { throw ("{0} drifted: actual={1}; expected={2}" -f $Label,$Actual,$Expected) }
}
function Require-AsciiSource([string]$Path,[string]$Label) {
    Require-File $Path
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    foreach ($b in $bytes) {
        if ($b -gt 127) { throw ("{0} must remain 7-bit ASCII for Windows PowerShell 5.1 source safety: {1}" -f $Label,$Path) }
    }
}
function Require-NormalizedTextSha256([string]$Path,[string]$Expected,[string]$Label) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n","`n").Replace("`r","`n")
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($enc.GetBytes($normalized)) } finally { $sha.Dispose() }
    $actual = ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) { throw ("{0} normalized-text SHA-256 drifted: actual={1}; expected={2}" -f $Label,$actual,$Expected) }
}
function Get-ByteSha256Hex([string]$Path) {
    Require-File $Path
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash([System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)) } finally { $sha.Dispose() }
    return ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
}
function Require-ByteTreeManifestSha256([string]$Root,[string]$Expected,[string]$Label) {
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { throw ("Required evidence root not found: {0}" -f $Root) }
    $rootFull = (Resolve-Path -LiteralPath $Root).Path
    $sep = [System.IO.Path]::DirectorySeparatorChar.ToString()
    $prefix = $rootFull
    if (-not $prefix.EndsWith($sep)) { $prefix += $sep }
    $paths = [string[]]@(Get-ChildItem -LiteralPath $Root -Recurse -File | ForEach-Object { $_.FullName })
    [Array]::Sort($paths,[StringComparer]::Ordinal)
    $builder = New-Object System.Text.StringBuilder
    foreach ($fullPath in $paths) {
        if (-not $fullPath.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw ("Evidence path escaped root: {0}" -f $fullPath) }
        $relative = $fullPath.Substring($prefix.Length).Replace('\','/')
        [void]$builder.Append((Get-ByteSha256Hex $fullPath)).Append('  ').Append($relative).Append("`n")
    }
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($enc.GetBytes($builder.ToString())) } finally { $sha.Dispose() }
    $actual = ([BitConverter]::ToString($hash)).Replace('-','').ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) { throw ("{0} byte-manifest SHA-256 drifted: actual={1}; expected={2}" -f $Label,$actual,$Expected) }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$validatorPath = $MyInvocation.MyCommand.Path
$contractPath = Join-Path $PSScriptRoot 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-contract.json'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1.cmd'
$adjudicationPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR1_RETURNED_EVIDENCE_ADJUDICATION.md'
$planPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1.md'
$reviewPath = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1_PREEXECUTION_REVIEW.md'
$adrPath = Join-Path $repoRoot 'docs\adr\0203-qualify-runtime-configuration-impact-before-rp1c-selection.md'
$frozenRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_DynamicPgoComparator1_Artifacts'
$pgo01 = Join-Path $frozenRoot '01-contract-and-provenance.txt'
$pgo02 = Join-Path $frozenRoot '02-pgo-run-summary.csv'
$pgo03 = Join-Path $frozenRoot '03-pgo-contrast-summary.csv'
$pgo04 = Join-Path $frozenRoot '04-tail-row-path-summary.csv'
$pgo05 = Join-Path $frozenRoot '05-pgo-evidence-summary.txt'
$pgo06 = Join-Path $frozenRoot '06-dynamic-pgo-comparator1-summary.txt'
$c4Path = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$exactPath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv'
$performancePath = Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv'
$hotPathTest = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Application.Tests\Milestones\M10972Hotfix2TenMillisecondHotPathHardeningTests.cs'
$simReplay = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Runtime\SimulationReplayTests.cs'
$simLong = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Runtime\SimulationLongRunDeterminismTests.cs'
$challengeReplay = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Application.Tests\Scenarios\Challenges\Replay\M10965ChallengeReplayCheckpointClosureTests.cs'
$sameSeed = Join-Path $repoRoot 'tests\NuclearReactorSimulator.Application.Tests\ControlRoom\Automation\M10984ReplayCheckpointSameSeedIntegrityTests.cs'

foreach ($path in @($contractPath,$runnerPath,$adjudicationPath,$planPath,$reviewPath,$adrPath,$pgo01,$pgo02,$pgo03,$pgo04,$pgo05,$pgo06,$c4Path,$exactPath,$performancePath,$hotPathTest,$simReplay,$simLong,$challengeReplay,$sameSeed)) { Require-File $path }
Require-AsciiSource $validatorPath 'Planning validator source'
Require-AsciiSource $runnerPath 'Planning runner source'

$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-v1') { throw 'Contract schema drifted.' }
if ($contract.status -ne 'CANDIDATE-PLANNING-ONLY') { throw 'Planning status drifted.' }
if ($contract.gate -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1') { throw 'Planning gate identity drifted.' }

$p = $contract.prerequisite
if ($p.dynamic_pgo_comparator1_returned_adjudication -ne 'PASS') { throw 'Dynamic PGO Comparator 1 returned adjudication must be PASS.' }
if ([int]$p.returned_evidence_files -ne 46 -or [int]$p.returned_processes -ne 10 -or [int]$p.returned_measured_calls -ne 230400) { throw 'Returned PGO evidence shape drifted.' }
if ($p.branch -ne 'A-RUNTIME-SENSITIVE') { throw 'Roadmap branch drifted.' }
Require-True $p.tiered_compilation_gross_slowdown_causal 'TieredCompilation gross slowdown causality'
Require-True $p.dynamic_pgo_central_distribution_causal 'Dynamic PGO central-distribution causality'
Require-False $p.dynamic_pgo_rare_strict_tail_causal 'Dynamic PGO rare strict-tail causality'
Require-True $p.c4_candidate_immutable 'C4 immutability'
Require-True $p.exact_v9_corpus_immutable 'Exact-v9 immutability'
Require-Near ([double]$p.strict_max_us) 409.30666666666673 0.0000000001 'Strict maximum'

$files = @(Get-ChildItem -LiteralPath $frozenRoot -Recurse -File)
if ($files.Count -ne 46) { throw ("Frozen Dynamic PGO Comparator evidence must contain exactly 46 files; found {0}." -f $files.Count) }
$processDirs = @(Get-ChildItem -LiteralPath $frozenRoot -Directory | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Directory })
if ($processDirs.Count -ne 10) { throw ("Frozen Dynamic PGO Comparator evidence must contain 10 process directories; found {0}." -f $processDirs.Count) }
foreach ($dir in $processDirs) {
    $processFiles = @(Get-ChildItem -LiteralPath $dir.FullName -File)
    if ($processFiles.Count -ne 4) { throw ("Each frozen PGO process directory must contain four files: {0}" -f $dir.FullName) }
    $raw = Join-Path $dir.FullName '02-exact-v9-call-timing.csv'
    $rows = @(Import-Csv -LiteralPath $raw)
    if ($rows.Count -ne 23040) { throw ("PGO raw process matrix must contain 23040 calls: {0}" -f $raw) }
    if (@($rows | Where-Object { $_.resolved -ne 'True' }).Count -ne 0) { throw ("PGO raw process contains unresolved calls: {0}" -f $raw) }
}
Require-ByteTreeManifestSha256 $frozenRoot $contract.returned_evidence_tree_byte_manifest_sha256 'Frozen Dynamic PGO Comparator evidence tree'

$h = $contract.normalized_lf_sha256
Require-NormalizedTextSha256 $pgo01 $h.pgo_contract 'PGO contract/provenance'
Require-NormalizedTextSha256 $pgo02 $h.pgo_run_summary 'PGO run summary'
Require-NormalizedTextSha256 $pgo03 $h.pgo_contrast_summary 'PGO contrast summary'
Require-NormalizedTextSha256 $pgo04 $h.pgo_tail_summary 'PGO tail summary'
Require-NormalizedTextSha256 $pgo05 $h.pgo_evidence_summary 'PGO evidence summary'
Require-NormalizedTextSha256 $pgo06 $h.pgo_gate_summary 'PGO gate summary'
Require-NormalizedTextSha256 $c4Path $h.c4_candidate 'C4 candidate'
Require-NormalizedTextSha256 $exactPath $h.exact_v9_corpus 'Exact-v9 corpus'
Require-NormalizedTextSha256 $performancePath $h.performance_baseline 'Performance baseline'

Require-Text $pgo01 'status=PASS-DYNAMIC-PGO-COMPARATOR-EVIDENCE-INTEGRITY'
Require-Text $pgo05 'PGO-OFF-QJFL-ON: processes=5; calls=115200; calls-over-100us=13; calls-over-max=1'
Require-Text $pgo05 'PGO-ON-QJFL-ON: processes=5; calls=115200; calls-over-100us=8; calls-over-max=0'
Require-Text $pgo06 'status=PASS-DYNAMIC-PGO-COMPARATOR1-EVIDENCE-COMPLETE'
Require-Text $adjudicationPath 'Dynamic PGO central-distribution causality'
Require-Text $adjudicationPath 'does **not** prove Dynamic PGO ownership of the rare strict wall-clock tail'
Require-Text $planPath 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1'
Require-Text $planPath 'AMBIENT-UNSET'
Require-Text $planPath 'EXPLICIT-REFERENCE-ALL-ON'
Require-Text $planPath 'ordinary non-explicit Release suite'
Require-Text $planPath '64 x 360 identity/order check per process'
Require-Text $reviewPath 'STATIC-PREEXECUTION-REVIEW-PASS'
Require-Text $adrPath 'qualification reference only'
Require-Text $hotPathTest '[Fact(Explicit = true)]'
Require-Text $simLong 'OneHundredThousandSteps_AreIndependentFromExternalPulseSegmentation'
Require-Text $challengeReplay 'RecordingProjection_ReconstructsLifecycleDemandAndScoreIdenticallyAfterCanonicalReplay'

$runRows = @(Import-Csv -LiteralPath $pgo02)
if ($runRows.Count -ne 10) { throw 'PGO run summary must contain 10 rows.' }
$offRows = @($runRows | Where-Object { $_.mode_id -eq 'PGO-OFF-QJFL-ON' })
$onRows = @($runRows | Where-Object { $_.mode_id -eq 'PGO-ON-QJFL-ON' })
if ($offRows.Count -ne 5 -or $onRows.Count -ne 5) { throw 'PGO mode process counts drifted.' }
foreach ($row in $runRows) {
    if ([int]$row.measured_calls -ne 23040 -or [int]$row.unresolved_calls -ne 0) { throw 'PGO process integrity summary drifted.' }
    if ([int]$row.candidate_allocated_bytes -ne 0 -or [int]$row.harness_allocated_bytes -ne 0) { throw 'PGO allocation neutrality drifted.' }
    if ([int]$row.gc_gen0 -ne 0 -or [int]$row.gc_gen1 -ne 0 -or [int]$row.gc_gen2 -ne 0) { throw 'PGO GC neutrality drifted.' }
}
$contrast = @(Import-Csv -LiteralPath $pgo03)
if ($contrast.Count -ne 1) { throw 'PGO contrast summary must contain one row.' }
$c = $contrast[0]
if ($c.changed_factor -ne 'DOTNET_TieredPGO') { throw 'PGO changed factor drifted.' }
if ([int]$c.left_calls_over_100us -ne 13 -or [int]$c.right_calls_over_100us -ne 8 -or [int]$c.left_calls_over_max -ne 1 -or [int]$c.right_calls_over_max -ne 0) { throw 'PGO tail counts drifted.' }
Require-Near ([double]$c.left_median_of_process_medians_us) 4.7 0.0000001 'PGO OFF process median aggregate'
Require-Near ([double]$c.right_median_of_process_medians_us) 2.1 0.0000001 'PGO ON process median aggregate'
Require-Near ([double]$c.left_median_of_process_p95_us) 5.4 0.0000001 'PGO OFF p95 aggregate'
Require-Near ([double]$c.right_median_of_process_p95_us) 2.5 0.0000001 'PGO ON p95 aggregate'

$f = $contract.future_gate
if ($f.id -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1') { throw 'Future A2 gate identity drifted.' }
Require-True $f.evidence_only 'Future A2 evidence-only rule'
if ([int]$f.profile_count -ne 2 -or [int]$f.exact_v9_processes_per_profile -ne 5 -or [int]$f.exact_v9_total_processes -ne 10) { throw 'Future A2 profile/process contract drifted.' }
if ([int]$f.exact_v9_rows -ne 360 -or [int]$f.warmup_passes -ne 16 -or [int]$f.measured_passes -ne 64 -or [int]$f.rotation_stride -ne 37) { throw 'Future A2 exact-v9 matrix drifted.' }
if ([int]$f.measured_calls_per_process -ne 23040 -or [int]$f.total_exact_v9_measured_calls -ne 230400) { throw 'Future A2 measured-call contract drifted.' }
Require-Near ([double]$f.strict_max_us) 409.30666666666673 0.0000000001 'Future A2 strict maximum'
Require-Near ([double]$f.diagnostic_tail_floor_us) 100.0 0.0000001 'Future A2 diagnostic floor'
Require-False $f.diagnostic_tail_floor_changes_qualification_threshold 'Diagnostic floor threshold rule'
if ([int]$f.same_boundary_strict_repro_processes -ne 2) { throw 'Same-boundary reproducibility rule drifted.' }
if ([int]$f.ordinary_release_suite_runs_per_profile -ne 1 -or [int]$f.replay_determinism_runs_per_profile -ne 1 -or [int]$f.non_vr2_performance_runs_per_profile -ne 3) { throw 'Future A2 assessment-family run counts drifted.' }
if ([int]$f.total_required_files -ne 78) { throw 'Future A2 evidence file count drifted.' }
Require-False $f.automatic_selection_authorized 'Automatic A2 selection'
Require-False $f.engineering_negative_is_infrastructure_red 'Engineering negative classification'

$profiles = @($contract.profiles)
if ($profiles.Count -ne 2 -or $profiles[0].id -ne 'AMBIENT-UNSET' -or $profiles[1].id -ne 'EXPLICIT-REFERENCE-ALL-ON') { throw 'A2 profile identities/order drifted.' }
$vars = @('DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_TC_QuickJit','DOTNET_TC_QuickJitForLoops','DOTNET_ReadyToRun')
foreach ($name in $vars) {
    if ($null -ne $profiles[0].environment.PSObject.Properties[$name].Value) { throw ("Ambient profile must leave {0} unset." -f $name) }
    if ([string]$profiles[1].environment.PSObject.Properties[$name].Value -ne '1') { throw ("Explicit reference profile must set {0}=1." -f $name) }
}

$classes = @($contract.focused_replay_determinism_classes)
if ($classes.Count -ne 4) { throw 'Focused replay/determinism class count drifted.' }
if ($contract.non_vr2_performance_class -ne 'NuclearReactorSimulator.Application.Tests.Milestones.M10972Hotfix2TenMillisecondHotPathHardeningTests') { throw 'Non-VR2 performance owner drifted.' }
$envList = @($contract.caller_environment_preflight.required_unset)
if ($envList.Count -ne 5) { throw 'Caller environment variable count drifted.' }
for ($i=0; $i -lt 5; $i++) { if ([string]$envList[$i] -ne $vars[$i]) { throw 'Caller environment identity/order drifted.' } }
if ($contract.caller_environment_preflight.failure_classification -ne 'CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN') { throw 'Caller environment failure classification drifted.' }
Require-True $contract.caller_environment_preflight.must_not_silently_clear_parent_environment 'Caller environment fail-closed rule'

$futureTest = Join-Path $repoRoot $contract.future_implementation_absence.test_file
$futureRunner = Join-Path $repoRoot $contract.future_implementation_absence.runner_file
if (Test-Path -LiteralPath $futureTest -PathType Leaf) { throw 'Future A2 test is already implemented; planning must remain planning-only.' }
if (Test-Path -LiteralPath $futureRunner -PathType Leaf) { throw 'Future A2 runner is already implemented; planning must remain planning-only.' }

$a = $contract.authority
Require-False $a.runtime_configuration_impact_assessment_implementation_authorized_now 'A2 implementation authority now'
Require-True $a.runtime_configuration_impact_assessment_implementation_authorizable_after_returned_planning_adjudication 'A2 post-planning eligibility'
Require-False $a.rp1c_selection_authorized 'RP1C selection authority'
Require-False $a.production_runtime_change_authorized 'Production runtime authority'
Require-False $a.production_repair_authorized 'Production repair authority'
Require-False $a.candidate_mutation_authorized 'Candidate mutation authority'
Require-False $a.threshold_change_authorized 'Threshold authority'
Require-False $a.exact_v9_change_authorized 'Exact-v9 authority'
Require-False $a.full_domain_performance_confirmation2_authorized 'FDPC2 authority'
Require-False $a.vr3_authorized 'VR3 authority'
Require-False $a.p3_r1_authorized 'P3-R1 authority'
Require-False $a.second_replacement_long_authorized 'Second replacement-long authority'

$artifactDir = Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding -ArgumentList $false
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), @(
    'status=PASS-AS-AUTHORED',
    'gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1',
    'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-v1',
    'dynamic-pgo-comparator1-returned-adjudication=PASS',
    'roadmap-branch=A-RUNTIME-SENSITIVE',
    'dynamic-pgo-central-distribution-causal=True',
    'dynamic-pgo-rare-strict-tail-causal=False',
    'future-gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1',
    'future-runtime-profiles=2',
    'future-exact-v9-processes=10',
    'future-exact-v9-measured-calls=230400',
    'future-required-files=78',
    'strict-max-us=409.30666666666673',
    'diagnostic-tail-floor-us=100',
    'diagnostic-tail-floor-is-threshold=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
), $utf8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-impact-assessment-matrix.csv'), @(
    'profile_id,tiered_compilation,tiered_pgo,quickjit,quickjit_for_loops,ready_to_run,exact_v9_processes,ordinary_suite_runs,replay_determinism_runs,non_vr2_performance_runs',
    'AMBIENT-UNSET,unset,unset,unset,unset,unset,5,1,1,3',
    'EXPLICIT-REFERENCE-ALL-ON,1,1,1,1,1,5,1,1,3'
), $utf8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-planning-summary.txt'), @(
    'status=PASS-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING-CONTRACT-FROZEN',
    'selection-result=NOT-PERFORMED',
    'a2-implementation-contained=False',
    'a2-implementation-authorized-now=False',
    'ambient-effective-default-equivalence=PLANNED-NOT-PROVEN',
    'explicit-reference-is-production-recommendation=False',
    'full-domain-performance-confirmation2-authorized=False',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False',
    'next-action=RETURN-COMPLETE-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING-ARTIFACTS-FOR-ADJUDICATION'
), $utf8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), @(
    'status=STATIC-PREEXECUTION-REVIEW-PASS',
    'finding-1=DYNAMIC-PGO-COMPARATOR1-EVIDENCE-COMPLETE',
    'finding-2=DYNAMIC-PGO-CENTRAL-DISTRIBUTION-CAUSAL-EFFECT',
    'finding-3=DYNAMIC-PGO-RARE-STRICT-TAIL-CAUSALITY-NOT-PROMOTED',
    'finding-4=AMBIENT-VS-EXPLICIT-PROJECT-IMPACT-ASSESSMENT-REQUIRED',
    'future-profiles=2',
    'future-exact-v9-processes=10',
    'future-exact-v9-measured-calls=230400',
    'future-required-files=78',
    'ordinary-release-suite=INCLUDED-BOTH-PROFILES',
    'm10-replay-determinism=INCLUDED-BOTH-PROFILES',
    'non-vr2-performance-owner=INCLUDED-3-FRESH-PROCESSES-PER-PROFILE',
    'rp1c-selection-authorized=False',
    'production-runtime-change-authorized=False'
), $utf8)

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 RUNTIME CONFIGURATION IMPACT ASSESSMENT PLANNING 1'
Write-Host '============================================================'
Write-Host 'Planning only on adjudicated Dynamic PGO Comparator 1 evidence.'
Write-Host 'No A2 implementation, RP1C selection, production/runtime change, FDPC2, VR3 or P3-R1 authorization.'
Write-Host ''
Write-Host 'Runtime Configuration Impact Assessment Planning 1 static audit: PASS-AS-AUTHORED'
Write-Host ("Artifacts: {0}" -f $artifactDir)
