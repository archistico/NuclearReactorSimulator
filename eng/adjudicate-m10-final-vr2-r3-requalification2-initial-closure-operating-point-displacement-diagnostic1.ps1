$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-D2([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Read-KvD2([string]$Path){
    Require-D2 (Test-Path -LiteralPath $Path -PathType Leaf) ("Missing artifact: {0}" -f $Path)
    $Map=@{}
    foreach($Line in [IO.File]::ReadAllLines($Path,[Text.Encoding]::UTF8)){
        if([string]::IsNullOrWhiteSpace($Line)){continue}
        $I=$Line.IndexOf('=')
        Require-D2 ($I -gt 0) ("Malformed key/value line: {0}" -f $Line)
        $K=$Line.Substring(0,$I)
        Require-D2 (-not $Map.ContainsKey($K)) ("Duplicate key: {0}" -f $K)
        $Map[$K]=$Line.Substring($I+1)
    }
    $Map
}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1-contract.json' -Raw | ConvertFrom-Json
$ArtifactRoot=Join-Path $Root ([string]$Contract.diagnostic.artifact_root).Replace('/','\')

$Nodes=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '01-initial-node-closure-comparison.csv')
$Heads=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '02-initial-hydraulic-head-comparison.csv')
$Dynamic=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '03-first-second-dynamic-comparison.csv')
$Summary=Read-KvD2 (Join-Path $ArtifactRoot '04-diagnostic-summary.txt')

Require-D2 ($Nodes.Count -eq 12) 'Initial node comparison must contain 12 nodes.'
Require-D2 ($Heads.Count -ge 6) 'Hydraulic head comparison is incomplete.'
Require-D2 ($Dynamic.Count -eq 100) 'Dynamic comparison must contain 100 steps.'
for($i=0;$i -lt 100;$i++){
    Require-D2 ([int]$Dynamic[$i].step -eq ($i+1)) ("Dynamic step sequence drift at index {0}." -f $i)
}

$Allowed=@($Contract.diagnostic.allowed_classifications)
Require-D2 ($Allowed -contains [string]$Summary['classification']) 'Unexpected diagnostic classification.'
Require-D2 ($Summary['production-repair-applied'] -eq 'False') 'Diagnostic unexpectedly applied production repair.'
Require-D2 ($Summary['threshold-change-applied'] -eq 'False') 'Diagnostic unexpectedly changed thresholds.'
Require-D2 ($Summary['r3-remains-red'] -eq 'True') 'R3 authority drift.'
Require-D2 ($Summary['r4-planning-authorized'] -eq 'False') 'R4 authority drift.'

[IO.File]::WriteAllLines((Join-Path $ArtifactRoot '05-pre-repair-review.txt'),@(
    'status=PASS-DIAGNOSTIC-EVIDENCE-COMPLETE',
    ('classification=' + [string]$Summary['classification']),
    ('all-initial-inventories-bitwise-equal=' + [string]$Summary['all-initial-inventories-bitwise-equal']),
    ('same-inventory-direct-closure-difference-nodes=' + [string]$Summary['same-inventory-direct-closure-difference-nodes']),
    ('max-same-inventory-direct-pressure-delta-pa=' + [string]$Summary['max-same-inventory-direct-pressure-delta-pa']),
    ('max-initial-hydraulic-head-delta-pa=' + [string]$Summary['max-initial-hydraulic-head-delta-pa']),
    ('mode1-key-envelope-violation-steps=' + [string]$Summary['mode1-key-envelope-violation-steps']),
    ('mode2-key-envelope-violation-steps=' + [string]$Summary['mode2-key-envelope-violation-steps']),
    'production-repair-authorized=False',
    'threshold-change-authorized=False',
    'r3-remains-red=True',
    'r4-planning-authorized=False',
    'next-step=RETURN-ARTIFACTS-FOR-ADJUDICATION'
),[Text.Encoding]::UTF8)

Write-Host 'R3-2 initial closure / operating-point displacement Diagnostic 1 adjudication: PASS' -ForegroundColor Green
Write-Host ('Classification: ' + [string]$Summary['classification'])
Write-Host 'Diagnostic only. Return all five artifacts before any repair/replanning decision.'
