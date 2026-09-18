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

Write-Host '============================================================'
Write-Host 'M10 FINAL - PLAN AMENDMENT 3 / PHYSICAL REFERENCE ASSESSMENT'
Write-Host '============================================================'
Write-Host 'Documentation/planning-only audit. No production repair, workload'
Write-Host 'change, exact-v9 change, P3-W selection or second-long freeze.'
Write-Host ''

$summary = 'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P2R2_ValidatedSummary.txt'
Require-Text $summary 'm10-final-replacement-long-closure-plan1-p2r2-passes=True'
Require-Text $summary 'p2r2-decision=P3-R-OWNER-LOCALIZATION'
Require-Text $summary 'production-repair-authorized=False'
Require-Text $summary 'second-replacement-long-authorized=False'

Require-Text 'docs/PROJECT.md' 'PLAN AMENDMENT 3'
Require-Text 'docs/PROJECT.md' 'P2R2 Decision Re-entry 2 is now'
Require-Text 'docs/M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md' 'VR0 Reference/Provenance Contract Freeze'
Require-Text 'docs/M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md' 'PROCEED-P3R1-EXACTV9'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'IAPWS-IF97'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'ANS-5.1'
Require-Text 'docs/M10_FINAL_MODEL_ASSESSMENT_CLAIM_POLICY.md' 'MODEL-ASSESSED'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_PLAN_AMENDMENT3_EXTERNAL_MODEL_ASSESSMENT.md' 'P3-R1 execution is temporarily held'
Require-Text 'docs/M10_FINAL_P3R1_TO_P6_DETAILED_GATE_MATRIX.md' 'P5B | does replacement-long 2'
Require-Text 'docs/POST_M10_REPOSITORY_RELEASE_PLAYABLE_SLICE_PLAN.md' 'cold shutdown'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'Plan Amendment 3'
Require-Text 'docs/ROADMAP.md' 'Physical Reference Model Assessment'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'external physical-reference assessment hold'

$contractPath = 'eng/m10-final-plan-amendment3-physical-reference-assessment-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contract.planning_only -ne $true) { throw 'Contract must remain planning_only=true.' }
if ($contract.p3r1_temporarily_held -ne $true) { throw 'P3-R1 must be held until VR5.' }
if ($contract.production_repair_authorized -ne $false) { throw 'Production repair must remain unauthorized.' }
if ($contract.exact_v9_change_authorized -ne $false) { throw 'exact-v9 must remain immutable.' }
if ($contract.second_replacement_long_authorized -ne $false) { throw 'Second replacement-long must remain unauthorized.' }
if ($contract.next_authorized_gate -ne 'VR0-Reference-Provenance-Contract-Freeze') { throw 'Unexpected next authorized gate.' }

$artifactDir = 'artifacts/m10-final-plan-amendment3-physical-reference-assessment'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$summaryOut = Join-Path $artifactDir '01-plan-amendment3-summary.txt'
@(
    'm10-final-plan-amendment3-physical-reference-assessment-passes=True',
    'documentation-planning-only=True',
    'p2r2-validated=True',
    'p2r2-decision=P3-R-OWNER-LOCALIZATION',
    'p3r1-temporarily-held=True',
    'physical-reference-assessment-required=True',
    'production-repair-authorized=False',
    'p3-w-authorized=False',
    'exact-v9-change-authorized=False',
    'replacement-workload-change-authorized=False',
    'second-replacement-long-authorized=False',
    'next-authorized-gate=VR0-Reference-Provenance-Contract-Freeze'
) | Set-Content -LiteralPath $summaryOut -Encoding UTF8

Write-Host 'M10 Final Plan Amendment 3 / Physical Reference Assessment audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
