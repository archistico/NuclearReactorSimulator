$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing required file: $Path"
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

$required = @(
    'eng/m10-final-replacement-long-closure-plan1-contract.json',
    'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md',
    'docs/M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md',
    'docs/M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md',
    'docs/M10_FINAL_VV_MATRIX.md',
    'docs/PROJECT.md',
    'docs/ROADMAP.md',
    'docs/README.md'
)
$required | ForEach-Object { Require-File $_ }

$contract = Get-Content -LiteralPath 'eng/m10-final-replacement-long-closure-plan1-contract.json' -Raw | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-replacement-long-closure-plan1-v1') { throw 'Unexpected P0 contract schema.' }
if ($contract.validator_hotfix -ne 'P0-HOTFIX2-PROJECT-MARKER-ALIGNMENT') { throw 'P0 Hotfix 2 validator marker missing.' }
if ($contract.status -ne 'P0-PLANNING-CANDIDATE-NOT-PROMOTION-EVIDENCE') { throw 'P0 contract status is not fail-closed planning status.' }
if ($contract.production_src_changed -ne $false) { throw 'P0 contract must not claim a production src change.' }
if ($contract.production_tests_changed -ne $false) { throw 'P0 contract must not claim a production test change.' }
if ($contract.second_replacement_long_authorized -ne $false) { throw 'P0 must not authorize Replacement-Long Execution 2.' }
if ($contract.m10_status -ne 'OPEN') { throw 'P0 must keep M10 OPEN.' }
if ($contract.hard_stops.exact_v9_never_reinterpreted -ne $true) { throw 'exact-v9 immutability hard stop missing.' }
if ($contract.hard_stops.no_second_long_freeze_before_p4_pass -ne $true) { throw 'P4 prerequisite for second long freeze missing.' }
if ($contract.hard_stops.no_ad_hoc_diagnostic_after_p1_inconclusive -ne $true) { throw 'P1 inconclusive hard stop missing.' }

$plan = 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md'
@(
    'Evidence & Planning Freeze',
    'Asymptotic First-Stage Qualification',
    'Decision Gate',
    'Workload / Procedure path',
    'Runtime Ownership path',
    'Short 5',
    'Replacement-Long Baseline 2 Freeze and Execution 2',
    'M10 Final Closure',
    'INCONCLUSIVE',
    'exact-v9 remains immutable',
    'No second replacement-long baseline may be frozen until P4 PASS.'
) | ForEach-Object { Require-Text $plan $_ }

Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md' 'RETURNED / EXECUTION PASS'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md' '50.000283643052136'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md' '5.7338236172924848'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md' '-0.27161903467884968'
Require-Text 'docs/PROJECT.md' 'P0 EVIDENCE & PLANNING FREEZE HOTFIX 2 CANDIDATE'
Require-Text 'docs/PROJECT.md' 'Diagnostic 6 returned execution PASS'
Require-Text 'docs/M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md' 'M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'Replacement-Long Execution 1 also remains **RED**'
Require-Text 'docs/ROADMAP.md' 'P3-W/P3-R'
Require-Text 'docs/README.md' 'M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md'

$artifactDir = 'artifacts/m10-final-replacement-long-closure-plan1'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$summary = @(
    'scope=M10 Final Replacement-Long Closure Plan 1 P0 documentation/planning freeze',
    'contract=m10-final-replacement-long-closure-plan1-v1',
    'validator-hotfix=P0-HOTFIX2-PROJECT-MARKER-ALIGNMENT',
    'diagnostics-returned-pass=D1|D2|D3|D4|D5|D6',
    'replacement-long-execution-1=RED-IMMUTABLE-EVIDENCE',
    'm10=OPEN',
    'route=P0|P1|P2|P3-W-or-P3-R|P4|P5A|P5B|P6',
    'production-src-changed=False',
    'production-tests-changed=False',
    'replacement-workload-changed=False',
    'authority-policy-changed=False',
    'generator-load-semantics-changed=False',
    'protection-semantics-changed=False',
    'exact-v9-changed=False',
    'mission-pack-changed=False',
    'second-replacement-long-authorized=False',
    'next-authorized-implementation=P1-Asymptotic-First-Stage-Qualification',
    'm10-final-replacement-long-closure-plan1-p0-passes=True'
)
Set-Content -LiteralPath (Join-Path $artifactDir '01-m10-final-replacement-long-closure-plan1.summary.txt') -Value $summary -Encoding UTF8

Write-Host 'M10 Final Replacement-Long Closure Plan 1 P0 Hotfix 2 audit: PASS'
Write-Host "Artifact: $artifactDir"
exit 0
