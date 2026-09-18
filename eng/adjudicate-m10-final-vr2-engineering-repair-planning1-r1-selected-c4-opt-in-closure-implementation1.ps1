$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$inv = [Globalization.CultureInfo]::InvariantCulture
function Fail([string]$Message) { throw $Message }
function Require([bool]$Condition,[string]$Message) { if (-not $Condition) { Fail $Message } }
function Read-Kv([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    $map = @{}
    foreach ($line0 in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path))) {
        $line = $line0.TrimStart([char]0xFEFF)
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $index = $line.IndexOf('=')
        if ($index -lt 1) { continue }
        $map[$line.Substring(0,$index)] = $line.Substring($index+1)
    }
    return $map
}
function I([hashtable]$Map,[string]$Key) { Require ($Map.ContainsKey($Key)) ("Missing key {0}" -f $Key); return [int64]::Parse($Map[$Key],$inv) }
function S([hashtable]$Map,[string]$Key) { Require ($Map.ContainsKey($Key)) ("Missing key {0}" -f $Key); return [string]$Map[$Key] }
function Write-Lines([string]$Path,[string[]]$Lines) { [IO.File]::WriteAllLines($Path,$Lines,(New-Object Text.UTF8Encoding($false))) }
function Sha([string]$Path) {
    $sha=[Security.Cryptography.SHA256]::Create(); try { $s=[IO.File]::OpenRead((Resolve-Path $Path)); try { return ([BitConverter]::ToString($sha.ComputeHash($s))).Replace('-','') } finally {$s.Dispose()} } finally {$sha.Dispose()}
}
$contract = ([IO.File]::ReadAllText((Resolve-Path 'eng\m10-final-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1-contract.json'))) | ConvertFrom-Json
$artifactRoot = Join-Path $root ([string]$contract.evidence.artifact_root).Replace('/',[IO.Path]::DirectorySeparatorChar)
Require (Test-Path -LiteralPath $artifactRoot -PathType Container) 'R1 artifact root missing.'
Require ($env:NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1_ORDINARY_PASS -eq '1') 'Ordinary Release PASS marker missing from runner.'
$prov=Read-Kv (Join-Path $artifactRoot '03-reference-data-provenance.txt')
$eq=Read-Kv (Join-Path $artifactRoot '04-c4-production-equivalence-summary.txt')
$hist=Read-Kv (Join-Path $artifactRoot '05-historical-mode-regression-summary.txt')
Require ((S $prov 'status') -eq 'PASS-REFERENCE-DATA-PROVENANCE') 'Reference-data provenance status mismatch.'
Require ((S $prov 'payload-sha256') -eq $contract.implementation.reference_payload_sha256) 'Payload provenance SHA mismatch.'
Require ((I $prov 'payload-bytes') -eq [int64]$contract.implementation.reference_payload_size_bytes) 'Payload provenance length mismatch.'
Require ((S $prov 'byte-identical-two-regenerations') -eq 'True') 'Two-regeneration reproducibility failed.'
Require ((S $prov 'byte-identical-to-checked-in') -eq 'True') 'Generated payload differs from checked-in payload.'
Require ((S $prov 'compiled-sha256-match') -eq 'True') 'Payload differs from compiled SHA authority.'
Require ((S $eq 'status') -eq 'PASS-C4-PRODUCTION-EQUIVALENCE') 'C4 production equivalence status mismatch.'
Require ((I $eq 'state-comparisons') -eq 1679) 'State comparison count mismatch.'
Require ((I $eq 'hydraulic-comparisons') -eq 288) 'Hydraulic comparison count mismatch.'
Require ((I $eq 'total-comparisons') -eq 1967) 'Total comparison count mismatch.'
foreach ($key in @('state-bit-mismatches','hydraulic-bit-mismatches','repeat-mismatches','resolve-mismatches','resolution-path-mismatches','model-integration-mismatches','resolve-allocation-bytes','payload-runtime-io-count','payload-decode-count')) { Require ((I $eq $key) -eq 0) ("Non-zero R1 equivalence/integrity result: {0}" -f $key) }
Require ((S $hist 'status') -eq 'PASS-HISTORICAL-MODE-REGRESSION-FOCUSED') 'Historical-mode regression status mismatch.'
Require ((I $hist 'historical-mode0-comparisons') -eq 360) 'Mode 0 comparison count mismatch.'
Require ((I $hist 'historical-mode0-default-mismatches') -eq 0) 'Default vs mode 0 regression mismatch.'
Require ((I $hist 'historical-mode1-comparisons') -eq 360) 'Mode 1 observational comparison count mismatch.'
Require ((S $hist 'historical-mode1-density-derived-input-observation-only') -eq 'True') 'Mode 1 density-derived comparison was incorrectly promoted to authority.'
Require ((S $hist 'historical-mode1-regression-basis') -eq 'LEGACY-SOURCE-PROJECTION-SHA256+ORDINARY-RELEASE-SUITE') 'Mode 1 regression basis mismatch.'
Require ((S $hist 'historical-mode1-baseline-production-model-sha256') -eq '93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E') 'Mode 1 baseline production-model SHA mismatch.'
Require ((S $hist 'historical-mode1-legacy-source-projection-sha256') -eq '93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E') 'Mode 1 legacy source projection SHA mismatch.'
Require ((S $hist 'historical-mode1-legacy-source-projection-match') -eq 'True') 'Mode 1 legacy source projection did not match the frozen Planning 1 source.'

Write-Lines (Join-Path $artifactRoot '01-contract-and-provenance.txt') @(
 'status=PASS-R1-IMPLEMENTATION-EVIDENCE-COMPLETE',
 'gate=R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1',
 'planning-returned-adjudication=PASS',
 'selection-result=SELECT-C4',
 'selected-runtime-profile=AMBIENT-UNSET',
 'new-closure-mode=ReferenceConsistentTabulatedInverseDomain',
 'new-closure-mode-value=2',
 'default-activation=False',
 'exact-v9-activation=False',
 'production-runtime-change-authorized=False',
 'r2-planning-authorized-now=False'
)
$manifest = New-Object 'System.Collections.Generic.List[string]'
$manifest.Add('area,path,change,sha256')
foreach ($p in $contract.scope.existing_modified) { $manifest.Add(('existing-production,'+$p+',MODIFIED,'+(Sha $p))) }
foreach ($p in $contract.scope.new_production) { $manifest.Add(('new-production,'+$p+',ADDED,'+(Sha $p))) }
foreach ($p in $contract.scope.new_tests) { $manifest.Add(('new-test,'+$p+',ADDED,'+(Sha $p))) }
Write-Lines (Join-Path $artifactRoot '02-production-change-manifest.csv') $manifest.ToArray()
Write-Lines (Join-Path $artifactRoot '06-r1-implementation-summary.txt') @(
 'status=PASS-R1-SELECTED-C4-OPT-IN-IMPLEMENTATION-QUALIFIED',
 'implementation-result=PASS',
 'selected-candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
 'production-mode=ReferenceConsistentTabulatedInverseDomain',
 'production-mode-value=2',
 'payload-schema=NRSVR2C4-v1',
 ('payload-sha256='+$contract.implementation.reference_payload_sha256),
 'production-vs-c4-state-comparisons=1679',
 'production-vs-c4-hydraulic-comparisons=288',
 'production-vs-c4-total-comparisons=1967',
 'all-semantic-mismatches=0',
 'resolve-time-allocation-bytes=0',
 'payload-runtime-io-count=0',
 'payload-decode-count=0',
 'historical-mode0-regression=PASS',
 'historical-mode1-regression=PASS',
 'ordinary-release-suite=PASS',
 'default-activation=False',
 'exact-v9-activation=False',
 'next-action=RETURN-COMPLETE-R1-IMPLEMENTATION-ARTIFACTS-FOR-ADJUDICATION-BEFORE-R2-PLANNING'
)
Write-Lines (Join-Path $artifactRoot '07-prequalification-review.txt') @(
 'status=STATIC-AND-FOCUSED-R1-PREQUALIFICATION-PASS',
 'finding-1=SELECTED-C4-PRODUCTION-MODE2-IMPLEMENTED-OPT-IN-ONLY',
 'finding-2=NRSVR2C4-V1-PAYLOAD-REPRODUCIBLE-AND-HASH-ANCHORED',
 'finding-3=PRODUCTION-VS-C4-1967-COMPARISONS-ZERO-MISMATCH',
 'finding-4=RESOLVE-TIME-PAYLOAD-IO-DECODE-ALLOCATION-ZERO',
 'finding-5=HISTORICAL-MODES-0-AND-1-REGRESSION-PASS',
 'finding-6=ORDINARY-RELEASE-SUITE-PASS',
 'finding-7=DEFAULT-AND-EXACT-V9-ACTIVATION-UNCHANGED',
 'r2-planning-authorized-now=False',
 'production-default-switch-authorized=False',
 'production-runtime-change-authorized=False'
)
$fileCount=@(Get-ChildItem -LiteralPath $artifactRoot -File).Count
Require ($fileCount -eq 7) ("R1 required artifact count is 7; actual is {0}" -f $fileCount)
Write-Host 'R1 selected-C4 opt-in implementation adjudication: PASS' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f $artifactRoot)
exit 0
