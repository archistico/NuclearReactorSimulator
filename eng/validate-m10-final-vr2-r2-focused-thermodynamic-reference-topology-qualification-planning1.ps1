$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Require-Text([string]$Path,[string]$Needle) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    $text=[System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8)
    Require ($text.IndexOf($Needle,[System.StringComparison]::Ordinal) -ge 0) ("Required marker not found in {0}: {1}" -f $Path,$Needle)
}
function Get-Sha256([byte[]]$Bytes) { $s=[System.Security.Cryptography.SHA256]::Create(); try { ([BitConverter]::ToString($s.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant() } finally { $s.Dispose() } }
function Get-FileSha256([string]$Path) { Get-Sha256 ([System.IO.File]::ReadAllBytes($Path)) }
function Get-NormalizedTextSha256([string]$Path) { $t=[System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8).Replace("`r`n","`n").Replace("`r","`n"); Get-Sha256 ([System.Text.Encoding]::UTF8.GetBytes($t)) }
function Get-TreeSha256([string]$Root,[string[]]$ExcludeDirectoryNames=@()) {
    $resolved=(Resolve-Path -LiteralPath $Root).Path
    $excluded=New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach($name in $ExcludeDirectoryNames){ [void]$excluded.Add($name) }
    $paths=New-Object 'System.Collections.Generic.List[string]'
    foreach($file in @(Get-ChildItem -LiteralPath $resolved -Recurse -File)) {
        $rel=$file.FullName.Substring($resolved.Length+1).Replace('\','/')
        $segments=@($rel.Split('/')); $skip=$false
        for($i=0;$i -lt ($segments.Count-1);$i++){ if($excluded.Contains($segments[$i])){$skip=$true;break} }
        if(-not $skip){$paths.Add($rel)}
    }
    $paths.Sort([System.StringComparer]::Ordinal)
    $ms=New-Object System.IO.MemoryStream
    try {
        foreach($rel in $paths){
            $rb=[System.Text.Encoding]::UTF8.GetBytes($rel); $ms.Write($rb,0,$rb.Length); $ms.WriteByte(0)
            $sha=[System.Security.Cryptography.SHA256]::Create(); $fs=[System.IO.File]::OpenRead((Join-Path $resolved $rel.Replace('/','\')))
            try{$fh=$sha.ComputeHash($fs)}finally{$fs.Dispose();$sha.Dispose()}
            $ms.Write($fh,0,$fh.Length); $ms.WriteByte(10)
        }
        $ms.Position=0; $s2=[System.Security.Cryptography.SHA256]::Create()
        try{$th=$s2.ComputeHash($ms)}finally{$s2.Dispose()}
        @{Hash=([BitConverter]::ToString($th)).Replace('-','').ToUpperInvariant();Count=$paths.Count}
    } finally {$ms.Dispose()}
}

$repoRoot=Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$contractPath=Join-Path $repoRoot 'eng\m10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification-planning1-contract.json'
$contract=Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification-planning1-v1') 'R2 Planning 1 schema mismatch.'
Require ($contract.status -eq 'PLANNING-ONLY') 'R2 Planning 1 status mismatch.'
Require ($contract.prerequisite.r1_returned_adjudication -eq 'PASS') 'R1 returned adjudication is not PASS.'
Require ($contract.prerequisite.production_mode -eq 'ReferenceConsistentTabulatedInverseDomain') 'Mode-2 identity drift.'
Require ([int]$contract.prerequisite.production_mode_value -eq 2) 'Mode-2 numeric identity drift.'
Require ($contract.prerequisite.payload_sha256 -eq 'EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267') 'Payload prerequisite hash drift.'
Require (-not [bool]$contract.prerequisite.default_activation) 'Default activation must remain false.'
Require (-not [bool]$contract.prerequisite.exact_v9_activation) 'Exact-v9 activation must remain false.'

$validatorPath=Join-Path $repoRoot ($contract.planning_gate_files.validator.path.Replace('/','\'))
$runnerPath=Join-Path $repoRoot ($contract.planning_gate_files.runner.path.Replace('/','\'))
Require ((Get-NormalizedTextSha256 $validatorPath) -eq $contract.planning_gate_files.validator.normalized_sha256) 'R2 Planning validator hash drift.'
Require ((Get-NormalizedTextSha256 $runnerPath) -eq $contract.planning_gate_files.runner.normalized_sha256) 'R2 Planning runner hash drift.'

$returnAudit=Join-Path $repoRoot ($contract.prerequisite.returned_audit.Replace('/','\'))
Require ((Get-FileSha256 $returnAudit) -eq $contract.prerequisite.returned_audit_sha256) 'Frozen R1 returned-audit hash drift.'
Require-Text $returnAudit 'status=PASS'
Require-Text $returnAudit 'r2-planning-authorized=True'
Require-Text $returnAudit 'r2-execution-authorized=False'

$r1Root=Join-Path $repoRoot ($contract.r1_returned_artifacts.root.Replace('/','\'))
$r1Props=@($contract.r1_returned_artifacts.sha256.PSObject.Properties)
Require ($r1Props.Count -eq 7) 'R1 returned artifact manifest must contain exactly seven files.'
foreach($prop in $r1Props){
    $p=Join-Path $r1Root $prop.Name
    Require (Test-Path -LiteralPath $p -PathType Leaf) ("Missing frozen R1 artifact: {0}" -f $prop.Name)
    Require ((Get-FileSha256 $p) -eq [string]$prop.Value) ("Frozen R1 artifact hash drift: {0}" -f $prop.Name)
}
Require-Text (Join-Path $r1Root '03-reference-data-provenance.txt') 'byte-identical-to-checked-in=True'
Require-Text (Join-Path $r1Root '04-c4-production-equivalence-summary.txt') 'total-comparisons=1967'
Require-Text (Join-Path $r1Root '04-c4-production-equivalence-summary.txt') 'state-bit-mismatches=0'
Require-Text (Join-Path $r1Root '06-r1-implementation-summary.txt') 'ordinary-release-suite=PASS'

$src=Get-TreeSha256 (Join-Path $repoRoot 'src') @('bin','obj')
Require ($src.Count -eq [int]$contract.baseline.src_file_count) 'R2 planning src file-count drift.'
Require ($src.Hash -eq $contract.baseline.src_tree_sha256) 'R2 planning src tree drift.'
$tests=Get-TreeSha256 (Join-Path $repoRoot 'tests') @('bin','obj')
Require ($tests.Count -eq [int]$contract.baseline.tests_file_count) 'R2 planning tests file-count drift.'
Require ($tests.Hash -eq $contract.baseline.tests_tree_sha256) 'R2 planning tests tree drift.'

$productionPaths=@{
 simulation_project='src\NuclearReactorSimulator.Simulation\NuclearReactorSimulator.Simulation.csproj'
 closure_mode='src\NuclearReactorSimulator.Simulation\Physics\Fluids\WaterSteamThermodynamicClosureMode.cs'
 production_model='src\NuclearReactorSimulator.Simulation\Physics\Fluids\SimplifiedWaterSteamThermodynamicModel.cs'
 mode2_resolver='src\NuclearReactorSimulator.Simulation\Physics\Fluids\ReferenceConsistentTabulatedInverseResolver.cs'
 payload='src\NuclearReactorSimulator.Simulation\Physics\Fluids\ReferenceData\NRSVR2C4.v1.bin'
}
foreach($key in $productionPaths.Keys){
    $p=Join-Path $repoRoot $productionPaths[$key]
    Require ((Get-FileSha256 $p) -eq [string]$contract.baseline.production_files_sha256.$key) ("Production prerequisite hash drift: {0}" -f $key)
}
$mode=Join-Path $repoRoot $productionPaths.closure_mode
Require-Text $mode 'HistoricalCorrelationTopology = 0'
Require-Text $mode 'CorrelationConsistentInverseDomain = 1'
Require-Text $mode 'ReferenceConsistentTabulatedInverseDomain = 2'
$payload=Join-Path $repoRoot $productionPaths.payload
Require ((Get-FileSha256 $payload) -eq 'EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267') 'Production payload hash drift.'

$refHelper=Join-Path $repoRoot ($contract.reference_contract.helper.Replace('/','\'))
Require ((Get-FileSha256 $refHelper) -eq $contract.baseline.reference_helper_sha256) 'Independent IF97 helper drift.'
Require (-not [bool]$contract.reference_contract.calls_production_model) 'Independent reference helper may not call production.'
Require ([double]$contract.reference_contract.selfcheck_maximum_relative_error -eq 1e-8) 'Reference self-check ceiling drift.'
Require ([double]$contract.reference_contract.vr2_blocking_maximum_relative_error -eq 0.25) 'VR2 blocking ceiling drift.'
Require ([double]$contract.reference_contract.planning1_pressure_target_maximum_relative_error -eq 0.10) 'Planning 1 pressure target drift.'
Require (-not [bool]$contract.reference_contract.threshold_change_allowed) 'R2 may not change thresholds.'
Require (-not [bool]$contract.reference_contract.c4_output_is_physical_oracle) 'C4 output must not become the R2 physical oracle.'

$vr2Path=Join-Path $repoRoot ($contract.frozen_corpora.vr2_reference.path.Replace('/','\'))
$exactPath=Join-Path $repoRoot ($contract.frozen_corpora.exact_v9_nodes.path.Replace('/','\'))
$seamPath=Join-Path $repoRoot ($contract.frozen_corpora.seam_map.path.Replace('/','\'))
$hydPath=Join-Path $repoRoot ($contract.frozen_corpora.hydraulic_context.path.Replace('/','\'))
foreach($pair in @(@($vr2Path,$contract.frozen_corpora.vr2_reference.sha256),@($exactPath,$contract.frozen_corpora.exact_v9_nodes.sha256),@($seamPath,$contract.frozen_corpora.seam_map.sha256),@($hydPath,$contract.frozen_corpora.hydraulic_context.sha256))){ Require ((Get-FileSha256 $pair[0]) -eq [string]$pair[1]) ("Frozen corpus hash drift: {0}" -f $pair[0]) }

$vr2=@(Import-Csv -LiteralPath $vr2Path)
Require ($vr2.Count -eq 40) 'VR2 reference corpus row-count drift.'
$inv=@($vr2 | Where-Object { -not [string]::IsNullOrWhiteSpace($_.specific_volume_m3_kg) })
Require ($inv.Count -eq 39) 'VR2 inverse-applicable row-count drift.'
$boundary=@($vr2 | Where-Object { $_.point_id -eq 'VR2-SAT-360C-PONLY' })
Require ($boundary.Count -eq 1) 'VR2 boundary-only point drift.'

$exact=@(Import-Csv -LiteralPath $exactPath)
Require ($exact.Count -eq 360) 'Exact-v9 node corpus row-count drift.'
$expectedNodes=@('suction','pressure','outlet','drum','feedwater-inventory')
foreach($n in $expectedNodes){ Require (@($exact | Where-Object {$_.node_id -eq $n}).Count -eq 72) ("Exact-v9 node cardinality drift: {0}" -f $n) }

$seam=@(Import-Csv -LiteralPath $seamPath)
Require ($seam.Count -eq 1280) 'Seam corpus row-count drift.'
$boundaries=@($seam | Select-Object -ExpandProperty boundary_index -Unique)
Require ($boundaries.Count -eq 320) 'Seam boundary-count drift.'
foreach($side in @('R1-SIDE','R4-LIQUID-SIDE','R4-VAPOR-SIDE','R2-SIDE')){ Require (@($seam | Where-Object {$_.probe_side -eq $side}).Count -eq 320) ("Seam side cardinality drift: {0}" -f $side) }
Require (@(Import-Csv -LiteralPath $hydPath).Count -eq 288) 'Hydraulic context row-count drift.'
Require (-not [bool]$contract.frozen_corpora.hydraulic_context.r2_execution_owner) 'R2 must not absorb R4 long-materiality ownership.'

$c=$contract.future_r2_gate
Require ($c.id -eq 'R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION1') 'Future R2 gate ID drift.'
Require (-not [bool]$c.production_source_change_allowed) 'Future R2 may not change production source.'
Require (-not [bool]$c.existing_test_change_allowed) 'Future R2 must add a focused test rather than rewriting historical tests.'
Require ([int]$c.new_test_file_count -eq 1) 'Future R2 new test-file count drift.'
Require ([bool]$c.ordinary_release_suite_required) 'Ordinary Release suite remains mandatory.'
Require ([bool]$c.reference_selfcheck_required) 'Reference self-check remains mandatory.'
Require ([int]$c.vr2_rows -eq 40 -and [int]$c.vr2_inverse_rows -eq 39) 'Future R2 VR2 matrix cardinality drift.'
Require ([int]$c.exact_v9_rows -eq 360) 'Future R2 exact-v9 row-count drift.'
Require ([int]$c.seam_rows -eq 1280 -and [int]$c.seam_boundary_count -eq 320) 'Future R2 seam cardinality drift.'
Require ([int]$c.vr2_unresolved_allowed -eq 0 -and [int]$c.vr2_phase_mismatch_allowed -eq 0) 'VR2 unresolved/phase allowance drift.'
Require ([int]$c.exact_v9_unresolved_allowed -eq 0 -and [int]$c.exact_v9_phase_mismatch_allowed -eq 0) 'Exact-v9 unresolved/phase allowance drift.'
Require ([double]$c.exact_v9_phase_agreement_percent_required -eq 100.0) 'Exact-v9 phase agreement target drift.'
Require ([int]$c.seam_unresolved_allowed -eq 0 -and [int]$c.seam_phase_mismatch_allowed -eq 0) 'Seam unresolved/phase allowance drift.'
Require ([double]$c.continuity_ceilings.max_liquid_seam_pressure_jump_mpa -eq 0.0005137228525280754) 'Liquid seam pressure continuity ceiling drift.'
Require ([double]$c.continuity_ceilings.max_liquid_seam_temperature_jump_c -eq 3.5596193356468575E-05) 'Liquid seam temperature continuity ceiling drift.'
Require ([double]$c.continuity_ceilings.max_vapor_seam_pressure_jump_mpa -eq 0.00026625516826150886) 'Vapor seam pressure continuity ceiling drift.'
Require ([double]$c.continuity_ceilings.max_vapor_seam_temperature_jump_c -eq 0.001903154074568647) 'Vapor seam temperature continuity ceiling drift.'
Require ([bool]$c.deterministic_repeat_required) 'Deterministic repeat remains mandatory.'
Require (-not [bool]$c.scenario_execution_allowed) 'R2 may not run a scenario.'
Require (-not [bool]$c.exact_v9_composition_allowed) 'R2 may not compose mode 2 into exact-v9.'
Require (-not [bool]$c.hydraulic_long_materiality_execution_allowed) 'R2 may not run R4 long materiality.'
Require ([int]$c.required_files -eq 9) 'Future R2 evidence file-count drift.'
Require ([bool]$c.returned_adjudication_required_before_r3) 'Returned R2 adjudication must precede R3 planning.'
Require ($c.post_successor -eq 'R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1') 'R2 successor drift.'
foreach($p in $contract.authority.PSObject.Properties){ Require (-not [bool]$p.Value) ("Planning authority must remain false: {0}" -f $p.Name) }

$futureTest=Join-Path $repoRoot ($c.new_test_files[0].Replace('/','\'))
Require (-not (Test-Path -LiteralPath $futureTest)) 'Planning candidate must not contain future R2 test source.'

$main=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R2_FOCUSED_THERMODYNAMIC_REFERENCE_TOPOLOGY_QUALIFICATION_PLANNING1.md'
$pre=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R2_FOCUSED_THERMODYNAMIC_REFERENCE_TOPOLOGY_QUALIFICATION_PLANNING1_PREEXECUTION_REVIEW.md'
$ret=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R2_FOCUSED_THERMODYNAMIC_REFERENCE_TOPOLOGY_QUALIFICATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md'
$adr=Join-Path $repoRoot 'docs\adr\0213-requalify-production-mode2-against-independent-if97-and-frozen-topology-before-exact-v9-composition.md'
Require-Text $main 'C4 shadow implementation is not the acceptance oracle'
Require-Text $main '39 inverse-applicable rows'
Require-Text $main '360 committed `(v,u)` node states'
Require-Text $main '1,280 seam probes'
Require-Text $main 'max liquid-side pressure jump <= 0.0005137228525280754 MPa'
Require-Text $main 'R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1'
Require-Text $pre 'future R2 implementation adds exactly one test source'
Require-Text $ret 'NOT YET RETURNED'
Require-Text $adr 'C4/shadow outputs are prerequisite provenance, not the R2 acceptance oracle'
foreach($property in $contract.documentation_files.PSObject.Properties){
    $docRelativePath=([string]$property.Value.path).Replace('/','\')
    $docPath=Join-Path $repoRoot $docRelativePath
    Require ((Get-NormalizedTextSha256 $docPath) -eq [string]$property.Value.normalized_sha256) ("R2 planning document hash drift: {0}" -f $property.Name)
}

$artifactDir=Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-r2-focused-thermodynamic-reference-topology-qualification-planning1'
if(Test-Path -LiteralPath $artifactDir){Remove-Item -LiteralPath $artifactDir -Recurse -Force}
New-Item -ItemType Directory -Path $artifactDir | Out-Null
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), @(
 'status=PASS-AS-AUTHORED',
 'gate=R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION-PLANNING1',
 'planning-only=True',
 'r1-returned-adjudication=PASS',
 'production-mode=ReferenceConsistentTabulatedInverseDomain',
 'production-mode-value=2',
 'payload-sha256=EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267',
 'independent-reference=IAPWS-R7-97-2012-REGIONS-1-2-4',
 'c4-is-r2-physical-oracle=False',
 'production-src-change=False',
 'tests-change=False',
 'r2-execution-authorized=False'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-qualification-matrix.csv'), @(
 'domain,rows,acceptance,execution_owner',
 'VR2-REFERENCE,40,39-INVERSE-RESOLVED+0-PHASE-MISMATCH+25PCT-BLOCKING+10PCT-PRESSURE-TARGET+1-BOUNDARY-PRESSURE,R2',
 'EXACT-V9-FROZEN-STATES,360,360-RESOLVED+100PCT-PHASE-AGREEMENT+25PCT-BLOCKING+10PCT-HOT-PRESSURE,R2-STATE-TOPOLOGY-ONLY',
 'SEAM-TOPOLOGY,1280,320x4-RESOLVED+0-PHASE-MISMATCH+CONTINUITY-CEILINGS,R2',
 'HYDRAULIC-CONTEXT,288,FROZEN-INPUT-ONLY-NOT-EXECUTED,R4'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-acceptance-and-successor-summary.txt'), @(
 'status=PASS-R2-PLANNING-CONTRACT-FROZEN',
 'reference-selfcheck-max=1E-08',
 'vr2-blocking-max-relative-error=0.25',
 'planning1-pressure-target-max-relative-error=0.10',
 'vr2-inverse-unresolved-allowed=0',
 'vr2-phase-mismatch-allowed=0',
 'exact-v9-unresolved-allowed=0',
 'exact-v9-phase-mismatch-allowed=0',
 'exact-v9-phase-agreement-percent-required=100',
 'seam-unresolved-allowed=0',
 'seam-phase-mismatch-allowed=0',
 'liquid-seam-pressure-jump-max-mpa=0.0005137228525280754',
 'liquid-seam-temperature-jump-max-c=3.5596193356468575E-05',
 'vapor-seam-pressure-jump-max-mpa=0.00026625516826150886',
 'vapor-seam-temperature-jump-max-c=0.001903154074568647',
 'future-gate=R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION1',
 'future-required-files=9',
 'r2-execution-authorized=False',
 'post-r2-successor=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1',
 'next-action=RETURN-COMPLETE-R2-PLANNING-ARTIFACTS-FOR-ADJUDICATION'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preexecution-review.txt'), @(
 'status=STATIC-PREEXECUTION-REVIEW-PASS',
 'finding-1=R1-RETURNED-EVIDENCE-ADJUDICATED-PASS',
 'finding-2=MODE2-REMAINS-OPT-IN-ONLY',
 'finding-3=INDEPENDENT-IF97-IS-R2-PHYSICAL-ORACLE',
 'finding-4=VR2-40-ROW-CORPUS-FROZEN',
 'finding-5=EXACT-V9-360-STATE-TOPOLOGY-CORPUS-FROZEN',
 'finding-6=SEAM-1280-ROW-320-BOUNDARY-CORPUS-FROZEN',
 'finding-7=25PCT-VR2-BLOCKING-AND-10PCT-PLANNING-TARGET-UNCHANGED',
 'finding-8=SEAM-CONTINUITY-NONREGRESSION-CEILINGS-FROZEN',
 'finding-9=NO-PRODUCTION-OR-EXACT-V9-COMPOSITION-IN-R2',
 'finding-10=R3-PLANNING-BLOCKED-PENDING-RETURNED-R2-EVIDENCE',
 'future-r2-test-contained=False',
 'r2-execution-authorized=False',
 'default-activation-authorized=False',
 'exact-v9-activation-authorized=False'
),[System.Text.Encoding]::UTF8)
$files=@(Get-ChildItem -LiteralPath $artifactDir -File)
Require ($files.Count -eq 4) 'R2 Planning 1 must emit exactly four planning artifacts.'
Write-Host 'R2 Focused Thermodynamic / Reference / Topology Qualification Planning 1 static audit: PASS-AS-AUTHORED' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f $artifactDir)
