using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Engineering Repair Planning 1 / RP1B Refinement 4. Diagnostic-only localization and
/// reproducibility study of the isolated C3 R1-side seam worst-case observed in Refinement 3.
/// C3 remains byte-identical; no C4, RP1C selection or production change is authorized.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement4Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT4";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement4";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const string FrozenRefinement3Relative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement3_Artifacts";
    private const int ScreenWarmupPasses = 16;
    private const int ScreenMeasuredPasses = 64;
    private const int TargetTopCount = 12;
    private const int TargetWarmupCalls = 64;
    private const int TargetMeasuredBlocks = 16;
    private const int TargetCallsPerBlock = 128;
    private const int RotationStride = 37;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement4")]
    public void Rp1bRefinement4_LocalizesAndTestsReproducibilityOfImmutableC3R1SeamWorstCase()
    {
        RequireOptIn();
        var root = FindRepositoryRoot();
        var artifactDirectory = ResetArtifactDirectory(root);
        var frozenRp1aDirectory = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var frozenRefinement3Directory = Path.Combine(root, FrozenRefinement3Relative.Replace('/', Path.DirectorySeparatorChar));

        ValidateFrozenRefinement3(frozenRefinement3Directory);

        var allSeamRows = LoadSeamRows(Path.Combine(frozenRp1aDirectory, "05-seam-probe-map.csv"));
        var r1Rows = allSeamRows
            .Where(static row => row.ProbeSide == "R1-SIDE")
            .OrderBy(static row => row.BoundaryIndex)
            .ToArray();
        var ceilings = LoadPerformanceCeilings(Path.Combine(frozenRp1aDirectory, "06-performance-baseline.csv"));

        Assert.Equal(1_280, allSeamRows.Count);
        Assert.Equal(320, r1Rows.Length);
        Assert.Equal(320, r1Rows.Select(static row => row.BoundaryIndex).Distinct().Count());

        WriteContractAndProvenance(artifactDirectory, ceilings);

        var candidate = new Rp1bVaporSeamCompleteTabulatedSurrogateCandidate();
        Assert.Equal("C3-VAPOR-SEAM-COMPLETE-SURROGATE", candidate.CandidateId);
        Assert.False(candidate.UsesDirectIf97AtResolveTime);
        ValidateR1PhysicalClosure(candidate, r1Rows);

        for (var pass = 0; pass < ScreenWarmupPasses; pass++)
        {
            ResolveR1RotatedPass(candidate, r1Rows, pass, measure: false, samples: null, ceilings.ResolveMaximumMicroseconds);
        }

        ForceFullCollection();
        var screenGen0Before = GC.CollectionCount(0);
        var screenGen1Before = GC.CollectionCount(1);
        var screenGen2Before = GC.CollectionCount(2);
        var screenSamples = new List<R1CallTimingSample>(r1Rows.Length * ScreenMeasuredPasses);
        for (var pass = 0; pass < ScreenMeasuredPasses; pass++)
        {
            ResolveR1RotatedPass(candidate, r1Rows, pass, measure: true, screenSamples, ceilings.ResolveMaximumMicroseconds);
        }
        var screenGen0After = GC.CollectionCount(0);
        var screenGen1After = GC.CollectionCount(1);
        var screenGen2After = GC.CollectionCount(2);

        Assert.Equal(20_480, screenSamples.Count);
        WriteR1CallTiming(artifactDirectory, screenSamples);

        var boundarySummaries = BuildBoundarySummaries(r1Rows, screenSamples, ceilings.ResolveMaximumMicroseconds);
        Assert.Equal(320, boundarySummaries.Count);
        WriteBoundarySummaries(artifactDirectory, boundarySummaries);

        var targetBoundaries = boundarySummaries
            .Where(static row => row.CallsOverMaximumCeiling > 0)
            .Concat(boundarySummaries.OrderByDescending(static row => row.P95Microseconds).Take(TargetTopCount))
            .Concat(boundarySummaries.OrderByDescending(static row => row.MaximumMicroseconds).Take(TargetTopCount))
            .GroupBy(static row => row.BoundaryIndex)
            .Select(static group => group.First())
            .OrderByDescending(static row => row.CallsOverMaximumCeiling)
            .ThenByDescending(static row => row.P95Microseconds)
            .ThenByDescending(static row => row.MaximumMicroseconds)
            .ToArray();
        Assert.NotEmpty(targetBoundaries);

        ForceFullCollection();
        var targetGen0Before = GC.CollectionCount(0);
        var targetGen1Before = GC.CollectionCount(1);
        var targetGen2Before = GC.CollectionCount(2);
        var targetRepeats = MeasureTargetBoundaries(candidate, r1Rows, targetBoundaries, ceilings.ResolveMaximumMicroseconds);
        var targetGen0After = GC.CollectionCount(0);
        var targetGen1After = GC.CollectionCount(1);
        var targetGen2After = GC.CollectionCount(2);
        Assert.Equal(targetBoundaries.Length, targetRepeats.Count);
        WriteTargetRepeats(artifactDirectory, targetRepeats);

        var screenMaximum = screenSamples.Max(static row => row.ElapsedMicroseconds);
        var screenCallsOver = screenSamples.Count(static row => row.ExceededMaximumCeiling);
        var screenExceedancesWithGcActivity = screenSamples.Count(static row => row.ExceededMaximumCeiling && row.AnyGcCollectionDelta);
        var targetedMaximum = targetRepeats.Max(static row => row.TargetMaximumMicroseconds);
        var targetedCallsOver = targetRepeats.Sum(static row => row.CallsOverMaximumCeiling);
        var targetedBlocksOver = targetRepeats.Sum(static row => row.BlocksWithExceedance);
        var targetedExceedancesWithGcActivity = targetRepeats.Sum(static row => row.ExceedancesWithGcActivity);
        var currentRunStrictR1ContractMet = screenMaximum <= ceilings.ResolveMaximumMicroseconds
            && targetedMaximum <= ceilings.ResolveMaximumMicroseconds;
        var attribution = ClassifyAttribution(
            screenCallsOver,
            targetedCallsOver,
            targetedBlocksOver,
            targetRepeats.Any(row => row.TargetP95Microseconds > ceilings.ResolveMaximumMicroseconds));

        WriteRuntimeContext(
            artifactDirectory,
            screenGen0After - screenGen0Before,
            screenGen1After - screenGen1Before,
            screenGen2After - screenGen2Before,
            targetGen0After - targetGen0Before,
            targetGen1After - targetGen1Before,
            targetGen2After - targetGen2Before,
            screenExceedancesWithGcActivity,
            targetedExceedancesWithGcActivity);
        WriteAttribution(
            artifactDirectory,
            ceilings,
            screenMaximum,
            screenCallsOver,
            targetedMaximum,
            targetedCallsOver,
            targetedBlocksOver,
            screenExceedancesWithGcActivity,
            targetedExceedancesWithGcActivity,
            targetRepeats.Count,
            currentRunStrictR1ContractMet,
            attribution);
        WriteSummary(
            artifactDirectory,
            screenMaximum,
            screenCallsOver,
            targetedMaximum,
            targetedCallsOver,
            currentRunStrictR1ContractMet,
            attribution,
            boundarySummaries);

        Assert.All(screenSamples, static sample => Assert.True(double.IsFinite(sample.ElapsedMicroseconds) && sample.ElapsedMicroseconds >= 0d));
        Assert.All(targetRepeats, static row => Assert.True(double.IsFinite(row.TargetMaximumMicroseconds) && row.TargetMaximumMicroseconds >= 0d));
    }

    private static void ValidateFrozenRefinement3(string directory)
    {
        var expected = new[]
        {
            "01-contract-and-provenance.txt",
            "02-c3-exact-v9-state-timing.csv",
            "03-c3-tail-target-repeats.csv",
            "04-c3-seam-side-timing.csv",
            "05-c3-performance-qualification.txt",
            "06-rp1b-refinement3-summary.txt",
        };
        foreach (var name in expected)
        {
            Assert.True(File.Exists(Path.Combine(directory, name)), $"Required frozen Refinement 3 artifact missing: {name}");
        }

        var summary = File.ReadAllText(Path.Combine(directory, "06-rp1b-refinement3-summary.txt"), Encoding.UTF8);
        Assert.True(summary.Contains("status=PASS-PERFORMANCE-ATTRIBUTION-EVIDENCE-COMPLETE", StringComparison.Ordinal));
        Assert.True(summary.Contains("candidate-source-changed=False", StringComparison.Ordinal));
        Assert.True(summary.Contains("exact-v9-resolve-max-us=150.5", StringComparison.Ordinal));
        Assert.True(summary.Contains("seam-resolve-max-us=3667.1", StringComparison.Ordinal));
        Assert.True(summary.Contains("strict-performance-contract-met=False", StringComparison.Ordinal));
        Assert.True(summary.Contains("attribution=SEAM-WORST-CASE-BLOCKING", StringComparison.Ordinal));
        Assert.True(summary.Contains("rp1c-selection-authorized=False", StringComparison.Ordinal));

        var seamLines = File.ReadAllLines(Path.Combine(directory, "04-c3-seam-side-timing.csv"), Encoding.UTF8);
        Assert.Equal(6, seamLines.Length);
        Assert.Contains(seamLines, static line => line.StartsWith("R1-SIDE,5120,", StringComparison.Ordinal) && line.Contains(",3667.1,1,", StringComparison.Ordinal));
    }

    private static void ValidateR1PhysicalClosure(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<SeamRow> r1Rows)
    {
        foreach (var row in r1Rows)
        {
            var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
            Assert.True(resolved, $"C3 failed frozen R1 seam boundary {row.BoundaryIndex}.");
            Assert.Equal(row.ReferencePhase, state.Phase);
        }
    }

    private static void ResolveR1RotatedPass(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<SeamRow> rows,
        int pass,
        bool measure,
        List<R1CallTimingSample>? samples,
        double maximumCeilingMicroseconds)
    {
        var startIndex = (pass * RotationStride) % rows.Count;
        for (var offset = 0; offset < rows.Count; offset++)
        {
            var rowIndex = (startIndex + offset) % rows.Count;
            var row = rows[rowIndex];
            if (!measure)
            {
                var warmResolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var warmState);
                Assert.True(warmResolved);
                Assert.Equal(row.ReferencePhase, warmState.Phase);
                continue;
            }

            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);
            var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
            var end = Stopwatch.GetTimestamp();
            var afterBytes = GC.GetAllocatedBytesForCurrentThread();
            var gen0After = GC.CollectionCount(0);
            var gen1After = GC.CollectionCount(1);
            var gen2After = GC.CollectionCount(2);

            Assert.True(resolved);
            Assert.Equal(row.ReferencePhase, state.Phase);
            var elapsed = TicksToMicroseconds(end - start);
            samples!.Add(new R1CallTimingSample(
                row.BoundaryIndex,
                row.BoundaryTemperatureCelsius,
                pass,
                rowIndex,
                elapsed,
                Math.Max(0L, afterBytes - beforeBytes),
                elapsed > maximumCeilingMicroseconds,
                gen0After - gen0Before,
                gen1After - gen1Before,
                gen2After - gen2Before));
        }
    }

    private static IReadOnlyList<R1BoundarySummary> BuildBoundarySummaries(
        IReadOnlyList<SeamRow> rows,
        IReadOnlyList<R1CallTimingSample> samples,
        double maximumCeilingMicroseconds)
    {
        var rowMap = rows.ToDictionary(static row => row.BoundaryIndex);
        return samples
            .GroupBy(static sample => sample.BoundaryIndex)
            .Select(group =>
            {
                var row = rowMap[group.Key];
                var values = group.Select(static sample => sample.ElapsedMicroseconds).ToArray();
                var maxSample = group.OrderByDescending(static sample => sample.ElapsedMicroseconds).First();
                var overCount = group.Count(sample => sample.ElapsedMicroseconds > maximumCeilingMicroseconds);
                return new R1BoundarySummary(
                    row.BoundaryIndex,
                    row.BoundaryTemperatureCelsius,
                    values.Length,
                    Median(values),
                    Percentile(values, 0.95d),
                    values.Max(),
                    maxSample.PassIndex,
                    maxSample.RowIndex,
                    overCount,
                    (double)overCount / values.Length,
                    group.Count(static sample => sample.AnyGcCollectionDelta),
                    group.Count(static sample => sample.ExceededMaximumCeiling && sample.AnyGcCollectionDelta),
                    ClassifyScreenTail(values.Max(), Percentile(values, 0.95d), overCount, maximumCeilingMicroseconds));
            })
            .OrderByDescending(static row => row.CallsOverMaximumCeiling)
            .ThenByDescending(static row => row.P95Microseconds)
            .ThenByDescending(static row => row.MaximumMicroseconds)
            .ToArray();
    }

    private static IReadOnlyList<R1TargetRepeat> MeasureTargetBoundaries(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<SeamRow> rows,
        IReadOnlyList<R1BoundarySummary> targets,
        double maximumCeilingMicroseconds)
    {
        var rowMap = rows.ToDictionary(static row => row.BoundaryIndex);
        var results = new List<R1TargetRepeat>(targets.Count);

        foreach (var target in targets)
        {
            var row = rowMap[target.BoundaryIndex];
            for (var warmup = 0; warmup < TargetWarmupCalls; warmup++)
            {
                var warmResolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var warmState);
                Assert.True(warmResolved);
                Assert.Equal(row.ReferencePhase, warmState.Phase);
            }

            var values = new List<double>(TargetMeasuredBlocks * TargetCallsPerBlock);
            var blocksWithExceedance = 0;
            var exceedancesWithGcActivity = 0;
            var callsWithGcActivity = 0;
            var maximumBlockIndex = -1;
            var maximumCallIndex = -1;
            var maximumValue = double.NegativeInfinity;

            for (var block = 0; block < TargetMeasuredBlocks; block++)
            {
                var blockExceeded = false;
                for (var call = 0; call < TargetCallsPerBlock; call++)
                {
                    var gen0Before = GC.CollectionCount(0);
                    var gen1Before = GC.CollectionCount(1);
                    var gen2Before = GC.CollectionCount(2);
                    var start = Stopwatch.GetTimestamp();
                    var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
                    var end = Stopwatch.GetTimestamp();
                    var gen0After = GC.CollectionCount(0);
                    var gen1After = GC.CollectionCount(1);
                    var gen2After = GC.CollectionCount(2);
                    Assert.True(resolved);
                    Assert.Equal(row.ReferencePhase, state.Phase);

                    var elapsed = TicksToMicroseconds(end - start);
                    values.Add(elapsed);
                    var gcActivity = gen0After != gen0Before || gen1After != gen1Before || gen2After != gen2Before;
                    if (gcActivity) callsWithGcActivity++;
                    if (elapsed > maximumCeilingMicroseconds)
                    {
                        blockExceeded = true;
                        if (gcActivity) exceedancesWithGcActivity++;
                    }
                    if (elapsed > maximumValue)
                    {
                        maximumValue = elapsed;
                        maximumBlockIndex = block;
                        maximumCallIndex = call;
                    }
                }
                if (blockExceeded) blocksWithExceedance++;
            }

            var overCount = values.Count(value => value > maximumCeilingMicroseconds);
            var p95 = Percentile(values, 0.95d);
            results.Add(new R1TargetRepeat(
                row.BoundaryIndex,
                row.BoundaryTemperatureCelsius,
                target.MedianMicroseconds,
                target.P95Microseconds,
                target.MaximumMicroseconds,
                target.CallsOverMaximumCeiling,
                values.Count,
                Median(values),
                p95,
                values.Max(),
                maximumBlockIndex,
                maximumCallIndex,
                overCount,
                blocksWithExceedance,
                callsWithGcActivity,
                exceedancesWithGcActivity,
                ClassifyTargetTail(values.Max(), p95, overCount, blocksWithExceedance, maximumCeilingMicroseconds)));
        }

        return results
            .OrderByDescending(static row => row.CallsOverMaximumCeiling)
            .ThenByDescending(static row => row.TargetP95Microseconds)
            .ThenByDescending(static row => row.TargetMaximumMicroseconds)
            .ToArray();
    }

    private static string ClassifyScreenTail(double maximum, double p95, int overCount, double ceiling)
    {
        if (maximum <= ceiling) return "SCREEN-WITHIN-CEILING";
        if (p95 > ceiling) return "SCREEN-PERSISTENT-SLOW-PATH";
        return overCount == 1 ? "SCREEN-ISOLATED-EXCEEDANCE" : "SCREEN-REPEATED-EXCEEDANCE";
    }

    private static string ClassifyTargetTail(double maximum, double p95, int overCount, int blocksWithExceedance, double ceiling)
    {
        if (maximum <= ceiling) return "TARGETED-WITHIN-CEILING";
        if (p95 > ceiling) return "TARGETED-PERSISTENT-SLOW-PATH";
        if (overCount > 1 || blocksWithExceedance > 1) return "TARGETED-REPEATED-EXCEEDANCE";
        return "TARGETED-ISOLATED-EXCEEDANCE";
    }

    private static string ClassifyAttribution(int screenCallsOver, int targetedCallsOver, int targetedBlocksOver, bool targetedP95Over)
    {
        if (targetedP95Over) return "R1-SEAM-DETERMINISTIC-SLOW-PATH-CONFIRMED";
        if (targetedCallsOver > 1 || targetedBlocksOver > 1) return "R1-SEAM-REPEATED-PERFORMANCE-TAIL-CONFIRMED";
        if (targetedCallsOver == 1) return "R1-SEAM-TARGETED-ISOLATED-EXCEEDANCE";
        if (screenCallsOver > 0) return "R1-SEAM-SCREEN-EXCEEDANCE-NOT-REPRODUCED";
        return "R1-SEAM-HISTORICAL-EXCEEDANCE-NOT-OBSERVED-IN-REFINEMENT4";
    }

    private static void WriteContractAndProvenance(string directory, PerformanceCeilings ceilings)
    {
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT4",
            "scope=C3-R1-SEAM-WORST-CASE-LOCALIZATION-REPRODUCIBILITY",
            "c3-source-identity=UNCHANGED-FROM-REFINEMENT2",
            "c4-created=False",
            "historical-refinement3-r1-side-max-us=3667.1",
            "historical-refinement3-r1-side-exceedance-count=1",
            "screen-warmup-passes=" + ScreenWarmupPasses.ToString(CultureInfo.InvariantCulture),
            "screen-measured-passes=" + ScreenMeasuredPasses.ToString(CultureInfo.InvariantCulture),
            "screen-r1-boundary-count=320",
            "target-top-count=" + TargetTopCount.ToString(CultureInfo.InvariantCulture),
            "target-measured-blocks=" + TargetMeasuredBlocks.ToString(CultureInfo.InvariantCulture),
            "target-calls-per-block=" + TargetCallsPerBlock.ToString(CultureInfo.InvariantCulture),
            "resolve-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "strict-single-call-max-preserved=True",
            "historical-exceedance-not-erased-by-clean-rerun=True",
            "rp1c-selection-authorized=False",
            "c4-implementation-authorized=False",
            "production-src-change-authorized=False",
            "thermodynamic-repair-authorized=False",
            "exact-v9-change-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "01-contract-and-provenance.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteR1CallTiming(string directory, IReadOnlyList<R1CallTimingSample> rows)
    {
        var lines = new List<string>
        {
            "boundary_index,boundary_temperature_c,pass_index,row_index,elapsed_us,allocated_bytes,exceeded_max_ceiling,gc_gen0_delta,gc_gen1_delta,gc_gen2_delta,any_gc_collection_delta"
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", new[]
            {
                row.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
                F(row.BoundaryTemperatureCelsius),
                row.PassIndex.ToString(CultureInfo.InvariantCulture),
                row.RowIndex.ToString(CultureInfo.InvariantCulture),
                F(row.ElapsedMicroseconds),
                row.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                B(row.ExceededMaximumCeiling),
                row.Gen0Delta.ToString(CultureInfo.InvariantCulture),
                row.Gen1Delta.ToString(CultureInfo.InvariantCulture),
                row.Gen2Delta.ToString(CultureInfo.InvariantCulture),
                B(row.AnyGcCollectionDelta),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "02-c3-r1-seam-call-timing.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteBoundarySummaries(string directory, IReadOnlyList<R1BoundarySummary> rows)
    {
        var lines = new List<string>
        {
            "boundary_index,boundary_temperature_c,sample_count,median_us,p95_us,max_us,max_pass_index,max_row_index,calls_over_max_ceiling,exceedance_fraction,calls_with_gc_activity,exceedances_with_gc_activity,screen_classification"
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", new[]
            {
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
                row.ScreenClassification,
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "03-c3-r1-boundary-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTargetRepeats(string directory, IReadOnlyList<R1TargetRepeat> rows)
    {
        var lines = new List<string>
        {
            "boundary_index,boundary_temperature_c,screen_median_us,screen_p95_us,screen_max_us,screen_calls_over_max_ceiling,target_sample_count,target_median_us,target_p95_us,target_max_us,target_max_block_index,target_max_call_index,calls_over_max_ceiling,blocks_with_exceedance,calls_with_gc_activity,exceedances_with_gc_activity,target_classification"
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", new[]
            {
                row.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
                F(row.BoundaryTemperatureCelsius),
                F(row.ScreenMedianMicroseconds),
                F(row.ScreenP95Microseconds),
                F(row.ScreenMaximumMicroseconds),
                row.ScreenCallsOverMaximumCeiling.ToString(CultureInfo.InvariantCulture),
                row.TargetSampleCount.ToString(CultureInfo.InvariantCulture),
                F(row.TargetMedianMicroseconds),
                F(row.TargetP95Microseconds),
                F(row.TargetMaximumMicroseconds),
                row.TargetMaximumBlockIndex.ToString(CultureInfo.InvariantCulture),
                row.TargetMaximumCallIndex.ToString(CultureInfo.InvariantCulture),
                row.CallsOverMaximumCeiling.ToString(CultureInfo.InvariantCulture),
                row.BlocksWithExceedance.ToString(CultureInfo.InvariantCulture),
                row.CallsWithGcActivity.ToString(CultureInfo.InvariantCulture),
                row.ExceedancesWithGcActivity.ToString(CultureInfo.InvariantCulture),
                row.TargetClassification,
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "04-c3-r1-target-repeats.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteRuntimeContext(
        string directory,
        int screenGen0,
        int screenGen1,
        int screenGen2,
        int targetGen0,
        int targetGen1,
        int targetGen2,
        int screenExceedancesWithGc,
        int targetExceedancesWithGc)
    {
        var lines = new[]
        {
            "stopwatch-frequency-hz=" + Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
            "processor-count=" + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            "runtime-version=" + Environment.Version,
            "screen-gc-gen0-collections=" + screenGen0.ToString(CultureInfo.InvariantCulture),
            "screen-gc-gen1-collections=" + screenGen1.ToString(CultureInfo.InvariantCulture),
            "screen-gc-gen2-collections=" + screenGen2.ToString(CultureInfo.InvariantCulture),
            "target-gc-gen0-collections=" + targetGen0.ToString(CultureInfo.InvariantCulture),
            "target-gc-gen1-collections=" + targetGen1.ToString(CultureInfo.InvariantCulture),
            "target-gc-gen2-collections=" + targetGen2.ToString(CultureInfo.InvariantCulture),
            "screen-exceedances-with-gc-activity=" + screenExceedancesWithGc.ToString(CultureInfo.InvariantCulture),
            "target-exceedances-with-gc-activity=" + targetExceedancesWithGc.ToString(CultureInfo.InvariantCulture),
        };
        File.WriteAllLines(Path.Combine(directory, "05-c3-r1-runtime-context.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteAttribution(
        string directory,
        PerformanceCeilings ceilings,
        double screenMaximum,
        int screenCallsOver,
        double targetedMaximum,
        int targetedCallsOver,
        int targetedBlocksOver,
        int screenExceedancesWithGc,
        int targetExceedancesWithGc,
        int targetBoundaryCount,
        bool currentRunStrictR1ContractMet,
        string attribution)
    {
        var lines = new[]
        {
            "candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "historical-refinement3-r1-side-max-us=3667.1",
            "historical-refinement3-r1-side-exceedance-count=1",
            "resolve-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "screen-measured-calls=" + (320 * ScreenMeasuredPasses).ToString(CultureInfo.InvariantCulture),
            "screen-max-us=" + F(screenMaximum),
            "screen-calls-over-max-ceiling=" + screenCallsOver.ToString(CultureInfo.InvariantCulture),
            "screen-exceedances-with-gc-activity=" + screenExceedancesWithGc.ToString(CultureInfo.InvariantCulture),
            "target-boundary-count=" + targetBoundaryCount.ToString(CultureInfo.InvariantCulture),
            "targeted-max-us=" + F(targetedMaximum),
            "targeted-calls-over-max-ceiling=" + targetedCallsOver.ToString(CultureInfo.InvariantCulture),
            "targeted-blocks-with-exceedance=" + targetedBlocksOver.ToString(CultureInfo.InvariantCulture),
            "targeted-exceedances-with-gc-activity=" + targetExceedancesWithGc.ToString(CultureInfo.InvariantCulture),
            "current-run-strict-r1-seam-contract-met=" + B(currentRunStrictR1ContractMet),
            "historical-refinement3-exceedance-preserved=True",
            "attribution=" + attribution,
            "strict-single-call-max-preserved=True",
            "rp1c-selection-authorized=False",
            "c4-created=False",
        };
        File.WriteAllLines(Path.Combine(directory, "06-c3-r1-performance-attribution.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(
        string directory,
        double screenMaximum,
        int screenCallsOver,
        double targetedMaximum,
        int targetedCallsOver,
        bool currentRunStrictR1ContractMet,
        string attribution,
        IReadOnlyList<R1BoundarySummary> boundarySummaries)
    {
        var worst = boundarySummaries
            .OrderByDescending(static row => row.MaximumMicroseconds)
            .First();
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT4",
            "status=PASS-R1-SEAM-LOCALIZATION-EVIDENCE-COMPLETE",
            "candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "candidate-source-changed=False",
            "c4-created=False",
            "r1-boundary-count=320",
            "screen-max-us=" + F(screenMaximum),
            "screen-calls-over-max-ceiling=" + screenCallsOver.ToString(CultureInfo.InvariantCulture),
            "screen-worst-boundary-index=" + worst.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
            "screen-worst-boundary-temperature-c=" + F(worst.BoundaryTemperatureCelsius),
            "targeted-max-us=" + F(targetedMaximum),
            "targeted-calls-over-max-ceiling=" + targetedCallsOver.ToString(CultureInfo.InvariantCulture),
            "current-run-strict-r1-seam-contract-met=" + B(currentRunStrictR1ContractMet),
            "historical-refinement3-r1-side-exceedance-preserved=True",
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
            "next-action=Return complete Refinement 4 artifacts for engineering review before RP1C or C4.",
        };
        File.WriteAllLines(Path.Combine(directory, "07-rp1b-refinement4-summary.txt"), lines, Utf8WithoutBom);
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
                D(parts[1]),
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

    private sealed record SeamRow(
        int BoundaryIndex,
        double BoundaryTemperatureCelsius,
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

    private sealed record R1CallTimingSample(
        int BoundaryIndex,
        double BoundaryTemperatureCelsius,
        int PassIndex,
        int RowIndex,
        double ElapsedMicroseconds,
        long AllocatedBytes,
        bool ExceededMaximumCeiling,
        int Gen0Delta,
        int Gen1Delta,
        int Gen2Delta)
    {
        public bool AnyGcCollectionDelta => Gen0Delta != 0 || Gen1Delta != 0 || Gen2Delta != 0;
    }

    private sealed record R1BoundarySummary(
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
        string ScreenClassification);

    private sealed record R1TargetRepeat(
        int BoundaryIndex,
        double BoundaryTemperatureCelsius,
        double ScreenMedianMicroseconds,
        double ScreenP95Microseconds,
        double ScreenMaximumMicroseconds,
        int ScreenCallsOverMaximumCeiling,
        int TargetSampleCount,
        double TargetMedianMicroseconds,
        double TargetP95Microseconds,
        double TargetMaximumMicroseconds,
        int TargetMaximumBlockIndex,
        int TargetMaximumCallIndex,
        int CallsOverMaximumCeiling,
        int BlocksWithExceedance,
        int CallsWithGcActivity,
        int ExceedancesWithGcActivity,
        string TargetClassification);
}
