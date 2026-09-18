$ErrorActionPreference = 'Stop'

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}
function Require-Text([string]$Path, [string]$Needle) {
    Require-File $Path
    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if (-not $text.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "============================================================"
Write-Host "PRE-M11 TODREAS/KAZIMI DEEP REVIEW PASS 1"
Write-Host "============================================================"
Write-Host "Documentation/planning-only pre-Plan-Amendment-2 audit."
Write-Host "No runtime/workload/P3/second-long authorization is created."
Write-Host ""

$deep = 'docs/research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md'
Require-Text $deep 'Deep Review Pass 1: COMPLETE FOR PRE-AMENDMENT PLANNING.'
Require-Text $deep 'LOAD REACHABILITY AT 6 MWe'
Require-Text $deep 'WHOLE-OPERATING-POINT STATIONARITY AT 6 MWe'
Require-Text $deep 'reduced HEM-like physics'
Require-Text $deep 'root / stationarity closure'
Require-Text $deep 'Candidate observable inventory for Plan Amendment 2'
Require-Text $deep 'not yet frozen'
Require-Text $deep 'second Todreas/Kazimi deep review is mandatory after Plan Amendment 2 is authored'
Require-Text $deep 'Do not duplicate constitutive physics in the diagnostic layer.'

$trace='docs/research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md'
Require-Text $trace 'TK1-HEM-01'
Require-Text $trace 'TK1-LED-01'
Require-Text $trace 'TK2-PLEN-01'
Require-Text $trace 'TK2-CV-01'
Require-Text $trace 'TK2-SENS-01'

Require-Text 'docs/research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md' '## 9. Reactor thermal-hydraulic fundamentals'
Require-Text 'docs/research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md' '## 10. Reactor thermal-hydraulic design'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'P1A has now completed execution PASS with overall classification `INCONCLUSIVE`'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'Deep Review Pass 2'
Require-Text 'docs/REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md' 'Todreas/Kazimi Deep Review Pass 1'
Require-Text 'docs/KNOWN_MODEL_LIMITATIONS.md' 'reduced HEM-like fidelity'
Require-Text 'docs/ROADMAP.md' 'Todreas/Kazimi thermal-hydraulic deep-review additions'
Require-Text 'docs/milestones/M14.md' 'Todreas/Kazimi hydraulic reduction boundary'
# PROJECT.md is the current-state handoff and legitimately advances after the pre-amendment review.
# Validate durable provenance instead of requiring the superseded pre-amendment heading.
Require-Text 'docs/PROJECT.md' 'Todreas/Kazimi Deep Review Pass 1'
Require-Text 'docs/PROJECT.md' 'Todreas/Kazimi Deep Review Pass 2'
Require-Text 'docs/README.md' 'PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md'
Require-Text 'docs/TOP_LEVEL_DOCUMENT_INDEX.md' 'PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md'

$contractPath='eng/pre-m11-todreas-kazimi-deep-review-pass1-contract.json'
Require-File $contractPath
$c=Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($c.schema -ne 'pre-m11-todreas-kazimi-deep-review-pass1-v1') { throw 'Unexpected contract schema.' }
if ($c.documentationPlanningOnly -ne $true) { throw 'Review must remain documentation/planning only.' }
if ($c.reviewedNewSourceCount -ne 2) { throw 'Exactly two new source volumes expected.' }
if ($c.reviewPhase -ne 'pre-plan-amendment-2') { throw 'Unexpected review phase.' }
if ($c.p1aFinalClassification -ne 'INCONCLUSIVE') { throw 'P1A final classification mismatch.' }
if ($c.sixMweLoadReachabilityDemonstrated -ne $true) { throw '6 MWe reachability evidence must be recorded.' }
if ($c.sixMweStationarityDemonstrated -ne $false) { throw '6 MWe stationarity must remain unproven.' }
if ($c.planAmendment2Frozen -ne $false) { throw 'This review must not freeze Plan Amendment 2.' }
if ($c.secondDeepPassRequiredAfterAmendment2 -ne $true) { throw 'Pass 2 must remain mandatory.' }
foreach ($name in @('productionSrcChanged','preExistingTestsChanged','p1aContractChanged','exactV9Changed','replacementWorkloadChanged','authorityPolicyChanged','generatorLoadSemanticsChanged','protectionSemanticsChanged','missionPackChanged','p3BranchAuthorized','secondReplacementLongAuthorized')) {
    if ($c.$name -ne $false) { throw ("Forbidden change/authorization: {0}" -f $name) }
}

$artifactDir='artifacts/pre-m11-todreas-kazimi-deep-review-pass1'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
@(
  'pre-m11-todreas-kazimi-deep-review-pass1-passes=True',
  'documentation-planning-only=True',
  'review-phase=pre-plan-amendment-2',
  'new-source-volume-count=2',
  'p1a-final-classification=INCONCLUSIVE',
  'six-mwe-load-reachability-demonstrated=True',
  'six-mwe-stationarity-demonstrated=False',
  'plan-amendment-2-frozen=False',
  'second-deep-pass-required=True',
  'production-src-changed=False',
  'pre-existing-tests-changed=False',
  'p3-branch-authorized=False',
  'second-replacement-long-authorized=False',
  'next-planning-action=Plan-Amendment-2-authoring'
) | Set-Content -LiteralPath (Join-Path $artifactDir '01-review-summary.txt') -Encoding UTF8

Write-Host 'Pre-M11 Todreas/Kazimi Deep Review Pass 1 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
