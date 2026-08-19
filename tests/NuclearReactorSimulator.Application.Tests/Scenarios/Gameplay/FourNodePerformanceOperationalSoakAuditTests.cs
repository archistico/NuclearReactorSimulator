using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.28 Requalification 2 of relative cost and bounded operational soak over the validated H.28.1-G
/// optimized corrected-commit runtime. Wall-clock values are machine-local evidence; qualification uses paired relative cost
/// plus deterministic numerical/ownership contracts and does not reinterpret the 10 ms simulated fixed step as a
/// hard wall-clock deadline for the xUnit runner.
/// </summary>
public sealed class FourNodePerformanceOperationalSoakAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int BenchmarkWarmupSteps = 64;
    private const int BenchmarkSteps = 256;
    private const int SoakSteps = 1_536;
    private const int SoakSampleStride = 32;
    private const int DeterminismSteps = 128;
    private const double MaximumMedianWallCostRatio = 8d;
    private const double ActivationFavorableMedianWallCostRatio = 4d;
    private const double MaximumP95WallCostRatio = 12d;
    private const double MaximumMedianAllocationRatio = 16d;
    private const double ActivationFavorableMedianAllocationRatio = 8d;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    private static readonly IReadOnlyDictionary<string, string> FrozenH27Fingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H27_ValidatedOffDesignQualificationSummary.txt"] = "DDEAC9E8987FC7C12483A792067B4134BB1D87BDDD052F5E0D46E9AEAD3107AE",
        ["H27_ValidatedOffDesignStepTelemetry.csv"] = "6EAA7CAFC4594B455F0933075EBC7486FBF90FBEAF73A65B1B29967111EBA067",
        ["H27_ValidatedOffDesignQualificationEnvelope.csv"] = "AE4A1E9202284BB12E365FEAD6FBA2244B6C16B710ECF8F3D13022DBE7FBD575",
        ["H27_ValidatedOffDesignQualificationMetrics.csv"] = "C1E3857BB734E64357F0DE7596F229A194A1ACF013B6B829BCACF90F983EE342",
    };

    private static readonly IReadOnlyDictionary<string, string> FrozenH281GFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_1G_ValidatedUntargetedDisagreementScanFastPathSummary.txt"] = "260904E90A4D7B6E64F109BAF6FFE76A27DDCA7B82390348E01EBE4B380CC1E2",
        ["H28_1G_ValidatedUntargetedDisagreementScanFastPathSteps.csv"] = "CBEC443C90CA49CB88A878352A5CAC392FBA9825E75C864AD1FD4FBD34AA05A0",
        ["H28_1G_ValidatedUntargetedDisagreementScanFastPathMetrics.csv"] = "4E868C072485FC575A4D71CBAC0FB230809F119A26B44CD1F9B6FEDD6F92A2CC",
    };

    [Fact]
    public void FrozenH27Evidence_RetainsValidatedBoundedOffDesignEnvelope()
    {
        foreach (var expected in FrozenH27Fingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen H.27 evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H27_ValidatedOffDesignQualificationSummary.txt"));
        Assert.Contains("matrix-scenarios=6", summary, StringComparison.Ordinal);
        Assert.Contains("runtime-steps=2080", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=529", summary, StringComparison.Ordinal);
        Assert.Contains("fallback-commit-violations=0", summary, StringComparison.Ordinal);
        Assert.Contains("unsafe-corrected-commits=0", summary, StringComparison.Ordinal);
        Assert.Contains("untargeted-branch-disagreements=0", summary, StringComparison.Ordinal);
        Assert.Contains("scenario=high-load-10mwe; classification=protected-boundary", summary, StringComparison.Ordinal);
        Assert.Contains("scenario=cooling-25pct-capacity; classification=corrected-qualified", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-off-design-robustness-qualification-envelope-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h27-audit-passes=True", summary, StringComparison.Ordinal);

        var telemetryLines = File.ReadLines(Path.Combine(EvidenceDirectory(), "H27_ValidatedOffDesignStepTelemetry.csv")).Count();
        Assert.Equal(2_081, telemetryLines);
    }

    [Fact]
    public void FrozenH281GEvidence_RetainsValidatedCpuTailClosure()
    {
        foreach (var expected in FrozenH281GFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen H.28.1-G evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_1G_ValidatedUntargetedDisagreementScanFastPathSummary.txt"));
        Assert.Contains("triggered=20; committed=20; rollbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("trigger-p95-engine-us=79702.300000000003", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-hydraulic-evaluations=35", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-probe-evaluations=32", summary, StringComparison.Ordinal);
        Assert.Contains("h9-max-jacobian-dimension=32", summary, StringComparison.Ordinal);
        Assert.Contains("probe-hydraulic-component-reuse-fraction=0.80902777777777779", summary, StringComparison.Ordinal);
        Assert.Contains("probe-mapped-fluid-node-integrations=0", summary, StringComparison.Ordinal);
        Assert.Contains("unchanged-h28-trigger-tail-threshold-us=88381.200000000012", summary, StringComparison.Ordinal);
        Assert.Contains("h28.1g-triggered-p95-estimated-ratio=10.821618172190465", summary, StringComparison.Ordinal);
        Assert.Contains("tail-ready-for-h28=True", summary, StringComparison.Ordinal);
        Assert.Contains("deterministic-fingerprint=518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38", summary, StringComparison.Ordinal);
        Assert.Contains("h28.1g-untargeted-disagreement-scan-fast-path-passes=True", summary, StringComparison.Ordinal);

        var stepLines = File.ReadLines(Path.Combine(EvidenceDirectory(), "H28_1G_ValidatedUntargetedDisagreementScanFastPathSteps.csv")).Count();
        Assert.Equal(257, stepLines);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodePerformanceOperationalSoakAudit")]
    public void OptInCommittedRuntime_HasBoundedRelativeCostAndStableOperationalSoakWithoutWeakeningNumericalContracts()
    {
        ResetProgress();

        WriteProgress("benchmark-start");
        var explicitEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var correctedEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, CurrentHydraulics(explicitEngine).Mode);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(correctedEngine).Mode);

        Warmup(explicitEngine, BenchmarkWarmupSteps);
        Warmup(correctedEngine, BenchmarkWarmupSteps);

        var explicitBenchmark = RunBenchmark("explicit", explicitEngine, BenchmarkSteps);
        var correctedBenchmark = RunBenchmark("corrected-commit", correctedEngine, BenchmarkSteps);
        WriteProgress($"benchmark-complete explicit-median-us={explicitBenchmark.MedianWallMicroseconds:0.000} corrected-median-us={correctedBenchmark.MedianWallMicroseconds:0.000}");

        var medianWallCostRatio = SafeRatio(correctedBenchmark.MedianWallMicroseconds, explicitBenchmark.MedianWallMicroseconds);
        var p95WallCostRatio = SafeRatio(correctedBenchmark.P95WallMicroseconds, explicitBenchmark.P95WallMicroseconds);
        var medianAllocationRatio = SafeRatio(correctedBenchmark.MedianAllocatedBytes, explicitBenchmark.MedianAllocatedBytes);
        var hardCostPass = correctedBenchmark.TriggerCount > 0
            && correctedBenchmark.CommitCount > 0
            && correctedBenchmark.RollbackCount == 0
            && correctedBenchmark.UnsafeCommitCount == 0
            && medianWallCostRatio <= MaximumMedianWallCostRatio
            && p95WallCostRatio <= MaximumP95WallCostRatio
            && medianAllocationRatio <= MaximumMedianAllocationRatio;
        var activationFavorable = hardCostPass
            && medianWallCostRatio <= ActivationFavorableMedianWallCostRatio
            && medianAllocationRatio <= ActivationFavorableMedianAllocationRatio;
        var performanceClass = activationFavorable
            ? "activation-favorable"
            : hardCostPass
                ? "bounded-but-costly"
                : "unbounded-regression";

        WriteProgress("soak-start");
        var soak = RunOperationalSoak();
        WriteProgress($"soak-complete steps={soak.TotalSteps} commits={soak.CommitCount} rollbacks={soak.RollbackCount} trips={soak.TripStepCount}");
        Assert.Equal(SoakSteps, soak.TotalSteps);
        Assert.True(soak.TriggerCount > 0);
        Assert.True(soak.CommitCount > 0);
        Assert.Equal(0, soak.FallbackCommitViolationCount);
        Assert.Equal(0, soak.UnsafeCommitCount);
        Assert.Equal(0, soak.UntargetedBranchDisagreementCount);
        Assert.Equal(0, soak.TripStepCount);
        Assert.InRange(soak.MaximumMassClosureResidualKilograms, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(soak.MaximumEnergyClosureResidualJoules, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(soak.MaximumBalanceMassRateResidualKilogramsPerSecond, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(soak.MaximumBalancePowerResidualWatts, 0d, MaximumBalancePowerResidualWatts);

        WriteProgress("determinism-control-start");
        var firstDeterminism = RunDeterminismControl();
        var repeatDeterminism = RunDeterminismControl();
        Assert.Equal(firstDeterminism, repeatDeterminism);
        var deterministicRepeat = firstDeterminism == repeatDeterminism;

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(defaultEngine).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var passes = hardCostPass
            && soak.CommitCount > 0
            && soak.FallbackCommitViolationCount == 0
            && soak.UnsafeCommitCount == 0
            && soak.UntargetedBranchDisagreementCount == 0
            && soak.TripStepCount == 0
            && soak.MaximumMassClosureResidualKilograms <= MaximumMassClosureResidualKilograms
            && soak.MaximumEnergyClosureResidualJoules <= MaximumEnergyClosureResidualJoules
            && soak.MaximumBalanceMassRateResidualKilogramsPerSecond <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && soak.MaximumBalancePowerResidualWatts <= MaximumBalancePowerResidualWatts
            && deterministicRepeat
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        WriteReports(explicitBenchmark, correctedBenchmark, soak, medianWallCostRatio, p95WallCostRatio,
            medianAllocationRatio, performanceClass, firstDeterminism, deterministicRepeat, defaultMode, passes);
        WriteProgress($"H.28 performance/cost/operational-soak audit completed passes={passes} class={performanceClass}");
        Assert.True(passes, $"H.28 qualification failed. Performance class={performanceClass}; median wall ratio={medianWallCostRatio:G17}; p95 wall ratio={p95WallCostRatio:G17}; median allocation ratio={medianAllocationRatio:G17}.");
    }

    private static void Warmup(IntegratedAutomaticOperationRuntimeEngine engine, int steps)
    {
        for (var step = 0; step < steps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive);
        }
    }

    private static BenchmarkRun RunBenchmark(string mode, IntegratedAutomaticOperationRuntimeEngine engine, int steps)
    {
        var rows = new List<BenchmarkStepRow>(steps);
        for (var step = 1; step <= steps; step++)
        {
            if (step == 64 || step == 192)
            {
                QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
            }
            else if (step == 128 || step == 224)
            {
                QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadRaise);
            }

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            var presentation = engine.Step(ControlRoomRunState.Running);
            var end = Stopwatch.GetTimestamp();
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.False(presentation.AnyTripActive);

            var numerics = CurrentHydraulics(engine);
            var telemetry = numerics.FourNodeBranchContinuity;
            var wallMicroseconds = (end - start) * 1_000_000d / Stopwatch.Frequency;
            var allocatedBytes = Math.Max(0L, allocatedAfter - allocatedBefore);
            var triggerObserved = telemetry?.TriggerObserved ?? false;
            var committed = telemetry?.CorrectedCandidateCommitted ?? false;
            var rollback = telemetry?.RollbackRequired ?? false;
            var shadowIterations = telemetry?.ShadowIterationCount ?? 0;
            var unsafeCommit = telemetry is not null && committed && !CommitIsQualified(telemetry);
            Assert.False(unsafeCommit);
            if (rollback)
            {
                Assert.False(committed);
            }

            rows.Add(new BenchmarkStepRow(mode, step, wallMicroseconds, allocatedBytes, triggerObserved, committed,
                rollback, shadowIterations, unsafeCommit));
        }

        var wall = rows.Select(static row => row.WallMicroseconds).ToArray();
        var allocations = rows.Select(static row => (double)row.AllocatedBytes).ToArray();
        var triggeredRows = rows.Where(static row => row.TriggerObserved).ToArray();
        return new BenchmarkRun(
            mode,
            rows,
            Percentile(wall, 0.50d),
            Percentile(wall, 0.95d),
            Percentile(wall, 1.00d),
            Percentile(allocations, 0.50d),
            Percentile(allocations, 0.95d),
            rows.Count(static row => row.TriggerObserved),
            rows.Count(static row => row.CorrectedCandidateCommitted),
            rows.Count(static row => row.RollbackRequired),
            rows.Count(static row => row.UnsafeCommit),
            triggeredRows.Length > 0 ? triggeredRows.Average(static row => row.WallMicroseconds) : 0d,
            triggeredRows.Length > 0 ? triggeredRows.Max(static row => row.WallMicroseconds) : 0d,
            triggeredRows.Length > 0 ? triggeredRows.Average(static row => (double)row.AllocatedBytes) : 0d,
            triggeredRows.Select(static row => row.ShadowIterationCount).DefaultIfEmpty().Average(),
            rows.Max(static row => row.ShadowIterationCount));
    }

    private static SoakRun RunOperationalSoak()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
        var rows = new List<SoakSampleRow>();
        var triggerCount = 0;
        var commitCount = 0;
        var rollbackCount = 0;
        var fallbackCommitViolations = 0;
        var unsafeCommits = 0;
        var untargetedDisagreements = 0;
        var tripSteps = 0;
        var maximumMassClosure = 0d;
        var maximumEnergyClosure = 0d;
        var maximumBalanceMassRate = 0d;
        var maximumBalancePower = 0d;
        var maximumShadowIterations = 0;
        var totalShadowIterationsOnTriggers = 0L;
        var totalAllocatedBytes = 0L;
        var maximumAllocatedBytesPerStep = 0L;
        var totalWallMicroseconds = 0d;
        var maximumWallMicroseconds = 0d;
        var managedHeapStart = GC.GetTotalMemory(forceFullCollection: false);
        var gen0Start = GC.CollectionCount(0);
        var gen1Start = GC.CollectionCount(1);
        var gen2Start = GC.CollectionCount(2);

        for (var step = 1; step <= SoakSteps; step++)
        {
            if (step == 384 || step == 896)
            {
                QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
            }
            else if (step == 640 || step == 1_152)
            {
                QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadRaise);
            }

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            var presentation = engine.Step(ControlRoomRunState.Running);
            var end = Stopwatch.GetTimestamp();
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            var wallMicroseconds = (end - start) * 1_000_000d / Stopwatch.Frequency;
            var allocatedBytes = Math.Max(0L, allocatedAfter - allocatedBefore);
            totalAllocatedBytes += allocatedBytes;
            maximumAllocatedBytesPerStep = Math.Max(maximumAllocatedBytesPerStep, allocatedBytes);
            totalWallMicroseconds += wallMicroseconds;
            maximumWallMicroseconds = Math.Max(maximumWallMicroseconds, wallMicroseconds);

            var numerics = CurrentHydraulics(engine);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            var audit = CurrentAudit(engine);
            var unsafeCommit = telemetry.CorrectedCandidateCommitted && !CommitIsQualified(telemetry);
            var fallbackCommitViolation = (!telemetry.ShadowCorrectedCandidateEligible || telemetry.RollbackRequired)
                && telemetry.CorrectedCandidateCommitted;

            if (telemetry.TriggerObserved)
            {
                triggerCount++;
                totalShadowIterationsOnTriggers += telemetry.ShadowIterationCount;
            }
            if (telemetry.CorrectedCandidateCommitted)
            {
                commitCount++;
            }
            if (telemetry.RollbackRequired)
            {
                rollbackCount++;
                Assert.False(telemetry.CorrectedCandidateCommitted);
            }
            if (telemetry.UntargetedBranchDisagreementDetected)
            {
                untargetedDisagreements++;
            }
            if (presentation.AnyTripActive)
            {
                tripSteps++;
            }
            if (fallbackCommitViolation)
            {
                fallbackCommitViolations++;
            }
            if (unsafeCommit)
            {
                unsafeCommits++;
            }

            maximumShadowIterations = Math.Max(maximumShadowIterations, telemetry.ShadowIterationCount);
            maximumMassClosure = Math.Max(maximumMassClosure, Math.Abs(audit.MassClosureResidualKilograms));
            maximumEnergyClosure = Math.Max(maximumEnergyClosure, Math.Abs(audit.EnergyClosureResidualJoules));
            maximumBalanceMassRate = Math.Max(maximumBalanceMassRate, Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond));
            maximumBalancePower = Math.Max(maximumBalancePower, Math.Abs(audit.BalancePowerResidualWatts));

            Assert.InRange(Math.Abs(audit.MassClosureResidualKilograms), 0d, MaximumMassClosureResidualKilograms);
            Assert.InRange(Math.Abs(audit.EnergyClosureResidualJoules), 0d, MaximumEnergyClosureResidualJoules);
            Assert.InRange(Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond), 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
            Assert.InRange(Math.Abs(audit.BalancePowerResidualWatts), 0d, MaximumBalancePowerResidualWatts);
            Assert.False(fallbackCommitViolation);
            Assert.False(unsafeCommit);
            Assert.False(presentation.AnyTripActive);

            if (step == 1 || step % SoakSampleStride == 0 || step == SoakSteps)
            {
                rows.Add(new SoakSampleRow(
                    step,
                    ControlRoomSnapshotFingerprint.Compute(presentation),
                    presentation.AnyTripActive,
                    telemetry.TriggerObserved,
                    telemetry.RollbackRequired,
                    telemetry.CorrectedCandidateCommitted,
                    telemetry.ShadowIterationCount,
                    wallMicroseconds,
                    allocatedBytes,
                    Math.Abs(audit.MassClosureResidualKilograms),
                    Math.Abs(audit.EnergyClosureResidualJoules)));
            }

            if (step % 256 == 0)
            {
                WriteProgress($"soak-progress step={step}/{SoakSteps} commits={commitCount} rollbacks={rollbackCount}");
            }
        }

        var managedHeapEnd = GC.GetTotalMemory(forceFullCollection: false);
        return new SoakRun(
            SoakSteps,
            rows,
            triggerCount,
            commitCount,
            rollbackCount,
            fallbackCommitViolations,
            unsafeCommits,
            untargetedDisagreements,
            tripSteps,
            maximumShadowIterations,
            triggerCount > 0 ? totalShadowIterationsOnTriggers / (double)triggerCount : 0d,
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            totalWallMicroseconds / SoakSteps,
            maximumWallMicroseconds,
            totalAllocatedBytes / (double)SoakSteps,
            maximumAllocatedBytesPerStep,
            managedHeapStart,
            managedHeapEnd,
            GC.CollectionCount(0) - gen0Start,
            GC.CollectionCount(1) - gen1Start,
            GC.CollectionCount(2) - gen2Start);
    }

    private static string RunDeterminismControl()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
        var builder = new StringBuilder();
        for (var step = 1; step <= DeterminismSteps; step++)
        {
            if (step == 48)
            {
                QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
            }
            else if (step == 96)
            {
                QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadRaise);
            }

            var presentation = engine.Step(ControlRoomRunState.Running);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(CurrentHydraulics(engine).FourNodeBranchContinuity);
            Assert.False(presentation.AnyTripActive);
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(presentation)}:{telemetry.TriggerObserved}:{telemetry.ProposedAuthority}:{telemetry.Reason}:{telemetry.RollbackRequired}:{telemetry.CorrectedCommitAuthorized}:{telemetry.CorrectedCandidateCommitted}:{telemetry.CorrectedCommitReason}:{telemetry.ShadowIterationCount}||"));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void QueueGeneratorCommand(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomCommandKind kind)
    {
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        engine.QueueOperatorCommand(new ControlRoomCommand(kind, generator.Id, ControlRoomCommandTargetKind.Generator));
    }

    private static bool CommitIsQualified(FourNodeBranchContinuityIntegrationTelemetry telemetry)
        => telemetry.ShadowCorrectedCandidateEligible
            && telemetry.CorrectedCommitAuthorized
            && !telemetry.RollbackRequired
            && !telemetry.UntargetedBranchDisagreementDetected
            && telemetry.ShadowCorrectionEvaluated
            && telemetry.ShadowConverged
            && !telemetry.ShadowLineSearchExhausted
            && telemetry.ShadowMaximumRelativePressureResidual <= 1e-5d
            && telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond <= 1e-2d
            && telemetry.ShadowMassClosureKilogramsPerSecond <= 1e-8d
            && telemetry.ShadowEnergyOwnershipResidualWatts <= 1e-3d
            && telemetry.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate
            && telemetry.Reason == FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection
            && telemetry.CorrectedCommitReason == FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority;

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        Assert.NotEmpty(values);
        var ordered = values.Order().ToArray();
        var index = (int)Math.Ceiling(percentile * ordered.Length) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
    }

    private static double SafeRatio(double numerator, double denominator)
    {
        if (Math.Abs(denominator) > 1e-12d)
        {
            return numerator / denominator;
        }
        return Math.Abs(numerator) <= 1e-12d ? 1d : double.PositiveInfinity;
    }

    private static void WriteReports(
        BenchmarkRun explicitBenchmark,
        BenchmarkRun correctedBenchmark,
        SoakRun soak,
        double medianWallCostRatio,
        double p95WallCostRatio,
        double medianAllocationRatio,
        string performanceClass,
        string determinismFingerprint,
        bool deterministicRepeat,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var benchmarkCsv = new List<string>
        {
            "mode,step,wall_microseconds,allocated_bytes,trigger_observed,corrected_candidate_committed,rollback_required,shadow_iterations,unsafe_commit",
        };
        benchmarkCsv.AddRange(explicitBenchmark.Rows.Concat(correctedBenchmark.Rows).Select(static row => FormattableString.Invariant(
            $"{row.Mode},{row.StepIndex},{row.WallMicroseconds:G17},{row.AllocatedBytes},{row.TriggerObserved},{row.CorrectedCandidateCommitted},{row.RollbackRequired},{row.ShadowIterationCount},{row.UnsafeCommit}")));
        File.WriteAllLines(Path.Combine(directory, "02-performance-benchmark.csv"), benchmarkCsv, Utf8WithoutBom);

        var soakCsv = new List<string>
        {
            "step,presentation_fingerprint,any_trip,trigger_observed,rollback_required,corrected_candidate_committed,shadow_iterations,wall_microseconds,allocated_bytes,mass_closure_kg,energy_closure_j",
        };
        soakCsv.AddRange(soak.Samples.Select(static row => FormattableString.Invariant(
            $"{row.StepIndex},{row.PresentationFingerprint},{row.AnyTripActive},{row.TriggerObserved},{row.RollbackRequired},{row.CorrectedCandidateCommitted},{row.ShadowIterationCount},{row.WallMicroseconds:G17},{row.AllocatedBytes},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17}")));
        File.WriteAllLines(Path.Combine(directory, "03-operational-soak-samples.csv"), soakCsv, Utf8WithoutBom);

        var benchmarkArtifactBytes = new FileInfo(Path.Combine(directory, "02-performance-benchmark.csv")).Length;
        var soakArtifactBytes = new FileInfo(Path.Combine(directory, "03-operational-soak-samples.csv")).Length;

        var summary = new[]
        {
            "=== 01-current-v2-four-node-performance-cost-operational-soak ===",
            "H.28 keeps the validated H.22-H.27 corrected-commit runtime unchanged and measures paired explicit-vs-corrected step cost plus a bounded operational soak. The 10 ms fixed step remains simulated time; machine-local wall-clock values are qualified by relative ratios rather than a hardware-specific absolute deadline. H.24 is not rerun.",
            FormattableString.Invariant($"benchmark-warmup-steps-per-mode={BenchmarkWarmupSteps}; benchmark-steps-per-mode={BenchmarkSteps}; explicit-median-step-us={explicitBenchmark.MedianWallMicroseconds:G17}; corrected-median-step-us={correctedBenchmark.MedianWallMicroseconds:G17}; median-wall-cost-ratio={medianWallCostRatio:G17}; median-wall-cost-ratio-limit={MaximumMedianWallCostRatio:G17}; corrected-performance-class={performanceClass};"),
            FormattableString.Invariant($"explicit-p95-step-us={explicitBenchmark.P95WallMicroseconds:G17}; corrected-p95-step-us={correctedBenchmark.P95WallMicroseconds:G17}; p95-wall-cost-ratio={p95WallCostRatio:G17}; p95-wall-cost-ratio-limit={MaximumP95WallCostRatio:G17}; explicit-max-step-us={explicitBenchmark.MaximumWallMicroseconds:G17}; corrected-max-step-us={correctedBenchmark.MaximumWallMicroseconds:G17};"),
            FormattableString.Invariant($"explicit-median-allocated-bytes={explicitBenchmark.MedianAllocatedBytes:G17}; corrected-median-allocated-bytes={correctedBenchmark.MedianAllocatedBytes:G17}; median-allocation-ratio={medianAllocationRatio:G17}; median-allocation-ratio-limit={MaximumMedianAllocationRatio:G17}; corrected-triggered={correctedBenchmark.TriggerCount}/{BenchmarkSteps}; corrected-committed={correctedBenchmark.CommitCount}/{BenchmarkSteps}; corrected-triggered-average-step-us={correctedBenchmark.AverageTriggeredWallMicroseconds:G17}; corrected-triggered-max-step-us={correctedBenchmark.MaximumTriggeredWallMicroseconds:G17}; corrected-triggered-average-allocated-bytes={correctedBenchmark.AverageTriggeredAllocatedBytes:G17}; corrected-average-trigger-iterations={correctedBenchmark.AverageTriggeredIterations:G17}; corrected-max-shadow-iterations={correctedBenchmark.MaximumShadowIterations};"),
            FormattableString.Invariant($"wall-seconds-per-simulated-second-explicit/corrected={explicitBenchmark.Rows.Average(static row => row.WallMicroseconds) * 100d / 1_000_000d:G17}/{correctedBenchmark.Rows.Average(static row => row.WallMicroseconds) * 100d / 1_000_000d:G17}; benchmark-load-manoeuvre=5-to-0-to-5-twice;"),
            FormattableString.Invariant($"soak-steps={soak.TotalSteps}; soak-simulated-seconds={soak.TotalSteps * Step.TotalSeconds:G17}; soak-triggered={soak.TriggerCount}; soak-committed={soak.CommitCount}; soak-rollbacks={soak.RollbackCount}; soak-fallback-commit-violations={soak.FallbackCommitViolationCount}; soak-unsafe-commits={soak.UnsafeCommitCount}; soak-untargeted-branch-disagreements={soak.UntargetedBranchDisagreementCount}; soak-trip-steps={soak.TripStepCount};"),
            FormattableString.Invariant($"soak-average-step-us={soak.AverageWallMicroseconds:G17}; soak-max-step-us={soak.MaximumWallMicroseconds:G17}; soak-average-allocated-bytes-per-step={soak.AverageAllocatedBytesPerStep:G17}; soak-max-allocated-bytes-per-step={soak.MaximumAllocatedBytesPerStep}; soak-managed-heap-start-bytes={soak.ManagedHeapStartBytes}; soak-managed-heap-end-bytes={soak.ManagedHeapEndBytes}; soak-managed-heap-delta-bytes={soak.ManagedHeapEndBytes - soak.ManagedHeapStartBytes}; gc-collections-gen0/gen1/gen2={soak.Gen0Collections}/{soak.Gen1Collections}/{soak.Gen2Collections};"),
            FormattableString.Invariant($"soak-max-shadow-iterations={soak.MaximumShadowIterations}; soak-average-trigger-iterations={soak.AverageTriggeredIterations:G17}; max-network-mass-closure-kg={soak.MaximumMassClosureResidualKilograms:G17}; max-network-energy-closure-j={soak.MaximumEnergyClosureResidualJoules:G17}; max-network-balance-mass-rate-kg-s={soak.MaximumBalanceMassRateResidualKilogramsPerSecond:G17}; max-network-balance-power-w={soak.MaximumBalancePowerResidualWatts:G17};"),
            FormattableString.Invariant($"benchmark-artifact-bytes={benchmarkArtifactBytes}; sampled-soak-artifact-bytes={soakArtifactBytes}; soak-sample-stride={SoakSampleStride}; determinism-control-steps={DeterminismSteps}; deterministic-repeat={deterministicRepeat}; deterministic-fingerprint={determinismFingerprint};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; H27-prerequisite-frozen=True; H24-rerun=False; production-fixed-step=10.000 ms; H20-contract-replaced=False; H22-commit-seam-replaced=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"four-node-performance-cost-operational-soak-passes={passes}; h28-audit-passes={passes};"),
            performanceClass == "activation-favorable"
                ? "H.28 recommendation: relative cost is activation-favorable under the paired machine-local benchmark and the bounded soak is stable. Keep default production explicit until the separately reviewed H.29 activation candidate and H.30 closure decision."
                : performanceClass == "bounded-but-costly"
                    ? "H.28 recommendation: runtime cost is bounded enough for continued qualification but remains materially costly relative to explicit. Preserve the opt-in path and carry this cost classification into H.29/H.30; do not hide cost by changing timestep or weakening the numerical contract."
                    : "H.28 recommendation: the corrected path exceeds the broad relative-cost ceilings. Do not prepare H.29 default activation from this result; keep production explicit/opt-in and investigate performance without changing the numerical contract, then rerun H.28.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-four-node-performance-cost-operational-soak.summary.txt"), summary, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "04-performance-cost-soak-metrics.csv"), new[]
        {
            "metric,value",
            $"benchmark_steps_per_mode,{BenchmarkSteps}",
            FormattableString.Invariant($"explicit_median_step_us,{explicitBenchmark.MedianWallMicroseconds:G17}"),
            FormattableString.Invariant($"corrected_median_step_us,{correctedBenchmark.MedianWallMicroseconds:G17}"),
            FormattableString.Invariant($"corrected_triggered_average_step_us,{correctedBenchmark.AverageTriggeredWallMicroseconds:G17}"),
            FormattableString.Invariant($"corrected_triggered_max_step_us,{correctedBenchmark.MaximumTriggeredWallMicroseconds:G17}"),
            FormattableString.Invariant($"corrected_triggered_average_allocated_bytes,{correctedBenchmark.AverageTriggeredAllocatedBytes:G17}"),
            FormattableString.Invariant($"median_wall_cost_ratio,{medianWallCostRatio:G17}"),
            FormattableString.Invariant($"p95_wall_cost_ratio,{p95WallCostRatio:G17}"),
            FormattableString.Invariant($"median_allocation_ratio,{medianAllocationRatio:G17}"),
            $"performance_class,{performanceClass}",
            $"soak_steps,{soak.TotalSteps}",
            $"soak_commits,{soak.CommitCount}",
            $"soak_rollbacks,{soak.RollbackCount}",
            $"soak_trip_steps,{soak.TripStepCount}",
            FormattableString.Invariant($"soak_average_step_us,{soak.AverageWallMicroseconds:G17}"),
            FormattableString.Invariant($"soak_average_allocated_bytes_per_step,{soak.AverageAllocatedBytesPerStep:G17}"),
            $"deterministic_repeat,{deterministicRepeat}",
            $"deterministic_fingerprint,{determinismFingerprint}",
            $"h28_audit_passes,{passes}",
        }, Utf8WithoutBom);
    }

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h28-four-node-performance-cost-operational-soak");

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
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string CanonicalSha256(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("H.28 performance/cost/operational-soak audit started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", Utf8WithoutBom);

    private sealed record BenchmarkStepRow(
        string Mode,
        int StepIndex,
        double WallMicroseconds,
        long AllocatedBytes,
        bool TriggerObserved,
        bool CorrectedCandidateCommitted,
        bool RollbackRequired,
        int ShadowIterationCount,
        bool UnsafeCommit);

    private sealed record BenchmarkRun(
        string Mode,
        IReadOnlyList<BenchmarkStepRow> Rows,
        double MedianWallMicroseconds,
        double P95WallMicroseconds,
        double MaximumWallMicroseconds,
        double MedianAllocatedBytes,
        double P95AllocatedBytes,
        int TriggerCount,
        int CommitCount,
        int RollbackCount,
        int UnsafeCommitCount,
        double AverageTriggeredWallMicroseconds,
        double MaximumTriggeredWallMicroseconds,
        double AverageTriggeredAllocatedBytes,
        double AverageTriggeredIterations,
        int MaximumShadowIterations);

    private sealed record SoakSampleRow(
        int StepIndex,
        string PresentationFingerprint,
        bool AnyTripActive,
        bool TriggerObserved,
        bool RollbackRequired,
        bool CorrectedCandidateCommitted,
        int ShadowIterationCount,
        double WallMicroseconds,
        long AllocatedBytes,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules);

    private sealed record SoakRun(
        int TotalSteps,
        IReadOnlyList<SoakSampleRow> Samples,
        int TriggerCount,
        int CommitCount,
        int RollbackCount,
        int FallbackCommitViolationCount,
        int UnsafeCommitCount,
        int UntargetedBranchDisagreementCount,
        int TripStepCount,
        int MaximumShadowIterations,
        double AverageTriggeredIterations,
        double MaximumMassClosureResidualKilograms,
        double MaximumEnergyClosureResidualJoules,
        double MaximumBalanceMassRateResidualKilogramsPerSecond,
        double MaximumBalancePowerResidualWatts,
        double AverageWallMicroseconds,
        double MaximumWallMicroseconds,
        double AverageAllocatedBytesPerStep,
        long MaximumAllocatedBytesPerStep,
        long ManagedHeapStartBytes,
        long ManagedHeapEndBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections);
}
