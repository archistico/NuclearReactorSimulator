$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require-C1([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Read-KvC1([string]$Path){
    $Map=@{}
    foreach($Line in [IO.File]::ReadAllLines($Path,[Text.Encoding]::UTF8)){
        if([string]::IsNullOrWhiteSpace($Line)){continue}
        $I=$Line.IndexOf('=');Require-C1 ($I -gt 0) ("Malformed line: {0}" -f $Line)
        $K=$Line.Substring(0,$I);Require-C1 (-not $Map.ContainsKey($K)) ("Duplicate key: {0}" -f $K)
        $Map[$K]=$Line.Substring($I+1)
    };$Map
}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-reference-consistent-raw-seed-candidate-construction1-contract.json' -Raw | ConvertFrom-Json
$ArtifactRoot=Join-Path $Root ([string]$Contract.candidate_construction.artifact_root).Replace('/','\')

$Candidates=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '01-candidate-conserved-inventory-vector.csv')
$Roundtrip=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '02-candidate-target-roundtrip.csv')
$Heads=Import-Csv -LiteralPath (Join-Path $ArtifactRoot '03-candidate-hydraulic-head-comparison.csv')
$Summary=Read-KvC1 (Join-Path $ArtifactRoot '04-candidate-construction-summary.txt')

Require-C1 ($Candidates.Count -eq 12) 'Candidate vector must contain 12 nodes.'
Require-C1 ($Roundtrip.Count -eq 12) 'Roundtrip evidence must contain 12 nodes.'
Require-C1 ($Heads.Count -eq 8) 'Hydraulic-head evidence must contain 8 paths.'
Require-C1 ($Summary['status'] -eq 'CANDIDATE-CONSTRUCTION-EVIDENCE-WRITTEN') 'Candidate summary status drift.'
Require-C1 ([int]$Summary['candidate-node-count'] -eq 12) 'Candidate summary node count drift.'
Require-C1 ([int]$Summary['resolved-node-count'] -eq 12) 'All candidate nodes must resolve.'
Require-C1 ($Summary['runtime-preconditioning-steps'] -eq '0') 'Runtime preconditioning was not zero.'
Require-C1 ($Summary['runtime-dynamic-steps'] -eq '0') 'Runtime dynamics were not zero.'
Require-C1 ($Summary['production-source-changed'] -eq 'False') 'Candidate Construction changed production source.'
Require-C1 ($Summary['c4-resolver-changed'] -eq 'False') 'Candidate Construction changed C4 resolver.'
Require-C1 ($Summary['c4-payload-changed'] -eq 'False') 'Candidate Construction changed C4 payload.'
Require-C1 ($Summary['canonical-exact-v9-changed'] -eq 'False') 'Candidate Construction changed canonical exact-v9.'
Require-C1 ($Summary['threshold-change-applied'] -eq 'False') 'Candidate Construction changed thresholds.'
Require-C1 ($Summary['r3-remains-red'] -eq 'True') 'R3 authority drift.'
Require-C1 ($Summary['r4-planning-authorized'] -eq 'False') 'R4 authority drift.'

$CandidateIds=@($Candidates | ForEach-Object { $_.node } | Sort-Object -Unique)
$RoundtripIds=@($Roundtrip | ForEach-Object { $_.node } | Sort-Object -Unique)
Require-C1 ($CandidateIds.Count -eq 12 -and $RoundtripIds.Count -eq 12) 'Candidate/roundtrip node IDs are not unique.'

[IO.File]::WriteAllLines((Join-Path $ArtifactRoot '05-pre-integration-review.txt'),@(
    'status=PASS-CANDIDATE-CONSTRUCTION-EVIDENCE-COMPLETE',
    ('resolved-node-count=' + [string]$Summary['resolved-node-count']),
    ('phase-match-node-count=' + [string]$Summary['phase-match-node-count']),
    ('max-abs-pressure-residual-pa=' + [string]$Summary['max-abs-pressure-residual-pa']),
    ('max-abs-temperature-residual-k=' + [string]$Summary['max-abs-temperature-residual-k']),
    ('max-abs-quality-residual=' + [string]$Summary['max-abs-quality-residual']),
    ('max-abs-hydraulic-head-residual-pa=' + [string]$Summary['max-abs-hydraulic-head-residual-pa']),
    'residual-acceptance-thresholds-defined=False',
    'candidate-vector-is-production=False',
    'production-seed-integration-authorized=False',
    'new-exact-version-identity-authorized=False',
    'r3-remains-red=True',
    'r4-planning-authorized=False',
    'next-step=RETURN-ARTIFACTS-FOR-SEED-INTEGRATION-PLANNING-ADJUDICATION'
),[Text.Encoding]::UTF8)

Write-Host 'R3 reference-consistent raw-seed Candidate Construction 1 adjudication: PASS' -ForegroundColor Green
Write-Host 'Evidence construction only. Return all five artifacts before Seed Integration Planning 1.'
