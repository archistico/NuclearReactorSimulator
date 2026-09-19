using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// Independent test-only IAPWS-IF97 counterfactual for Diagnostic 3 REV1.
/// The test consumes only the minimal runtime artifact emitted by Application.Tests.
/// It does not call production thermodynamic models and does not authorize a repair.
/// </summary>
public sealed class M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactualTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1_REFERENCE";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactual")]
    public void RawDrumPoint_IndependentIf97Reference_EmitsTransportCounterfactual()
    {
        RequireOptIn();

        var inputPath = Path.Combine(ArtifactDirectory(), "04-reference-counterfactual-input.csv");
        var row = ReadSingleRow(inputPath);

        var temperatureKelvins = D(row["drum_temperature_k"]);
        var pressurePascals = D(row["drum_pressure_pa"]);
        var pressureMegapascals = pressurePascals / 1_000_000d;
        var suctionTransport = D(row["mode2_raw_suction_transport_j_kg"]);
        var productionTransport = D(row["production_drum_liquid_transport_j_kg"]);
        var recirculationFlow = D(row["recirculation_flow_kg_s"]);
        var pumpFlow = D(row["pump_flow_kg_s"]);
        var actualCandidateObservedEnergyRate = D(row["actual_candidate_observed_energy_rate_w"]);

        var saturationPressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var saturationPressurePascals = saturationPressureMegapascals * 1_000_000d;
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressureMegapascals);

        var referenceFlowWork = pressurePascals * liquid.SpecificVolumeCubicMetresPerKilogram;
        var referenceTransport = liquid.SpecificInternalEnergyJoulesPerKilogram + referenceFlowWork;
        var counterfactualSourceRate = referenceTransport * recirculationFlow;
        var counterfactualSinkRate = suctionTransport * pumpFlow;
        var counterfactualNetRate = counterfactualSourceRate - counterfactualSinkRate;
        var valid = AllFinite(
                temperatureKelvins,
                pressurePascals,
                saturationPressurePascals,
                liquid.SpecificVolumeCubicMetresPerKilogram,
                liquid.DensityKilogramsPerCubicMetre,
                liquid.SpecificInternalEnergyJoulesPerKilogram,
                referenceFlowWork,
                referenceTransport,
                counterfactualSourceRate,
                counterfactualSinkRate,
                counterfactualNetRate)
            && temperatureKelvins > 0d
            && pressurePascals > 0d
            && liquid.SpecificVolumeCubicMetresPerKilogram > 0d
            && liquid.DensityKilogramsPerCubicMetre > 0d;

        var lines = new[]
        {
            "input_sha256,reference_calculation_valid,temperature_k,drum_pressure_pa,if97_saturation_pressure_pa,saturation_pressure_delta_pa,if97_region1_specific_volume_m3_kg,if97_region1_density_kg_m3,if97_region1_internal_energy_j_kg,if97_region1_specific_flow_work_j_kg,if97_region1_transport_j_kg,production_drum_transport_j_kg,if97_minus_production_transport_j_kg,mode2_raw_suction_transport_j_kg,if97_minus_mode2_suction_transport_j_kg,recirculation_flow_kg_s,pump_flow_kg_s,counterfactual_source_rate_w,counterfactual_sink_rate_w,counterfactual_net_energy_rate_w,actual_candidate_observed_energy_rate_w",
            string.Join(",",
                Sha256(inputPath),
                valid ? "true" : "false",
                F(temperatureKelvins),
                F(pressurePascals),
                F(saturationPressurePascals),
                F(pressurePascals - saturationPressurePascals),
                F(liquid.SpecificVolumeCubicMetresPerKilogram),
                F(liquid.DensityKilogramsPerCubicMetre),
                F(liquid.SpecificInternalEnergyJoulesPerKilogram),
                F(referenceFlowWork),
                F(referenceTransport),
                F(productionTransport),
                F(referenceTransport - productionTransport),
                F(suctionTransport),
                F(referenceTransport - suctionTransport),
                F(recirculationFlow),
                F(pumpFlow),
                F(counterfactualSourceRate),
                F(counterfactualSinkRate),
                F(counterfactualNetRate),
                F(actualCandidateObservedEnergyRate))
        };

        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "05-if97-reference-counterfactual.csv"),
            lines,
            Utf8WithoutBom);
    }

    private static IReadOnlyDictionary<string, string> ReadSingleRow(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.Equal(2, lines.Length);
        var headers = lines[0].Split(',');
        var values = lines[1].Split(',');
        Assert.Equal(headers.Length, values.Length);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < headers.Length; i++)
        {
            result.Add(headers[i], values[i]);
        }

        return result;
    }

    private static bool AllFinite(params double[] values)
        => values.All(double.IsFinite);

    private static string Sha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1");

    private static void RequireOptIn()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
            "1",
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled Diagnostic 3 REV1 runner.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln.");
    }

    private static double D(string value)
        => double.Parse(value, CultureInfo.InvariantCulture);

    private static string F(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);
}
