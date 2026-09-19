$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Req([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Sha([string]$Path){$Resolved=(Resolve-Path $Path).Path;$Stream=[IO.File]::OpenRead($Resolved);$Hasher=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($Hasher.ComputeHash($Stream))).Replace('-','').ToUpperInvariant()}finally{$Stream.Dispose();$Hasher.Dispose()}}
function Tree([string]$RootPath,[string[]]$Excludes=@()){$ResolvedRoot=(Resolve-Path $RootPath).Path;$List=New-Object 'Collections.Generic.List[string]';Get-ChildItem $ResolvedRoot -Recurse -File|ForEach-Object{$Rel=$_.FullName.Substring($ResolvedRoot.Length+1).Replace('\','/');$Parts=$Rel.Split('/');if($Parts -contains 'bin' -or $Parts -contains 'obj' -or $Excludes -contains $Rel){return};$List.Add($Rel)};$Items=$List.ToArray();[Array]::Sort($Items,[StringComparer]::Ordinal);$Memory=New-Object IO.MemoryStream;try{foreach($Rel in $Items){$Bytes=[Text.Encoding]::UTF8.GetBytes($Rel);$Memory.Write($Bytes,0,$Bytes.Length);$Memory.WriteByte(0);$FileHasher=[Security.Cryptography.SHA256]::Create();try{$FileHash=$FileHasher.ComputeHash([IO.File]::ReadAllBytes((Join-Path $ResolvedRoot $Rel.Replace('/','\'))))}finally{$FileHasher.Dispose()};$Memory.Write($FileHash,0,$FileHash.Length);$Memory.WriteByte(10)};$Memory.Position=0;$TreeHasher=[Security.Cryptography.SHA256]::Create();try{$TreeHash=$TreeHasher.ComputeHash($Memory)}finally{$TreeHasher.Dispose()};@{Count=$Items.Count;Hash=([BitConverter]::ToString($TreeHash)).Replace('-','').ToUpperInvariant()}}finally{$Memory.Dispose()}}
function ReadUtf8([string]$Path){[IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8)}
function AddMarkerIssue([Collections.Generic.List[string]]$Issues,[string]$Path,[string]$Marker){if(-not (Test-Path -LiteralPath $Path)){$Issues.Add(($Path+': file missing'));return};$Text=ReadUtf8 $Path;$Count=([regex]::Matches($Text,[regex]::Escape($Marker))).Count;if($Count -ne 1){$Issues.Add(($Path+': '+$Marker+' expected exactly once; found '+$Count))}}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10974-fingerprint-v1-cross-host-determinism-hotfix1-contract.json' -Raw | ConvertFrom-Json
$Adjudication=Get-Content 'eng\m10974-fingerprint-v1-cross-host-diagnostic1-returned-evidence-adjudication1-contract.json' -Raw | ConvertFrom-Json

Req ($Contract.schema -eq 'm10974-fingerprint-v1-cross-host-determinism-hotfix1-rev2-v1') 'hotfix REV2 contract schema drift'
Req ([int]$Contract.revision -eq 2) 'hotfix revision drift'
Req ($Contract.status -eq 'CANDIDATE-READY-FOR-LOCAL-VALIDATION') 'hotfix contract status drift'
Req ($Contract.rev1_local_result.status -eq 'RED') 'REV1 local result must remain RED provenance'
Req ($Adjudication.status -eq 'PASS-AS-AUTHORED') 'returned-evidence adjudication is not PASS-AS-AUTHORED'
Req ($Adjudication.engineering_classification -eq 'PRESENTATION-CULTURE-DRIFT-CONFIRMED') 'returned-evidence classification drift'
Req ($Contract.fingerprint.algorithm_id -eq 'sha256-control-room-snapshot-v1') 'fingerprint algorithm id drift'
Req (-not [bool]$Contract.fingerprint.golden_reanchor_authorized) 'V1 golden re-anchor unexpectedly authorized'
Req ([bool]$Contract.fingerprint.historical_hash_preserved_as_runtime_golden) 'historical V1 golden preservation missing'
Req (-not [bool]$Contract.authority.exact_v9_anchor_change_authorized) 'Exact-V9 anchor change unexpectedly authorized'
Req (-not [bool]$Contract.authority.physics_change_authorized) 'physics change unexpectedly authorized'
Req (-not [bool]$Contract.authority.vr2_r3_change_authorized) 'VR2/R3 change unexpectedly authorized'

$EvidenceRoot='eng\frozen-evidence\ordinary\M10974_FingerprintV1_CrossHostDiagnostic1_ReturnedEvidence'
$LocalZip=Join-Path $EvidenceRoot 'local-fingerprint-v1-cross-host-diagnostic.zip'
$HostedOuterZip=Join-Path $EvidenceRoot 'hosted-ordinary-ci-diagnostics.zip'
$DiffSummary=Join-Path $EvidenceRoot 'cross-host-diff-summary.json'
Req ((Sha $LocalZip) -eq [string]$Adjudication.evidence.local_zip_sha256) 'local returned diagnostic ZIP drift'
Req ((Sha $HostedOuterZip) -eq [string]$Adjudication.evidence.hosted_outer_zip_sha256) 'hosted ordinary diagnostic ZIP drift'
Req ((Sha $DiffSummary) -eq [string]$Adjudication.evidence.diff_summary_sha256) 'cross-host diff summary drift'

$Temp=Join-Path ([IO.Path]::GetTempPath()) ('nrs-fpv1-hotfix1-rev2-'+[Guid]::NewGuid().ToString('N'))
$LocalDir=Join-Path $Temp 'local'
$HostedOuterDir=Join-Path $Temp 'hosted-outer'
$HostedDir=Join-Path $Temp 'hosted'
try{
    New-Item -ItemType Directory -Force -Path $LocalDir,$HostedOuterDir,$HostedDir | Out-Null
    Expand-Archive -LiteralPath $LocalZip -DestinationPath $LocalDir -Force
    Expand-Archive -LiteralPath $HostedOuterZip -DestinationPath $HostedOuterDir -Force
    $HostedInner=Join-Path $HostedOuterDir 'fingerprint-v1\fingerprint-v1-cross-host-diagnostic.zip'
    Req (Test-Path -LiteralPath $HostedInner) 'hosted inner fingerprint diagnostic ZIP missing'
    Req ((Sha $HostedInner) -eq [string]$Adjudication.evidence.hosted_inner_zip_sha256) 'hosted inner diagnostic ZIP drift'
    Expand-Archive -LiteralPath $HostedInner -DestinationPath $HostedDir -Force

    $LocalSummary=Get-Content (Join-Path $LocalDir 'summary.json') -Raw | ConvertFrom-Json
    $HostedSummary=Get-Content (Join-Path $HostedDir 'summary.json') -Raw | ConvertFrom-Json
    Req ($LocalSummary.actualFingerprint -eq [string]$Adjudication.findings.historical_local_fingerprint) 'local actual fingerprint drift'
    Req ($HostedSummary.actualFingerprint -eq [string]$Adjudication.findings.hosted_canonical_payload_fingerprint) 'hosted pre-repair actual fingerprint drift'
    Req ([int]$LocalSummary.counts.jsonNodeCount -eq 731 -and [int]$HostedSummary.counts.jsonNodeCount -eq 731) 'JSON node count drift'
    Req ([int]$LocalSummary.counts.scalarLeafCount -eq 596 -and [int]$HostedSummary.counts.scalarLeafCount -eq 596) 'scalar leaf count drift'

    $LocalNodes=@(Import-Csv (Join-Path $LocalDir 'nodes.tsv') -Delimiter "`t")
    $HostedNodes=@(Import-Csv (Join-Path $HostedDir 'nodes.tsv') -Delimiter "`t")
    Req ($LocalNodes.Count -eq 731 -and $HostedNodes.Count -eq 731) 'nodes.tsv cardinality drift'
    $PathDiff=0;$KindDiff=0;$NumericRawDiff=0;$RawDiff=New-Object 'Collections.Generic.List[object]'
    for($i=0;$i -lt $LocalNodes.Count;$i++){
        $L=$LocalNodes[$i];$H=$HostedNodes[$i]
        if($L.path -cne $H.path){$PathDiff++}
        if($L.kind -cne $H.kind){$KindDiff++}
        if($L.scalarRawValue -cne $H.scalarRawValue){$RawDiff.Add([pscustomobject]@{Path=$L.path;Kind=$L.kind;Local=$L.scalarRawValue;Hosted=$H.scalarRawValue});if($L.kind -eq 'Number'){$NumericRawDiff++}}
    }
    Req ($PathDiff -eq 0) 'cross-host JSON path drift found'
    Req ($KindDiff -eq 0) 'cross-host JSON kind drift found'
    Req ($RawDiff.Count -eq 1) ('expected one scalar raw-value difference; found '+$RawDiff.Count)
    Req ($RawDiff[0].Path -eq [string]$Adjudication.findings.first_divergent_pointer) 'first divergent pointer drift'
    Req ($RawDiff[0].Kind -eq 'String') 'divergent leaf kind drift'
    Req ($NumericRawDiff -eq 0) 'numeric cross-host drift found'

    $LocalPayload=[IO.File]::ReadAllBytes((Join-Path $LocalDir 'normalized-control-room-snapshot-v1.json'))
    $HostedPayload=[IO.File]::ReadAllBytes((Join-Path $HostedDir 'normalized-control-room-snapshot-v1.json'))
    Req ($LocalPayload.Length -eq $HostedPayload.Length) 'normalized payload length drift'
    $ByteDiff=New-Object 'Collections.Generic.List[int]'
    for($i=0;$i -lt $LocalPayload.Length;$i++){if($LocalPayload[$i] -ne $HostedPayload[$i]){$ByteDiff.Add($i)}}
    Req ($ByteDiff.Count -eq 1) ('expected one pre-repair payload byte difference; found '+$ByteDiff.Count)
    Req ($LocalPayload[$ByteDiff[0]] -eq 44 -and $HostedPayload[$ByteDiff[0]] -eq 46) 'pre-repair comma/period evidence drift'
}
finally{if(Test-Path -LiteralPath $Temp){Remove-Item -LiteralPath $Temp -Recurse -Force}}

$Src=Tree 'src' @(
    'NuclearReactorSimulator.Application/ControlRoom/ControlRoomSnapshotProjector.cs',
    'NuclearReactorSimulator.Application/Scenarios/Recording/ControlRoomSnapshotFingerprint.cs')
Req ($Src.Count -eq [int]$Contract.frozen_boundaries.src_excluding_allowed_file_count) 'src file count drift outside allowed production files'
Req ($Src.Hash -eq [string]$Contract.frozen_boundaries.src_excluding_allowed_tree_sha256) 'src tree drift outside allowed production files'
$Tests=Tree 'tests' @(
    'NuclearReactorSimulator.Application.Tests/ControlRoom/MissionPerformance/M10974FingerprintV1SchemaAnchorTests.cs',
    'NuclearReactorSimulator.Application.Tests/ControlRoom/MissionPerformance/M10974FingerprintV1CrossHostDiagnostic1.cs')
Req ($Tests.Count -eq [int]$Contract.frozen_boundaries.tests_excluding_allowed_file_count) 'tests file count drift outside allowed files'
Req ($Tests.Hash -eq [string]$Contract.frozen_boundaries.tests_excluding_allowed_tree_sha256) 'tests tree drift outside allowed files'

Req ((Sha $Contract.allowed_changes.projector_file) -eq [string]$Contract.allowed_changes.projector_new_sha256) 'projector hash drift'
Req ((Sha $Contract.allowed_changes.fingerprint_file) -eq [string]$Contract.allowed_changes.fingerprint_new_sha256) 'fingerprint implementation hash drift'
Req ((Sha $Contract.allowed_changes.schema_test_file) -eq [string]$Contract.allowed_changes.schema_test_new_sha256) 'schema-anchor test hash drift'
Req ((Sha $Contract.allowed_changes.diagnostic_helper_file) -eq [string]$Contract.allowed_changes.diagnostic_helper_new_sha256) 'diagnostic helper hash drift'
Req ((Sha $Contract.transitive_anchor.test) -eq [string]$Contract.transitive_anchor.test_sha256) 'Exact-V9 transitive-anchor test drift'
Req ((Sha 'src\NuclearReactorSimulator.Application\Scenarios\Training\DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.cs') -eq [string]$Contract.frozen_boundaries.h29_factory_sha256) 'H29 factory drift'
Req ((Sha 'eng\ci-ordinary.cmd') -eq [string]$Contract.frozen_boundaries.ci_ordinary_sha256) 'ordinary CI runner drift'
Req ((Sha '.github\workflows\ordinary-ci.yml') -eq [string]$Contract.frozen_boundaries.ordinary_workflow_sha256) 'ordinary hosted workflow drift'

$Projector=ReadUtf8 $Contract.allowed_changes.projector_file
Req ($Projector.Contains('FormattableString.Invariant($"Void {branch.OutletVoidFraction.Value.Percent:0.0}%")')) 'invariant live VoidText repair missing'
Req (-not $Projector.Contains('? $"Void {branch.OutletVoidFraction.Value.Percent:0.0}%"')) 'culture-sensitive live VoidText expression still present'

$Fingerprint=ReadUtf8 $Contract.allowed_changes.fingerprint_file
Req ($Fingerprint.Contains('public const string AlgorithmId = "sha256-control-room-snapshot-v1";')) 'V1 algorithm id changed'
Req ($Fingerprint.Contains('NormalizeLegacyV1PrimaryCircuit(snapshot.PrimaryCircuit)')) 'V1 primary-circuit compatibility normalization missing'
Req ($Fingerprint.Contains("return voidText.Replace('.', ',');")) 'legacy branch VoidText byte canonicalization missing'
Req ($Fingerprint.Contains('internal static byte[] SerializeCanonicalPayload')) 'shared canonical payload routine missing'

$SchemaTest=ReadUtf8 $Contract.allowed_changes.schema_test_file
Req ($SchemaTest.Contains([string]$Contract.fingerprint.frozen_v1_fingerprint)) 'frozen V1 golden missing from schema-anchor test'
Req (-not $SchemaTest.Contains([string]$Contract.fingerprint.observed_hosted_pre_repair_payload_fingerprint)) 'pre-repair hosted hash must not become the schema golden'
Req ($SchemaTest.Contains('FingerprintV1_PopulatedExactVersionFixtureIsInvariantAcrossItalianAndUsCultures')) 'cross-culture regression missing'
Req ($SchemaTest.Contains('Assert.Equal("Void 0.0%", italian.VoidText);') -and $SchemaTest.Contains('Assert.Equal("Void 0.0%", us.VoidText);')) 'live invariant presentation assertions missing'

$Diagnostic=ReadUtf8 $Contract.allowed_changes.diagnostic_helper_file
Req ($Diagnostic.Contains('ControlRoomSnapshotFingerprint.SerializeCanonicalPayload(snapshot)')) 'diagnostic helper must use production V1 canonical payload'

$Exact=ReadUtf8 $Contract.transitive_anchor.test
Req ($Exact.Contains([string]$Contract.transitive_anchor.expected_determinism_fingerprint)) 'frozen Exact-V9 expected determinism fingerprint missing'
$ActivationContract=Get-Content 'eng\m10-final-v9-production-activation-decision-contract.json' -Raw | ConvertFrom-Json
$ActivationRecord=Get-Content 'eng\m10-final-v9-production-activation-decision-record.json' -Raw | ConvertFrom-Json
Req ($ActivationContract.focusedEnvelope.expectedDeterminismFingerprint -eq [string]$Contract.transitive_anchor.expected_determinism_fingerprint) 'Exact-V9 activation contract fingerprint drift'
Req ($ActivationRecord.fingerprint -eq [string]$Contract.transitive_anchor.expected_determinism_fingerprint) 'Exact-V9 activation record fingerprint drift'

$MarkerIssues=New-Object 'Collections.Generic.List[string]'
foreach($Entry in $Contract.documentation_marker_files){foreach($Marker in $Entry.markers){AddMarkerIssue $MarkerIssues ([string]$Entry.path) ([string]$Marker)}}
if($MarkerIssues.Count -gt 0){throw ("documentation marker validation failed:`n - "+($MarkerIssues -join "`n - "))}

Write-Host 'Fingerprint V1 Cross-Host Determinism Hotfix 1 REV2 static/evidence validation PASS.'
Write-Host ('Frozen V1 fingerprint preserved: '+[string]$Contract.fingerprint.frozen_v1_fingerprint)
Write-Host ('Frozen Exact-V9 transitive anchor preserved: '+[string]$Contract.transitive_anchor.expected_determinism_fingerprint)
Write-Host 'R3 remains RED; no VR2/R3 production change is authorized.'
