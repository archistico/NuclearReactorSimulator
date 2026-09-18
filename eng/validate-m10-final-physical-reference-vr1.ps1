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
Write-Host 'M10 FINAL - VR1 POINT-KINETICS INDEPENDENT BENCHMARK'
Write-Host '============================================================'
Write-Host 'Static contract audit before explicit model-assessment execution.'
Write-Host 'No production repair, exact-v9 change, P3-R1 or second-long authorization.'
Write-Host ''

$vr0Summary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR0_UserReportedValidatedSummary.txt'
Require-Text $vr0Summary 'vr0-reference-contract-passes=True'
Require-Text $vr0Summary 'next-authorized-gate=VR1-Point-Kinetics-Independent-Benchmark'

Require-Text 'docs/PROJECT.md' 'VR0 REFERENCE / PROVENANCE CONTRACT FREEZE'
Require-Text 'docs/PROJECT.md' 'is now VALIDATED from the user-reported local audit PASS'
Require-Text 'docs/PROJECT.md' 'VR1 POINT-KINETICS INDEPENDENT BENCHMARK'
Require-Text 'docs/M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md' 'REFERENCE-CONCORDANT'
Require-Text 'docs/M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md' 'BOUNDED-NUMERICAL-DISCREPANCY'
Require-Text 'docs/M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md' 'MODEL-DISCREPANCY'
Require-Text 'docs/M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md' 'Dormand-Prince 5(4)'
Require-Text 'docs/M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md' 'exact-v9 one-group parameter calibration'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'VR1 is now the active execution candidate'
Require-Text 'docs/M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md' 'One dollar is `rho = beta`'
Require-Text 'docs/research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md' 'Exercise 5.10'
Require-Text 'docs/research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md' 'solver implementation is M10-material'

$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Reactor/Neutronics/M10FinalPhysicalReferenceVr1PointKineticsBenchmarkTests.cs'
Require-Text $testPath 'Fact(Explicit = true)'
Require-Text $testPath 'IndependentReferenceIntegrator'
Require-Text $testPath 'DormandPrinceTrial'
Require-Text $testPath 'PointKineticsSolver'
Require-Text $testPath 'ReferenceMaximumStepSeconds = 1e-3d'
Require-Text $testPath 'OneGroupSelfCheckMaximumRelativeError = 1e-8d'
Require-Text $testPath 'ConcordantNeutronMaximumRelativeError = 0.0025d'
Require-Text $testPath 'BoundedNeutronMaximumRelativeError = 0.01d'

$contractPath = 'eng/m10-final-physical-reference-vr1-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($contract.production_src_change_authorized -ne $false) { throw 'VR1 cannot authorize production source changes.' }
if ($contract.production_repair_authorized -ne $false) { throw 'VR1 cannot authorize production repair.' }
if ($contract.exact_v9_change_authorized -ne $false) { throw 'VR1 cannot authorize exact-v9 changes.' }
if ($contract.p3_r1_execution_authorized -ne $false) { throw 'VR1 cannot execute P3-R1.' }
if ($contract.second_replacement_long_authorized -ne $false) { throw 'VR1 cannot authorize a second replacement long.' }
if ($contract.reference_calls_production_solver -ne $false) { throw 'VR1 reference path cannot call production solver.' }
if ($contract.production_generates_reference -ne $false) { throw 'Production cannot generate VR1 reference values.' }
if ($contract.reference_relative_tolerance -ne 1e-10) { throw 'VR1 reference relative tolerance drifted.' }
if ($contract.reference_absolute_tolerance -ne 1e-12) { throw 'VR1 reference absolute tolerance drifted.' }
if ($contract.reference_maximum_step_seconds -ne 0.001) { throw 'VR1 maximum reference step drifted.' }
if ($contract.one_group_selfcheck.maximum_relative_neutron_error -ne 1e-8) { throw 'VR1 self-check ceiling drifted.' }
if ($contract.six_group.beta_total -ne 0.007) { throw 'VR1 six-group beta total drifted.' }
if ($contract.six_group.groups.Count -ne 6) { throw 'VR1 must use six delayed-neutron groups.' }
if ($contract.six_group.cases.Count -ne 5) { throw 'VR1 must use five frozen reactivity cases.' }
if ($contract.production_caller_steps_seconds.Count -ne 3) { throw 'VR1 must use 10/5/2.5 ms production caller steps.' }
if ($contract.next_authorized_gate_on_pass -ne 'VR2-IAPWS-IF97-Water-Steam-Error-Map') { throw 'Unexpected next gate after VR1 PASS.' }

Write-Host 'M10 Final VR1 static contract audit: PASS'
