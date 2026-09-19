$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ContractPath=Join-Path $Root 'eng\github-ordinary-ci-stable-contract-v2.json'
$C=Get-Content $ContractPath -Raw|ConvertFrom-Json
Req ($C.schema -eq 'github-ordinary-ci-stable-contract-v2') 'CI V2 schema drift'
Req ($C.status -eq 'ACTIVE-CI-CONTRACT') 'CI V2 status drift'

Req (-not [bool]$C.permanent_contract.source_tree_snapshot_enforced) 'permanent CI must not freeze src tree snapshots'
Req (-not [bool]$C.permanent_contract.test_tree_snapshot_enforced) 'permanent CI must not freeze tests tree snapshots'
Req (-not [bool]$C.permanent_contract.file_count_snapshot_enforced) 'permanent CI must not freeze repository file counts'
Req (-not [bool]$C.permanent_contract.raw_worktree_hash_enforced) 'permanent CI must not depend on raw worktree hashes'
Req (-not [bool]$C.historical_hotfix1.active_ci_contract) 'Hotfix 1 must remain provenance-only'

foreach($p in $C.paths.PSObject.Properties){
    $path=Join-Path $Root ([string]$p.Value).Replace('/','\')
    Req (Test-Path $path -PathType Leaf) ("required CI V2 path missing: "+$p.Name)
}

$ci=[IO.File]::ReadAllText((Join-Path $Root ([string]$C.paths.ci_ordinary).Replace('/','\')),[Text.Encoding]::UTF8)
Req ($ci.Contains('set "CI=true"')) 'CI=true must be intrinsic to ci-ordinary.cmd'
Req ($ci.Contains([string]$C.deterministic_ci.validator_command_fragment)) 'CI V2 validator must run from ci-ordinary.cmd'
Req (-not $ci.Contains('validate-github-ordinary-ci-deterministic-hotfix1.ps1')) 'legacy Hotfix 1 validator must not run from permanent ordinary CI'
Req ($ci.Contains([string]$C.deterministic_ci.restore_command)) 'restore fail-closed contract drift'
Req ($ci.Contains([string]$C.deterministic_ci.build_command)) 'build fail-closed contract drift'
Req ($ci.Contains([string]$C.deterministic_ci.expected_dotnet_test_command)) 'deterministic test command drift'
Req ($ci.Contains([string]$C.deterministic_ci.current_evidence_command)) 'current evidence fail-closed contract drift'
Req (-not $ci.Contains('--filter')) 'test filter is forbidden in ordinary CI'
Req (-not $ci.Contains('continue-on-error')) 'continue-on-error is forbidden in ordinary CI'
Req (-not $ci.Contains('|| ver >nul')) 'failure suppression is forbidden in ordinary CI'
Req (-not $ci.Contains('retry')) 'retry semantics are forbidden in ordinary CI'

$wf=[IO.File]::ReadAllText((Join-Path $Root ([string]$C.paths.workflow).Replace('/','\')),[Text.Encoding]::UTF8)
Req ($wf.Contains(('runs-on: '+[string]$C.workflow.runner))) 'hosted runner drift'
Req ($wf.Contains("CI: 'true'")) 'hosted CI environment drift'
Req ($wf.Contains(('uses: '+[string]$C.workflow.checkout_action))) 'checkout action drift'
Req ($wf.Contains(('uses: '+[string]$C.workflow.setup_dotnet_action))) 'setup-dotnet action drift'
Req ($wf.Contains('global-json-file: global.json')) 'global.json workflow binding drift'
Req ($wf.Contains(('run: '+[string]$C.workflow.entry_point))) 'workflow ordinary entry point drift'
Req (-not $wf.Contains('continue-on-error: true')) 'hosted continue-on-error forbidden'

$G=Get-Content (Join-Path $Root ([string]$C.paths.global_json).Replace('/','\')) -Raw|ConvertFrom-Json
Req ([string]$G.sdk.version -eq [string]$C.deterministic_ci.sdk_version) 'SDK version drift'
Req ([string]$G.test.runner -eq [string]$C.deterministic_ci.runner) 'test runner drift'

$props=[IO.File]::ReadAllText((Join-Path $Root ([string]$C.paths.directory_build_props).Replace('/','\')),[Text.Encoding]::UTF8)
Req ($props.Contains('<TreatWarningsAsErrors>true</TreatWarningsAsErrors>')) 'warnings-as-errors contract drift'
Req ($props.Contains('<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>')) 'ContinuousIntegrationBuild contract drift'

$current=[IO.File]::ReadAllText((Join-Path $Root ([string]$C.paths.current_evidence).Replace('/','\')),[Text.Encoding]::UTF8)
Req ($current.Contains('run-phase-i-audit-consolidation-ci-baseline-audit.cmd')) 'current evidence I.2 gate drift'
Req ($current.Contains('run-i5-synchronization-corrected-v3-activation-audit.cmd')) 'current evidence I.5 gate drift'
Req ($current.Contains('run-m10-final-v9-authoritative-production-audit.cmd')) 'current evidence exact-v9 gate drift'

Req (-not [bool]$C.authority.production_physics_change_authorized) 'CI V2 must not authorize production physics changes'
Req (-not [bool]$C.authority.test_semantics_change_authorized) 'CI V2 must not authorize test semantics changes'
Req (-not [bool]$C.authority.threshold_change_authorized) 'CI V2 must not authorize threshold changes'
Req (-not [bool]$C.authority.r3_passed) 'R3 must remain RED'
Req (-not [bool]$C.authority.repair_owner_selected) 'repair owner must remain unselected'
Write-Host 'GitHub Ordinary CI Stable Contract V2 static audit: PASS' -ForegroundColor Green
