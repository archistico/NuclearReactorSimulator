using System.Globalization;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Fast implementation gate for the opt-in reference-consistent conserved-inventory seed seam.
/// It validates the frozen raw vector through the production mode-2 closure, constructs the new opt-in factory
/// (including its canonical two seed-preconditioning steps), and runs the first 100 Running steps.
/// R3 remains RED until the later 120 s Requalification 3.
/// </summary>
public sealed class M10FinalVr2R3ReferenceConsistentSeedIntegrationImplementation1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1";
    private const string ContractRelativePath = "eng/m10-final-vr2-r3-reference-consistent-seed-integration-implementation1-contract.json";
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private const int FastRunningSteps = 100;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3ReferenceConsistentSeedIntegrationImplementation1")]
    public void OptInReferenceConsistentSeed_PreservesRawTargetsAndFirstHundredStepExactV9Health()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var contract = LoadContract();
        var rawRows = ResolveRawCandidate(contract);
        WriteRawRoundtrip(rawRows);

        Assert.Equal(12, rawRows.Count);
        Assert.Equal(12, rawRows.Count(static row => row.PhaseMatch));
        Assert.Equal(0, rawRows.Count(static row => !row.Finite));

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory
                .CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep));
        Assert.Equal(RuntimeStep, engine.FixedDeltaTime);

        var postSeedNodes = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.FluidNodes;
        Assert.Equal(12, postSeedNodes.Count);
        Assert.All(postSeedNodes, static node =>
        {
            Assert.True(double.IsFinite(node.Mass.Kilograms));
            Assert.True(double.IsFinite(node.InternalEnergy.Joules));
            Assert.True(double.IsFinite(node.Pressure.Pascals));
            Assert.True(double.IsFinite(node.Temperature.Kelvins));
        });

        var telemetry = new DesktopHydraulicProductionTelemetryProbe();
        var rows = new List<HealthRow>(FastRunningSteps);
        for (var step = 1; step <= FastRunningSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            telemetry.Observe(engine);

            var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var speed = engine.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary
                .ControlAndActuator.Controllers.GetDiagnostic("speed-control");

            var electrical = generator.ElectricalOutput.NumericValue ?? double.NaN;
            var primaryPump = fullPlant.IntegratedCycle.PrimaryCircuit.MainCirculation
                .TotalPumpMassFlowRate.KilogramsPerSecond;
            var drumLevel = drum.LiquidLevelFraction.Fraction;
            var governor = speed.Output;
            var finite = double.IsFinite(electrical)
                && double.IsFinite(primaryPump)
                && double.IsFinite(drumLevel)
                && double.IsFinite(governor);
            var inEnvelope = finite
                && electrical >= 4.99d && electrical <= 5.01d
                && primaryPump >= 99.9d && primaryPump <= 100.1d
                && drumLevel >= 0.49d && drumLevel <= 0.51d
                && governor >= 29.27d && governor <= 29.30d
                && !snapshot.AnyTripActive
                && generator.BreakerClosed;

            var telemetrySnapshot = telemetry.Snapshot();
            rows.Add(new HealthRow(
                step,
                electrical,
                primaryPump,
                drumLevel,
                governor,
                snapshot.AnyTripActive,
                generator.BreakerClosed,
                telemetrySnapshot.RollbackSteps,
                finite,
                inEnvelope));
        }

        WriteFastHealth(rows);

        Assert.Equal(FastRunningSteps, rows.Count);
        Assert.Equal(0, rows.Count(static row => !row.Finite));
        Assert.Equal(0, rows.Count(static row => !row.InEnvelope));
        Assert.Equal(0, rows.Count(static row => row.TripActive));
        Assert.Equal(0, rows.Count(static row => !row.BreakerClosed));
        Assert.Equal(0L, telemetry.Snapshot().RollbackSteps);
    }

    private static IReadOnlyList<RawRoundtripRow> ResolveRawCandidate(ImplementationContract contract)
    {
        var model = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var rows = new List<RawRoundtripRow>(contract.Nodes.Count);

        foreach (var node in contract.Nodes.OrderBy(static node => node.NodeId, StringComparer.Ordinal))
        {
            var definition = new FluidNodeDefinition(
                node.NodeId,
                Volume.FromCubicMetres(node.NodeVolumeCubicMetres));
            var inventory = new FluidNodeInventory(
                Mass.FromKilograms(node.MassKilograms),
                Energy.FromJoules(node.InternalEnergyJoules));
            var phase = Enum.Parse<FluidPhase>(node.TargetPhase, ignoreCase: false);
            var quality = node.TargetQuality.HasValue
                ? VaporQuality.FromFraction(node.TargetQuality.Value)
                : (VaporQuality?)null;
            var previous = new FluidThermodynamicState(
                Pressure.FromPascals(node.TargetPressurePascals),
                Temperature.FromKelvins(node.TargetTemperatureKelvins),
                phase,
                quality);
            var resolved = model.Resolve(definition, inventory, previous);

            rows.Add(new RawRoundtripRow(
                node.NodeId,
                node.MassKilograms,
                node.InternalEnergyJoules,
                node.TargetPressurePascals,
                resolved.Pressure.Pascals,
                resolved.Pressure.Pascals - node.TargetPressurePascals,
                node.TargetTemperatureKelvins,
                resolved.Temperature.Kelvins,
                resolved.Temperature.Kelvins - node.TargetTemperatureKelvins,
                node.TargetPhase,
                resolved.Phase.ToString(),
                string.Equals(node.TargetPhase, resolved.Phase.ToString(), StringComparison.Ordinal),
                node.TargetQuality,
                resolved.VaporQuality?.Fraction,
                double.IsFinite(resolved.Pressure.Pascals)
                    && double.IsFinite(resolved.Temperature.Kelvins)));
        }

        return rows;
    }

    private static ImplementationContract LoadContract()
    {
        var root = FindRepositoryRoot();
        using var doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, ContractRelativePath.Replace('/', Path.DirectorySeparatorChar))));
        var c = doc.RootElement;
        var targets = c.GetProperty("frozen_candidate_vector").GetProperty("nodes");
        var targetStates = c.GetProperty("target_state_vector").GetProperty("nodes")
            .EnumerateArray()
            .ToDictionary(
                static n => n.GetProperty("node").GetString()!,
                static n => n,
                StringComparer.Ordinal);
        var volumes = c.GetProperty("candidate_node_volumes_cubic_metres");

        var nodes = targets.EnumerateArray().Select(n =>
        {
            var id = n.GetProperty("node").GetString()!;
            var target = targetStates[id];
            return new NodeContract(
                id,
                volumes.GetProperty(id).GetDouble(),
                n.GetProperty("mass_kg").GetDouble(),
                n.GetProperty("internal_energy_j").GetDouble(),
                target.GetProperty("target_pressure_pa").GetDouble(),
                target.GetProperty("target_temperature_k").GetDouble(),
                target.GetProperty("target_phase").GetString()!,
                target.GetProperty("target_quality").ValueKind == JsonValueKind.Null
                    ? null
                    : target.GetProperty("target_quality").GetDouble());
        }).ToArray();

        return new ImplementationContract(nodes);
    }

    private static void WriteRawRoundtrip(IReadOnlyCollection<RawRoundtripRow> rows)
    {
        var lines = new List<string>
        {
            "node,mass_kg,internal_energy_j,target_pressure_pa,resolved_pressure_pa,pressure_residual_pa,target_temperature_k,resolved_temperature_k,temperature_residual_k,target_phase,resolved_phase,phase_match,target_quality,resolved_quality,finite"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            row.NodeId,
            F(row.MassKilograms),
            F(row.InternalEnergyJoules),
            F(row.TargetPressurePascals),
            F(row.ResolvedPressurePascals),
            F(row.PressureResidualPascals),
            F(row.TargetTemperatureKelvins),
            F(row.ResolvedTemperatureKelvins),
            F(row.TemperatureResidualKelvins),
            row.TargetPhase,
            row.ResolvedPhase,
            row.PhaseMatch ? "true" : "false",
            Optional(row.TargetQuality),
            Optional(row.ResolvedQuality),
            row.Finite ? "true" : "false")));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "03-raw-seed-roundtrip.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteFastHealth(IReadOnlyCollection<HealthRow> rows)
    {
        var lines = new List<string>
        {
            "step,electrical_mwe,primary_pump_kg_s,drum_level,governor_percent,trip_active,breaker_closed,rollbacks,finite,in_envelope"
        };
        lines.AddRange(rows.Select(static row => string.Join(",",
            row.Step.ToString(CultureInfo.InvariantCulture),
            F(row.ElectricalMegawatts),
            F(row.PrimaryPumpKilogramsPerSecond),
            F(row.DrumLevelFraction),
            F(row.GovernorPercent),
            row.TripActive ? "true" : "false",
            row.BreakerClosed ? "true" : "false",
            row.Rollbacks.ToString(CultureInfo.InvariantCulture),
            row.Finite ? "true" : "false",
            row.InEnvelope ? "true" : "false")));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "04-fast-100-step-health.csv"), lines, Utf8WithoutBom);
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Optional(double? value) => value.HasValue ? F(value.Value) : "null";

    private static void RequireOptIn()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled Seed Integration Implementation 1 runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-implementation1");

    private static void ResetArtifactDirectory()
    {
        var path = ArtifactDirectory();
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        Directory.CreateDirectory(path);
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

    private sealed record ImplementationContract(IReadOnlyList<NodeContract> Nodes);

    private sealed record NodeContract(
        string NodeId,
        double NodeVolumeCubicMetres,
        double MassKilograms,
        double InternalEnergyJoules,
        double TargetPressurePascals,
        double TargetTemperatureKelvins,
        string TargetPhase,
        double? TargetQuality);

    private sealed record RawRoundtripRow(
        string NodeId,
        double MassKilograms,
        double InternalEnergyJoules,
        double TargetPressurePascals,
        double ResolvedPressurePascals,
        double PressureResidualPascals,
        double TargetTemperatureKelvins,
        double ResolvedTemperatureKelvins,
        double TemperatureResidualKelvins,
        string TargetPhase,
        string ResolvedPhase,
        bool PhaseMatch,
        double? TargetQuality,
        double? ResolvedQuality,
        bool Finite);

    private sealed record HealthRow(
        int Step,
        double ElectricalMegawatts,
        double PrimaryPumpKilogramsPerSecond,
        double DrumLevelFraction,
        double GovernorPercent,
        bool TripActive,
        bool BreakerClosed,
        long Rollbacks,
        bool Finite,
        bool InEnvelope);
}
