using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Engineering Repair Planning 1 / RP1B Refinement 3. Performance-only attribution gate for
/// immutable C3. It does not modify thermodynamic behavior, create C4, select RP1C or authorize any
/// production change. The frozen RP1A max ceiling is applied to both exact-v9 and seam-side calls.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement3Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT3";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement3";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const string FrozenRefinement2Relative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement2_Artifacts";
    private const int FullCorpusWarmupPasses = 16;
    private const int FullCorpusMeasuredPasses = 64;
    private const int TailTopStateCount = 12;
    private const int TailWarmupCallsPerState = 32;
    private const int TailMeasuredBlocks = 8;
    private const int TailCallsPerBlock = 64;
    private const int SeamWarmupPasses = 4;
    private const int SeamMeasuredPasses = 16;
    private const int RotationStride = 37;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement3")]
    public void Rp1bRefinement3_AttributesAndQualifiesImmutableC3PerformanceTail()
    {
        RequireOptIn();
        var root = FindRepositoryRoot();
        var artifactDirectory = ResetArtifactDirectory(root);
        var frozenRp1aDirectory = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var frozenRefinement2Directory = Path.Combine(root, FrozenRefinement2Relative.Replace('/', Path.DirectorySeparatorChar));

        ValidateFrozenRefinement2(frozenRefinement2Directory);

        var nodeRows = LoadNodeRows(Path.Combine(frozenRp1aDirectory, "03-exact-v9-node-corpus.csv"));
        var seamRows = LoadSeamRows(Path.Combine(frozenRp1aDirectory, "05-seam-probe-map.csv"));
        var ceilings = LoadPerformanceCeilings(Path.Combine(frozenRp1aDirectory, "06-performance-baseline.csv"));
        Assert.Equal(360, nodeRows.Count);
        Assert.Equal(1_280, seamRows.Count);
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSide == "R1-SIDE"));
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSide == "R4-LIQUID-SIDE"));
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSide == "R4-VAPOR-SIDE"));
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSide == "R2-SIDE"));

        WriteContractAndProvenance(artifactDirectory, ceilings);

        var candidate = new Rp1bVaporSeamCompleteTabulatedSurrogateCandidate();
        Assert.Equal("C3-VAPOR-SEAM-COMPLETE-SURROGATE", candidate.CandidateId);
        Assert.False(candidate.UsesDirectIf97AtResolveTime);
        ValidateC3PhysicalClosure(candidate, nodeRows, seamRows);

        for (var pass = 0; pass < FullCorpusWarmupPasses; pass++)
        {
            ResolveRotatedPass(candidate, nodeRows, pass, measure: false, samples: null, ceilings.ResolveMaximumMicroseconds);
        }

        ForceFullCollection();
        var generation0Before = GC.CollectionCount(0);
        var generation1Before = GC.CollectionCount(1);
        var generation2Before = GC.CollectionCount(2);
        var samples = new List<NodeTimingSample>(nodeRows.Count * FullCorpusMeasuredPasses);
        for (var pass = 0; pass < FullCorpusMeasuredPasses; pass++)
        {
            ResolveRotatedPass(candidate, nodeRows, pass, measure: true, samples, ceilings.ResolveMaximumMicroseconds);
        }
        var generation0After = GC.CollectionCount(0);
        var generation1After = GC.CollectionCount(1);
        var generation2After = GC.CollectionCount(2);

        var stateTimings = BuildStateTimings(nodeRows, samples, ceilings.ResolveMaximumMicroseconds);
        WriteStateTiming(artifactDirectory, stateTimings);

        var tailTargets = stateTimings
            .Where(static row => row.CallsOverMaximumCeiling > 0)
            .Concat(stateTimings.OrderByDescending(static row => row.P95Microseconds).Take(TailTopStateCount))
            .GroupBy(static row => (row.ProbeId, row.LogicalStep, row.NodeId))
            .Select(static group => group.First())
            .OrderByDescending(static row => row.P95Microseconds)
            .ThenByDescending(static row => row.MaximumMicroseconds)
            .ToArray();

        var tailRepeats = MeasureTailTargets(candidate, nodeRows, tailTargets, ceilings.ResolveMaximumMicroseconds);
        WriteTailRepeats(artifactDirectory, tailRepeats);

        for (var pass = 0; pass < SeamWarmupPasses; pass++)
        {
            foreach (var row in seamRows)
            {
                _ = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
            }
        }

        var seamTimings = MeasureSeamTimings(candidate, seamRows, ceilings.ResolveMaximumMicroseconds);
        WriteSeamSideTiming(artifactDirectory, seamTimings);

        var allNodeTimings = samples.Select(static sample => sample.ElapsedMicroseconds).ToArray();
        var allAllocations = samples.Select(static sample => (double)sample.AllocatedBytes).ToArray();
        var exactMedian = Median(allNodeTimings);
        var exactP95 = Percentile(allNodeTimings, 0.95d);
        var exactMaximum = allNodeTimings.Max();
        var medianAllocation = Median(allAllocations);
        var seamMaximum = seamTimings.Max(static row => row.MaximumMicroseconds);
        var targetedMaximum = tailRepeats.Max(static row => row.TargetMaximumMicroseconds);
        var strictPerformanceContractMet = exactMedian <= ceilings.ResolveMedianMicroseconds
            && exactP95 <= ceilings.ResolveP95Microseconds
            && exactMaximum <= ceilings.ResolveMaximumMicroseconds
            && seamMaximum <= ceilings.ResolveMaximumMicroseconds
            && targetedMaximum <= ceilings.ResolveMaximumMicroseconds
            && medianAllocation <= ceilings.ResolveMedianAllocatedBytes;
        var targetedRepeatedExceedance = tailRepeats.Any(static row => row.CallsOverMaximumCeiling > 1);
        var targetedPersistentP95Exceedance = tailRepeats.Any(row => row.TargetP95Microseconds > ceilings.ResolveMaximumMicroseconds);
        var attribution = ClassifyAttribution(
            strictPerformanceContractMet,
            exactMaximum,
            seamMaximum,
            targetedMaximum,
            ceilings.ResolveMaximumMicroseconds,
            targetedRepeatedExceedance,
            targetedPersistentP95Exceedance);

        WritePerformanceQualification(
            artifactDirectory,
            ceilings,
            exactMedian,
            exactP95,
            exactMaximum,
            seamMaximum,
            targetedMaximum,
            medianAllocation,
            samples.Count,
            seamRows.Count * SeamMeasuredPasses,
            generation0After - generation0Before,
            generation1After - generation1Before,
            generation2After - generation2Before,
            tailRepeats,
            strictPerformanceContractMet,
            attribution);
        WriteSummary(artifactDirectory, strictPerformanceContractMet, attribution, exactMaximum, seamMaximum, tailRepeats);

        Assert.Equal(360, stateTimings.Count);
        Assert.NotEmpty(tailRepeats);
        Assert.Equal(5, seamTimings.Count);
        Assert.All(samples, static sample => Assert.True(double.IsFinite(sample.ElapsedMicroseconds) && sample.ElapsedMicroseconds >= 0d));
        Assert.All(seamTimings, static row => Assert.True(double.IsFinite(row.MaximumMicroseconds) && row.MaximumMicroseconds >= 0d));
    }

    private static void ValidateFrozenRefinement2(string directory)
    {
        var summaryPath = Path.Combine(directory, "09-rp1b-refinement2-summary.txt");
        Assert.True(File.Exists(summaryPath));
        var summary = File.ReadAllText(summaryPath, Encoding.UTF8);
        Assert.True(summary.Contains("status=PASS-EVIDENCE-MATRIX-COMPLETE", StringComparison.Ordinal));
        Assert.True(summary.Contains("c3-vapor-seam-complete-surrogate-all-frozen-points-resolved=True", StringComparison.Ordinal));
        Assert.True(summary.Contains("c3-vapor-seam-complete-surrogate-exact-v9-phase-agreement-percent=100", StringComparison.Ordinal));
        Assert.True(summary.Contains("c3-vapor-seam-complete-surrogate-seam-unresolved=0", StringComparison.Ordinal));
        Assert.True(summary.Contains("c3-vapor-seam-complete-surrogate-seam-phase-mismatch=0", StringComparison.Ordinal));
        Assert.True(summary.Contains("c3-vapor-seam-complete-surrogate-performance-ceiling-met=False", StringComparison.Ordinal));
        Assert.True(summary.Contains("d3-vapor-seam-complete-if97-comparator-rp1c-selection-eligible=True", StringComparison.Ordinal));

        var performancePath = Path.Combine(directory, "06-candidate-performance.csv");
        var lines = File.ReadAllLines(performancePath, Encoding.UTF8);
        Assert.Equal(3, lines.Length);
        Assert.True(lines[1].Contains("C3-VAPOR-SEAM-COMPLETE-SURROGATE,", StringComparison.Ordinal));
        Assert.True(lines[1].Contains(",823.4,175.1,", StringComparison.Ordinal));
        Assert.True(lines[2].Contains("D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR,", StringComparison.Ordinal));
        Assert.True(lines[2].Contains(",208.5,16504.9,", StringComparison.Ordinal));
    }

    private static void ValidateC3PhysicalClosure(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<NodeRow> nodeRows,
        IReadOnlyList<SeamRow> seamRows)
    {
        foreach (var row in nodeRows)
        {
            Assert.True(candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state));
            Assert.True(PhaseMatches(row.ReferencePhase, state.Phase));
        }

        foreach (var row in seamRows)
        {
            Assert.True(candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state));
            Assert.True(PhaseMatches(row.ReferencePhase, state.Phase));
        }
    }

    private static void ResolveRotatedPass(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<NodeRow> rows,
        int pass,
        bool measure,
        List<NodeTimingSample>? samples,
        double maximumCeilingMicroseconds)
    {
        var startIndex = (pass * RotationStride) % rows.Count;
        for (var offset = 0; offset < rows.Count; offset++)
        {
            var rowIndex = (startIndex + offset) % rows.Count;
            var row = rows[rowIndex];
            if (!measure)
            {
                _ = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
                continue;
            }

            var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
            var end = Stopwatch.GetTimestamp();
            var afterBytes = GC.GetAllocatedBytesForCurrentThread();
            Assert.True(resolved);
            var elapsed = TicksToMicroseconds(end - start);
            samples!.Add(new NodeTimingSample(
                row.ProbeId,
                row.LogicalStep,
                row.ElapsedSeconds,
                row.NodeId,
                pass,
                rowIndex,
                elapsed,
                Math.Max(0L, afterBytes - beforeBytes),
                elapsed > maximumCeilingMicroseconds));
        }
    }

    private static IReadOnlyList<StateTiming> BuildStateTimings(
        IReadOnlyList<NodeRow> rows,
        IReadOnlyList<NodeTimingSample> samples,
        double maximumCeilingMicroseconds)
    {
        var rowMap = rows.ToDictionary(static row => (row.ProbeId, row.LogicalStep, row.NodeId));
        return samples
            .GroupBy(static sample => (sample.ProbeId, sample.LogicalStep, sample.NodeId))
            .Select(group =>
            {
                var row = rowMap[group.Key];
                var values = group.Select(static sample => sample.ElapsedMicroseconds).ToArray();
                var maxSample = group.OrderByDescending(static sample => sample.ElapsedMicroseconds).First();
                var overCount = group.Count(sample => sample.ElapsedMicroseconds > maximumCeilingMicroseconds);
                var p95 = Percentile(values, 0.95d);
                return new StateTiming(
                    row.ProbeId,
                    row.LogicalStep,
                    row.ElapsedSeconds,
                    row.NodeId,
                    row.ReferenceRegion,
                    row.ReferencePhase,
                    values.Length,
                    Median(values),
                    p95,
                    values.Max(),
                    maxSample.PassIndex,
                    maxSample.RowIndex,
                    overCount,
                    (double)overCount / values.Length,
                    ClassifyStateTail(values.Max(), p95, overCount, maximumCeilingMicroseconds));
            })
            .OrderByDescending(static row => row.P95Microseconds)
            .ThenByDescending(static row => row.MaximumMicroseconds)
            .ToArray();
    }

    private static IReadOnlyList<TailRepeat> MeasureTailTargets(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<NodeRow> nodeRows,
        IReadOnlyList<StateTiming> targets,
        double maximumCeilingMicroseconds)
    {
        var rowMap = nodeRows.ToDictionary(static row => (row.ProbeId, row.LogicalStep, row.NodeId));
        var results = new List<TailRepeat>(targets.Count);

        foreach (var target in targets)
        {
            var row = rowMap[(target.ProbeId, target.LogicalStep, target.NodeId)];
            for (var warmup = 0; warmup < TailWarmupCallsPerState; warmup++)
            {
                _ = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
            }

            var values = new List<double>(TailMeasuredBlocks * TailCallsPerBlock);
            var blocksWithExceedance = 0;
            for (var block = 0; block < TailMeasuredBlocks; block++)
            {
                var blockExceeded = false;
                for (var call = 0; call < TailCallsPerBlock; call++)
                {
                    var start = Stopwatch.GetTimestamp();
                    var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
                    var end = Stopwatch.GetTimestamp();
                    Assert.True(resolved);
                    var elapsed = TicksToMicroseconds(end - start);
                    values.Add(elapsed);
                    blockExceeded |= elapsed > maximumCeilingMicroseconds;
                }
                if (blockExceeded) blocksWithExceedance++;
            }

            var overCount = values.Count(value => value > maximumCeilingMicroseconds);
            var p95 = Percentile(values, 0.95d);
            var maximum = values.Max();
            results.Add(new TailRepeat(
                row.ProbeId,
                row.LogicalStep,
                row.ElapsedSeconds,
                row.NodeId,
                target.MedianMicroseconds,
                target.P95Microseconds,
                target.MaximumMicroseconds,
                values.Count,
                Median(values),
                p95,
                maximum,
                overCount,
                blocksWithExceedance,
                ClassifyTargetedTail(maximum, p95, overCount, blocksWithExceedance, maximumCeilingMicroseconds)));
        }

        return results
            .OrderByDescending(static row => row.TargetP95Microseconds)
            .ThenByDescending(static row => row.TargetMaximumMicroseconds)
            .ToArray();
    }

    private static IReadOnlyList<SeamSideTiming> MeasureSeamTimings(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<SeamRow> seamRows,
        double maximumCeilingMicroseconds)
    {
        var samples = new List<SeamTimingSample>(seamRows.Count * SeamMeasuredPasses);
        for (var pass = 0; pass < SeamMeasuredPasses; pass++)
        {
            var startIndex = (pass * RotationStride) % seamRows.Count;
            for (var offset = 0; offset < seamRows.Count; offset++)
            {
                var row = seamRows[(startIndex + offset) % seamRows.Count];
                var start = Stopwatch.GetTimestamp();
                var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
                var end = Stopwatch.GetTimestamp();
                Assert.True(resolved);
                Assert.True(PhaseMatches(row.ReferencePhase, state.Phase));
                samples.Add(new SeamTimingSample(
                    row.ProbeSide,
                    TicksToMicroseconds(end - start)));
            }
        }

        var results = samples
            .GroupBy(static sample => sample.ProbeSide, StringComparer.Ordinal)
            .Select(group => BuildSeamSideTiming(group.Key, group.Select(static sample => sample.ElapsedMicroseconds).ToArray(), maximumCeilingMicroseconds))
            .OrderBy(static row => row.ProbeSide, StringComparer.Ordinal)
            .ToList();
        results.Add(BuildSeamSideTiming("ALL-SEAM-SIDES", samples.Select(static sample => sample.ElapsedMicroseconds).ToArray(), maximumCeilingMicroseconds));
        return results;
    }

    private static SeamSideTiming BuildSeamSideTiming(string probeSide, IReadOnlyList<double> values, double maximumCeilingMicroseconds)
    {
        var overCount = values.Count(value => value > maximumCeilingMicroseconds);
        return new SeamSideTiming(
            probeSide,
            values.Count,
            Median(values),
            Percentile(values, 0.95d),
            values.Max(),
            overCount,
            (double)overCount / values.Count);
    }

    private static string ClassifyStateTail(double maximum, double p95, int overCount, double ceiling)
    {
        if (maximum <= ceiling) return "WITHIN-CEILING";
        if (p95 > ceiling) return "PERSISTENT-SLOW-PATH";
        return overCount == 1 ? "ISOLATED-EXCEEDANCE" : "REPEATED-EXCEEDANCE";
    }

    private static string ClassifyTargetedTail(double maximum, double p95, int overCount, int blocksWithExceedance, double ceiling)
    {
        if (maximum <= ceiling) return "TARGETED-WITHIN-CEILING";
        if (p95 > ceiling) return "TARGETED-PERSISTENT-SLOW-PATH";
        if (blocksWithExceedance > 1 || overCount > 1) return "TARGETED-REPEATED-EXCEEDANCE";
        return "TARGETED-ISOLATED-EXCEEDANCE";
    }

    private static string ClassifyAttribution(
        bool strictPerformanceContractMet,
        double exactMaximum,
        double seamMaximum,
        double targetedMaximum,
        double maximumCeiling,
        bool targetedRepeatedExceedance,
        bool targetedPersistentP95Exceedance)
    {
        if (strictPerformanceContractMet) return "STRICT-PERFORMANCE-CONTRACT-MET";
        if (seamMaximum > maximumCeiling) return "SEAM-WORST-CASE-BLOCKING";
        if (targetedPersistentP95Exceedance) return "DETERMINISTIC-SLOW-PATH-CONFIRMED";
        if (targetedRepeatedExceedance) return "REPEATED-PERFORMANCE-TAIL-CONFIRMED";
        if (exactMaximum > maximumCeiling || targetedMaximum > maximumCeiling) return "ISOLATED-PERFORMANCE-TAIL-NOT-QUALIFIED";
        return "PERFORMANCE-CONTRACT-NOT-MET";
    }

    private static void WriteContractAndProvenance(string directory, PerformanceCeilings ceilings)
    {
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT3",
            "scope=C3-UNCHANGED-PERFORMANCE-TAIL-ATTRIBUTION-QUALIFICATION",
            "c3-source-identity=UNCHANGED-FROM-REFINEMENT2",
            "c4-created=False",
            "full-corpus-warmup-passes=" + FullCorpusWarmupPasses.ToString(CultureInfo.InvariantCulture),
            "full-corpus-measured-passes=" + FullCorpusMeasuredPasses.ToString(CultureInfo.InvariantCulture),
            "tail-top-state-count=" + TailTopStateCount.ToString(CultureInfo.InvariantCulture),
            "tail-measured-blocks=" + TailMeasuredBlocks.ToString(CultureInfo.InvariantCulture),
            "tail-calls-per-block=" + TailCallsPerBlock.ToString(CultureInfo.InvariantCulture),
            "seam-measured-passes=" + SeamMeasuredPasses.ToString(CultureInfo.InvariantCulture),
            "resolve-median-ceiling-us=" + F(ceilings.ResolveMedianMicroseconds),
            "resolve-p95-ceiling-us=" + F(ceilings.ResolveP95Microseconds),
            "resolve-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "seam-max-uses-resolve-max-ceiling=True",
            "resolve-median-allocation-ceiling-bytes=" + F(ceilings.ResolveMedianAllocatedBytes),
            "strict-max-not-replaced-by-statistical-tail=True",
            "rp1c-selection-authorized=False",
            "production-src-change-authorized=False",
            "thermodynamic-repair-authorized=False",
            "exact-v9-change-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "01-contract-and-provenance.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteStateTiming(string directory, IReadOnlyList<StateTiming> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,elapsed_s,node_id,reference_region,reference_phase,sample_count,median_us,p95_us,max_us,max_pass_index,max_row_index,calls_over_max_ceiling,exceedance_fraction,tail_classification"
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", new[]
            {
                row.ProbeId,
                row.LogicalStep.ToString(CultureInfo.InvariantCulture),
                F(row.ElapsedSeconds),
                row.NodeId,
                row.ReferenceRegion,
                row.ReferencePhase,
                row.SampleCount.ToString(CultureInfo.InvariantCulture),
                F(row.MedianMicroseconds),
                F(row.P95Microseconds),
                F(row.MaximumMicroseconds),
                row.MaximumPassIndex.ToString(CultureInfo.InvariantCulture),
                row.MaximumRowIndex.ToString(CultureInfo.InvariantCulture),
                row.CallsOverMaximumCeiling.ToString(CultureInfo.InvariantCulture),
                F(row.ExceedanceFraction),
                row.TailClassification,
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "02-c3-exact-v9-state-timing.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTailRepeats(string directory, IReadOnlyList<TailRepeat> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,elapsed_s,node_id,screen_median_us,screen_p95_us,screen_max_us,target_sample_count,target_median_us,target_p95_us,target_max_us,calls_over_max_ceiling,blocks_with_exceedance,target_classification"
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", new[]
            {
                row.ProbeId,
                row.LogicalStep.ToString(CultureInfo.InvariantCulture),
                F(row.ElapsedSeconds),
                row.NodeId,
                F(row.ScreenMedianMicroseconds),
                F(row.ScreenP95Microseconds),
                F(row.ScreenMaximumMicroseconds),
                row.TargetSampleCount.ToString(CultureInfo.InvariantCulture),
                F(row.TargetMedianMicroseconds),
                F(row.TargetP95Microseconds),
                F(row.TargetMaximumMicroseconds),
                row.CallsOverMaximumCeiling.ToString(CultureInfo.InvariantCulture),
                row.BlocksWithExceedance.ToString(CultureInfo.InvariantCulture),
                row.TargetClassification,
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "03-c3-tail-target-repeats.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSeamSideTiming(string directory, IReadOnlyList<SeamSideTiming> rows)
    {
        var lines = new List<string>
        {
            "probe_side,sample_count,median_us,p95_us,max_us,calls_over_max_ceiling,exceedance_fraction"
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", new[]
            {
                row.ProbeSide,
                row.SampleCount.ToString(CultureInfo.InvariantCulture),
                F(row.MedianMicroseconds),
                F(row.P95Microseconds),
                F(row.MaximumMicroseconds),
                row.CallsOverMaximumCeiling.ToString(CultureInfo.InvariantCulture),
                F(row.ExceedanceFraction),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "04-c3-seam-side-timing.csv"), lines, Utf8WithoutBom);
    }

    private static void WritePerformanceQualification(
        string directory,
        PerformanceCeilings ceilings,
        double exactMedian,
        double exactP95,
        double exactMaximum,
        double seamMaximum,
        double targetedMaximum,
        double medianAllocation,
        int exactMeasuredCalls,
        int seamMeasuredCalls,
        int generation0Collections,
        int generation1Collections,
        int generation2Collections,
        IReadOnlyList<TailRepeat> tailRepeats,
        bool strictPerformanceContractMet,
        string attribution)
    {
        var lines = new[]
        {
            "candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "stopwatch-frequency-hz=" + Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
            "exact-v9-measured-calls=" + exactMeasuredCalls.ToString(CultureInfo.InvariantCulture),
            "seam-measured-calls=" + seamMeasuredCalls.ToString(CultureInfo.InvariantCulture),
            "resolve-median-us=" + F(exactMedian),
            "resolve-p95-us=" + F(exactP95),
            "resolve-max-us=" + F(exactMaximum),
            "seam-max-us=" + F(seamMaximum),
            "targeted-repeat-max-us=" + F(targetedMaximum),
            "resolve-median-allocated-bytes=" + F(medianAllocation),
            "resolve-median-ceiling-us=" + F(ceilings.ResolveMedianMicroseconds),
            "resolve-p95-ceiling-us=" + F(ceilings.ResolveP95Microseconds),
            "resolve-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "seam-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "resolve-median-allocation-ceiling-bytes=" + F(ceilings.ResolveMedianAllocatedBytes),
            "gc-gen0-collections-during-full-corpus-measurement=" + generation0Collections.ToString(CultureInfo.InvariantCulture),
            "gc-gen1-collections-during-full-corpus-measurement=" + generation1Collections.ToString(CultureInfo.InvariantCulture),
            "gc-gen2-collections-during-full-corpus-measurement=" + generation2Collections.ToString(CultureInfo.InvariantCulture),
            "targeted-state-count=" + tailRepeats.Count.ToString(CultureInfo.InvariantCulture),
            "targeted-repeated-exceedance=" + B(tailRepeats.Any(static row => row.CallsOverMaximumCeiling > 1)),
            "targeted-persistent-p95-exceedance=" + B(tailRepeats.Any(row => row.TargetP95Microseconds > ceilings.ResolveMaximumMicroseconds)),
            "strict-performance-contract-met=" + B(strictPerformanceContractMet),
            "attribution=" + attribution,
            "strict-max-not-replaced-by-statistical-tail=True",
        };
        File.WriteAllLines(Path.Combine(directory, "05-c3-performance-qualification.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(
        string directory,
        bool strictPerformanceContractMet,
        string attribution,
        double exactMaximum,
        double seamMaximum,
        IReadOnlyList<TailRepeat> tailRepeats)
    {
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT3",
            "status=PASS-PERFORMANCE-ATTRIBUTION-EVIDENCE-COMPLETE",
            "candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "candidate-source-changed=False",
            "c4-created=False",
            "exact-v9-resolve-max-us=" + F(exactMaximum),
            "seam-resolve-max-us=" + F(seamMaximum),
            "targeted-state-count=" + tailRepeats.Count.ToString(CultureInfo.InvariantCulture),
            "strict-performance-contract-met=" + B(strictPerformanceContractMet),
            "attribution=" + attribution,
            "rp1c-selection-performed=False",
            "rp1c-selection-authorized=False",
            "production-src-change-authorized=False",
            "thermodynamic-repair-authorized=False",
            "thermodynamic-tolerance-change-authorized=False",
            "exact-v9-change-authorized=False",
            "vr3-authorized=False",
            "p3-r1-authorized=False",
            "second-replacement-long-authorized=False",
            "next-action=Return complete Refinement 3 artifacts for engineering review before RP1C or C4.",
        };
        File.WriteAllLines(Path.Combine(directory, "06-rp1b-refinement3-summary.txt"), lines, Utf8WithoutBom);
    }

    private static IReadOnlyList<NodeRow> LoadNodeRows(string path)
    {
        const string header = "probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        return lines.Select(static line =>
        {
            var parts = line.Split(',');
            Assert.Equal(17, parts.Length);
            var density = D(parts[6]);
            Assert.True(density > 0d);
            Assert.True(bool.Parse(parts[10]));
            return new NodeRow(
                parts[0],
                long.Parse(parts[1], CultureInfo.InvariantCulture),
                D(parts[2]),
                parts[3],
                1d / density,
                D(parts[7]),
                parts[11],
                parts[12]);
        }).ToArray();
    }

    private static IReadOnlyList<SeamRow> LoadSeamRows(string path)
    {
        const string header = "boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        return lines.Select(static line =>
        {
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            return new SeamRow(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                parts[2],
                parts[3],
                parts[4],
                D(parts[6]),
                D(parts[7]));
        }).ToArray();
    }

    private static PerformanceCeilings LoadPerformanceCeilings(string path)
    {
        var values = File.ReadAllLines(path, Encoding.UTF8)
            .Skip(1)
            .Where(static line => line.Contains(',', StringComparison.Ordinal))
            .Select(static line => line.Split(',', 2))
            .Where(static parts => parts.Length == 2)
            .ToDictionary(static parts => parts[0], static parts => parts[1], StringComparer.Ordinal);
        return new PerformanceCeilings(
            D(values["rp1b_candidate_resolve_median_ceiling_us"]),
            D(values["rp1b_candidate_resolve_p95_ceiling_us"]),
            D(values["rp1b_candidate_resolve_max_ceiling_us"]),
            D(values["rp1b_candidate_resolve_median_allocated_bytes_ceiling"]));
    }

    private static IReadOnlyList<string> ReadCsvLines(string path, string expectedHeader)
    {
        Assert.True(File.Exists(path), $"Required frozen RP1A artifact missing: {path}");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.NotEmpty(lines);
        Assert.Equal(expectedHeader, lines[0]);
        return lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    private static bool PhaseMatches(string reference, string candidate)
        => string.Equals(reference, candidate, StringComparison.Ordinal);

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static double Median(IEnumerable<double> source)
    {
        var sorted = source.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0) return double.NaN;
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? 0.5d * (sorted[middle - 1] + sorted[middle]) : sorted[middle];
    }

    private static double Percentile(IEnumerable<double> source, double percentile)
    {
        var sorted = source.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0) return double.NaN;
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;
    private static double D(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string B(bool value) => value ? "True" : "False";

    private static string ResetArtifactDirectory(string root)
    {
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void RequireOptIn()
        => Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));

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

    private sealed record NodeRow(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string NodeId,
        double SpecificVolume,
        double SpecificEnergy,
        string ReferenceRegion,
        string ReferencePhase);

    private sealed record SeamRow(
        int BoundaryIndex,
        string ProbeSide,
        string ReferenceRegion,
        string ReferencePhase,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record PerformanceCeilings(
        double ResolveMedianMicroseconds,
        double ResolveP95Microseconds,
        double ResolveMaximumMicroseconds,
        double ResolveMedianAllocatedBytes);

    private sealed record NodeTimingSample(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string NodeId,
        int PassIndex,
        int RowIndex,
        double ElapsedMicroseconds,
        long AllocatedBytes,
        bool ExceededMaximumCeiling);

    private sealed record StateTiming(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string NodeId,
        string ReferenceRegion,
        string ReferencePhase,
        int SampleCount,
        double MedianMicroseconds,
        double P95Microseconds,
        double MaximumMicroseconds,
        int MaximumPassIndex,
        int MaximumRowIndex,
        int CallsOverMaximumCeiling,
        double ExceedanceFraction,
        string TailClassification);

    private sealed record TailRepeat(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string NodeId,
        double ScreenMedianMicroseconds,
        double ScreenP95Microseconds,
        double ScreenMaximumMicroseconds,
        int TargetSampleCount,
        double TargetMedianMicroseconds,
        double TargetP95Microseconds,
        double TargetMaximumMicroseconds,
        int CallsOverMaximumCeiling,
        int BlocksWithExceedance,
        string TargetClassification);

    private sealed record SeamTimingSample(string ProbeSide, double ElapsedMicroseconds);

    private sealed record SeamSideTiming(
        string ProbeSide,
        int SampleCount,
        double MedianMicroseconds,
        double P95Microseconds,
        double MaximumMicroseconds,
        int CallsOverMaximumCeiling,
        double ExceedanceFraction);
}
