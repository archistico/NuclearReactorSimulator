using System.Globalization;
using System.Reflection;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Diagnostic only for returned R3 Requalification 2 RED.
/// Compares mode-1 and repaired mode-2 initial conserved inventories, thermodynamic closure mapping,
/// pre-step hydraulic pressure heads, and the first 100 running steps.
/// No production repair or acceptance-threshold change is performed here.
/// </summary>
public sealed class M10FinalVr2R3Requalification2InitialClosureOperatingPointDisplacementDiagnostic1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_R2_INITIAL_DISPLACEMENT_DIAGNOSTIC1";
    private const int DynamicSteps = 100;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3Requalification2InitialClosureOperatingPointDisplacementDiagnostic1")]
    public void ReturnedR3R2Red_IsLocalizedToInitialClosureMappingOrPostSeedDynamics()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var mode1 = CreateShadow(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var mode2 = CreateShadow(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);

        var mode1Initial = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant;
        var mode2Initial = mode2.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant;

        var initialRows = CompareInitialNodes(mode1Initial, mode2Initial);
        WriteInitialNodeComparison(initialRows);

        var headRows = CompareHydraulicHeads(mode1Initial, mode2Initial);
        WriteHydraulicHeadComparison(headRows);

        var dynamicRows = RunFirstSecond(mode1, mode2);
        WriteDynamicComparison(dynamicRows);

        var allInventoriesBitwiseEqual = initialRows.All(static row => row.InventoryBitwiseEqual);
        var factoryThermodynamicDifferenceCount = initialRows.Count(static row => !row.FactoryThermodynamicsBitwiseEqual);
        var sameInventoryDirectClosureDifferenceCount = initialRows.Count(static row => !row.DirectClosureBitwiseEqual);
        var maxFactoryPressureDelta = initialRows.Max(static row => Math.Abs(row.FactoryPressureDeltaPascals));
        var maxDirectPressureDelta = initialRows.Max(static row => Math.Abs(row.DirectPressureDeltaPascals));
        var maxHeadDelta = headRows.Max(static row => Math.Abs(row.HeadDeltaPascals));
        var mode1ViolationSteps = dynamicRows.Count(static row => !row.Mode1KeyEnvelope);
        var mode2ViolationSteps = dynamicRows.Count(static row => !row.Mode2KeyEnvelope);
        var firstStateFingerprintMismatch = dynamicRows.FirstOrDefault(static row => !row.SnapshotFingerprintMatch)?.Step ?? 0;

        var classification = !allInventoriesBitwiseEqual
            ? "SEED-INVENTORY-DIVERGENCE"
            : sameInventoryDirectClosureDifferenceCount > 0
                ? "IDENTICAL-INVENTORY-CLOSURE-MAPPING-DISPLACEMENT"
                : mode2ViolationSteps > mode1ViolationSteps
                    ? "POST-SEED-INTEGRATION-DIVERGENCE"
                    : "UNRESOLVED-DIAGNOSTIC1";

        WriteSummary(
            classification,
            allInventoriesBitwiseEqual,
            factoryThermodynamicDifferenceCount,
            sameInventoryDirectClosureDifferenceCount,
            maxFactoryPressureDelta,
            maxDirectPressureDelta,
            maxHeadDelta,
            mode1ViolationSteps,
            mode2ViolationSteps,
            firstStateFingerprintMismatch,
            dynamicRows[^1].Mode1Rollbacks,
            dynamicRows[^1].Mode2Rollbacks);

        Assert.Equal(DynamicSteps, dynamicRows.Count);
        Assert.Equal(12, initialRows.Count);
        Assert.True(headRows.Count >= 6);
    }

    private static IReadOnlyList<InitialNodeRow> CompareInitialNodes(PlantSnapshot mode1, PlantSnapshot mode2)
    {
        var mode1ById = mode1.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var mode2ById = mode2.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        Assert.Equal(mode1ById.Keys.OrderBy(static x => x, StringComparer.Ordinal), mode2ById.Keys.OrderBy(static x => x, StringComparer.Ordinal));

        var directMode1 = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var directMode2 = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);

        var rows = new List<InitialNodeRow>();
        foreach (var id in mode1ById.Keys.OrderBy(static x => x, StringComparer.Ordinal))
        {
            var left = mode1ById[id];
            var right = mode2ById[id];

            var inventoryEqual = Bits(left.Mass.Kilograms) == Bits(right.Mass.Kilograms)
                && Bits(left.InternalEnergy.Joules) == Bits(right.InternalEnergy.Joules);

            var factoryThermoEqual = StateBitsEqual(left.Thermodynamics, right.Thermodynamics);

            var replay1 = directMode1.Resolve(left.Definition, left.Inventory, left.Thermodynamics);
            var replay2 = directMode2.Resolve(left.Definition, left.Inventory, left.Thermodynamics);
            var directEqual = StateBitsEqual(replay1, replay2);

            rows.Add(new InitialNodeRow(
                id,
                inventoryEqual,
                left.Mass.Kilograms,
                right.Mass.Kilograms,
                left.InternalEnergy.Joules,
                right.InternalEnergy.Joules,
                factoryThermoEqual,
                left.Pressure.Pascals,
                right.Pressure.Pascals,
                right.Pressure.Pascals - left.Pressure.Pascals,
                left.Temperature.Kelvins,
                right.Temperature.Kelvins,
                right.Temperature.Kelvins - left.Temperature.Kelvins,
                left.Phase.ToString(),
                right.Phase.ToString(),
                Quality(left),
                Quality(right),
                directEqual,
                replay1.Pressure.Pascals,
                replay2.Pressure.Pascals,
                replay2.Pressure.Pascals - replay1.Pressure.Pascals,
                replay1.Temperature.Kelvins,
                replay2.Temperature.Kelvins,
                replay2.Temperature.Kelvins - replay1.Temperature.Kelvins,
                replay1.Phase.ToString(),
                replay2.Phase.ToString(),
                Quality(replay1),
                Quality(replay2)));
        }

        return rows;
    }

    private static IReadOnlyList<HydraulicHeadRow> CompareHydraulicHeads(PlantSnapshot mode1, PlantSnapshot mode2)
    {
        var pairs = new[]
        {
            new HeadSpec("main-circulation-pump", "suction", "pressure", 1_000_000d),
            new HeadSpec("channel", "pressure", "outlet", 0d),
            new HeadSpec("return", "outlet", "drum", 0d),
            new HeadSpec("main-steam-line", "steam", "header", 0d),
            new HeadSpec("stop-valve", "header", "stop-out", 0d),
            new HeadSpec("control-valve", "stop-out", "control-out", 0d),
            new HeadSpec("admission-valve", "control-out", "turbine-inlet", 0d),
            new HeadSpec("turbine-expansion", "turbine-inlet", "exhaust", 0d),
        };

        return pairs.Select(spec =>
        {
            var mode1Head = mode1.GetFluidNode(spec.From).Pressure.Pascals
                + spec.BoostPascals
                - mode1.GetFluidNode(spec.To).Pressure.Pascals;
            var mode2Head = mode2.GetFluidNode(spec.From).Pressure.Pascals
                + spec.BoostPascals
                - mode2.GetFluidNode(spec.To).Pressure.Pascals;
            return new HydraulicHeadRow(
                spec.Id,
                spec.From,
                spec.To,
                spec.BoostPascals,
                mode1Head,
                mode2Head,
                mode2Head - mode1Head);
        }).ToArray();
    }

    private static IReadOnlyList<DynamicRow> RunFirstSecond(
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine mode2)
    {
        var telemetry1 = new DesktopHydraulicProductionTelemetryProbe();
        var telemetry2 = new DesktopHydraulicProductionTelemetryProbe();
        var rows = new List<DynamicRow>(DynamicSteps);

        for (var step = 1; step <= DynamicSteps; step++)
        {
            var snapshot1 = mode1.Step(ControlRoomRunState.Running);
            var snapshot2 = mode2.Step(ControlRoomRunState.Running);
            telemetry1.Observe(mode1);
            telemetry2.Observe(mode2);

            var state1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var state2 = mode2.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;

            var generator1 = Assert.Single(snapshot1.Electrical.Generators);
            var generator2 = Assert.Single(snapshot2.Electrical.Generators);
            var drum1 = Assert.Single(state1.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var drum2 = Assert.Single(state2.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var speed1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary
                .ControlAndActuator.Controllers.GetDiagnostic("speed-control");
            var speed2 = mode2.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary
                .ControlAndActuator.Controllers.GetDiagnostic("speed-control");

            var electrical1 = generator1.ElectricalOutput.NumericValue ?? double.NaN;
            var electrical2 = generator2.ElectricalOutput.NumericValue ?? double.NaN;
            var pump1 = state1.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
            var pump2 = state2.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
            var drumLevel1 = drum1.LiquidLevelFraction.Fraction;
            var drumLevel2 = drum2.LiquidLevelFraction.Fraction;
            var governor1 = speed1.Output;
            var governor2 = speed2.Output;

            var keyEnvelope1 = InKeyEnvelope(electrical1, pump1, drumLevel1, governor1)
                && !snapshot1.AnyTripActive
                && generator1.BreakerClosed;
            var keyEnvelope2 = InKeyEnvelope(electrical2, pump2, drumLevel2, governor2)
                && !snapshot2.AnyTripActive
                && generator2.BreakerClosed;

            rows.Add(new DynamicRow(
                step,
                step * 0.01d,
                electrical1,
                electrical2,
                pump1,
                pump2,
                drumLevel1,
                drumLevel2,
                governor1,
                governor2,
                telemetry1.Snapshot().RollbackSteps,
                telemetry2.Snapshot().RollbackSteps,
                keyEnvelope1,
                keyEnvelope2,
                ControlRoomSnapshotFingerprint.Compute(snapshot1),
                ControlRoomSnapshotFingerprint.Compute(snapshot2)));
        }

        return rows;
    }

    private static bool InKeyEnvelope(double electrical, double pump, double drum, double governor)
        => electrical >= 4.99d && electrical <= 5.01d
            && pump >= 99.9d && pump <= 100.1d
            && drum >= 0.49d && drum <= 0.51d
            && governor >= 29.27d && governor <= 29.30d;

    private static IntegratedAutomaticOperationRuntimeEngine CreateShadow(WaterSteamThermodynamicClosureMode mode)
    {
        var method = typeof(M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalification2Tests)
            .GetMethod("CreateExactV9EquivalentShadow", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalification2Tests).FullName,
                "CreateExactV9EquivalentShadow");

        return Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            method.Invoke(null, new object[] { mode }));
    }

    private static bool StateBitsEqual(FluidThermodynamicState left, FluidThermodynamicState right)
        => Bits(left.Pressure.Pascals) == Bits(right.Pressure.Pascals)
            && Bits(left.Temperature.Kelvins) == Bits(right.Temperature.Kelvins)
            && left.Phase == right.Phase
            && NullableBits(left.VaporQuality?.Fraction) == NullableBits(right.VaporQuality?.Fraction);

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);
    private static long NullableBits(double? value) => value.HasValue ? Bits(value.Value) : long.MinValue;
    private static string Quality(FluidNodeState state) => Quality(state.Thermodynamics);
    private static string Quality(FluidThermodynamicState state)
        => state.VaporQuality?.Fraction.ToString("R", CultureInfo.InvariantCulture) ?? "null";
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static void WriteInitialNodeComparison(IReadOnlyList<InitialNodeRow> rows)
    {
        var lines = new List<string>
        {
            "node,inventory_bitwise_equal,mode1_mass_kg,mode2_mass_kg,mode1_energy_j,mode2_energy_j,factory_thermo_bitwise_equal,mode1_pressure_pa,mode2_pressure_pa,factory_pressure_delta_pa,mode1_temperature_k,mode2_temperature_k,factory_temperature_delta_k,mode1_phase,mode2_phase,mode1_quality,mode2_quality,direct_closure_bitwise_equal,direct_mode1_pressure_pa,direct_mode2_pressure_pa,direct_pressure_delta_pa,direct_mode1_temperature_k,direct_mode2_temperature_k,direct_temperature_delta_k,direct_mode1_phase,direct_mode2_phase,direct_mode1_quality,direct_mode2_quality"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.NodeId,
            row.InventoryBitwiseEqual.ToString().ToLowerInvariant(),
            F(row.Mode1MassKilograms), F(row.Mode2MassKilograms),
            F(row.Mode1InternalEnergyJoules), F(row.Mode2InternalEnergyJoules),
            row.FactoryThermodynamicsBitwiseEqual.ToString().ToLowerInvariant(),
            F(row.Mode1PressurePascals), F(row.Mode2PressurePascals), F(row.FactoryPressureDeltaPascals),
            F(row.Mode1TemperatureKelvins), F(row.Mode2TemperatureKelvins), F(row.FactoryTemperatureDeltaKelvins),
            row.Mode1Phase, row.Mode2Phase, row.Mode1Quality, row.Mode2Quality,
            row.DirectClosureBitwiseEqual.ToString().ToLowerInvariant(),
            F(row.DirectMode1PressurePascals), F(row.DirectMode2PressurePascals), F(row.DirectPressureDeltaPascals),
            F(row.DirectMode1TemperatureKelvins), F(row.DirectMode2TemperatureKelvins), F(row.DirectTemperatureDeltaKelvins),
            row.DirectMode1Phase, row.DirectMode2Phase, row.DirectMode1Quality, row.DirectMode2Quality)));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "01-initial-node-closure-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteHydraulicHeadComparison(IReadOnlyList<HydraulicHeadRow> rows)
    {
        var lines = new List<string>
        {
            "path,from,to,boost_pa,mode1_head_pa,mode2_head_pa,head_delta_pa"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.PathId, row.From, row.To, F(row.BoostPascals),
            F(row.Mode1HeadPascals), F(row.Mode2HeadPascals), F(row.HeadDeltaPascals))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "02-initial-hydraulic-head-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDynamicComparison(IReadOnlyList<DynamicRow> rows)
    {
        var lines = new List<string>
        {
            "step,seconds,mode1_electrical_mwe,mode2_electrical_mwe,mode1_primary_pump_kg_s,mode2_primary_pump_kg_s,mode1_drum_level,mode2_drum_level,mode1_governor_percent,mode2_governor_percent,mode1_rollbacks,mode2_rollbacks,mode1_key_envelope,mode2_key_envelope,mode1_fingerprint,mode2_fingerprint,fingerprint_match"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.Step, F(row.Seconds),
            F(row.Mode1ElectricalMegawatts), F(row.Mode2ElectricalMegawatts),
            F(row.Mode1PrimaryPumpKilogramsPerSecond), F(row.Mode2PrimaryPumpKilogramsPerSecond),
            F(row.Mode1DrumLevelFraction), F(row.Mode2DrumLevelFraction),
            F(row.Mode1GovernorPercent), F(row.Mode2GovernorPercent),
            row.Mode1Rollbacks, row.Mode2Rollbacks,
            row.Mode1KeyEnvelope.ToString().ToLowerInvariant(),
            row.Mode2KeyEnvelope.ToString().ToLowerInvariant(),
            row.Mode1Fingerprint, row.Mode2Fingerprint,
            (row.Mode1Fingerprint == row.Mode2Fingerprint).ToString().ToLowerInvariant())));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "03-first-second-dynamic-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(
        string classification,
        bool allInventoriesBitwiseEqual,
        int factoryThermodynamicDifferenceCount,
        int directClosureDifferenceCount,
        double maxFactoryPressureDelta,
        double maxDirectPressureDelta,
        double maxHeadDelta,
        int mode1ViolationSteps,
        int mode2ViolationSteps,
        int firstFingerprintMismatch,
        long mode1Rollbacks,
        long mode2Rollbacks)
    {
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "04-diagnostic-summary.txt"),
            new[]
            {
                "status=DIAGNOSTIC-EVIDENCE-WRITTEN",
                $"classification={classification}",
                $"all-initial-inventories-bitwise-equal={allInventoriesBitwiseEqual}",
                $"factory-thermodynamic-difference-nodes={factoryThermodynamicDifferenceCount}",
                $"same-inventory-direct-closure-difference-nodes={directClosureDifferenceCount}",
                FormattableString.Invariant($"max-factory-pressure-delta-pa={maxFactoryPressureDelta:R}"),
                FormattableString.Invariant($"max-same-inventory-direct-pressure-delta-pa={maxDirectPressureDelta:R}"),
                FormattableString.Invariant($"max-initial-hydraulic-head-delta-pa={maxHeadDelta:R}"),
                $"mode1-key-envelope-violation-steps={mode1ViolationSteps}",
                $"mode2-key-envelope-violation-steps={mode2ViolationSteps}",
                $"first-snapshot-fingerprint-mismatch-step={firstFingerprintMismatch}",
                $"mode1-rollbacks-after-100-steps={mode1Rollbacks}",
                $"mode2-rollbacks-after-100-steps={mode2Rollbacks}",
                "production-repair-applied=False",
                "threshold-change-applied=False",
                "r3-remains-red=True",
                "r4-planning-authorized=False",
            },
            Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled R3-2 failure diagnostic runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1");

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

    private sealed record HeadSpec(string Id, string From, string To, double BoostPascals);

    private sealed record InitialNodeRow(
        string NodeId,
        bool InventoryBitwiseEqual,
        double Mode1MassKilograms,
        double Mode2MassKilograms,
        double Mode1InternalEnergyJoules,
        double Mode2InternalEnergyJoules,
        bool FactoryThermodynamicsBitwiseEqual,
        double Mode1PressurePascals,
        double Mode2PressurePascals,
        double FactoryPressureDeltaPascals,
        double Mode1TemperatureKelvins,
        double Mode2TemperatureKelvins,
        double FactoryTemperatureDeltaKelvins,
        string Mode1Phase,
        string Mode2Phase,
        string Mode1Quality,
        string Mode2Quality,
        bool DirectClosureBitwiseEqual,
        double DirectMode1PressurePascals,
        double DirectMode2PressurePascals,
        double DirectPressureDeltaPascals,
        double DirectMode1TemperatureKelvins,
        double DirectMode2TemperatureKelvins,
        double DirectTemperatureDeltaKelvins,
        string DirectMode1Phase,
        string DirectMode2Phase,
        string DirectMode1Quality,
        string DirectMode2Quality);

    private sealed record HydraulicHeadRow(
        string PathId,
        string From,
        string To,
        double BoostPascals,
        double Mode1HeadPascals,
        double Mode2HeadPascals,
        double HeadDeltaPascals);

    private sealed record DynamicRow(
        int Step,
        double Seconds,
        double Mode1ElectricalMegawatts,
        double Mode2ElectricalMegawatts,
        double Mode1PrimaryPumpKilogramsPerSecond,
        double Mode2PrimaryPumpKilogramsPerSecond,
        double Mode1DrumLevelFraction,
        double Mode2DrumLevelFraction,
        double Mode1GovernorPercent,
        double Mode2GovernorPercent,
        long Mode1Rollbacks,
        long Mode2Rollbacks,
        bool Mode1KeyEnvelope,
        bool Mode2KeyEnvelope,
        string Mode1Fingerprint,
        string Mode2Fingerprint)
    {
        public bool SnapshotFingerprintMatch
            => string.Equals(Mode1Fingerprint, Mode2Fingerprint, StringComparison.Ordinal);
    }
}
