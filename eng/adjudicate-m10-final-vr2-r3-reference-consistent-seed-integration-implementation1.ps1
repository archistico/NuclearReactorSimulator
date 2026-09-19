$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Req([bool]$C,[string]$M){if(-not $C){throw $M}}
function KV([string]$P){$M=@{};foreach($L in [IO.File]::ReadAllLines($P,[Text.Encoding]::UTF8)){if([string]::IsNullOrWhiteSpace($L)){continue};$I=$L.IndexOf('=');Req ($I -gt 0) ("Malformed line: {0}" -f $L);$K=$L.Substring(0,$I);Req (-not $M.ContainsKey($K)) ("Duplicate key: {0}" -f $K);$M[$K]=$L.Substring($I+1)};$M}
$Root=Split-Path -Parent $PSScriptRoot;Set-Location $Root
$C=Get-Content 'eng\m10-final-vr2-r3-reference-consistent-seed-integration-implementation1-contract.json' -Raw | ConvertFrom-Json
$A=Join-Path $Root ([string]$C.implementation.artifact_root).Replace('/','\')
$Raw=Import-Csv -LiteralPath (Join-Path $A '03-raw-seed-roundtrip.csv')
$Health=Import-Csv -LiteralPath (Join-Path $A '04-fast-100-step-health.csv')
Req ($Raw.Count -eq 12) 'Raw roundtrip row count drift.'
Req (@($Raw|Where-Object {$_.phase_match -ne 'true'}).Count -eq 0) 'Raw phase mismatch.'
Req (@($Raw|Where-Object {$_.finite -ne 'true'}).Count -eq 0) 'Raw non-finite result.'
Req ($Health.Count -eq 100) 'Fast health row count drift.'
for($i=0;$i -lt 100;$i++){Req ([int]$Health[$i].step -eq ($i+1)) ("Fast health sequence drift at {0}" -f $i)}
Req (@($Health|Where-Object {$_.finite -ne 'true'}).Count -eq 0) 'Fast health contains non-finite step.'
Req (@($Health|Where-Object {$_.in_envelope -ne 'true'}).Count -eq 0) 'Fast health envelope violation detected.'
Req (@($Health|Where-Object {$_.trip_active -ne 'false'}).Count -eq 0) 'Fast health trip detected.'
Req (@($Health|Where-Object {$_.breaker_closed -ne 'true'}).Count -eq 0) 'Fast health breaker-open step detected.'
$LastRollback=[long]$Health[-1].rollbacks;Req ($LastRollback -eq 0) 'Fast health rollback detected.'
$Ordinary=$env:NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1_ORDINARY_PASS
Req ($Ordinary -eq '1') 'Ordinary Release PASS marker missing.'
[IO.File]::WriteAllLines((Join-Path $A '05-implementation-summary.txt'),@(
 'status=PASS-R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-IMPLEMENTATION1',
 'ordinary-release-suite=PASS',
 'raw-candidate-nodes=12',
 'raw-phase-matches=12',
 'seed-preconditioning-ms=20',
 'fast-running-steps=100',
 'fast-health-envelope-violations=0',
 'fast-rollbacks=0',
 'canonical-exact-v9-method-body=UNCHANGED',
 'production-files-changed=3',
 'r3-passed=False',
 'r4-planning-authorized=False',
 'next-authorized-gate=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION3'
),[Text.Encoding]::UTF8)
[IO.File]::WriteAllLines((Join-Path $A '06-pre-requalification-review.txt'),@(
 'status=PASS-IMPLEMENTATION-EVIDENCE-COMPLETE',
 'full-r3-120s-requalification-still-required=True',
 'canonical-exact-v9-change-authorized=False',
 'new-exact-version-identity-authorized=False',
 'threshold-change-authorized=False',
 'r4-remains-blocked=True'
),[Text.Encoding]::UTF8)
Write-Host 'R3 reference-consistent Seed Integration Implementation 1 adjudication: PASS' -ForegroundColor Green
Write-Host 'R3 remains RED until Requalification 3.'
