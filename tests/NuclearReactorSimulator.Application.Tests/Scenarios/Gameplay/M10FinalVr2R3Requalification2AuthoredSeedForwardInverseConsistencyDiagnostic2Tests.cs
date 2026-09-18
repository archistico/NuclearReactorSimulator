using System.Globalization;
using System.Reflection;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Diagnostic 2 for returned R3 Requalification 2 RED.
/// Reconstructs the authored exact-v9 raw fluid inventories before deterministic seed preconditioning,
/// using the same legacy-forward saturation-property recipe as the factory, then resolves each identical
/// conserved inventory with closure mode 1 and closure mode 2.
/// No production repair, threshold change, or new exact-version identity is created here.
/// </summary>
public sealed class M10FinalVr2R3Requalification2AuthoredSeedForwardInverseConsistencyDiagnostic2Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_R2_SEED_CONSISTENCY_DIAGNOSTIC2";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3Requalification2AuthoredSeedForwardInverseConsistencyDiagnostic2")]
    public void RawAuthoredExactV9Seed_IsCheckedForMode2ForwardInverseConsistencyBeforePreconditioning()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var plant = GetExactV9PlantDefinition();
        var forwardMode1 = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var forwardMode2 = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var inverseMode1 = forwardMode1;
        var inverseMode2 = forwardMode2;

        var seeds = BuildRawAuthoredSeeds(plant, forwardMode1);
        Assert.Equal(12, seeds.Count);

        var rows = seeds.Select(seed =>
        {
            var mode1 = inverseMode1.Resolve(seed.Definition, seed.Inventory, seed.PreviousState);
            var mode2 = inverseMode2.Resolve(seed.Definition, seed.Inventory, seed.PreviousState);

            return new SeedClosureRow(
                seed.NodeId,
                seed.SeedKind,
                seed.AuthoredPressurePascals,
                seed.AuthoredTemperatureKelvins,
                seed.AuthoredQuality,
                seed.Inventory.Mass.Kilograms,
                seed.Inventory.InternalEnergy.Joules,
                ForwardPropertiesBitwiseEqual(seed, forwardMode1, forwardMode2),
                StateBitsEqual(mode1, mode2),
                mode1.Pressure.Pascals,
                mode2.Pressure.Pascals,
                mode2.Pressure.Pascals - mode1.Pressure.Pascals,
                mode1.Temperature.Kelvins,
                mode2.Temperature.Kelvins,
                mode2.Temperature.Kelvins - mode1.Temperature.Kelvins,
                mode1.Phase.ToString(),
                mode2.Phase.ToString(),
                Quality(mode1),
                Quality(mode2));
        }).ToArray();

        WriteSeedClosureComparison(rows);

        var mode1States = rows.ToDictionary(static row => row.NodeId, StringComparer.Ordinal);
        var headRows = BuildHeadRows(mode1States);
        WriteHydraulicHeadComparison(headRows);

        var forwardProvidersEqual = rows.All(static row => row.ForwardPropertiesBitwiseEqual);
        var inverseDifferenceCount = rows.Count(static row => !row.Mode1Mode2StateBitwiseEqual);
        var phaseDifferenceCount = rows.Count(static row => !string.Equals(row.Mode1Phase, row.Mode2Phase, StringComparison.Ordinal));
        var maxPressureDelta = rows.Max(static row => Math.Abs(row.PressureDeltaPascals));
        var maxTemperatureDelta = rows.Max(static row => Math.Abs(row.TemperatureDeltaKelvins));
        var maxHeadDelta = headRows.Max(static row => Math.Abs(row.HeadDeltaPascals));

        var classification = forwardProvidersEqual && inverseDifferenceCount > 0
            ? "LEGACY-FORWARD-MODE2-INVERSE-SEED-CONSISTENCY-GAP"
            : !forwardProvidersEqual
                ? "FORWARD-PROPERTY-PROVIDER-DIVERGENCE"
                : "NO-RAW-SEED-CLOSURE-DIVERGENCE";

        WriteSummary(
            classification,
            forwardProvidersEqual,
            inverseDifferenceCount,
            phaseDifferenceCount,
            maxPressureDelta,
            maxTemperatureDelta,
            maxHeadDelta);

        Assert.Equal(12, rows.Length);
        Assert.Equal(8, headRows.Length);
    }

    private static PlantDefinition GetExactV9PlantDefinition()
    {
        var method = typeof(M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalification2Tests)
            .GetMethod("CreateExactV9EquivalentShadow", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalification2Tests).FullName,
                "CreateExactV9EquivalentShadow");

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            method.Invoke(
                null,
                new object[] { WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain }));

        return engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.Definition;
    }

    private static IReadOnlyList<RawSeed> BuildRawAuthoredSeeds(
        PlantDefinition plant,
        SimplifiedWaterSteamThermodynamicModel forward)
    {
        var rows = new List<RawSeed>
        {
            Saturated(plant, forward, "suction", 6.416459281680372d, 0d),
            Subcooled(plant, forward, "pressure", 280.1582998275795d, 0.0002203018767873456d),
            Saturated(plant, forward, "outlet", 6.666459281680372d, 0.21514191259171503d),
            SteamDrumAtLevel(plant, forward, "drum", 280d, 0.5d),
            Saturated(plant, forward, "steam", 6.398665756944915d, 0.99978878048067332d),
            Saturated(plant, forward, "header", 6.2474207966935325d, 0.99802224982943177d),
            Saturated(plant, forward, "stop-out", 6.0694855493389648d, 0.99600970115709719d),
            Saturated(plant, forward, "control-out", 3.9941878857133641d, 0.97776527205630726d),
            Saturated(plant, forward, "turbine-inlet", 3.8162526383587956d, 0.97666768842836382d),
            Saturated(plant, forward, "exhaust", 0.008438344971042927d, 0.87290510788436326d),
            Saturated(plant, forward, "hotwell", 0.010808002980612689d, 0d),
            Subcooled(plant, forward, "feedwater-inventory", 47.37848866583073d, 0.000003024302581887423d),
        };

        return rows.OrderBy(static row => row.NodeId, StringComparer.Ordinal).ToArray();
    }

    private static RawSeed Saturated(
        PlantDefinition plant,
        SimplifiedWaterSteamThermodynamicModel forward,
        string nodeId,
        double pressureMegapascals,
        double quality)
    {
        var definition = plant.GetFluidNode(nodeId);
        var saturation = forward.GetSaturationProperties(Pressure.FromMegapascals(pressureMegapascals));
        var v = saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram
            + quality
            * (saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram
                - saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram);
        var u = SpecificEnergy.FromJoulesPerKilogram(
            saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram
            + quality
            * (saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram
                - saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram));
        var mass = Mass.FromKilograms(definition.Volume.CubicMetres / v);
        var inventory = new FluidNodeInventory(mass, u * mass);
        var previous = new FluidThermodynamicState(
            saturation.Pressure,
            saturation.Temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(quality));

        return new RawSeed(
            nodeId,
            "SaturatedMixture",
            definition,
            inventory,
            previous,
            saturation.Pressure.Pascals,
            saturation.Temperature.Kelvins,
            quality,
            SeedForwardBasis.Pressure,
            pressureMegapascals);
    }

    private static RawSeed Subcooled(
        PlantDefinition plant,
        SimplifiedWaterSteamThermodynamicModel forward,
        string nodeId,
        double temperatureCelsius,
        double compressionFraction)
    {
        var definition = plant.GetFluidNode(nodeId);
        var temperature = Temperature.FromDegreesCelsius(temperatureCelsius);
        var saturation = forward.GetSaturationProperties(temperature);
        var density = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre * (1d + compressionFraction);
        var mass = Mass.FromKilograms(density * definition.Volume.CubicMetres);
        var inventory = new FluidNodeInventory(mass, saturation.SaturatedLiquidInternalEnergy * mass);
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(saturation.Pressure.Pascals + 2_200d),
            temperature,
            FluidPhase.SubcooledLiquid,
            null);

        return new RawSeed(
            nodeId,
            "SubcooledLiquid",
            definition,
            inventory,
            previous,
            previous.Pressure.Pascals,
            temperature.Kelvins,
            null,
            SeedForwardBasis.Temperature,
            temperature.Kelvins);
    }

    private static RawSeed SteamDrumAtLevel(
        PlantDefinition plant,
        SimplifiedWaterSteamThermodynamicModel forward,
        string nodeId,
        double temperatureCelsius,
        double liquidLevelFraction)
    {
        var definition = plant.GetFluidNode(nodeId);
        var temperature = Temperature.FromDegreesCelsius(temperatureCelsius);
        var saturation = forward.GetSaturationProperties(temperature);
        var liquidMass = Mass.FromKilograms(
            liquidLevelFraction
            * definition.Volume.CubicMetres
            * saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre);
        var vaporMass = Mass.FromKilograms(
            (1d - liquidLevelFraction)
            * definition.Volume.CubicMetres
            * saturation.SaturatedVaporDensity.KilogramsPerCubicMetre);
        var totalMass = liquidMass + vaporMass;
        var quality = vaporMass.Kilograms / totalMass.Kilograms;
        var inventory = new FluidNodeInventory(
            totalMass,
            (saturation.SaturatedLiquidInternalEnergy * liquidMass)
                + (saturation.SaturatedVaporInternalEnergy * vaporMass));
        var previous = new FluidThermodynamicState(
            saturation.Pressure,
            temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(quality));

        return new RawSeed(
            nodeId,
            "SteamDrumAtLevel",
            definition,
            inventory,
            previous,
            saturation.Pressure.Pascals,
            temperature.Kelvins,
            quality,
            SeedForwardBasis.Temperature,
            temperature.Kelvins);
    }

    private static bool ForwardPropertiesBitwiseEqual(
        RawSeed seed,
        SimplifiedWaterSteamThermodynamicModel mode1,
        SimplifiedWaterSteamThermodynamicModel mode2)
    {
        WaterSteamSaturationProperties left;
        WaterSteamSaturationProperties right;

        if (seed.ForwardBasis == SeedForwardBasis.Pressure)
        {
            left = mode1.GetSaturationProperties(Pressure.FromMegapascals(seed.ForwardBasisValue));
            right = mode2.GetSaturationProperties(Pressure.FromMegapascals(seed.ForwardBasisValue));
        }
        else
        {
            left = mode1.GetSaturationProperties(Temperature.FromKelvins(seed.ForwardBasisValue));
            right = mode2.GetSaturationProperties(Temperature.FromKelvins(seed.ForwardBasisValue));
        }

        return Bits(left.Pressure.Pascals) == Bits(right.Pressure.Pascals)
            && Bits(left.Temperature.Kelvins) == Bits(right.Temperature.Kelvins)
            && Bits(left.SaturatedLiquidDensity.KilogramsPerCubicMetre) == Bits(right.SaturatedLiquidDensity.KilogramsPerCubicMetre)
            && Bits(left.SaturatedVaporDensity.KilogramsPerCubicMetre) == Bits(right.SaturatedVaporDensity.KilogramsPerCubicMetre)
            && Bits(left.SaturatedLiquidInternalEnergy.JoulesPerKilogram) == Bits(right.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            && Bits(left.SaturatedVaporInternalEnergy.JoulesPerKilogram) == Bits(right.SaturatedVaporInternalEnergy.JoulesPerKilogram);
    }

    private static HydraulicHeadRow[] BuildHeadRows(IReadOnlyDictionary<string, SeedClosureRow> states)
    {
        var specs = new[]
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

        return specs.Select(spec =>
        {
            var mode1Head = states[spec.From].Mode1PressurePascals
                + spec.BoostPascals
                - states[spec.To].Mode1PressurePascals;
            var mode2Head = states[spec.From].Mode2PressurePascals
                + spec.BoostPascals
                - states[spec.To].Mode2PressurePascals;
            return new HydraulicHeadRow(
                spec.Id,
                spec.From,
                spec.To,
                mode1Head,
                mode2Head,
                mode2Head - mode1Head);
        }).ToArray();
    }

    private static bool StateBitsEqual(FluidThermodynamicState left, FluidThermodynamicState right)
        => Bits(left.Pressure.Pascals) == Bits(right.Pressure.Pascals)
            && Bits(left.Temperature.Kelvins) == Bits(right.Temperature.Kelvins)
            && left.Phase == right.Phase
            && NullableBits(left.VaporQuality?.Fraction) == NullableBits(right.VaporQuality?.Fraction);

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);
    private static long NullableBits(double? value) => value.HasValue ? Bits(value.Value) : long.MinValue;
    private static string Quality(FluidThermodynamicState state)
        => state.VaporQuality?.Fraction.ToString("R", CultureInfo.InvariantCulture) ?? "null";
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static void WriteSeedClosureComparison(IReadOnlyList<SeedClosureRow> rows)
    {
        var lines = new List<string>
        {
            "node,seed_kind,authored_pressure_pa,authored_temperature_k,authored_quality,mass_kg,internal_energy_j,forward_properties_bitwise_equal,mode1_mode2_state_bitwise_equal,mode1_pressure_pa,mode2_pressure_pa,pressure_delta_pa,mode1_temperature_k,mode2_temperature_k,temperature_delta_k,mode1_phase,mode2_phase,mode1_quality,mode2_quality"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.NodeId,
            row.SeedKind,
            F(row.AuthoredPressurePascals),
            F(row.AuthoredTemperatureKelvins),
            row.AuthoredQuality?.ToString("R", CultureInfo.InvariantCulture) ?? "null",
            F(row.MassKilograms),
            F(row.InternalEnergyJoules),
            row.ForwardPropertiesBitwiseEqual.ToString().ToLowerInvariant(),
            row.Mode1Mode2StateBitwiseEqual.ToString().ToLowerInvariant(),
            F(row.Mode1PressurePascals),
            F(row.Mode2PressurePascals),
            F(row.PressureDeltaPascals),
            F(row.Mode1TemperatureKelvins),
            F(row.Mode2TemperatureKelvins),
            F(row.TemperatureDeltaKelvins),
            row.Mode1Phase,
            row.Mode2Phase,
            row.Mode1Quality,
            row.Mode2Quality)));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "01-raw-authored-seed-closure-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteHydraulicHeadComparison(IReadOnlyList<HydraulicHeadRow> rows)
    {
        var lines = new List<string>
        {
            "path,from,to,mode1_head_pa,mode2_head_pa,head_delta_pa"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.PathId,
            row.From,
            row.To,
            F(row.Mode1HeadPascals),
            F(row.Mode2HeadPascals),
            F(row.HeadDeltaPascals))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "02-raw-authored-seed-hydraulic-head-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(
        string classification,
        bool forwardProvidersEqual,
        int inverseDifferenceCount,
        int phaseDifferenceCount,
        double maxPressureDelta,
        double maxTemperatureDelta,
        double maxHeadDelta)
    {
        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "03-diagnostic-summary.txt"),
            new[]
            {
                "status=DIAGNOSTIC-EVIDENCE-WRITTEN",
                $"classification={classification}",
                $"forward-property-providers-bitwise-equal={forwardProvidersEqual}",
                $"raw-seed-mode1-mode2-inverse-difference-nodes={inverseDifferenceCount}",
                $"raw-seed-phase-difference-nodes={phaseDifferenceCount}",
                FormattableString.Invariant($"max-raw-seed-pressure-delta-pa={maxPressureDelta:R}"),
                FormattableString.Invariant($"max-raw-seed-temperature-delta-k={maxTemperatureDelta:R}"),
                FormattableString.Invariant($"max-raw-seed-hydraulic-head-delta-pa={maxHeadDelta:R}"),
                "deterministic-preconditioning-included=False",
                "production-repair-applied=False",
                "threshold-change-applied=False",
                "new-exact-version-created=False",
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
                $"Set {OptInEnvironmentVariable}=1 only from the controlled Diagnostic 2 runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2");

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

    private enum SeedForwardBasis
    {
        Pressure,
        Temperature,
    }

    private sealed record RawSeed(
        string NodeId,
        string SeedKind,
        FluidNodeDefinition Definition,
        FluidNodeInventory Inventory,
        FluidThermodynamicState PreviousState,
        double AuthoredPressurePascals,
        double AuthoredTemperatureKelvins,
        double? AuthoredQuality,
        SeedForwardBasis ForwardBasis,
        double ForwardBasisValue);

    private sealed record SeedClosureRow(
        string NodeId,
        string SeedKind,
        double AuthoredPressurePascals,
        double AuthoredTemperatureKelvins,
        double? AuthoredQuality,
        double MassKilograms,
        double InternalEnergyJoules,
        bool ForwardPropertiesBitwiseEqual,
        bool Mode1Mode2StateBitwiseEqual,
        double Mode1PressurePascals,
        double Mode2PressurePascals,
        double PressureDeltaPascals,
        double Mode1TemperatureKelvins,
        double Mode2TemperatureKelvins,
        double TemperatureDeltaKelvins,
        string Mode1Phase,
        string Mode2Phase,
        string Mode1Quality,
        string Mode2Quality);

    private sealed record HeadSpec(string Id, string From, string To, double BoostPascals);

    private sealed record HydraulicHeadRow(
        string PathId,
        string From,
        string To,
        double Mode1HeadPascals,
        double Mode2HeadPascals,
        double HeadDeltaPascals);
}
