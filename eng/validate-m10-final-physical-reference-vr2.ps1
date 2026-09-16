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
Write-Host 'M10 FINAL - VR2 IAPWS-IF97 WATER/STEAM ERROR MAP'
Write-Host '============================================================'
Write-Host 'Static contract audit before explicit thermodynamic assessment.'
Write-Host 'No production repair, exact-v9 change, P3-R1 or second-long authorization.'
Write-Host ''

$vr1Summary = 'eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR1_UserReportedValidatedSummary.txt'
Require-Text $vr1Summary 'status=VALIDATED'
Require-Text $vr1Summary 'result=PASS'
Require-Text $vr1Summary 'next-authorized-gate=VR2-IAPWS-IF97-Water-Steam-Error-Map'

Require-Text 'docs/PROJECT.md' 'VR1 POINT-KINETICS INDEPENDENT BENCHMARK** is now VALIDATED'
Require-Text 'docs/PROJECT.md' 'VR2 IAPWS-IF97 WATER/STEAM ERROR MAP'
Require-Text 'docs/M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md' 'VR2 is now the active execution candidate'
Require-Text 'docs/M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md' 'IAPWS R7-97(2012)'
Require-Text 'docs/M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md' 'CorrelationConsistentInverseDomain'
Require-Text 'docs/M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md' 'maximum relative error <= 1e-8'
Require-Text 'docs/M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md' 'MODEL-DISCREPANCY-BLOCKING'
Require-Text 'docs/M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md' 'VR3-I135-Xe135-Shutdown-Reference-Trajectory'

$helperPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/IapwsIf97Reference.cs'
Require-Text $helperPath 'internal static class IapwsIf97Reference'
Require-Text $helperPath 'Region1(double temperatureKelvins, double pressureMegapascals)'
Require-Text $helperPath 'Region2(double temperatureKelvins, double pressureMegapascals)'
Require-Text $helperPath 'SaturationPressureMegapascals(double temperatureKelvins)'
Require-Text $helperPath 'SaturationTemperatureKelvins(double pressureMegapascals)'
Require-Text $helperPath 'SimplifiedWaterSteamThermodynamicModel and is never referenced by production code.'

$testPath = 'tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalPhysicalReferenceVr2WaterSteamBenchmarkTests.cs'
Require-Text $testPath 'Fact(Explicit = true)'
Require-Text $testPath 'IapwsIf97Reference'
Require-Text $testPath 'WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain'
Require-Text $testPath 'ReferenceSelfCheckMaximumRelativeError = 1e-8d'
Require-Text $testPath 'QuantitativeEducationalMaximumRelativeError = 0.02d'
Require-Text $testPath 'BoundedEducationalMaximumRelativeError = 0.10d'
Require-Text $testPath 'QualitativeOnlyMaximumRelativeError = 0.25d'
Require-Text $testPath 'VR2-PASS-NONBLOCKING'
Require-Text $testPath 'MODEL-DISCREPANCY-BLOCKING'
Require-Text $testPath 'deterministicRepeat'
Require-Text $testPath '09-deterministic-repeat.txt'

$contractPath = 'eng/m10-final-physical-reference-vr2-contract.json'
Require-File $contractPath
$contract = Get-Content -LiteralPath $contractPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($contract.schema -ne 'm10-final-physical-reference-vr2-v1') { throw 'Unexpected VR2 contract schema.' }
if ($contract.status -ne 'CANDIDATE') { throw 'VR2 contract must remain CANDIDATE until returned artifacts are reviewed.' }
if ($contract.production_src_change_authorized -ne $false) { throw 'VR2 cannot authorize production source changes.' }
if ($contract.production_repair_authorized -ne $false) { throw 'VR2 cannot authorize production repair.' }
if ($contract.exact_v9_change_authorized -ne $false) { throw 'VR2 cannot authorize exact-v9 changes.' }
if ($contract.p3_r1_execution_authorized -ne $false) { throw 'VR2 cannot execute P3-R1.' }
if ($contract.second_replacement_long_authorized -ne $false) { throw 'VR2 cannot authorize a second replacement long.' }
if ($contract.reference.calls_production_model -ne $false) { throw 'VR2 reference path cannot call the production model.' }
if ($contract.reference.third_party_runtime_library -ne $false) { throw 'VR2 cannot add a third-party IF97 runtime dependency.' }
if ($contract.reference.selfcheck_maximum_relative_error -ne 1e-8) { throw 'VR2 reference self-check ceiling drifted.' }
if ($contract.reference.regions.Count -ne 3) { throw 'VR2 reference must be limited to Regions 1, 2 and 4.' }
if (($contract.reference.regions -join ',') -ne '1,2,4') { throw 'VR2 reference region list drifted.' }
if ($contract.matrix.saturation_full_property_temperatures_celsius.Count -ne 10) { throw 'VR2 must use ten full-property saturation points.' }
if (($contract.matrix.saturation_full_property_temperatures_celsius -join ',') -ne '100,125,150,175,200,225,250,280,300,340') { throw 'VR2 full-property saturation matrix drifted.' }
if (($contract.matrix.saturation_pressure_only_temperatures_celsius -join ',') -ne '360') { throw 'VR2 pressure-only saturation matrix drifted.' }
if ($contract.matrix.saturation_pressure_only_temperatures_celsius.Count -ne 1) { throw 'VR2 must use one pressure-only saturation point.' }
if ($contract.matrix.compressed_liquid.Count -ne 5) { throw 'VR2 must use five compressed-liquid inverse points.' }
if ($contract.matrix.superheated_vapor.Count -ne 4) { throw 'VR2 must use four superheated-vapor inverse points.' }
$compressed = $contract.matrix.compressed_liquid
if ($compressed[0].temperature_celsius -ne 100 -or $compressed[0].pressure_megapascals -ne 0.5) { throw 'VR2 compressed-liquid point 1 drifted.' }
if ($compressed[1].temperature_celsius -ne 150 -or $compressed[1].pressure_megapascals -ne 1.0) { throw 'VR2 compressed-liquid point 2 drifted.' }
if ($compressed[2].temperature_celsius -ne 200 -or $compressed[2].pressure_megapascals -ne 2.0) { throw 'VR2 compressed-liquid point 3 drifted.' }
if ($compressed[3].temperature_celsius -ne 250 -or $compressed[3].pressure_megapascals -ne 7.0) { throw 'VR2 compressed-liquid point 4 drifted.' }
if ($compressed[4].temperature_celsius -ne 280 -or $compressed[4].pressure_megapascals -ne 10.0) { throw 'VR2 compressed-liquid point 5 drifted.' }
$vapor = $contract.matrix.superheated_vapor
if ($vapor[0].temperature_celsius -ne 200 -or $vapor[0].pressure_megapascals -ne 0.2) { throw 'VR2 superheated-vapor point 1 drifted.' }
if ($vapor[1].temperature_celsius -ne 250 -or $vapor[1].pressure_megapascals -ne 0.5) { throw 'VR2 superheated-vapor point 2 drifted.' }
if ($vapor[2].temperature_celsius -ne 300 -or $vapor[2].pressure_megapascals -ne 1.0) { throw 'VR2 superheated-vapor point 3 drifted.' }
if ($vapor[3].temperature_celsius -ne 400 -or $vapor[3].pressure_megapascals -ne 5.0) { throw 'VR2 superheated-vapor point 4 drifted.' }
if ($contract.m10_core.minimum_temperature_celsius -ne 200) { throw 'VR2 M10-core lower temperature drifted.' }
if ($contract.m10_core.maximum_temperature_celsius -ne 300) { throw 'VR2 M10-core upper temperature drifted.' }
if ($contract.claim_bands.quantitative_educational_maximum_relative_error -ne 0.02) { throw 'VR2 quantitative claim band drifted.' }
if ($contract.claim_bands.bounded_educational_maximum_relative_error -ne 0.10) { throw 'VR2 bounded claim band drifted.' }
if ($contract.claim_bands.qualitative_only_maximum_relative_error -ne 0.25) { throw 'VR2 qualitative-only claim band drifted.' }
if ($contract.artifacts.Count -ne 9) { throw 'VR2 must return nine evidence artifacts.' }
if ($contract.next_authorized_gate_on_pass -ne 'VR3-I135-Xe135-Shutdown-Reference-Trajectory') { throw 'Unexpected next gate after VR2 PASS.' }
if ($contract.production.closure_mode -ne 'CorrelationConsistentInverseDomain') { throw 'VR2 production closure mode drifted.' }

Write-Host 'M10 Final VR2 static contract audit: PASS'
