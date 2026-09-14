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
Write-Host "PRE-M11 DEEP ENGINEERING SECTION REVIEW 2"
Write-Host "============================================================"
Write-Host "Documentation/planning-only audit. P1A remains executable"
Write-Host "and unchanged; this audit does not run or replace P1A."
Write-Host ""

$deep = 'docs/research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md'
Require-Text $deep 'Basu & Debnath'
Require-Text $deep 'Riznic'
Require-Text $deep 'Badescu, Lazaroiu & Barelli'
Require-Text $deep 'Judd'
Require-Text $deep 'Hébert'
Require-Text $deep 'P1A remains frozen'
Require-Text $deep 'GPT is a specific adjoint sensitivity method'
Require-Text $deep 'carry-under'
Require-Text $deep 'requested demand'
Require-Text $deep 'homogenization should preserve selected physics'

$trace = 'docs/research/PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md'
Require-Text $trace 'BD-CCS-02'
Require-Text $trace 'RZ-SEP-01'
Require-Text $trace 'BA-HEAD-01'
Require-Text $trace 'JU-ROD-01'
Require-Text $trace 'HE-SPH-01'

Require-Text 'docs/PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md' 'Detailed-section re-review — Review 2 disposition'
Require-Text 'docs/research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md' 'Detailed-section Review 2 coverage'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'including detailed-section Review 2'
Require-Text 'docs/REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md' 'Deep-section Review 2 refinement'
Require-Text 'docs/KNOWN_MODEL_LIMITATIONS.md' 'carry-under can lower returning-liquid density'
Require-Text 'docs/ROADMAP.md' 'any homogenized/coarse reduction must declare'
Require-Text 'docs/milestones/M14.md' 'Deep-section Review 2 physics boundary'
Require-Text 'docs/PROJECT.md' 'Reviews 1–2 now record and deeply re-check five additional sources'
Require-Text 'docs/README.md' 'PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md'
Require-Text 'docs/TOP_LEVEL_DOCUMENT_INDEX.md' 'PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md'

$contractPath = 'eng/pre-m11-deep-engineering-section-review2-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($contract.schema -ne 'pre-m11-deep-engineering-section-review2-v1') { throw 'Unexpected contract schema.' }
if ($contract.p1aContractChanged -ne $false) { throw 'P1A contract must remain unchanged.' }
if ($contract.productionSrcChanged -ne $false) { throw 'Production src change is forbidden in this review.' }
if ($contract.preExistingTestsChanged -ne $false) { throw 'Pre-existing test change is forbidden in this review.' }
if ($contract.reviewedSourceCount -ne 5) { throw 'Exactly five source books must be retained.' }
if ($contract.reviewScope -ne 'previously-selected-deep-study-sections-only') { throw 'Review scope widened unexpectedly.' }
if ($contract.p3BranchAuthorized -ne $false) { throw 'P3 must remain unauthorized.' }
if ($contract.secondReplacementLongAuthorized -ne $false) { throw 'Second replacement-long must remain unauthorized.' }
if ($contract.nextExecutableGate -ne 'P1A-Asymptotic-Closure-Extension') { throw 'P1A must remain the next executable gate.' }
if ($contract.nextDecisionGateAfterP1A -ne 'P2R-Decision-Re-entry') { throw 'P2R must remain the next decision gate after P1A.' }

$artifactDir = 'artifacts/pre-m11-deep-engineering-section-review2'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$summary = @(
    'pre-m11-deep-engineering-section-review2-passes=True',
    'documentation-planning-only=True',
    'deep-study-source-count=5',
    'review-scope=previously-selected-deep-study-sections-only',
    'p1a-contract-changed=False',
    'production-src-changed=False',
    'pre-existing-tests-changed=False',
    'p3-branch-authorized=False',
    'second-replacement-long-authorized=False',
    'next-executable-gate=P1A-Asymptotic-Closure-Extension',
    'next-decision-gate-after-p1a=P2R-Decision-Re-entry'
)
$summary | Set-Content -LiteralPath (Join-Path $artifactDir '01-review-summary.txt') -Encoding UTF8

Write-Host 'Pre-M11 Deep Engineering Section Review 2 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
