using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// R1 selected-C4 opt-in production implementation gate. The test is explicit and writes only
/// bounded R1 evidence. It does not activate mode 2 at any existing application/exact-v9 call site.
/// </summary>
public sealed class R1SelectedC4OptInClosureImplementationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1";
    private const string FrozenC4Relative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Artifacts";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const string HistoricalMode1BaselineProductionModelSha256 = "93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E";
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R1SelectedC4OptInClosureImplementation1")]
    public void R1SelectedC4_ProductionMode2_IsBitEquivalentReproducibleAndHistoricalModesRemainStable()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));
        var root = FindRepositoryRoot();
        var artifactDirectory = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(artifactDirectory);

        VerifyPayloadReproducibility(root, artifactDirectory);

        Assert.Equal(0, (int)WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology);
        Assert.Equal(1, (int)WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        Assert.Equal(2, (int)WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);

        var observations = LoadStateObservations(root);
        Assert.Equal(1_679, observations.Count);

        var productionResolver = new ReferenceConsistentTabulatedInverseResolver();
        var secondResolver = new ReferenceConsistentTabulatedInverseResolver();
        Assert.Equal(1, ReferenceConsistentTabulatedInverseResolver.PayloadLoadCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimeResourceIoCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimePayloadDecodeCount);

        var c4 = new Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate();
        var productionModel = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);

        var stateMismatches = 0;
        var repeatMismatches = 0;
        var resolveMismatches = 0;
        var pathMismatches = 0;
        var modelIntegrationMismatches = 0;
        var exactV9 = new Dictionary<NodeKey, StateBits>();

        for (var index = 0; index < observations.Count; index++)
        {
            var observation = observations[index];

            var c4Resolved = c4.TryResolveWithPath(
                observation.SpecificVolume,
                observation.SpecificEnergy,
                out var c4State,
                out var c4Path);
            var firstResolved = productionResolver.TryResolveWithPath(
                observation.SpecificVolume,
                observation.SpecificEnergy,
                out var firstState,
                out var firstPath);
            var secondResolved = secondResolver.TryResolveWithPath(
                observation.SpecificVolume,
                observation.SpecificEnergy,
                out var secondState,
                out var secondPath);

            var firstBits = ToBits(firstResolved, firstState);
            var secondBits = ToBits(secondResolved, secondState);
            var c4Bits = ToBits(c4Resolved, c4State);
            var expectedBits = observation.Expected;

            if (firstResolved != c4Resolved || firstResolved != expectedBits.Resolved)
            {
                resolveMismatches++;
            }

            if (!StateBitsEqual(firstBits, c4Bits) || !StateBitsEqual(firstBits, expectedBits))
            {
                stateMismatches++;
            }

            if (!StateBitsEqual(firstBits, secondBits) || firstPath != secondPath)
            {
                repeatMismatches++;
            }

            if (c4Resolved && firstResolved
                && !string.Equals(c4Path.ToString(), firstPath.ToString(), StringComparison.Ordinal))
            {
                pathMismatches++;
            }

            // The resolver/C4 comparison above remains in the frozen Celsius/MPa bit domain.
            // Model integration must instead compare the Domain quantities in their canonical stored units:
            // Temperature stores kelvins and Pressure stores pascals. Round-tripping the model pressure back
            // through Megapascals is not bit-surjective and skips 79 otherwise-correct frozen MPa values.
            var modelBits = ResolveModelCanonical(
                productionModel,
                observation.SourceIdentity,
                observation.SpecificVolume,
                observation.SpecificEnergy);
            var expectedModelBits = ToModelCanonicalBits(firstResolved, firstState);
            if (!modelBits.Equals(expectedModelBits))
            {
                modelIntegrationMismatches++;
            }

            if (observation.NodeKey.HasValue)
            {
                exactV9.Add(observation.NodeKey.Value, firstBits);
            }
        }

        Assert.Equal(0, stateMismatches);
        Assert.Equal(0, repeatMismatches);
        Assert.Equal(0, resolveMismatches);
        Assert.Equal(0, pathMismatches);
        Assert.Equal(0, modelIntegrationMismatches);
        Assert.Equal(360, exactV9.Count);

        // Resolve-time allocation is measured after payload construction/warm-up and against the
        // allocation-neutral internal resolver struct path, not the public reference-class state wrapper.
        foreach (var observation in observations)
        {
            _ = productionResolver.TryResolveWithPath(
                observation.SpecificVolume,
                observation.SpecificEnergy,
                out _,
                out _);
        }

        ForceFullCollection();
        var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < observations.Count; index++)
        {
            var observation = observations[index];
            _ = productionResolver.TryResolveWithPath(
                observation.SpecificVolume,
                observation.SpecificEnergy,
                out _,
                out _);
        }
        var resolveAllocationBytes = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        Assert.Equal(0, resolveAllocationBytes);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimeResourceIoCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimePayloadDecodeCount);

        var hydraulicMismatches = VerifyHydraulicEquivalence(root, exactV9);
        Assert.Equal(0, hydraulicMismatches);

        var historical = VerifyHistoricalModes(root);
        Assert.Equal(0, historical.DefaultVsMode0Mismatches);
        Assert.True(
            historical.Mode1LegacyProjectionMatchesBaseline,
            $"Mode 1 legacy source projection drifted. Expected {HistoricalMode1BaselineProductionModelSha256}, actual {historical.Mode1LegacyProjectionSha256}.");

        WriteEquivalenceSummary(
            artifactDirectory,
            observations.Count,
            hydraulicComparisons: 288,
            stateMismatches,
            hydraulicMismatches,
            repeatMismatches,
            resolveMismatches,
            pathMismatches,
            modelIntegrationMismatches,
            resolveAllocationBytes);
        WriteHistoricalSummary(artifactDirectory, historical);
    }

    private static void VerifyPayloadReproducibility(string root, string artifactDirectory)
    {
        var first = NrsVr2C4ReferencePayloadGenerator.GeneratePayloadBytes();
        var second = NrsVr2C4ReferencePayloadGenerator.GeneratePayloadBytes();
        Assert.Equal(first, second);

        var payloadPath = Path.Combine(
            root,
            "src",
            "NuclearReactorSimulator.Simulation",
            "Physics",
            "Fluids",
            "ReferenceData",
            "NRSVR2C4.v1.bin");
        var checkedIn = File.ReadAllBytes(payloadPath);
        Assert.Equal(first, checkedIn);

        var sha = NrsVr2C4ReferencePayloadGenerator.Sha256Hex(first);
        Assert.Equal(ReferenceConsistentTabulatedInverseResolver.ExpectedPayloadSha256, sha);

        var lines = new[]
        {
            "status=PASS-REFERENCE-DATA-PROVENANCE",
            "generator=OFFLINE-CSharp-ONLY",
            "schema=NRSVR2C4-v1",
            "payload-bytes=" + first.Length.ToString(CultureInfo.InvariantCulture),
            "payload-sha256=" + sha,
            "regeneration-1-sha256=" + NrsVr2C4ReferencePayloadGenerator.Sha256Hex(first),
            "regeneration-2-sha256=" + NrsVr2C4ReferencePayloadGenerator.Sha256Hex(second),
            "byte-identical-two-regenerations=True",
            "byte-identical-to-checked-in=True",
            "compiled-sha256-match=True",
            "runtime-authority=COMPILED-CSharp-CONSTANT",
            "runtime-if97-dependency=False",
        };
        File.WriteAllLines(Path.Combine(artifactDirectory, "03-reference-data-provenance.txt"), lines, Utf8WithoutBom);
    }

    private static List<StateObservation> LoadStateObservations(string root)
    {
        var rp1a = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var c4 = Path.Combine(root, FrozenC4Relative.Replace('/', Path.DirectorySeparatorChar));
        var expectedRows = File.ReadAllLines(Path.Combine(c4, "02-state-semantic-equivalence.csv"), Encoding.UTF8)
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseFrozenC4StateRow)
            .ToArray();
        Assert.Equal(1_679, expectedRows.Length);

        var observations = new List<StateObservation>(1_679);

        foreach (var line in File.ReadAllLines(Path.Combine(rp1a, "02-vr2-reference-point-corpus.csv"), Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            var volume = DN(parts[6]);
            var energy = DN(parts[7]);
            if (!double.IsFinite(volume) || volume <= 0d || !double.IsFinite(energy)) continue;
            AddObservation(observations, expectedRows, "VR2", parts[0], volume, energy, null);
        }

        foreach (var line in File.ReadAllLines(Path.Combine(rp1a, "03-exact-v9-node-corpus.csv"), Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            var logicalStep = long.Parse(parts[1], CultureInfo.InvariantCulture);
            var density = D(parts[6]);
            var key = new NodeKey(parts[0], logicalStep, parts[3]);
            var identity = NodeIdentity(key.ProbeId, key.LogicalStep, key.NodeId);
            AddObservation(observations, expectedRows, "EXACT-V9", identity, 1d / density, D(parts[7]), key);
        }

        foreach (var line in File.ReadAllLines(Path.Combine(rp1a, "05-seam-probe-map.csv"), Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            var identity = SeamIdentity(int.Parse(parts[0], CultureInfo.InvariantCulture), parts[2]);
            AddObservation(observations, expectedRows, "SEAM", identity, D(parts[6]), D(parts[7]), null);
        }

        Assert.Equal(39, observations.Count(static row => row.Domain == "VR2"));
        Assert.Equal(360, observations.Count(static row => row.Domain == "EXACT-V9"));
        Assert.Equal(1_280, observations.Count(static row => row.Domain == "SEAM"));
        return observations;
    }

    private static void AddObservation(
        List<StateObservation> observations,
        FrozenStateRow[] expectedRows,
        string domain,
        string identity,
        double volume,
        double energy,
        NodeKey? nodeKey)
    {
        var index = observations.Count;
        var expected = expectedRows[index];
        Assert.Equal(index, expected.ObservationIndex);
        Assert.Equal(domain, expected.Domain);
        Assert.Equal(identity, expected.SourceIdentity);
        observations.Add(new StateObservation(domain, index, identity, volume, energy, expected.Bits, nodeKey));
    }

    private static FrozenStateRow ParseFrozenC4StateRow(string line)
    {
        var parts = line.Split(',');
        Assert.Equal(20, parts.Length);
        var resolved = bool.Parse(parts[4]);
        return new FrozenStateRow(
            parts[0],
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            parts[2],
            new StateBits(
                resolved,
                parts[8],
                resolved ? long.Parse(parts[10], CultureInfo.InvariantCulture) : 0L,
                resolved ? long.Parse(parts[12], CultureInfo.InvariantCulture) : 0L,
                bool.Parse(parts[14]),
                bool.Parse(parts[14]) ? long.Parse(parts[16], CultureInfo.InvariantCulture) : 0L));
    }

    private static int VerifyHydraulicEquivalence(string root, IReadOnlyDictionary<NodeKey, StateBits> exactV9)
    {
        var rp1a = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var c4 = Path.Combine(root, FrozenC4Relative.Replace('/', Path.DirectorySeparatorChar));

        var expected = new Dictionary<string, HydraulicBits>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(Path.Combine(c4, "03-hydraulic-semantic-equivalence.csv"), Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            Assert.Equal(18, parts.Length);
            var identity = HydraulicIdentity(parts[2], long.Parse(parts[3], CultureInfo.InvariantCulture), parts[4]);
            expected.Add(identity, new HydraulicBits(
                bool.Parse(parts[8]),
                long.Parse(parts[10], CultureInfo.InvariantCulture),
                long.Parse(parts[12], CultureInfo.InvariantCulture),
                bool.Parse(parts[14])));
        }

        var mismatches = 0;
        var count = 0;
        foreach (var line in File.ReadAllLines(Path.Combine(rp1a, "04-hydraulic-context.csv"), Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            var logicalStep = long.Parse(parts[1], CultureInfo.InvariantCulture);
            var identity = HydraulicIdentity(parts[0], logicalStep, parts[3]);
            Assert.True(expected.TryGetValue(identity, out var expectedBits));

            var from = exactV9[new NodeKey(parts[0], logicalStep, parts[4])];
            var to = exactV9[new NodeKey(parts[0], logicalStep, parts[5])];
            var actual = EvaluateHydraulic(
                from,
                to,
                parts[3],
                D(parts[6]),
                D(parts[7]),
                D(parts[8]));
            if (!actual.Equals(expectedBits))
            {
                mismatches++;
            }
            count++;
        }

        Assert.Equal(288, count);
        return mismatches;
    }

    private static HydraulicBits EvaluateHydraulic(
        StateBits from,
        StateBits to,
        string pathId,
        double resistance,
        double activeBoostPascals,
        double productionDrivingPascals)
    {
        var resolved = from.Resolved && to.Resolved;
        if (!resolved) return new HydraulicBits(false, 0L, 0L, false);

        var fromPressure = BitConverter.Int64BitsToDouble(from.PressureBits);
        var toPressure = BitConverter.Int64BitsToDouble(to.PressureBits);
        var driving = ((fromPressure - toPressure) * 1_000_000d) + activeBoostPascals;
        var flow = SolveQuadraticFlow(driving, resistance, HasFrozenCheckValve(pathId));
        var signChanged = double.IsFinite(driving)
            && double.IsFinite(productionDrivingPascals)
            && Math.Abs(driving) > 1e-9d
            && Math.Abs(productionDrivingPascals) > 1e-9d
            && Math.Sign(driving) != Math.Sign(productionDrivingPascals);
        return new HydraulicBits(
            true,
            BitConverter.DoubleToInt64Bits(driving),
            BitConverter.DoubleToInt64Bits(flow),
            signChanged);
    }

    private static HistoricalRegression VerifyHistoricalModes(string root)
    {
        var rp1a = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var defaultModel = new SimplifiedWaterSteamThermodynamicModel();
        var mode0 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology);
        var mode1 = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);

        var defaultVsMode0 = 0;
        var mode1Frozen = 0;
        var count = 0;
        foreach (var line in File.ReadAllLines(Path.Combine(rp1a, "03-exact-v9-node-corpus.csv"), Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            var density = D(parts[6]);
            var volume = 1d / density;
            var energy = D(parts[7]);
            var id = NodeIdentity(parts[0], long.Parse(parts[1], CultureInfo.InvariantCulture), parts[3]);

            var defaultBits = ResolveModel(defaultModel, id, volume, energy);
            var mode0Bits = ResolveModel(mode0, id, volume, energy);
            if (!StateBitsEqual(defaultBits, mode0Bits)) defaultVsMode0++;

            var mode1Bits = ResolveModel(mode1, id, volume, energy);
            var frozenMode1 = new StateBits(
                true,
                parts[4],
                BitConverter.DoubleToInt64Bits(D(parts[8])),
                BitConverter.DoubleToInt64Bits(D(parts[9])),
                !string.IsNullOrWhiteSpace(parts[5]),
                string.IsNullOrWhiteSpace(parts[5]) ? 0L : BitConverter.DoubleToInt64Bits(D(parts[5])));
            if (!StateBitsEqual(mode1Bits, frozenMode1)) mode1Frozen++;
            count++;
        }

        Assert.Equal(360, count);
        var legacyProjectionSha256 = ComputeHistoricalMode1LegacyProjectionSha256(root);
        var legacyProjectionMatchesBaseline = string.Equals(
            legacyProjectionSha256,
            HistoricalMode1BaselineProductionModelSha256,
            StringComparison.Ordinal);

        return new HistoricalRegression(
            count,
            defaultVsMode0,
            mode1Frozen,
            legacyProjectionSha256,
            legacyProjectionMatchesBaseline);
    }

    private static string ComputeHistoricalMode1LegacyProjectionSha256(string root)
    {
        var modelPath = Path.Combine(
            root,
            "src",
            "NuclearReactorSimulator.Simulation",
            "Physics",
            "Fluids",
            "SimplifiedWaterSteamThermodynamicModel.cs");

        var source = File.ReadAllText(modelPath, Encoding.UTF8)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        source = RemoveExactlyOnce(
            source,
            "    private readonly ReferenceConsistentTabulatedInverseResolver? _referenceConsistentTabulatedInverseResolver;\n",
            "mode2 resolver field");

        source = RemoveExactlyOnce(
            source,
            "        if (closureMode == WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain)\n"
                + "        {\n"
                + "            _referenceConsistentTabulatedInverseResolver = new ReferenceConsistentTabulatedInverseResolver();\n"
                + "        }\n",
            "mode2 constructor dispatch");

        source = RemoveExactlyOnce(
            source,
            "        if (_referenceConsistentTabulatedInverseResolver is not null)\n"
                + "        {\n"
                + "            if (_referenceConsistentTabulatedInverseResolver.TryResolve(\n"
                + "                    specificVolume,\n"
                + "                    specificInternalEnergy,\n"
                + "                    out var referenceConsistentState))\n"
                + "            {\n"
                + "                return referenceConsistentState.ToFluidThermodynamicState();\n"
                + "            }\n"
                + "\n"
                + "            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);\n"
                + "        }\n"
                + "\n",
            "mode2 Resolve dispatch");

        var historicalCrlfBytes = Encoding.UTF8.GetBytes(
            source.Replace("\n", "\r\n", StringComparison.Ordinal));
        return Convert.ToHexString(SHA256.HashData(historicalCrlfBytes));
    }

    private static string RemoveExactlyOnce(string text, string fragment, string label)
    {
        var first = text.IndexOf(fragment, StringComparison.Ordinal);
        Assert.True(first >= 0, $"Historical source projection fragment missing: {label}.");
        var second = text.IndexOf(fragment, first + fragment.Length, StringComparison.Ordinal);
        Assert.Equal(-1, second);
        return text.Remove(first, fragment.Length);
    }

    private static ModelStateBits ResolveModelCanonical(
        SimplifiedWaterSteamThermodynamicModel model,
        string id,
        double specificVolume,
        double specificEnergy)
    {
        try
        {
            var definition = new FluidNodeDefinition(id, Volume.FromCubicMetres(specificVolume));
            var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(specificEnergy));
            var state = model.Resolve(
                definition,
                inventory,
                new FluidThermodynamicState(Pressure.StandardAtmosphere, Temperature.FromDegreesCelsius(20d)));
            return new ModelStateBits(
                true,
                state.Phase.ToString(),
                BitConverter.DoubleToInt64Bits(state.Temperature.Kelvins),
                BitConverter.DoubleToInt64Bits(state.Pressure.Pascals),
                state.VaporQuality.HasValue,
                state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value.Fraction) : 0L);
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return new ModelStateBits(false, FluidPhase.Unspecified.ToString(), 0L, 0L, false, 0L);
        }
    }

    private static ModelStateBits ToModelCanonicalBits(
        bool resolved,
        ReferenceConsistentTabulatedInverseResolver.ResolvedState state)
        => resolved
            ? new ModelStateBits(
                true,
                state.Phase.ToString(),
                BitConverter.DoubleToInt64Bits(Temperature.FromDegreesCelsius(state.TemperatureCelsius).Kelvins),
                BitConverter.DoubleToInt64Bits(Pressure.FromMegapascals(state.PressureMegapascals).Pascals),
                state.VaporQuality.HasValue,
                state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value) : 0L)
            : new ModelStateBits(false, FluidPhase.Unspecified.ToString(), 0L, 0L, false, 0L);

    private static StateBits ResolveModel(
        SimplifiedWaterSteamThermodynamicModel model,
        string id,
        double specificVolume,
        double specificEnergy)
    {
        try
        {
            var definition = new FluidNodeDefinition(id, Volume.FromCubicMetres(specificVolume));
            var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(specificEnergy));
            var state = model.Resolve(
                definition,
                inventory,
                new FluidThermodynamicState(Pressure.StandardAtmosphere, Temperature.FromDegreesCelsius(20d)));
            return new StateBits(
                true,
                state.Phase.ToString(),
                BitConverter.DoubleToInt64Bits(state.Temperature.DegreesCelsius),
                BitConverter.DoubleToInt64Bits(state.Pressure.Megapascals),
                state.VaporQuality.HasValue,
                state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value.Fraction) : 0L);
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return new StateBits(false, FluidPhase.Unspecified.ToString(), 0L, 0L, false, 0L);
        }
    }

    private static StateBits ToBits(
        bool resolved,
        ReferenceConsistentTabulatedInverseResolver.ResolvedState state)
        => resolved
            ? new StateBits(
                true,
                state.Phase.ToString(),
                BitConverter.DoubleToInt64Bits(state.TemperatureCelsius),
                BitConverter.DoubleToInt64Bits(state.PressureMegapascals),
                state.VaporQuality.HasValue,
                state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value) : 0L)
            : new StateBits(false, FluidPhase.Unspecified.ToString(), 0L, 0L, false, 0L);

    private static StateBits ToBits(bool resolved, Rp1bShadowState state)
        => resolved
            ? new StateBits(
                true,
                state.Phase,
                BitConverter.DoubleToInt64Bits(state.TemperatureCelsius),
                BitConverter.DoubleToInt64Bits(state.PressureMegapascals),
                state.VaporQuality.HasValue,
                state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value) : 0L)
            : new StateBits(false, FluidPhase.Unspecified.ToString(), 0L, 0L, false, 0L);

    private static bool StateBitsEqual(StateBits left, StateBits right)
        => left.Resolved == right.Resolved
            && string.Equals(left.Phase, right.Phase, StringComparison.Ordinal)
            && left.TemperatureBits == right.TemperatureBits
            && left.PressureBits == right.PressureBits
            && left.HasQuality == right.HasQuality
            && left.QualityBits == right.QualityBits;

    private static void WriteEquivalenceSummary(
        string directory,
        int stateComparisons,
        int hydraulicComparisons,
        int stateMismatches,
        int hydraulicMismatches,
        int repeatMismatches,
        int resolveMismatches,
        int pathMismatches,
        int modelIntegrationMismatches,
        long resolveAllocationBytes)
    {
        File.WriteAllLines(
            Path.Combine(directory, "04-c4-production-equivalence-summary.txt"),
            new[]
            {
                "status=PASS-C4-PRODUCTION-EQUIVALENCE",
                "state-comparisons=" + stateComparisons.ToString(CultureInfo.InvariantCulture),
                "hydraulic-comparisons=" + hydraulicComparisons.ToString(CultureInfo.InvariantCulture),
                "total-comparisons=" + (stateComparisons + hydraulicComparisons).ToString(CultureInfo.InvariantCulture),
                "state-bit-mismatches=" + stateMismatches.ToString(CultureInfo.InvariantCulture),
                "hydraulic-bit-mismatches=" + hydraulicMismatches.ToString(CultureInfo.InvariantCulture),
                "repeat-mismatches=" + repeatMismatches.ToString(CultureInfo.InvariantCulture),
                "resolve-mismatches=" + resolveMismatches.ToString(CultureInfo.InvariantCulture),
                "resolution-path-mismatches=" + pathMismatches.ToString(CultureInfo.InvariantCulture),
                "model-integration-mismatches=" + modelIntegrationMismatches.ToString(CultureInfo.InvariantCulture),
                "resolve-allocation-bytes=" + resolveAllocationBytes.ToString(CultureInfo.InvariantCulture),
                "payload-runtime-io-count=" + ReferenceConsistentTabulatedInverseResolver.ResolveTimeResourceIoCount.ToString(CultureInfo.InvariantCulture),
                "payload-decode-count=" + ReferenceConsistentTabulatedInverseResolver.ResolveTimePayloadDecodeCount.ToString(CultureInfo.InvariantCulture),
            },
            Utf8WithoutBom);
    }

    private static void WriteHistoricalSummary(string directory, HistoricalRegression historical)
    {
        File.WriteAllLines(
            Path.Combine(directory, "05-historical-mode-regression-summary.txt"),
            new[]
            {
                "status=PASS-HISTORICAL-MODE-REGRESSION-FOCUSED",
                "historical-mode0-comparisons=" + historical.Comparisons.ToString(CultureInfo.InvariantCulture),
                "historical-mode0-default-mismatches=" + historical.DefaultVsMode0Mismatches.ToString(CultureInfo.InvariantCulture),
                "historical-mode1-comparisons=" + historical.Comparisons.ToString(CultureInfo.InvariantCulture),
                "historical-mode1-density-derived-input-observation-mismatches=" + historical.Mode1DensityDerivedInputObservationMismatches.ToString(CultureInfo.InvariantCulture),
                "historical-mode1-density-derived-input-observation-only=True",
                "historical-mode1-regression-basis=LEGACY-SOURCE-PROJECTION-SHA256+ORDINARY-RELEASE-SUITE",
                "historical-mode1-baseline-production-model-sha256=" + HistoricalMode1BaselineProductionModelSha256,
                "historical-mode1-legacy-source-projection-sha256=" + historical.Mode1LegacyProjectionSha256,
                "historical-mode1-legacy-source-projection-match=" + historical.Mode1LegacyProjectionMatchesBaseline.ToString(CultureInfo.InvariantCulture),
                "ordinary-release-suite=PENDING-RUNNER",
            },
            Utf8WithoutBom);
    }

    private static FrozenStateRow[] LoadFrozenStateRows(string path)
        => File.ReadAllLines(path, Encoding.UTF8)
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseFrozenC4StateRow)
            .ToArray();

    private static bool HasFrozenCheckValve(string pathId)
        => pathId switch
        {
            "MCP" => false,
            "CHANNEL" => false,
            "RETURN" => false,
            "FEEDWATER-PUMP" => true,
            _ => throw new InvalidOperationException($"Unexpected frozen hydraulic path: {pathId}"),
        };

    private static double SolveQuadraticFlow(double drivingPressurePascals, double resistance, bool hasCheckValve)
    {
        if (!double.IsFinite(drivingPressurePascals) || !double.IsFinite(resistance) || resistance <= 0d) return double.NaN;
        if (Math.Abs(drivingPressurePascals) <= 1e-15d) return 0d;
        var magnitude = Math.Sqrt(Math.Abs(drivingPressurePascals) / resistance);
        var signed = drivingPressurePascals > 0d ? magnitude : -magnitude;
        return hasCheckValve && signed < 0d ? 0d : signed;
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static double D(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static double DN(string value) => string.IsNullOrWhiteSpace(value) ? double.NaN : D(value);
    private static string NodeIdentity(string probeId, long logicalStep, string nodeId)
        => probeId + "|" + logicalStep.ToString(CultureInfo.InvariantCulture) + "|" + nodeId;
    private static string SeamIdentity(int boundaryIndex, string probeSide)
        => boundaryIndex.ToString(CultureInfo.InvariantCulture) + "|" + probeSide;
    private static string HydraulicIdentity(string probeId, long logicalStep, string pathId)
        => probeId + "|" + logicalStep.ToString(CultureInfo.InvariantCulture) + "|" + pathId;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate NuclearReactorSimulator.sln.");
    }

    private readonly record struct StateBits(
        bool Resolved,
        string Phase,
        long TemperatureBits,
        long PressureBits,
        bool HasQuality,
        long QualityBits);

    private readonly record struct ModelStateBits(
        bool Resolved,
        string Phase,
        long TemperatureKelvinsBits,
        long PressurePascalsBits,
        bool HasQuality,
        long QualityBits);

    private readonly record struct FrozenStateRow(
        string Domain,
        int ObservationIndex,
        string SourceIdentity,
        StateBits Bits);

    private sealed record StateObservation(
        string Domain,
        int ObservationIndex,
        string SourceIdentity,
        double SpecificVolume,
        double SpecificEnergy,
        StateBits Expected,
        NodeKey? NodeKey);

    private readonly record struct NodeKey(string ProbeId, long LogicalStep, string NodeId);
    private readonly record struct HydraulicBits(
        bool Resolved,
        long DrivingPressureBits,
        long FlowBits,
        bool SignChangedFromProduction);
    private readonly record struct HistoricalRegression(
        int Comparisons,
        int DefaultVsMode0Mismatches,
        int Mode1DensityDerivedInputObservationMismatches,
        string Mode1LegacyProjectionSha256,
        bool Mode1LegacyProjectionMatchesBaseline);
}
