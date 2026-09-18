$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
}
function Read-Utf8Text([string]$Path) {
    Require-File $Path
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}
function Require-Text([string]$Path,[string]$Needle) {
    $text = Read-Utf8Text $Path
    if (-not $text.Contains($Needle)) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Require-AsciiSource([string]$Path,[string]$Label) {
    Require-File $Path
    foreach ($b in [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)) {
        if ($b -gt 127) { throw ("{0} must remain 7-bit ASCII for Windows PowerShell 5.1: {1}" -f $Label,$Path) }
    }
}
function Get-Sha256HexFromStream([System.IO.Stream]$Stream) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-','').ToUpperInvariant() }
    finally { $sha.Dispose() }
}
function Get-FileSha256([string]$Path) {
    Require-File $Path
    $s = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
    try { return Get-Sha256HexFromStream $s } finally { $s.Dispose() }
}
function Get-NormalizedTextSha256([string]$Path) {
    $text = Read-Utf8Text $Path
    $normalized = $text.Replace("`r`n","`n").Replace("`r","`n")
    $enc = New-Object System.Text.UTF8Encoding -ArgumentList $false
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($enc.GetBytes($normalized)))).Replace('-','').ToUpperInvariant() }
    finally { $sha.Dispose() }
}
function Get-TreeSha256([string]$Root) {
    $resolved = (Resolve-Path -LiteralPath $Root).Path
    # Tree identity is defined by ordinal, case-sensitive relative-path order.
    # Do not use Sort-Object here: Windows PowerShell 5.1 sorting is culture/case sensitive to host semantics.
    $relativePaths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in @(Get-ChildItem -LiteralPath $resolved -Recurse -File)) {
        $relativePaths.Add($file.FullName.Substring($resolved.Length + 1).Replace('\','/'))
    }
    $relativePaths.Sort([System.StringComparer]::Ordinal)

    $ms = New-Object System.IO.MemoryStream
    try {
        foreach ($rel in $relativePaths) {
            $fullPath = Join-Path $resolved $rel.Replace('/','\')
            $relBytes = [System.Text.Encoding]::UTF8.GetBytes($rel)
            $ms.Write($relBytes,0,$relBytes.Length); $ms.WriteByte(0)
            $fileSha = [System.Security.Cryptography.SHA256]::Create()
            $fs = [System.IO.File]::OpenRead($fullPath)
            try { $fileHash = $fileSha.ComputeHash($fs) }
            finally { $fs.Dispose(); $fileSha.Dispose() }
            $ms.Write($fileHash,0,$fileHash.Length); $ms.WriteByte(10)
        }
        $ms.Position = 0
        $treeSha = [System.Security.Cryptography.SHA256]::Create()
        try { $treeHash = $treeSha.ComputeHash($ms) }
        finally { $treeSha.Dispose() }
        return @{ Hash = ([BitConverter]::ToString($treeHash)).Replace('-','').ToUpperInvariant(); Count = $relativePaths.Count }
    }
    finally { $ms.Dispose() }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $PSScriptRoot 'm10-final-frozen-evidence-ordinary-compaction1-contract.json'
$manifestPath = Join-Path $repoRoot 'eng\frozen-evidence\large-payload-manifest.csv'
$inventoryPath = Join-Path $repoRoot 'eng\frozen-evidence\archive\ordinary-large-payload-inventory-v1.csv'
$ordinaryRoot = Join-Path $repoRoot 'eng\frozen-evidence\ordinary'
$archiveRoot = Join-Path $repoRoot 'eng\frozen-evidence\archive'
$restorePath = Join-Path $repoRoot 'scripts\restore-frozen-evidence-large-payloads.ps1'
$compactPath = Join-Path $repoRoot 'scripts\compact-frozen-evidence-ordinary.ps1'
$runnerPath = Join-Path $repoRoot 'scripts\run-m10-final-frozen-evidence-ordinary-compaction1.cmd'
$policyDoc = Join-Path $repoRoot 'docs\history\m10-final\vr2\M10_FINAL_FROZEN_EVIDENCE_ORDINARY_COMPACTION1.md'
$adr = Join-Path $repoRoot 'docs\adr\0205-keep-ordinary-frozen-evidence-compact-and-archive-large-immutable-payloads.md'
$returnedAdj = Join-Path $repoRoot 'docs\M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT_PLANNING1_AMENDMENT1_HOST_PROVENANCE_RETURNED_EVIDENCE_ADJUDICATION.md'
$amendRoot = Join-Path $ordinaryRoot 'M10FinalVR2EngineeringRepairPlanning1_RP1C_C4_RuntimeConfigurationImpactAssessmentPlanning1Amendment1HostProvenance_Artifacts'

foreach ($p in @($contractPath,$manifestPath,$inventoryPath,$restorePath,$compactPath,$runnerPath,$policyDoc,$adr,$returnedAdj)) { Require-File $p }
Require-AsciiSource $MyInvocation.MyCommand.Path 'Compaction validator'
Require-AsciiSource $restorePath 'Restore helper'
Require-AsciiSource $compactPath 'Compaction helper'
Require-AsciiSource $runnerPath 'Compaction runner'

$contract = Read-Utf8Text $contractPath | ConvertFrom-Json
if ($contract.schema -ne 'm10-final-frozen-evidence-ordinary-compaction1-v1') { throw 'Compaction schema drifted.' }
if ($contract.status -ne 'CANDIDATE-MAINTENANCE-ONLY') { throw 'Compaction status drifted.' }
if ($contract.gate -ne 'M10-FINAL-FROZEN-EVIDENCE-ORDINARY-COMPACTION1') { throw 'Compaction gate identity drifted.' }
if ($contract.purpose.physics_or_runtime_change -ne $false -or $contract.purpose.a2_implementation_contained -ne $false) { throw 'Compaction scope drifted.' }
if ($contract.prerequisite.host_provenance_amendment_returned_status -ne 'PASS-AS-AUTHORED') { throw 'Returned host-provenance amendment status drifted.' }
if ([int]$contract.prerequisite.host_provenance_amendment_artifact_count -ne 4) { throw 'Returned host-provenance artifact count drifted.' }
if ($contract.prerequisite.future_a2_gate -ne 'RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1') { throw 'Future A2 identity drifted.' }
if ($contract.prerequisite.future_a2_implementation_authorized_by_this_gate -ne $false) { throw 'Compaction must not authorize A2 by itself.' }

$amendFiles = @(Get-ChildItem -LiteralPath $amendRoot -File)
if ($amendFiles.Count -ne 4) { throw ("Returned Amendment 1 frozen set must contain four files; found {0}." -f $amendFiles.Count) }
$expectedAmend = $contract.returned_amendment_normalized_lf_sha256
foreach ($file in $amendFiles) {
    $prop = $expectedAmend.PSObject.Properties[$file.Name]
    if ($null -eq $prop) { throw ("Unexpected returned Amendment 1 file: {0}" -f $file.Name) }
    $expected = [string]$prop.Value
    if ((Get-NormalizedTextSha256 $file.FullName) -ne $expected.ToUpperInvariant()) { throw ("Returned Amendment 1 hash drifted: {0}" -f $file.Name) }
}
Require-Text (Join-Path $amendRoot '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $amendRoot '01-contract-and-provenance.txt') 'single-physical-host-required=True'
Require-Text (Join-Path $amendRoot '02-host-provenance-policy.txt') 'host-scope=ENTIRE-A2-GATE'
Require-Text (Join-Path $amendRoot '03-planning-amendment-summary.txt') 'a2-implementation-authorized-now=False'
Require-Text (Join-Path $amendRoot '04-preexecution-review.txt') 'finding-3=SINGLE-HOST-PER-A2-GATE-REQUIRED'
Require-Text $returnedAdj 'PASS-AS-AUTHORED'
Require-Text $policyDoc 'Files at or below 1 MiB remain directly under `eng/frozen-evidence/ordinary`'
Require-Text $adr 'Direct payloads in `eng/frozen-evidence/ordinary` must remain at or below 1 MiB'

$srcTree = Get-TreeSha256 (Join-Path $repoRoot 'src')
$testsTree = Get-TreeSha256 (Join-Path $repoRoot 'tests')
if ($srcTree.Count -ne [int]$contract.tree_identity.src_files -or $srcTree.Hash -ne $contract.tree_identity.src_tree_sha256.ToUpperInvariant()) { throw 'src tree drifted during compaction.' }
if ($testsTree.Count -ne [int]$contract.tree_identity.tests_files -or $testsTree.Hash -ne $contract.tree_identity.tests_tree_sha256.ToUpperInvariant()) { throw 'tests tree drifted during compaction.' }

$ordinaryFiles = @(Get-ChildItem -LiteralPath $ordinaryRoot -Recurse -File)
$maxAllowed = [int64]$contract.compaction.ordinary_max_bundled_payload_bytes
$oversized = @($ordinaryFiles | Where-Object { $_.Length -gt $maxAllowed })
if ($oversized.Count -ne 0) { throw ("Expanded ordinary store still contains {0} files above {1} bytes." -f $oversized.Count,$maxAllowed) }
if ($ordinaryFiles.Count -ne [int]$contract.compaction.compact_ordinary_file_count) { throw 'Compact ordinary file count drifted.' }
$ordinaryBytes = [int64](($ordinaryFiles | Measure-Object -Property Length -Sum).Sum)
if ($ordinaryBytes -ne [int64]$contract.compaction.compact_ordinary_total_bytes) { throw 'Compact ordinary byte total drifted.' }
$ordinaryMax = [int64](($ordinaryFiles | Measure-Object -Property Length -Maximum).Maximum)
if ($ordinaryMax -ne [int64]$contract.compaction.compact_ordinary_max_file_bytes) { throw 'Compact ordinary maximum file size drifted.' }

$manifestRows = @(Import-Csv -LiteralPath $manifestPath)
$bundledRows = @($manifestRows | Where-Object { $_.storage -like 'bundled-compressed-archive:*' })
$externalRows = @($manifestRows | Where-Object { $_.storage -eq 'external-frozen-payload' })
$inventoryRows = @(Import-Csv -LiteralPath $inventoryPath)
if ($manifestRows.Count -ne [int]$contract.compaction.large_payload_manifest_total_rows) { throw 'Large payload manifest row count drifted.' }
if ($bundledRows.Count -ne [int]$contract.compaction.bundled_archive_manifest_rows) { throw 'Bundled archive manifest count drifted.' }
if ($externalRows.Count -ne [int]$contract.compaction.preexisting_external_manifest_rows_preserved) { throw 'Pre-existing external manifest rows were not preserved.' }
if ($inventoryRows.Count -ne $bundledRows.Count) { throw 'Archive inventory and manifest bundled row counts differ.' }

$manifestByPath = @{}
foreach ($r in $bundledRows) {
    if ($manifestByPath.ContainsKey($r.logical_path)) { throw ("Duplicate bundled logical path: {0}" -f $r.logical_path) }
    $manifestByPath[$r.logical_path] = $r
}
foreach ($r in $inventoryRows) {
    if (-not $manifestByPath.ContainsKey($r.logical_path)) { throw ("Inventory path missing from canonical manifest: {0}" -f $r.logical_path) }
    $m = $manifestByPath[$r.logical_path]
    if ($m.canonical_sha256 -ne $r.canonical_sha256 -or $m.uncompressed_bytes -ne $r.uncompressed_bytes -or $m.storage -ne $r.storage) { throw ("Inventory row drifted: {0}" -f $r.logical_path) }
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archives = @{}
$totalUncompressed = [int64]0
try {
    foreach ($r in $bundledRows) {
        $expanded = Join-Path $ordinaryRoot $r.logical_path.Replace('/','\')
        if (Test-Path -LiteralPath $expanded -PathType Leaf) { throw ("Compacted payload must not remain expanded: {0}" -f $r.logical_path) }
        $archiveRelative = $r.storage.Substring('bundled-compressed-archive:'.Length)
        $archivePath = Join-Path $repoRoot $archiveRelative.Replace('/','\')
        if (-not $archives.ContainsKey($archivePath)) {
            Require-File $archivePath
            $archives[$archivePath] = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
        }
        $entry = $archives[$archivePath].GetEntry($r.logical_path)
        if ($null -eq $entry) { throw ("Archive entry missing: {0}" -f $r.logical_path) }
        if ([int64]$entry.Length -ne [int64]$r.uncompressed_bytes) { throw ("Archive entry size mismatch: {0}" -f $r.logical_path) }
        $s = $entry.Open()
        try { $hash = Get-Sha256HexFromStream $s } finally { $s.Dispose() }
        if ($hash -ne $r.canonical_sha256.ToUpperInvariant()) { throw ("Archive entry SHA-256 mismatch: {0}" -f $r.logical_path) }
        $totalUncompressed += [int64]$entry.Length
    }
}
finally { foreach ($zip in $archives.Values) { $zip.Dispose() } }
if ($totalUncompressed -ne [int64]$contract.compaction.externalized_uncompressed_bytes) { throw 'Externalized uncompressed byte total drifted.' }

$archiveSpecs = @($contract.archive_packs)
if ($archiveSpecs.Count -ne [int]$contract.compaction.archive_pack_count) { throw 'Archive pack count drifted.' }
$archiveBytes = [int64]0
foreach ($spec in $archiveSpecs) {
    $archivePath = Join-Path $repoRoot $spec.path.Replace('/','\')
    Require-File $archivePath
    $fi = Get-Item -LiteralPath $archivePath
    if ([int64]$fi.Length -ne [int64]$spec.bytes) { throw ("Archive pack byte count drifted: {0}" -f $spec.path) }
    if ((Get-FileSha256 $archivePath) -ne $spec.sha256.ToUpperInvariant()) { throw ("Archive pack SHA-256 drifted: {0}" -f $spec.path) }
    $archiveBytes += [int64]$fi.Length
}
if ($archiveBytes -ne [int64]$contract.compaction.archive_total_bytes) { throw 'Archive total byte count drifted.' }

$a = $contract.authority
foreach ($name in @('threshold_change_authorized','exact_v9_change_authorized','rp1c_selection_authorized','production_runtime_change_authorized','production_repair_authorized','full_domain_performance_confirmation2_authorized','vr3_authorized','p3_r1_authorized','second_replacement_long_authorized')) {
    if ($a.$name -ne $false) { throw ("Authority must remain false: {0}" -f $name) }
}

$artifactDir = Join-Path $repoRoot 'artifacts\m10-final-frozen-evidence-ordinary-compaction1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding -ArgumentList $false

$contractEvidence = @"
status=PASS-AS-AUTHORED
gate=M10-FINAL-FROZEN-EVIDENCE-ORDINARY-COMPACTION1
host-provenance-amendment-returned=PASS-AS-AUTHORED
ordinary-max-direct-bytes=$maxAllowed
compact-ordinary-files=$($ordinaryFiles.Count)
compact-ordinary-bytes=$ordinaryBytes
archived-payloads=$($bundledRows.Count)
archived-uncompressed-bytes=$totalUncompressed
archive-packs=$($archiveSpecs.Count)
a2-implementation-authorized=False
rp1c-selection-authorized=False
production-runtime-change-authorized=False
"@
[System.IO.File]::WriteAllText((Join-Path $artifactDir '01-contract-and-provenance.txt'), $contractEvidence.TrimStart(), $utf8)

$archiveLines = New-Object System.Collections.Generic.List[string]
$archiveLines.Add('archive_path,sha256,compressed_bytes')
foreach ($spec in $archiveSpecs) { $archiveLines.Add(('"{0}",{1},{2}' -f $spec.path,$spec.sha256,$spec.bytes)) }
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-archive-pack-inventory.csv'), $archiveLines.ToArray(), $utf8)

$summaryEvidence = @"
status=PASS-FROZEN-EVIDENCE-ORDINARY-COMPACTION
expanded-large-payloads-present=0
bundled-archive-manifest-rows=$($bundledRows.Count)
preexisting-external-manifest-rows=$($externalRows.Count)
ordinary-direct-files=$($ordinaryFiles.Count)
ordinary-direct-bytes=$ordinaryBytes
archive-total-bytes=$archiveBytes
restore-tool=.\scripts\restore-frozen-evidence-large-payloads.ps1
compact-tool=.\scripts\compact-frozen-evidence-ordinary.ps1
engineering-contract-changed=False
next-action=RETURN-COMPLETE-COMPACTION-ARTIFACTS-BEFORE-A2-IMPLEMENTATION
"@
[System.IO.File]::WriteAllText((Join-Path $artifactDir '03-compaction-summary.txt'), $summaryEvidence.TrimStart(), $utf8)

$reviewEvidence = @"
status=STATIC-MAINTENANCE-REVIEW-PASS
finding-1=HOST-PROVENANCE-AMENDMENT-RETURNED-PASS-AS-AUTHORED
finding-2=ORDINARY-COMPACT-STORE-BOUNDARY-RESTORED
finding-3=LARGE-RAW-EVIDENCE-BYTE-IDENTITY-PRESERVED-IN-AUTHENTICATED-ARCHIVES
finding-4=HISTORICAL-REHYDRATION-FAIL-CLOSED-AVAILABLE
finding-5=SRC-AND-TESTS-UNCHANGED
finding-6=A2-NOT-IMPLEMENTED
rp1c-selection-authorized=False
production-runtime-change-authorized=False
"@
[System.IO.File]::WriteAllText((Join-Path $artifactDir '04-preexecution-review.txt'), $reviewEvidence.TrimStart(), $utf8)

Write-Host '============================================================'
Write-Host 'M10 FINAL - FROZEN EVIDENCE ORDINARY COMPACTION 1'
Write-Host '============================================================'
Write-Host 'Frozen Evidence Ordinary Compaction 1 static audit: PASS-AS-AUTHORED'
Write-Host ("Ordinary: {0} files / {1} bytes. Archived large payloads: {2} / {3} uncompressed bytes." -f $ordinaryFiles.Count,$ordinaryBytes,$bundledRows.Count,$totalUncompressed)
Write-Host ("Artifacts: {0}" -f $artifactDir)
