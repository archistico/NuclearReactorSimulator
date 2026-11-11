using System.Security.Cryptography;
using System.Text.Json;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class M10R3StageEnergyLawReconstructionTests
{
    private const string CurrentProtocol = "r3-stage-energy-law-reconstruction2";
    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string Protocol(string? value)
    {
        Assert.NotNull(value);
        Assert.Matches("^r3-stage-energy-law-reconstruction[0-9]+$", value);
        return value;
    }

    private static object Providers(SimplifiedWaterSteamThermodynamicModel model, FluidEnergyTransportMode mode, Pressure pressure, Temperature temperature)
    {
        var historical = model.GetSaturationProperties(pressure);
        var active = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(pressure, temperature);
        return new
        {
            PressurePa = pressure.Pascals,
            TemperatureK = temperature.Kelvins,
            HistoricalSaturationTemperatureK = historical.Temperature.Kelvins,
            Historical = new
            {
                LiquidInternalJKg = historical.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
                VaporInternalJKg = historical.SaturatedVaporInternalEnergy.JoulesPerKilogram,
                LiquidDensityKgM3 = historical.SaturatedLiquidDensity.KilogramsPerCubicMetre,
                VaporDensityKgM3 = historical.SaturatedVaporDensity.KilogramsPerCubicMetre,
                LiquidSelectedJKg = FluidEnergyTransport.ResolveSelectedSpecificEnergy(mode, historical.SaturatedLiquidInternalEnergy, pressure, historical.SaturatedLiquidDensity).JoulesPerKilogram,
                VaporSelectedJKg = FluidEnergyTransport.ResolveSelectedSpecificEnergy(mode, historical.SaturatedVaporInternalEnergy, pressure, historical.SaturatedVaporDensity).JoulesPerKilogram,
            },
            ActiveClosure = new
            {
                LiquidInternalJKg = active.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
                VaporInternalJKg = active.SaturatedVaporInternalEnergy.JoulesPerKilogram,
                LiquidDensityKgM3 = active.SaturatedLiquidDensity.KilogramsPerCubicMetre,
                VaporDensityKgM3 = active.SaturatedVaporDensity.KilogramsPerCubicMetre,
                LiquidSelectedJKg = FluidEnergyTransport.ResolveSelectedSpecificEnergy(mode, active.SaturatedLiquidInternalEnergy, pressure, active.SaturatedLiquidDensity).JoulesPerKilogram,
                VaporSelectedJKg = FluidEnergyTransport.ResolveSelectedSpecificEnergy(mode, active.SaturatedVaporInternalEnergy, pressure, active.SaturatedVaporDensity).JoulesPerKilogram,
            },
        };
    }

    [Fact]
    public void Preexecution_HistoricalClosureTransportEqualsForwardSaturation()
    {
        var pressure = Pressure.FromPascals(3_800_000d);
        var model = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology);
        var saturation = model.GetSaturationProperties(pressure);
        var transport = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(pressure, saturation.Temperature);
        Assert.Equal(saturation.SaturatedLiquidInternalEnergy, transport.SaturatedLiquidInternalEnergy);
        Assert.Equal(saturation.SaturatedVaporInternalEnergy, transport.SaturatedVaporInternalEnergy);
        Assert.Equal(saturation.SaturatedLiquidDensity, transport.SaturatedLiquidDensity);
        Assert.Equal(saturation.SaturatedVaporDensity, transport.SaturatedVaporDensity);
    }

    [Fact]
    public void Preexecution_ReconstructionContractNamesThisExecutor()
    {
        Assert.ThrowsAny<Exception>(() => Protocol("../outside"));
        Assert.ThrowsAny<Exception>(() => Protocol("r3-op-control-valve-sensitivity1"));
        var contract = Path.Combine(R3HFeasibilityHarness.Root, "eng", Protocol(CurrentProtocol) + "-contract.json");
        using var document = JsonDocument.Parse(File.ReadAllText(contract));
        Assert.Contains(document.RootElement.GetProperty("implementation_files").EnumerateArray(),
            static f => f.GetProperty("path").GetString()!.EndsWith(nameof(M10R3StageEnergyLawReconstructionTests) + ".cs", StringComparison.Ordinal));
    }

    [Fact(Explicit = true)]
    public void ExecuteReviewedReconstruction()
    {
        var protocol = Protocol(Environment.GetEnvironmentVariable("NRS_R3_STAGE_PROTOCOL"));
        var output = Path.Combine(R3HFeasibilityHarness.Root, "artifacts", protocol);
        var contractPath = Path.Combine(R3HFeasibilityHarness.Root, "eng", protocol + "-contract.json");
        using var contractDocument = JsonDocument.Parse(File.ReadAllText(contractPath));
        var contract = contractDocument.RootElement;
        Assert.Equal(protocol + "-v1", contract.GetProperty("schema").GetString());
        Assert.Equal("artifacts/" + protocol, contract.GetProperty("output").GetString());
        foreach (var file in contract.GetProperty("implementation_files").EnumerateArray())
            Assert.Equal(file.GetProperty("sha256").GetString(), Sha(Path.Combine(R3HFeasibilityHarness.Root, file.GetProperty("path").GetString()!)));
        using var tokenDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "02-preexecution.json")));
        Assert.Equal(Sha(contractPath), tokenDocument.RootElement.GetProperty("contract_sha256").GetString());
        Assert.Equal(Sha(typeof(M10R3StageEnergyLawReconstructionTests).Assembly.Location), tokenDocument.RootElement.GetProperty("assembly_sha256").GetString());

        var evaluation = new R3HFeasibilityHarness().Evaluate(new double[24]);
        Assert.All(evaluation.Observations, static o => Assert.True(o.Healthy));
        var full = evaluation.Engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var cycle = full.IntegratedCycle;
        var model = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var train = Assert.Single(cycle.TurbineExpansion.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
        var condenser = Assert.Single(cycle.Condenser.Condensers);
        // Both solvers read the committed state at the start of the step; their snapshots record that state.
        var record = new
        {
            Protocol = protocol,
            Checkpoint = "1",
            Residuals = evaluation.Residuals,
            StateSha256 = R3HFeasibilityHarness.Hash(evaluation.Observations[^1].StateJson),
            Stage = new
            {
                stage.StageGroupId, stage.InletNodeId, stage.ExhaustNodeId, stage.MoistureDrainNodeId,
                EnergyTransportMode = stage.EnergyTransportMode.ToString(),
                InletPressurePa = stage.InletPressure.Pascals, InletTemperatureK = stage.InletTemperature.Kelvins,
                InletPhase = stage.InletPhase.ToString(), InletQuality = stage.InletVaporQuality?.Fraction,
                InletSpecificInternalJKg = stage.InletSpecificInternalEnergy.JoulesPerKilogram,
                InletAdvectedJKg = stage.InletAdvectedSpecificEnergy.JoulesPerKilogram,
                VaporAdvectedJKg = stage.VaporInletAdvectedSpecificEnergy.JoulesPerKilogram,
                DrainAdvectedJKg = stage.MoistureDrainAdvectedSpecificEnergy.JoulesPerKilogram,
                VaporMassKgS = stage.EffectiveMassFlowRate.KilogramsPerSecond, DrainMassKgS = stage.MoistureDrainMassFlowRate.KilogramsPerSecond,
                InletEnergyW = stage.InletEnergyFlowRate.Watts, DrainEnergyW = stage.MoistureDrainEnergyFlowRate.Watts,
                Providers = Providers(model, stage.EnergyTransportMode, stage.InletPressure, stage.InletTemperature),
            },
            Admission = new
            {
                train.TrainId, train.TurbineInletNodeId,
                MassKgS = train.AdmissionValve.MassFlowRate.KilogramsPerSecond, EnergyW = train.AdmissionValve.AdvectedEnergyFlowRate.Watts,
            },
            Condenser = new
            {
                condenser.CondenserId, condenser.SteamSpaceNodeId, condenser.HotwellNodeId,
                EnergyTransportMode = condenser.EnergyTransportMode.ToString(),
                CondensationKgS = condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
                SteamAdvectedJKg = condenser.SteamAdvectedSpecificEnergy.JoulesPerKilogram,
                CondensateAdvectedJKg = condenser.CondensateAdvectedSpecificEnergy.JoulesPerKilogram,
                HeatRejectionW = condenser.HeatRejectionPower.Watts,
                Providers = Providers(model, condenser.EnergyTransportMode, condenser.InitialSteamSpacePressure, condenser.InitialSteamSpaceTemperature),
            },
        };
        File.WriteAllText(Path.Combine(output, "03-reconstruction.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
    }
}
