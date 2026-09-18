$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Missing required file: {0}" -f $Path)
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

$required = @(
    'eng/m10-final-replacement-long-closure-plan1-p2-contract.json',
    'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1_DecisionSummary.txt',
    'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1_ProbeSummary.csv',
    'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1_ReferenceNoiseCalibration.csv',
    'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1_Events.csv',
    'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2_DECISION.md',
    'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md',
    'docs/PROJECT.md',
    'docs/ROADMAP.md',
    'docs/M10_FINAL_VV_MATRIX.md',
    'docs/M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md',
    'docs/README.md'
)
$required | ForEach-Object { Require-File $_ }

$contract = Get-Content -LiteralPath 'eng/m10-final-replacement-long-closure-plan1-p2-contract.json' -Raw | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-replacement-long-closure-plan1-p2-v1') { throw 'Unexpected P2 contract schema.' }
if ($contract.status -ne 'P2-DECISION-CANDIDATE-PLAN-STOP-INCONCLUSIVE') { throw 'Unexpected P2 status.' }
if ($contract.p1_final_classification -ne 'INCONCLUSIVE') { throw 'P1 final classification must be INCONCLUSIVE.' }
if ($contract.p2_decision -ne 'PLAN-STOP-INCONCLUSIVE') { throw 'P2 must remain planning-stop.' }
if ($contract.p3_w_authorized -ne $false) { throw 'P3-W must remain unauthorized.' }
if ($contract.p3_r_authorized -ne $false) { throw 'P3-R must remain unauthorized.' }
if ($contract.production_src_changed -ne $false) { throw 'P2 must not change production src.' }
if ($contract.production_tests_changed -ne $false) { throw 'P2 must not change production tests.' }
if ($contract.second_replacement_long_authorized -ne $false) { throw 'P2 must not authorize a second replacement long.' }
if ($contract.plan_amendment_1.id -ne 'P1A-ASYMPTOTIC-CLOSURE-EXTENSION') { throw 'Plan Amendment 1 identity mismatch.' }
if ($contract.plan_amendment_1.max_total_hold_seconds_after_load -ne 3600) { throw 'P1A max horizon must be 3600 s.' }
if ($contract.plan_amendment_1.exact_v4_rerun_authorized -ne $false) { throw 'P1A must not rerun exact-v4.' }
if ($contract.plan_amendment_1.further_automatic_continuation_authorized -ne $false) { throw 'P1A must not authorize unbounded continuation.' }
if ($contract.next_authorized_implementation -ne 'P1A-Asymptotic-Closure-Extension') { throw 'Unexpected next authorized implementation.' }

$p1 = 'eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P1_DecisionSummary.txt'
Require-Text $p1 'p1-final-classification=INCONCLUSIVE'
Require-Text $p1 'p2-branch-signal=P2-PLAN-STOP-INCONCLUSIVE'
Require-Text $p1 'bounded-continuation-invoked=True'
Require-Text $p1 'm10-final-replacement-long-closure-plan1-p1-passes=True'
Require-Text $p1 'output-error:0.068735004129901078'
Require-Text $p1 'dispatch-adequacy:-0.070137744201449845'

$p2 = 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2_DECISION.md'
@(
    'PLAN-STOP-INCONCLUSIVE',
    'P3-W-AUTHORIZED = False',
    'P3-R-AUTHORIZED = False',
    'P1A Asymptotic Closure Extension',
    '3,600 s after its load command',
    'P2R',
    'no further automatic continuation beyond 3,600 s'
) | ForEach-Object { Require-Text $p2 $_ }

Require-Text 'docs/PROJECT.md' 'P2 DECISION GATE 1 — PLAN-STOP-INCONCLUSIVE CANDIDATE'
Require-Text 'docs/ROADMAP.md' 'P1A Asymptotic Closure Extension'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'P1 returned `INCONCLUSIVE`'
Require-Text 'docs/M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md' 'P2 planning stop'
Require-Text 'docs/README.md' 'M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2_DECISION.md'

$artifactDir = 'artifacts/m10-final-replacement-long-closure-plan1-p2'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$summary = @(
    'scope=M10 Final Replacement-Long Closure Plan 1 P2 Decision Gate 1',
    'contract=m10-final-replacement-long-closure-plan1-p2-v1',
    'p1=RETURNED-EXECUTION-PASS',
    'p1-final-classification=INCONCLUSIVE',
    'p2-decision=PLAN-STOP-INCONCLUSIVE',
    'p3-w-authorized=False',
    'p3-r-authorized=False',
    'plan-amendment-1=P1A-ASYMPTOTIC-CLOSURE-EXTENSION',
    'p1a-max-total-hold-seconds=3600',
    'production-src-changed=False',
    'production-tests-changed=False',
    'replacement-workload-changed=False',
    'runtime-semantics-changed=False',
    'second-replacement-long-authorized=False',
    'm10=OPEN',
    'next-authorized-implementation=P1A-Asymptotic-Closure-Extension',
    'm10-final-replacement-long-closure-plan1-p2-passes=True'
)
Set-Content -LiteralPath (Join-Path $artifactDir '01-m10-final-replacement-long-closure-plan1-p2.summary.txt') -Value $summary -Encoding UTF8

Write-Host 'M10 Final Replacement-Long Closure Plan 1 P2 Decision Gate 1 audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
exit 0
