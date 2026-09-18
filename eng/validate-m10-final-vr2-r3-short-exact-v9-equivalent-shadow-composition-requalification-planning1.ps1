$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-R3([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Require-R3Text([string]$Path,[string]$Needle) {
    Require-R3 (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    $text=[System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8)
    Require-R3 ($text.IndexOf($Needle,[System.StringComparison]::Ordinal) -ge 0) ("Required marker not found in {0}: {1}" -f $Path,$Needle)
}
function Get-R3Sha256([byte[]]$Bytes) {
    $sha=[System.Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant() } finally { $sha.Dispose() }
}
function Get-R3FileSha256([string]$Path) { Get-R3Sha256 ([System.IO.File]::ReadAllBytes($Path)) }
function Get-R3NormalizedTextSha256([string]$Path) {
    $t=[System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n")
    Get-R3Sha256 ([System.Text.Encoding]::UTF8.GetBytes($t))
}
function Get-R3TreeSha256([string]$Root) {
    $resolved=(Resolve-Path -LiteralPath $Root).Path
    $paths=New-Object 'System.Collections.Generic.List[string]'
    foreach($file in @(Get-ChildItem -LiteralPath $resolved -Recurse -File)) {
        $rel=$file.FullName.Substring($resolved.Length+1).Replace('\','/')
        $segments=@($rel.Split('/')); $skip=$false
        for($i=0;$i -lt ($segments.Count-1);$i++){ if($segments[$i] -eq 'bin' -or $segments[$i] -eq 'obj'){$skip=$true;break} }
        if(-not $skip){$paths.Add($rel)}
    }
    $paths.Sort([System.StringComparer]::Ordinal)
    $ms=New-Object System.IO.MemoryStream
    try {
        foreach($rel in $paths){
            $rb=[System.Text.Encoding]::UTF8.GetBytes($rel);$ms.Write($rb,0,$rb.Length);$ms.WriteByte(0)
            $fh=[System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes((Join-Path $resolved $rel.Replace('/','\'))))
            $ms.Write($fh,0,$fh.Length);$ms.WriteByte(10)
        }
        $ms.Position=0
        $th=[System.Security.Cryptography.SHA256]::HashData($ms.ToArray())
        @{Hash=([BitConverter]::ToString($th)).Replace('-','').ToUpperInvariant();Count=$paths.Count}
    } finally { $ms.Dispose() }
}

$repoRoot=Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$contractPath=Join-Path $repoRoot 'eng\m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification-planning1-contract.json'
$contract=Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json

Require-R3 ($contract.schema -eq 'm10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification-planning1-v1') 'R3 Planning 1 schema mismatch.'
Require-R3 ($contract.status -eq 'PLANNING-ONLY') 'R3 Planning 1 status mismatch.'
Require-R3 ($contract.prerequisite.r2_returned_adjudication -eq 'PASS') 'Returned R2 adjudication is not PASS.'
Require-R3 ($contract.prerequisite.r2_classification -eq 'PASS-R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFIED') 'R2 classification drift.'
Require-R3 (-not [bool]$contract.prerequisite.default_mode2_activation) 'Mode 2 default activation must remain false.'
Require-R3 (-not [bool]$contract.prerequisite.exact_v9_composition) 'Historical exact-v9 must remain unmodified before R3 execution.'

$src=Get-R3TreeSha256 (Join-Path $repoRoot 'src')
$tests=Get-R3TreeSha256 (Join-Path $repoRoot 'tests')
Require-R3 ($src.Count -eq [int]$contract.baseline.src_file_count -and $src.Hash -eq [string]$contract.baseline.src_tree_sha256) 'Production src tree drift.'
Require-R3 ($tests.Count -eq [int]$contract.baseline.tests_file_count -and $tests.Hash -eq [string]$contract.baseline.tests_tree_sha256) 'Tests tree drift.'

foreach($prop in $contract.baseline.exact_v9_provenance_files_sha256.PSObject.Properties){
    $p=Join-Path $repoRoot ([string]$prop.Value.path).Replace('/','\')
    Require-R3 ((Get-R3FileSha256 $p) -eq [string]$prop.Value.sha256) ("Exact-v9 provenance hash drift: {0}" -f $prop.Name)
}

$r2Root=Join-Path $repoRoot ($contract.r2_returned_artifacts.root.Replace('/','\'))
Require-R3 (Test-Path -LiteralPath $r2Root -PathType Container) 'Frozen R2 artifact root missing.'
$actualR2=@(Get-ChildItem -LiteralPath $r2Root -File)
Require-R3 ($actualR2.Count -eq [int]$contract.r2_returned_artifacts.required_files) 'Frozen R2 artifact count drift.'
foreach($prop in $contract.r2_returned_artifacts.sha256.PSObject.Properties){
    $p=Join-Path $r2Root $prop.Name
    Require-R3 (Test-Path -LiteralPath $p -PathType Leaf) ("Frozen R2 artifact missing: {0}" -f $prop.Name)
    Require-R3 ((Get-R3FileSha256 $p) -eq [string]$prop.Value) ("Frozen R2 artifact hash drift: {0}" -f $prop.Name)
}
$audit=Join-Path $repoRoot ($contract.prerequisite.r2_returned_audit.Replace('/','\'))
Require-R3 ((Get-R3FileSha256 $audit) -eq [string]$contract.prerequisite.r2_returned_audit_sha256) 'Returned R2 audit hash drift.'
Require-R3Text $audit 'r3-planning-authorized=True'
Require-R3Text (Join-Path $r2Root '08-r2-qualification-summary.txt') 'classification=PASS-R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFIED'
Require-R3Text (Join-Path $r2Root '06-topology-qualification-summary.txt') 'exact-v9-phase-agreement-percent=100'
Require-R3Text (Join-Path $r2Root '06-topology-qualification-summary.txt') 'deterministic-repeat-mismatches=0'

$self=@(Import-Csv -LiteralPath (Join-Path $r2Root '02-reference-selfcheck.csv'))
$vr2=@(Import-Csv -LiteralPath (Join-Path $r2Root '03-vr2-reference-point-qualification.csv'))
$exact=@(Import-Csv -LiteralPath (Join-Path $r2Root '04-exact-v9-topology-qualification.csv'))
$seam=@(Import-Csv -LiteralPath (Join-Path $r2Root '05-seam-topology-continuity.csv'))
Require-R3 ($self.Count -eq 18) 'R2 self-check row-count drift.'
Require-R3 ($vr2.Count -eq 40) 'R2 VR2 row-count drift.'
Require-R3 ($exact.Count -eq 360) 'R2 exact-v9 topology row-count drift.'
Require-R3 ($seam.Count -eq 1280) 'R2 seam row-count drift.'
Require-R3 (@($exact | Where-Object { $_.production_resolved -ne 'true' -or $_.phase_matches -ne 'true' }).Count -eq 0) 'R2 exact-v9 topology returned evidence is not fully resolved/matched.'
Require-R3 (@($seam | Where-Object { $_.production_resolved -ne 'true' -or $_.phase_matches -ne 'true' }).Count -eq 0) 'R2 seam returned evidence is not fully resolved/matched.'

$env=$contract.future_r3_gate.inherited_exact_v9_health_envelope
Require-R3 ([int]$contract.future_r3_gate.baseline_equivalence_steps -eq 128) 'R3 baseline-equivalence step count drift.'
Require-R3 ([int]$contract.future_r3_gate.mode2_shadow_steps -eq 12000) 'R3 short workload step count drift.'
Require-R3 ([double]$contract.future_r3_gate.simulated_seconds -eq 120.0) 'R3 short workload duration drift.'
Require-R3 ([double]$contract.future_r3_gate.fixed_timestep_ms -eq 10.0) 'R3 fixed timestep drift.'
Require-R3 ([double]$env.electrical_export_mwe.minimum -eq 4.99 -and [double]$env.electrical_export_mwe.maximum -eq 5.01) 'Electrical envelope drift.'
Require-R3 ([double]$env.primary_pump_mass_flow_kg_s.minimum -eq 99.9 -and [double]$env.primary_pump_mass_flow_kg_s.maximum -eq 100.1) 'Primary-flow envelope drift.'
Require-R3 ([double]$env.drum_level_fraction.minimum -eq 0.49 -and [double]$env.drum_level_fraction.maximum -eq 0.51) 'Drum-level envelope drift.'
Require-R3 ([double]$env.governor_output_percent.minimum -eq 29.27 -and [double]$env.governor_output_percent.maximum -eq 29.30) 'Governor envelope drift.'
Require-R3 ([double]$env.maximum_mass_closure_residual_kg -eq 1E-06) 'Mass closure ceiling drift.'
Require-R3 ([double]$env.maximum_full_energy_closure_residual_j -eq 1E-02) 'Energy closure ceiling drift.'
Require-R3 ([double]$env.maximum_balance_mass_rate_residual_kg_s -eq 1E-08) 'Balance mass-rate ceiling drift.'
Require-R3 ([double]$env.maximum_balance_power_residual_w -eq 1E-03) 'Balance power ceiling drift.'
Require-R3 ([double]$env.maximum_stage_energy_ownership_residual_w -eq 1E-03) 'Stage ownership ceiling drift.'
Require-R3 ([double]$env.maximum_commanded_transfer_mismatch_kg_s -eq 1E-08) 'Transfer mismatch ceiling drift.'
Require-R3 ([int]$env.trip_steps -eq 0 -and [int]$env.breaker_open_steps -eq 0 -and [int]$env.rollbacks -eq 0) 'R3 zero-event contract drift.'

Require-R3 (-not [bool]$contract.future_r3_gate.production_source_change_allowed) 'R3 production source change forbidden.'
Require-R3 (-not [bool]$contract.future_r3_gate.existing_test_change_allowed) 'R3 historical test mutation forbidden.'
Require-R3 ([int]$contract.future_r3_gate.new_test_file_count -eq 1) 'R3 future test file-count drift.'
Require-R3 (-not [bool]$contract.future_r3_gate.r4_long_materiality_allowed) 'R3 may not absorb R4 long materiality.'
Require-R3 ([int]$contract.future_r3_gate.required_files -eq 7) 'R3 execution evidence file-count drift.'
Require-R3 ([bool]$contract.future_r3_gate.returned_adjudication_required_before_r4_planning) 'Returned R3 adjudication must precede R4 planning.'
Require-R3 ($contract.future_r3_gate.post_successor -eq 'R4-P1B-EQUIVALENT-LONG-MATERIALITY-RECHECK-PLANNING1') 'R3 successor drift.'

foreach($p in $contract.authority.PSObject.Properties){ Require-R3 (-not [bool]$p.Value) ("Planning authority must remain false: {0}" -f $p.Name) }

$futureTest=Join-Path $repoRoot ($contract.future_r3_gate.new_test_files[0].Replace('/','\'))
Require-R3 (-not (Test-Path -LiteralPath $futureTest)) 'Planning candidate must not contain future R3 test source.'

foreach($prop in $contract.documentation_files.PSObject.Properties){
    $p=Join-Path $repoRoot ([string]$prop.Value.path).Replace('/','\')
    Require-R3 ((Get-R3NormalizedTextSha256 $p) -eq [string]$prop.Value.normalized_sha256) ("R3 planning document hash drift: {0}" -f $prop.Name)
}
$validatorPath=Join-Path $repoRoot ($contract.planning_gate_files.validator.path.Replace('/','\'))
$runnerPath=Join-Path $repoRoot ($contract.planning_gate_files.runner.path.Replace('/','\'))
Require-R3 ((Get-R3NormalizedTextSha256 $validatorPath) -eq [string]$contract.planning_gate_files.validator.normalized_sha256) 'R3 planning validator hash drift.'
Require-R3 ((Get-R3NormalizedTextSha256 $runnerPath) -eq [string]$contract.planning_gate_files.runner.normalized_sha256) 'R3 planning runner hash drift.'

$main=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1.md'
$pre=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1_PREEXECUTION_REVIEW.md'
$ret=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md'
Require-R3Text $main '128 running steps'
Require-R3Text $main '120 simulated seconds'
Require-R3Text $main 'R4-P1B-EQUIVALENT-LONG-MATERIALITY-RECHECK-PLANNING1'
Require-R3Text $pre 'only allowed factor change'
Require-R3Text $ret 'NOT YET RETURNED'

$artifactDir=Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification-planning1'
if(Test-Path -LiteralPath $artifactDir){Remove-Item -LiteralPath $artifactDir -Recurse -Force}
New-Item -ItemType Directory -Path $artifactDir | Out-Null
$utf8=New-Object System.Text.UTF8Encoding($false)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), @(
 'status=PASS-AS-AUTHORED',
 'gate=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1',
 'planning-only=True',
 'r2-returned-adjudication=PASS',
 'r2-classification=PASS-R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFIED',
 'production-src-change=False',
 'tests-change=False',
 'historical-exact-v9-change=False',
 'mode2-default-activation=False',
 'r3-execution-authorized=False'
),$utf8)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-shadow-composition-matrix.csv'), @(
 'path,closure_mode,role,acceptance',
 'CANONICAL-EXACT-V9,CorrelationConsistentInverseDomain,HISTORICAL-CANONICAL,IMMUTABLE',
 'SHADOW-BASELINE,CorrelationConsistentInverseDomain,EQUIVALENCE-PROOF,128-STEPS-ZERO-FINGERPRINT-MISMATCH',
 'SHADOW-MODE2,ReferenceConsistentTabulatedInverseDomain,SCORED-SINGLE-FACTOR-CANDIDATE,120S-HEALTH+OWNERSHIP+DETERMINISM'
),$utf8)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-acceptance-and-successor-summary.txt'), @(
 'status=PASS-R3-PLANNING-CONTRACT-FROZEN',
 'baseline-equivalence-steps=128',
 'baseline-fingerprint-mismatches-allowed=0',
 'mode2-shadow-simulated-seconds=120',
 'mode2-shadow-steps=12000',
 'fixed-timestep-ms=10',
 'electrical-range-mwe=4.99..5.01',
 'primary-pump-range-kg-s=99.9..100.1',
 'drum-level-range=0.49..0.51',
 'governor-output-range-percent=29.27..29.30',
 'trip-steps=0',
 'breaker-open-steps=0',
 'rollbacks=0',
 'fallback-commit-violations=0',
 'unsafe-commit-violations=0',
 'untargeted-branch-disagreements=0',
 'max-commanded-transfer-mismatch-kg-s=1E-08',
 'max-stage-energy-ownership-residual-w=1E-03',
 'max-network-mass-closure-kg=1E-06',
 'max-network-energy-closure-j=1E-02',
 'max-network-balance-mass-rate-kg-s=1E-08',
 'max-network-balance-power-w=1E-03',
 'mode2-deterministic-repeat-steps=128',
 'future-gate=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION1',
 'future-required-files=7',
 'r3-execution-authorized=False',
 'post-r3-successor=R4-P1B-EQUIVALENT-LONG-MATERIALITY-RECHECK-PLANNING1',
 'next-action=RETURN-COMPLETE-R3-PLANNING-ARTIFACTS-FOR-ADJUDICATION'
),$utf8)

[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), @(
 'status=STATIC-PREEXECUTION-REVIEW-PASS',
 'finding-1=R2-RETURNED-EVIDENCE-ADJUDICATED-PASS',
 'finding-2=CANONICAL-EXACT-V9-REMAINS-IMMUTABLE',
 'finding-3=SHADOW-BASELINE-MUST-MATCH-CANONICAL-FOR-128-STEPS',
 'finding-4=MODE2-IS-THE-ONLY-SCORED-FACTOR-CHANGE',
 'finding-5=SHORT-WORKLOAD-120S-12000-STEPS-AT-5MWE',
 'finding-6=HISTORICAL-EXACT-V9-HEALTH-ENVELOPE-INHERITED-NOT-WIDENED',
 'finding-7=MODE2-SHADOW-DETERMINISTIC-REPEAT-REQUIRED',
 'finding-8=R4-OWNS-P1B-5TO6MWE-LONG-MATERIALITY',
 'finding-9=FUTURE-R3-ADDS-ONE-APPLICATION-TEST-ONLY',
 'finding-10=NO-DEFAULT-OR-EXACT-V9-ACTIVATION',
 'future-r3-test-contained=False',
 'r3-execution-authorized=False',
 'r4-planning-authorized=False'
),$utf8)

$files=@(Get-ChildItem -LiteralPath $artifactDir -File)
Require-R3 ($files.Count -eq 4) 'R3 Planning 1 must emit exactly four planning artifacts.'
Write-Host 'R3 Short Exact-v9-Equivalent Shadow / Composition Requalification Planning 1 static audit: PASS-AS-AUTHORED' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f $artifactDir)
