$ErrorActionPreference = 'Stop'

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}

function Require-Text([string]$Path, [string]$Needle) {
    Require-File $Path
    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if (-not $text.Contains($Needle)) {
        throw ("Required marker not found in {0}: {1}" -f $Path, $Needle)
    }
}

Write-Host '============================================================'
Write-Host 'M10 FINAL - VR0 REFERENCE / PROVENANCE CONTRACT FREEZE'
Write-Host '============================================================'
Write-Host 'Documentation/reference-contract-only audit. This does not execute'
Write-Host 'VR1-VR4, change production physics, execute P3-R1 or authorize a second long.'
Write-Host ''

$amendment3Summary = 'eng/frozen-evidence/ordinary/M10FinalPlanAmendment3_PhysicalReferenceAssessment_UserReportedValidatedSummary.txt'
Require-Text $amendment3Summary 'm10-final-plan-amendment3-physical-reference-assessment-passes=True'
Require-Text $amendment3Summary 'next-authorized-gate=VR0-Reference-Provenance-Contract-Freeze'

Require-Text 'docs/PROJECT.md' 'VR0 REFERENCE / PROVENANCE CONTRACT FREEZE'
Require-Text 'docs/PROJECT.md' 'Plan Amendment 3 is VALIDATED'
Require-Text 'docs/M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md' 'VR0-REFERENCE-CONTRACT-PASS'
Require-Text 'docs/M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md' 'VR1-KP50'
Require-Text 'docs/M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md' 'IAPWS R7-97(2012)'
Require-Text 'docs/M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md' '5.0e13 n/cm2/s'
Require-Text 'docs/M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md' 'REFERENCE-GAP-NO-CANONICAL-PRODUCTION-CONFIG'
Require-Text 'docs/research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md' 'Applied Reactor Physics'
Require-Text 'docs/research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md' 'IAPWS'
Require-Text 'docs/research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md' 'Introduction to Nuclear Engineering'
Require-Text 'docs/research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md' 'ANSI/ANS-5.1-2014 (R2023)'
Require-Text 'docs/research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md' 'VR2 water/steam'
Require-Text 'docs/research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md' 'no canonical production model in exact-v9'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'VR0 is now the active candidate'
Require-Text 'docs/M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_PLAN_AMENDMENT3_EXTERNAL_MODEL_ASSESSMENT.md' 'Plan Amendment 3 is VALIDATED'
Require-Text 'docs/ROADMAP.md' 'VR0 Reference/Provenance Contract Freeze'
Require-Text 'docs/M10_FINAL_VV_MATRIX.md' 'VR0 reference/provenance contract'

# Runtime applicability markers: verify that VR0 did not invent ownership that the source tree contradicts.
Require-Text 'src/NuclearReactorSimulator.Simulation/Physics/Reactor/Neutronics/PointKineticsSolver.cs' 'Deterministic point-reactor kinetics with delayed-neutron groups.'
Require-Text 'src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs' 'IAPWS-IF97 Region-4 saturation-pressure equation'
Require-Text 'src/NuclearReactorSimulator.Application/Scenarios/Xenon/AdvancedXenonModelConfiguration.cs' 'educational configuration-relative coefficients'
Require-Text 'src/NuclearReactorSimulator.Application/Scenarios/PreStartup/ColdShutdownInitialConditionFactory.cs' 'primary, AggregatedCoreState.CreateNominal(core), Power.Zero, Power.Zero, primaryBoundaryInputs'

$productionDecayDefinitions = Get-ChildItem -LiteralPath 'src' -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '[\\/]NuclearReactorSimulator\.Domain[\\/]' } |
    Select-String -SimpleMatch 'new DecayHeatDefinition(' -List
if ($productionDecayDefinitions) {
    throw 'VR0 applicability audit expected no canonical production new DecayHeatDefinition(...) construction outside Domain.'
}

$contractPath = 'eng/m10-final-physical-reference-vr0-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($contract.documentation_reference_only -ne $true) { throw 'VR0 must remain documentation_reference_only=true.' }
if ($contract.production_src_change_authorized -ne $false) { throw 'VR0 cannot authorize production source changes.' }
if ($contract.production_repair_authorized -ne $false) { throw 'VR0 cannot authorize production repair.' }
if ($contract.exact_v9_change_authorized -ne $false) { throw 'VR0 cannot authorize exact-v9 changes.' }
if ($contract.p3_r1_execution_authorized -ne $false) { throw 'VR0 cannot execute P3-R1.' }
if ($contract.second_replacement_long_authorized -ne $false) { throw 'VR0 cannot authorize a second replacement long.' }
if ($contract.reference_implementation_language -ne 'CSharp-test-only') { throw 'VR reference implementation must remain test-only C#.' }
if ($contract.python_project_tooling_authorized -ne $false) { throw 'VR0 must not add Python project tooling.' }
if ($contract.noncircularity.production_may_generate_reference_values -ne $false) { throw 'Production cannot generate reference values.' }
if ($contract.noncircularity.reference_may_call_production_solver -ne $false) { throw 'Reference path cannot call production solver.' }
if ($contract.vr1.six_group.beta_total -ne 0.007) { throw 'Unexpected frozen VR1 beta total.' }
if ($contract.vr1.six_group.reactivity_cases_delta_k_over_k.Count -ne 5) { throw 'VR1 must freeze exactly five canonical reactivity cases.' }
if ($contract.vr2.saturation_full_property_temperatures_celsius.Count -lt 10) { throw 'VR2 saturation matrix is unexpectedly small.' }
if ($contract.vr2.compressed_liquid_points.Count -lt 5) { throw 'VR2 compressed-liquid matrix is unexpectedly small.' }
if ($contract.vr2.superheated_vapor_points.Count -lt 4) { throw 'VR2 superheated matrix is unexpectedly small.' }
if ($contract.vr3.pre_shutdown_thermal_flux_per_cm2_second -ne 5e13) { throw 'Unexpected VR3 pre-shutdown flux.' }
if ($contract.vr3.exact_v9_materiality -ne 'INACTIVE-IN-SUSTAINED-GENERATION-PATH') { throw 'VR3 exact-v9 applicability drifted.' }
if ($contract.vr4.canonical_production_decay_heat_definition_exists -ne $false) { throw 'VR4 incorrectly claims a canonical production decay-heat definition.' }
if ($contract.vr4.exact_v9_total_decay_heat_active -ne $false) { throw 'VR4 incorrectly claims exact-v9 active decay heat.' }
if ($contract.next_authorized_gate_on_pass -ne 'VR1-Point-Kinetics-Independent-Benchmark') { throw 'Unexpected next gate after VR0.' }

$artifactDir = 'artifacts/m10-final-physical-reference-vr0'
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$summaryOut = Join-Path $artifactDir '01-vr0-reference-contract-summary.txt'
@(
    'vr0-reference-contract-passes=True',
    'vr0-disposition=VR0-REFERENCE-CONTRACT-PASS',
    'documentation-reference-only=True',
    'plan-amendment3-validated=True',
    'vr1-reference-ready=True',
    'vr2-reference-ready=True',
    'vr3-reference-ready=True',
    'vr4-reference-ready=True',
    'vr4-current-exact-v9-applicability=INACTIVE-NO-CANONICAL-PRODUCTION-CONFIG',
    'production-src-changed=False',
    'pre-existing-tests-changed=False',
    'exact-v9-changed=False',
    'p3r1-authorized=False',
    'production-repair-authorized=False',
    'second-replacement-long-authorized=False',
    'next-authorized-gate=VR1-Point-Kinetics-Independent-Benchmark'
) | Set-Content -LiteralPath $summaryOut -Encoding UTF8

$sourceOut = Join-Path $artifactDir '02-vr0-source-contract.txt'
@(
    'VR1=Hebert-Applied-Reactor-Physics-3e-2020-section-5.4.1-exercises-5.10-5.11',
    'VR2=IAPWS-R7-97-2012',
    'VR3=Lamarsh-Baratta-Introduction-to-Nuclear-Engineering-3e-section-7.5',
    'VR4=ANSI-ANS-5.1-2014-R2023-scope-plus-Todreas-Kazimi-Vol1-3.9',
    'reference-implementation=CSharp-test-only',
    'python-project-tooling=False',
    'production-generates-reference=False'
) | Set-Content -LiteralPath $sourceOut -Encoding UTF8

Write-Host 'M10 Final VR0 Reference / Provenance Contract Freeze audit: PASS'
Write-Host ("Artifact: {0}" -f $artifactDir)
