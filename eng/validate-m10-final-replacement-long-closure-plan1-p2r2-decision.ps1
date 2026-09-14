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
Write-Host "M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P2R2 DECISION RE-ENTRY 2"
Write-Host "============================================================"
Write-Host "Documentation/planning-only branch decision audit."
Write-Host "This does not implement P3-R1, change production runtime/workload,"
Write-Host "authorize a production repair or freeze a second replacement long."
Write-Host ""

$doc='docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md'
Require-Text $doc 'P1B execution PASS'
Require-Text $doc 'COUPLED-MULTI-DOMAIN'
Require-Text $doc '6 MWe load reachability'
Require-Text $doc 'P2R2-DECISION = P3-R-OWNER-LOCALIZATION'
Require-Text $doc 'P3-W-AUTHORIZED = False'
Require-Text $doc 'P3-R-OWNER-LOCALIZATION-AUTHORIZED = True'
Require-Text $doc 'PRODUCTION-REPAIR-AUTHORIZED = False'
Require-Text $doc 'NEXT-AUTHORIZED-IMPLEMENTATION = P3-R1-Primary-Inventory-Hydraulic-Slow-State-Owner-Localization'
Require-Text $doc 'P2R2 does not authorize a production repair'

$p1b='eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1B_ValidatedSummary.txt'
Require-Text $p1b 'p1b-owner-evidence-label=COUPLED-MULTI-DOMAIN'
Require-Text $p1b 'load-probe=execution-pass:True|load-command-step:2785|checkpoints:3/3|max-hold-seconds:3600'
Require-Text $p1b 'p3-w-authorized=False;p3-r-authorized=False;second-replacement-long-authorized=False'
Require-Text $p1b 'm10-final-replacement-long-closure-plan1-p1b-passes=True'
Require-Text $p1b 'active-domain=PRIMARY-INVENTORY-HYDRAULIC'
Require-Text $p1b 'active-domain=STEAM-PATH-TURBINE'
Require-Text $p1b 'active-domain=CONTROL-ACTUATOR-MEMORY'
Require-Text $p1b 'active-domain=ELECTROMECHANICAL-GRID'

$derived='eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1B_P2R2DerivedEvidence.txt'
Require-Text $derived 'load-reachability-at-3600s=output:6.000344232909117MWe'
Require-Text $derived 'thermofluid-stored-energy:+1.317398473MW'
Require-Text $derived 'outlet-mass:2283.995428424272->3141.048091688905kg'
Require-Text $derived 'feedwater-inventory:7621.349871824543->6565.187761476099kg'
Require-Text $derived 'p2r2-proposed-decision=P3-R-OWNER-LOCALIZATION'
Require-Text $derived 'production-repair-authorized=False'

$checkpoints='eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1B_CheckpointReproduction.csv'
Require-Text $checkpoints '900,92784,92784,True'
Require-Text $checkpoints '1800,182784,182784,True'
Require-Text $checkpoints '3600,362784,362784,True'

$sentinels='eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1B_Sentinels.csv'
Require-Text $sentinels 'steps=60000;trip=0;nonconverged=0;nonfinite=0;rollback=0;line-search-exhausted=0;untargeted-disagreement=0;shadow-nonconverged=0'
Require-Text $sentinels 'steps=362784;trip=0;nonconverged=0;nonfinite=0;rollback=0;line-search-exhausted=0;untargeted-disagreement=0;shadow-nonconverged=0'

Require-File 'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1B_LateWindowTrends.csv'
Require-Text 'eng/frozen-evidence/ordinary/PreM11_TodreasKazimi_DeepReviewPass2_ValidatedSummary.txt' 'disposition=PASS-AS-AUTHORED'
Require-Text 'docs/PROJECT.md' 'P2R2 DECISION RE-ENTRY 2 / P3-R OWNER-LOCALIZATION SELECTION'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md' 'P3-R1-Primary-Inventory-Hydraulic-Slow-State-Owner-Localization'
Require-Text 'docs/ROADMAP.md' 'P3-R1'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'P2R2'
Require-Text 'docs/M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md' 'P2R2'
Require-Text 'docs/README.md' 'M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md'

$contractPath='eng/m10-final-replacement-long-closure-plan1-p2r2-decision-contract.json'
Require-File $contractPath
$c=Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($c.ContractId -ne 'm10-final-replacement-long-closure-plan1-p2r2-decision-v1') { throw 'Unexpected P2R2 contract id.' }
if ($c.P1BExecutionPassRequired -ne $true) { throw 'P2R2 requires returned P1B PASS evidence.' }
if ($c.P1BExpectedEvidenceLabel -ne 'COUPLED-MULTI-DOMAIN') { throw 'Unexpected P1B evidence label.' }
if ($c.P1BRequiredCheckpointCount -ne 3 -or $c.P1BRequiredSentinelTripCount -ne 0) { throw 'Unexpected P1B prerequisite contract.' }
if ($c.P2R2Decision -ne 'P3-R-OWNER-LOCALIZATION') { throw 'Unexpected P2R2 branch decision.' }
if ($c.P3WAuthorized -ne $false) { throw 'P3-W must remain unauthorized.' }
if ($c.P3RAuthorizedForOwnerLocalization -ne $true) { throw 'P3-R owner localization must be the selected branch.' }
if ($c.ProductionRepairAuthorized -ne $false) { throw 'P2R2 may not authorize a production repair.' }
if ($c.NextAuthorizedImplementation -ne 'P3-R1-Primary-Inventory-Hydraulic-Slow-State-Owner-Localization') { throw 'Unexpected next implementation.' }
if ($c.P3R1MayChangeProductionSource -ne $false -or $c.P3R1MayAddConstitutivePhysics -ne $false -or $c.P3R1MayAddSecondHydraulicSolve -ne $false) { throw 'P3-R1 must remain observation/contract-audit only.' }
foreach ($name in @('ReplacementWorkloadChangeAuthorized','AuthorityPolicyChangeAuthorized','GeneratorLoadSemanticsChangeAuthorized','ProtectionSemanticsChangeAuthorized','ExactV9ChangeAuthorized','MissionPackChangeAuthorized','SecondReplacementLongAuthorized','P4Authorized','M11Authorized')) {
    if ($c.$name -ne $false) { throw ("Forbidden authorization: {0}" -f $name) }
}

$artifactDir='artifacts/m10-final-replacement-long-closure-plan1-p2r2'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
@(
  'm10-final-replacement-long-closure-plan1-p2r2-passes=True',
  'documentation-planning-only=True',
  'p1b-execution-pass=True',
  'p1b-owner-evidence-label=COUPLED-MULTI-DOMAIN',
  'p1b-checkpoints-reproduced=3/3',
  'p1b-trip-count=0',
  'six-mwe-load-reachability-demonstrated=True',
  'six-mwe-whole-operating-point-stationarity-demonstrated=False',
  'p2r2-decision=P3-R-OWNER-LOCALIZATION',
  'p3-w-authorized=False',
  'p3-r-owner-localization-authorized=True',
  'production-repair-authorized=False',
  'replacement-workload-change-authorized=False',
  'exact-v9-change-authorized=False',
  'second-replacement-long-authorized=False',
  'next-authorized-implementation=P3-R1-Primary-Inventory-Hydraulic-Slow-State-Owner-Localization'
) | Set-Content -LiteralPath (Join-Path $artifactDir '01-p2r2-decision-summary.txt') -Encoding UTF8

Write-Host 'M10 Final Replacement-Long Closure Plan 1 P2R2 Decision Re-entry 2 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
