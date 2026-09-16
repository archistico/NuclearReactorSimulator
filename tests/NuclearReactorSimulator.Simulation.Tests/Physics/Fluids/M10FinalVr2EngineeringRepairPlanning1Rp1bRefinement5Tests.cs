using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Engineering Repair Planning 1 / RP1B Refinement 5. Diagnostic-only cross-process
/// reproducibility study of immutable C3 on the frozen 320-boundary R1 seam corpus. Each test
/// invocation is one independent process run. C3 remains unchanged; no C4, RP1C selection or
/// production change is authorized by this evidence-generation test.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5";
    private const string RunIndexEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement5";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const int IndependentProcessRuns = 5;
    private const int ScreenWarmupPasses = 16;
    private const int ScreenMeasuredPasses = 64;
    private const int RotationStride = 37;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5")]
    public void Rp1bRefinement5_MeasuresImmutableC3R1SeamAcrossOneIndependentProcessRun()
    {
        RequireOptIn();
        var runIndex = ReadRunIndex();
        var root = FindRepositoryRoot();
        var runDirectory = ResetRunDirectory(root, runIndex);
        var frozenRp1aDirectory = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));

        var allSeamRows = LoadSeamRows(Path.Combine(frozenRp1aDirectory, "05-seam-probe-map.csv"));
        var r1Rows = allSeamRows
            .Where(static row => row.ProbeSide == "R1-SIDE")
            .OrderBy(static row => row.BoundaryIndex)
            .ToArray();
        var ceilings = LoadPerformanceCeilings(Path.Combine(frozenRp1aDirectory, "06-performance-baseline.csv"));

        Assert.Equal(1_280, allSeamRows.Count);
        Assert.Equal(320, r1Rows.Length);
        Assert.Equal(320, r1Rows.Select(static row => row.BoundaryIndex).Distinct().Count());

        var candidate = new Rp1bVaporSeamCompleteTabulatedSurrogateCandidate();
        Assert.Equal("C3-VAPOR-SEAM-COMPLETE-SURROGATE", candidate.CandidateId);
        Assert.False(candidate.UsesDirectIf97AtResolveTime);
        ValidateR1PhysicalClosure(candidate, r1Rows);
        WriteProcessContract(runDirectory, runIndex, ceilings);

        for (var pass = 0; pass < ScreenWarmupPasses; pass++)
        {
            ResolveR1RotatedPass(candidate, r1Rows, pass, measure: false, samples: null, ceilings.ResolveMaximumMicroseconds);
        }

        ForceFullCollection();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        var samples = new List<R1CallTimingSample>(r1Rows.Length * ScreenMeasuredPasses);
        for (var pass = 0; pass < ScreenMeasuredPasses; pass++)
        {
            ResolveR1RotatedPass(candidate, r1Rows, pass, measure: true, samples, ceilings.ResolveMaximumMicroseconds);
        }
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        Assert.Equal(20_480, samples.Count);
        WriteCallTiming(runDirectory, samples);

        var boundarySummaries = BuildBoundarySummaries(r1Rows, samples, ceilings.ResolveMaximumMicroseconds);
        Assert.Equal(320, boundarySummaries.Count);
        WriteBoundarySummaries(runDirectory, boundarySummaries);

        var maximumSample = samples.OrderByDescending(static row => row.ElapsedMicroseconds).First();
        var callsOver = samples.Count(static row => row.ExceededMaximumCeiling);
        var boundariesOver = boundarySummaries.Count(static row => row.CallsOverMaximumCeiling > 0);
        var exceedancesWithGc = samples.Count(static row => row.ExceededMaximumCeiling && row.AnyGcCollectionDelta);
        WriteRuntimeContext(
            runDirectory,
            runIndex,
            gen0After - gen0Before,
            gen1After - gen1Before,
            gen2After - gen2Before,
            exceedancesWithGc);
        WriteProcessSummary(
            runDirectory,
            runIndex,
            ceilings.ResolveMaximumMicroseconds,
            maximumSample,
            callsOver,
            boundariesOver,
            exceedancesWithGc);

        Assert.All(samples, static sample => Assert.True(double.IsFinite(sample.ElapsedMicroseconds) && sample.ElapsedMicroseconds >= 0d));
        Assert.All(boundarySummaries, static row => Assert.Equal(64, row.SampleCount));
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
                    group.Count(static sample => sample.ExceededMaximumCeiling && sample.AnyGcCollectionDelta));
            })
            .OrderBy(static row => row.BoundaryIndex)
            .ToArray();
    }

    private static void WriteProcessContract(string directory, int runIndex, PerformanceCeilings ceilings)
    {
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT5",
            "scope=C3-CROSS-PROCESS-WALL-CLOCK-TAIL-REPRODUCIBILITY",
            "candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "candidate-source-changed=False",
            "process-run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "independent-process-run-count=" + IndependentProcessRuns.ToString(CultureInfo.InvariantCulture),
            "r1-boundary-count=320",
            "warmup-passes=16",
            "measured-passes=64",
            "measured-calls=20480",
            "resolve-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "strict-single-call-max-preserved=True",
            "c4-created=False",
            "rp1c-selection-authorized=False",
            "production-src-change-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "01-process-contract.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteCallTiming(string directory, IReadOnlyList<R1CallTimingSample> samples)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "02-c3-r1-call-timing.csv"), append: false, Utf8WithoutBom);
        writer.WriteLine("boundary_index,boundary_temperature_c,pass_index,row_index,elapsed_us,allocated_bytes,exceeded_max_ceiling,gen0_delta,gen1_delta,gen2_delta,any_gc_collection_delta");
        foreach (var row in samples)
        {
            writer.WriteLine(string.Join(",",
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
                B(row.AnyGcCollectionDelta)));
        }
    }

    private static void WriteBoundarySummaries(string directory, IReadOnlyList<R1BoundarySummary> rows)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "03-c3-r1-boundary-summary.csv"), append: false, Utf8WithoutBom);
        writer.WriteLine("boundary_index,boundary_temperature_c,sample_count,median_us,p95_us,max_us,max_pass_index,max_row_index,calls_over_max_ceiling,exceedance_fraction,calls_with_gc_activity,exceedances_with_gc_activity");
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
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
                row.ExceedancesWithGcActivity.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void WriteRuntimeContext(
        string directory,
        int runIndex,
        int gen0Collections,
        int gen1Collections,
        int gen2Collections,
        int exceedancesWithGcActivity)
    {
        var lines = new[]
        {
            "process-run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "process-id=" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
            "stopwatch-frequency-hz=" + Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
            "processor-count=" + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            "runtime-version=" + Environment.Version,
            "gc-gen0-collections=" + gen0Collections.ToString(CultureInfo.InvariantCulture),
            "gc-gen1-collections=" + gen1Collections.ToString(CultureInfo.InvariantCulture),
            "gc-gen2-collections=" + gen2Collections.ToString(CultureInfo.InvariantCulture),
            "exceedances-with-gc-activity=" + exceedancesWithGcActivity.ToString(CultureInfo.InvariantCulture),
        };
        File.WriteAllLines(Path.Combine(directory, "04-runtime-context.txt"), lines, Utf8WithoutBom);
    }

    private static void WriteProcessSummary(
        string directory,
        int runIndex,
        double maximumCeilingMicroseconds,
        R1CallTimingSample maximumSample,
        int callsOver,
        int boundariesOver,
        int exceedancesWithGcActivity)
    {
        var lines = new[]
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT5",
            "status=PASS-PROCESS-EVIDENCE-COMPLETE",
            "candidate=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "candidate-source-changed=False",
            "process-run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "measured-calls=20480",
            "resolve-max-ceiling-us=" + F(maximumCeilingMicroseconds),
            "process-max-us=" + F(maximumSample.ElapsedMicroseconds),
            "process-max-boundary-index=" + maximumSample.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
            "process-max-boundary-temperature-c=" + F(maximumSample.BoundaryTemperatureCelsius),
            "process-max-pass-index=" + maximumSample.PassIndex.ToString(CultureInfo.InvariantCulture),
            "calls-over-max-ceiling=" + callsOver.ToString(CultureInfo.InvariantCulture),
            "boundaries-over-max-ceiling=" + boundariesOver.ToString(CultureInfo.InvariantCulture),
            "exceedances-with-gc-activity=" + exceedancesWithGcActivity.ToString(CultureInfo.InvariantCulture),
            "strict-single-call-max-preserved=True",
            "c4-created=False",
            "rp1c-selection-authorized=False",
        };
        File.WriteAllLines(Path.Combine(directory, "05-process-summary.txt"), lines, Utf8WithoutBom);
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
        return new PerformanceCeilings(D(values["rp1b_candidate_resolve_max_ceiling_us"]));
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

    private static int ReadRunIndex()
    {
        var value = Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable);
        Assert.True(int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runIndex));
        Assert.InRange(runIndex, 1, IndependentProcessRuns);
        return runIndex;
    }

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;
    private static double D(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string B(bool value) => value ? "True" : "False";

    private static string ResetRunDirectory(string root, int runIndex)
    {
        var rootPath = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(rootPath);
        var runPath = Path.Combine(rootPath, $"process-{runIndex:D2}");
        if (Directory.Exists(runPath)) Directory.Delete(runPath, recursive: true);
        Directory.CreateDirectory(runPath);
        return runPath;
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
        string ReferencePhase,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record PerformanceCeilings(double ResolveMaximumMicroseconds);

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
        int ExceedancesWithGcActivity);
}
