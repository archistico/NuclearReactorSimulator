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
/// M10.9.4.1-H.28.1-E exact CPU-tail optimization gate. It starts from validated H.28.1-D, freezes the
/// failed H.28 Requalification 1 p95 evidence, and removes only exactly reusable thermodynamic/hydraulic
/// work inside the unchanged 32-probe finite-difference Newton path.
/// </summary>
public sealed class FourNodeHydraulicProbeCpuTailReductionAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int WarmupSteps = 64;
    private const int AttributionSteps = 256;
    private const int DeterminismSteps = 128;
    private const string H28DeterministicFingerprint = "518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38";

    private const double H281DBaselineTriggerEngineMicroseconds = 299_634.61499999993d;
    private const double H281DBaselineH9Microseconds = 277_483.07500000001d;
    private const double H281DBaselineJacobianMicroseconds = 228_904.07500000001d;
    private const double H281DBaselineJacobianAllocatedBytes = 549_182.80000000005d;
    private const double H281DBaselineH9AllocatedBytes = 628_328d;
    private const double H281DBaselineNonTriggerPredictorMicroseconds = 114.23432203389834d;

    // Frozen H.28 Requalification 1 machine-local p95 evidence. H.28 itself remains the authority; this
    // stricter triggered-tail readiness check prevents rerunning H.28 unless E has enough measured headroom.
    private const double H28Requalification1ExplicitP95Microseconds = 7_365.1000000000004d;
    private const double H28P95RatioLimit = 12d;
    private const double MaximumTriggeredP95Microseconds = H28Requalification1ExplicitP95Microseconds * H28P95RatioLimit;

    private const double MaximumJacobianWallFractionOfH281D = 0.40d;
    private const double MaximumH9WallFractionOfH281D = 0.40d;
    private const double MaximumTriggerEngineWallFractionOfH281D = 0.35d;
    private const double MaximumJacobianAllocationFractionOfH281D = 1.25d;
    private const double MaximumH9AllocationFractionOfH281D = 1.25d;
    private const double MaximumNonTriggerPredictorWallFractionOfH281D = 1.50d;
    private const double MinimumHydraulicComponentReuseFraction = 0.50d;

    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    private static readonly IReadOnlyDictionary<string, string> FrozenH281DValidatedFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_1D_ValidatedHydraulicProbeCpuHotPathOptimizationSummary.txt"] = "239CFEFB6F64FBCDCE59D24FA9B357487E05360819E25E7158054F13A4B27C64",
        ["H28_1D_ValidatedHydraulicProbeCpuHotPathOptimizationSteps.csv"] = "ACF2D9E771FACDCC791A3276DDF2C1053708250C3A8240674F231E71DF1AD8DA",
        ["H28_1D_ValidatedHydraulicProbeCpuHotPathOptimizationCostCenters.csv"] = "107029B28DA825EAB868ECC50B4938E842725AD8AEF63C7EF8EB15A20B1D50D5",
        ["H28_1D_ValidatedHydraulicProbeCpuHotPathOptimizationMetrics.csv"] = "302BA442FDCD00E007ED7BC6B39FA427C3370CBB9FA166CBAD7C8C5289F9E754",
    };

    private static readonly IReadOnlyDictionary<string, string> FrozenH28Requalification1FailureFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_Requalification1_FailedPerformanceCostSoakSummary.txt"] = "1F4959DA935AE036798F813B00DB760B9FCBC703946D07C7E9E24E7E6DB49D10",
        ["H28_Requalification1_FailedPerformanceBenchmark.csv"] = "C31E4C2C1D728CB5309BBA370D83D334EA35C6F2518DC38CD4A3E64E87AF02D5",
        ["H28_Requalification1_FailedOperationalSoakSamples.csv"] = "BE60348C5345B495C81DA4F5140F8DBC0A0187F6DEB9F14F075D3967D3E5742F",
        ["H28_Requalification1_FailedPerformanceCostSoakMetrics.csv"] = "0062DF097AE323D5E78E27C8CCE7EBE3436B8DE3AA3E57D879A9F73953C4BE48",
    };

    [Fact]
    public void FrozenValidatedH281DEvidence_AnchorsExactCpuTailOptimizationToValidatedRuntime()
    {
        foreach (var expected in FrozenH281DValidatedFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen validated-H.28.1-D evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_1D_ValidatedHydraulicProbeCpuHotPathOptimizationSummary.txt"));
        Assert.Contains("trigger-average-engine-us=299634.61499999993", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-total-us=277483.07500000001", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-jacobian-build-us=228904.07500000001", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-hydraulic-evaluations=35", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-probe-evaluations=32", summary, StringComparison.Ordinal);
        Assert.Contains($"deterministic-fingerprint={H28DeterministicFingerprint}", summary, StringComparison.Ordinal);
        Assert.Contains("h28.1d-hydraulic-probe-cpu-hot-path-optimization-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FrozenFailedH28Requalification1Evidence_DefinesTheUnchangedP95ProblemToBeat()
    {
        foreach (var expected in FrozenH28Requalification1FailureFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen failed-H.28 Requalification 1 evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_Requalification1_FailedPerformanceCostSoakSummary.txt"));
        Assert.Contains("median-wall-cost-ratio=4.5869950614157275", summary, StringComparison.Ordinal);
        Assert.Contains("p95-wall-cost-ratio=38.713649509171638", summary, StringComparison.Ordinal);
        Assert.Contains("median-allocation-ratio=1.1156355393376458", summary, StringComparison.Ordinal);
        Assert.Contains("explicit-p95-step-us=7365.1000000000004", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-p95-step-us=285129.90000000002", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-triggered=20/256", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-performance-cost-operational-soak-passes=False", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeHydraulicProbeCpuTailReductionAudit")]
    public void ExactIncrementalProbeEvaluation_ReducesTriggeredTailWithoutChangingNumericalContract()
    {
        ResetProgress();
        using var measurementScope = PerformanceAttributionMeasurement.Push(
            static () => Stopwatch.GetTimestamp(),
            static () => GC.GetAllocatedBytesForCurrentThread());
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);

        for (var step = 0; step < WarmupSteps; step++)
        {
            Assert.False(engine.Step(ControlRoomRunState.Running).AnyTripActive);
        }

        WriteProgress("attribution-start");
        var rows = new List<AttributionRow>(AttributionSteps);
        for (var step = 1; step <= AttributionSteps; step++)
        {
            ApplyBenchmarkManeuver(engine, step);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            var presentation = engine.Step(ControlRoomRunState.Running);
            var elapsed = Stopwatch.GetTimestamp() - started;
            var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            Assert.False(presentation.AnyTripActive);

            var numerics = CurrentHydraulics(engine);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            Assert.True(
                FourNodeBranchContinuityPerformanceAttributionRegistry.TryGet(telemetry, out var attribution),
                $"Missing H.28.1-E CPU-tail attribution for step {step}.");
            Assert.NotNull(attribution);
            var audit = CurrentAudit(engine);
            var fallbackCommitViolation = (!telemetry.ShadowCorrectedCandidateEligible || telemetry.RollbackRequired)
                && telemetry.CorrectedCandidateCommitted;
            var unsafeCommit = telemetry.CorrectedCandidateCommitted && !CommitIsQualified(telemetry);
            Assert.False(fallbackCommitViolation);
            Assert.False(unsafeCommit);
            Assert.False(telemetry.UntargetedBranchDisagreementDetected);
            Assert.InRange(Math.Abs(audit.MassClosureResidualKilograms), 0d, MaximumMassClosureResidualKilograms);
            Assert.InRange(Math.Abs(audit.EnergyClosureResidualJoules), 0d, MaximumEnergyClosureResidualJoules);
            Assert.InRange(Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond), 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
            Assert.InRange(Math.Abs(audit.BalancePowerResidualWatts), 0d, MaximumBalancePowerResidualWatts);

            var h9 = attribution!.H9;
            if (telemetry.TriggerObserved)
            {
                Assert.NotNull(h9);
                Assert.Equal(35, attribution.HydraulicEvaluationCount);
                Assert.Equal(32, attribution.ProbeEvaluationCount);
                Assert.Equal(32, attribution.MaximumJacobianDimension);
                Assert.True(telemetry.CorrectedCandidateCommitted);
            }
            else
            {
                Assert.Null(h9);
                Assert.Equal(0, attribution.HydraulicEvaluationCount);
                Assert.Equal(0, attribution.ProbeEvaluationCount);
            }

            rows.Add(ToRow(step, presentation, telemetry, attribution, h9, elapsed, allocated));
            if (step % 64 == 0)
            {
                WriteProgress($"attribution-progress step={step}/{AttributionSteps} triggers={rows.Count(static row => row.TriggerObserved)}");
            }
        }

        var triggered = rows.Where(static row => row.TriggerObserved).ToArray();
        var nonTriggered = rows.Where(static row => !row.TriggerObserved).ToArray();
        Assert.Equal(20, triggered.Length);
        Assert.NotEmpty(nonTriggered);
        Assert.Equal(20, rows.Count(static row => row.CorrectedCandidateCommitted));
        Assert.Equal(0, rows.Count(static row => row.RollbackRequired));
        Assert.Equal(0, rows.Count(static row => row.UnsafeCommit));
        Assert.Equal(0, rows.Count(static row => row.FallbackCommitViolation));
        Assert.All(triggered, static row => Assert.Equal(35, row.HydraulicEvaluationCount));
        Assert.All(triggered, static row => Assert.Equal(32, row.ProbeEvaluationCount));
        Assert.All(triggered, static row => Assert.Equal(32, row.MaximumJacobianDimension));
        Assert.All(triggered, static row => Assert.True(row.ProbeHydraulicComponentCount > 0));

        var averageJacobianMicroseconds = triggered.Average(static row => row.H9JacobianBuildMicroseconds);
        var averageH9Microseconds = triggered.Average(static row => row.H9TotalMicroseconds);
        var averageTriggerEngineMicroseconds = triggered.Average(static row => row.EngineMicroseconds);
        var triggeredP95Microseconds = Percentile(triggered.Select(static row => row.EngineMicroseconds).ToArray(), 0.95d);
        var averageJacobianAllocatedBytes = triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes);
        var averageH9AllocatedBytes = triggered.Average(static row => (double)row.H9TotalAllocatedBytes);
        var averageNonTriggerPredictorMicroseconds = nonTriggered.Average(static row => row.PredictorMicroseconds);
        var hydraulicComponentReuseFraction = SafeRatio(
            triggered.Sum(static row => row.ProbeHydraulicComponentReuseCount),
            triggered.Sum(static row => row.ProbeHydraulicComponentCount));
        var estimatedH28TriggeredP95Ratio = SafeRatio(triggeredP95Microseconds, H28Requalification1ExplicitP95Microseconds);

        var jacobianWallPasses = averageJacobianMicroseconds <= H281DBaselineJacobianMicroseconds * MaximumJacobianWallFractionOfH281D;
        var h9WallPasses = averageH9Microseconds <= H281DBaselineH9Microseconds * MaximumH9WallFractionOfH281D;
        var triggerEngineWallPasses = averageTriggerEngineMicroseconds <= H281DBaselineTriggerEngineMicroseconds * MaximumTriggerEngineWallFractionOfH281D;
        var tailReadyForH28 = triggeredP95Microseconds <= MaximumTriggeredP95Microseconds;
        var componentReusePasses = hydraulicComponentReuseFraction >= MinimumHydraulicComponentReuseFraction;
        var jacobianAllocationPasses = averageJacobianAllocatedBytes <= H281DBaselineJacobianAllocatedBytes * MaximumJacobianAllocationFractionOfH281D;
        var h9AllocationPasses = averageH9AllocatedBytes <= H281DBaselineH9AllocatedBytes * MaximumH9AllocationFractionOfH281D;
        var predictorRegressionPasses = averageNonTriggerPredictorMicroseconds <= H281DBaselineNonTriggerPredictorMicroseconds * MaximumNonTriggerPredictorWallFractionOfH281D;

        WriteProgress("determinism-control-start");
        var determinism = RunDeterminismControl();
        var determinismPasses = string.Equals(H28DeterministicFingerprint, determinism, StringComparison.Ordinal);

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, CurrentHydraulics(defaultEngine).Mode);

        var passes = jacobianWallPasses
            && h9WallPasses
            && triggerEngineWallPasses
            && tailReadyForH28
            && componentReusePasses
            && jacobianAllocationPasses
            && h9AllocationPasses
            && predictorRegressionPasses
            && determinismPasses;
        WriteReports(rows, triggered, nonTriggered, determinism, passes);

        Assert.True(componentReusePasses, $"H.28.1-E hydraulic component reuse was too low: {hydraulicComponentReuseFraction:G17}.");
        Assert.True(jacobianWallPasses, $"H.28.1-E Jacobian wall reduction insufficient. D={H281DBaselineJacobianMicroseconds:G17} us; E={averageJacobianMicroseconds:G17} us.");
        Assert.True(h9WallPasses, $"H.28.1-E H.9 wall reduction insufficient. D={H281DBaselineH9Microseconds:G17} us; E={averageH9Microseconds:G17} us.");
        Assert.True(triggerEngineWallPasses, $"H.28.1-E trigger wall reduction insufficient. D={H281DBaselineTriggerEngineMicroseconds:G17} us; E={averageTriggerEngineMicroseconds:G17} us.");
        Assert.True(tailReadyForH28, $"H.28.1-E triggered p95 is not ready for unchanged H.28. H.28 Requalification 1 explicit p95={H28Requalification1ExplicitP95Microseconds:G17} us; unchanged ratio limit={H28P95RatioLimit:G17}; E triggered p95={triggeredP95Microseconds:G17} us; estimated ratio={estimatedH28TriggeredP95Ratio:G17}.");
        Assert.True(jacobianAllocationPasses, "H.28.1-E regressed validated H.28.1-D Jacobian allocation gains.");
        Assert.True(h9AllocationPasses, "H.28.1-E regressed validated H.28.1-D H.9 allocation gains.");
        Assert.True(predictorRegressionPasses, "H.28.1-E regressed validated H.28.1-D non-trigger predictor cost.");
        Assert.Equal(H28DeterministicFingerprint, determinism);
        WriteProgress("H.28.1-E exact hydraulic-probe CPU-tail optimization completed");
    }

    private static AttributionRow ToRow(
        int step,
        ControlRoomSnapshot presentation,
        FourNodeBranchContinuityIntegrationTelemetry telemetry,
        FourNodeBranchContinuityPerformanceAttribution attribution,
        JacobianHydraulicCorrectorPerformanceAttribution? h9,
        long engineElapsedTicks,
        long engineAllocatedBytes)
        => new(
            step,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            telemetry.TriggerObserved,
            telemetry.CorrectedCandidateCommitted,
            telemetry.RollbackRequired,
            telemetry.CorrectedCandidateCommitted && !CommitIsQualified(telemetry),
            (!telemetry.ShadowCorrectedCandidateEligible || telemetry.RollbackRequired) && telemetry.CorrectedCandidateCommitted,
            telemetry.ShadowIterationCount,
            TicksToMicroseconds(engineElapsedTicks),
            engineAllocatedBytes,
            TicksToMicroseconds(attribution.PredictorElapsedTicks),
            attribution.PredictorAllocatedBytes,
            TicksToMicroseconds(attribution.CorrectorElapsedTicks),
            attribution.CorrectorAllocatedBytes,
            h9 is null ? 0d : TicksToMicroseconds(h9.TotalElapsedTicks),
            h9?.TotalAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.JacobianBuildElapsedTicks),
            h9?.JacobianBuildAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.NewtonLineSearchElapsedTicks),
            h9?.NewtonLineSearchAllocatedBytes ?? 0L,
            h9?.ProbeAppliedFluidNodeReuseCount ?? 0,
            h9?.ProbeAppliedFluidNodeCount ?? 0,
            h9?.ProbeMappedFluidNodeReuseCount ?? 0,
            h9?.ProbeMappedFluidNodeCount ?? 0,
            h9?.ProbeHydraulicComponentReuseCount ?? 0,
            h9?.ProbeHydraulicComponentCount ?? 0,
            attribution.HydraulicEvaluationCount,
            attribution.ProbeEvaluationCount,
            attribution.MaximumJacobianDimension);

    private static void WriteReports(
        IReadOnlyList<AttributionRow> rows,
        IReadOnlyList<AttributionRow> triggered,
        IReadOnlyList<AttributionRow> nonTriggered,
        string determinismFingerprint,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var stepCsv = new List<string>
        {
            "step,presentation_fingerprint,trigger_observed,corrected_committed,rollback_required,unsafe_commit,fallback_commit_violation,shadow_iterations,engine_us,engine_alloc_bytes,predictor_us,predictor_alloc_bytes,corrector_us,corrector_alloc_bytes,h9_total_us,h9_total_alloc_bytes,h9_jacobian_us,h9_jacobian_alloc_bytes,h9_line_search_us,h9_line_search_alloc_bytes,probe_applied_reused,probe_applied_total,probe_mapped_reused,probe_mapped_total,probe_hydraulic_components_reused,probe_hydraulic_components_total,hydraulic_evaluations,probe_evaluations,max_jacobian_dimension",
        };
        stepCsv.AddRange(rows.Select(static row => row.ToCsv()));
        File.WriteAllLines(Path.Combine(directory, "02-hydraulic-probe-cpu-tail-reduction-steps.csv"), stepCsv, Utf8WithoutBom);

        var averageJacobian = triggered.Average(static row => row.H9JacobianBuildMicroseconds);
        var averageH9 = triggered.Average(static row => row.H9TotalMicroseconds);
        var averageTrigger = triggered.Average(static row => row.EngineMicroseconds);
        var triggeredP95 = Percentile(triggered.Select(static row => row.EngineMicroseconds).ToArray(), 0.95d);
        var componentReuseFraction = SafeRatio(
            triggered.Sum(static row => row.ProbeHydraulicComponentReuseCount),
            triggered.Sum(static row => row.ProbeHydraulicComponentCount));
        var estimatedH28P95Ratio = SafeRatio(triggeredP95, H28Requalification1ExplicitP95Microseconds);

        var summary = new[]
        {
            "=== 01-current-v2-hydraulic-probe-cpu-tail-reduction ===",
            "H.28.1-E starts from validated H.28.1-D and preserves the unchanged 32-probe finite-difference Newton contract while fusing duplicate branch-continuity inverse-map work and reusing pipe/valve/pump component results only when every dependency is the exact reference from the unperturbed evaluation.",
            FormattableString.Invariant($"warmup-steps={WarmupSteps}; attribution-steps={AttributionSteps}; triggered={triggered.Count}; committed={rows.Count(static row => row.CorrectedCandidateCommitted)}; rollbacks={rows.Count(static row => row.RollbackRequired)}; unsafe-commits={rows.Count(static row => row.UnsafeCommit)}; fallback-commit-violations={rows.Count(static row => row.FallbackCommitViolation)};"),
            FormattableString.Invariant($"nontrigger-average-engine-us={nonTriggered.Average(static row => row.EngineMicroseconds):G17}; nontrigger-average-predictor-us={nonTriggered.Average(static row => row.PredictorMicroseconds):G17}; nontrigger-average-predictor-alloc-bytes={nonTriggered.Average(static row => (double)row.PredictorAllocatedBytes):G17};"),
            FormattableString.Invariant($"trigger-average-engine-us={averageTrigger:G17}; trigger-p95-engine-us={triggeredP95:G17}; trigger-average-corrector-us={triggered.Average(static row => row.CorrectorMicroseconds):G17};"),
            FormattableString.Invariant($"h9-average-total-us={averageH9:G17}; h9-average-jacobian-build-us={averageJacobian:G17}; h9-average-line-search-us={triggered.Average(static row => row.H9NewtonLineSearchMicroseconds):G17}; h9-average-hydraulic-evaluations={triggered.Average(static row => row.HydraulicEvaluationCount):G17}; h9-average-probe-evaluations={triggered.Average(static row => row.ProbeEvaluationCount):G17}; h9-max-jacobian-dimension={triggered.Max(static row => row.MaximumJacobianDimension)};"),
            FormattableString.Invariant($"probe-hydraulic-components-reused={triggered.Sum(static row => row.ProbeHydraulicComponentReuseCount)}; probe-hydraulic-components-total={triggered.Sum(static row => row.ProbeHydraulicComponentCount)}; probe-hydraulic-component-reuse-fraction={componentReuseFraction:G17}; probe-applied-fluid-node-reuse-fraction={SafeRatio(triggered.Sum(static row => row.ProbeAppliedFluidNodeReuseCount), triggered.Sum(static row => row.ProbeAppliedFluidNodeCount)):G17}; probe-mapped-fluid-node-reuse-fraction={SafeRatio(triggered.Sum(static row => row.ProbeMappedFluidNodeReuseCount), triggered.Sum(static row => row.ProbeMappedFluidNodeCount)):G17};"),
            FormattableString.Invariant($"h28.1d-baseline-jacobian-us={H281DBaselineJacobianMicroseconds:G17}; jacobian-wall-fraction-of-h28.1d={SafeRatio(averageJacobian, H281DBaselineJacobianMicroseconds):G17}; h28.1d-baseline-h9-us={H281DBaselineH9Microseconds:G17}; h9-wall-fraction-of-h28.1d={SafeRatio(averageH9, H281DBaselineH9Microseconds):G17}; h28.1d-baseline-trigger-engine-us={H281DBaselineTriggerEngineMicroseconds:G17}; trigger-engine-wall-fraction-of-h28.1d={SafeRatio(averageTrigger, H281DBaselineTriggerEngineMicroseconds):G17};"),
            FormattableString.Invariant($"h28-requalification1-explicit-p95-us={H28Requalification1ExplicitP95Microseconds:G17}; unchanged-h28-p95-ratio-limit={H28P95RatioLimit:G17}; unchanged-h28-trigger-tail-threshold-us={MaximumTriggeredP95Microseconds:G17}; h28.1e-triggered-p95-estimated-ratio={estimatedH28P95Ratio:G17}; tail-ready-for-h28={triggeredP95 <= MaximumTriggeredP95Microseconds};"),
            FormattableString.Invariant($"h9-average-total-alloc-bytes={triggered.Average(static row => (double)row.H9TotalAllocatedBytes):G17}; h9-average-jacobian-build-alloc-bytes={triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes):G17};"),
            FormattableString.Invariant($"determinism-control-steps={DeterminismSteps}; deterministic-fingerprint={determinismFingerprint}; matches-failed-H28-fingerprint={string.Equals(determinismFingerprint, H28DeterministicFingerprint, StringComparison.Ordinal)}; default-current-v2-mode=ExplicitCommittedState;"),
            $"h28.1e-hydraulic-probe-cpu-tail-reduction-passes={passes}; h28-remains-failed=True; H29-default-activation-blocked=True;",
            "H.28.1-E recommendation: rerun the unchanged H.28 only if the exact CPU-tail gate is green. Do not raise H.28 ceilings, reduce the 32 probes, retune H.9, or alter P060/F040 from this optimization evidence.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-hydraulic-probe-cpu-tail-reduction.summary.txt"), summary, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "03-hydraulic-probe-cpu-tail-reduction-metrics.csv"), new[]
        {
            "metric,value",
            $"attribution_steps,{AttributionSteps}",
            $"triggered,{triggered.Count}",
            FormattableString.Invariant($"trigger_average_engine_us,{averageTrigger:G17}"),
            FormattableString.Invariant($"trigger_p95_engine_us,{triggeredP95:G17}"),
            FormattableString.Invariant($"h9_average_total_us,{averageH9:G17}"),
            FormattableString.Invariant($"h9_average_jacobian_us,{averageJacobian:G17}"),
            FormattableString.Invariant($"hydraulic_component_reuse_fraction,{componentReuseFraction:G17}"),
            FormattableString.Invariant($"jacobian_wall_fraction_of_h28_1d,{SafeRatio(averageJacobian, H281DBaselineJacobianMicroseconds):G17}"),
            FormattableString.Invariant($"h9_wall_fraction_of_h28_1d,{SafeRatio(averageH9, H281DBaselineH9Microseconds):G17}"),
            FormattableString.Invariant($"trigger_engine_wall_fraction_of_h28_1d,{SafeRatio(averageTrigger, H281DBaselineTriggerEngineMicroseconds):G17}"),
            FormattableString.Invariant($"estimated_h28_triggered_p95_ratio,{estimatedH28P95Ratio:G17}"),
            FormattableString.Invariant($"tail_ready_threshold_us,{MaximumTriggeredP95Microseconds:G17}"),
            FormattableString.Invariant($"h9_average_total_alloc_bytes,{triggered.Average(static row => (double)row.H9TotalAllocatedBytes):G17}"),
            FormattableString.Invariant($"h9_average_jacobian_alloc_bytes,{triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes):G17}"),
            $"deterministic_fingerprint,{determinismFingerprint}",
            $"h28_1e_cpu_tail_reduction_passes,{passes}",
        }, Utf8WithoutBom);
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

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;

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

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h28-1e-hydraulic-probe-cpu-tail-reduction");

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
        WriteProgress("H.28.1-E exact hydraulic-probe CPU-tail optimization started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", Utf8WithoutBom);

    private sealed record AttributionRow(
        int StepIndex,
        string PresentationFingerprint,
        bool TriggerObserved,
        bool CorrectedCandidateCommitted,
        bool RollbackRequired,
        bool UnsafeCommit,
        bool FallbackCommitViolation,
        int ShadowIterationCount,
        double EngineMicroseconds,
        long EngineAllocatedBytes,
        double PredictorMicroseconds,
        long PredictorAllocatedBytes,
        double CorrectorMicroseconds,
        long CorrectorAllocatedBytes,
        double H9TotalMicroseconds,
        long H9TotalAllocatedBytes,
        double H9JacobianBuildMicroseconds,
        long H9JacobianBuildAllocatedBytes,
        double H9NewtonLineSearchMicroseconds,
        long H9NewtonLineSearchAllocatedBytes,
        int ProbeAppliedFluidNodeReuseCount,
        int ProbeAppliedFluidNodeCount,
        int ProbeMappedFluidNodeReuseCount,
        int ProbeMappedFluidNodeCount,
        int ProbeHydraulicComponentReuseCount,
        int ProbeHydraulicComponentCount,
        int HydraulicEvaluationCount,
        int ProbeEvaluationCount,
        int MaximumJacobianDimension)
    {
        public string ToCsv() => FormattableString.Invariant(
            $"{StepIndex},{PresentationFingerprint},{TriggerObserved},{CorrectedCandidateCommitted},{RollbackRequired},{UnsafeCommit},{FallbackCommitViolation},{ShadowIterationCount},{EngineMicroseconds:G17},{EngineAllocatedBytes},{PredictorMicroseconds:G17},{PredictorAllocatedBytes},{CorrectorMicroseconds:G17},{CorrectorAllocatedBytes},{H9TotalMicroseconds:G17},{H9TotalAllocatedBytes},{H9JacobianBuildMicroseconds:G17},{H9JacobianBuildAllocatedBytes},{H9NewtonLineSearchMicroseconds:G17},{H9NewtonLineSearchAllocatedBytes},{ProbeAppliedFluidNodeReuseCount},{ProbeAppliedFluidNodeCount},{ProbeMappedFluidNodeReuseCount},{ProbeMappedFluidNodeCount},{ProbeHydraulicComponentReuseCount},{ProbeHydraulicComponentCount},{HydraulicEvaluationCount},{ProbeEvaluationCount},{MaximumJacobianDimension}");
    }
}
