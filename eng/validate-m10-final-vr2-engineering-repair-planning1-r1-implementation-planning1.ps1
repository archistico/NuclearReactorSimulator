$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Require-Text([string]$Path,[string]$Needle) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
    $text = [System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8)
    if ($text.IndexOf($Needle,[System.StringComparison]::Ordinal) -lt 0) { throw ("Required marker not found in {0}: {1}" -f $Path,$Needle) }
}
function Require-NotText([string]$Path,[string]$Needle) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ("Required file not found: {0}" -f $Path) }
    $text = [System.IO.File]::ReadAllText($Path,[System.Text.Encoding]::UTF8)
    if ($text.IndexOf($Needle,[System.StringComparison]::Ordinal) -ge 0) { throw ("Forbidden stale marker found in {0}: {1}" -f $Path,$Needle) }
}
function Get-Sha256([byte[]]$Bytes) {
    $sha=[System.Security.Cryptography.SHA256]::Create(); try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','').ToUpperInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha256([string]$Path) { return Get-Sha256 ([System.IO.File]::ReadAllBytes($Path)) }
function Get-TreeSha256([string]$Root, [string[]]$ExcludeDirectoryNames = @()) {
    $resolved=(Resolve-Path -LiteralPath $Root).Path
    $excluded=New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach($name in $ExcludeDirectoryNames) { [void]$excluded.Add($name) }
    $paths=New-Object 'System.Collections.Generic.List[string]'
    foreach($file in @(Get-ChildItem -LiteralPath $resolved -Recurse -File)) {
        $rel=$file.FullName.Substring($resolved.Length + 1).Replace('\','/')
        $segments=@($rel.Split('/'))
        $skip=$false
        for($i=0;$i -lt ($segments.Count - 1);$i++) { if($excluded.Contains($segments[$i])) { $skip=$true; break } }
        if(-not $skip) { $paths.Add($rel) }
    }
    $paths.Sort([System.StringComparer]::Ordinal)
    $ms=New-Object System.IO.MemoryStream
    try {
        foreach($rel in $paths) {
            $full=Join-Path $resolved $rel.Replace('/','\')
            $relBytes=[System.Text.Encoding]::UTF8.GetBytes($rel)
            $ms.Write($relBytes,0,$relBytes.Length); $ms.WriteByte(0)
            $sha=[System.Security.Cryptography.SHA256]::Create(); $fs=[System.IO.File]::OpenRead($full)
            try { $fh=$sha.ComputeHash($fs) } finally { $fs.Dispose(); $sha.Dispose() }
            $ms.Write($fh,0,$fh.Length); $ms.WriteByte(10)
        }
        $ms.Position=0
        $sha2=[System.Security.Cryptography.SHA256]::Create()
        try { $th=$sha2.ComputeHash($ms) } finally { $sha2.Dispose() }
        return @{ Hash=([BitConverter]::ToString($th)).Replace('-','').ToUpperInvariant(); Count=$paths.Count }
    } finally { $ms.Dispose() }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$contractPath = Join-Path $repoRoot 'eng\m10-final-vr2-engineering-repair-planning1-r1-implementation-planning1-contract.json'
$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
Require ($contract.schema -eq 'm10-final-vr2-engineering-repair-planning1-r1-implementation-planning1-v1') 'R1 Planning 1 schema mismatch.'
Require ($contract.status -eq 'PLANNING-ONLY') 'R1 Planning 1 status mismatch.'
Require ($contract.prerequisite.selection_result -eq 'SELECT-C4') 'Returned selection must be SELECT-C4.'
Require ($contract.prerequisite.selection_mode -eq 'AUTHORED-ENGINEERING-DECISION') 'Selection mode drift.'
Require (-not [bool]$contract.prerequisite.automatic_selection) 'Selection must not be automatic.'
Require ([bool]$contract.prerequisite.select_none_preserved) 'SELECT-NONE preservation missing.'
Require ($contract.prerequisite.selected_runtime_profile -eq 'AMBIENT-UNSET') 'Selected runtime profile drift.'
Require ($contract.selected_repair.new_closure_mode_name -eq 'ReferenceConsistentTabulatedInverseDomain') 'New closure-mode name drift.'
Require ([int]$contract.selected_repair.new_closure_mode_numeric_value -eq 2) 'New closure-mode numeric value drift.'
Require ($contract.selected_repair.existing_mode_0 -eq 'HistoricalCorrelationTopology') 'Historical closure-mode identity drift.'
Require ($contract.selected_repair.existing_mode_1 -eq 'CorrelationConsistentInverseDomain') 'Current closure-mode identity drift.'
Require ([bool]$contract.selected_repair.existing_modes_immutable) 'Existing closure modes must remain immutable.'
Require ([bool]$contract.selected_repair.default_constructor_unchanged) 'Default constructor must remain unchanged.'
Require ([bool]$contract.selected_repair.exact_v9_unchanged) 'exact-v9 must remain unchanged.'
Require ([bool]$contract.selected_repair.scenario_factory_selection_unchanged) 'Scenario factory selection must remain unchanged.'
Require ($contract.selected_repair.runtime_profile -eq 'AMBIENT-UNSET') 'Selected repair runtime profile drift.'
Require (-not [bool]$contract.selected_repair.direct_if97_in_production_runtime_allowed) 'Direct IF97 production runtime must remain forbidden.'
Require ([bool]$contract.selected_repair.test_only_c4_candidate_immutable) 'Selected test-only C4 must remain immutable.'
Require ($contract.production_data_contract.reference_generation -eq 'OFFLINE-CSharp-ONLY') 'Reference-data generation mode drift.'
Require ($contract.production_data_contract.payload_format -eq 'VERSIONED-BIT-EXACT-EMBEDDED-BINARY-RESOURCE') 'Reference payload format drift.'
Require ([bool]$contract.production_data_contract.checked_in_payload_required) 'Checked-in reference payload must remain required.'
Require ([bool]$contract.production_data_contract.payload_sha256_manifest_required) 'Reference payload SHA-256 manifest is required.'
Require ($contract.production_data_contract.missing_or_corrupt_payload_behavior -eq 'FAIL-CLOSED') 'Missing/corrupt payload must remain fail-closed.'
Require ([bool]$contract.production_data_contract.generation_reproducibility_test_required) 'Reference-data generation reproducibility test must remain required.'
Require (-not [bool]$contract.production_data_contract.runtime_generation_from_if97_allowed) 'Runtime IF97 generation must remain forbidden.'
Require (-not [bool]$contract.production_data_contract.build_time_implicit_regeneration_allowed) 'Implicit build-time regeneration must remain forbidden.'
Require ($contract.production_data_contract.payload_magic -eq 'NRSVR2C4') 'Reference payload magic drift.'
Require ([int]$contract.production_data_contract.payload_schema_version -eq 1) 'Reference payload schema-version drift.'
Require ($contract.production_data_contract.payload_byte_order -eq 'LITTLE-ENDIAN') 'Reference payload byte-order drift.'
Require ($contract.production_data_contract.payload_float_encoding -eq 'IEEE754-BINARY64-BITS') 'Reference payload floating-point encoding drift.'
Require ($contract.production_data_contract.payload_integrity -eq 'SHA256-FULL-PAYLOAD') 'Reference payload integrity contract drift.'
Require ([bool]$contract.production_data_contract.payload_hash_validation_required) 'Reference payload hash validation must remain required.'
Require ($contract.production_data_contract.payload_load_policy -eq 'LOAD-ONCE-PER-PROCESS') 'Reference payload load policy drift.'
Require ($contract.production_data_contract.payload_process_cache -eq 'STATIC-PROCESS-WIDE-IMMUTABLE') 'Reference payload process-cache contract drift.'
Require ($contract.production_data_contract.payload_process_cache_initialization -eq 'FIRST-MODE2-RESOLVER-CONSTRUCTION') 'Reference payload process-cache initialization drift.'
Require ([bool]$contract.production_data_contract.multiple_mode2_resolvers_reuse_same_payload) 'Mode-2 resolvers must reuse the same process-wide payload.'
Require ($contract.production_data_contract.payload_sha256_manifest_scope -eq 'R1-EVIDENCE-PROVENANCE-NOT-RUNTIME-AUTHORITY') 'Reference payload SHA-256 manifest scope drift.'
Require ($contract.production_data_contract.payload_sha256_manifest_output -eq '03-reference-data-provenance.txt') 'Reference payload SHA-256 provenance output drift.'
Require (-not [bool]$contract.production_data_contract.payload_manifest_runtime_dependency_allowed) 'Production runtime must not trust a mutable SHA-256 manifest file.'
Require ($contract.production_data_contract.payload_resource_path -eq 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin') 'Reference payload resource path drift.'
Require ($contract.production_data_contract.payload_logical_name -eq 'NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin') 'Reference payload logical name drift.'
Require ($contract.production_data_contract.payload_hash_anchor -eq 'COMPILED-CSharp-CONSTANT-IN-PRODUCTION-SOURCE') 'Reference payload hash-anchor drift.'
Require ($contract.production_data_contract.payload_item_type -eq 'EmbeddedResource') 'Reference payload item type drift.'
Require (-not [bool]$contract.production_data_contract.payload_copy_to_output_allowed) 'Reference payload must not be copied as loose output.'
Require ($contract.production_data_contract.payload_initialization_point -eq 'MODE2-RESOLVER-CONSTRUCTION') 'Reference payload initialization-point drift.'
Require (-not [bool]$contract.production_data_contract.lazy_first_resolve_loading_allowed) 'Lazy first-resolve payload loading must remain forbidden.'
Require ([bool]$contract.production_data_contract.loaded_structures_immutable) 'Loaded payload structures must remain immutable.'
Require ($contract.production_data_contract.generator_culture -eq 'INVARIANT') 'Reference generator culture drift.'
Require ($contract.production_data_contract.generator_order -eq 'FROZEN-CANONICAL') 'Reference generator ordering drift.'
Require (-not [bool]$contract.production_data_contract.resolve_time_payload_io_allowed) 'Resolve-time payload I/O must remain forbidden.'
Require (-not [bool]$contract.production_data_contract.resolve_time_payload_decode_allowed) 'Resolve-time payload decoding must remain forbidden.'
Require (-not [bool]$contract.production_data_contract.resolve_time_allocation_allowed) 'Resolve-time payload allocation must remain forbidden.'
Require ($contract.production_data_contract.corrupt_payload_exception -eq 'InvalidDataException') 'Corrupt payload exception contract drift.'
Require ($contract.production_data_contract.generator_owner -eq 'TEST-ONLY-CSharp-IN-SIMULATION-TEST-PROJECT') 'Reference-data generator ownership drift.'
Require (-not [bool]$contract.production_data_contract.generator_python_allowed) 'Reference-data generator must remain C#/PowerShell only.'
Require ([bool]$contract.implementation_scope.simulation_project_file_change_required) 'Simulation project-file change must be planned for embedded payload.'
Require ([bool]$contract.implementation_scope.embedded_resource_include_required) 'Explicit embedded-resource include must remain required.'
Require ($contract.implementation_scope.allowed_production_area -eq 'src/NuclearReactorSimulator.Simulation') 'R1 production-area boundary drift.'
Require ($contract.implementation_scope.new_production_source_area -eq 'src/NuclearReactorSimulator.Simulation/Physics/Fluids') 'R1 new production-source area drift.'
Require ($contract.implementation_scope.reference_payload_area -eq 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData') 'R1 reference-payload area drift.'
Require ($contract.implementation_scope.allowed_test_generation_area -eq 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference') 'R1 test-only generator area drift.'
$expectedExistingProductionFiles=@(
 'src/NuclearReactorSimulator.Simulation/NuclearReactorSimulator.Simulation.csproj',
 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs',
 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs'
)
Require (@($contract.implementation_scope.expected_existing_files_modified).Count -eq $expectedExistingProductionFiles.Count) 'Existing production-file allowlist count drift.'
for($i=0;$i -lt $expectedExistingProductionFiles.Count;$i++) { Require ($contract.implementation_scope.expected_existing_files_modified[$i] -eq $expectedExistingProductionFiles[$i]) ("Existing production-file allowlist drift at index {0}." -f $i) }
Require ([int]$contract.implementation_scope.existing_production_file_modification_count -eq 3) 'Existing production-file modification count drift.'
$expectedNewProductionFiles=@(
 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceConsistentTabulatedInverseResolver.cs',
 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin'
)
Require (@($contract.implementation_scope.expected_new_production_files).Count -eq $expectedNewProductionFiles.Count) 'New production-file allowlist count drift.'
for($i=0;$i -lt $expectedNewProductionFiles.Count;$i++) { Require ($contract.implementation_scope.expected_new_production_files[$i] -eq $expectedNewProductionFiles[$i]) ("New production-file allowlist drift at index {0}." -f $i) }
Require ([int]$contract.implementation_scope.new_production_file_count -eq 2) 'New production-file count drift.'
$expectedNewTestFiles=@(
 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/NrsVr2C4ReferencePayloadGenerator.cs',
 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/R1SelectedC4OptInClosureImplementationTests.cs'
)
Require (@($contract.implementation_scope.expected_new_test_files).Count -eq $expectedNewTestFiles.Count) 'New R1 test-file allowlist count drift.'
for($i=0;$i -lt $expectedNewTestFiles.Count;$i++) { Require ($contract.implementation_scope.expected_new_test_files[$i] -eq $expectedNewTestFiles[$i]) ("New R1 test-file allowlist drift at index {0}." -f $i) }
Require ([int]$contract.implementation_scope.new_test_file_count -eq 2) 'New R1 test-file count drift.'
Require (-not [bool]$contract.implementation_scope.application_callsite_change_allowed) 'Application call-site changes are outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.domain_change_allowed) 'Domain changes are outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.infrastructure_change_allowed) 'Infrastructure changes are outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.default_switch_allowed) 'Default switch is outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.exact_version_creation_allowed) 'Exact-version creation is outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.existing_save_reinterpretation_allowed) 'Existing save reinterpretation is outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.solution_file_change_allowed) 'Solution-file changes are outside R1 scope.'
Require (-not [bool]$contract.implementation_scope.unrelated_project_file_change_allowed) 'Unrelated project-file changes are outside R1 scope.'
Require ([bool]$contract.implementation_scope.existing_production_file_modification_allowlist_only) 'Existing production-file modification allowlist must remain exact.'
Require (-not [bool]$contract.implementation_scope.production_file_deletion_allowed) 'Production-file deletion must remain forbidden.'
Require (-not [bool]$contract.implementation_scope.new_production_source_outside_area_allowed) 'New production source outside the bounded area must remain forbidden.'
Require (-not [bool]$contract.implementation_scope.reference_payload_outside_area_allowed) 'Reference payload outside the bounded area must remain forbidden.'

$expectedPrecedence=@(
 'C3-SUPERHEATED-VAPOR-SEAM',
 'C2-MIXTURE-PREFIX',
 'C2-LIQUID-TABLE-PREFIX',
 'C2-NEAR-BOUNDARY-LIQUID-PREFIX',
 'IMMUTABLE-C2-FALLBACK-EQUIVALENT',
 'C3-SATURATED-VAPOR-SEAM-FALLBACK'
)
Require (@($contract.resolution_precedence).Count -eq $expectedPrecedence.Count) 'Resolution-precedence count drift.'
for($i=0;$i -lt $expectedPrecedence.Count;$i++) { Require ($contract.resolution_precedence[$i] -eq $expectedPrecedence[$i]) ("Resolution-precedence drift at index {0}." -f $i) }

Require ($contract.future_r1_gate.id -eq 'R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1') 'Future R1 gate id drift.'
Require ([int]$contract.future_r1_gate.required_equivalence_comparisons -eq 1967) 'R1 equivalence comparison count drift.'
Require ([int]$contract.future_r1_gate.required_state_comparisons -eq 1679) 'R1 state-comparison corpus size drift.'
Require ([int]$contract.future_r1_gate.required_hydraulic_comparisons -eq 288) 'R1 hydraulic-comparison corpus size drift.'
Require (([int]$contract.future_r1_gate.required_state_comparisons + [int]$contract.future_r1_gate.required_hydraulic_comparisons) -eq [int]$contract.future_r1_gate.required_equivalence_comparisons) 'R1 semantic corpus total is inconsistent.'
Require ([bool]$contract.future_r1_gate.semantic_identity_uniqueness_required) 'Semantic identity uniqueness must remain required.'
Require ([bool]$contract.future_r1_gate.mode2_dispatch_before_existing_resolution_pipeline) 'Mode 2 must dispatch before the existing mode 0/1 resolution pipeline.'
Require (-not [bool]$contract.future_r1_gate.mode2_fallthrough_to_modes_0_or_1_allowed) 'Mode 2 must not fall through into mode 0/1 semantics.'
Require (-not [bool]$contract.future_r1_gate.mode2_resolve_time_allocation_allowed) 'Mode 2 resolve-time allocation must remain forbidden.'
Require (-not [bool]$contract.future_r1_gate.mode2_resolve_time_resource_io_allowed) 'Mode 2 resolve-time resource I/O must remain forbidden.'
Require ([int]$contract.future_r1_gate.required_resolve_allocation_bytes -eq 0) 'Mode 2 resolve allocation-byte requirement drift.'
Require ([int]$contract.future_r1_gate.required_payload_runtime_io_count -eq 0) 'Mode 2 payload runtime-I/O allowance drift.'
Require ([int]$contract.future_r1_gate.required_payload_decode_count -eq 0) 'Mode 2 payload decode allowance drift.'
Require (@($contract.future_r1_gate.historical_mode_regression_modes).Count -eq 2) 'Historical-mode regression mode count drift.'
Require ([int]$contract.future_r1_gate.historical_mode_regression_modes[0] -eq 0 -and [int]$contract.future_r1_gate.historical_mode_regression_modes[1] -eq 1) 'Historical-mode regression must cover modes 0 and 1 exactly.'
Require ($contract.future_r1_gate.payload_reproducibility_method -eq 'REGENERATE-TWICE-TO-TEMP-COMPARE-BYTES-TO-CHECKED-IN-PAYLOAD-AND-COMPILED-SHA256') 'Payload reproducibility method drift.'
Require ([int]$contract.future_r1_gate.production_change_manifest_existing_modified_count -eq 3) 'Future R1 existing-production modification count drift.'
Require ([int]$contract.future_r1_gate.production_change_manifest_new_production_count -eq 2) 'Future R1 new-production file count drift.'
Require ([int]$contract.future_r1_gate.test_change_manifest_new_test_count -eq 2) 'Future R1 new-test file count drift.'
Require ([bool]$contract.future_r1_gate.production_change_manifest_exact_scope_required) 'Future R1 production change manifest must prove exact bounded scope.'
Require ([int]$contract.future_r1_gate.required_state_bit_mismatches -eq 0) 'State-bit mismatch allowance drift.'
Require ([int]$contract.future_r1_gate.required_hydraulic_bit_mismatches -eq 0) 'Hydraulic-bit mismatch allowance drift.'
Require ([int]$contract.future_r1_gate.required_repeat_mismatches -eq 0) 'Repeat mismatch allowance drift.'
Require ([int]$contract.future_r1_gate.required_resolve_mismatches -eq 0) 'Resolve mismatch allowance drift.'
Require ([bool]$contract.future_r1_gate.production_source_change_bounded_to_plan) 'R1 production source change must remain bounded to the returned plan.'
Require (-not [bool]$contract.future_r1_gate.new_measurement_campaign_allowed) 'R1 implementation must not open a new measurement campaign.'
Require ([bool]$contract.future_r1_gate.ordinary_release_suite_required) 'Ordinary Release suite must remain required.'
Require ([bool]$contract.future_r1_gate.historical_modes_regression_required) 'Historical-mode regression must remain required.'
Require ([bool]$contract.future_r1_gate.reference_payload_reproducibility_required) 'Reference payload reproducibility must remain required.'
Require (-not [bool]$contract.future_r1_gate.runtime_override_allowed) 'R1 runtime override must remain forbidden.'
Require (-not [bool]$contract.future_r1_gate.default_activation_allowed) 'R1 must not activate the new mode by default.'
Require (-not [bool]$contract.future_r1_gate.exact_v9_activation_allowed) 'R1 must not activate the new mode in exact-v9.'
Require (-not [bool]$contract.future_r1_gate.new_exact_version_allowed) 'R1 must not create a new exact version.'
Require ($contract.future_r1_gate.post_successor -eq 'R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION-PLANNING-ONLY') 'R1 successor boundary drift.'
Require ([bool]$contract.future_r1_gate.returned_adjudication_required_before_r2) 'Returned R1 adjudication must remain required before R2.'
Require ([int]$contract.future_r1_gate.required_files -eq 7) 'Future R1 evidence-shape drift.'
$expectedFutureOutputs=@(
 '01-contract-and-provenance.txt',
 '02-production-change-manifest.csv',
 '03-reference-data-provenance.txt',
 '04-c4-production-equivalence-summary.txt',
 '05-historical-mode-regression-summary.txt',
 '06-r1-implementation-summary.txt',
 '07-prequalification-review.txt'
)
Require (@($contract.future_r1_gate.outputs).Count -eq $expectedFutureOutputs.Count) 'Future R1 output-list count drift.'
for($i=0;$i -lt $expectedFutureOutputs.Count;$i++) { Require ($contract.future_r1_gate.outputs[$i] -eq $expectedFutureOutputs[$i]) ("Future R1 output-list drift at index {0}." -f $i) }
$expectedPlanningOutputs=@('01-contract-and-provenance.txt','02-production-integration-map.csv','03-implementation-and-requalification-summary.txt','04-preimplementation-review.txt')
Require (@($contract.planning_outputs).Count -eq $expectedPlanningOutputs.Count) 'Planning output-list count drift.'
for($i=0;$i -lt $expectedPlanningOutputs.Count;$i++) { Require ($contract.planning_outputs[$i] -eq $expectedPlanningOutputs[$i]) ("Planning output-list drift at index {0}." -f $i) }
foreach ($p in $contract.authority.PSObject.Properties) { Require (-not [bool]$p.Value) ("Authority must remain false: {0}" -f $p.Name) }

$selectionRoot=Join-Path $repoRoot 'eng\frozen-evidence\ordinary\M10FinalVR2EngineeringRepairPlanning1_RP1C_Selection1_Artifacts'
$selectionProperties=@($contract.selection_artifacts_sha256.PSObject.Properties)
Require ($selectionProperties.Count -eq 4) 'Returned selection artifact hash manifest must contain exactly four files.'
foreach($property in $selectionProperties) {
    $name=$property.Name
    $expectedHash=[string]$property.Value
    $p=Join-Path $selectionRoot $name
    Require (Test-Path -LiteralPath $p -PathType Leaf) ("Missing returned selection artifact: {0}" -f $name)
    Require ((Get-FileSha256 $p) -eq $expectedHash) ("Returned selection artifact hash drift: {0}" -f $name)
}
Require-Text (Join-Path $selectionRoot '01-selection-decision-and-provenance.txt') 'selection-result=SELECT-C4'
Require-Text (Join-Path $selectionRoot '01-selection-decision-and-provenance.txt') 'automatic-selection=False'
Require-Text (Join-Path $selectionRoot '01-selection-decision-and-provenance.txt') 'select-none-preserved=True'
Require-Text (Join-Path $selectionRoot '03-selection-decision-summary.txt') 'post-selection-successor=R1-IMPLEMENTATION-PLANNING-ONLY'
Require-Text (Join-Path $selectionRoot '04-preexecution-review.txt') 'finding-6=NO-NEW-MEASUREMENT-OR-MUTATION'

$stateCorpus=Join-Path $repoRoot ($contract.future_r1_gate.state_corpus_source.Replace('/','\'))
$hydraulicCorpus=Join-Path $repoRoot ($contract.future_r1_gate.hydraulic_corpus_source.Replace('/','\'))
Require (Test-Path -LiteralPath $stateCorpus -PathType Leaf) 'Frozen R1 state semantic corpus is missing.'
Require (Test-Path -LiteralPath $hydraulicCorpus -PathType Leaf) 'Frozen R1 hydraulic semantic corpus is missing.'
Require ((Get-FileSha256 $stateCorpus) -eq $contract.future_r1_gate.state_corpus_sha256) 'Frozen R1 state semantic corpus hash drift.'
Require ((Get-FileSha256 $hydraulicCorpus) -eq $contract.future_r1_gate.hydraulic_corpus_sha256) 'Frozen R1 hydraulic semantic corpus hash drift.'
$stateRows=@(Import-Csv -LiteralPath $stateCorpus)
$hydraulicRows=@(Import-Csv -LiteralPath $hydraulicCorpus)
Require ($stateRows.Count -eq [int]$contract.future_r1_gate.required_state_comparisons) 'Frozen R1 state semantic corpus row-count drift.'
Require ($hydraulicRows.Count -eq [int]$contract.future_r1_gate.required_hydraulic_comparisons) 'Frozen R1 hydraulic semantic corpus row-count drift.'
$stateIds=@($stateRows | ForEach-Object { '{0}|{1}|{2}' -f $_.domain,$_.observation_index,$_.source_identity })
$hydraulicIds=@($hydraulicRows | ForEach-Object { '{0}|{1}|{2}|{3}|{4}|{5}|{6}' -f $_.domain,$_.observation_index,$_.probe_id,$_.logical_step,$_.path_id,$_.from_node,$_.to_node })
$stateIdentitySet=New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach($id in $stateIds) { Require ($stateIdentitySet.Add($id)) 'Frozen R1 state semantic corpus contains duplicate identities.' }
$hydraulicIdentitySet=New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach($id in $hydraulicIds) { Require ($hydraulicIdentitySet.Add($id)) 'Frozen R1 hydraulic semantic corpus contains duplicate identities.' }

$srcTree=Get-TreeSha256 (Join-Path $repoRoot 'src') @('bin','obj')
Require ($srcTree.Count -eq [int]$contract.baseline.src_file_count) 'src file-count drift from Selection 1 baseline.'
Require ($srcTree.Hash -eq $contract.baseline.src_tree_sha256) 'src tree drift from Selection 1 baseline.'
$testsTree=Get-TreeSha256 (Join-Path $repoRoot 'tests') @('bin','obj')
Require ($testsTree.Count -eq [int]$contract.baseline.tests_file_count) 'tests file-count drift from Selection 1 baseline.'
Require ($testsTree.Hash -eq $contract.baseline.tests_tree_sha256) 'tests tree drift from Selection 1 baseline.'

$c4=Join-Path $repoRoot 'tests\NuclearReactorSimulator.Simulation.Tests\Physics\Fluids\Reference\Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs'
$mode=Join-Path $repoRoot 'src\NuclearReactorSimulator.Simulation\Physics\Fluids\WaterSteamThermodynamicClosureMode.cs'
$model=Join-Path $repoRoot 'src\NuclearReactorSimulator.Simulation\Physics\Fluids\SimplifiedWaterSteamThermodynamicModel.cs'
$simulationProject=Join-Path $repoRoot 'src\NuclearReactorSimulator.Simulation\NuclearReactorSimulator.Simulation.csproj'
Require ((Get-FileSha256 $c4) -eq $contract.baseline_files_sha256.c4_test_candidate) 'Frozen C4 test candidate drift.'
Require ((Get-FileSha256 $mode) -eq $contract.baseline_files_sha256.closure_mode) 'Closure-mode source drift during planning.'
Require ((Get-FileSha256 $model) -eq $contract.baseline_files_sha256.production_model) 'Production thermodynamic model drift during planning.'
Require ((Get-FileSha256 $simulationProject) -eq $contract.baseline_files_sha256.simulation_project) 'Simulation project-file drift during planning.'
Require-Text $mode 'HistoricalCorrelationTopology = 0'
Require-Text $mode 'CorrelationConsistentInverseDomain = 1'
$modeText=[System.IO.File]::ReadAllText($mode,[System.Text.Encoding]::UTF8)
Require ($modeText.IndexOf('ReferenceConsistentTabulatedInverseDomain',[System.StringComparison]::Ordinal) -lt 0) 'Planning candidate must not implement mode 2 yet.'
$projectText=[System.IO.File]::ReadAllText($simulationProject,[System.Text.Encoding]::UTF8)
Require ($projectText.IndexOf('NRSVR2C4',[System.StringComparison]::Ordinal) -lt 0) 'Planning candidate must not embed the future R1 payload yet.'
foreach($plannedNewFile in @($contract.implementation_scope.expected_new_production_files + $contract.implementation_scope.expected_new_test_files)) {
    Require (-not (Test-Path -LiteralPath (Join-Path $repoRoot ($plannedNewFile.Replace('/','\'))))) ("Planning candidate must not contain future R1 file yet: {0}" -f $plannedNewFile)
}

$main=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1.md'
$pre=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1_PREEXECUTION_REVIEW.md'
$ret=Join-Path $repoRoot 'docs\M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md'
$adr=Join-Path $repoRoot 'docs\adr\0211-stage-selected-c4-behind-new-opt-in-production-closure-mode-before-requalification.md'
Require-Text $main 'ReferenceConsistentTabulatedInverseDomain = 2'
Require-Text $main 'generated **offline in C#**'
Require-Text $main 'production code has no test-assembly or direct IF97 runtime dependency'
Require-Text $main 'NuclearReactorSimulator.Simulation.csproj'
Require-Text $main 'magic             = NRSVR2C4'
Require-Text $main 'logical name       = NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin'
Require-Text $main 'hash anchor        = compiled C# constant in production source'
Require-Text $main 'Lazy first-resolve loading is forbidden.'
Require-Text $main 'static process-wide immutable cache'
Require-Text $main 'R1 evidence/provenance output only, not a runtime authority file'
Require-Text $main 'ReferenceConsistentTabulatedInverseResolver.cs'
Require-Text $main 'NrsVr2C4ReferencePayloadGenerator.cs'
Require-Text $main 'R1SelectedC4OptInClosureImplementationTests.cs'
Require-Text $main 'exact modification allowlist'
Require-Text $main 'Mode 2 must dispatch to its dedicated resolver **before** entering the existing mode-0/mode-1 resolution pipeline.'
Require-Text $main '1,679 state rows'
Require-Text $pre 'this planning candidate contains no production source or test modifications.'
Require-Text $pre 'process-wide immutable cache'
Require-Text $pre 'exact future production/test file allowlist'
Require-Text $ret 'future-gate=R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1'
Require-Text $ret 'reference-payload-schema=NRSVR2C4-v1'
Require-Text $ret 'semantic-state-comparisons=1679'
Require-Text $ret 'semantic-hydraulic-comparisons=288'
Require-Text $ret 'payload-provenance-output=03-reference-data-provenance.txt'
Require-Text $adr 'Preserve values 0 and 1'
Require-Text (Join-Path $repoRoot 'docs\PROJECT.md') 'The only live activity is planning-only `R1-IMPLEMENTATION-PLANNING1`.'
Require-NotText (Join-Path $repoRoot 'docs\PROJECT.md') 'The active candidate is now **Runtime Configuration Impact Assessment 1**'
Require-NotText (Join-Path $repoRoot 'docs\PROJECT.md') 'the active candidate is `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1`'
Require-NotText (Join-Path $repoRoot 'docs\README.md') 'The only live gate is planning-only `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`.'
Require-NotText (Join-Path $repoRoot 'README.md') 'The live gate is planning-only **RP1C Selection Planning 1**'
Require-NotText (Join-Path $repoRoot 'README.md') '### Current M10 RP1C checkpoint'
Require-Text (Join-Path $repoRoot 'docs\ROADMAP.md') '### Live post-selection checkpoint -- R1 Implementation Planning 1'
Require-Text (Join-Path $repoRoot 'docs\README.md') '### Live M10 VR2 checkpoint -- Selection returned / R1 Implementation Planning 1'
Require-Text (Join-Path $repoRoot 'docs\M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md') 'The currently authorized action is planning-only `R1-IMPLEMENTATION-PLANNING1`.'
Require-Text (Join-Path $repoRoot 'docs\TOP_LEVEL_DOCUMENT_INDEX.md') 'M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1.md'
Require-Text (Join-Path $repoRoot 'docs\adr\README.md') 'ADR-0211'

foreach($futurePath in @($contract.future_r1_gate.contract,$contract.future_r1_gate.validator,$contract.future_r1_gate.runner)) {
    Require (-not (Test-Path -LiteralPath (Join-Path $repoRoot ($futurePath.Replace('/','\'))))) ("Future R1 implementation must not be contained in planning candidate: {0}" -f $futurePath)
}

$artifactDir=Join-Path $repoRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-r1-implementation-planning1'
if (Test-Path -LiteralPath $artifactDir) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '01-contract-and-provenance.txt'), @(
 'status=PASS-AS-AUTHORED',
 'gate=R1-IMPLEMENTATION-PLANNING1',
 'contract-schema=m10-final-vr2-engineering-repair-planning1-r1-implementation-planning1-v1',
 'selection-returned-adjudication=PASS',
 'selection-result=SELECT-C4',
 'selected-candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE',
 'selected-runtime-profile=AMBIENT-UNSET',
 'new-closure-mode=ReferenceConsistentTabulatedInverseDomain',
 'new-closure-mode-value=2',
 'existing-modes-immutable=True',
 'exact-v9-unchanged=True',
 'direct-if97-production-runtime=False',
 'reference-data-generation=OFFLINE-CSharp-ONLY',
 'reference-payload-schema=NRSVR2C4-v1',
 'reference-payload-logical-name=NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin',
 'reference-payload-hash-anchor=COMPILED-CSharp-CONSTANT-IN-PRODUCTION-SOURCE',
 'payload-load-point=MODE2-RESOLVER-CONSTRUCTION',
 'payload-process-cache=STATIC-PROCESS-WIDE-IMMUTABLE',
 'payload-manifest-runtime-authority=False',
 'payload-provenance-output=03-reference-data-provenance.txt',
 'simulation-project-resource-include-planned=True',
 'future-existing-production-modifications=3',
 'future-new-production-files=2',
 'future-new-test-files=2',
 'mode2-dedicated-dispatch=True',
 'semantic-state-comparisons=1679',
 'semantic-hydraulic-comparisons=288',
 'future-gate=R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1',
 'future-required-files=7',
 'r1-implementation-authorized=False',
 'production-default-switch-authorized=False'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '02-production-integration-map.csv'), @(
 'area,item,planning_action,implementation_boundary',
 'existing-production,src/NuclearReactorSimulator.Simulation/NuclearReactorSimulator.Simulation.csproj,MODIFY-LATER,EXPLICIT-EMBEDDEDRESOURCE-LOGICALNAME',
 'existing-production,src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs,MODIFY-LATER,ADD-VALUE-2',
 'existing-production,src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs,MODIFY-LATER,DEDICATED-MODE2-DISPATCH-NO-FALLTHROUGH',
 'new-production,src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceConsistentTabulatedInverseResolver.cs,ADD-LATER,STATIC-PROCESS-WIDE-IMMUTABLE-CACHE',
 'new-production,src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin,ADD-LATER,VERSIONED-BIT-EXACT-EMBEDDED-PAYLOAD',
 'new-test,tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/NrsVr2C4ReferencePayloadGenerator.cs,ADD-LATER,OFFLINE-CSharp-DETERMINISTIC-GENERATOR',
 'new-test,tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/R1SelectedC4OptInClosureImplementationTests.cs,ADD-LATER,EQUIVALENCE-REGRESSION-INTEGRITY-GATE',
 'selected-shadow,Rp1bC4AllocationNeutralShadowThermodynamicCandidate,KEEP-IMMUTABLE,EQUIVALENCE-ORACLE',
 'application-call-sites,exact-v9-and-scenario-factories,NO-CHANGE,NO-ACTIVATION',
 'historical-modes,enum-values-0-and-1,NO-CHANGE,PRESERVE-HISTORY'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '03-implementation-and-requalification-summary.txt'), @(
 'status=PASS-R1-IMPLEMENTATION-PLANNING-CONTRACT-FROZEN',
 'implementation-result=NOT-PERFORMED',
 'new-closure-mode=ReferenceConsistentTabulatedInverseDomain',
 'new-closure-mode-value=2',
 'production-if97-runtime-dependency=False',
 'reference-data-payload=VERSIONED-BIT-EXACT-EMBEDDED-BINARY-RESOURCE',
 'reference-data-generation=OFFLINE-CSharp-ONLY',
 'reference-payload-schema=NRSVR2C4-v1',
 'reference-payload-cache=STATIC-PROCESS-WIDE-IMMUTABLE',
 'reference-payload-reproducibility=REGENERATE-TWICE-TO-TEMP-COMPARE-BYTES-TO-CHECKED-IN-PAYLOAD-AND-COMPILED-SHA256',
 'historical-mode-regression=MODE0-AND-MODE1',
 'semantic-state-comparisons-required=1679',
 'semantic-hydraulic-comparisons-required=288',
 'production-vs-c4-semantic-comparisons-required=1967',
 'state-bit-mismatches-allowed=0',
 'hydraulic-bit-mismatches-allowed=0',
 'repeat-mismatches-allowed=0',
 'resolve-mismatches-allowed=0',
 'resolve-allocation-bytes-allowed=0',
 'payload-runtime-io-count-allowed=0',
 'payload-decode-count-allowed=0',
 'default-activation=False',
 'exact-v9-activation=False',
 'post-r1-successor=R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION-PLANNING-ONLY',
 'next-action=RETURN-COMPLETE-R1-IMPLEMENTATION-PLANNING-ARTIFACTS-FOR-ADJUDICATION'
),[System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllLines((Join-Path $artifactDir '04-preimplementation-review.txt'), @(
 'status=STATIC-PREIMPLEMENTATION-REVIEW-PASS',
 'finding-1=RP1C-SELECTION-RETURNED-SELECT-C4',
 'finding-2=EXISTING-CLOSURE-MODES-0-AND-1-IMMUTABLE',
 'finding-3=NEW-MODE-2-OPT-IN-ONLY',
 'finding-4=EXACT-V9-AND-DEFAULT-CONSTRUCTION-UNCHANGED',
 'finding-5=PRODUCTION-IF97-RUNTIME-DEPENDENCY-FORBIDDEN',
 'finding-6=OFFLINE-CSharp-REFERENCE-DATA-PROVENANCE-REQUIRED',
 'finding-7=NRSVR2C4-V1-EMBEDDED-RESOURCE-PROJECT-BOUNDARY-REQUIRED',
 'finding-8=MODE2-DEDICATED-DISPATCH-NO-FALLTHROUGH',
 'finding-9=FROZEN-1679-STATE-PLUS-288-HYDRAULIC-CORPUS-IDENTITY-REQUIRED',
 'finding-10=RESOLVE-TIME-PAYLOAD-IO-DECODE-ALLOCATION-FORBIDDEN',
 'finding-11=STATIC-PROCESS-WIDE-IMMUTABLE-PAYLOAD-CACHE-REQUIRED',
 'finding-12=EXACT-FUTURE-PRODUCTION-AND-TEST-FILE-ALLOWLIST-FROZEN',
 'finding-13=FROZEN-C4-BIT-EQUIVALENCE-REQUIRED-BEFORE-R2',
 'future-r1-implementation-contained=False',
 'r1-implementation-authorized=False',
 'production-default-switch-authorized=False'
),[System.Text.Encoding]::UTF8)
$files=@(Get-ChildItem -LiteralPath $artifactDir -File)
Require ($files.Count -eq 4) 'R1 Implementation Planning 1 must write exactly four artifacts.'
Write-Host 'R1 Implementation Planning 1 static audit: PASS-AS-AUTHORED' -ForegroundColor Green
Write-Host ("Artifacts: {0}" -f $artifactDir)
