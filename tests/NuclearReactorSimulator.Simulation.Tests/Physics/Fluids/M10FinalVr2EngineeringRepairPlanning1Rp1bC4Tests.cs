using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Engineering Repair Planning 1 / RP1B C4. Test-only evidence generation for the separately
/// authorized allocation-neutral C4 shadow candidate. Engineering-negative results are recorded as
/// evidence and do not fail xUnit; only harness/evidence-integrity failures fail the focused process.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4";
    private const string LaneEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE";
    private const string RunIndexEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const string FrozenRefinement2Relative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts";
    private const int BoundaryCount = 320;
    private const int WarmupPasses = 16;
    private const int MeasuredPasses = 64;
    private const int MeasuredCallsPerRun = BoundaryCount * MeasuredPasses;
    private const int HistoricalRotationStride = 37;
    private const double ResolveMedianCeilingMicroseconds = 94.8d;
    private const double ResolveP95CeilingMicroseconds = 158.80666666666667d;
    private const double ResolveMaximumCeilingMicroseconds = 409.30666666666673d;
    private const string ExpectedCandidateId = "C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE";
    private const string ExpectedFamilyId = "C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE";
    private const string FrozenC3CandidateId = "C3-VAPOR-SEAM-COMPLETE-SURROGATE";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1bC4")]
    public void Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus()
    {
        RequireOptIn();
        Assert.Null(Environment.GetEnvironmentVariable(LaneEnvironmentVariable));
        Assert.Null(Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable));

        var root = FindRepositoryRoot();
        var artifactDirectory = EnsureArtifactDirectory(root);
        var frozenRp1a = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var frozenRefinement2 = Path.Combine(root, FrozenRefinement2Relative.Replace('/', Path.DirectorySeparatorChar));

        var candidate = new Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate();
        AssertCandidateContract(candidate);

        var observations = LoadStateObservations(frozenRp1a, frozenRefinement2);
        Assert.Equal(1_679, observations.Count);

        var results = new StateSemanticResult[observations.Count];
        var exactV9Repeats = new Dictionary<NodeKey, StateRepeat>();
        for (var index = 0; index < observations.Count; index++)
        {
            var observation = observations[index];
            var first = Resolve(candidate, observation.SpecificVolume, observation.SpecificEnergy);
            var second = Resolve(candidate, observation.SpecificVolume, observation.SpecificEnergy);
            var deterministic = StateBitsEqual(first, second);
            var equivalent = StateBitsEqual(observation.Expected, first) && deterministic;
            results[index] = new StateSemanticResult(observation, first, second, deterministic, equivalent);

            if (observation.Domain == "EXACT-V9")
            {
                Assert.True(observation.NodeKey.HasValue);
                exactV9Repeats.Add(observation.NodeKey.Value, new StateRepeat(first, second));
            }
        }

        Assert.Equal(360, exactV9Repeats.Count);
        WriteStateSemanticEvidence(artifactDirectory, results);

        var hydraulicRows = LoadHydraulicRows(
            Path.Combine(frozenRp1a, "04-hydraulic-context.csv"),
            Path.Combine(frozenRefinement2, "05-candidate-hydraulic-replay.csv"));
        Assert.Equal(288, hydraulicRows.Count);

        var hydraulicResults = new HydraulicSemanticResult[hydraulicRows.Count];
        for (var index = 0; index < hydraulicRows.Count; index++)
        {
            var row = hydraulicRows[index];
            var first = EvaluateHydraulic(row, exactV9Repeats, repeatIndex: 0);
            var second = EvaluateHydraulic(row, exactV9Repeats, repeatIndex: 1);
            var deterministic = HydraulicBitsEqual(first, second);
            var equivalent = HydraulicBitsEqual(row.Expected, first) && deterministic;
            hydraulicResults[index] = new HydraulicSemanticResult(row, first, second, deterministic, equivalent);
        }

        WriteHydraulicSemanticEvidence(artifactDirectory, hydraulicResults);
        WriteSemanticSummary(artifactDirectory, results, hydraulicResults, candidate);

        Assert.Equal(1_679, results.Length);
        Assert.Equal(288, hydraulicResults.Length);
        Assert.True(File.Exists(Path.Combine(artifactDirectory, "02-state-semantic-equivalence.csv")));
        Assert.True(File.Exists(Path.Combine(artifactDirectory, "03-hydraulic-semantic-equivalence.csv")));
        Assert.True(File.Exists(Path.Combine(artifactDirectory, "04-semantic-equivalence-summary.txt")));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1bC4")]
    public void Rp1bC4_MeasuresOneIndependentR1LaneRun()
    {
        RequireOptIn();
        var lane = ParseLane(Environment.GetEnvironmentVariable(LaneEnvironmentVariable));
        var runIndex = ParseRunIndex(Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable));
        ValidateLanePermutationCoverage(lane, runIndex);

        var root = FindRepositoryRoot();
        var frozenRefinement2 = Path.Combine(root, FrozenRefinement2Relative.Replace('/', Path.DirectorySeparatorChar));
        var r1Rows = LoadR1Rows(Path.Combine(frozenRefinement2, "04-candidate-seam-map.csv"));
        Assert.Equal(BoundaryCount, r1Rows.Length);
        for (var index = 0; index < r1Rows.Length; index++)
        {
            Assert.Equal(index, r1Rows[index].BoundaryIndex);
            Assert.Equal("SubcooledLiquid", r1Rows[index].ReferencePhase);
        }

        var candidate = new Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate();
        AssertCandidateContract(candidate);

        for (var pass = 0; pass < WarmupPasses; pass++)
        {
            ResolveWarmupPass(candidate, r1Rows, lane, runIndex, pass);
        }

        PrimeTimingHarness(candidate, r1Rows[0]);
        var samples = new TimingSample[MeasuredCallsPerRun];
        ForceFullCollection();

        var regionBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var regionGen0Before = GC.CollectionCount(0);
        var regionGen1Before = GC.CollectionCount(1);
        var regionGen2Before = GC.CollectionCount(2);
        long candidateAllocatedBytes = 0;
        var sampleIndex = 0;

        for (var pass = 0; pass < MeasuredPasses; pass++)
        {
            for (var offset = 0; offset < BoundaryCount; offset++)
            {
                var rowIndex = RowIndex(lane, runIndex, pass, offset);
                var row = r1Rows[rowIndex];
                var gen0Before = GC.CollectionCount(0);
                var gen1Before = GC.CollectionCount(1);
                var gen2Before = GC.CollectionCount(2);
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp();
                var resolved = candidate.TryResolveWithPath(
                    row.SpecificVolume,
                    row.SpecificEnergy,
                    out var state,
                    out var resolutionPath);
                var end = Stopwatch.GetTimestamp();
                var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                var gen0After = GC.CollectionCount(0);
                var gen1After = GC.CollectionCount(1);
                var gen2After = GC.CollectionCount(2);

                var allocatedBytes = Math.Max(0L, allocatedAfter - allocatedBefore);
                candidateAllocatedBytes += allocatedBytes;
                samples[sampleIndex++] = new TimingSample(
                    row.BoundaryIndex,
                    row.BoundaryTemperatureCelsius,
                    pass,
                    rowIndex,
                    TicksToMicroseconds(end - start),
                    allocatedBytes,
                    resolved,
                    PhaseCode(resolved, state),
                    resolutionPath,
                    gen0After - gen0Before,
                    gen1After - gen1Before,
                    gen2After - gen2Before);
            }
        }

        var regionGen0After = GC.CollectionCount(0);
        var regionGen1After = GC.CollectionCount(1);
        var regionGen2After = GC.CollectionCount(2);
        var regionBytesAfter = GC.GetAllocatedBytesForCurrentThread();
        var wholeRegionAllocatedBytes = Math.Max(0L, regionBytesAfter - regionBytesBefore);
        var harnessAllocatedBytes = Math.Max(0L, wholeRegionAllocatedBytes - candidateAllocatedBytes);

        Assert.Equal(MeasuredCallsPerRun, sampleIndex);
        for (var index = 0; index < samples.Length; index++)
        {
            Assert.True(double.IsFinite(samples[index].ElapsedMicroseconds));
            Assert.True(samples[index].ElapsedMicroseconds >= 0d);
        }

        var artifactDirectory = EnsureArtifactDirectory(root);
        var processDirectory = ResetOwnedProcessDirectory(artifactDirectory, lane, runIndex);
        WriteProcessContract(processDirectory, lane, runIndex, candidate);
        WriteCallTiming(processDirectory, lane, runIndex, samples);
        var boundarySummaries = BuildBoundarySummaries(lane, runIndex, r1Rows, samples);
        WriteBoundarySummaries(processDirectory, boundarySummaries);
        WriteRuntimeContext(
            processDirectory,
            lane,
            runIndex,
            wholeRegionAllocatedBytes,
            candidateAllocatedBytes,
            harnessAllocatedBytes,
            regionGen0After - regionGen0Before,
            regionGen1After - regionGen1Before,
            regionGen2After - regionGen2Before);
        WriteProcessSummary(
            processDirectory,
            lane,
            runIndex,
            samples,
            candidateAllocatedBytes,
            harnessAllocatedBytes,
            boundarySummaries);

        Assert.Equal(BoundaryCount, boundarySummaries.Length);
        for (var index = 0; index < boundarySummaries.Length; index++)
        {
            Assert.Equal(64, boundarySummaries[index].SampleCount);
        }
    }

    private static void AssertCandidateContract(Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate)
    {
        Assert.Equal(ExpectedCandidateId, candidate.CandidateId);
        Assert.Equal(ExpectedFamilyId, candidate.FamilyId);
        Assert.Equal(48, candidate.MaximumIterativeSolveIterations);
        Assert.False(candidate.UsesDirectIf97AtResolveTime);
        Assert.True(candidate.InitializationReferencePointCount > 0);
    }

    private static List<StateObservation> LoadStateObservations(string frozenRp1a, string frozenRefinement2)
    {
        var expectedVr2 = LoadFrozenStateMap(
            Path.Combine(frozenRefinement2, "02-candidate-vr2-error-map.csv"),
            "candidate_id,point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,inverse_applicable,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,pressure_relative_error,temperature_relative_kelvin_error",
            static parts => parts[1],
            resolvedIndex: 9,
            regionIndex: 10,
            phaseIndex: 11,
            temperatureIndex: 12,
            pressureIndex: 13,
            qualityIndex: 14,
            include: static parts => bool.Parse(parts[8]));
        var expectedNodes = LoadFrozenStateMap(
            Path.Combine(frozenRefinement2, "03-candidate-exact-v9-node-map.csv"),
            "candidate_id,probe_id,logical_step,elapsed_s,node_id,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,pressure_relative_error,temperature_relative_kelvin_error",
            static parts => NodeIdentity(parts[1], long.Parse(parts[2], CultureInfo.InvariantCulture), parts[4]),
            resolvedIndex: 10,
            regionIndex: 11,
            phaseIndex: 12,
            temperatureIndex: 13,
            pressureIndex: 14,
            qualityIndex: 15,
            include: null);
        var expectedSeams = LoadFrozenStateMap(
            Path.Combine(frozenRefinement2, "04-candidate-seam-map.csv"),
            "candidate_id,boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,candidate_minus_reference_temperature_c,candidate_minus_reference_pressure_mpa",
            static parts => SeamIdentity(int.Parse(parts[1], CultureInfo.InvariantCulture), parts[3]),
            resolvedIndex: 9,
            regionIndex: 10,
            phaseIndex: 11,
            temperatureIndex: 12,
            pressureIndex: 13,
            qualityIndex: 14,
            include: null);

        var observations = new List<StateObservation>(1_679);
        const string vr2Header = "point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa";
        foreach (var line in ReadCsvLines(Path.Combine(frozenRp1a, "02-vr2-reference-point-corpus.csv"), vr2Header))
        {
            var parts = line.Split(',');
            Assert.Equal(13, parts.Length);
            var volume = DN(parts[6]);
            var energy = DN(parts[7]);
            if (!double.IsFinite(volume) || volume <= 0d || !double.IsFinite(energy))
            {
                continue;
            }

            Assert.True(expectedVr2.TryGetValue(parts[0], out var expected));
            observations.Add(new StateObservation("VR2", observations.Count, parts[0], volume, energy, expected, null));
        }

        const string nodeHeader = "probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa";
        foreach (var line in ReadCsvLines(Path.Combine(frozenRp1a, "03-exact-v9-node-corpus.csv"), nodeHeader))
        {
            var parts = line.Split(',');
            Assert.Equal(17, parts.Length);
            var logicalStep = long.Parse(parts[1], CultureInfo.InvariantCulture);
            var density = D(parts[6]);
            Assert.True(density > 0d);
            var key = new NodeKey(parts[0], logicalStep, parts[3]);
            var identity = NodeIdentity(key.ProbeId, key.LogicalStep, key.NodeId);
            Assert.True(expectedNodes.TryGetValue(identity, out var expected));
            observations.Add(new StateObservation("EXACT-V9", observations.Count, identity, 1d / density, D(parts[7]), expected, key));
        }

        const string seamHeader = "boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa";
        foreach (var line in ReadCsvLines(Path.Combine(frozenRp1a, "05-seam-probe-map.csv"), seamHeader))
        {
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            var identity = SeamIdentity(int.Parse(parts[0], CultureInfo.InvariantCulture), parts[2]);
            Assert.True(expectedSeams.TryGetValue(identity, out var expected));
            observations.Add(new StateObservation("SEAM", observations.Count, identity, D(parts[6]), D(parts[7]), expected, null));
        }

        Assert.Equal(39, observations.Count(static row => row.Domain == "VR2"));
        Assert.Equal(360, observations.Count(static row => row.Domain == "EXACT-V9"));
        Assert.Equal(1_280, observations.Count(static row => row.Domain == "SEAM"));
        return observations;
    }

    private static Dictionary<string, StateBits> LoadFrozenStateMap(
        string path,
        string header,
        Func<string[], string> identity,
        int resolvedIndex,
        int regionIndex,
        int phaseIndex,
        int temperatureIndex,
        int pressureIndex,
        int qualityIndex,
        Func<string[], bool>? include)
    {
        var result = new Dictionary<string, StateBits>(StringComparer.Ordinal);
        foreach (var line in ReadCsvLines(path, header))
        {
            var parts = line.Split(',');
            if (!string.Equals(parts[0], FrozenC3CandidateId, StringComparison.Ordinal))
            {
                continue;
            }

            if (include is not null && !include(parts))
            {
                continue;
            }

            var resolved = bool.Parse(parts[resolvedIndex]);
            result.Add(identity(parts), FrozenState(
                resolved,
                parts[regionIndex],
                parts[phaseIndex],
                parts[temperatureIndex],
                parts[pressureIndex],
                parts[qualityIndex]));
        }

        return result;
    }

    private static StateBits FrozenState(
        bool resolved,
        string region,
        string phase,
        string temperature,
        string pressure,
        string quality)
    {
        if (!resolved)
        {
            return new StateBits(false, region, phase, 0L, 0L, false, 0L);
        }

        var hasQuality = !string.IsNullOrWhiteSpace(quality);
        return new StateBits(
            true,
            region,
            phase,
            BitConverter.DoubleToInt64Bits(D(temperature)),
            BitConverter.DoubleToInt64Bits(D(pressure)),
            hasQuality,
            hasQuality ? BitConverter.DoubleToInt64Bits(D(quality)) : 0L);
    }

    private static StateBits Resolve(
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate,
        double specificVolume,
        double specificEnergy)
    {
        var resolved = candidate.TryResolve(specificVolume, specificEnergy, out var state);
        if (!resolved)
        {
            return new StateBits(false, "UNRESOLVED", "Unspecified", 0L, 0L, false, 0L);
        }

        return new StateBits(
            true,
            state.Region,
            state.Phase,
            BitConverter.DoubleToInt64Bits(state.TemperatureCelsius),
            BitConverter.DoubleToInt64Bits(state.PressureMegapascals),
            state.VaporQuality.HasValue,
            state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value) : 0L);
    }

    private static bool StateBitsEqual(StateBits left, StateBits right)
        => left.Resolved == right.Resolved
            && string.Equals(left.Region, right.Region, StringComparison.Ordinal)
            && string.Equals(left.Phase, right.Phase, StringComparison.Ordinal)
            && left.TemperatureBits == right.TemperatureBits
            && left.PressureBits == right.PressureBits
            && left.HasQuality == right.HasQuality
            && left.QualityBits == right.QualityBits;

    private static List<HydraulicObservation> LoadHydraulicRows(string sourcePath, string expectedPath)
    {
        const string expectedHeader = "candidate_id,probe_id,logical_step,elapsed_s,path_id,from_node,to_node,candidate_resolved,candidate_driving_pa,candidate_flow_kg_s,canonical_flow_kg_s,if97_counterfactual_flow_kg_s,abs_candidate_minus_canonical_flow_kg_s,abs_candidate_minus_if97_flow_kg_s,driving_sign_changed_from_production";
        var expected = new Dictionary<string, HydraulicBits>(StringComparer.Ordinal);
        foreach (var line in ReadCsvLines(expectedPath, expectedHeader))
        {
            var parts = line.Split(',');
            Assert.Equal(15, parts.Length);
            if (!string.Equals(parts[0], FrozenC3CandidateId, StringComparison.Ordinal))
            {
                continue;
            }

            var identity = HydraulicIdentity(parts[1], long.Parse(parts[2], CultureInfo.InvariantCulture), parts[4]);
            var resolved = bool.Parse(parts[7]);
            expected.Add(identity, new HydraulicBits(
                resolved,
                resolved ? BitConverter.DoubleToInt64Bits(D(parts[8])) : 0L,
                resolved ? BitConverter.DoubleToInt64Bits(D(parts[9])) : 0L,
                bool.Parse(parts[14])));
        }

        const string sourceHeader = "probe_id,logical_step,elapsed_s,path_id,from_node,to_node,resistance_pa_s2_kg2,active_boost_pa,production_driving_pa,if97_driving_pa,canonical_flow_kg_s,production_formula_flow_kg_s,if97_pressure_only_counterfactual_flow_kg_s,reference_resolved,driving_pressure_sign_changed,abs_counterfactual_flow_shift_kg_s";
        var rows = new List<HydraulicObservation>(288);
        foreach (var line in ReadCsvLines(sourcePath, sourceHeader))
        {
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            var logicalStep = long.Parse(parts[1], CultureInfo.InvariantCulture);
            var identity = HydraulicIdentity(parts[0], logicalStep, parts[3]);
            Assert.True(expected.TryGetValue(identity, out var expectedBits));
            rows.Add(new HydraulicObservation(
                rows.Count,
                parts[0],
                logicalStep,
                parts[3],
                parts[4],
                parts[5],
                D(parts[6]),
                D(parts[7]),
                D(parts[8]),
                expectedBits));
        }

        return rows;
    }

    private static HydraulicBits EvaluateHydraulic(
        HydraulicObservation row,
        IReadOnlyDictionary<NodeKey, StateRepeat> exactV9,
        int repeatIndex)
    {
        Assert.True(exactV9.TryGetValue(new NodeKey(row.ProbeId, row.LogicalStep, row.FromNodeId), out var from));
        Assert.True(exactV9.TryGetValue(new NodeKey(row.ProbeId, row.LogicalStep, row.ToNodeId), out var to));
        var fromState = repeatIndex == 0 ? from.First : from.Second;
        var toState = repeatIndex == 0 ? to.First : to.Second;
        var resolved = fromState.Resolved && toState.Resolved;
        if (!resolved)
        {
            return new HydraulicBits(false, 0L, 0L, false);
        }

        var fromPressure = BitConverter.Int64BitsToDouble(fromState.PressureBits);
        var toPressure = BitConverter.Int64BitsToDouble(toState.PressureBits);
        var driving = ((fromPressure - toPressure) * 1_000_000d) + row.ActiveBoostPascals;
        var flow = SolveQuadraticFlow(driving, row.Resistance, HasFrozenCheckValve(row.PathId));
        var signChanged = double.IsFinite(driving)
            && double.IsFinite(row.ProductionDrivingPascals)
            && Math.Abs(driving) > 1e-9d
            && Math.Abs(row.ProductionDrivingPascals) > 1e-9d
            && Math.Sign(driving) != Math.Sign(row.ProductionDrivingPascals);
        return new HydraulicBits(
            true,
            BitConverter.DoubleToInt64Bits(driving),
            BitConverter.DoubleToInt64Bits(flow),
            signChanged);
    }

    private static bool HydraulicBitsEqual(HydraulicBits left, HydraulicBits right)
        => left.Resolved == right.Resolved
            && left.DrivingPressureBits == right.DrivingPressureBits
            && left.FlowBits == right.FlowBits
            && left.SignChangedFromProduction == right.SignChangedFromProduction;

    private static void WriteStateSemanticEvidence(string directory, IReadOnlyList<StateSemanticResult> rows)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "02-state-semantic-equivalence.csv"), append: false, Utf8WithoutBom);
        writer.WriteLine("domain,observation_index,source_identity,c3_resolved,c4_resolved,c3_region,c4_region,c3_phase,c4_phase,c3_temperature_bits,c4_temperature_bits,c3_pressure_bits,c4_pressure_bits,c3_quality_has_value,c4_quality_has_value,c3_quality_bits,c4_quality_bits,c4_repeat_count,c4_repeat_deterministic,bit_equivalent");
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            writer.WriteLine(string.Join(",",
                row.Observation.Domain,
                row.Observation.ObservationIndex.ToString(CultureInfo.InvariantCulture),
                row.Observation.SourceIdentity,
                B(row.Observation.Expected.Resolved),
                B(row.First.Resolved),
                row.Observation.Expected.Region,
                row.First.Region,
                row.Observation.Expected.Phase,
                row.First.Phase,
                row.Observation.Expected.TemperatureBits.ToString(CultureInfo.InvariantCulture),
                row.First.TemperatureBits.ToString(CultureInfo.InvariantCulture),
                row.Observation.Expected.PressureBits.ToString(CultureInfo.InvariantCulture),
                row.First.PressureBits.ToString(CultureInfo.InvariantCulture),
                B(row.Observation.Expected.HasQuality),
                B(row.First.HasQuality),
                row.Observation.Expected.QualityBits.ToString(CultureInfo.InvariantCulture),
                row.First.QualityBits.ToString(CultureInfo.InvariantCulture),
                "2",
                B(row.Deterministic),
                B(row.Equivalent)));
        }
    }

    private static void WriteHydraulicSemanticEvidence(string directory, IReadOnlyList<HydraulicSemanticResult> rows)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "03-hydraulic-semantic-equivalence.csv"), append: false, Utf8WithoutBom);
        writer.WriteLine("domain,observation_index,probe_id,logical_step,path_id,from_node,to_node,c3_resolved,c4_resolved,c3_driving_pressure_bits,c4_driving_pressure_bits,c3_flow_bits,c4_flow_bits,c3_sign_changed_from_production,c4_sign_changed_from_production,c4_repeat_count,c4_repeat_deterministic,bit_equivalent");
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            writer.WriteLine(string.Join(",",
                "HYDRAULIC",
                row.Observation.ObservationIndex.ToString(CultureInfo.InvariantCulture),
                row.Observation.ProbeId,
                row.Observation.LogicalStep.ToString(CultureInfo.InvariantCulture),
                row.Observation.PathId,
                row.Observation.FromNodeId,
                row.Observation.ToNodeId,
                B(row.Observation.Expected.Resolved),
                B(row.First.Resolved),
                row.Observation.Expected.DrivingPressureBits.ToString(CultureInfo.InvariantCulture),
                row.First.DrivingPressureBits.ToString(CultureInfo.InvariantCulture),
                row.Observation.Expected.FlowBits.ToString(CultureInfo.InvariantCulture),
                row.First.FlowBits.ToString(CultureInfo.InvariantCulture),
                B(row.Observation.Expected.SignChangedFromProduction),
                B(row.First.SignChangedFromProduction),
                "2",
                B(row.Deterministic),
                B(row.Equivalent)));
        }
    }

    private static void WriteSemanticSummary(
        string directory,
        IReadOnlyList<StateSemanticResult> states,
        IReadOnlyList<HydraulicSemanticResult> hydraulics,
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate)
    {
        var stateMismatches = states.Count(static row => !row.Equivalent);
        var hydraulicMismatches = hydraulics.Count(static row => !row.Equivalent);
        var repeatMismatches = states.Count(static row => !row.Deterministic)
            + hydraulics.Count(static row => !row.Deterministic);
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-C4",
            "status=PASS-SEMANTIC-EVIDENCE-COMPLETE",
            "candidate=" + candidate.CandidateId,
            "family=" + candidate.FamilyId,
            "state-observation-count=" + states.Count.ToString(CultureInfo.InvariantCulture),
            "hydraulic-observation-count=" + hydraulics.Count.ToString(CultureInfo.InvariantCulture),
            "total-observation-count=" + (states.Count + hydraulics.Count).ToString(CultureInfo.InvariantCulture),
            "state-bit-mismatch-count=" + stateMismatches.ToString(CultureInfo.InvariantCulture),
            "hydraulic-bit-mismatch-count=" + hydraulicMismatches.ToString(CultureInfo.InvariantCulture),
            "repeat-mismatch-count=" + repeatMismatches.ToString(CultureInfo.InvariantCulture),
            "semantic-bit-equivalent=" + B(stateMismatches == 0 && hydraulicMismatches == 0 && repeatMismatches == 0),
            "engineering-negative-outcome-is-xunit-failure=False",
            "rp1c-selection-authorized=False",
            "production-repair-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "04-semantic-equivalence-summary.txt"), lines, Utf8WithoutBom);
    }

    private static R1Row[] LoadR1Rows(string path)
    {
        const string header = "candidate_id,boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,candidate_minus_reference_temperature_c,candidate_minus_reference_pressure_mpa";
        var result = new List<R1Row>(BoundaryCount);
        foreach (var line in ReadCsvLines(path, header))
        {
            var parts = line.Split(',');
            Assert.Equal(18, parts.Length);
            if (!string.Equals(parts[0], FrozenC3CandidateId, StringComparison.Ordinal)
                || !string.Equals(parts[3], "R1-SIDE", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.True(bool.Parse(parts[9]));
            result.Add(new R1Row(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                D(parts[2]),
                parts[11],
                double.NaN,
                double.NaN));
        }

        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar),
            "05-seam-probe-map.csv");
        const string sourceHeader = "boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa";
        var inputs = new Dictionary<int, (double Volume, double Energy)>();
        foreach (var line in ReadCsvLines(sourcePath, sourceHeader))
        {
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            if (!string.Equals(parts[2], "R1-SIDE", StringComparison.Ordinal))
            {
                continue;
            }

            inputs.Add(int.Parse(parts[0], CultureInfo.InvariantCulture), (D(parts[6]), D(parts[7])));
        }

        result.Sort(static (left, right) => left.BoundaryIndex.CompareTo(right.BoundaryIndex));
        var rows = result.ToArray();
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.True(inputs.TryGetValue(rows[index].BoundaryIndex, out var input));
            rows[index] = rows[index] with { SpecificVolume = input.Volume, SpecificEnergy = input.Energy };
        }

        return rows;
    }

    private static void ResolveWarmupPass(
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate,
        IReadOnlyList<R1Row> rows,
        Lane lane,
        int runIndex,
        int pass)
    {
        for (var offset = 0; offset < BoundaryCount; offset++)
        {
            var rowIndex = RowIndex(lane, runIndex, pass, offset);
            var row = rows[rowIndex];
            _ = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out _);
        }
    }

    private static void PrimeTimingHarness(
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate,
        R1Row row)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        var start = Stopwatch.GetTimestamp();
        var resolved = candidate.TryResolveWithPath(
            row.SpecificVolume,
            row.SpecificEnergy,
            out var state,
            out var resolutionPath);
        var end = Stopwatch.GetTimestamp();
        var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        _ = GC.CollectionCount(0) - gen0Before;
        _ = GC.CollectionCount(1) - gen1Before;
        _ = GC.CollectionCount(2) - gen2Before;
        _ = Math.Max(0L, allocatedAfter - allocatedBefore);
        _ = TicksToMicroseconds(end - start);
        _ = PhaseCode(resolved, state);
        _ = new TimingSample(
            row.BoundaryIndex,
            row.BoundaryTemperatureCelsius,
            0,
            0,
            0d,
            0L,
            resolved,
            PhaseCode(resolved, state),
            resolutionPath,
            0,
            0,
            0);
    }

    private static int RowIndex(Lane lane, int runIndex, int pass, int offset)
    {
        if (lane == Lane.A)
        {
            var start = (pass * HistoricalRotationStride) % BoundaryCount;
            return (start + offset) % BoundaryCount;
        }

        var parameters = LaneBParameters(runIndex);
        return (parameters.BaseOffset + (pass * parameters.PassAdvance) + (offset * parameters.PermutationStride)) % BoundaryCount;
    }

    private static void ValidateLanePermutationCoverage(Lane lane, int runIndex)
    {
        var seen = new bool[BoundaryCount];
        for (var pass = 0; pass < MeasuredPasses; pass++)
        {
            Array.Clear(seen);
            for (var offset = 0; offset < BoundaryCount; offset++)
            {
                var index = RowIndex(lane, runIndex, pass, offset);
                Assert.InRange(index, 0, BoundaryCount - 1);
                Assert.False(seen[index]);
                seen[index] = true;
            }

            Assert.All(seen, static value => Assert.True(value));
        }
    }

    private static LaneBOrder LaneBParameters(int runIndex)
        => runIndex switch
        {
            1 => new LaneBOrder(11, 41, 73),
            2 => new LaneBOrder(47, 53, 107),
            3 => new LaneBOrder(83, 61, 149),
            4 => new LaneBOrder(119, 71, 181),
            5 => new LaneBOrder(157, 83, 213),
            _ => throw new InvalidOperationException("C4 run index must be 1..5."),
        };

    private static BoundarySummary[] BuildBoundarySummaries(
        Lane lane,
        int runIndex,
        IReadOnlyList<R1Row> rows,
        IReadOnlyList<TimingSample> samples)
    {
        var result = new BoundarySummary[BoundaryCount];
        var values = new double[MeasuredPasses];
        for (var boundary = 0; boundary < BoundaryCount; boundary++)
        {
            var count = 0;
            var max = double.NegativeInfinity;
            var maxPass = -1;
            var maxRow = -1;
            var over = 0;
            var gcCalls = 0;
            var gcExceedances = 0;
            var nonzeroAllocation = 0;
            var fallbackCalls = 0;
            for (var index = 0; index < samples.Count; index++)
            {
                var sample = samples[index];
                if (sample.BoundaryIndex != boundary)
                {
                    continue;
                }

                values[count++] = sample.ElapsedMicroseconds;
                if (sample.ElapsedMicroseconds > max)
                {
                    max = sample.ElapsedMicroseconds;
                    maxPass = sample.PassIndex;
                    maxRow = sample.RowIndex;
                }

                var exceeded = sample.ElapsedMicroseconds > ResolveMaximumCeilingMicroseconds;
                if (exceeded) over++;
                if (sample.AnyGcCollectionDelta) gcCalls++;
                if (exceeded && sample.AnyGcCollectionDelta) gcExceedances++;
                if (sample.AllocatedBytes != 0) nonzeroAllocation++;
                if (sample.ResolutionPath == Rp1bC4ResolutionPath.ImmutableC2Fallback) fallbackCalls++;
            }

            Assert.Equal(MeasuredPasses, count);
            Array.Sort(values, 0, count);
            result[boundary] = new BoundarySummary(
                lane,
                runIndex,
                boundary,
                rows[boundary].BoundaryTemperatureCelsius,
                count,
                MedianSorted(values, count),
                PercentileSorted(values, count, 0.95d),
                max,
                maxPass,
                maxRow,
                over,
                (double)over / count,
                gcCalls,
                gcExceedances,
                nonzeroAllocation,
                fallbackCalls);
        }

        return result;
    }

    private static void WriteProcessContract(
        string directory,
        Lane lane,
        int runIndex,
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate)
    {
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-C4",
            "scope=C4-TEST-ONLY-ALLOCATION-TAIL-EVIDENCE",
            "candidate=" + candidate.CandidateId,
            "lane=" + LaneText(lane),
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "warmup-passes=16",
            "measured-passes=64",
            "measured-calls=20480",
            "r1-boundary-count=320",
            "resolve-median-ceiling-us=" + F(ResolveMedianCeilingMicroseconds),
            "resolve-p95-ceiling-us=" + F(ResolveP95CeilingMicroseconds),
            "resolve-max-ceiling-us=" + F(ResolveMaximumCeilingMicroseconds),
            "required-bytes-per-call=0",
            "required-fallback-calls=0",
            "rp1c-selection-authorized=False",
            "production-repair-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "01-process-contract.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteCallTiming(string directory, Lane lane, int runIndex, IReadOnlyList<TimingSample> samples)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "02-c4-r1-call-timing.csv"), append: false, Utf8WithoutBom);
        writer.WriteLine("lane,run_index,boundary_index,boundary_temperature_c,pass_index,row_index,elapsed_us,allocated_bytes,resolved,phase,resolution_path,exceeded_max_ceiling,gen0_delta,gen1_delta,gen2_delta,any_gc_collection_delta");
        for (var index = 0; index < samples.Count; index++)
        {
            var row = samples[index];
            writer.WriteLine(string.Join(",",
                LaneText(lane),
                runIndex.ToString(CultureInfo.InvariantCulture),
                row.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
                F(row.BoundaryTemperatureCelsius),
                row.PassIndex.ToString(CultureInfo.InvariantCulture),
                row.RowIndex.ToString(CultureInfo.InvariantCulture),
                F(row.ElapsedMicroseconds),
                row.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                B(row.Resolved),
                PhaseText(row.PhaseCode),
                ResolutionPathText(row.ResolutionPath),
                B(row.ElapsedMicroseconds > ResolveMaximumCeilingMicroseconds),
                row.Gen0Delta.ToString(CultureInfo.InvariantCulture),
                row.Gen1Delta.ToString(CultureInfo.InvariantCulture),
                row.Gen2Delta.ToString(CultureInfo.InvariantCulture),
                B(row.AnyGcCollectionDelta)));
        }
    }

    private static void WriteBoundarySummaries(string directory, IReadOnlyList<BoundarySummary> rows)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "03-c4-r1-boundary-summary.csv"), append: false, Utf8WithoutBom);
        writer.WriteLine("lane,run_index,boundary_index,boundary_temperature_c,sample_count,median_us,p95_us,max_us,max_pass_index,max_row_index,calls_over_max_ceiling,exceedance_fraction,calls_with_gc_activity,exceedances_with_gc_activity,nonzero_allocation_calls,fallback_calls");
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            writer.WriteLine(string.Join(",",
                LaneText(row.Lane),
                row.RunIndex.ToString(CultureInfo.InvariantCulture),
                row.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
                F(row.BoundaryTemperatureCelsius),
                row.SampleCount.ToString(CultureInfo.InvariantCulture),
                F(row.MedianMicroseconds),
                F(row.P95Microseconds),
                F(row.MaximumMicroseconds),
                row.MaximumPassIndex.ToString(CultureInfo.InvariantCulture),
                row.MaximumRowIndex.ToString(CultureInfo.InvariantCulture),
                row.CallsOverMaximumCeiling.ToString(CultureInfo.InvariantCulture),
                F(row.ExceedanceFraction),
                row.CallsWithGcActivity.ToString(CultureInfo.InvariantCulture),
                row.ExceedancesWithGcActivity.ToString(CultureInfo.InvariantCulture),
                row.NonzeroAllocationCalls.ToString(CultureInfo.InvariantCulture),
                row.FallbackCalls.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void WriteRuntimeContext(
        string directory,
        Lane lane,
        int runIndex,
        long wholeRegionAllocatedBytes,
        long candidateAllocatedBytes,
        long harnessAllocatedBytes,
        int gen0Collections,
        int gen1Collections,
        int gen2Collections)
    {
        var lines = new[]
        {
            "lane=" + LaneText(lane),
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "process-id=" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
            "stopwatch-frequency-hz=" + Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
            "processor-count=" + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            "runtime-version=" + Environment.Version,
            "whole-measured-region-allocated-bytes=" + wholeRegionAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "candidate-call-allocated-bytes-sum=" + candidateAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "harness-allocated-bytes=" + harnessAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "gc-gen0-collections=" + gen0Collections.ToString(CultureInfo.InvariantCulture),
            "gc-gen1-collections=" + gen1Collections.ToString(CultureInfo.InvariantCulture),
            "gc-gen2-collections=" + gen2Collections.ToString(CultureInfo.InvariantCulture),
        };
        File.WriteAllLines(Path.Combine(directory, "04-runtime-context.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteProcessSummary(
        string directory,
        Lane lane,
        int runIndex,
        IReadOnlyList<TimingSample> samples,
        long candidateAllocatedBytes,
        long harnessAllocatedBytes,
        IReadOnlyList<BoundarySummary> boundarySummaries)
    {
        var elapsed = new double[samples.Count];
        var unresolved = 0;
        var nonzeroAllocation = 0;
        var fallbackCalls = 0;
        var callsOver = 0;
        for (var index = 0; index < samples.Count; index++)
        {
            var sample = samples[index];
            elapsed[index] = sample.ElapsedMicroseconds;
            if (!sample.Resolved) unresolved++;
            if (sample.AllocatedBytes != 0) nonzeroAllocation++;
            if (sample.ResolutionPath == Rp1bC4ResolutionPath.ImmutableC2Fallback) fallbackCalls++;
            if (sample.ElapsedMicroseconds > ResolveMaximumCeilingMicroseconds) callsOver++;
        }

        Array.Sort(elapsed);
        var median = MedianSorted(elapsed, elapsed.Length);
        var p95 = PercentileSorted(elapsed, elapsed.Length, 0.95d);
        var maximum = elapsed[^1];
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-C4",
            "status=PASS-PROCESS-EVIDENCE-COMPLETE",
            "candidate=" + ExpectedCandidateId,
            "lane=" + LaneText(lane),
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "measured-calls=" + samples.Count.ToString(CultureInfo.InvariantCulture),
            "boundary-summary-rows=" + boundarySummaries.Count.ToString(CultureInfo.InvariantCulture),
            "median-us=" + F(median),
            "p95-us=" + F(p95),
            "max-us=" + F(maximum),
            "calls-over-max-ceiling=" + callsOver.ToString(CultureInfo.InvariantCulture),
            "unresolved-calls=" + unresolved.ToString(CultureInfo.InvariantCulture),
            "nonzero-allocation-calls=" + nonzeroAllocation.ToString(CultureInfo.InvariantCulture),
            "candidate-call-allocated-bytes-sum=" + candidateAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "harness-allocated-bytes=" + harnessAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "fallback-calls=" + fallbackCalls.ToString(CultureInfo.InvariantCulture),
            "median-within-ceiling=" + B(median <= ResolveMedianCeilingMicroseconds),
            "p95-within-ceiling=" + B(p95 <= ResolveP95CeilingMicroseconds),
            "max-within-ceiling=" + B(maximum <= ResolveMaximumCeilingMicroseconds),
            "zero-allocation=" + B(nonzeroAllocation == 0 && candidateAllocatedBytes == 0),
            "harness-allocation-neutral=" + B(harnessAllocatedBytes == 0),
            "zero-fallback=" + B(fallbackCalls == 0),
            "engineering-negative-outcome-is-xunit-failure=False",
            "rp1c-selection-authorized=False",
            "production-repair-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "05-process-summary.txt"), lines, Utf8WithoutBom);
    }

    private static byte PhaseCode(bool resolved, Rp1bShadowState state)
    {
        if (!resolved) return 0;
        if (string.Equals(state.Phase, "SubcooledLiquid", StringComparison.Ordinal)) return 1;
        if (string.Equals(state.Phase, "SaturatedMixture", StringComparison.Ordinal)) return 2;
        if (string.Equals(state.Phase, "SuperheatedVapor", StringComparison.Ordinal)) return 3;
        return 255;
    }

    private static string PhaseText(byte code)
        => code switch
        {
            0 => "Unresolved",
            1 => "SubcooledLiquid",
            2 => "SaturatedMixture",
            3 => "SuperheatedVapor",
            _ => "Unknown",
        };

    private static string ResolutionPathText(Rp1bC4ResolutionPath path)
        => path switch
        {
            Rp1bC4ResolutionPath.Unresolved => "UNRESOLVED",
            Rp1bC4ResolutionPath.C3SuperheatedVaporSeam => "C3-VAPOR-PRECEDENCE",
            Rp1bC4ResolutionPath.C2MixturePrefix => "C2-MIXTURE-PREFIX",
            Rp1bC4ResolutionPath.C2LiquidTablePrefix => "C2-LIQUID-TABLE-PREFIX",
            Rp1bC4ResolutionPath.C2NearBoundaryLiquidPrefix => "C2-NEAR-BOUNDARY-LIQUID-PREFIX",
            Rp1bC4ResolutionPath.ImmutableC2Fallback => "IMMUTABLE-C2-FALLBACK",
            Rp1bC4ResolutionPath.C3SaturatedVaporSeamFallback => "C3-SATURATED-VAPOR-FALLBACK",
            _ => "UNKNOWN",
        };

    private static Lane ParseLane(string? value)
        => value switch
        {
            "A" => Lane.A,
            "B" => Lane.B,
            _ => throw new InvalidOperationException("C4 timing lane must be A or B."),
        };

    private static int ParseRunIndex(string? value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runIndex)
            || runIndex < 1
            || runIndex > 5)
        {
            throw new InvalidOperationException("C4 timing run index must be 1..5.");
        }

        return runIndex;
    }

    private static string LaneText(Lane lane) => lane == Lane.A ? "A" : "B";

    private static string ResetOwnedProcessDirectory(string artifactDirectory, Lane lane, int runIndex)
    {
        var laneDirectory = Path.Combine(artifactDirectory, lane == Lane.A ? "lane-a" : "lane-b");
        Directory.CreateDirectory(laneDirectory);
        var processDirectory = Path.Combine(laneDirectory, $"process-{runIndex:00}");
        if (Directory.Exists(processDirectory)) Directory.Delete(processDirectory, recursive: true);
        Directory.CreateDirectory(processDirectory);
        return processDirectory;
    }

    private static string EnsureArtifactDirectory(string root)
    {
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(path);
        return path;
    }

    private static IReadOnlyList<string> ReadCsvLines(string path, string expectedHeader)
    {
        Assert.True(File.Exists(path), $"Required frozen evidence missing: {path}");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.NotEmpty(lines);
        Assert.Equal(expectedHeader, lines[0]);
        return lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

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

    private static double MedianSorted(double[] sorted, int count)
    {
        if (count == 0) return double.NaN;
        var middle = count / 2;
        return count % 2 == 0
            ? 0.5d * (sorted[middle - 1] + sorted[middle])
            : sorted[middle];
    }

    private static double PercentileSorted(double[] sorted, int count, double percentile)
    {
        if (count == 0) return double.NaN;
        var index = (int)Math.Ceiling(percentile * count) - 1;
        return sorted[Math.Clamp(index, 0, count - 1)];
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;
    private static double D(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static double DN(string value) => string.IsNullOrWhiteSpace(value) ? double.NaN : D(value);
    private static string F(double value) => double.IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : string.Empty;
    private static string B(bool value) => value ? "true" : "false";
    private static string NodeIdentity(string probeId, long logicalStep, string nodeId)
        => probeId + "|" + logicalStep.ToString(CultureInfo.InvariantCulture) + "|" + nodeId;
    private static string SeamIdentity(int boundaryIndex, string probeSide)
        => boundaryIndex.ToString(CultureInfo.InvariantCulture) + "|" + probeSide;
    private static string HydraulicIdentity(string probeId, long logicalStep, string pathId)
        => probeId + "|" + logicalStep.ToString(CultureInfo.InvariantCulture) + "|" + pathId;

    private static void RequireOptIn() => Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln"))) return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate NuclearReactorSimulator.sln from test base directory.");
    }

    private enum Lane : byte { A = 1, B = 2 }
    private readonly record struct LaneBOrder(int BaseOffset, int PassAdvance, int PermutationStride);
    private readonly record struct NodeKey(string ProbeId, long LogicalStep, string NodeId);
    private readonly record struct StateBits(bool Resolved, string Region, string Phase, long TemperatureBits, long PressureBits, bool HasQuality, long QualityBits);
    private readonly record struct StateRepeat(StateBits First, StateBits Second);
    private sealed record StateObservation(string Domain, int ObservationIndex, string SourceIdentity, double SpecificVolume, double SpecificEnergy, StateBits Expected, NodeKey? NodeKey);
    private sealed record StateSemanticResult(StateObservation Observation, StateBits First, StateBits Second, bool Deterministic, bool Equivalent);
    private readonly record struct HydraulicBits(bool Resolved, long DrivingPressureBits, long FlowBits, bool SignChangedFromProduction);
    private sealed record HydraulicObservation(int ObservationIndex, string ProbeId, long LogicalStep, string PathId, string FromNodeId, string ToNodeId, double Resistance, double ActiveBoostPascals, double ProductionDrivingPascals, HydraulicBits Expected);
    private sealed record HydraulicSemanticResult(HydraulicObservation Observation, HydraulicBits First, HydraulicBits Second, bool Deterministic, bool Equivalent);
    private readonly record struct R1Row(int BoundaryIndex, double BoundaryTemperatureCelsius, string ReferencePhase, double SpecificVolume, double SpecificEnergy);

    private readonly struct TimingSample
    {
        public TimingSample(
            int boundaryIndex,
            double boundaryTemperatureCelsius,
            int passIndex,
            int rowIndex,
            double elapsedMicroseconds,
            long allocatedBytes,
            bool resolved,
            byte phaseCode,
            Rp1bC4ResolutionPath resolutionPath,
            int gen0Delta,
            int gen1Delta,
            int gen2Delta)
        {
            BoundaryIndex = boundaryIndex;
            BoundaryTemperatureCelsius = boundaryTemperatureCelsius;
            PassIndex = passIndex;
            RowIndex = rowIndex;
            ElapsedMicroseconds = elapsedMicroseconds;
            AllocatedBytes = allocatedBytes;
            Resolved = resolved;
            PhaseCode = phaseCode;
            ResolutionPath = resolutionPath;
            Gen0Delta = gen0Delta;
            Gen1Delta = gen1Delta;
            Gen2Delta = gen2Delta;
        }

        public int BoundaryIndex { get; }
        public double BoundaryTemperatureCelsius { get; }
        public int PassIndex { get; }
        public int RowIndex { get; }
        public double ElapsedMicroseconds { get; }
        public long AllocatedBytes { get; }
        public bool Resolved { get; }
        public byte PhaseCode { get; }
        public Rp1bC4ResolutionPath ResolutionPath { get; }
        public int Gen0Delta { get; }
        public int Gen1Delta { get; }
        public int Gen2Delta { get; }
        public bool AnyGcCollectionDelta => Gen0Delta != 0 || Gen1Delta != 0 || Gen2Delta != 0;
    }

    private readonly record struct BoundarySummary(
        Lane Lane,
        int RunIndex,
        int BoundaryIndex,
        double BoundaryTemperatureCelsius,
        int SampleCount,
        double MedianMicroseconds,
        double P95Microseconds,
        double MaximumMicroseconds,
        int MaximumPassIndex,
        int MaximumRowIndex,
        int CallsOverMaximumCeiling,
        double ExceedanceFraction,
        int CallsWithGcActivity,
        int ExceedancesWithGcActivity,
        int NonzeroAllocationCalls,
        int FallbackCalls);
}
