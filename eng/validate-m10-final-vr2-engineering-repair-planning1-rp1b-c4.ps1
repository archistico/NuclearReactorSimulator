$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Fail([string]$Message) {
    Write-Host "C4 static contract audit: FAIL - $Message" -ForegroundColor Red
    exit 2
}

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { Fail $Message }
}

function Read-All([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) "missing file: $Path"
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}

function Normalized-Sha256([string]$Path) {
    $text = Read-All $Path
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally { $sha.Dispose() }
}

function Require-Contains([string]$Text, [string]$Needle, [string]$Context) {
    Require ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) "$Context missing marker: $Needle"
}

$contractPath = 'eng\m10-final-vr2-engineering-repair-planning1-rp1b-c4-contract.json'
$contract = (Read-All $contractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-rp1b-c4-v1') 'contract schema mismatch'
Require ($contract.status -eq 'CANDIDATE') 'contract status mismatch'
Require ([bool]$contract.authority.c4_test_only_implementation_authorized) 'C4 test-only implementation is not authorized by returned planning adjudication'
Require (-not [bool]$contract.authority.rp1c_selection_authorized) 'RP1C must remain unauthorized'
Require (-not [bool]$contract.authority.production_repair_authorized) 'production repair must remain unauthorized'
Require ($contract.candidate.candidate_id -eq 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE') 'candidate identity drift'
Require ($contract.candidate.implementation_topology -eq 'ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK') 'candidate topology drift'
Require ([int]$contract.evidence.total_required_files -eq 59) 'required file count drift'
Require ([int]$contract.runner.focused_process_invocations -eq 11) 'focused process count drift'

$planningRoot = 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Planning1_Artifacts'
$planningFiles = @(
    '01-contract-and-provenance.txt',
    '02-source-attribution.txt',
    '03-c4-planning-summary.txt',
    '04-preexecution-hardening-review.txt'
)
foreach ($name in $planningFiles) { Require (Test-Path -LiteralPath (Join-Path $planningRoot $name) -PathType Leaf) "returned planning artifact missing: $name" }
$planningContract = Read-All (Join-Path $planningRoot '01-contract-and-provenance.txt')
$planningSummary = Read-All (Join-Path $planningRoot '03-c4-planning-summary.txt')
$planningReview = Read-All (Join-Path $planningRoot '04-preexecution-hardening-review.txt')
Require-Contains $planningContract 'status=PASS-AS-AUTHORED' 'returned planning contract'
Require-Contains $planningContract 'contract-schema=v4' 'returned planning contract'
Require-Contains $planningContract 'c4-test-only-implementation-authorizable-after-returned-planning-adjudication=True' 'returned planning contract'
Require-Contains $planningSummary 'classification=C4-PLANNING-CONTRACT-FROZEN' 'returned planning summary'
Require-Contains $planningContract 'future-required-file-count=59' 'returned planning contract'
Require-Contains $planningReview 'status=REV2-PREEXECUTION-REVIEW-PASS' 'returned planning review'

$returnedSummary = Read-All 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Planning1_UserReturnedSummary.txt'
Require-Contains $returnedSummary 'returned-planning-adjudication=PASS' 'returned adjudication summary'
Require-Contains $returnedSummary 'c4-test-only-implementation-authorized=True' 'returned adjudication summary'
Require-Contains $returnedSummary 'rp1c-selection-authorized=False' 'returned adjudication summary'

$c2 = 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bRefinement1ShadowThermodynamicCandidates.cs'
$c3 = 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bRefinement2ShadowThermodynamicCandidates.cs'
$r5 = 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.cs'
Require ((Normalized-Sha256 $c2) -eq '3731100664621D2E7E396BC12790C36EF4667F1526E5AEF0186F3DD6260274C8') 'immutable C2 source changed'
Require ((Normalized-Sha256 $c3) -eq 'B92302DB5B19213407C6B8A2F3A9CD33212FF55109E3087972928B4121A87064') 'immutable C3 source changed'
Require ((Normalized-Sha256 $r5) -eq '7B3CFEA0464051D1E658978BA2ED6F7BB0B5B057D6B64F94884C800911F6A207') 'historical Refinement 5 timing test changed'

$semanticPins = @{
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\02-vr2-reference-point-corpus.csv' = '8B4EB8EBD18CD251E7016E4213DC159DE6424E542DB7F9D3FBDC2438CF1662B9'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv' = '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\04-hydraulic-context.csv' = 'CFC1F96B45F310A470BF93688608A8E64F79D964C9504D5BCEBF735C518C6636'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\05-seam-probe-map.csv' = '9035396FDB5A1F6AD89F0B9D28A26992D577906812DE412EFA2AABA98FEBE47D'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts\02-candidate-vr2-error-map.csv' = '0089C21ED51488711A33FDB8F9D93DE78FD33641D306AFF45257176BD96AC9CF'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts\03-candidate-exact-v9-node-map.csv' = '50AC3A1648CB5B2AB18215D77EF8B5FEDD73C1698BF7312E731302D857E3DBA2'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts\04-candidate-seam-map.csv' = 'DA788DDBC6F0A392AD07E1D26C01C726AC0AD2C8CA600E56AF2B8732395AEA37'
    'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts\05-candidate-hydraulic-replay.csv' = '3D07BDBC21B5B2E403270592ABF6B42343B161DAB6B7BE3484527772368A1A81'
}
foreach ($path in $semanticPins.Keys) {
    Require ((Normalized-Sha256 $path) -eq $semanticPins[$path]) "frozen semantic evidence changed: $path"
}

$c4SourcePath = 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$c4TestPath = 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests.cs'
$c4Source = Read-All $c4SourcePath
$c4Tests = Read-All $c4TestPath
Require ((Normalized-Sha256 $c4SourcePath) -eq [string]$contract.implementation_normalized_lf_sha256.c4_candidate) 'C4 candidate hash does not match implementation contract'
Require ((Normalized-Sha256 $c4TestPath) -eq [string]$contract.implementation_normalized_lf_sha256.c4_tests) 'C4 test hash does not match implementation contract'

Require-Contains $c4Source 'internal sealed class Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate' 'C4 source'
Require-Contains $c4Source 'C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE' 'C4 source'
Require-Contains $c4Source 'C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE' 'C4 source'
Require-Contains $c4Source 'Rp1bC4ResolutionPath.ImmutableC2Fallback' 'C4 source'
Require-Contains $c4Source 'TryResolveMixtureAllocationNeutral' 'C4 source'
Require-Contains $c4Source 'TryResolveLiquidTableAllocationNeutral' 'C4 source'
Require-Contains $c4Source 'TryResolveNearBoundaryLiquid' 'C4 source'
Require-Contains $c4Source '_fallback.TryResolve' 'C4 source'
Require-Contains $c4Source 'TryBuildReachabilityBoundaryAllocationNeutral' 'C4 source'
Require ($c4Source.IndexOf('BoundaryIndex == 3', [StringComparison]::OrdinalIgnoreCase) -lt 0) 'boundary-3 special case is forbidden'
Require ($c4Source.IndexOf('boundary_index == 3', [StringComparison]::OrdinalIgnoreCase) -lt 0) 'boundary-3 special case is forbidden'

$resolveStart = $c4Source.IndexOf('internal bool TryResolveWithPath', [StringComparison]::Ordinal)
$buildStart = $c4Source.IndexOf('private static C4TemperatureRow[] BuildLiquidRows', [StringComparison]::Ordinal)
Require ($resolveStart -ge 0 -and $buildStart -gt $resolveStart) 'cannot isolate C4 resolve-time section'
$resolveSection = $c4Source.Substring($resolveStart, $buildStart - $resolveStart)
Require ($resolveSection.IndexOf('new List<', [StringComparison]::Ordinal) -lt 0) 'resolve-time List allocation is forbidden'
Require ($resolveSection.IndexOf('.OrderBy', [StringComparison]::Ordinal) -lt 0) 'resolve-time LINQ ordering is forbidden'
Require ($resolveSection.IndexOf('.Select(', [StringComparison]::Ordinal) -lt 0) 'resolve-time LINQ projection is forbidden'
Require ($resolveSection.IndexOf('foreach', [StringComparison]::Ordinal) -lt 0) 'resolve-time foreach is forbidden'
Require ($resolveSection.IndexOf('IapwsIf97Reference.', [StringComparison]::Ordinal) -lt 0) 'direct IF97 in resolve-time section is forbidden'

$tailResolveStart = $c4Source.IndexOf('private static bool TryBisectMixture', [StringComparison]::Ordinal)
$tailResolveEnd = $c4Source.IndexOf('private readonly record struct C4TablePoint', [StringComparison]::Ordinal)
Require ($tailResolveStart -ge 0 -and $tailResolveEnd -gt $tailResolveStart) 'cannot isolate C4 tail resolve-time helper section'
$tailResolveSection = $c4Source.Substring($tailResolveStart, $tailResolveEnd - $tailResolveStart)
Require ($tailResolveSection.IndexOf('new List<', [StringComparison]::Ordinal) -lt 0) 'tail resolve-time List allocation is forbidden'
Require ($tailResolveSection.IndexOf('.OrderBy', [StringComparison]::Ordinal) -lt 0) 'tail resolve-time LINQ ordering is forbidden'
Require ($tailResolveSection.IndexOf('.Select(', [StringComparison]::Ordinal) -lt 0) 'tail resolve-time LINQ projection is forbidden'
Require ($tailResolveSection.IndexOf('foreach', [StringComparison]::Ordinal) -lt 0) 'tail resolve-time foreach is forbidden'
Require ($tailResolveSection.IndexOf('IapwsIf97Reference.', [StringComparison]::Ordinal) -lt 0) 'direct IF97 in tail resolve-time helper section is forbidden'

Require-Contains $c4Tests '[Fact(Explicit = true)]' 'C4 tests'
Require-Contains $c4Tests 'Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus' 'C4 tests'
Require-Contains $c4Tests 'Rp1bC4_MeasuresOneIndependentR1LaneRun' 'C4 tests'
Require-Contains $c4Tests 'BitConverter.DoubleToInt64Bits' 'C4 semantic test'
Require-Contains $c4Tests 'private readonly struct TimingSample' 'C4 timing harness'
Require-Contains $c4Tests 'var samples = new TimingSample[MeasuredCallsPerRun];' 'C4 timing harness'
Require-Contains $c4Tests 'PrimeTimingHarness(candidate, r1Rows[0]);' 'C4 timing harness'
Require-Contains $c4Tests 'wholeRegionAllocatedBytes' 'C4 timing harness'
Require-Contains $c4Tests 'harnessAllocatedBytes' 'C4 timing harness'
Require-Contains $c4Tests 'ImmutableC2Fallback' 'C4 timing harness'
Require ($c4Tests.IndexOf('samples.Add(', [StringComparison]::Ordinal) -lt 0) 'timing harness must not heap-append samples'

$runner = 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-c4.cmd'
$adjudicator = 'eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1b-c4.ps1'
Require (Test-Path -LiteralPath $runner -PathType Leaf) 'C4 runner missing'
Require (Test-Path -LiteralPath $adjudicator -PathType Leaf) 'C4 adjudicator missing'
$runnerText = Read-All $runner
Require-Contains $runnerText 'call eng\ci-ordinary.cmd' 'C4 runner'
Require-Contains $runnerText '--no-build' 'C4 runner'
Require-Contains $runnerText '--explicit only' 'C4 runner'
Require-Contains $runnerText '--parallel none' 'C4 runner'
Require-Contains $runnerText 'Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus' 'C4 runner'
Require-Contains $runnerText 'Rp1bC4_MeasuresOneIndependentR1LaneRun' 'C4 runner'
Require-Contains $runnerText 'for /L %%R in (1,1,5)' 'C4 runner'

Write-Host 'C4 static contract audit: PASS-AS-AUTHORED' -ForegroundColor Green
exit 0
