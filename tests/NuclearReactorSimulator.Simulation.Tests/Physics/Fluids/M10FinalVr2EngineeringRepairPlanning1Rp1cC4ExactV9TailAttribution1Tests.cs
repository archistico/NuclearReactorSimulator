using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// RP1C C4 Exact-v9 Wall-Clock Tail Attribution 1. Evidence-only exact-v9 timing under four
/// explicitly controlled runtime compilation modes. The test records comparative evidence only:
/// it does not promote a causal interpretation, select C4, mutate C4, or authorize production repair.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_TAIL_ATTRIBUTION1";
    private const string ModeEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_MODE";
    private const string RunIndexEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_RUN_INDEX";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const int ExactRowCount = 360;
    private const int WarmupPasses = 16;
    private const int MeasuredPasses = 64;
    private const int MeasuredCalls = ExactRowCount * MeasuredPasses;
    private const int RotationStride = 37;
    private const double DiagnosticTailFloorMicroseconds = 100d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1")]
    public void Rp1cC4ExactV9TailAttribution1_MeasuresOneFreshRuntimeModeProcess()
    {
        RequireOptIn();
        var mode = GetMode(Environment.GetEnvironmentVariable(ModeEnvironmentVariable));
        var runIndex = ParseRunIndex(Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable));
        ValidateRuntimeEnvironment(mode);

        var root = FindRepositoryRoot();
        var frozenRp1a = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var exactRows = LoadExactRows(Path.Combine(frozenRp1a, "03-exact-v9-node-corpus.csv"));
        var ceilings = LoadPerformanceCeilings(Path.Combine(frozenRp1a, "06-performance-baseline.csv"));
        Assert.Equal(ExactRowCount, exactRows.Length);

        var candidate = new Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate();
        Assert.Equal("C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE", candidate.CandidateId);
        Assert.Equal("C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE", candidate.FamilyId);
        Assert.Equal(48, candidate.MaximumIterativeSolveIterations);
        Assert.False(candidate.UsesDirectIf97AtResolveTime);

        for (var pass = 0; pass < WarmupPasses; pass++)
        {
            ResolveWarmup(candidate, exactRows, pass);
        }

        PrimeTimingHarness(candidate, exactRows[0]);
        var samples = new TimingSample[MeasuredCalls];
        ForceFullCollection();

        var regionBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var regionGen0Before = GC.CollectionCount(0);
        var regionGen1Before = GC.CollectionCount(1);
        var regionGen2Before = GC.CollectionCount(2);
        long candidateAllocatedBytes = 0;

        var sampleIndex = 0;
        for (var pass = 0; pass < MeasuredPasses; pass++)
        {
            var startIndex = (pass * RotationStride) % ExactRowCount;
            for (var offset = 0; offset < ExactRowCount; offset++)
            {
                var rowIndex = (startIndex + offset) % ExactRowCount;
                var row = exactRows[rowIndex];
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp();
                var resolved = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out var path);
                var end = Stopwatch.GetTimestamp();
                var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                var allocated = Math.Max(0L, allocatedAfter - allocatedBefore);
                candidateAllocatedBytes += allocated;
                samples[sampleIndex++] = new TimingSample(
                    pass,
                    rowIndex,
                    TicksToMicroseconds(end - start),
                    allocated,
                    resolved,
                    path);
            }
        }

        var regionGen0After = GC.CollectionCount(0);
        var regionGen1After = GC.CollectionCount(1);
        var regionGen2After = GC.CollectionCount(2);
        var regionBytesAfter = GC.GetAllocatedBytesForCurrentThread();
        var wholeRegionAllocatedBytes = Math.Max(0L, regionBytesAfter - regionBytesBefore);
        var harnessAllocatedBytes = Math.Max(0L, wholeRegionAllocatedBytes - candidateAllocatedBytes);

        Assert.Equal(MeasuredCalls, sampleIndex);
        ValidateFiniteSamples(samples);

        var artifactDirectory = EnsureArtifactDirectory(root);
        var processDirectory = ResetOwnedProcessDirectory(artifactDirectory, mode.Id, runIndex);
        WriteProcessContract(processDirectory, mode, runIndex, candidate, ceilings);
        WriteExactTiming(processDirectory, mode, runIndex, exactRows, samples, ceilings.ResolveMaximumMicroseconds);
        WriteRuntimeContext(
            processDirectory,
            mode,
            runIndex,
            wholeRegionAllocatedBytes,
            candidateAllocatedBytes,
            harnessAllocatedBytes,
            regionGen0After - regionGen0Before,
            regionGen1After - regionGen1Before,
            regionGen2After - regionGen2Before);
        WriteProcessSummary(
            processDirectory,
            mode,
            runIndex,
            samples,
            candidateAllocatedBytes,
            harnessAllocatedBytes,
            regionGen0After - regionGen0Before,
            regionGen1After - regionGen1Before,
            regionGen2After - regionGen2Before,
            ceilings);
    }

    private static void ResolveWarmup(
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate,
        ExactRow[] rows,
        int pass)
    {
        var startIndex = (pass * RotationStride) % rows.Length;
        for (var offset = 0; offset < rows.Length; offset++)
        {
            var row = rows[(startIndex + offset) % rows.Length];
            _ = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out _);
        }
    }

    private static void PrimeTimingHarness(
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate,
        ExactRow row)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        _ = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out _);
        var end = Stopwatch.GetTimestamp();
        var after = GC.GetAllocatedBytesForCurrentThread();
        _ = Math.Max(0L, after - before);
        _ = TicksToMicroseconds(end - start);
        _ = GC.CollectionCount(0);
        _ = GC.CollectionCount(1);
        _ = GC.CollectionCount(2);
        _ = new TimingSample(0, 0, 0d, 0L, true, Rp1bC4ResolutionPath.Unresolved);
    }

    private static void ValidateFiniteSamples(TimingSample[] samples)
    {
        for (var index = 0; index < samples.Length; index++)
        {
            Assert.True(double.IsFinite(samples[index].ElapsedMicroseconds));
            Assert.True(samples[index].ElapsedMicroseconds >= 0d);
            Assert.True(samples[index].AllocatedBytes >= 0L);
        }
    }

    private static void WriteProcessContract(
        string directory,
        ModeSpec mode,
        int runIndex,
        Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate,
        PerformanceCeilings ceilings)
    {
        File.WriteAllLines(Path.Combine(directory, "01-process-contract.txt"), new[]
        {
            "gate=RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1",
            "status=PASS-PROCESS-CONTRACT",
            "mode-id=" + mode.Id,
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "candidate=" + candidate.CandidateId,
            "candidate-family=" + candidate.FamilyId,
            "candidate-source-mutation=False",
            "exact-v9-rows=360",
            "warmup-passes=16",
            "measured-passes=64",
            "measured-calls=23040",
            "rotation-stride=37",
            "strict-max-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "diagnostic-tail-floor-us=100",
            "diagnostic-tail-floor-is-qualification-threshold=False",
            "harness-allocation-neutral-required=True",
            "automatic-causal-promotion=False",
            "automatic-selection-authorized=False",
            "rp1c-selection-authorized=False",
            "production-repair-authorized=False",
        }, Utf8WithoutBom);
    }

    private static void WriteExactTiming(
        string directory,
        ModeSpec mode,
        int runIndex,
        ExactRow[] rows,
        TimingSample[] samples,
        double strictMaximumMicroseconds)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "02-exact-v9-call-timing.csv"), false, Utf8WithoutBom);
        writer.WriteLine("mode_id,run_index,pass_index,row_index,probe_id,logical_step,node_id,elapsed_us,allocated_bytes,resolved,resolution_path,over_100us,over_max_ceiling");
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var row = rows[sample.RowIndex];
            writer.WriteLine(string.Join(',',
                mode.Id,
                runIndex.ToString(CultureInfo.InvariantCulture),
                sample.PassIndex.ToString(CultureInfo.InvariantCulture),
                sample.RowIndex.ToString(CultureInfo.InvariantCulture),
                row.ProbeId,
                row.LogicalStep.ToString(CultureInfo.InvariantCulture),
                row.NodeId,
                F(sample.ElapsedMicroseconds),
                sample.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                B(sample.Resolved),
                PathText(sample.ResolutionPath),
                B(sample.ElapsedMicroseconds > DiagnosticTailFloorMicroseconds),
                B(sample.ElapsedMicroseconds > strictMaximumMicroseconds)));
        }
    }

    private static void WriteRuntimeContext(
        string directory,
        ModeSpec mode,
        int runIndex,
        long wholeRegionAllocatedBytes,
        long candidateAllocatedBytes,
        long harnessAllocatedBytes,
        int gen0,
        int gen1,
        int gen2)
    {
        File.WriteAllLines(Path.Combine(directory, "03-runtime-context.txt"), new[]
        {
            "status=PASS-RUNTIME-CONTEXT-CAPTURED",
            "mode-id=" + mode.Id,
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "framework=" + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            "os=" + System.Runtime.InteropServices.RuntimeInformation.OSDescription.Replace('\r', ' ').Replace('\n', ' '),
            "process-architecture=" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
            "process-id=" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
            "processor-count=" + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            "managed-thread-id-after-region=" + Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture),
            "server-gc=" + B(System.Runtime.GCSettings.IsServerGC),
            "gc-latency-mode=" + System.Runtime.GCSettings.LatencyMode,
            "stopwatch-frequency=" + Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
            "DOTNET_TieredCompilation=" + EnvText(Environment.GetEnvironmentVariable("DOTNET_TieredCompilation")),
            "DOTNET_TieredPGO=" + EnvText(Environment.GetEnvironmentVariable("DOTNET_TieredPGO")),
            "DOTNET_TC_QuickJit=" + EnvText(Environment.GetEnvironmentVariable("DOTNET_TC_QuickJit")),
            "DOTNET_TC_QuickJitForLoops=" + EnvText(Environment.GetEnvironmentVariable("DOTNET_TC_QuickJitForLoops")),
            "DOTNET_ReadyToRun=" + EnvText(Environment.GetEnvironmentVariable("DOTNET_ReadyToRun")),
            "whole-measured-region-allocated-bytes=" + wholeRegionAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "candidate-call-allocated-bytes-sum=" + candidateAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "harness-allocated-bytes=" + harnessAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "gc-gen0-collections-during-measured-region=" + gen0.ToString(CultureInfo.InvariantCulture),
            "gc-gen1-collections-during-measured-region=" + gen1.ToString(CultureInfo.InvariantCulture),
            "gc-gen2-collections-during-measured-region=" + gen2.ToString(CultureInfo.InvariantCulture),
        }, Utf8WithoutBom);
    }

    private static void WriteProcessSummary(
        string directory,
        ModeSpec mode,
        int runIndex,
        TimingSample[] samples,
        long candidateAllocatedBytes,
        long harnessAllocatedBytes,
        int gen0,
        int gen1,
        int gen2,
        PerformanceCeilings ceilings)
    {
        var elapsed = new double[samples.Length];
        var allocated = new double[samples.Length];
        var unresolved = 0;
        var over100 = 0;
        var overMax = 0;
        for (var index = 0; index < samples.Length; index++)
        {
            elapsed[index] = samples[index].ElapsedMicroseconds;
            allocated[index] = samples[index].AllocatedBytes;
            if (!samples[index].Resolved) unresolved++;
            if (samples[index].ElapsedMicroseconds > DiagnosticTailFloorMicroseconds) over100++;
            if (samples[index].ElapsedMicroseconds > ceilings.ResolveMaximumMicroseconds) overMax++;
        }
        Array.Sort(elapsed);
        Array.Sort(allocated);

        File.WriteAllLines(Path.Combine(directory, "04-process-summary.txt"), new[]
        {
            "status=PASS-PROCESS-EVIDENCE-COMPLETE",
            "mode-id=" + mode.Id,
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "measured-calls=" + samples.Length.ToString(CultureInfo.InvariantCulture),
            "median-us=" + F(MedianSorted(elapsed)),
            "p95-us=" + F(PercentileSorted(elapsed, 0.95d)),
            "max-us=" + F(elapsed[^1]),
            "median-allocated-bytes=" + F(MedianSorted(allocated)),
            "unresolved-calls=" + unresolved.ToString(CultureInfo.InvariantCulture),
            "calls-over-100us=" + over100.ToString(CultureInfo.InvariantCulture),
            "calls-over-max-ceiling=" + overMax.ToString(CultureInfo.InvariantCulture),
            "candidate-call-allocated-bytes-sum=" + candidateAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "harness-allocated-bytes=" + harnessAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "gc-gen0-collections-during-measured-region=" + gen0.ToString(CultureInfo.InvariantCulture),
            "gc-gen1-collections-during-measured-region=" + gen1.ToString(CultureInfo.InvariantCulture),
            "gc-gen2-collections-during-measured-region=" + gen2.ToString(CultureInfo.InvariantCulture),
            "strict-max-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "diagnostic-tail-floor-us=100",
            "automatic-causal-promotion=False",
        }, Utf8WithoutBom);
    }

    private static ExactRow[] LoadExactRows(string path)
    {
        const string header = "probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        var rows = new ExactRow[lines.Length];
        for (var index = 0; index < lines.Length; index++)
        {
            var parts = lines[index].Split(',');
            Assert.Equal(17, parts.Length);
            var density = D(parts[6]);
            Assert.True(density > 0d);
            Assert.True(bool.Parse(parts[10]));
            rows[index] = new ExactRow(
                parts[0],
                long.Parse(parts[1], CultureInfo.InvariantCulture),
                parts[3],
                1d / density,
                D(parts[7]));
        }
        return rows;
    }

    private static PerformanceCeilings LoadPerformanceCeilings(string path)
    {
        var values = File.ReadAllLines(path, Encoding.UTF8)
            .Skip(1)
            .Where(static line => line.Contains(','))
            .Select(static line => line.Split(',', 2))
            .Where(static parts => parts.Length == 2)
            .ToDictionary(static parts => parts[0], static parts => parts[1], StringComparer.Ordinal);
        return new PerformanceCeilings(
            D(values["rp1b_candidate_resolve_median_ceiling_us"]),
            D(values["rp1b_candidate_resolve_p95_ceiling_us"]),
            D(values["rp1b_candidate_resolve_max_ceiling_us"]),
            D(values["rp1b_candidate_resolve_median_allocated_bytes_ceiling"]));
    }

    private static string[] ReadCsvLines(string path, string expectedHeader)
    {
        Assert.True(File.Exists(path), $"Required frozen RP1A artifact missing: {path}");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.NotEmpty(lines);
        Assert.Equal(expectedHeader, lines[0]);
        return lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    private static ModeSpec GetMode(string? value) => value switch
    {
        "AMBIENT-UNSET-CONTROL" => new ModeSpec("AMBIENT-UNSET-CONTROL", null, null, null, null, null),
        "TIERING-OFF" => new ModeSpec("TIERING-OFF", "0", "0", "0", "0", "1"),
        "TIERING-ON-PGO-OFF" => new ModeSpec("TIERING-ON-PGO-OFF", "1", "0", "1", "0", "1"),
        "TIERING-ON-PGO-ON" => new ModeSpec("TIERING-ON-PGO-ON", "1", "1", "1", "0", "1"),
        _ => throw new InvalidOperationException("Unknown or missing tail attribution runtime mode: " + (value ?? "<null>")),
    };

    private static void ValidateRuntimeEnvironment(ModeSpec mode)
    {
        Assert.Equal(mode.TieredCompilation, Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"));
        Assert.Equal(mode.TieredPgo, Environment.GetEnvironmentVariable("DOTNET_TieredPGO"));
        Assert.Equal(mode.QuickJit, Environment.GetEnvironmentVariable("DOTNET_TC_QuickJit"));
        Assert.Equal(mode.QuickJitForLoops, Environment.GetEnvironmentVariable("DOTNET_TC_QuickJitForLoops"));
        Assert.Equal(mode.ReadyToRun, Environment.GetEnvironmentVariable("DOTNET_ReadyToRun"));
    }

    private static string PathText(Rp1bC4ResolutionPath path) => path switch
    {
        Rp1bC4ResolutionPath.Unresolved => "UNRESOLVED",
        Rp1bC4ResolutionPath.C3SuperheatedVaporSeam => "C3-SUPERHEATED-VAPOR-SEAM",
        Rp1bC4ResolutionPath.C2MixturePrefix => "C2-MIXTURE-PREFIX",
        Rp1bC4ResolutionPath.C2LiquidTablePrefix => "C2-LIQUID-TABLE-PREFIX",
        Rp1bC4ResolutionPath.C2NearBoundaryLiquidPrefix => "C2-NEAR-BOUNDARY-LIQUID-PREFIX",
        Rp1bC4ResolutionPath.ImmutableC2Fallback => "IMMUTABLE-C2-FALLBACK",
        Rp1bC4ResolutionPath.C3SaturatedVaporSeamFallback => "C3-SATURATED-VAPOR-SEAM-FALLBACK",
        _ => "UNKNOWN",
    };

    private static double MedianSorted(double[] sorted)
    {
        if (sorted.Length == 0) return double.NaN;
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? 0.5d * (sorted[middle - 1] + sorted[middle])
            : sorted[middle];
    }

    private static double PercentileSorted(double[] sorted, double percentile)
    {
        if (sorted.Length == 0) return double.NaN;
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static int ParseRunIndex(string? value)
    {
        Assert.True(int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var runIndex));
        Assert.InRange(runIndex, 1, 5);
        return runIndex;
    }

    private static void RequireOptIn() =>
        Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));

    private static string EnsureArtifactDirectory(string root)
    {
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string ResetOwnedProcessDirectory(string artifactDirectory, string modeId, int runIndex)
    {
        var modeDirectory = Path.Combine(artifactDirectory, "mode-" + modeId);
        Directory.CreateDirectory(modeDirectory);
        var path = Path.Combine(modeDirectory, "process-" + runIndex.ToString("00", CultureInfo.InvariantCulture));
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate NuclearReactorSimulator.sln from test base directory.");
    }

    private static double TicksToMicroseconds(long ticks) =>
        ticks * 1_000_000d / Stopwatch.Frequency;

    private static double D(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string F(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static string B(bool value) =>
        value ? "True" : "False";

    private static string EnvText(string? value) =>
        value ?? "UNSET";

    private readonly record struct ExactRow(
        string ProbeId,
        long LogicalStep,
        string NodeId,
        double SpecificVolume,
        double SpecificEnergy);

    private readonly record struct PerformanceCeilings(
        double ResolveMedianMicroseconds,
        double ResolveP95Microseconds,
        double ResolveMaximumMicroseconds,
        double ResolveMedianAllocatedBytes);

    private readonly record struct ModeSpec(
        string Id,
        string? TieredCompilation,
        string? TieredPgo,
        string? QuickJit,
        string? QuickJitForLoops,
        string? ReadyToRun);

    private readonly struct TimingSample
    {
        public TimingSample(
            int passIndex,
            int rowIndex,
            double elapsedMicroseconds,
            long allocatedBytes,
            bool resolved,
            Rp1bC4ResolutionPath resolutionPath)
        {
            PassIndex = passIndex;
            RowIndex = rowIndex;
            ElapsedMicroseconds = elapsedMicroseconds;
            AllocatedBytes = allocatedBytes;
            Resolved = resolved;
            ResolutionPath = resolutionPath;
        }

        public int PassIndex { get; }
        public int RowIndex { get; }
        public double ElapsedMicroseconds { get; }
        public long AllocatedBytes { get; }
        public bool Resolved { get; }
        public Rp1bC4ResolutionPath ResolutionPath { get; }
    }
}
