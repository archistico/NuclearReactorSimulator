param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Pre-M11 engineering review consolidation validation failed: $Message" }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }

$requiredDocs = @(
    'docs\PRE_M11_ENGINEERING_REVIEW_CONSOLIDATION.md',
    'docs\PRE_M11_NUCLEAR_CODE_VV_REVIEW.md',
    'docs\PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md',
    'docs\DIGITAL_IC_ARCHITECTURE_INVARIANTS.md',
    'docs\HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md',
    'docs\DIGITAL_IC_HAZARD_CATALOG.md',
    'docs\HMI_CLASSIC_FAILURE_MODES_CHECKLIST.md',
    'docs\M11_COTS_DEPENDENCY_ASSURANCE_PLAN.md',
    'docs\M11_PLUS_DIGITAL_IC_BACKLOG.md',
    'docs\PRE_M11_IMPLEMENTATION_DECISIONS.md',
    'docs\M11_DIGITAL_IC_RELEASE_ASSURANCE_PLAN.md',
    'docs\M11_RELEASE_EVIDENCE_MATRIX_PLAN.md',
    'docs\CHANGE_IMPACT_REVALIDATION_POLICY.md',
    'docs\POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md',
    'docs\REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md',
    'docs\M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md',
    'docs\M13_DIGITAL_IC_DEGRADATION_AUTOMATION_TRANSPARENCY_PLAN.md',
    'docs\research\PRE_M11_ENGINEERING_REVIEW_SOURCES.md',
    'docs\research\LAMARSH_FOLLOW_UP_CANDIDATES.md'
)
foreach ($relative in $requiredDocs) {
    Require (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf) "missing required document: $relative"
}

$requiredJson = @(
    'eng\pre-m11-engineering-review-consolidation-contract.json',
    'eng\pre-m11-engineering-review-source-map.json',
    'eng\pre-m11-digital-ic-hazard-catalog.json',
    'eng\m11-digital-ic-implementation-map.json',
    'eng\post-m10-execution-plan.json',
    'eng\reference-operating-point-equilibrium-plan.json'
)
foreach ($relative in $requiredJson) {
    $path = Join-Path $RepositoryRoot $relative
    Require (Test-Path -LiteralPath $path -PathType Leaf) "missing required JSON: $relative"
    $null = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}


$contract = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'eng\pre-m11-engineering-review-consolidation-contract.json') -Raw | ConvertFrom-Json
Require ($contract.schema -eq 'pre-m11-engineering-review-consolidation-v1') 'consolidation contract schema changed.'
Require ($contract.source_review_streams -eq 3) 'consolidation contract must retain three review streams.'
Require ($contract.production_src_changed -eq $false) 'planning consolidation may not change production src.'
Require ($contract.tests_changed_relative_to_long_candidate -eq $false) 'planning consolidation may not change long-candidate tests.'
Require ($contract.long_validation_contract_changed -eq $false) 'frozen long contract may not change in planning consolidation.'
Require ($contract.long_acceptance_thresholds_changed -eq $false) 'frozen long acceptance thresholds may not change.'
Require ($contract.current_long_evidence.lr_h1_status -eq 'FAILED') 'current LR-H1 failure record is missing.'
Require ($contract.current_long_evidence.cause_status -eq 'UNCLASSIFIED-AWAITING-COMPLETE-ARTIFACTS') 'LR-H1 cause must remain unclassified until complete artifacts are analyzed.'

$sourceMap = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'eng\pre-m11-engineering-review-source-map.json') -Raw | ConvertFrom-Json
Require ($sourceMap.schema -eq 1) 'source-map schema changed.'
Require ($sourceMap.id -eq 'pre-m11-engineering-review-source-map-v1') 'source-map id changed.'
Require (@($sourceMap.sources).Count -eq 3) 'exactly three source-driven review streams are required.'
Require ($sourceMap.governance.noDirectPlantSpecificConstantImport -eq $true) 'direct plant-specific constant import must remain prohibited.'
Require ($sourceMap.governance.noLicensingGradeClaim -eq $true) 'licensing-grade claim guard changed.'

$hazards = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'eng\pre-m11-digital-ic-hazard-catalog.json') -Raw | ConvertFrom-Json
Require (@($hazards.hazards).Count -eq 27) 'Digital I&C hazard catalog must retain 27 reviewed hazards.'

$projectText = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'docs\PROJECT.md') -Raw
Require ($projectText.Contains('M10 remains OPEN')) 'PROJECT must keep M10 open until long PASS and explicit closure.'
Require ($projectText.Contains('LR-H1 is already RED')) 'PROJECT must record the current LR-H1 failure evidence.'
Require ($projectText.Contains('planning only')) 'PROJECT must distinguish planning overlay from promotion evidence.'

$m12Text = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'docs\milestones\M12.md') -Raw
Require ($m12Text.Contains('M12.0 — Reference Operating-Point Equilibrium & Stability Qualification')) 'M12.0 equilibrium qualification is missing.'
Require ($m12Text.Contains('Existing `integrated-operations-desktop-stable@4` remains immutable')) 'exact-v4 immutability rule is missing from M12.0.'

$m13Text = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'docs\milestones\M13.md') -Raw
Require ($m13Text.Contains('M13.9 — Digital I&C Degradation & Automation Transparency')) 'M13.9 Digital I&C slice is missing.'
Require ($m13Text.Contains('M13.10')) 'M13.10 integrated UX closure renumbering is missing.'

$roadmapText = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'docs\ROADMAP.md') -Raw
Require ($roadmapText.Contains('M12.0 — **Reference Operating-Point Equilibrium & Stability Qualification**')) 'ROADMAP does not expose M12.0.'
Require ($roadmapText.Contains('M13.9 — **Digital I&C Degradation & Automation Transparency**')) 'ROADMAP does not expose M13.9.'
Require ($roadmapText.Contains('M13.10 — integrated keyboard/minimum-window/replay/session UX closure')) 'ROADMAP does not expose M13.10.'

$longValidator = Join-Path $RepositoryRoot 'eng\validate-m10-final-long-validation-contract.ps1'
Require (Test-Path -LiteralPath $longValidator -PathType Leaf) 'authoritative long-contract validator is missing.'
& $longValidator -RepositoryRoot $RepositoryRoot

Write-Host 'pre-m11-engineering-review-consolidation-passes=True'
Write-Host 'pre-m11-engineering-review-source-streams=3'
Write-Host 'pre-m11-digital-ic-hazards=27'
Write-Host 'pre-m11-m11-feature-freeze-preserved=True'
Write-Host 'pre-m11-m12-equilibrium-plan-present=True'
Write-Host 'pre-m11-m13-digital-ic-plan-present=True'
Write-Host 'pre-m11-production-src-unchanged=True'
Write-Host 'pre-m11-test-surface-unchanged-relative-to-long-candidate=True'
Write-Host 'm10-closure-still-blocked-by-long=True'
