using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 14 repaired-closure performance/cost/operational-soak requalification stage 4.
/// Reuses the validated H.28 machine-local relative-cost contract while comparing repaired explicit against repaired
/// corrected ownership, so thermodynamic-repair cost is held common to both modes. No production identity is activated.
/// </summary>
public sealed class PhaseIThermodynamicRepairPerformanceCostOperationalSoakRequalificationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int BenchmarkWarmupSteps = 64;
    private const int BenchmarkSteps = 256;
    private const int SoakSteps = 1_536;
    private const int SoakSampleStride = 32;
    private const int DeterminismSteps = 128;
    private const double MedianWallCostRatioLimit = 8d;
    private const double P95WallCostRatioLimit = 12d;
    private const double MedianAllocationRatioLimit = 16d;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIThermodynamicRepairPerformanceCostOperationalSoakRequalification")]
    public void RepairedClosure_PreservesH28RelativeCostBoundsAndFailClosedOperationalSoak()
    {
        ResetReportDirectory();
        WriteProgress("stage4-start");
        var explicitBenchmark = RunBenchmark("repair-explicit", useFourNodeCorrectedCommit: false);
        WriteProgress("explicit-benchmark-complete");
        var correctedBenchmark = RunBenchmark("repair-corrected", useFourNodeCorrectedCommit: true);
        WriteProgress("corrected-benchmark-complete");
        var soak = RunSoak();
        WriteProgress("operational-soak-complete");
        var deterministicFirst = RunDeterminismControl();
        var deterministicSecond = RunDeterminismControl();
        var deterministicRepeat = string.Equals(deterministicFirst, deterministicSecond, StringComparison.Ordinal);

        var metrics = Classify(explicitBenchmark, correctedBenchmark, soak, deterministicRepeat, deterministicFirst);
        WriteArtifacts(explicitBenchmark, correctedBenchmark, soak, metrics);

        Assert.True(metrics.Stage4PerformanceCostSoakPasses);
    }

    private static BenchmarkRun RunBenchmark(string label, bool useFourNodeCorrectedCommit)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(
                Step,
                useFourNodeCorrectedCommit));
        var expectedMode = useFourNodeCorrectedCommit
            ? HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn
            : HydraulicNumericalCouplingMode.ExplicitCommittedState;
        Assert.Equal(expectedMode, CurrentHydraulics(engine).Mode);

        for (var step = 0; step < BenchmarkWarmupSteps; step++)
        {
            var warmup = engine.Step(ControlRoomRunState.Running);
            Assert.False(warmup.AnyTripActive);
        }

        var rows = new List<BenchmarkRow>(BenchmarkSteps);
        for (var step = 1; step <= BenchmarkSteps; step++)
        {
            ApplyBenchmarkManeuver(engine, step);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            var presentation = engine.Step(ControlRoomRunState.Running);
            var elapsed = Stopwatch.GetTimestamp() - started;
            var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            Assert.False(presentation.AnyTripActive, $"Unexpected Stage-4 benchmark trip in {label} at measured step {step}.");

            rows.Add(CaptureBenchmarkRow(step, elapsed, allocated, presentation, engine, useFourNodeCorrectedCommit));
        }

        return new BenchmarkRun(label, useFourNodeCorrectedCommit, rows);
    }

    private static SoakRun RunSoak()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(
                Step,
                useFourNodeCorrectedCommit: true));
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);

        var heapStart = GC.GetTotalMemory(forceFullCollection: false);
        var gen0Start = GC.CollectionCount(0);
        var gen1Start = GC.CollectionCount(1);
        var gen2Start = GC.CollectionCount(2);
        var rows = new List<SoakRow>(SoakSteps);

        for (var step = 1; step <= SoakSteps; step++)
        {
            ApplyRepeatedSoakManeuver(engine, step);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            var presentation = engine.Step(ControlRoomRunState.Running);
            var elapsed = Stopwatch.GetTimestamp() - started;
            var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);

            var row = CaptureSoakRow(step, elapsed, allocated, presentation, engine);
            Assert.False(row.AnyTripActive, $"Unexpected repaired Stage-4 operational-soak trip at step {step}.");
            AssertFailClosedSafety(row);
            rows.Add(row);

            if (step % 256 == 0 || step == SoakSteps)
            {
                WriteProgress($"soak-progress step={step}/{SoakSteps}");
            }
        }

        return new SoakRun(
            rows,
            heapStart,
            GC.GetTotalMemory(forceFullCollection: false),
            GC.CollectionCount(0) - gen0Start,
            GC.CollectionCount(1) - gen1Start,
            GC.CollectionCount(2) - gen2Start);
    }

    private static Classification Classify(
        BenchmarkRun explicitBenchmark,
        BenchmarkRun correctedBenchmark,
        SoakRun soak,
        bool deterministicRepeat,
        string deterministicFingerprint)
    {
        var explicitWall = explicitBenchmark.Rows.Select(static row => row.WallMicroseconds).ToArray();
        var correctedWall = correctedBenchmark.Rows.Select(static row => row.WallMicroseconds).ToArray();
        var explicitAlloc = explicitBenchmark.Rows.Select(static row => (double)row.AllocatedBytes).ToArray();
        var correctedAlloc = correctedBenchmark.Rows.Select(static row => (double)row.AllocatedBytes).ToArray();

        var explicitMedianWall = Median(explicitWall);
        var correctedMedianWall = Median(correctedWall);
        var explicitP95Wall = Percentile(explicitWall, 0.95d);
        var correctedP95Wall = Percentile(correctedWall, 0.95d);
        var explicitMedianAllocation = Median(explicitAlloc);
        var correctedMedianAllocation = Median(correctedAlloc);
        var medianWallRatio = SafeRatio(correctedMedianWall, explicitMedianWall);
        var p95WallRatio = SafeRatio(correctedP95Wall, explicitP95Wall);
        var allocationRatio = SafeRatio(correctedMedianAllocation, explicitMedianAllocation);

        var correctedRows = correctedBenchmark.Rows;
        var benchmarkTriggers = correctedRows.Count(static row => row.TriggerObserved);
        var benchmarkCommits = correctedRows.Count(static row => row.CorrectedCandidateCommitted);
        var benchmarkRollbacks = correctedRows.Count(static row => row.RollbackRequired);
        var benchmarkFallbackCommitViolations = correctedRows.Count(static row => row.FallbackCommitViolation);
        var benchmarkUnsafeCommits = correctedRows.Count(static row => row.UnsafeCommit);
        var benchmarkUntargetedDisagreements = correctedRows.Count(static row => row.UntargetedBranchDisagreement);

        var soakRows = soak.Rows;
        var soakTriggers = soakRows.Count(static row => row.TriggerObserved);
        var soakCommits = soakRows.Count(static row => row.CorrectedCandidateCommitted);
        var soakRollbacks = soakRows.Count(static row => row.RollbackRequired);
        var soakFallbackCommitViolations = soakRows.Count(static row => row.FallbackCommitViolation);
        var soakUnsafeCommits = soakRows.Count(static row => row.UnsafeCommit);
        var soakUntargetedDisagreements = soakRows.Count(static row => row.UntargetedBranchDisagreement);
        var soakTripSteps = soakRows.Count(static row => row.AnyTripActive);
        var maximumMassClosure = soakRows.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = soakRows.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = soakRows.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = soakRows.Max(static row => row.BalancePowerResidualWatts);

        var relativeCostPasses = medianWallRatio <= MedianWallCostRatioLimit
            && p95WallRatio <= P95WallCostRatioLimit
            && allocationRatio <= MedianAllocationRatioLimit;
        var benchmarkSafetyPasses = benchmarkCommits == benchmarkTriggers
            && benchmarkRollbacks == 0
            && benchmarkFallbackCommitViolations == 0
            && benchmarkUnsafeCommits == 0
            && benchmarkUntargetedDisagreements == 0;
        var soakSafetyPasses = soakTriggers > 0
            && soakCommits == soakTriggers
            && soakRollbacks == 0
            && soakFallbackCommitViolations == 0
            && soakUnsafeCommits == 0
            && soakUntargetedDisagreements == 0
            && soakTripSteps == 0
            && maximumMassClosure <= MaximumMassClosureResidualKilograms
            && maximumEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maximumBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maximumBalancePower <= MaximumBalancePowerResidualWatts;
        var passes = relativeCostPasses && benchmarkSafetyPasses && soakSafetyPasses && deterministicRepeat;
        var performanceClass = relativeCostPasses
            ? (medianWallRatio > 1d || p95WallRatio > 1d ? "bounded-but-costly" : "bounded-at-or-below-explicit")
            : "unbounded-regression";

        return new Classification(
            explicitMedianWall,
            correctedMedianWall,
            explicitP95Wall,
            correctedP95Wall,
            explicitWall.Max(),
            correctedWall.Max(),
            explicitMedianAllocation,
            correctedMedianAllocation,
            medianWallRatio,
            p95WallRatio,
            allocationRatio,
            benchmarkTriggers,
            benchmarkCommits,
            benchmarkRollbacks,
            benchmarkFallbackCommitViolations,
            benchmarkUnsafeCommits,
            benchmarkUntargetedDisagreements,
            soakTriggers,
            soakCommits,
            soakRollbacks,
            soakFallbackCommitViolations,
            soakUnsafeCommits,
            soakUntargetedDisagreements,
            soakTripSteps,
            soakRows.Average(static row => row.WallMicroseconds),
            soakRows.Max(static row => row.WallMicroseconds),
            soakRows.Average(static row => (double)row.AllocatedBytes),
            soakRows.Max(static row => row.AllocatedBytes),
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            deterministicRepeat,
            deterministicFingerprint,
            performanceClass,
            relativeCostPasses,
            benchmarkSafetyPasses,
            soakSafetyPasses,
            passes);
    }

    private static BenchmarkRow CaptureBenchmarkRow(
        int step,
        long elapsedTicks,
        long allocatedBytes,
        ControlRoomSnapshot presentation,
        IntegratedAutomaticOperationRuntimeEngine engine,
        bool corrected)
    {
        var audit = CurrentAudit(engine);
        var telemetry = corrected
            ? Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(CurrentHydraulics(engine).FourNodeBranchContinuity)
            : null;
        var fallbackCommitViolation = telemetry is not null
            && (!telemetry.ShadowCorrectedCandidateEligible || telemetry.RollbackRequired)
            && telemetry.CorrectedCandidateCommitted;
        var unsafeCommit = telemetry is not null
            && telemetry.CorrectedCandidateCommitted
            && !CommitIsQualified(telemetry);

        return new BenchmarkRow(
            step,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            TicksToMicroseconds(elapsedTicks),
            allocatedBytes,
            telemetry?.TriggerObserved ?? false,
            telemetry?.CorrectedCandidateCommitted ?? false,
            telemetry?.RollbackRequired ?? false,
            fallbackCommitViolation,
            unsafeCommit,
            telemetry?.UntargetedBranchDisagreementDetected ?? false,
            telemetry?.ShadowIterationCount ?? 0,
            Math.Abs(audit.MassClosureResidualKilograms),
            Math.Abs(audit.EnergyClosureResidualJoules));
    }

    private static SoakRow CaptureSoakRow(
        int step,
        long elapsedTicks,
        long allocatedBytes,
        ControlRoomSnapshot presentation,
        IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var audit = CurrentAudit(engine);
        var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(CurrentHydraulics(engine).FourNodeBranchContinuity);
        var fallbackCommitViolation = (!telemetry.ShadowCorrectedCandidateEligible || telemetry.RollbackRequired)
            && telemetry.CorrectedCandidateCommitted;
        var unsafeCommit = telemetry.CorrectedCandidateCommitted && !CommitIsQualified(telemetry);

        return new SoakRow(
            step,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            presentation.AnyTripActive,
            telemetry.TriggerObserved,
            telemetry.RollbackRequired,
            telemetry.CorrectedCandidateCommitted,
            fallbackCommitViolation,
            unsafeCommit,
            telemetry.UntargetedBranchDisagreementDetected,
            telemetry.ShadowIterationCount,
            TicksToMicroseconds(elapsedTicks),
            allocatedBytes,
            Math.Abs(audit.MassClosureResidualKilograms),
            Math.Abs(audit.EnergyClosureResidualJoules),
            Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
            Math.Abs(audit.BalancePowerResidualWatts));
    }

    private static string RunDeterminismControl()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(
                Step,
                useFourNodeCorrectedCommit: true));
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
            Assert.False(presentation.AnyTripActive);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(CurrentHydraulics(engine).FourNodeBranchContinuity);
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(presentation)}:{telemetry.TriggerObserved}:{telemetry.ProposedAuthority}:{telemetry.Reason}:{telemetry.RollbackRequired}:{telemetry.CorrectedCommitAuthorized}:{telemetry.CorrectedCandidateCommitted}:{telemetry.CorrectedCommitReason}:{telemetry.ShadowIterationCount}||"));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void ApplyBenchmarkManeuver(IntegratedAutomaticOperationRuntimeEngine engine, int step)
    {
        if (step == 64 || step == 192)
        {
            QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
        }
        else if (step == 128 || step == 224)
        {
            QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadRaise);
        }
    }

    private static void ApplyRepeatedSoakManeuver(IntegratedAutomaticOperationRuntimeEngine engine, int step)
    {
        var cycleStep = ((step - 1) % BenchmarkSteps) + 1;
        ApplyBenchmarkManeuver(engine, cycleStep);
    }

    private static void QueueGeneratorCommand(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomCommandKind kind)
    {
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        engine.QueueOperatorCommand(new ControlRoomCommand(kind, generator.Id, ControlRoomCommandTargetKind.Generator));
    }

    private static void AssertFailClosedSafety(SoakRow row)
    {
        Assert.False(row.FallbackCommitViolation, $"Fallback commit violation at repaired Stage-4 soak step {row.Step}.");
        Assert.False(row.UnsafeCommit, $"Unsafe corrected commit at repaired Stage-4 soak step {row.Step}.");
        Assert.False(row.UntargetedBranchDisagreement, $"Untargeted branch disagreement at repaired Stage-4 soak step {row.Step}.");
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

    private static void WriteArtifacts(BenchmarkRun explicitBenchmark, BenchmarkRun correctedBenchmark, SoakRun soak, Classification metrics)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var benchmarkCsv = new List<string>
        {
            "mode,step,presentation_fingerprint,wall_microseconds,allocated_bytes,trigger_observed,corrected_committed,rollback_required,fallback_commit_violation,unsafe_commit,untargeted_disagreement,shadow_iterations,mass_closure_kg,energy_closure_j",
        };
        benchmarkCsv.AddRange(explicitBenchmark.Rows.Select(row => row.ToCsv(explicitBenchmark.Label)));
        benchmarkCsv.AddRange(correctedBenchmark.Rows.Select(row => row.ToCsv(correctedBenchmark.Label)));
        File.WriteAllLines(Path.Combine(directory, "02-repaired-performance-benchmark.csv"), benchmarkCsv, Utf8WithoutBom);

        var soakCsv = new List<string>
        {
            "step,presentation_fingerprint,any_trip,trigger_observed,rollback_required,corrected_committed,fallback_commit_violation,unsafe_commit,untargeted_disagreement,shadow_iterations,wall_microseconds,allocated_bytes,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w",
        };
        soakCsv.AddRange(soak.Rows
            .Where(static row => row.Step == 1 || row.Step % SoakSampleStride == 0)
            .Select(static row => row.ToCsv()));
        File.WriteAllLines(Path.Combine(directory, "03-repaired-operational-soak-samples.csv"), soakCsv, Utf8WithoutBom);

        var heapDelta = soak.ManagedHeapEndBytes - soak.ManagedHeapStartBytes;
        var wallSecondsPerSimulatedSecondExplicit = explicitBenchmark.Rows.Sum(static row => row.WallMicroseconds) / 1_000_000d / (BenchmarkSteps * Step.TotalSeconds);
        var wallSecondsPerSimulatedSecondCorrected = correctedBenchmark.Rows.Sum(static row => row.WallMicroseconds) / 1_000_000d / (BenchmarkSteps * Step.TotalSeconds);

        var summary = new[]
        {
            "=== 01-i5-thermodynamic-repair-performance-cost-operational-soak-requalification-stage4 ===",
            "scope=validated CorrelationConsistentInverseDomain repair evidence seam only; paired repair-explicit vs repair-corrected H.28-style machine-local relative-cost benchmark plus bounded corrected operational soak; registered/default runtimes unchanged;",
            FormattableString.Invariant($"benchmark-warmup-steps-per-mode={BenchmarkWarmupSteps}; benchmark-steps-per-mode={BenchmarkSteps}; explicit-median-step-us={metrics.ExplicitMedianWallMicroseconds:G17}; corrected-median-step-us={metrics.CorrectedMedianWallMicroseconds:G17}; median-wall-cost-ratio={metrics.MedianWallCostRatio:G17}; median-wall-cost-ratio-limit={MedianWallCostRatioLimit:G17}; repaired-performance-class={metrics.PerformanceClass};"),
            FormattableString.Invariant($"explicit-p95-step-us={metrics.ExplicitP95WallMicroseconds:G17}; corrected-p95-step-us={metrics.CorrectedP95WallMicroseconds:G17}; p95-wall-cost-ratio={metrics.P95WallCostRatio:G17}; p95-wall-cost-ratio-limit={P95WallCostRatioLimit:G17}; explicit-max-step-us={metrics.ExplicitMaximumWallMicroseconds:G17}; corrected-max-step-us={metrics.CorrectedMaximumWallMicroseconds:G17};"),
            FormattableString.Invariant($"explicit-median-allocated-bytes={metrics.ExplicitMedianAllocatedBytes:G17}; corrected-median-allocated-bytes={metrics.CorrectedMedianAllocatedBytes:G17}; median-allocation-ratio={metrics.MedianAllocationRatio:G17}; median-allocation-ratio-limit={MedianAllocationRatioLimit:G17}; corrected-triggered={metrics.BenchmarkTriggers}/{BenchmarkSteps}; corrected-committed={metrics.BenchmarkCommits}/{BenchmarkSteps}; corrected-rollbacks={metrics.BenchmarkRollbacks}; corrected-fallback-commit-violations={metrics.BenchmarkFallbackCommitViolations}; corrected-unsafe-commits={metrics.BenchmarkUnsafeCommits}; corrected-untargeted-disagreements={metrics.BenchmarkUntargetedDisagreements};"),
            FormattableString.Invariant($"wall-seconds-per-simulated-second-explicit/corrected={wallSecondsPerSimulatedSecondExplicit:G17}/{wallSecondsPerSimulatedSecondCorrected:G17}; benchmark-load-manoeuvre=5-to-0-to-5-twice;"),
            FormattableString.Invariant($"soak-steps={SoakSteps}; soak-simulated-seconds={SoakSteps * Step.TotalSeconds:G17}; soak-manoeuvre=six-repetitions-of-h28-benchmark-load-cycle; soak-triggered={metrics.SoakTriggers}; soak-committed={metrics.SoakCommits}; soak-rollbacks={metrics.SoakRollbacks}; soak-fallback-commit-violations={metrics.SoakFallbackCommitViolations}; soak-unsafe-commits={metrics.SoakUnsafeCommits}; soak-untargeted-branch-disagreements={metrics.SoakUntargetedDisagreements}; soak-trip-steps={metrics.SoakTripSteps};"),
            FormattableString.Invariant($"soak-average-step-us={metrics.SoakAverageWallMicroseconds:G17}; soak-max-step-us={metrics.SoakMaximumWallMicroseconds:G17}; soak-average-allocated-bytes-per-step={metrics.SoakAverageAllocatedBytes:G17}; soak-max-allocated-bytes-per-step={metrics.SoakMaximumAllocatedBytes}; soak-managed-heap-start-bytes={soak.ManagedHeapStartBytes}; soak-managed-heap-end-bytes={soak.ManagedHeapEndBytes}; soak-managed-heap-delta-bytes={heapDelta}; gc-collections-gen0/gen1/gen2={soak.Gen0Collections}/{soak.Gen1Collections}/{soak.Gen2Collections};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={metrics.MaximumMassClosure:G17}; max-network-energy-closure-j={metrics.MaximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={metrics.MaximumBalanceMassRate:G17}; max-network-balance-power-w={metrics.MaximumBalancePower:G17};"),
            $"determinism-control-steps={DeterminismSteps}; deterministic-repeat={metrics.DeterministicRepeat}; deterministic-fingerprint={metrics.DeterministicFingerprint}; relative-cost-bounds-pass={metrics.RelativeCostPasses}; benchmark-safety-passes={metrics.BenchmarkSafetyPasses}; soak-safety-passes={metrics.SoakSafetyPasses};",
            "default-current-v2-mode=ExplicitCommittedState; repaired-closure=CorrelationConsistentInverseDomain; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; production-activation=False; production-fixed-step=10.000 ms; H20-contract-replaced=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;",
            $"stage4-performance-cost-operational-soak-passes={metrics.Stage4PerformanceCostSoakPasses}; production-activation=False;",
            "interpretation=green means the repaired closure preserves the original H.28 machine-local relative-cost ceilings while corrected ownership remains fail-closed, deterministic and conservation-bounded through the repaired operational soak. Timing values remain machine-local evidence; only the frozen relative ceilings are acceptance authority;",
            "next-step=if Stage 4 is green, create a new exact repaired desktop identity without reinterpreting historical @2/@3, then run a narrow activation audit before the final scheduled-long/reference-plant/I.3/cumulative Phase-I closure;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-thermodynamic-repair-performance-cost-operational-soak-stage4.summary.txt"), summary, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "04-i5-thermodynamic-repair-performance-cost-operational-soak-stage4-metrics.csv"), new[]
        {
            "metric,value",
            FormattableString.Invariant($"median_wall_cost_ratio,{metrics.MedianWallCostRatio:G17}"),
            FormattableString.Invariant($"p95_wall_cost_ratio,{metrics.P95WallCostRatio:G17}"),
            FormattableString.Invariant($"median_allocation_ratio,{metrics.MedianAllocationRatio:G17}"),
            $"performance_class,{metrics.PerformanceClass}",
            $"benchmark_triggers,{metrics.BenchmarkTriggers}",
            $"benchmark_commits,{metrics.BenchmarkCommits}",
            $"soak_steps,{SoakSteps}",
            $"soak_triggers,{metrics.SoakTriggers}",
            $"soak_commits,{metrics.SoakCommits}",
            $"soak_rollbacks,{metrics.SoakRollbacks}",
            $"soak_trip_steps,{metrics.SoakTripSteps}",
            FormattableString.Invariant($"soak_average_step_us,{metrics.SoakAverageWallMicroseconds:G17}"),
            FormattableString.Invariant($"soak_average_allocated_bytes,{metrics.SoakAverageAllocatedBytes:G17}"),
            $"deterministic_repeat,{metrics.DeterministicRepeat}",
            $"deterministic_fingerprint,{metrics.DeterministicFingerprint}",
            $"relative_cost_bounds_pass,{metrics.RelativeCostPasses}",
            $"benchmark_safety_pass,{metrics.BenchmarkSafetyPasses}",
            $"soak_safety_pass,{metrics.SoakSafetyPasses}",
            $"stage4_passes,{metrics.Stage4PerformanceCostSoakPasses}",
        }, Utf8WithoutBom);
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;

    private static double Median(IReadOnlyList<double> values)
    {
        Assert.NotEmpty(values);
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2d
            : ordered[middle];
    }

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        Assert.NotEmpty(values);
        var ordered = values.Order().ToArray();
        var index = (int)Math.Ceiling(percentile * ordered.Length) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
    }

    private static double SafeRatio(double numerator, double denominator)
        => Math.Abs(denominator) > 1e-12d
            ? numerator / denominator
            : (Math.Abs(numerator) <= 1e-12d ? 1d : double.PositiveInfinity);

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-thermodynamic-repair-performance-cost-operational-soak-stage4");

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
        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("stage4-report-directory-ready");
    }

    private static void WriteProgress(string message)
        => File.AppendAllLines(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            new[] { $"{DateTimeOffset.UtcNow:O} {message}" },
            Utf8WithoutBom);

    private sealed record BenchmarkRun(string Label, bool Corrected, IReadOnlyList<BenchmarkRow> Rows);

    private sealed record BenchmarkRow(
        int Step,
        string PresentationFingerprint,
        double WallMicroseconds,
        long AllocatedBytes,
        bool TriggerObserved,
        bool CorrectedCandidateCommitted,
        bool RollbackRequired,
        bool FallbackCommitViolation,
        bool UnsafeCommit,
        bool UntargetedBranchDisagreement,
        int ShadowIterationCount,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules)
    {
        public string ToCsv(string mode)
            => FormattableString.Invariant(
                $"{mode},{Step},{PresentationFingerprint},{WallMicroseconds:G17},{AllocatedBytes},{TriggerObserved},{CorrectedCandidateCommitted},{RollbackRequired},{FallbackCommitViolation},{UnsafeCommit},{UntargetedBranchDisagreement},{ShadowIterationCount},{MassClosureResidualKilograms:G17},{EnergyClosureResidualJoules:G17}");
    }

    private sealed record SoakRun(
        IReadOnlyList<SoakRow> Rows,
        long ManagedHeapStartBytes,
        long ManagedHeapEndBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections);

    private sealed record SoakRow(
        int Step,
        string PresentationFingerprint,
        bool AnyTripActive,
        bool TriggerObserved,
        bool RollbackRequired,
        bool CorrectedCandidateCommitted,
        bool FallbackCommitViolation,
        bool UnsafeCommit,
        bool UntargetedBranchDisagreement,
        int ShadowIterationCount,
        double WallMicroseconds,
        long AllocatedBytes,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts)
    {
        public string ToCsv()
            => FormattableString.Invariant(
                $"{Step},{PresentationFingerprint},{AnyTripActive},{TriggerObserved},{RollbackRequired},{CorrectedCandidateCommitted},{FallbackCommitViolation},{UnsafeCommit},{UntargetedBranchDisagreement},{ShadowIterationCount},{WallMicroseconds:G17},{AllocatedBytes},{MassClosureResidualKilograms:G17},{EnergyClosureResidualJoules:G17},{BalanceMassRateResidualKilogramsPerSecond:G17},{BalancePowerResidualWatts:G17}");
    }

    private sealed record Classification(
        double ExplicitMedianWallMicroseconds,
        double CorrectedMedianWallMicroseconds,
        double ExplicitP95WallMicroseconds,
        double CorrectedP95WallMicroseconds,
        double ExplicitMaximumWallMicroseconds,
        double CorrectedMaximumWallMicroseconds,
        double ExplicitMedianAllocatedBytes,
        double CorrectedMedianAllocatedBytes,
        double MedianWallCostRatio,
        double P95WallCostRatio,
        double MedianAllocationRatio,
        int BenchmarkTriggers,
        int BenchmarkCommits,
        int BenchmarkRollbacks,
        int BenchmarkFallbackCommitViolations,
        int BenchmarkUnsafeCommits,
        int BenchmarkUntargetedDisagreements,
        int SoakTriggers,
        int SoakCommits,
        int SoakRollbacks,
        int SoakFallbackCommitViolations,
        int SoakUnsafeCommits,
        int SoakUntargetedDisagreements,
        int SoakTripSteps,
        double SoakAverageWallMicroseconds,
        double SoakMaximumWallMicroseconds,
        double SoakAverageAllocatedBytes,
        long SoakMaximumAllocatedBytes,
        double MaximumMassClosure,
        double MaximumEnergyClosure,
        double MaximumBalanceMassRate,
        double MaximumBalancePower,
        bool DeterministicRepeat,
        string DeterministicFingerprint,
        string PerformanceClass,
        bool RelativeCostPasses,
        bool BenchmarkSafetyPasses,
        bool SoakSafetyPasses,
        bool Stage4PerformanceCostSoakPasses);
}
