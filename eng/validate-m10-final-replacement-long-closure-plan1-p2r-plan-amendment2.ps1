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
Write-Host "M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P2R / PLAN AMENDMENT 2"
Write-Host "============================================================"
Write-Host "Documentation/planning-only decision and amendment audit."
Write-Host "This does not implement P1B, select P3 or freeze a second long baseline."
Write-Host ""

$doc='docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md'
Require-Text $doc 'P2R-DECISION = PLAN-STOP-INCONCLUSIVE'
Require-Text $doc '6 MWe LOAD REACHABILITY = DEMONSTRATED'
Require-Text $doc '6 MWe WHOLE-OPERATING-POINT STATIONARITY = NOT DEMONSTRATED'
Require-Text $doc 'P1B Slow-State Closure & Phenomenon-Owner Qualification'
Require-Text $doc '5 MWe background-reference observation of 600 s'
Require-Text $doc 'no hold beyond 3,600 s after the 6 MWe load command'
Require-Text $doc '1,200 s'
Require-Text $doc 'four contiguous **300 s** windows'
Require-Text $doc 'Tier C'
Require-Text $doc 'forbidden to synthesize in P1B'
Require-Text $doc 'Deep Review Pass 2'
Require-Text $doc 'P2R2 Decision Re-entry 2'

$p1a='eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1A_DecisionSummary.txt'
Require-Text $p1a 'p1a-final-classification=INCONCLUSIVE'
Require-Text $p1a 'exact-v9-5p5-classification=CONVERGED'
Require-Text $p1a 'exact-v9-6-classification=INCONCLUSIVE'
Require-Text $p1a 'p1-checkpoints-reproduced=True'
Require-Text $p1a 'p3-w-authorized=False;p3-r-authorized=False;second-replacement-long-authorized=False'
Require-Text $p1a 'm10-final-replacement-long-closure-plan1-p1a-passes=True'

Require-File 'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1A_ProbeSummary.csv'
Require-File 'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1A_Events.csv'
Require-File 'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1A_FrozenP1Calibration.csv'

Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'Plan Amendment 2'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'P1B Slow-State Closure & Phenomenon-Owner Qualification'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'Todreas/Kazimi Deep Review Pass 2'
Require-Text 'docs/PROJECT.md' 'P2R DECISION RE-ENTRY 1 / PLAN AMENDMENT 2'
Require-Text 'docs/PROJECT.md' 'Todreas/Kazimi Deep Review Pass 2'
Require-Text 'docs/ROADMAP.md' 'P1B Slow-State Closure & Phenomenon-Owner Qualification'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'P2R1 records PLAN-STOP-INCONCLUSIVE'
Require-Text 'docs/README.md' 'M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md'

$contractPath='eng/m10-final-replacement-long-closure-plan1-p2r-plan-amendment2-contract.json'
Require-File $contractPath
$c=Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($c.schema -ne 'm10-final-replacement-long-closure-plan1-p2r-plan-amendment2-v1') { throw 'Unexpected contract schema.' }
if ($c.documentationPlanningOnly -ne $true) { throw 'Plan Amendment 2 gate must remain documentation/planning only.' }
if ($c.p1aExecutionPass -ne $true -or $c.p1aFinalClassification -ne 'INCONCLUSIVE') { throw 'P1A evidence mismatch.' }
if ($c.sixMweLoadReachabilityDemonstrated -ne $true -or $c.sixMweStationarityDemonstrated -ne $false) { throw '6 MWe evidence interpretation mismatch.' }
if ($c.p2rDecision -ne 'PLAN-STOP-INCONCLUSIVE') { throw 'P2R must remain a planning stop.' }
if ($c.planAmendment2 -ne 'P1B-SLOW-STATE-CLOSURE-PHENOMENON-OWNER-QUALIFICATION') { throw 'Unexpected Plan Amendment 2 id.' }
if ($c.p1b.backgroundReferenceSeconds -ne 600) { throw 'Unexpected P1B background-reference duration.' }
if ($c.p1b.maximumHoldSecondsAfterLoad -ne 3600) { throw 'P1B may not extend beyond the P1A horizon.' }
if ($c.p1b.trajectorySampleSeconds -ne 1 -or $c.p1b.lateAnalysisSeconds -ne 1200 -or $c.p1b.lateSubwindowSeconds -ne 300) { throw 'Unexpected P1B observation windows.' }
if ($c.p1b.expectedLoadCommandLogicalStep -ne 2785) { throw 'Unexpected P1A load-command checkpoint.' }
if ($c.p1b.forbidNewConstitutivePhysics -ne $true -or $c.p1b.forbidBlindHoldExtension -ne $true -or $c.p1b.forbidDirectP3Selection -ne $true) { throw 'P1B governance safeguards missing.' }
if ($c.deepReviewPass2RequiredBeforeP1BImplementation -ne $true -or $c.p1bImplementationAuthorized -ne $false) { throw 'Deep Review Pass 2 hold must remain active.' }
foreach ($name in @('productionSrcChanged','preExistingTestsChanged','p1aContractChanged','exactV9Changed','replacementWorkloadChanged','authorityPolicyChanged','generatorLoadSemanticsChanged','protectionSemanticsChanged','missionPackChanged','p3WAuthorized','p3RAuthorized','secondReplacementLongAuthorized')) {
    if ($c.$name -ne $false) { throw ("Forbidden change/authorization: {0}" -f $name) }
}

$artifactDir='artifacts/m10-final-replacement-long-closure-plan1-p2r-plan-amendment2'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
@(
  'm10-final-replacement-long-closure-plan1-p2r-plan-amendment2-passes=True',
  'documentation-planning-only=True',
  'p1a-execution-pass=True',
  'p1a-final-classification=INCONCLUSIVE',
  'six-mwe-load-reachability-demonstrated=True',
  'six-mwe-stationarity-demonstrated=False',
  'p2r-decision=PLAN-STOP-INCONCLUSIVE',
  'p3-w-authorized=False',
  'p3-r-authorized=False',
  'second-replacement-long-authorized=False',
  'plan-amendment-2=P1B-SLOW-STATE-CLOSURE-PHENOMENON-OWNER-QUALIFICATION',
  'p1b-implementation-authorized=False',
  'p1b-background-reference-seconds=600',
  'p1b-max-hold-after-load-seconds=3600',
  'p1b-blind-extension-authorized=False',
  'deep-review-pass2-required-before-p1b=True',
  'production-src-changed=False',
  'pre-existing-tests-changed=False',
  'next-authorized-activity=Todreas-Kazimi-Deep-Review-Pass2',
  'next-decision-gate-after-p1b=P2R2-Decision-Reentry-2'
) | Set-Content -LiteralPath (Join-Path $artifactDir '01-p2r-plan-amendment2-summary.txt') -Encoding UTF8

Write-Host 'M10 Final Replacement-Long Closure Plan 1 P2R / Plan Amendment 2 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
