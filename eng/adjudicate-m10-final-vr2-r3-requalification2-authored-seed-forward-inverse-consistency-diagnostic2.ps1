$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require-D2([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Read-KvD2([string]$Path){
    $Map=@{}
    foreach($Line in [IO.File]::ReadAllLines($Path,[Text.Encoding]::UTF8)){
        if([string]::IsNullOrWhiteSpace($Line)){continue}
        $I=$Line.IndexOf('=');Require-D2 ($I -gt 0) ("Malformed line: {0}" -f $Line)
        $K=$Line.Substring(0,$I);Require-D2 (-not $Map.ContainsKey($K)) ("Duplicate key: {0}" -f $K)
        $Map[$K]=$Line.Substring($I+1)
    };$Map
}
$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2-contract.json' -Raw | ConvertFrom-Json
$ArtifactRoot=Join-Path $Root ([string]$Contract.diagnostic.artifact_root).Replace('/','\')
$Rows=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '01-raw-authored-seed-closure-comparison.csv')
$Heads=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '02-raw-authored-seed-hydraulic-head-comparison.csv')
$Summary=Read-KvD2 (Join-Path $ArtifactRoot '03-diagnostic-summary.txt')
Require-D2 ($Rows.Count -eq 12) 'Raw seed row count drift.'
Require-D2 ($Heads.Count -eq 8) 'Hydraulic head row count drift.'
$Allowed=@($Contract.diagnostic.allowed_classifications)
Require-D2 ($Allowed -contains [string]$Summary['classification']) 'Unexpected diagnostic classification.'
Require-D2 ($Summary['deterministic-preconditioning-included'] -eq 'False') 'Diagnostic unexpectedly included preconditioning.'
Require-D2 ($Summary['production-repair-applied'] -eq 'False') 'Diagnostic unexpectedly applied production repair.'
Require-D2 ($Summary['threshold-change-applied'] -eq 'False') 'Diagnostic unexpectedly changed thresholds.'
Require-D2 ($Summary['new-exact-version-created'] -eq 'False') 'Diagnostic unexpectedly created an exact version.'
Require-D2 ($Summary['r3-remains-red'] -eq 'True') 'R3 authority drift.'
Require-D2 ($Summary['r4-planning-authorized'] -eq 'False') 'R4 authority drift.'

[IO.File]::WriteAllLines((Join-Path $ArtifactRoot '04-pre-repair-review.txt'),@(
    'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
    ('classification=' + [string]$Summary['classification']),
    ('forward-property-providers-bitwise-equal=' + [string]$Summary['forward-property-providers-bitwise-equal']),
    ('raw-seed-mode1-mode2-inverse-difference-nodes=' + [string]$Summary['raw-seed-mode1-mode2-inverse-difference-nodes']),
    ('raw-seed-phase-difference-nodes=' + [string]$Summary['raw-seed-phase-difference-nodes']),
    ('max-raw-seed-pressure-delta-pa=' + [string]$Summary['max-raw-seed-pressure-delta-pa']),
    ('max-raw-seed-hydraulic-head-delta-pa=' + [string]$Summary['max-raw-seed-hydraulic-head-delta-pa']),
    'production-repair-authorized=False',
    'seed-repair-authorized=False',
    'new-exact-version-identity-authorized=False',
    'r3-remains-red=True',
    'r4-planning-authorized=False',
    'next-step=RETURN-ARTIFACTS-FOR-ADJUDICATION'
),[Text.Encoding]::UTF8)

Write-Host 'R3-2 authored-seed forward/inverse consistency Diagnostic 2 adjudication: PASS' -ForegroundColor Green
Write-Host ('Classification: ' + [string]$Summary['classification'])
Write-Host 'Return all four artifacts before repair/replanning.'
