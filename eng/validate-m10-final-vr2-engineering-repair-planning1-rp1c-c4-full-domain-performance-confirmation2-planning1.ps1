$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$inv = [Globalization.CultureInfo]::InvariantCulture

function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Read-Utf8Text([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path), [System.Text.Encoding]::UTF8)
}
function Normalized-Sha256([string]$Path) {
    $text = Read-Utf8Text $Path
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToUpperInvariant() }
    finally { $sha.Dispose() }
}
function File-Sha256([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    $stream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $Path))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToUpperInvariant() }
    finally { $sha.Dispose(); $stream.Dispose() }
}
function Stream-Sha256([IO.Stream]$Stream) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '').ToUpperInvariant() }
    finally { $sha.Dispose() }
}
function Require-Text([string]$Path, [string]$Needle) {
    $text = Read-Utf8Text $Path
    Require ($text.Contains($Needle)) ("Required marker not found in {0}: {1}" -f $Path,$Needle)
}
function Write-Utf8NoBom([string]$Path, [string[]]$Lines) {
    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    [IO.File]::WriteAllLines($Path, $Lines, (New-Object Text.UTF8Encoding($false)))
}

$contractPath = 'eng\m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-planning1-contract.json'
$contract = (Read-Utf8Text $contractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-planning1-v1') 'Planning contract schema mismatch.'
Require ($contract.status -eq 'CANDIDATE-PLANNING-ONLY') 'Planning contract status mismatch.'
Require ([bool]$contract.authority.fdpc2_planning_authorized) 'FDPC2 planning must be authorized.'
Require (-not [bool]$contract.authority.fdpc2_implementation_authorized) 'FDPC2 implementation must remain unauthorized.'
Require (-not [bool]$contract.authority.rp1c_selection_authorized) 'RP1C selection must remain unauthorized.'
Require (-not [bool]$contract.authority.production_runtime_change_authorized) 'Production runtime change must remain unauthorized.'
Require (-not [bool]$contract.authority.production_repair_authorized) 'Production repair must remain unauthorized.'
Require ($contract.future_gate.runtime_profile -eq 'AMBIENT-UNSET') 'Future runtime profile drift.'
Require ([bool]$contract.future_gate.same_physical_host_as_a2_required) 'Same-host requirement must remain true.'
Require ($contract.future_gate.required_host_fingerprint_sha256 -eq '931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336') 'A2 host fingerprint drift.'
Require ($contract.future_gate.required_active_power_scheme_guid -eq '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c') 'A2 power scheme drift.'
Require ([int]$contract.future_gate.fresh_process_count -eq 5) 'Future process count drift.'
Require ([int]$contract.future_gate.total_measured_calls -eq 217600) 'Future measured call count drift.'
Require ([int]$contract.future_gate.total_required_files -eq 32) 'Future required-file count drift.'
Require ([Math]::Abs(([double]$contract.corrected_performance_predicate.single_call_max_us_max) - 409.30666666666673) -lt 1e-12) 'Strict max drift.'

$a2Root = 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeConfigurationImpactAssessment1_Artifacts'
$a2Hashes = @{
 '01-contract-and-provenance.txt'='E3703784EAEF582E90E2FE8D36B462C664587D8671C620547A8F8BA4329684AF';
 '02-execution-host-provenance.txt'='05631B2383C1E8DA146221B984AAA8900D6594830823FE5D080E73BE74684FEB';
 '03-host-consistency-audit.txt'='24591955F2D0771BBB206AD7999A34809C21FD5D64CC565E8175A2059E84703F';
 '04-exact-v9-profile-summary.csv'='D1C13EF366912C590425C96F8BFA1E4ADA0DE85BF5D70956FD95D7026D5A79AB';
 '05-ordinary-suite-impact-summary.csv'='273E5F13A08F6009FED6C876CD89FF913470CEFA09A63C36FB5A9350F5F42774';
 '06-replay-determinism-impact-summary.csv'='6D46AB9CBF5B8A201FDC047A62EF3097A070DD1869E0297C2530CC623034D591';
 '07-non-vr2-performance-impact-summary.csv'='8BBE70D7EE2D8B18186D60632D246A3E6FA9DC7C067F9F93AF91C795804B3E40';
 '08-runtime-configuration-impact-assessment1-summary.txt'='8667A7BBAC574984C27BA0B4581165AE80AA5281A19154883EBC62077CF06C7D'
}
foreach ($name in $a2Hashes.Keys) { Require ((Normalized-Sha256 (Join-Path $a2Root $name)) -eq $a2Hashes[$name]) ("A2 aggregate hash mismatch: {0}" -f $name) }
Require-Text (Join-Path $a2Root '01-contract-and-provenance.txt') 'status=PASS-A2-EVIDENCE-INTEGRITY'
Require-Text (Join-Path $a2Root '03-host-consistency-audit.txt') 'status=PASS-SINGLE-HOST-INTEGRITY'
Require-Text (Join-Path $a2Root '08-runtime-configuration-impact-assessment1-summary.txt') 'ambient-calls-over-max=0'
Require-Text (Join-Path $a2Root '08-runtime-configuration-impact-assessment1-summary.txt') 'explicit-calls-over-max=1'
Require-Text (Join-Path $a2Root '08-runtime-configuration-impact-assessment1-summary.txt') 'ambient-ordinary-engineering-pass=True'
Require-Text (Join-Path $a2Root '08-runtime-configuration-impact-assessment1-summary.txt') 'ambient-replay-engineering-pass=True'

$treeManifestPath = Join-Path $a2Root '09-returned-tree-manifest.csv'
Require ((Normalized-Sha256 $treeManifestPath) -eq 'E618F2113E68440F44D431838A2EE351CE57EEB6670E7005312E72D12A0B7CF0') 'A2 returned-tree manifest hash mismatch.'
$treeRows = @(Import-Csv -LiteralPath $treeManifestPath)
Require ($treeRows.Count -eq 78) ("A2 returned-tree manifest row count mismatch: {0}" -f $treeRows.Count)
Require (@($treeRows | Where-Object { $_.storage -eq 'archive' }).Count -eq 10) 'A2 archived raw payload count mismatch.'

$archivePath = 'eng\frozen-evidence\archive\rp1c-c4-runtimeconfigurationimpactassessment1-large-payloads.zip'
Require ((File-Sha256 $archivePath) -eq 'C7A6D767665D30AE5DFA24D5858DFA1FC7001D92142DB0A07046AF1457298409') 'A2 archive pack byte hash mismatch.'
Require ((Normalized-Sha256 'eng\frozen-evidence\large-payload-manifest.csv') -eq $contract.normalized_lf_sha256.large_payload_manifest) 'Frozen large-payload manifest hash drift.'
$largeManifestRows = @(Import-Csv -LiteralPath 'eng\frozen-evidence\large-payload-manifest.csv' | Where-Object { $_.storage -eq 'bundled-compressed-archive:eng/frozen-evidence/archive/rp1c-c4-runtimeconfigurationimpactassessment1-large-payloads.zip' })
Require ($largeManifestRows.Count -eq 10) 'A2 large-payload manifest row count mismatch.'
$ordinaryLarge = @(Get-ChildItem -LiteralPath 'eng\frozen-evidence\ordinary' -File -Recurse | Where-Object { $_.Length -gt 1048576 })
Require ($ordinaryLarge.Count -eq 0) 'Frozen ordinary store contains direct payloads above 1 MiB.'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $archivePath))
try {
    foreach ($row in $treeRows) {
        $expectedHash = $row.canonical_sha256.ToUpperInvariant()
        $expectedBytes = [int64]$row.bytes
        if ($row.storage -eq 'direct') {
            $path = Join-Path $a2Root ($row.relative_path.Replace('/','\'))
            Require (Test-Path -LiteralPath $path -PathType Leaf) ("A2 direct evidence missing: {0}" -f $row.relative_path)
            Require ((Get-Item -LiteralPath $path).Length -eq $expectedBytes) ("A2 direct evidence size mismatch: {0}" -f $row.relative_path)
            Require ((File-Sha256 $path) -eq $expectedHash) ("A2 direct evidence hash mismatch: {0}" -f $row.relative_path)
        }
        elseif ($row.storage -eq 'archive') {
            $entryName = 'M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeConfigurationImpactAssessment1_Artifacts/' + $row.relative_path
            $entry = $zip.GetEntry($entryName)
            Require ($null -ne $entry) ("A2 archive entry missing: {0}" -f $row.relative_path)
            Require ([int64]$entry.Length -eq $expectedBytes) ("A2 archive entry size mismatch: {0}" -f $row.relative_path)
            $stream = $entry.Open()
            try { $actualHash = Stream-Sha256 $stream } finally { $stream.Dispose() }
            Require ($actualHash -eq $expectedHash) ("A2 archive entry hash mismatch: {0}" -f $row.relative_path)
        }
        else { throw ("Unexpected A2 tree-manifest storage value: {0}" -f $row.storage) }
    }
}
finally { $zip.Dispose() }

Require ((Normalized-Sha256 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs') -eq '6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6') 'C4 candidate drift.'
Require ((Normalized-Sha256 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\03-exact-v9-node-corpus.csv') -eq '6EB6AEA3BF45621BD9BB890E3449E9BB010E02C3CF5A81AFDAFF67809DE4B50E') 'Exact-v9 corpus drift.'
Require ((Normalized-Sha256 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\05-seam-probe-map.csv') -eq '9035396FDB5A1F6AD89F0B9D28A26992D577906812DE412EFA2AABA98FEBE47D') 'Seam corpus drift.'
Require ((Normalized-Sha256 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts\06-performance-baseline.csv') -eq '91776709A34142E9312A092F3B19EAFCF0FCA576D561F296CC9272FCDC4F7DEB') 'Performance baseline drift.'

$fdpc1Root = 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_FullDomainPerformanceConfirmation1_Artifacts'
Require-Text (Join-Path $fdpc1Root '05-rp1c-c4-full-domain-performance-confirmation1-summary.txt') 'classification=C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED'
Require-Text 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1_RETURNED_EVIDENCE_ADJUDICATION.md' 'A2-CLASSIFICATION=AMBIENT-UNSET-PROJECT-QUALIFICATION-SUFFICIENT-ON-A2-HOST'
Require-Text 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1_RETURNED_EVIDENCE_ADJUDICATION.md' 'FDPC2-PLANNING-AUTHORIZED=True'
Require-Text 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1.md' 'AMBIENT-UNSET'
Require-Text 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1.md' '217,600 calls'
Require-Text 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1_PREEXECUTION_REVIEW.md' 'STATIC PRE-EXECUTION REVIEW: PASS'
Require-Text 'docs\adr\0207-reconfirm-c4-full-domain-performance-under-ambient-runtime-on-a2-host.md' 'Accepted for planning only.'

Require-Text 'README.md' 'Current M10 Branch A3 checkpoint'
Require-Text 'docs\README.md' 'Current executable M10 gate'
Require-Text 'docs\ROADMAP.md' 'Live checkpoint override -- A2 returned / FDPC2 Planning 1'
Require-Text 'docs\M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md' 'Live checkpoint override -- A2 returned / Full-Domain Performance Confirmation 2 Planning 1'
Require-Text 'docs\PROJECT.md' '2026-09-18 -- A2 returned / FDPC2 Planning 1 live checkpoint'

Require ((Normalized-Sha256 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1.md') -eq $contract.normalized_lf_sha256.planning_main) 'Planning main document hash drift.'
Require ((Normalized-Sha256 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1_PREEXECUTION_REVIEW.md') -eq $contract.normalized_lf_sha256.planning_preexecution) 'Planning pre-execution document hash drift.'
Require ((Normalized-Sha256 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md') -eq $contract.normalized_lf_sha256.planning_returned_slot) 'Planning returned-evidence slot hash drift.'
Require ((Normalized-Sha256 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1_RETURNED_EVIDENCE_ADJUDICATION.md') -eq $contract.normalized_lf_sha256.a2_returned_adjudication) 'A2 returned adjudication document hash drift.'
Require ((Normalized-Sha256 'docs\adr\0207-reconfirm-c4-full-domain-performance-under-ambient-runtime-on-a2-host.md') -eq $contract.normalized_lf_sha256.adr_0207) 'ADR 0207 hash drift.'

# Planning-only guard: future implementation must not be present yet.
Require (-not (Test-Path -LiteralPath 'eng\m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-contract.json')) 'FDPC2 implementation contract must not be present in planning candidate.'
Require (-not (Test-Path -LiteralPath 'scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.cmd')) 'FDPC2 implementation runner must not be present in planning candidate.'

$artifactRoot = 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-planning1'
if (Test-Path -LiteralPath $artifactRoot) { Remove-Item -LiteralPath $artifactRoot -Recurse -Force }
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
Write-Utf8NoBom (Join-Path $artifactRoot '01-contract-and-provenance.txt') @(
 'status=PASS-AS-AUTHORED',
 'gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2-PLANNING1',
 'contract-schema=m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-planning1-v1',
 'a2-returned-adjudication=PASS',
 'a2-classification=AMBIENT-UNSET-PROJECT-QUALIFICATION-SUFFICIENT-ON-A2-HOST',
 'future-gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2',
 'future-runtime-profile=AMBIENT-UNSET',
 'future-same-host-required=True',
 'future-host-fingerprint-sha256=931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336',
 'future-fresh-processes=5',
 'future-total-measured-calls=217600',
 'future-required-files=32',
 'strict-max-us=409.30666666666673',
 'diagnostic-tail-floor-us=100',
 'diagnostic-tail-floor-is-threshold=False',
 'fdpc2-implementation-authorized=False',
 'rp1c-selection-authorized=False',
 'production-runtime-change-authorized=False'
)
Write-Utf8NoBom (Join-Path $artifactRoot '02-fdpc2-design-matrix.csv') @(
 'dimension,value,qualification_role',
 'runtime-profile,AMBIENT-UNSET,no explicit production runtime override',
 'physical-host,931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336,same host as A2',
 'power-scheme,8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c,stable start/end',
 'fresh-processes,5,full-domain replication',
 'exact-v9-rows,360,immutable RP1A corpus',
 'exact-v9-measured-calls-per-process,23040,64 x 360',
 'seam-rows,1280,immutable RP1A seam corpus',
 'seam-measured-calls-per-process,20480,16 x 1280',
 'total-measured-calls,217600,5 x 43520',
 'future-required-files,32,25 process + 7 aggregate',
 'strict-max-us,409.30666666666673,unchanged engineering ceiling'
)
Write-Utf8NoBom (Join-Path $artifactRoot '03-planning-summary.txt') @(
 'status=PASS-FDPC2-PLANNING-CONTRACT-FROZEN',
 'historical-fdpc1-classification=C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED',
 'a2-project-impact-classification=AMBIENT-UNSET-SUFFICIENT-ON-A2-HOST',
 'explicit-runtime-override-required=False',
 'ambient-internal-runtime-equivalence-proven=False',
 'future-fdpc2-profile=AMBIENT-UNSET',
 'future-fdpc2-same-a2-host=True',
 'future-fdpc2-processes=5',
 'future-fdpc2-measured-calls=217600',
 'future-fdpc2-required-files=32',
 'future-fdpc2-implementation-contained=False',
 'fdpc2-implementation-authorized-now=False',
 'rp1c-selection-authorized=False',
 'production-runtime-change-authorized=False',
 'next-action=RETURN-COMPLETE-FDPC2-PLANNING1-ARTIFACTS-FOR-ADJUDICATION'
)
Write-Utf8NoBom (Join-Path $artifactRoot '04-preexecution-review.txt') @(
 'status=STATIC-PREEXECUTION-REVIEW-PASS',
 'finding-1=A2-RETURNED-EVIDENCE-COMPLETE-78-FILES',
 'finding-2=AMBIENT-UNSET-PROJECT-QUALIFICATION-SUFFICIENT-ON-A2-HOST',
 'finding-3=EXPLICIT-RUNTIME-OVERRIDE-NOT-REQUIRED',
 'finding-4=FDPC1-NEGATIVE-HISTORY-PRESERVED',
 'finding-5=FDPC2-MUST-USE-SAME-A2-HOST-AND-POWER-SCHEME',
 'finding-6=C4-EXACT-V9-SEAM-THRESHOLDS-IMMUTABLE',
 'finding-7=A2-RAW-EVIDENCE-PRESERVED-IN-AUTHENTICATED-COMPACT-STORE',
 'future-required-files=32',
 'fdpc2-implementation-authorized=False',
 'rp1c-selection-authorized=False',
 'production-runtime-change-authorized=False'
)
Require (@(Get-ChildItem -LiteralPath $artifactRoot -File -Recurse).Count -eq 4) 'Planning artifact count mismatch.'
Write-Host 'Full-Domain Performance Confirmation 2 Planning 1 static audit: PASS-AS-AUTHORED' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f (Resolve-Path -LiteralPath $artifactRoot))
