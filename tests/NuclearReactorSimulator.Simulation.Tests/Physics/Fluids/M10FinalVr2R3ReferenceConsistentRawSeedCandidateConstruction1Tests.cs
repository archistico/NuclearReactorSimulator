using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// Test-only construction of reference-consistent conserved inventories for the frozen exact-v9 operating-point
/// thermodynamic target vector. This gate performs no runtime preconditioning or dynamic simulation and does not
/// modify production source, the C4 payload, canonical exact-v9, or any acceptance threshold.
/// </summary>
public sealed class M10FinalVr2R3ReferenceConsistentRawSeedCandidateConstruction1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_RAW_SEED_CANDIDATE_CONSTRUCTION1";
    private const string ContractRelativePath = "eng/m10-final-vr2-r3-reference-consistent-raw-seed-candidate-construction1-contract.json";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3ReferenceConsistentRawSeedCandidateConstruction1")]
    public void FrozenExactV9TargetVector_HasDeterministicMode2ConservedInventoryCandidates()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var root = FindRepositoryRoot();
        var contract = LoadContract(root);
        var payload = LoadPayload();
        var resolver = new ReferenceConsistentTabulatedInverseResolver();

        var candidateRows = new List<CandidateRow>(contract.Targets.Count);
        var roundtripRows = new List<RoundtripRow>(contract.Targets.Count);

        foreach (var target in contract.Targets.OrderBy(static target => target.NodeId, StringComparer.Ordinal))
        {
            var starts = BuildCandidateStarts(target, payload);
            var evaluations = starts
                .Select(start => EvaluateCandidate(target, start, resolver))
                .Where(static evaluation => evaluation.Resolved)
                .OrderBy(static evaluation => evaluation.Score)
                .ThenBy(static evaluation => evaluation.Method, StringComparer.Ordinal)
                .ToArray();

            Assert.NotEmpty(evaluations);
            var best = evaluations[0];

            var massKilograms = target.VolumeCubicMetres / best.SpecificVolume;
            var internalEnergyJoules = massKilograms * best.SpecificEnergy;

            candidateRows.Add(new CandidateRow(
                target.NodeId,
                best.Method,
                target.VolumeCubicMetres,
                best.SpecificVolume,
                best.SpecificEnergy,
                massKilograms,
                internalEnergyJoules,
                best.Path.ToString()));

            roundtripRows.Add(new RoundtripRow(
                target.NodeId,
                target.TargetPressurePascals,
                best.State.PressureMegapascals * 1_000_000d,
                (best.State.PressureMegapascals * 1_000_000d) - target.TargetPressurePascals,
                target.TargetTemperatureKelvins,
                best.State.TemperatureCelsius + 273.15d,
                (best.State.TemperatureCelsius + 273.15d) - target.TargetTemperatureKelvins,
                target.TargetPhase,
                best.State.Phase.ToString(),
                string.Equals(target.TargetPhase, best.State.Phase.ToString(), StringComparison.Ordinal),
                target.TargetQuality,
                best.State.VaporQuality,
                QualityResidual(target.TargetQuality, best.State.VaporQuality),
                best.Score,
                best.Path.ToString()));
        }

        Assert.Equal(12, candidateRows.Count);
        Assert.Equal(12, roundtripRows.Count);

        var heads = BuildHydraulicHeadRows(contract.Targets, roundtripRows);
        Assert.Equal(8, heads.Count);

        WriteCandidateVector(candidateRows);
        WriteRoundtrip(roundtripRows);
        WriteHydraulicHeads(heads);
        WriteSummary(candidateRows, roundtripRows, heads);
    }

    private static ContractData LoadContract(string root)
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, ContractRelativePath.Replace('/', Path.DirectorySeparatorChar))));
        var contract = document.RootElement;
        var targetNodes = contract
            .GetProperty("target_state_vector")
            .GetProperty("nodes")
            .EnumerateArray()
            .Select(node => new TargetState(
                node.GetProperty("node").GetString() ?? throw new InvalidDataException("Target node id missing."),
                node.GetProperty("target_pressure_pa").GetDouble(),
                node.GetProperty("target_temperature_k").GetDouble(),
                node.GetProperty("target_phase").GetString() ?? throw new InvalidDataException("Target phase missing."),
                node.GetProperty("target_quality").ValueKind == JsonValueKind.Null
                    ? null
                    : node.GetProperty("target_quality").GetDouble(),
                node.GetProperty("legacy_raw_mass_kg").GetDouble(),
                node.GetProperty("legacy_raw_internal_energy_j").GetDouble(),
                contract
                    .GetProperty("candidate_construction")
                    .GetProperty("node_volumes_cubic_metres")
                    .GetProperty(node.GetProperty("node").GetString()!)
                    .GetDouble()))
            .ToArray();

        return new ContractData(targetNodes);
    }

    private static Payload LoadPayload()
    {
        using var stream = typeof(ReferenceConsistentTabulatedInverseResolver).Assembly
            .GetManifestResourceStream(ReferenceConsistentTabulatedInverseResolver.PayloadLogicalName)
            ?? throw new InvalidDataException("C4 embedded payload was not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        Assert.Equal(ReferenceConsistentTabulatedInverseResolver.ExpectedPayloadSha256, sha);

        using var reader = new BinaryReader(new MemoryStream(bytes, writable: false), Encoding.ASCII, leaveOpen: false);
        Assert.Equal("NRSVR2C4", Encoding.ASCII.GetString(reader.ReadBytes(8)));
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(0x01020304, reader.ReadInt32());

        var denseCount = reader.ReadInt32();
        var prefixCount = reader.ReadInt32();
        var liquidCount = reader.ReadInt32();
        var vaporCount = reader.ReadInt32();
        Assert.Equal(17_502, denseCount);
        Assert.Equal(701, prefixCount);
        Assert.Equal(351, liquidCount);
        Assert.Equal(401, vaporCount);

        var dense = ReadSaturationNodes(reader, denseCount);
        var prefix = ReadSaturationNodes(reader, prefixCount);
        var liquid = ReadRows(reader, liquidCount);
        _ = ReadRows(reader, vaporCount);

        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
        return new Payload(dense, prefix, liquid);
    }

    private static SaturationNode[] ReadSaturationNodes(BinaryReader reader, int count)
    {
        var result = new SaturationNode[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = new SaturationNode(
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble());
        }
        return result;
    }

    private static TemperatureRow[] ReadRows(BinaryReader reader, int count)
    {
        var result = new TemperatureRow[count];
        for (var rowIndex = 0; rowIndex < count; rowIndex++)
        {
            var temperatureKelvins = reader.ReadDouble();
            var pointCount = reader.ReadInt32();
            var points = new TablePoint[pointCount];
            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                points[pointIndex] = new TablePoint(
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble());
            }
            result[rowIndex] = new TemperatureRow(temperatureKelvins, points);
        }
        return result;
    }

    private static IReadOnlyList<CandidateStart> BuildCandidateStarts(TargetState target, Payload payload)
    {
        var starts = new List<CandidateStart>();

        var legacySpecificVolume = target.VolumeCubicMetres / target.LegacyRawMassKilograms;
        var legacySpecificEnergy = target.LegacyRawInternalEnergyJoules / target.LegacyRawMassKilograms;
        starts.Add(new CandidateStart("legacy-raw", legacySpecificVolume, legacySpecificEnergy));

        if (string.Equals(target.TargetPhase, nameof(FluidPhase.SaturatedMixture), StringComparison.Ordinal)
            && target.TargetQuality.HasValue)
        {
            var byPressure = InterpolateSaturationByPressure(payload.PrefixSaturation, target.TargetPressurePascals / 1_000_000d);
            starts.Add(MixtureStart("c4-prefix-by-target-pressure", byPressure, target.TargetQuality.Value));

            var byTemperature = InterpolateSaturationByTemperature(payload.PrefixSaturation, target.TargetTemperatureKelvins);
            starts.Add(MixtureStart("c4-prefix-by-target-temperature", byTemperature, target.TargetQuality.Value));
        }
        else if (string.Equals(target.TargetPhase, nameof(FluidPhase.SubcooledLiquid), StringComparison.Ordinal))
        {
            if (TryBuildLiquidTableStart(
                    payload.LiquidRows,
                    target.TargetTemperatureKelvins,
                    target.TargetPressurePascals / 1_000_000d,
                    out var tableStart))
            {
                starts.Add(tableStart with { Method = "c4-liquid-table-target-tp" });
            }

            var boundary = InterpolateSaturationByTemperature(payload.PrefixSaturation, target.TargetTemperatureKelvins);
            var compression = ((target.TargetPressurePascals / 1_000_000d) - boundary.PressureMegapascals)
                * 1_000_000d
                / boundary.NearBoundaryLiquidBulkModulusPascals;
            compression = Math.Max(compression, 2.5e-10d);
            starts.Add(new CandidateStart(
                "c4-near-boundary-liquid-target-tp",
                boundary.LiquidSpecificVolume / (1d + compression),
                boundary.LiquidSpecificEnergy));
        }

        return starts
            .Where(static start =>
                double.IsFinite(start.SpecificVolume)
                && start.SpecificVolume > 0d
                && double.IsFinite(start.SpecificEnergy))
            .Distinct()
            .ToArray();
    }

    private static CandidateStart MixtureStart(string method, SaturationNode node, double quality)
        => new(
            method,
            node.LiquidSpecificVolume
                + (quality * (node.VaporSpecificVolume - node.LiquidSpecificVolume)),
            node.LiquidSpecificEnergy
                + (quality * (node.VaporSpecificEnergy - node.LiquidSpecificEnergy)));

    private static CandidateEvaluation EvaluateCandidate(
        TargetState target,
        CandidateStart start,
        ReferenceConsistentTabulatedInverseResolver resolver)
    {
        if (!resolver.TryResolveWithPath(
                start.SpecificVolume,
                start.SpecificEnergy,
                out var state,
                out var path))
        {
            return CandidateEvaluation.Unresolved(start);
        }

        var resolvedPressurePascals = state.PressureMegapascals * 1_000_000d;
        var resolvedTemperatureKelvins = state.TemperatureCelsius + 273.15d;
        var pressureResidual = resolvedPressurePascals - target.TargetPressurePascals;
        var temperatureResidual = resolvedTemperatureKelvins - target.TargetTemperatureKelvins;
        var qualityResidual = QualityResidual(target.TargetQuality, state.VaporQuality);
        var phasePenalty = string.Equals(target.TargetPhase, state.Phase.ToString(), StringComparison.Ordinal)
            ? 0d
            : 100d;

        var score = phasePenalty
            + (Math.Abs(pressureResidual) / Math.Max(1d, Math.Abs(target.TargetPressurePascals)))
            + (Math.Abs(temperatureResidual) / Math.Max(1d, Math.Abs(target.TargetTemperatureKelvins)))
            + (qualityResidual.HasValue ? Math.Abs(qualityResidual.Value) : 0d);

        return new CandidateEvaluation(
            start.Method,
            start.SpecificVolume,
            start.SpecificEnergy,
            true,
            state,
            path,
            score);
    }

    private static double? QualityResidual(double? target, double? resolved)
        => target.HasValue && resolved.HasValue
            ? resolved.Value - target.Value
            : target.HasValue == resolved.HasValue
                ? 0d
                : null;

    private static SaturationNode InterpolateSaturationByTemperature(
        IReadOnlyList<SaturationNode> nodes,
        double temperatureKelvins)
    {
        if (temperatureKelvins <= nodes[0].TemperatureKelvins)
        {
            return nodes[0];
        }
        if (temperatureKelvins >= nodes[^1].TemperatureKelvins)
        {
            return nodes[^1];
        }

        var upper = 1;
        while (upper < nodes.Count && nodes[upper].TemperatureKelvins < temperatureKelvins)
        {
            upper++;
        }

        var left = nodes[upper - 1];
        var right = nodes[upper];
        var fraction = (temperatureKelvins - left.TemperatureKelvins)
            / (right.TemperatureKelvins - left.TemperatureKelvins);
        return Interpolate(left, right, fraction);
    }

    private static SaturationNode InterpolateSaturationByPressure(
        IReadOnlyList<SaturationNode> nodes,
        double pressureMegapascals)
    {
        if (pressureMegapascals <= nodes[0].PressureMegapascals)
        {
            return nodes[0];
        }
        if (pressureMegapascals >= nodes[^1].PressureMegapascals)
        {
            return nodes[^1];
        }

        var upper = 1;
        while (upper < nodes.Count && nodes[upper].PressureMegapascals < pressureMegapascals)
        {
            upper++;
        }

        var left = nodes[upper - 1];
        var right = nodes[upper];
        var fraction = (pressureMegapascals - left.PressureMegapascals)
            / (right.PressureMegapascals - left.PressureMegapascals);
        return Interpolate(left, right, fraction);
    }

    private static SaturationNode Interpolate(SaturationNode left, SaturationNode right, double fraction)
        => new(
            Lerp(left.TemperatureKelvins, right.TemperatureKelvins, fraction),
            Lerp(left.PressureMegapascals, right.PressureMegapascals, fraction),
            Lerp(left.LiquidSpecificVolume, right.LiquidSpecificVolume, fraction),
            Lerp(left.LiquidSpecificEnergy, right.LiquidSpecificEnergy, fraction),
            Lerp(left.VaporSpecificVolume, right.VaporSpecificVolume, fraction),
            Lerp(left.VaporSpecificEnergy, right.VaporSpecificEnergy, fraction),
            Lerp(left.NearBoundaryLiquidBulkModulusPascals, right.NearBoundaryLiquidBulkModulusPascals, fraction),
            Lerp(left.NearBoundaryVaporPressurePerSpecificVolumeSlope, right.NearBoundaryVaporPressurePerSpecificVolumeSlope, fraction));

    private static double Lerp(double left, double right, double fraction)
        => left + (fraction * (right - left));

    private static bool TryBuildLiquidTableStart(
        IReadOnlyList<TemperatureRow> rows,
        double targetTemperatureKelvins,
        double targetPressureMegapascals,
        out CandidateStart start)
    {
        var upper = 0;
        while (upper < rows.Count && rows[upper].TemperatureKelvins < targetTemperatureKelvins)
        {
            upper++;
        }

        TemperatureRow lowerRow;
        TemperatureRow upperRow;
        double temperatureFraction;
        if (upper <= 0)
        {
            lowerRow = upperRow = rows[0];
            temperatureFraction = 0d;
        }
        else if (upper >= rows.Count)
        {
            lowerRow = upperRow = rows[^1];
            temperatureFraction = 0d;
        }
        else if (rows[upper].TemperatureKelvins == targetTemperatureKelvins)
        {
            lowerRow = upperRow = rows[upper];
            temperatureFraction = 0d;
        }
        else
        {
            lowerRow = rows[upper - 1];
            upperRow = rows[upper];
            temperatureFraction = (targetTemperatureKelvins - lowerRow.TemperatureKelvins)
                / (upperRow.TemperatureKelvins - lowerRow.TemperatureKelvins);
        }

        var minimumVolume = Math.Max(lowerRow.Points[^1].SpecificVolume, upperRow.Points[^1].SpecificVolume);
        var maximumVolume = Math.Min(lowerRow.Points[0].SpecificVolume, upperRow.Points[0].SpecificVolume);
        if (!(minimumVolume < maximumVolume))
        {
            start = default;
            return false;
        }

        if (!TryPressureAndEnergy(lowerRow, upperRow, temperatureFraction, minimumVolume, out var lowPressure, out _)
            || !TryPressureAndEnergy(lowerRow, upperRow, temperatureFraction, maximumVolume, out var highPressure, out _))
        {
            start = default;
            return false;
        }

        var minimumPressure = Math.Min(lowPressure, highPressure);
        var maximumPressure = Math.Max(lowPressure, highPressure);
        if (targetPressureMegapascals < minimumPressure || targetPressureMegapascals > maximumPressure)
        {
            start = default;
            return false;
        }

        var lowVolume = minimumVolume;
        var highVolume = maximumVolume;
        for (var iteration = 0; iteration < 80; iteration++)
        {
            var middleVolume = 0.5d * (lowVolume + highVolume);
            if (!TryPressureAndEnergy(
                    lowerRow,
                    upperRow,
                    temperatureFraction,
                    middleVolume,
                    out var middlePressure,
                    out _))
            {
                start = default;
                return false;
            }

            // Liquid-table pressure decreases monotonically as specific volume increases.
            if (middlePressure > targetPressureMegapascals)
            {
                lowVolume = middleVolume;
            }
            else
            {
                highVolume = middleVolume;
            }
        }

        var specificVolume = 0.5d * (lowVolume + highVolume);
        if (!TryPressureAndEnergy(
                lowerRow,
                upperRow,
                temperatureFraction,
                specificVolume,
                out _,
                out var specificEnergy))
        {
            start = default;
            return false;
        }

        start = new CandidateStart("c4-liquid-table-target-tp", specificVolume, specificEnergy);
        return true;
    }

    private static bool TryPressureAndEnergy(
        TemperatureRow lowerRow,
        TemperatureRow upperRow,
        double temperatureFraction,
        double specificVolume,
        out double pressureMegapascals,
        out double specificEnergy)
    {
        if (!TryInterpolateRow(lowerRow, specificVolume, out var lowerPressure, out var lowerEnergy)
            || !TryInterpolateRow(upperRow, specificVolume, out var upperPressure, out var upperEnergy))
        {
            pressureMegapascals = default;
            specificEnergy = default;
            return false;
        }

        pressureMegapascals = Lerp(lowerPressure, upperPressure, temperatureFraction);
        specificEnergy = Lerp(lowerEnergy, upperEnergy, temperatureFraction);
        return true;
    }

    private static bool TryInterpolateRow(
        TemperatureRow row,
        double specificVolume,
        out double pressureMegapascals,
        out double specificEnergy)
    {
        for (var index = 0; index < row.Points.Length - 1; index++)
        {
            var left = row.Points[index];
            var right = row.Points[index + 1];
            if (specificVolume > left.SpecificVolume || specificVolume < right.SpecificVolume)
            {
                continue;
            }

            var denominator = right.SpecificVolume - left.SpecificVolume;
            var fraction = Math.Abs(denominator) <= 1e-30d
                ? 0d
                : Math.Clamp((specificVolume - left.SpecificVolume) / denominator, 0d, 1d);
            pressureMegapascals = Lerp(left.PressureMegapascals, right.PressureMegapascals, fraction);
            specificEnergy = Lerp(left.SpecificEnergy, right.SpecificEnergy, fraction);
            return true;
        }

        pressureMegapascals = default;
        specificEnergy = default;
        return false;
    }

    private static IReadOnlyList<HydraulicHeadRow> BuildHydraulicHeadRows(
        IReadOnlyList<TargetState> targets,
        IReadOnlyList<RoundtripRow> resolved)
    {
        var targetById = targets.ToDictionary(static target => target.NodeId, StringComparer.Ordinal);
        var resolvedById = resolved.ToDictionary(static row => row.NodeId, StringComparer.Ordinal);
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
            var targetHead = targetById[spec.From].TargetPressurePascals
                + spec.BoostPascals
                - targetById[spec.To].TargetPressurePascals;
            var candidateHead = resolvedById[spec.From].ResolvedPressurePascals
                + spec.BoostPascals
                - resolvedById[spec.To].ResolvedPressurePascals;
            return new HydraulicHeadRow(
                spec.Id,
                spec.From,
                spec.To,
                targetHead,
                candidateHead,
                candidateHead - targetHead);
        }).ToArray();
    }

    private static void WriteCandidateVector(IReadOnlyList<CandidateRow> rows)
    {
        var lines = new List<string>
        {
            "node,construction_method,node_volume_m3,specific_volume_m3_kg,specific_energy_j_kg,mass_kg,internal_energy_j,resolution_path"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.NodeId,
            row.ConstructionMethod,
            F(row.NodeVolumeCubicMetres),
            F(row.SpecificVolume),
            F(row.SpecificEnergy),
            F(row.MassKilograms),
            F(row.InternalEnergyJoules),
            row.ResolutionPath)));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "01-candidate-conserved-inventory-vector.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteRoundtrip(IReadOnlyList<RoundtripRow> rows)
    {
        var lines = new List<string>
        {
            "node,target_pressure_pa,resolved_pressure_pa,pressure_residual_pa,target_temperature_k,resolved_temperature_k,temperature_residual_k,target_phase,resolved_phase,phase_match,target_quality,resolved_quality,quality_residual,selection_score,resolution_path"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.NodeId,
            F(row.TargetPressurePascals),
            F(row.ResolvedPressurePascals),
            F(row.PressureResidualPascals),
            F(row.TargetTemperatureKelvins),
            F(row.ResolvedTemperatureKelvins),
            F(row.TemperatureResidualKelvins),
            row.TargetPhase,
            row.ResolvedPhase,
            row.PhaseMatch.ToString().ToLowerInvariant(),
            Optional(row.TargetQuality),
            Optional(row.ResolvedQuality),
            Optional(row.QualityResidual),
            F(row.SelectionScore),
            row.ResolutionPath)));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "02-candidate-target-roundtrip.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteHydraulicHeads(IReadOnlyList<HydraulicHeadRow> rows)
    {
        var lines = new List<string>
        {
            "path,from,to,target_head_pa,candidate_head_pa,head_residual_pa"
        };
        lines.AddRange(rows.Select(row => string.Join(",",
            row.PathId,
            row.From,
            row.To,
            F(row.TargetHeadPascals),
            F(row.CandidateHeadPascals),
            F(row.HeadResidualPascals))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(), "03-candidate-hydraulic-head-comparison.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(
        IReadOnlyList<CandidateRow> candidates,
        IReadOnlyList<RoundtripRow> roundtrip,
        IReadOnlyList<HydraulicHeadRow> heads)
    {
        var phaseMatches = roundtrip.Count(static row => row.PhaseMatch);
        var maxPressure = roundtrip.Max(static row => Math.Abs(row.PressureResidualPascals));
        var maxTemperature = roundtrip.Max(static row => Math.Abs(row.TemperatureResidualKelvins));
        var maxQuality = roundtrip
            .Where(static row => row.QualityResidual.HasValue)
            .Select(static row => Math.Abs(row.QualityResidual!.Value))
            .DefaultIfEmpty(0d)
            .Max();
        var maxHead = heads.Max(static row => Math.Abs(row.HeadResidualPascals));

        File.WriteAllLines(
            Path.Combine(ArtifactDirectory(), "04-candidate-construction-summary.txt"),
            new[]
            {
                "status=CANDIDATE-CONSTRUCTION-EVIDENCE-WRITTEN",
                $"candidate-node-count={candidates.Count}",
                $"resolved-node-count={roundtrip.Count}",
                $"phase-match-node-count={phaseMatches}",
                FormattableString.Invariant($"max-abs-pressure-residual-pa={maxPressure:R}"),
                FormattableString.Invariant($"max-abs-temperature-residual-k={maxTemperature:R}"),
                FormattableString.Invariant($"max-abs-quality-residual={maxQuality:R}"),
                FormattableString.Invariant($"max-abs-hydraulic-head-residual-pa={maxHead:R}"),
                "runtime-preconditioning-steps=0",
                "runtime-dynamic-steps=0",
                "production-source-changed=False",
                "c4-resolver-changed=False",
                "c4-payload-changed=False",
                "canonical-exact-v9-changed=False",
                "new-exact-version-created=False",
                "threshold-change-applied=False",
                "r3-remains-red=True",
                "r4-planning-authorized=False",
            },
            Utf8WithoutBom);
    }

    private static string Optional(double? value)
        => value.HasValue ? F(value.Value) : "null";

    private static string F(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);

    private static void RequireOptIn()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(OptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set {OptInEnvironmentVariable}=1 only from the controlled Candidate Construction 1 runner.");
        }
    }

    private static string ArtifactDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "m10-final-physical-reference-vr2-r3-reference-consistent-raw-seed-candidate-construction1");

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

    private sealed record ContractData(IReadOnlyList<TargetState> Targets);

    private sealed record TargetState(
        string NodeId,
        double TargetPressurePascals,
        double TargetTemperatureKelvins,
        string TargetPhase,
        double? TargetQuality,
        double LegacyRawMassKilograms,
        double LegacyRawInternalEnergyJoules,
        double VolumeCubicMetres);

    private readonly record struct CandidateStart(
        string Method,
        double SpecificVolume,
        double SpecificEnergy);

    private readonly record struct CandidateEvaluation(
        string Method,
        double SpecificVolume,
        double SpecificEnergy,
        bool Resolved,
        ReferenceConsistentTabulatedInverseResolver.ResolvedState State,
        ReferenceConsistentTabulatedInverseResolver.ResolutionPath Path,
        double Score)
    {
        internal static CandidateEvaluation Unresolved(CandidateStart start)
            => new(
                start.Method,
                start.SpecificVolume,
                start.SpecificEnergy,
                false,
                default,
                ReferenceConsistentTabulatedInverseResolver.ResolutionPath.Unresolved,
                double.PositiveInfinity);
    }

    private sealed record CandidateRow(
        string NodeId,
        string ConstructionMethod,
        double NodeVolumeCubicMetres,
        double SpecificVolume,
        double SpecificEnergy,
        double MassKilograms,
        double InternalEnergyJoules,
        string ResolutionPath);

    private sealed record RoundtripRow(
        string NodeId,
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
        double? QualityResidual,
        double SelectionScore,
        string ResolutionPath);

    private sealed record HeadSpec(string Id, string From, string To, double BoostPascals);

    private sealed record HydraulicHeadRow(
        string PathId,
        string From,
        string To,
        double TargetHeadPascals,
        double CandidateHeadPascals,
        double HeadResidualPascals);

    private sealed record Payload(
        SaturationNode[] DenseSaturation,
        SaturationNode[] PrefixSaturation,
        TemperatureRow[] LiquidRows);

    private readonly record struct SaturationNode(
        double TemperatureKelvins,
        double PressureMegapascals,
        double LiquidSpecificVolume,
        double LiquidSpecificEnergy,
        double VaporSpecificVolume,
        double VaporSpecificEnergy,
        double NearBoundaryLiquidBulkModulusPascals,
        double NearBoundaryVaporPressurePerSpecificVolumeSlope);

    private readonly record struct TablePoint(
        double PressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record TemperatureRow(
        double TemperatureKelvins,
        TablePoint[] Points);
}
