$ErrorActionPreference = 'Stop'

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    Require-File $Path
    $text = Get-Content -LiteralPath $Path -Raw
    if (-not $text.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "============================================================"
Write-Host "PRE-M11 PLANT DYNAMICS / TH / REACTOR PHYSICS REVIEW 1"
Write-Host "============================================================"
Write-Host "Documentation/planning-only audit. P1A remains executable"
Write-Host "and unchanged; this audit does not run or replace P1A."
Write-Host ""

$review = 'docs/PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md'
Require-Text $review 'P1A remains frozen'
Require-Text $review 'P2R interpretation checklist'
Require-Text $review 'global point kinetics is not space-time/full-core kinetics'
Require-Text $review 'Carryover, carry-under'
Require-Text $review 'A sustained load change is an energy-balance problem for the whole unit'

$sources = 'docs/research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md'
Require-Text $sources 'Power Plant Instrumentation and Control Handbook'
Require-Text $sources 'Power Engineering: Advances and Challenges'
Require-Text $sources 'An Introduction to the Engineering of Fast Nuclear Reactors'
Require-Text $sources 'Steam Generators for Nuclear Power Plants'
Require-Text $sources 'Applied Reactor Physics'

Require-Text 'docs/PRE_M11_ENGINEERING_REVIEW_CONSOLIDATION.md' 'Plant dynamics, thermal-hydraulics and reactor physics'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'Parallel literature-review constraint for P1A / P2R'
Require-Text 'docs/REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md' 'Additional reviewed-source reinforcement'
Require-Text 'docs/KNOWN_MODEL_LIMITATIONS.md' 'industrial coordinated unit-load/direct-energy-balance controller'
Require-Text 'docs/PROJECT.md' 'P1A ASYMPTOTIC CLOSURE EXTENSION CANDIDATE'
Require-Text 'docs/PROJECT.md' 'five additional sources on coordinated plant control/flexibility'
Require-Text 'docs/README.md' 'PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md'
Require-Text 'docs/TOP_LEVEL_DOCUMENT_INDEX.md' 'PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md'

$contractPath = 'eng/pre-m11-plant-dynamics-reference-review1-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($contract.schema -ne 'pre-m11-plant-dynamics-reference-review1-v1') { throw 'Unexpected contract schema.' }
if ($contract.p1aContractChanged -ne $false) { throw 'P1A contract must remain unchanged.' }
if ($contract.productionSrcChanged -ne $false) { throw 'Production src change is forbidden in this review.' }
if ($contract.preExistingTestsChanged -ne $false) { throw 'Pre-existing test change is forbidden in this review.' }
if ($contract.p3BranchAuthorized -ne $false) { throw 'P3 must remain unauthorized.' }
if ($contract.secondReplacementLongAuthorized -ne $false) { throw 'Second replacement-long must remain unauthorized.' }
if ($contract.nextExecutableGate -ne 'P1A-Asymptotic-Closure-Extension') { throw 'P1A must remain the next executable gate.' }
if ($contract.nextDecisionGateAfterP1A -ne 'P2R-Decision-Re-entry') { throw 'P2R must remain the next decision gate after P1A.' }
if ($contract.reviewedSources.Count -ne 5) { throw 'Exactly five Review-1 sources must be recorded.' }

$artifactDir = 'artifacts/pre-m11-plant-dynamics-reference-review1'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$summary = @(
    'pre-m11-plant-dynamics-reference-review1-passes=True',
    'documentation-planning-only=True',
    'p1a-contract-changed=False',
    'production-src-changed=False',
    'pre-existing-tests-changed=False',
    'p3-branch-authorized=False',
    'second-replacement-long-authorized=False',
    'reviewed-source-count=5',
    'next-executable-gate=P1A-Asymptotic-Closure-Extension',
    'next-decision-gate-after-p1a=P2R-Decision-Re-entry'
)
$summary | Set-Content -LiteralPath (Join-Path $artifactDir '01-review-summary.txt') -Encoding UTF8

Write-Host 'Pre-M11 Plant Dynamics / TH / Reactor Physics Review 1 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
