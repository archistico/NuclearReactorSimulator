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
Write-Host "PRE-M11 TODREAS/KAZIMI DEEP REVIEW PASS 2"
Write-Host "============================================================"
Write-Host "Documentation/planning-only post-Plan-Amendment-2 literature/runtime audit."
Write-Host "This does not execute P1B, select P3 or authorize a second long."
Write-Host ""

$pass1='eng/frozen-evidence/ordinary/PreM11_TodreasKazimi_DeepReviewPass1_ValidatedSummary.txt'
Require-Text $pass1 'pre-m11-todreas-kazimi-deep-review-pass1-passes=True'
Require-Text $pass1 'six-mwe-load-reachability-demonstrated=True'
Require-Text $pass1 'six-mwe-stationarity-demonstrated=False'
Require-Text $pass1 'second-deep-pass-required=True'
Require-Text $pass1 'production-src-changed=False'
Require-Text $pass1 'pre-existing-tests-changed=False'

$amend='eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P2R_PlanAmendment2_ValidatedSummary.txt'
Require-Text $amend 'm10-final-replacement-long-closure-plan1-p2r-plan-amendment2-passes=True'
Require-Text $amend 'p1a-final-classification=INCONCLUSIVE'
Require-Text $amend 'six-mwe-load-reachability-demonstrated=True'
Require-Text $amend 'six-mwe-stationarity-demonstrated=False'
Require-Text $amend 'p2r-decision=PLAN-STOP-INCONCLUSIVE'
Require-Text $amend 'p3-w-authorized=False'
Require-Text $amend 'p3-r-authorized=False'
Require-Text $amend 'second-replacement-long-authorized=False'
Require-Text $amend 'deep-review-pass2-required-before-p1b=True'
Require-Text $amend 'next-authorized-activity=Todreas-Kazimi-Deep-Review-Pass2'

$deep='docs/research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md'
Require-Text $deep 'PASS-AS-AUTHORED'
Require-Text $deep 'Tier A observable map'
Require-Text $deep 'No single scalar cross-domain owner score is enabled in P1B v1.'
Require-Text $deep '1 s data as slow-state downsampling'
Require-Text $deep 'use the existing `SecondaryCycleHeatBalanceAudit` ledger'
Require-Text $deep 'P2R2 Decision Re-entry 2'
Require-Text $deep '**Mandatory check 10 disposition:** `PASS`.'
Require-Text $deep 'No Plan Amendment 2 engineering hotfix is required.'

$trace='docs/research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS2.md'
Require-Text $trace 'TKP2-A01'
Require-Text $trace 'TKP2-A15'
Require-Text $trace 'TKP2-T03'
Require-Text $trace 'TKP2-R02'
Require-Text $trace 'TKP2-X04'
Require-Text $trace 'TKP2-G01'

$plan='docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md'
Require-Text $plan 'P1B Slow-State Closure & Phenomenon-Owner Qualification'
Require-Text $plan 'Deep Review Pass 2'
Require-Text $plan 'P2R2 Decision Re-entry 2'

Require-Text 'docs/PROJECT.md' 'Todreas/Kazimi Deep Review Pass 2'
Require-Text 'docs/PROJECT.md' 'PASS-AS-AUTHORED'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'Deep Review Pass 2'
Require-Text 'docs/ROADMAP.md' 'P1B Slow-State Closure & Phenomenon-Owner Qualification'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'Deep Review Pass 2'
Require-Text 'docs/README.md' 'PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md'
Require-Text 'docs/TOP_LEVEL_DOCUMENT_INDEX.md' 'PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS2.md'

$contractPath='eng/pre-m11-todreas-kazimi-deep-review-pass2-contract.json'
Require-File $contractPath
$c=Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($c.schema -ne 'pre-m11-todreas-kazimi-deep-review-pass2-v1') { throw 'Unexpected Pass 2 contract schema.' }
if ($c.documentationPlanningOnly -ne $true) { throw 'Pass 2 must remain documentation/planning only.' }
if ($c.baseline -ne 'P2R-PLAN-AMENDMENT2-HOTFIX1-VALIDATED') { throw 'Unexpected Pass 2 baseline.' }
if ($c.deepReviewPass1Validated -ne $true -or $c.planAmendment2Validated -ne $true) { throw 'Validated prerequisites missing.' }
if ($c.p1aFinalClassification -ne 'INCONCLUSIVE') { throw 'P1A classification mismatch.' }
if ($c.sixMweLoadReachabilityDemonstrated -ne $true -or $c.sixMweStationarityDemonstrated -ne $false) { throw '6 MWe evidence interpretation mismatch.' }
if ($c.disposition -ne 'PASS-AS-AUTHORED') { throw 'Pass 2 disposition mismatch.' }
if ($c.amendmentHotfixRequired -ne $false -or $c.planStopRequired -ne $false) { throw 'Unexpected amendment hotfix/plan stop disposition.' }
if ($c.mandatoryAuditPointCount -ne 10 -or $c.mandatoryAuditPointsPassed -ne 10) { throw 'All ten mandatory audit points must pass.' }
foreach ($name in @('tierAObservableMapSupported','tierCPhysicsExcluded','timingWindowsAreProjectModelWindowsNotPrototypeConstants','oneSecondSamplingIsSlowStateDownsample','perStepProtectionAndNumericalSentinelAuditRequired','inventoryTransferEvidencePrecedesDownstreamOwnerInference','controllerMemorySeparatedFromPhysicalActuator','physicalVsNumericalDriftSeparationRequired','rawPhysicalUnitEvidenceRequired','p1bImplementationAuthorizedAfterPass2Validation')) {
    if ($c.$name -ne $true) { throw ("Required Pass 2 safeguard missing: {0}" -f $name) }
}
foreach ($name in @('hydraulicDiagnosticCreatesSecondSolve','normalizedCrossDomainOwnerScoreEnabled','newConstitutivePhysicsImported','literatureCorrelationOrCoefficientImported','noMaterialLateDriftMeansStationarity','p1bImplementationAuthorizedNow','p3WAuthorized','p3RAuthorized','secondReplacementLongAuthorized','productionSrcChanged','preExistingTestsChanged','exactV9Changed','replacementWorkloadChanged','authorityPolicyChanged','generatorLoadSemanticsChanged','protectionSemanticsChanged','missionPackChanged')) {
    if ($c.$name -ne $false) { throw ("Forbidden change/authorization: {0}" -f $name) }
}
if ($c.nextAuthorizedActivityAfterValidation -ne 'P1B-Implementation') { throw 'Unexpected next activity.' }
if ($c.nextDecisionGateAfterP1B -ne 'P2R2-Decision-Reentry-2') { throw 'Unexpected next decision gate.' }

$artifactDir='artifacts/pre-m11-todreas-kazimi-deep-review-pass2'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
@(
  'pre-m11-todreas-kazimi-deep-review-pass2-passes=True',
  'documentation-planning-only=True',
  'baseline=P2R-PLAN-AMENDMENT2-HOTFIX1-VALIDATED',
  'deep-review-pass1-validated=True',
  'plan-amendment2-validated=True',
  'p1a-final-classification=INCONCLUSIVE',
  'six-mwe-load-reachability-demonstrated=True',
  'six-mwe-stationarity-demonstrated=False',
  'pass2-disposition=PASS-AS-AUTHORED',
  'mandatory-audit-points-passed=10/10',
  'normalized-cross-domain-owner-score-enabled=False',
  'one-second-sampling-is-slow-state-downsample=True',
  'per-step-protection-and-numerical-sentinel-audit-required=True',
  'new-constitutive-physics-imported=False',
  'production-src-changed=False',
  'pre-existing-tests-changed=False',
  'p3-w-authorized=False',
  'p3-r-authorized=False',
  'second-replacement-long-authorized=False',
  'next-authorized-activity=P1B-Implementation',
  'next-decision-gate-after-p1b=P2R2-Decision-Reentry-2'
) | Set-Content -LiteralPath (Join-Path $artifactDir '01-pass2-summary.txt') -Encoding UTF8

Write-Host 'Pre-M11 Todreas/Kazimi Deep Review Pass 2 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
