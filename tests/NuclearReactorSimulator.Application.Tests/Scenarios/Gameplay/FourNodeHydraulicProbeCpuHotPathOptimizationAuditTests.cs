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
/// M10.9.4.1-H.28.1-D hydraulic-probe CPU hot-path optimization gate. It runs after validated H.28.1-B and
/// reduces CPU-only duplicate thermodynamic work inside the unchanged 32-probe finite-difference Jacobian.
/// Probe count, hydraulic-evaluation count, H.9 mathematics, authority, ownership and deterministic trajectory stay fixed.
/// </summary>
public sealed class FourNodeHydraulicProbeCpuHotPathOptimizationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int WarmupSteps = 64;
    private const int AttributionSteps = 256;
    private const int DeterminismSteps = 128;
    private const string H28DeterministicFingerprint = "518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38";
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private const double H281BBaselineJacobianMicroseconds = 1_560_980.9600000002d;
    private const double H281BBaselineH9Microseconds = 1_658_501.1100000001d;
    private const double H281BBaselineTriggerEngineMicroseconds = 1_699_250.3850000002d;
    private const double H281BBaselineJacobianAllocatedBytes = 925_340d;
    private const double H281BBaselineH9AllocatedBytes = 1_004_460.4d;
    private const double H281BBaselineNonTriggerPredictorMicroseconds = 392.382627118644d;
    private const double MaximumJacobianWallFractionOfH281B = 0.85d;
    private const double MaximumH9WallFractionOfH281B = 0.87d;
    private const double MaximumTriggerEngineWallFractionOfH281B = 0.90d;
    private const double MaximumJacobianAllocationFractionOfH281B = 1.10d;
    private const double MaximumH9AllocationFractionOfH281B = 1.10d;
    private const double MaximumNonTriggerPredictorWallFractionOfH281B = 1.50d;
    private const double MinimumAppliedProbeFluidNodeReuseFraction = 0.80d;

    private static readonly IReadOnlyDictionary<string, string> FrozenH27ValidatedFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H27_ValidatedOffDesignQualificationSummary.txt"] = "DDEAC9E8987FC7C12483A792067B4134BB1D87BDDD052F5E0D46E9AEAD3107AE",
        ["H27_ValidatedOffDesignStepTelemetry.csv"] = "6EAA7CAFC4594B455F0933075EBC7486FBF90FBEAF73A65B1B29967111EBA067",
        ["H27_ValidatedOffDesignQualificationEnvelope.csv"] = "AE4A1E9202284BB12E365FEAD6FBA2244B6C16B710ECF8F3D13022DBE7FBD575",
        ["H27_ValidatedOffDesignQualificationMetrics.csv"] = "C1E3857BB734E64357F0DE7596F229A194A1ACF013B6B829BCACF90F983EE342",
    };

    private static readonly IReadOnlyDictionary<string, string> FrozenH28FailureFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_FailedPerformanceCostSoakSummary.txt"] = "089746CCE912A87D8F0BC036AF9C33C16F6A390DDA2104F2482062D864AF60BB",
        ["H28_FailedPerformanceBenchmark.csv"] = "AA86D19958E2E6C3E0426893132705B5FAD96928AF94833889D2C33148CF1027",
        ["H28_FailedOperationalSoakSamples.csv"] = "B08CCDDE91E7F8A816B87C3E7B29CE7B21655FBD89AEF3AA1C73838C5C0EF10E",
        ["H28_FailedPerformanceCostSoakMetrics.csv"] = "077ADE0E41C11A18A731CFCE54F1DED88332A972092A29EF09F76DF1A15912FC",
    };


    private static readonly IReadOnlyDictionary<string, string> FrozenH281AValidatedFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_1A_ValidatedPerformanceAttributionSummary.txt"] = "9B1C7F0189C8610B95C349BB4032F37C4744415A1DFF269177A8840C599A698B",
        ["H28_1A_ValidatedPerformanceAttributionSteps.csv"] = "BDE978CD1C5D12455C230EAFA6F16147184D059656FD71A745B28BA4A123859D",
        ["H28_1A_ValidatedPerformanceAttributionCostCenters.csv"] = "9D36788C6BC4D8FEDDF70049B311CFA90735D0D086ED37C23DF906D13BA9574C",
        ["H28_1A_ValidatedPerformanceAttributionMetrics.csv"] = "4028857717446B22FC5C5972B4C93C0427D55A71E6B6C4A89B7E642E44E13079",
    };

    private static readonly IReadOnlyDictionary<string, string> FrozenH281CValidatedFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_1C_ValidatedH9JacobianProbeHotPathOptimizationSummary.txt"] = "29B57674E2286D6A4AE1C84896ED87392209C8AE0DB03D1919BF49543334211B",
        ["H28_1C_ValidatedH9HotPathOptimizationSteps.csv"] = "3BB2280F65FE9F56345B2493C624B1E957FB319BDDDD5BF3EF20476BD1AF1970",
        ["H28_1C_ValidatedH9HotPathCostCenters.csv"] = "226311E9B01FAE0F85DE908EDF844C5646DB7689A45156141F9CCEB99C7939E3",
        ["H28_1C_ValidatedH9HotPathOptimizationMetrics.csv"] = "27C9BBEA917AC43A9B86FC2B4794749FBB6F63C8554964BA94CA53B7C9B8AA44",
    };

    private static readonly IReadOnlyDictionary<string, string> FrozenH281BValidatedFingerprints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H28_1B_ValidatedHistoricalExplicitPredictorReuseSummary.txt"] = "C60D613BE5F61DEB7F64CA51217E8EE754300309907100341838838AFD5B225C",
        ["H28_1B_ValidatedHistoricalExplicitPredictorReuseSteps.csv"] = "B69AB6F437494A0F837D60E6491B84532C3AEA7D4AEB1AAF5EA93540BB8DCC7A",
        ["H28_1B_ValidatedHistoricalExplicitPredictorReuseCostCenters.csv"] = "5077B7F2EC1ED533918135D09B49406DDD893E8B16D421F9CFC907517DD57C0A",
        ["H28_1B_ValidatedHistoricalExplicitPredictorReuseMetrics.csv"] = "A1E34F634A3F920C22BD2CE8E953BF7D100839B500E58B33DB2202C805EAE14C",
    };
    [Fact]
    public void FrozenValidatedH27Evidence_AnchorsPerformanceAttributionToAuthoritativeBaseline()
    {
        foreach (var expected in FrozenH27ValidatedFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen validated-H.27 evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H27_ValidatedOffDesignQualificationSummary.txt"));
        Assert.Contains("matrix-scenarios=6", summary, StringComparison.Ordinal);
        Assert.Contains("runtime-steps=2080", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=529", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-off-design-robustness-qualification-envelope-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h27-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FrozenFailedH28Evidence_RetainsObservedUnboundedRegressionWithoutPromotingItToBaseline()
    {
        foreach (var expected in FrozenH28FailureFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen failed-H.28 evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_FailedPerformanceCostSoakSummary.txt"));
        Assert.Contains("corrected-performance-class=unbounded-regression", summary, StringComparison.Ordinal);
        Assert.Contains("median-wall-cost-ratio=9.1252571494799053", summary, StringComparison.Ordinal);
        Assert.Contains("p95-wall-cost-ratio=100.01553278882017", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-triggered-average-step-us=1702179.9900000002", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-triggered-average-allocated-bytes=43460418", summary, StringComparison.Ordinal);
        Assert.Contains($"deterministic-fingerprint={H28DeterministicFingerprint}", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-performance-cost-operational-soak-passes=False", summary, StringComparison.Ordinal);
        Assert.Contains("h28-audit-passes=False", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FrozenValidatedH281AEvidence_AnchorsOptimizationToMeasuredCostCenters()
    {
        foreach (var expected in FrozenH281AValidatedFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen validated-H.28.1-A evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_1A_ValidatedPerformanceAttributionSummary.txt"));
        Assert.Contains("h9-average-hydraulic-evaluations=35", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-probe-evaluations=32", summary, StringComparison.Ordinal);
        Assert.Contains("h9-max-jacobian-dimension=32", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-jacobian-build-alloc-bytes=39071378", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-total-alloc-bytes=41523908", summary, StringComparison.Ordinal);
        Assert.Contains("h28.1a-performance-attribution-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FrozenValidatedH281CEvidence_AnchorsPredictorReuseToAllocationOptimizedRuntime()
    {
        foreach (var expected in FrozenH281CValidatedFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen validated-H.28.1-C evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_1C_ValidatedH9JacobianProbeHotPathOptimizationSummary.txt"));
        Assert.Contains("nontrigger-average-predictor-us=9309.4457627118718", summary, StringComparison.Ordinal);
        Assert.Contains("nontrigger-average-predictor-alloc-bytes=26308.203389830509", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-hydraulic-evaluations=35", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-probe-evaluations=32", summary, StringComparison.Ordinal);
        Assert.Contains("h28.1c-jacobian-probe-hot-path-optimization-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FrozenValidatedH281BEvidence_AnchorsCpuOptimizationToPredictorOptimizedRuntime()
    {
        foreach (var expected in FrozenH281BValidatedFingerprints)
        {
            var path = Path.Combine(EvidenceDirectory(), expected.Key);
            Assert.True(File.Exists(path), $"Missing frozen validated-H.28.1-B evidence file: {expected.Key}.");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(EvidenceDirectory(), "H28_1B_ValidatedHistoricalExplicitPredictorReuseSummary.txt"));
        Assert.Contains("nontrigger-average-predictor-us=392.382627118644", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-jacobian-build-us=1560980.9600000002", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-total-us=1658501.1100000001", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-hydraulic-evaluations=35", summary, StringComparison.Ordinal);
        Assert.Contains("h9-average-probe-evaluations=32", summary, StringComparison.Ordinal);
        Assert.Contains("h28.1b-historical-explicit-predictor-reuse-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeHydraulicProbeCpuHotPathOptimizationAudit")]
    public void HydraulicProbeCpuHotPathOptimization_ReducesTriggeredJacobianCostWithoutChangingNumericalAuthorityOrTrajectory()
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
                $"Missing H.28.1-D CPU-hot-path attribution for step {step}.");
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
                Assert.True(attribution.HydraulicEvaluationCount > 0);
                Assert.True(attribution.ProbeEvaluationCount > 0);
                Assert.True(attribution.MaximumJacobianDimension > 0);
                Assert.True(telemetry.CorrectedCandidateCommitted);
            }
            else
            {
                Assert.Null(h9);
                Assert.Equal(0, attribution.HydraulicEvaluationCount);
                Assert.Equal(0, attribution.ProbeEvaluationCount);
            }

            rows.Add(ToRow(step, presentation, telemetry, attribution!, h9, elapsed, allocated));
            if (step % 64 == 0)
            {
                WriteProgress($"attribution-progress step={step}/{AttributionSteps} triggers={rows.Count(static row => row.TriggerObserved)}");
            }
        }

        var triggered = rows.Where(static row => row.TriggerObserved).ToArray();
        var nonTriggered = rows.Where(static row => !row.TriggerObserved).ToArray();
        Assert.NotEmpty(triggered);
        Assert.NotEmpty(nonTriggered);
        Assert.Equal(triggered.Length, rows.Count(static row => row.CorrectedCandidateCommitted));
        Assert.Equal(0, rows.Count(static row => row.RollbackRequired));
        Assert.Equal(0, rows.Count(static row => row.UnsafeCommit));
        Assert.Equal(0, rows.Count(static row => row.FallbackCommitViolation));
        Assert.Equal(20, triggered.Length);
        Assert.All(rows, static row => Assert.True(row.HistoricalPredictorFluidNodeCount > 0));
        Assert.True(rows.Sum(static row => row.HistoricalPredictorFluidNodeReuseCount) > 0,
            "H.28.1-D did not reuse any historical explicit fluid-node predictor state.");
        Assert.All(triggered, static row => Assert.Equal(35, row.HydraulicEvaluationCount));
        Assert.All(triggered, static row => Assert.Equal(32, row.ProbeEvaluationCount));
        Assert.All(triggered, static row => Assert.Equal(32, row.MaximumJacobianDimension));
        Assert.All(triggered, static row => Assert.True(row.ProbeAppliedFluidNodeCount > 0));
        Assert.True(triggered.Sum(static row => row.ProbeAppliedFluidNodeReuseCount) > 0,
            "H.28.1-D did not reuse any exact probe fluid-node state.");

        var averageJacobianAllocatedBytes = triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes);
        var averageH9AllocatedBytes = triggered.Average(static row => (double)row.H9TotalAllocatedBytes);
        var averageJacobianMicroseconds = triggered.Average(static row => row.H9JacobianBuildMicroseconds);
        var averageH9Microseconds = triggered.Average(static row => row.H9TotalMicroseconds);
        var averageTriggerEngineMicroseconds = triggered.Average(static row => row.EngineMicroseconds);
        var averageNonTriggerPredictorMicroseconds = nonTriggered.Average(static row => row.PredictorMicroseconds);
        var appliedProbeReuseFraction = SafeRatio(
            triggered.Sum(static row => row.ProbeAppliedFluidNodeReuseCount),
            triggered.Sum(static row => row.ProbeAppliedFluidNodeCount));
        var jacobianWallPasses = averageJacobianMicroseconds <= H281BBaselineJacobianMicroseconds * MaximumJacobianWallFractionOfH281B;
        var h9WallPasses = averageH9Microseconds <= H281BBaselineH9Microseconds * MaximumH9WallFractionOfH281B;
        var triggerEngineWallPasses = averageTriggerEngineMicroseconds <= H281BBaselineTriggerEngineMicroseconds * MaximumTriggerEngineWallFractionOfH281B;
        var jacobianAllocationPasses = averageJacobianAllocatedBytes <= H281BBaselineJacobianAllocatedBytes * MaximumJacobianAllocationFractionOfH281B;
        var h9AllocationPasses = averageH9AllocatedBytes <= H281BBaselineH9AllocatedBytes * MaximumH9AllocationFractionOfH281B;
        var predictorRegressionPasses = averageNonTriggerPredictorMicroseconds <= H281BBaselineNonTriggerPredictorMicroseconds * MaximumNonTriggerPredictorWallFractionOfH281B;
        var probeReusePasses = appliedProbeReuseFraction >= MinimumAppliedProbeFluidNodeReuseFraction;

        WriteProgress("determinism-control-start");
        var determinism = RunDeterminismControl();
        var determinismPasses = string.Equals(H28DeterministicFingerprint, determinism, StringComparison.Ordinal);

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, CurrentHydraulics(defaultEngine).Mode);

        var cpuOptimizationPasses = jacobianWallPasses
            && h9WallPasses
            && triggerEngineWallPasses
            && jacobianAllocationPasses
            && h9AllocationPasses
            && predictorRegressionPasses
            && probeReusePasses
            && determinismPasses;
        WriteReports(rows, triggered, nonTriggered, determinism, cpuOptimizationPasses);

        Assert.True(
            jacobianWallPasses,
            $"H.28.1-D Jacobian CPU reduction was insufficient. H.28.1-B baseline={H281BBaselineJacobianMicroseconds:G17} us, actual={averageJacobianMicroseconds:G17} us.");
        Assert.True(
            h9WallPasses,
            $"H.28.1-D total H.9 CPU reduction was insufficient. H.28.1-B baseline={H281BBaselineH9Microseconds:G17} us, actual={averageH9Microseconds:G17} us.");
        Assert.True(
            triggerEngineWallPasses,
            $"H.28.1-D trigger-engine reduction was insufficient. H.28.1-B baseline={H281BBaselineTriggerEngineMicroseconds:G17} us, actual={averageTriggerEngineMicroseconds:G17} us.");
        Assert.True(jacobianAllocationPasses, "H.28.1-D regressed H.28.1-C/B Jacobian allocation gains.");
        Assert.True(h9AllocationPasses, "H.28.1-D regressed H.28.1-C/B H.9 allocation gains.");
        Assert.True(predictorRegressionPasses, "H.28.1-D regressed the validated H.28.1-B non-trigger predictor cost.");
        Assert.True(probeReusePasses, $"H.28.1-D applied probe fluid-node reuse was too low: {appliedProbeReuseFraction:G17}.");
        Assert.Equal(H28DeterministicFingerprint, determinism);
        WriteProgress("H.28.1-D hydraulic-probe CPU hot-path optimization completed");
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
            attribution.HistoricalPredictorFluidNodeReuseCount,
            attribution.HistoricalPredictorFluidNodeCount,
            telemetry.ShadowIterationCount,
            TicksToMicroseconds(engineElapsedTicks),
            engineAllocatedBytes,
            TicksToMicroseconds(attribution.OrchestratorElapsedTicks),
            attribution.OrchestratorAllocatedBytes,
            TicksToMicroseconds(attribution.HistoricalExplicitPreparationElapsedTicks),
            attribution.HistoricalExplicitPreparationAllocatedBytes,
            TicksToMicroseconds(attribution.SidecarElapsedTicks),
            attribution.SidecarAllocatedBytes,
            TicksToMicroseconds(attribution.PredictorElapsedTicks),
            attribution.PredictorAllocatedBytes,
            TicksToMicroseconds(attribution.CorrectorElapsedTicks),
            attribution.CorrectorAllocatedBytes,
            TicksToMicroseconds(attribution.UntargetedDisagreementScanElapsedTicks),
            attribution.UntargetedDisagreementScanAllocatedBytes,
            TicksToMicroseconds(attribution.AuthorityEvaluationElapsedTicks),
            attribution.AuthorityEvaluationAllocatedBytes,
            TicksToMicroseconds(attribution.CommitAndAccountingElapsedTicks),
            attribution.CommitAndAccountingAllocatedBytes,
            h9 is null ? 0d : TicksToMicroseconds(h9.TotalElapsedTicks),
            h9?.TotalAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.CoordinateLayoutElapsedTicks),
            h9?.CoordinateLayoutAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.InitialResidualElapsedTicks),
            h9?.InitialResidualAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.JacobianBuildElapsedTicks),
            h9?.JacobianBuildAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.NewtonLineSearchElapsedTicks),
            h9?.NewtonLineSearchAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.ResidualFallbackElapsedTicks),
            h9?.ResidualFallbackAllocatedBytes ?? 0L,
            h9 is null ? 0d : TicksToMicroseconds(h9.OtherElapsedTicks),
            h9?.OtherAllocatedBytes ?? 0L,
            h9?.ProbeAppliedFluidNodeReuseCount ?? 0,
            h9?.ProbeAppliedFluidNodeCount ?? 0,
            h9?.ProbeMappedFluidNodeReuseCount ?? 0,
            h9?.ProbeMappedFluidNodeCount ?? 0,
            attribution.HydraulicEvaluationCount,
            attribution.ProbeEvaluationCount,
            attribution.MaximumJacobianDimension,
            attribution.JacobianBuildAttempts,
            attribution.JacobianDirectionAcceptances,
            attribution.JacobianRejectedCount,
            attribution.ResidualFallbackAttempts,
            attribution.ResidualFallbackAcceptances,
            attribution.BacktrackingTrialCount);

    private static void WriteReports(
        IReadOnlyList<AttributionRow> rows,
        IReadOnlyList<AttributionRow> triggered,
        IReadOnlyList<AttributionRow> nonTriggered,
        string determinismFingerprint,
        bool cpuOptimizationPasses)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var csv = new List<string>
        {
            "step,presentation_fingerprint,trigger,committed,rollback,unsafe_commit,fallback_commit_violation,historical_predictor_fluid_nodes_reused,historical_predictor_fluid_nodes_total,shadow_iterations,engine_us,engine_alloc_bytes,orchestrator_us,orchestrator_alloc_bytes,explicit_preparation_us,explicit_preparation_alloc_bytes,sidecar_us,sidecar_alloc_bytes,predictor_us,predictor_alloc_bytes,corrector_us,corrector_alloc_bytes,disagreement_scan_us,disagreement_scan_alloc_bytes,authority_us,authority_alloc_bytes,commit_accounting_us,commit_accounting_alloc_bytes,h9_total_us,h9_total_alloc_bytes,h9_layout_us,h9_layout_alloc_bytes,h9_initial_residual_us,h9_initial_residual_alloc_bytes,h9_jacobian_build_us,h9_jacobian_build_alloc_bytes,h9_newton_line_search_us,h9_newton_line_search_alloc_bytes,h9_residual_fallback_us,h9_residual_fallback_alloc_bytes,h9_other_us,h9_other_alloc_bytes,probe_applied_fluid_nodes_reused,probe_applied_fluid_nodes_total,probe_mapped_fluid_nodes_reused,probe_mapped_fluid_nodes_total,hydraulic_evaluations,probe_evaluations,max_jacobian_dimension,jacobian_build_attempts,jacobian_direction_acceptances,jacobian_rejected,residual_fallback_attempts,residual_fallback_acceptances,backtracking_trials",
        };
        csv.AddRange(rows.Select(static row => row.ToCsv()));
        File.WriteAllLines(Path.Combine(directory, "02-hydraulic-probe-cpu-hot-path-optimization-steps.csv"), csv, Utf8WithoutBom);

        var triggerCenters = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["application-outside-orchestrator"] = triggered.Average(static row => Math.Max(0d, row.EngineMicroseconds - row.OrchestratorMicroseconds)),
            ["orchestrator-unattributed"] = triggered.Average(static row => Math.Max(0d, row.OrchestratorMicroseconds - row.ExplicitPreparationMicroseconds - row.SidecarMicroseconds - row.CommitAccountingMicroseconds)),
            ["sidecar-unattributed"] = triggered.Average(static row => Math.Max(0d, row.SidecarMicroseconds - row.PredictorMicroseconds - row.CorrectorMicroseconds - row.DisagreementScanMicroseconds - row.AuthorityMicroseconds)),
            ["historical-explicit-preparation"] = triggered.Average(static row => row.ExplicitPreparationMicroseconds),
            ["sidecar-predictor"] = triggered.Average(static row => row.PredictorMicroseconds),
            ["corrector-setup-wrapper"] = triggered.Average(static row => Math.Max(0d, row.CorrectorMicroseconds - row.H9TotalMicroseconds)),
            ["h9-coordinate-layout"] = triggered.Average(static row => row.H9LayoutMicroseconds),
            ["h9-initial-residual"] = triggered.Average(static row => row.H9InitialResidualMicroseconds),
            ["h9-jacobian-build-probes"] = triggered.Average(static row => row.H9JacobianBuildMicroseconds),
            ["h9-newton-line-search"] = triggered.Average(static row => row.H9NewtonLineSearchMicroseconds),
            ["h9-residual-fallback"] = triggered.Average(static row => row.H9ResidualFallbackMicroseconds),
            ["h9-other"] = triggered.Average(static row => row.H9OtherMicroseconds),
            ["untargeted-disagreement-scan"] = triggered.Average(static row => row.DisagreementScanMicroseconds),
            ["authority"] = triggered.Average(static row => row.AuthorityMicroseconds),
            ["commit-accounting"] = triggered.Average(static row => row.CommitAccountingMicroseconds),
        };
        var allocationCenters = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["application-outside-orchestrator"] = triggered.Average(static row => (double)Math.Max(0L, row.EngineAllocatedBytes - row.OrchestratorAllocatedBytes)),
            ["orchestrator-unattributed"] = triggered.Average(static row => (double)Math.Max(0L, row.OrchestratorAllocatedBytes - row.ExplicitPreparationAllocatedBytes - row.SidecarAllocatedBytes - row.CommitAccountingAllocatedBytes)),
            ["sidecar-unattributed"] = triggered.Average(static row => (double)Math.Max(0L, row.SidecarAllocatedBytes - row.PredictorAllocatedBytes - row.CorrectorAllocatedBytes - row.DisagreementScanAllocatedBytes - row.AuthorityAllocatedBytes)),
            ["historical-explicit-preparation"] = triggered.Average(static row => (double)row.ExplicitPreparationAllocatedBytes),
            ["sidecar-predictor"] = triggered.Average(static row => (double)row.PredictorAllocatedBytes),
            ["corrector-setup-wrapper"] = triggered.Average(static row => (double)Math.Max(0L, row.CorrectorAllocatedBytes - row.H9TotalAllocatedBytes)),
            ["h9-coordinate-layout"] = triggered.Average(static row => (double)row.H9LayoutAllocatedBytes),
            ["h9-initial-residual"] = triggered.Average(static row => (double)row.H9InitialResidualAllocatedBytes),
            ["h9-jacobian-build-probes"] = triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes),
            ["h9-newton-line-search"] = triggered.Average(static row => (double)row.H9NewtonLineSearchAllocatedBytes),
            ["h9-residual-fallback"] = triggered.Average(static row => (double)row.H9ResidualFallbackAllocatedBytes),
            ["h9-other"] = triggered.Average(static row => (double)row.H9OtherAllocatedBytes),
            ["untargeted-disagreement-scan"] = triggered.Average(static row => (double)row.DisagreementScanAllocatedBytes),
            ["authority"] = triggered.Average(static row => (double)row.AuthorityAllocatedBytes),
            ["commit-accounting"] = triggered.Average(static row => (double)row.CommitAccountingAllocatedBytes),
        };
        var primaryWall = triggerCenters.MaxBy(static item => item.Value);
        var primaryAlloc = allocationCenters.MaxBy(static item => item.Value);

        var h9Avg = triggered.Average(static row => row.H9TotalMicroseconds);
        var h9JacobianAvg = triggered.Average(static row => row.H9JacobianBuildMicroseconds);
        var h9LineSearchAvg = triggered.Average(static row => row.H9NewtonLineSearchMicroseconds + row.H9ResidualFallbackMicroseconds);
        var summary = new[]
        {
            "=== 01-current-v2-hydraulic-probe-cpu-hot-path-optimization ===",
            "H.28.1-D preserves the validated H.28.1-B predictor reuse and unchanged 32-probe finite-difference Newton mathematics while eliminating repeated thermodynamic CPU work inside Jacobian probes. Exact-equal applied/mapped probe fluid nodes are reused by reference, and the fixed 513-point saturated-mixture coarse grid reuses immutable precomputed saturation properties; dynamic boundary/bisection paths and all numerical decisions remain unchanged.",
            FormattableString.Invariant($"warmup-steps={WarmupSteps}; attribution-steps={AttributionSteps}; triggered={triggered.Count}; committed={rows.Count(static row => row.CorrectedCandidateCommitted)}; rollbacks={rows.Count(static row => row.RollbackRequired)}; unsafe-commits={rows.Count(static row => row.UnsafeCommit)}; fallback-commit-violations={rows.Count(static row => row.FallbackCommitViolation)};"),
            FormattableString.Invariant($"historical-predictor-fluid-nodes-reused={rows.Sum(static row => row.HistoricalPredictorFluidNodeReuseCount)}; historical-predictor-fluid-nodes-total={rows.Sum(static row => row.HistoricalPredictorFluidNodeCount)}; historical-predictor-fluid-node-reuse-fraction={SafeRatio(rows.Sum(static row => row.HistoricalPredictorFluidNodeReuseCount), rows.Sum(static row => row.HistoricalPredictorFluidNodeCount)):G17}; nontrigger-average-reused-fluid-nodes={nonTriggered.Average(static row => row.HistoricalPredictorFluidNodeReuseCount):G17};"),
            FormattableString.Invariant($"nontrigger-average-engine-us={nonTriggered.Average(static row => row.EngineMicroseconds):G17}; nontrigger-average-orchestrator-us={nonTriggered.Average(static row => row.OrchestratorMicroseconds):G17}; nontrigger-average-predictor-us={nonTriggered.Average(static row => row.PredictorMicroseconds):G17}; nontrigger-average-predictor-alloc-bytes={nonTriggered.Average(static row => (double)row.PredictorAllocatedBytes):G17};"),
            FormattableString.Invariant($"h28.1b-baseline-nontrigger-predictor-us={H281BBaselineNonTriggerPredictorMicroseconds:G17}; predictor-wall-fraction-of-h28.1b={SafeRatio(nonTriggered.Average(static row => row.PredictorMicroseconds), H281BBaselineNonTriggerPredictorMicroseconds):G17};"),
            FormattableString.Invariant($"trigger-average-engine-us={triggered.Average(static row => row.EngineMicroseconds):G17}; trigger-average-orchestrator-us={triggered.Average(static row => row.OrchestratorMicroseconds):G17}; trigger-average-predictor-us={triggered.Average(static row => row.PredictorMicroseconds):G17}; trigger-average-corrector-us={triggered.Average(static row => row.CorrectorMicroseconds):G17}; trigger-average-disagreement-scan-us={triggered.Average(static row => row.DisagreementScanMicroseconds):G17}; trigger-average-commit-accounting-us={triggered.Average(static row => row.CommitAccountingMicroseconds):G17};"),
            FormattableString.Invariant($"h9-average-total-us={h9Avg:G17}; h9-average-jacobian-build-us={h9JacobianAvg:G17}; h9-jacobian-wall-share={SafeRatio(h9JacobianAvg, h9Avg):G17}; h9-average-line-search-us={h9LineSearchAvg:G17}; h9-line-search-wall-share={SafeRatio(h9LineSearchAvg, h9Avg):G17}; h9-average-hydraulic-evaluations={triggered.Average(static row => row.HydraulicEvaluationCount):G17}; h9-average-probe-evaluations={triggered.Average(static row => row.ProbeEvaluationCount):G17}; h9-max-jacobian-dimension={triggered.Max(static row => row.MaximumJacobianDimension)};"),
            FormattableString.Invariant($"trigger-average-engine-alloc-bytes={triggered.Average(static row => (double)row.EngineAllocatedBytes):G17}; trigger-average-corrector-alloc-bytes={triggered.Average(static row => (double)row.CorrectorAllocatedBytes):G17}; h9-average-total-alloc-bytes={triggered.Average(static row => (double)row.H9TotalAllocatedBytes):G17}; h9-average-jacobian-build-alloc-bytes={triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes):G17};"),
            FormattableString.Invariant($"h28.1b-baseline-jacobian-us={H281BBaselineJacobianMicroseconds:G17}; jacobian-wall-fraction-of-h28.1b={SafeRatio(h9JacobianAvg, H281BBaselineJacobianMicroseconds):G17}; h28.1b-baseline-h9-us={H281BBaselineH9Microseconds:G17}; h9-wall-fraction-of-h28.1b={SafeRatio(h9Avg, H281BBaselineH9Microseconds):G17}; h28.1b-baseline-trigger-engine-us={H281BBaselineTriggerEngineMicroseconds:G17}; trigger-engine-wall-fraction-of-h28.1b={SafeRatio(triggered.Average(static row => row.EngineMicroseconds), H281BBaselineTriggerEngineMicroseconds):G17};"),
            FormattableString.Invariant($"probe-applied-fluid-nodes-reused={triggered.Sum(static row => row.ProbeAppliedFluidNodeReuseCount)}; probe-applied-fluid-nodes-total={triggered.Sum(static row => row.ProbeAppliedFluidNodeCount)}; probe-applied-fluid-node-reuse-fraction={SafeRatio(triggered.Sum(static row => row.ProbeAppliedFluidNodeReuseCount), triggered.Sum(static row => row.ProbeAppliedFluidNodeCount)):G17}; probe-mapped-fluid-nodes-reused={triggered.Sum(static row => row.ProbeMappedFluidNodeReuseCount)}; probe-mapped-fluid-nodes-total={triggered.Sum(static row => row.ProbeMappedFluidNodeCount)}; probe-mapped-fluid-node-reuse-fraction={SafeRatio(triggered.Sum(static row => row.ProbeMappedFluidNodeReuseCount), triggered.Sum(static row => row.ProbeMappedFluidNodeCount)):G17};"),
            FormattableString.Invariant($"h28.1b-baseline-jacobian-alloc-bytes={H281BBaselineJacobianAllocatedBytes:G17}; jacobian-allocation-fraction-of-h28.1b={SafeRatio(triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes), H281BBaselineJacobianAllocatedBytes):G17}; h28.1b-baseline-h9-alloc-bytes={H281BBaselineH9AllocatedBytes:G17}; h9-allocation-fraction-of-h28.1b={SafeRatio(triggered.Average(static row => (double)row.H9TotalAllocatedBytes), H281BBaselineH9AllocatedBytes):G17};"),
            FormattableString.Invariant($"primary-trigger-wall-cost-center={primaryWall.Key}; primary-trigger-wall-average-us={primaryWall.Value:G17}; primary-trigger-allocation-center={primaryAlloc.Key}; primary-trigger-allocation-average-bytes={primaryAlloc.Value:G17};"),
            FormattableString.Invariant($"determinism-control-steps={DeterminismSteps}; deterministic-fingerprint={determinismFingerprint}; matches-failed-H28-fingerprint={string.Equals(determinismFingerprint, H28DeterministicFingerprint, StringComparison.Ordinal)}; default-current-v2-mode=ExplicitCommittedState;"),
            $"h28.1d-hydraulic-probe-cpu-hot-path-optimization-passes={cpuOptimizationPasses}; h28-remains-failed=True; H29-default-activation-blocked=True;",
            "H.28.1-D recommendation: if Jacobian/H.9 wall-clock cost is materially lower with exact deterministic trajectory and preserved allocation gains, rerun H.28 next. If the 32-probe Jacobian remains near the H.28.1-B CPU baseline, treat finite-difference probe cost as intrinsic evidence and favor OPT-IN ONLY unless a separately reviewed algorithm change is justified.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-hydraulic-probe-cpu-hot-path-optimization.summary.txt"), summary, Utf8WithoutBom);

        var centersCsv = new List<string> { "cost_center,average_trigger_wall_us,average_trigger_allocated_bytes" };
        centersCsv.AddRange(triggerCenters.Keys.Order(StringComparer.Ordinal).Select(key => FormattableString.Invariant(
            $"{key},{triggerCenters[key]:G17},{allocationCenters[key]:G17}")));
        File.WriteAllLines(Path.Combine(directory, "03-hydraulic-probe-cpu-hot-path-optimization-cost-centers.csv"), centersCsv, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "04-hydraulic-probe-cpu-hot-path-optimization-metrics.csv"), new[]
        {
            "metric,value",
            $"attribution_steps,{AttributionSteps}",
            $"triggered,{triggered.Count}",
            $"historical_predictor_fluid_nodes_reused,{rows.Sum(static row => row.HistoricalPredictorFluidNodeReuseCount)}",
            $"historical_predictor_fluid_nodes_total,{rows.Sum(static row => row.HistoricalPredictorFluidNodeCount)}",
            FormattableString.Invariant($"historical_predictor_fluid_node_reuse_fraction,{SafeRatio(rows.Sum(static row => row.HistoricalPredictorFluidNodeReuseCount), rows.Sum(static row => row.HistoricalPredictorFluidNodeCount)):G17}"),
            FormattableString.Invariant($"nontrigger_average_predictor_us,{nonTriggered.Average(static row => row.PredictorMicroseconds):G17}"),
            FormattableString.Invariant($"predictor_wall_fraction_of_h28_1b,{SafeRatio(nonTriggered.Average(static row => row.PredictorMicroseconds), H281BBaselineNonTriggerPredictorMicroseconds):G17}"),
            FormattableString.Invariant($"nontrigger_average_predictor_alloc_bytes,{nonTriggered.Average(static row => (double)row.PredictorAllocatedBytes):G17}"),
            FormattableString.Invariant($"trigger_average_engine_us,{triggered.Average(static row => row.EngineMicroseconds):G17}"),
            FormattableString.Invariant($"trigger_average_h9_us,{h9Avg:G17}"),
            FormattableString.Invariant($"h9_jacobian_wall_share,{SafeRatio(h9JacobianAvg, h9Avg):G17}"),
            FormattableString.Invariant($"h9_line_search_wall_share,{SafeRatio(h9LineSearchAvg, h9Avg):G17}"),
            FormattableString.Invariant($"h9_average_hydraulic_evaluations,{triggered.Average(static row => row.HydraulicEvaluationCount):G17}"),
            FormattableString.Invariant($"h9_average_probe_evaluations,{triggered.Average(static row => row.ProbeEvaluationCount):G17}"),
            $"h9_max_jacobian_dimension,{triggered.Max(static row => row.MaximumJacobianDimension)}",
            $"primary_trigger_wall_cost_center,{primaryWall.Key}",
            $"primary_trigger_allocation_center,{primaryAlloc.Key}",
            $"deterministic_fingerprint,{determinismFingerprint}",
            FormattableString.Invariant($"jacobian_wall_fraction_of_h28_1b,{SafeRatio(h9JacobianAvg, H281BBaselineJacobianMicroseconds):G17}"),
            FormattableString.Invariant($"h9_wall_fraction_of_h28_1b,{SafeRatio(h9Avg, H281BBaselineH9Microseconds):G17}"),
            FormattableString.Invariant($"trigger_engine_wall_fraction_of_h28_1b,{SafeRatio(triggered.Average(static row => row.EngineMicroseconds), H281BBaselineTriggerEngineMicroseconds):G17}"),
            FormattableString.Invariant($"probe_applied_fluid_node_reuse_fraction,{SafeRatio(triggered.Sum(static row => row.ProbeAppliedFluidNodeReuseCount), triggered.Sum(static row => row.ProbeAppliedFluidNodeCount)):G17}"),
            FormattableString.Invariant($"probe_mapped_fluid_node_reuse_fraction,{SafeRatio(triggered.Sum(static row => row.ProbeMappedFluidNodeReuseCount), triggered.Sum(static row => row.ProbeMappedFluidNodeCount)):G17}"),
            FormattableString.Invariant($"jacobian_allocation_fraction_of_h28_1b,{SafeRatio(triggered.Average(static row => (double)row.H9JacobianBuildAllocatedBytes), H281BBaselineJacobianAllocatedBytes):G17}"),
            FormattableString.Invariant($"h9_allocation_fraction_of_h28_1b,{SafeRatio(triggered.Average(static row => (double)row.H9TotalAllocatedBytes), H281BBaselineH9AllocatedBytes):G17}"),
            $"h28_1d_cpu_hot_path_optimization_passes,{cpuOptimizationPasses}",
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

    private static double SafeRatio(double numerator, double denominator)
        => Math.Abs(denominator) > 1e-12d ? numerator / denominator : (Math.Abs(numerator) <= 1e-12d ? 1d : double.PositiveInfinity);

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h28-1d-hydraulic-probe-cpu-hot-path-optimization");

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
        WriteProgress("H.28.1-D hydraulic probe CPU hot-path optimization started");
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
        int HistoricalPredictorFluidNodeReuseCount,
        int HistoricalPredictorFluidNodeCount,
        int ShadowIterationCount,
        double EngineMicroseconds,
        long EngineAllocatedBytes,
        double OrchestratorMicroseconds,
        long OrchestratorAllocatedBytes,
        double ExplicitPreparationMicroseconds,
        long ExplicitPreparationAllocatedBytes,
        double SidecarMicroseconds,
        long SidecarAllocatedBytes,
        double PredictorMicroseconds,
        long PredictorAllocatedBytes,
        double CorrectorMicroseconds,
        long CorrectorAllocatedBytes,
        double DisagreementScanMicroseconds,
        long DisagreementScanAllocatedBytes,
        double AuthorityMicroseconds,
        long AuthorityAllocatedBytes,
        double CommitAccountingMicroseconds,
        long CommitAccountingAllocatedBytes,
        double H9TotalMicroseconds,
        long H9TotalAllocatedBytes,
        double H9LayoutMicroseconds,
        long H9LayoutAllocatedBytes,
        double H9InitialResidualMicroseconds,
        long H9InitialResidualAllocatedBytes,
        double H9JacobianBuildMicroseconds,
        long H9JacobianBuildAllocatedBytes,
        double H9NewtonLineSearchMicroseconds,
        long H9NewtonLineSearchAllocatedBytes,
        double H9ResidualFallbackMicroseconds,
        long H9ResidualFallbackAllocatedBytes,
        double H9OtherMicroseconds,
        long H9OtherAllocatedBytes,
        int ProbeAppliedFluidNodeReuseCount,
        int ProbeAppliedFluidNodeCount,
        int ProbeMappedFluidNodeReuseCount,
        int ProbeMappedFluidNodeCount,
        int HydraulicEvaluationCount,
        int ProbeEvaluationCount,
        int MaximumJacobianDimension,
        int JacobianBuildAttempts,
        int JacobianDirectionAcceptances,
        int JacobianRejectedCount,
        int ResidualFallbackAttempts,
        int ResidualFallbackAcceptances,
        int BacktrackingTrialCount)
    {
        public string ToCsv() => FormattableString.Invariant(
            $"{StepIndex},{PresentationFingerprint},{TriggerObserved},{CorrectedCandidateCommitted},{RollbackRequired},{UnsafeCommit},{FallbackCommitViolation},{HistoricalPredictorFluidNodeReuseCount},{HistoricalPredictorFluidNodeCount},{ShadowIterationCount},{EngineMicroseconds:G17},{EngineAllocatedBytes},{OrchestratorMicroseconds:G17},{OrchestratorAllocatedBytes},{ExplicitPreparationMicroseconds:G17},{ExplicitPreparationAllocatedBytes},{SidecarMicroseconds:G17},{SidecarAllocatedBytes},{PredictorMicroseconds:G17},{PredictorAllocatedBytes},{CorrectorMicroseconds:G17},{CorrectorAllocatedBytes},{DisagreementScanMicroseconds:G17},{DisagreementScanAllocatedBytes},{AuthorityMicroseconds:G17},{AuthorityAllocatedBytes},{CommitAccountingMicroseconds:G17},{CommitAccountingAllocatedBytes},{H9TotalMicroseconds:G17},{H9TotalAllocatedBytes},{H9LayoutMicroseconds:G17},{H9LayoutAllocatedBytes},{H9InitialResidualMicroseconds:G17},{H9InitialResidualAllocatedBytes},{H9JacobianBuildMicroseconds:G17},{H9JacobianBuildAllocatedBytes},{H9NewtonLineSearchMicroseconds:G17},{H9NewtonLineSearchAllocatedBytes},{H9ResidualFallbackMicroseconds:G17},{H9ResidualFallbackAllocatedBytes},{H9OtherMicroseconds:G17},{H9OtherAllocatedBytes},{ProbeAppliedFluidNodeReuseCount},{ProbeAppliedFluidNodeCount},{ProbeMappedFluidNodeReuseCount},{ProbeMappedFluidNodeCount},{HydraulicEvaluationCount},{ProbeEvaluationCount},{MaximumJacobianDimension},{JacobianBuildAttempts},{JacobianDirectionAcceptances},{JacobianRejectedCount},{ResidualFallbackAttempts},{ResidualFallbackAcceptances},{BacktrackingTrialCount}");
    }
}
