using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// RP1C C4 Full-Domain Performance Confirmation 2. Performance-only evidence over the immutable
/// C4 candidate and frozen RP1A exact-v9/seam corpora. Negative performance outcomes are written as
/// engineering evidence and do not fail the xUnit harness. No RP1C selection or production repair is performed.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation2Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERF2";
    private const string RunIndexEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_PERF_RUN_INDEX";
    private const string HostFingerprintEnvironmentVariable = "NRS_FDPC2_HOST_FINGERPRINT_SHA256";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const int ExactRowCount = 360;
    private const int ExactWarmupPasses = 16;
    private const int ExactMeasuredPasses = 64;
    private const int ExactMeasuredCalls = ExactRowCount * ExactMeasuredPasses;
    private const int SeamRowCount = 1_280;
    private const int SeamWarmupPasses = 4;
    private const int SeamMeasuredPasses = 16;
    private const int SeamMeasuredCalls = SeamRowCount * SeamMeasuredPasses;
    private const int TotalMeasuredCalls = ExactMeasuredCalls + SeamMeasuredCalls;
    private const int RotationStride = 37;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation2")]
    public void Rp1cC4FullDomainPerformanceConfirmation2_MeasuresOneFreshProcess()
    {
        RequireOptIn();
        var runIndex = ParseRunIndex(Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable));
        var hostFingerprint = RequireHostFingerprint();
        RequireAmbientRuntimeControlsUnset();
        var root = FindRepositoryRoot();
        var frozenRp1a = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var exactRows = LoadExactRows(Path.Combine(frozenRp1a, "03-exact-v9-node-corpus.csv"));
        var seamRows = LoadSeamRows(Path.Combine(frozenRp1a, "05-seam-probe-map.csv"));
        var ceilings = LoadPerformanceCeilings(Path.Combine(frozenRp1a, "06-performance-baseline.csv"));

        Assert.Equal(ExactRowCount, exactRows.Length);
        Assert.Equal(SeamRowCount, seamRows.Length);
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSideCode == 1));
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSideCode == 2));
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSideCode == 3));
        Assert.Equal(320, seamRows.Count(static row => row.ProbeSideCode == 4));

        var candidate = new Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate();
        Assert.Equal("C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE", candidate.CandidateId);
        Assert.Equal("C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE", candidate.FamilyId);
        Assert.Equal(48, candidate.MaximumIterativeSolveIterations);
        Assert.False(candidate.UsesDirectIf97AtResolveTime);

        for (var pass = 0; pass < ExactWarmupPasses; pass++) ResolveExactWarmup(candidate, exactRows, pass);
        for (var pass = 0; pass < SeamWarmupPasses; pass++) ResolveSeamWarmup(candidate, seamRows, pass);

        PrimeTimingHarness(candidate, exactRows[0], seamRows[0]);
        var exactSamples = new TimingSample[ExactMeasuredCalls];
        var seamSamples = new TimingSample[SeamMeasuredCalls];
        ForceFullCollection();

        var regionBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var regionGen0Before = GC.CollectionCount(0);
        var regionGen1Before = GC.CollectionCount(1);
        var regionGen2Before = GC.CollectionCount(2);
        long candidateAllocatedBytes = 0;

        var exactIndex = 0;
        for (var pass = 0; pass < ExactMeasuredPasses; pass++)
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
                exactSamples[exactIndex++] = new TimingSample(pass, rowIndex, TicksToMicroseconds(end - start), allocated, resolved, path, 0);
            }
        }

        var seamIndex = 0;
        for (var pass = 0; pass < SeamMeasuredPasses; pass++)
        {
            var startIndex = (pass * RotationStride) % SeamRowCount;
            for (var offset = 0; offset < SeamRowCount; offset++)
            {
                var rowIndex = (startIndex + offset) % SeamRowCount;
                var row = seamRows[rowIndex];
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp();
                var resolved = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out var path);
                var end = Stopwatch.GetTimestamp();
                var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                var allocated = Math.Max(0L, allocatedAfter - allocatedBefore);
                candidateAllocatedBytes += allocated;
                seamSamples[seamIndex++] = new TimingSample(pass, rowIndex, TicksToMicroseconds(end - start), allocated, resolved, path, row.ProbeSideCode);
            }
        }

        var regionGen0After = GC.CollectionCount(0);
        var regionGen1After = GC.CollectionCount(1);
        var regionGen2After = GC.CollectionCount(2);
        var regionBytesAfter = GC.GetAllocatedBytesForCurrentThread();
        var wholeRegionAllocatedBytes = Math.Max(0L, regionBytesAfter - regionBytesBefore);
        var harnessAllocatedBytes = Math.Max(0L, wholeRegionAllocatedBytes - candidateAllocatedBytes);

        Assert.Equal(ExactMeasuredCalls, exactIndex);
        Assert.Equal(SeamMeasuredCalls, seamIndex);
        ValidateFiniteSamples(exactSamples);
        ValidateFiniteSamples(seamSamples);

        var artifactDirectory = EnsureArtifactDirectory(root);
        var processDirectory = ResetOwnedProcessDirectory(artifactDirectory, runIndex);
        WriteProcessContract(processDirectory, runIndex, candidate, ceilings, hostFingerprint);
        WriteExactTiming(processDirectory, exactRows, exactSamples);
        WriteSeamTiming(processDirectory, seamRows, seamSamples);
        WriteRuntimeContext(
            processDirectory,
            runIndex,
            wholeRegionAllocatedBytes,
            candidateAllocatedBytes,
            harnessAllocatedBytes,
            regionGen0After - regionGen0Before,
            regionGen1After - regionGen1Before,
            regionGen2After - regionGen2Before,
            hostFingerprint);
        WriteProcessSummary(processDirectory, runIndex, exactSamples, seamSamples, candidateAllocatedBytes, harnessAllocatedBytes, ceilings, hostFingerprint);
    }

    private static void ResolveExactWarmup(Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate, ExactRow[] rows, int pass)
    {
        var startIndex = (pass * RotationStride) % rows.Length;
        for (var offset = 0; offset < rows.Length; offset++)
        {
            var row = rows[(startIndex + offset) % rows.Length];
            _ = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out _);
        }
    }

    private static void ResolveSeamWarmup(Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate, SeamRow[] rows, int pass)
    {
        var startIndex = (pass * RotationStride) % rows.Length;
        for (var offset = 0; offset < rows.Length; offset++)
        {
            var row = rows[(startIndex + offset) % rows.Length];
            _ = candidate.TryResolveWithPath(row.SpecificVolume, row.SpecificEnergy, out _, out _);
        }
    }

    private static void PrimeTimingHarness(Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate, ExactRow exact, SeamRow seam)
    {
        PrimeOne(candidate, exact.SpecificVolume, exact.SpecificEnergy);
        PrimeOne(candidate, seam.SpecificVolume, seam.SpecificEnergy);
        _ = new TimingSample(0, 0, 0d, 0L, true, Rp1bC4ResolutionPath.Unresolved, 0);
    }

    private static void PrimeOne(Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate, double volume, double energy)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        _ = candidate.TryResolveWithPath(volume, energy, out _, out _);
        var end = Stopwatch.GetTimestamp();
        var after = GC.GetAllocatedBytesForCurrentThread();
        _ = Math.Max(0L, after - before);
        _ = TicksToMicroseconds(end - start);
        _ = GC.CollectionCount(0);
        _ = GC.CollectionCount(1);
        _ = GC.CollectionCount(2);
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

    private static void WriteProcessContract(string directory, int runIndex, Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate candidate, PerformanceCeilings ceilings, string hostFingerprint)
    {
        File.WriteAllLines(Path.Combine(directory, "01-process-contract.txt"), new[]
        {
            "gate=RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2",
            "status=PASS-PROCESS-CONTRACT",
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "runtime-profile=AMBIENT-UNSET",
            "host-fingerprint-sha256=" + hostFingerprint,
            "controlled-runtime-variables-unset=True",
            "candidate=" + candidate.CandidateId,
            "candidate-family=" + candidate.FamilyId,
            "candidate-source-mutation=False",
            "exact-v9-rows=360",
            "exact-v9-warmup-passes=16",
            "exact-v9-measured-passes=64",
            "exact-v9-measured-calls=23040",
            "seam-rows=1280",
            "seam-warmup-passes=4",
            "seam-measured-passes=16",
            "seam-measured-calls=20480",
            "total-measured-calls=43520",
            "rotation-stride=37",
            "resolve-median-ceiling-us=" + F(ceilings.ResolveMedianMicroseconds),
            "resolve-p95-ceiling-us=" + F(ceilings.ResolveP95Microseconds),
            "single-call-max-ceiling-us=" + F(ceilings.ResolveMaximumMicroseconds),
            "resolve-median-allocation-ceiling-bytes=" + F(ceilings.ResolveMedianAllocatedBytes),
            "harness-allocation-neutral-required=True",
            "negative-engineering-outcome-is-xunit-failure=False",
            "rp1c-selection-authorized=False",
            "production-runtime-change-authorized=False",
            "production-repair-authorized=False",
        }, Utf8WithoutBom);
    }

    private static void WriteExactTiming(string directory, ExactRow[] rows, TimingSample[] samples)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "02-exact-v9-call-timing.csv"), false, Utf8WithoutBom);
        writer.WriteLine("run_index,pass_index,row_index,probe_id,logical_step,node_id,elapsed_us,allocated_bytes,resolved,resolution_path");
        var runIndex = ParseRunIndex(Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable));
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var row = rows[sample.RowIndex];
            writer.WriteLine(string.Join(',',
                runIndex.ToString(CultureInfo.InvariantCulture),
                sample.PassIndex.ToString(CultureInfo.InvariantCulture),
                sample.RowIndex.ToString(CultureInfo.InvariantCulture),
                row.ProbeId,
                row.LogicalStep.ToString(CultureInfo.InvariantCulture),
                row.NodeId,
                F(sample.ElapsedMicroseconds),
                sample.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                B(sample.Resolved),
                PathText(sample.ResolutionPath)));
        }
    }

    private static void WriteSeamTiming(string directory, SeamRow[] rows, TimingSample[] samples)
    {
        using var writer = new StreamWriter(Path.Combine(directory, "03-seam-call-timing.csv"), false, Utf8WithoutBom);
        writer.WriteLine("run_index,pass_index,row_index,boundary_index,probe_side,elapsed_us,allocated_bytes,resolved,resolution_path");
        var runIndex = ParseRunIndex(Environment.GetEnvironmentVariable(RunIndexEnvironmentVariable));
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var row = rows[sample.RowIndex];
            writer.WriteLine(string.Join(',',
                runIndex.ToString(CultureInfo.InvariantCulture),
                sample.PassIndex.ToString(CultureInfo.InvariantCulture),
                sample.RowIndex.ToString(CultureInfo.InvariantCulture),
                row.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
                ProbeSideText(row.ProbeSideCode),
                F(sample.ElapsedMicroseconds),
                sample.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                B(sample.Resolved),
                PathText(sample.ResolutionPath)));
        }
    }

    private static void WriteRuntimeContext(string directory, int runIndex, long wholeRegionAllocatedBytes, long candidateAllocatedBytes, long harnessAllocatedBytes, int gen0, int gen1, int gen2, string hostFingerprint)
    {
        File.WriteAllLines(Path.Combine(directory, "04-runtime-context.txt"), new[]
        {
            "status=PASS-RUNTIME-CONTEXT-CAPTURED",
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "runtime-profile=AMBIENT-UNSET",
            "host-fingerprint-sha256=" + hostFingerprint,
            "controlled-runtime-variables-unset=True",
            "framework=" + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            "os=" + System.Runtime.InteropServices.RuntimeInformation.OSDescription.Replace('\r', ' ').Replace('\n', ' '),
            "process-architecture=" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
            "stopwatch-frequency=" + Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
            "whole-measured-region-allocated-bytes=" + wholeRegionAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "candidate-call-allocated-bytes-sum=" + candidateAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "harness-allocated-bytes=" + harnessAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "gc-gen0-collections-during-measured-region=" + gen0.ToString(CultureInfo.InvariantCulture),
            "gc-gen1-collections-during-measured-region=" + gen1.ToString(CultureInfo.InvariantCulture),
            "gc-gen2-collections-during-measured-region=" + gen2.ToString(CultureInfo.InvariantCulture),
        }, Utf8WithoutBom);
    }

    private static void WriteProcessSummary(string directory, int runIndex, TimingSample[] exactSamples, TimingSample[] seamSamples, long candidateAllocatedBytes, long harnessAllocatedBytes, PerformanceCeilings ceilings, string hostFingerprint)
    {
        var exactElapsed = new double[exactSamples.Length];
        var exactAllocated = new double[exactSamples.Length];
        var exactUnresolved = 0;
        var exactOver = 0;
        for (var i = 0; i < exactSamples.Length; i++)
        {
            exactElapsed[i] = exactSamples[i].ElapsedMicroseconds;
            exactAllocated[i] = exactSamples[i].AllocatedBytes;
            if (!exactSamples[i].Resolved) exactUnresolved++;
            if (exactSamples[i].ElapsedMicroseconds > ceilings.ResolveMaximumMicroseconds) exactOver++;
        }
        Array.Sort(exactElapsed);
        Array.Sort(exactAllocated);

        var seamUnresolved = 0;
        var seamOver = 0;
        var seamMaximum = double.NegativeInfinity;
        var sideMaximum = new double[5];
        for (var side = 0; side < sideMaximum.Length; side++) sideMaximum[side] = double.NegativeInfinity;
        for (var i = 0; i < seamSamples.Length; i++)
        {
            var sample = seamSamples[i];
            if (!sample.Resolved) seamUnresolved++;
            if (sample.ElapsedMicroseconds > ceilings.ResolveMaximumMicroseconds) seamOver++;
            seamMaximum = Math.Max(seamMaximum, sample.ElapsedMicroseconds);
            if (sample.ProbeSideCode >= 1 && sample.ProbeSideCode <= 4)
            {
                sideMaximum[sample.ProbeSideCode] = Math.Max(sideMaximum[sample.ProbeSideCode], sample.ElapsedMicroseconds);
            }
        }

        var exactMedian = MedianSorted(exactElapsed);
        var exactP95 = PercentileSorted(exactElapsed, 0.95d);
        var exactMax = exactElapsed[^1];
        var exactMedianAllocation = MedianSorted(exactAllocated);
        var strictMet = exactUnresolved == 0
            && seamUnresolved == 0
            && exactMedian <= ceilings.ResolveMedianMicroseconds
            && exactP95 <= ceilings.ResolveP95Microseconds
            && exactMax <= ceilings.ResolveMaximumMicroseconds
            && seamMaximum <= ceilings.ResolveMaximumMicroseconds
            && exactMedianAllocation <= ceilings.ResolveMedianAllocatedBytes;

        File.WriteAllLines(Path.Combine(directory, "05-process-summary.txt"), new[]
        {
            "status=PASS-PROCESS-EVIDENCE-COMPLETE",
            "run-index=" + runIndex.ToString(CultureInfo.InvariantCulture),
            "runtime-profile=AMBIENT-UNSET",
            "host-fingerprint-sha256=" + hostFingerprint,
            "controlled-runtime-variables-unset=True",
            "exact-v9-measured-calls=" + exactSamples.Length.ToString(CultureInfo.InvariantCulture),
            "seam-measured-calls=" + seamSamples.Length.ToString(CultureInfo.InvariantCulture),
            "total-measured-calls=" + TotalMeasuredCalls.ToString(CultureInfo.InvariantCulture),
            "exact-v9-median-us=" + F(exactMedian),
            "exact-v9-p95-us=" + F(exactP95),
            "exact-v9-max-us=" + F(exactMax),
            "exact-v9-median-allocated-bytes=" + F(exactMedianAllocation),
            "exact-v9-unresolved-calls=" + exactUnresolved.ToString(CultureInfo.InvariantCulture),
            "exact-v9-calls-over-max-ceiling=" + exactOver.ToString(CultureInfo.InvariantCulture),
            "seam-max-us=" + F(seamMaximum),
            "seam-r1-side-max-us=" + F(sideMaximum[1]),
            "seam-r4-liquid-side-max-us=" + F(sideMaximum[2]),
            "seam-r4-vapor-side-max-us=" + F(sideMaximum[3]),
            "seam-r2-side-max-us=" + F(sideMaximum[4]),
            "seam-unresolved-calls=" + seamUnresolved.ToString(CultureInfo.InvariantCulture),
            "seam-calls-over-max-ceiling=" + seamOver.ToString(CultureInfo.InvariantCulture),
            "candidate-call-allocated-bytes-sum=" + candidateAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "harness-allocated-bytes=" + harnessAllocatedBytes.ToString(CultureInfo.InvariantCulture),
            "strict-corrected-performance-predicate-met=" + B(strictMet),
            "engineering-negative-outcome-is-xunit-failure=False",
        }, Utf8WithoutBom);
    }

    private static ExactRow[] LoadExactRows(string path)
    {
        const string header = "probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        var rows = new ExactRow[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            Assert.Equal(17, parts.Length);
            var density = D(parts[6]);
            Assert.True(density > 0d);
            Assert.True(bool.Parse(parts[10]));
            rows[i] = new ExactRow(parts[0], long.Parse(parts[1], CultureInfo.InvariantCulture), parts[3], 1d / density, D(parts[7]));
        }
        return rows;
    }

    private static SeamRow[] LoadSeamRows(string path)
    {
        const string header = "boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        var rows = new SeamRow[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            Assert.Equal(16, parts.Length);
            rows[i] = new SeamRow(int.Parse(parts[0], CultureInfo.InvariantCulture), ProbeSideCode(parts[2]), D(parts[6]), D(parts[7]));
        }
        return rows;
    }

    private static PerformanceCeilings LoadPerformanceCeilings(string path)
    {
        var values = File.ReadAllLines(path, Encoding.UTF8)
            .Skip(1)
            .Where(static line => line.Contains(',', StringComparison.Ordinal))
            .Select(static line => line.Split(',', 2))
            .Where(static parts => parts.Length == 2)
            .ToDictionary(static parts => parts[0], static parts => parts[1], StringComparer.Ordinal);
        return new PerformanceCeilings(D(values["rp1b_candidate_resolve_median_ceiling_us"]), D(values["rp1b_candidate_resolve_p95_ceiling_us"]), D(values["rp1b_candidate_resolve_max_ceiling_us"]), D(values["rp1b_candidate_resolve_median_allocated_bytes_ceiling"]));
    }

    private static string[] ReadCsvLines(string path, string expectedHeader)
    {
        Assert.True(File.Exists(path), $"Required frozen RP1A artifact missing: {path}");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.NotEmpty(lines);
        Assert.Equal(expectedHeader, lines[0]);
        return lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    private static int ProbeSideCode(string value) => value switch
    {
        "R1-SIDE" => 1,
        "R4-LIQUID-SIDE" => 2,
        "R4-VAPOR-SIDE" => 3,
        "R2-SIDE" => 4,
        _ => throw new InvalidOperationException("Unexpected seam probe side: " + value),
    };

    private static string ProbeSideText(int code) => code switch
    {
        1 => "R1-SIDE",
        2 => "R4-LIQUID-SIDE",
        3 => "R4-VAPOR-SIDE",
        4 => "R2-SIDE",
        _ => "UNKNOWN",
    };

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
        return sorted.Length % 2 == 0 ? 0.5d * (sorted[middle - 1] + sorted[middle]) : sorted[middle];
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

    private static void RequireAmbientRuntimeControlsUnset()
    {
        var names = new[]
        {
            "DOTNET_TieredCompilation",
            "DOTNET_TieredPGO",
            "DOTNET_TC_QuickJit",
            "DOTNET_TC_QuickJitForLoops",
            "DOTNET_ReadyToRun",
            "COMPlus_TieredCompilation",
            "COMPlus_TieredPGO",
            "COMPlus_TC_QuickJit",
            "COMPlus_TC_QuickJitForLoops",
            "COMPlus_ReadyToRun",
        };

        foreach (var name in names)
        {
            Assert.True(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)), $"Runtime control must be unset in FDPC2 child: {name}");
        }
    }

    private static string RequireHostFingerprint()
    {
        var value = Environment.GetEnvironmentVariable(HostFingerprintEnvironmentVariable);
        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.Equal(64, value!.Length);
        return value;
    }

    private static void RequireOptIn() => Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));

    private static string EnsureArtifactDirectory(string root)
    {
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string ResetOwnedProcessDirectory(string artifactDirectory, int runIndex)
    {
        var path = Path.Combine(artifactDirectory, "process-" + runIndex.ToString("00", CultureInfo.InvariantCulture));
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
        return path;
    }

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

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;
    private static double D(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string B(bool value) => value ? "True" : "False";

    private readonly record struct ExactRow(string ProbeId, long LogicalStep, string NodeId, double SpecificVolume, double SpecificEnergy);
    private readonly record struct SeamRow(int BoundaryIndex, int ProbeSideCode, double SpecificVolume, double SpecificEnergy);
    private readonly record struct PerformanceCeilings(double ResolveMedianMicroseconds, double ResolveP95Microseconds, double ResolveMaximumMicroseconds, double ResolveMedianAllocatedBytes);

    private readonly struct TimingSample
    {
        public TimingSample(int passIndex, int rowIndex, double elapsedMicroseconds, long allocatedBytes, bool resolved, Rp1bC4ResolutionPath resolutionPath, int probeSideCode)
        {
            PassIndex = passIndex;
            RowIndex = rowIndex;
            ElapsedMicroseconds = elapsedMicroseconds;
            AllocatedBytes = allocatedBytes;
            Resolved = resolved;
            ResolutionPath = resolutionPath;
            ProbeSideCode = probeSideCode;
        }
        public int PassIndex { get; }
        public int RowIndex { get; }
        public double ElapsedMicroseconds { get; }
        public long AllocatedBytes { get; }
        public bool Resolved { get; }
        public Rp1bC4ResolutionPath ResolutionPath { get; }
        public int ProbeSideCode { get; }
    }
}
