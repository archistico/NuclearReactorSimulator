using System.Security.Cryptography;
using System.Text.Json;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class M10R3StageTransportOwnershipRepairTests
{
    private const string Protocol = "r3-stage-transport-ownership-repair1";
    private static string Contract => Path.Combine(R3HFeasibilityHarness.Root, "eng", Protocol + "-contract.json");
    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void Preexecution_ContractBindsProductionScopeAndMeasurementCriteria()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Contract));
        var contract = document.RootElement;
        Assert.Equal(Protocol + "-v1", contract.GetProperty("schema").GetString());
        Assert.Equal(4, contract.GetProperty("production_files").GetArrayLength());
        Assert.Equal(2, contract.GetProperty("policy").GetProperty("repeats").GetInt32());
        Assert.Equal(.1d, contract.GetProperty("policy").GetProperty("maximum_inlet_ratio").GetDouble());
        Assert.Contains(contract.GetProperty("implementation_files").EnumerateArray(),
            static entry => entry.GetProperty("path").GetString()!.EndsWith(nameof(M10R3StageTransportOwnershipRepairTests) + ".cs", StringComparison.Ordinal));
    }

    [Fact(Explicit = true)]
    public void ExecuteReviewedMeasurement()
    {
        var output = Path.Combine(R3HFeasibilityHarness.Root, "artifacts", Protocol);
        using var contractDocument = JsonDocument.Parse(File.ReadAllText(Contract));
        foreach (var entry in contractDocument.RootElement.GetProperty("implementation_files").EnumerateArray())
        {
            Assert.Equal(entry.GetProperty("sha256").GetString(), Sha(Path.Combine(R3HFeasibilityHarness.Root, entry.GetProperty("path").GetString()!)));
        }
        using var preexecution = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "02-preexecution.json")));
        Assert.Equal(Sha(Contract), preexecution.RootElement.GetProperty("contract_sha256").GetString());
        Assert.Equal(Sha(typeof(M10R3StageTransportOwnershipRepairTests).Assembly.Location), preexecution.RootElement.GetProperty("assembly_sha256").GetString());
        var harness = new R3HFeasibilityHarness();
        var records = new List<object>();
        for (var repeat = 1; repeat <= 2; repeat++)
        {
            var evaluation = harness.Evaluate(new double[24]);
            var cycle = evaluation.Engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
            var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
            var train = Assert.Single(cycle.TurbineExpansion.MainSteamNetwork.AdmissionTrains);
            var condenser = Assert.Single(cycle.Condenser.Condensers);
            var model = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
            var stageProperties = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(stage.InletPressure);
            var condenserProperties = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(condenser.InitialSteamSpacePressure);
            var expectedVapor = FluidEnergyTransport.ResolveSelectedSpecificEnergy(stage.EnergyTransportMode,
                stageProperties.SaturatedVaporInternalEnergy, stage.InletPressure, stageProperties.SaturatedVaporDensity).JoulesPerKilogram;
            var expectedDrain = FluidEnergyTransport.ResolveSelectedSpecificEnergy(stage.EnergyTransportMode,
                stageProperties.SaturatedLiquidInternalEnergy, stage.InletPressure, stageProperties.SaturatedLiquidDensity).JoulesPerKilogram;
            var expectedCondensate = FluidEnergyTransport.ResolveSelectedSpecificEnergy(condenser.EnergyTransportMode,
                condenserProperties.SaturatedLiquidInternalEnergy, condenser.InitialSteamSpacePressure, condenserProperties.SaturatedLiquidDensity).JoulesPerKilogram;
            records.Add(new
            {
                Id = $"base/repeat/{repeat}", Coordinates = new double[24],
                Reason = evaluation.Observations.All(o => o.Healthy) ? "VALID" : "HEALTH-SENTINEL",
                evaluation.Raw, evaluation.Residuals,
                Phases = evaluation.Raw.Select(n => n.Phase).Concat(evaluation.Observations.SelectMany(o => o.Nodes.Select(n => n.Phase))).ToArray(),
                Observations = evaluation.Observations.Select(o => new
                {
                    o.Checkpoint, o.Nodes, o.ScaledResiduals, o.Healthy, o.InEnvelope,
                    o.ElectricalMwe, o.PumpKgS, o.DrumLevel, o.Governor, o.RotorTorque,
                    o.MassClosure, o.EnergyClosure, StateSha256 = R3HFeasibilityHarness.Hash(o.StateJson)
                }).ToArray(),
                Stage = new
                {
                    VaporAdvectedJKg = stage.VaporInletAdvectedSpecificEnergy.JoulesPerKilogram,
                    DrainAdvectedJKg = stage.MoistureDrainAdvectedSpecificEnergy.JoulesPerKilogram,
                    ExpectedVaporJKg = expectedVapor, ExpectedDrainJKg = expectedDrain,
                    VaporMassKgS = stage.EffectiveMassFlowRate.KilogramsPerSecond,
                    DrainMassKgS = stage.MoistureDrainMassFlowRate.KilogramsPerSecond,
                    InletEnergyW = stage.InletEnergyFlowRate.Watts, ExhaustEnergyW = stage.ExhaustEnergyFlowRate.Watts,
                    DrainEnergyW = stage.MoistureDrainEnergyFlowRate.Watts, ShaftPowerW = stage.ShaftPower.Watts,
                    OwnershipResidualW = stage.TurbineEnergyOwnershipResidual.Watts,
                    AdmissionEnergyW = train.AdmissionValve.AdvectedEnergyFlowRate.Watts,
                    AdmissionMassKgS = train.AdmissionValve.MassFlowRate.KilogramsPerSecond
                },
                Condenser = new
                {
                    CondensateAdvectedJKg = condenser.CondensateAdvectedSpecificEnergy.JoulesPerKilogram,
                    ExpectedCondensateJKg = expectedCondensate,
                    SteamAdvectedJKg = condenser.SteamAdvectedSpecificEnergy.JoulesPerKilogram,
                    MassKgS = condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
                    ThermalLimitKgS = condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond,
                    InventoryLimitKgS = condenser.InventoryLimitedCondensationMassFlowRate.KilogramsPerSecond,
                    MaximumLimitKgS = condenser.MaximumCondensationMassFlowRate.KilogramsPerSecond,
                    CoolingCapacityW = condenser.EffectiveHeatRejectionCapacity.Watts,
                    HeatRejectionW = condenser.HeatRejectionPower.Watts,
                    condenser.ActiveCondensationLimits
                }
            });
        }
        File.WriteAllLines(Path.Combine(output, "03-measurements.jsonl"), records.Select(record => JsonSerializer.Serialize(record)));
    }

    [Fact]
    public void CurrentRawC4Recipe_UsesActiveTransportAndRemovesStructuralInletAccumulation()
    {
        var evaluation = new R3HFeasibilityHarness().Evaluate(new double[24]);
        Assert.All(evaluation.Observations, static observation => Assert.True(observation.Healthy));
        var previous = evaluation.Raw;
        foreach (var observation in evaluation.Observations)
        {
            var before = Assert.Single(previous, static node => node.Id == "turbine-inlet");
            var after = Assert.Single(observation.Nodes, static node => node.Id == "turbine-inlet");
            Assert.InRange(Math.Abs((after.Energy - before.Energy) / .01d), 0d, .1d * 1_006_560.2752685547d);
            previous = observation.Nodes;
        }
        var cycle = evaluation.Engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
        var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
        var condenser = Assert.Single(cycle.Condenser.Condensers);
        var provider = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain).PhaseTransportPropertyProvider;
        var inlet = provider.GetSaturatedPhaseTransportProperties(stage.InletPressure);
        var expectedVapor = FluidEnergyTransport.ResolveSelectedSpecificEnergy(stage.EnergyTransportMode,
            inlet.SaturatedVaporInternalEnergy, stage.InletPressure, inlet.SaturatedVaporDensity);
        var expectedDrain = FluidEnergyTransport.ResolveSelectedSpecificEnergy(stage.EnergyTransportMode,
            inlet.SaturatedLiquidInternalEnergy, stage.InletPressure, inlet.SaturatedLiquidDensity);
        Assert.Equal(expectedVapor, stage.VaporInletAdvectedSpecificEnergy);
        Assert.Equal(expectedDrain, stage.MoistureDrainAdvectedSpecificEnergy);
        var liquid = provider.GetSaturatedPhaseTransportProperties(condenser.InitialSteamSpacePressure);
        Assert.Equal(FluidEnergyTransport.ResolveSelectedSpecificEnergy(condenser.EnergyTransportMode,
            liquid.SaturatedLiquidInternalEnergy, condenser.InitialSteamSpacePressure, liquid.SaturatedLiquidDensity), condenser.CondensateAdvectedSpecificEnergy);
    }
}
