$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Fail([string]$Message) { throw $Message }
function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { Fail $Message } }
function Read-Text([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}
function Raw-Sha256([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) ("Required file not found: {0}" -f $Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $Path))
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
        finally { $stream.Dispose() }
    }
    finally { $sha.Dispose() }
}
function Normalized-Sha256([string]$Path) {
    $text = Read-Text $Path
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}
function Require-Text([string]$Path, [string]$Needle) {
    $text = Read-Text $Path
    Require ($text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) ("Required marker not found in {0}: {1}" -f $Path, $Needle)
}
function Require-NotText([string]$Path, [string]$Needle) {
    $text = Read-Text $Path
    Require ($text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) ("Forbidden marker found in {0}: {1}" -f $Path, $Needle)
}
function Get-CanonicalTree([string]$TreeRoot, [string[]]$Excluded) {
    $rootFull = (Resolve-Path -LiteralPath $TreeRoot).Path
    $excludedSet = @{}
    foreach ($item in $Excluded) { $excludedSet[$item.Replace('\','/')] = $true }
    $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in Get-ChildItem -LiteralPath $rootFull -File -Recurse) {
        $relative = $file.FullName.Substring($rootFull.Length).TrimStart([char[]]@('\','/')).Replace('\','/')
        $parts = $relative.Split('/')
        if ($parts -contains 'bin' -or $parts -contains 'obj' -or $parts -contains 'TestResults') { continue }
        if ($excludedSet.ContainsKey($relative)) { continue }
        $paths.Add($relative)
    }
    [string[]]$ordered = $paths.ToArray()
    [Array]::Sort($ordered, [StringComparer]::Ordinal)
    $builder = New-Object System.Text.StringBuilder
    foreach ($relative in $ordered) {
        $full = Join-Path $rootFull ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
        [void]$builder.Append((Raw-Sha256 $full)).Append('|').Append($relative).Append("`n")
    }
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($builder.ToString())
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
    return New-Object PSObject -Property @{ Count = $ordered.Length; Sha256 = $hash }
}

$contractPath = 'eng\m10-final-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1-contract.json'
$contract = (Read-Text $contractPath) | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1-v1') 'R1 implementation contract schema drift.'
Require ($contract.status -eq 'IMPLEMENTATION-EVIDENCE-GATE') 'R1 implementation contract status drift.'
Require ([bool]$contract.authority.r1_implementation_gate_authorized) 'R1 implementation gate is not authorized by returned Planning 1 adjudication.'
Require (-not [bool]$contract.authority.production_default_switch_authorized) 'Production default switch must remain unauthorized.'
Require (-not [bool]$contract.authority.production_runtime_change_authorized) 'Production runtime change must remain unauthorized.'
Require (-not [bool]$contract.authority.exact_v9_change_authorized) 'Exact-v9 change must remain unauthorized.'
Require (-not [bool]$contract.authority.r2_planning_authorized_now) 'R2 planning must remain unauthorized before returned R1 adjudication.'
Require ($contract.implementation.gate -eq 'R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1') 'R1 implementation gate id drift.'
Require ([int]$contract.implementation.closure_mode_value -eq 2) 'Mode 2 numeric identity drift.'
Require (-not [bool]$contract.implementation.default_activation) 'Mode 2 default activation is forbidden.'
Require (-not [bool]$contract.implementation.exact_v9_activation) 'Mode 2 exact-v9 activation is forbidden.'
Require ([int]$contract.implementation.state_comparisons -eq 1679) 'State comparison count drift.'
Require ([int]$contract.implementation.hydraulic_comparisons -eq 288) 'Hydraulic comparison count drift.'
Require ([int]$contract.implementation.total_comparisons -eq 1967) 'Total comparison count drift.'
Require ([int]$contract.evidence.required_files -eq 7) 'Required R1 artifact count drift.'
Require ($contract.implementation.historical_mode_1_regression_basis -eq 'LEGACY-SOURCE-PROJECTION-SHA256+ORDINARY-RELEASE-SUITE') 'Historical mode 1 regression basis drift.'
Require ($contract.implementation.historical_mode_1_baseline_production_model_sha256 -eq '93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E') 'Historical mode 1 baseline source SHA drift.'
Require (-not [bool]$contract.implementation.historical_mode_1_density_derived_frozen_output_comparison_authoritative) 'Lossy density-derived mode 1 comparison must not become authoritative.'

foreach ($property in $contract.gate_files.PSObject.Properties) {
    $entry = $property.Value
    Require ((Normalized-Sha256 ([string]$entry.path)) -eq ([string]$entry.normalized_sha256).ToUpperInvariant()) ("R1 gate-file hash drifted: {0}" -f $property.Name)
}

$planningRoot = 'eng\frozen-evidence\ordinary\M10FinalVR2_R1_ImplementationPlanning1_Artifacts'
foreach ($property in $contract.planning_artifacts_sha256.PSObject.Properties) {
    $path = Join-Path $planningRoot $property.Name
    Require ((Raw-Sha256 $path) -eq ([string]$property.Value).ToUpperInvariant()) ("Returned Planning 1 artifact hash drift: {0}" -f $property.Name)
}
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'status=PASS-AS-AUTHORED'
Require-Text (Join-Path $planningRoot '01-contract-and-provenance.txt') 'future-gate=R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1'
Require-Text (Join-Path $planningRoot '03-implementation-and-requalification-summary.txt') 'production-vs-c4-semantic-comparisons-required=1967'
Require-Text (Join-Path $planningRoot '04-preimplementation-review.txt') 'finding-13=FROZEN-C4-BIT-EQUIVALENCE-REQUIRED-BEFORE-R2'

Require ((Raw-Sha256 $contract.frozen_corpus.c4_source_path) -eq $contract.frozen_corpus.c4_source_sha256) 'Frozen C4 source hash drift.'
Require ((Raw-Sha256 $contract.frozen_corpus.state_path) -eq $contract.frozen_corpus.state_sha256) 'Frozen C4 state corpus hash drift.'
Require ((Raw-Sha256 $contract.frozen_corpus.hydraulic_path) -eq $contract.frozen_corpus.hydraulic_sha256) 'Frozen C4 hydraulic corpus hash drift.'
$stateRows = @(Import-Csv -LiteralPath $contract.frozen_corpus.state_path)
$hydraulicRows = @(Import-Csv -LiteralPath $contract.frozen_corpus.hydraulic_path)
Require ($stateRows.Count -eq 1679) 'Frozen state corpus row count drift.'
Require ($hydraulicRows.Count -eq 288) 'Frozen hydraulic corpus row count drift.'
$stateIds = @($stateRows | ForEach-Object { $_.domain + '|' + $_.observation_index + '|' + $_.source_identity } | Sort-Object -Unique)
$hydraulicIds = @($hydraulicRows | ForEach-Object { $_.domain + '|' + $_.observation_index + '|' + $_.probe_id + '|' + $_.logical_step + '|' + $_.path_id } | Sort-Object -Unique)
Require ($stateIds.Count -eq 1679) 'Frozen state corpus identity uniqueness drift.'
Require ($hydraulicIds.Count -eq 288) 'Frozen hydraulic corpus identity uniqueness drift.'

foreach ($property in $contract.implementation_files.PSObject.Properties) {
    $entry = $property.Value
    $path = [string]$entry.path
    if ($property.Name -eq 'payload') {
        $actual = Raw-Sha256 $path
        $expected = ([string]$entry.sha256).ToUpperInvariant()
    }
    else {
        $actual = Normalized-Sha256 $path
        $expected = ([string]$entry.normalized_sha256).ToUpperInvariant()
    }
    Require ($actual -eq $expected) ("R1 implementation hash drifted: {0}" -f $property.Name)
}

$srcExcluded = @(
 'NuclearReactorSimulator.Simulation/NuclearReactorSimulator.Simulation.csproj',
 'NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs',
 'NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs',
 'NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceConsistentTabulatedInverseResolver.cs',
 'NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin'
)
$testExcluded = @(
 'NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/NrsVr2C4ReferencePayloadGenerator.cs',
 'NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/R1SelectedC4OptInClosureImplementationTests.cs'
)
$srcTree = Get-CanonicalTree 'src' $srcExcluded
$testTree = Get-CanonicalTree 'tests' $testExcluded
Require ($srcTree.Count -eq [int]$contract.unchanged_scope.src_excluding_allowed_file_count) 'Unchanged src file count drift.'
Require ($srcTree.Sha256 -eq $contract.unchanged_scope.src_excluding_allowed_tree_sha256) 'Unchanged src tree drift.'
Require ($testTree.Count -eq [int]$contract.unchanged_scope.tests_excluding_new_file_count) 'Historical test file count drift.'
Require ($testTree.Sha256 -eq $contract.unchanged_scope.tests_excluding_new_tree_sha256) 'Historical test tree drift.'

$payloadPath = [string]$contract.implementation_files.payload.path
Require ((Get-Item -LiteralPath $payloadPath).Length -eq [int64]$contract.implementation.reference_payload_size_bytes) 'Payload byte length drift.'
Require ((Raw-Sha256 $payloadPath) -eq $contract.implementation.reference_payload_sha256) 'Payload SHA-256 drift.'
$stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $payloadPath))
$reader = New-Object System.IO.BinaryReader($stream, [Text.Encoding]::ASCII)
try {
    $magic = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(8))
    $schema = $reader.ReadInt32()
    $endian = $reader.ReadInt32()
    $dense = $reader.ReadInt32(); $prefix = $reader.ReadInt32(); $liquid = $reader.ReadInt32(); $vapor = $reader.ReadInt32()
    Require ($magic -eq 'NRSVR2C4') 'Payload magic drift.'
    Require ($schema -eq 1) 'Payload schema drift.'
    Require ($endian -eq 0x01020304) 'Payload endian marker drift.'
    Require ($dense -eq 17502 -and $prefix -eq 701 -and $liquid -eq 351 -and $vapor -eq 401) 'Payload table-count header drift.'
}
finally { $reader.Dispose(); $stream.Dispose() }

$project = [string]$contract.implementation_files.simulation_project.path
$mode = [string]$contract.implementation_files.closure_mode.path
$model = [string]$contract.implementation_files.production_model.path
$resolver = [string]$contract.implementation_files.resolver.path
$generator = [string]$contract.implementation_files.generator.path
$focused = [string]$contract.implementation_files.focused_test.path
Require-Text $project '<EmbeddedResource Include="Physics\Fluids\ReferenceData\NRSVR2C4.v1.bin" LogicalName="NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin" />'
Require-Text $mode 'HistoricalCorrelationTopology = 0'
Require-Text $mode 'CorrelationConsistentInverseDomain = 1'
Require-Text $mode 'ReferenceConsistentTabulatedInverseDomain = 2'
Require-Text $model ': this(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology)'
Require-Text $model 'closureMode == WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain'
Require-Text $model '_referenceConsistentTabulatedInverseResolver.TryResolve('
Require-Text $resolver 'internal const string ExpectedPayloadSha256 = "EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267";'
Require-Text $resolver 'private static readonly Lazy<PayloadData> ProcessPayload'
Require-Text $resolver '_payload = ProcessPayload.Value;'
Require-Text $resolver '.GetManifestResourceStream(PayloadLogicalName)'
Require-Text $resolver 'throw new InvalidDataException'
Require-NotText $resolver 'IapwsIf97Reference'
Require-Text $generator 'IapwsIf97Reference.Region1'
Require-Text $generator 'IapwsIf97Reference.Region2'
Require-Text $generator 'writer.Write(Encoding.ASCII.GetBytes("NRSVR2C4"))'
Require-NotText $generator 'CultureInfo.CurrentCulture ='
Require-Text $focused 'Assert.Equal(1_679, observations.Count);'
Require-Text $focused 'Assert.Equal(0, resolveAllocationBytes);'
Require-Text $focused 'state.Temperature.Kelvins'
Require-Text $focused 'state.Pressure.Pascals'
Require-Text $focused 'Temperature.FromDegreesCelsius(state.TemperatureCelsius).Kelvins'
Require-Text $focused 'Pressure.FromMegapascals(state.PressureMegapascals).Pascals'
Require-Text $focused 'parts[8],'
Require-Text $focused 'HistoricalMode1BaselineProductionModelSha256 = "93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E"'
Require-Text $focused 'historical-mode1-density-derived-input-observation-mismatches='
Require-Text $focused 'historical-mode1-legacy-source-projection-match='

function Remove-ExactlyOnce([string]$Text,[string]$Fragment,[string]$Label) {
    $first = $Text.IndexOf($Fragment,[StringComparison]::Ordinal)
    Require ($first -ge 0) ("Historical source projection fragment missing: {0}" -f $Label)
    $second = $Text.IndexOf($Fragment,$first + $Fragment.Length,[StringComparison]::Ordinal)
    Require ($second -lt 0) ("Historical source projection fragment duplicated: {0}" -f $Label)
    return $Text.Remove($first,$Fragment.Length)
}
$legacyProjection = (Read-Text $model).Replace("`r`n","`n").Replace("`r","`n")
$legacyProjection = Remove-ExactlyOnce $legacyProjection ('    private readonly ReferenceConsistentTabulatedInverseResolver? _referenceConsistentTabulatedInverseResolver;' + "`n") 'mode2 resolver field'
$legacyProjection = Remove-ExactlyOnce $legacyProjection ((@(
    '        if (closureMode == WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain)',
    '        {',
    '            _referenceConsistentTabulatedInverseResolver = new ReferenceConsistentTabulatedInverseResolver();',
    '        }'
) -join "`n") + "`n") 'mode2 constructor dispatch'
$legacyProjection = Remove-ExactlyOnce $legacyProjection ((@(
    '        if (_referenceConsistentTabulatedInverseResolver is not null)',
    '        {',
    '            if (_referenceConsistentTabulatedInverseResolver.TryResolve(',
    '                    specificVolume,',
    '                    specificInternalEnergy,',
    '                    out var referenceConsistentState))',
    '            {',
    '                return referenceConsistentState.ToFluidThermodynamicState();',
    '            }',
    '',
    '            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);',
    '        }',
    ''
) -join "`n") + "`n") 'mode2 Resolve dispatch'
$legacyBytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($legacyProjection.Replace("`n","`r`n"))
$legacyShaProvider = [System.Security.Cryptography.SHA256]::Create()
try { $legacyProjectionSha = ([BitConverter]::ToString($legacyShaProvider.ComputeHash($legacyBytes))).Replace('-','') }
finally { $legacyShaProvider.Dispose() }
Require ($legacyProjectionSha -eq [string]$contract.implementation.historical_mode_1_baseline_production_model_sha256) 'Historical mode 1 legacy source projection does not match the Planning 1 baseline source SHA.'

foreach ($property in $contract.documentation_files.PSObject.Properties) {
    $entry = $property.Value
    Require ((Normalized-Sha256 ([string]$entry.path)) -eq ([string]$entry.normalized_sha256).ToUpperInvariant()) ("R1 documentation hash drifted: {0}" -f $property.Name)
}
$mainDoc = [string]$contract.documentation_files.main.path
$preDoc = [string]$contract.documentation_files.preexecution.path
$returnedDoc = [string]$contract.documentation_files.returned_adjudication.path
$projectDoc = [string]$contract.documentation_files.project.path
$roadmapDoc = [string]$contract.documentation_files.roadmap.path
Require-Text $mainDoc 'IMPLEMENTATION EVIDENCE ONLY'
Require-Text $mainDoc '1,679 frozen state comparisons'
Require-Text $preDoc 'STATIC PRE-EXECUTION REVIEW: PASS'
Require-Text $returnedDoc '**NOT YET RETURNED**'
Require-Text $projectDoc 'R1 SELECTED C4 OPT-IN CLOSURE IMPLEMENTATION 1.'
Require-Text $roadmapDoc '### Live R1 checkpoint -- Selected C4 opt-in closure implementation'
Require-NotText $projectDoc 'Active engineering state: M10 FINAL - VR2 R1 IMPLEMENTATION PLANNING 1.'

$mode2Hits = New-Object 'System.Collections.Generic.List[string]'
foreach ($file in Get-ChildItem -LiteralPath 'src' -File -Recurse -Filter '*.cs') {
    $relative = $file.FullName.Substring((Resolve-Path 'src').Path.Length).TrimStart([char[]]@('\','/')).Replace('\','/')
    $parts = $relative.Split('/')
    if ($parts -contains 'bin' -or $parts -contains 'obj') { continue }
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text.IndexOf('ReferenceConsistentTabulatedInverseDomain', [StringComparison]::Ordinal) -ge 0) { $mode2Hits.Add($relative) }
    Require ($text.IndexOf('IapwsIf97Reference', [StringComparison]::Ordinal) -lt 0) ("Test-only IF97 reference leaked into production src: {0}" -f $relative)
}
[string[]]$orderedHits = $mode2Hits.ToArray(); [Array]::Sort($orderedHits, [StringComparer]::Ordinal)
Require ($orderedHits.Length -eq 2) 'Mode 2 production reference count drift.'
Require ($orderedHits[0] -eq 'NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs') 'Unexpected mode 2 production callsite.'
Require ($orderedHits[1] -eq 'NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs') 'Unexpected mode 2 production identity file.'

Write-Host 'R1 selected-C4 opt-in implementation static audit: PASS' -ForegroundColor Green
Write-Host 'No default/exact-v9 activation or downstream authority is granted.'
exit 0
