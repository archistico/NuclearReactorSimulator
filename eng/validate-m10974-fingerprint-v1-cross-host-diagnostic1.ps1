$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$c,[string]$m){if(-not $c){throw $m}}
function ReadUtf8([string]$p){return [IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)}
function Norm([string]$s){return (($s -replace "`r`n","`n") -replace "`r","`n")}
function ShaText([string]$p){
  $text=Norm (ReadUtf8 $p)
  $sha=[Security.Cryptography.SHA256]::Create()
  try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))).Replace('-','')) }
  finally { $sha.Dispose() }
}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$ContractPath=Join-Path $Root 'eng\m10974-fingerprint-v1-cross-host-diagnostic1-contract.json'
Req (Test-Path $ContractPath -PathType Leaf) 'diagnostic contract missing'
$C=Get-Content $ContractPath -Raw|ConvertFrom-Json
Req ($C.schema -eq 'm10974-fingerprint-v1-cross-host-diagnostic1-contract-v1') 'diagnostic schema drift'
Req ($C.status -eq 'TEST-ONLY-DIAGNOSTIC') 'diagnostic status drift'
foreach($p in $C.paths.PSObject.Properties){
  $path=Join-Path $Root ([string]$p.Value).Replace('/','\')
  Req (Test-Path $path -PathType Leaf) ('required diagnostic path missing: '+$p.Name)
}
$fingerprint=Join-Path $Root ([string]$C.paths.fingerprint_production).Replace('/','\')
$h29=Join-Path $Root ([string]$C.paths.h29_factory).Replace('/','\')
$ci=Join-Path $Root ([string]$C.paths.ci_ordinary).Replace('/','\')
Req ((ShaText $fingerprint) -eq [string]$C.frozen_normalized_sha256.fingerprint_production) 'fingerprint-v1 production implementation drift'
Req ((ShaText $h29) -eq [string]$C.frozen_normalized_sha256.h29_factory) 'H29 exact-version factory drift'
Req ((ShaText $ci) -eq [string]$C.frozen_normalized_sha256.ci_ordinary) 'ordinary CI execution contract drift'
$test=ReadUtf8 (Join-Path $Root ([string]$C.paths.schema_anchor_test).Replace('/','\'))
Req ($test.Contains('private const string GoldenFingerprint = "63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362";')) 'frozen golden fingerprint drift'
Req ($test.Contains('M10974FingerprintV1CrossHostDiagnostic1.TryWrite(snapshot, GoldenFingerprint, actualFingerprint);')) 'same-execution diagnostic hook missing'
Req ($test.Contains('Assert.Equal(GoldenFingerprint, actualFingerprint);')) 'golden pass/fail assertion drift'
$helper=ReadUtf8 (Join-Path $Root ([string]$C.paths.diagnostic_helper).Replace('/','\'))
Req ($helper.Contains('NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR')) 'diagnostic environment binding missing'
Req ($helper.Contains('normalized-control-room-snapshot-v1.json')) 'normalized payload capture missing'
Req ($helper.Contains('top-level.tsv')) 'top-level hash capture missing'
Req ($helper.Contains('nodes.tsv')) 'JSON node hash capture missing'
Req (-not $helper.Contains('Assert.')) 'diagnostic helper must not own test assertions'
$workflow=Norm (ReadUtf8 (Join-Path $Root ([string]$C.paths.workflow).Replace('/','\')))
$workflowCanonical=$workflow.Replace('\','/')
Req ($workflowCanonical.Contains('NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR:')) 'hosted diagnostic environment binding missing'
Req ($workflowCanonical.Contains('path: artifacts/ci/')) 'hosted diagnostic upload root drift'
Req (([regex]::Matches($workflowCanonical,[regex]::Escape('eng/ci-ordinary.cmd'))).Count -eq 1) 'hosted ordinary entry point cardinality drift'
Req (-not $workflowCanonical.Contains('dotnet test')) 'workflow must not add a second test execution'
$runner=ReadUtf8 (Join-Path $Root ([string]$C.paths.runner).Replace('/','\'))
Req ($runner.Contains('M10974FingerprintV1SchemaAnchorTests.FingerprintV1_PopulatedExactVersionFixtureMatchesFrozenGoldenHash')) 'local focused method drift'
Req ($runner.Contains('NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR')) 'local diagnostic output binding missing'
Req (-not [bool]$C.capture.production_source_change_authorized) 'production source change must remain unauthorized'
Req (-not [bool]$C.capture.test_pass_fail_semantics_change_authorized) 'test pass/fail semantics change must remain unauthorized'
Req (-not [bool]$C.capture.second_hosted_test_execution_authorized) 'second hosted test execution must remain unauthorized'
Req (-not [bool]$C.authority.fingerprint_v1_change_authorized) 'fingerprint-v1 change must remain unauthorized'
Req (-not [bool]$C.authority.golden_hash_change_authorized) 'golden hash change must remain unauthorized'
Req (-not [bool]$C.authority.repair_planning_authorized) 'repair planning must remain blocked'
Req (-not [bool]$C.authority.r3_passed) 'R3 must remain RED'
Req (-not [bool]$C.authority.repair_owner_selected) 'repair owner must remain unselected'
$bytes=[IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
Req (-not ($bytes | Where-Object { $_ -gt 127 })) 'diagnostic validator must remain ASCII-only'
Write-Host 'M10.9.7.4 Fingerprint V1 Cross-Host Determinism Diagnostic 1 static audit: PASS' -ForegroundColor Green
