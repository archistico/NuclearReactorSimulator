using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Validation;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Validation;

public sealed class ReferenceValidationSuiteTests
{
    [Fact]
    public void ColdShutdownReferenceCase_MatchesCanonicalValidatedSeed()
    {
        var snapshot = new ColdShutdownInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var report = ReferenceValidationRunner.Evaluate(
            BuiltInReferenceValidationCatalog.ColdShutdownSteadyState,
            new[] { ControlRoomReferenceMetricExtractor.Extract(snapshot) });

        Assert.True(report.IsPassed);
        Assert.Equal(0, report.FailedMetricCount);
        Assert.Equal(0, report.MissingMetricCount);
    }

    [Fact]
    public void GridSynchronizationReferenceCase_MatchesCanonicalValidatedSeed()
    {
        var snapshot = new GridSynchronizationInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var report = ReferenceValidationRunner.Evaluate(
            BuiltInReferenceValidationCatalog.GridSynchronizationSteadyState,
            new[] { ControlRoomReferenceMetricExtractor.Extract(snapshot) });

        Assert.True(report.IsPassed);
    }

    [Fact]
    public void InitialGridLoadTransientReferenceCase_MatchesCanonicalCommandTrace()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new GridSynchronizationInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(GridSynchronizationLoadProgram.Scenario);
        var generator = Assert.Single(session.Coordinator.Current.Electrical.Generators);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            generator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var sample = ControlRoomReferenceMetricExtractor.Extract(session.Coordinator.Current);
        var report = ReferenceValidationRunner.Evaluate(
            BuiltInReferenceValidationCatalog.InitialGridLoadTransient,
            new[] { sample });

        Assert.Equal(2, sample.LogicalStep);
        Assert.True(report.IsPassed);
    }

    [Fact]
    public void Runner_FailsClosed_WhenRequiredMetricIsUnavailable()
    {
        var definition = new ReferenceValidationCaseDefinition(
            "missing-metric",
            "Missing metric",
            "Ensures unavailable evidence cannot be treated as a pass.",
            ReferenceValidationCaseKind.SteadyState,
            "test-model-v1",
            "test reference",
            new[]
            {
                new ReferenceValidationTarget(
                    "metric-a",
                    0,
                    10d,
                    new ReferenceValidationToleranceBudget(1d)),
            });
        var sample = new ReferenceValidationSample(0, new Dictionary<string, double?>
        {
            ["metric-a"] = null,
        });

        var report = ReferenceValidationRunner.Evaluate(definition, new[] { sample });

        Assert.False(report.IsPassed);
        Assert.Equal(1, report.MissingMetricCount);
        Assert.Equal(ReferenceValidationMetricStatus.Missing, Assert.Single(report.Metrics).Status);
    }

    [Fact]
    public void Runner_UsesMaximumOfAbsoluteAndRelativeToleranceBudgets()
    {
        var definition = new ReferenceValidationCaseDefinition(
            "tolerance-budget",
            "Tolerance budget",
            "Exercises explicit absolute and relative tolerance composition.",
            ReferenceValidationCaseKind.SteadyState,
            "test-model-v1",
            "test reference",
            new[]
            {
                new ReferenceValidationTarget(
                    "metric-a",
                    4,
                    100d,
                    new ReferenceValidationToleranceBudget(1d, 0.05d)),
            });
        var sample = new ReferenceValidationSample(4, new Dictionary<string, double?>
        {
            ["metric-a"] = 104.5d,
        });

        var result = Assert.Single(ReferenceValidationRunner.Evaluate(definition, new[] { sample }).Metrics);

        Assert.Equal(5d, result.AllowedError, 8);
        Assert.True(result.AbsoluteError.HasValue);
        Assert.Equal(4.5d, result.AbsoluteError.Value, 8);
        Assert.Equal(ReferenceValidationMetricStatus.Passed, result.Status);
    }

    [Fact]
    public void ModelVersionAndReferenceSource_AreExplicitPerCuratedCase()
    {
        Assert.All(BuiltInReferenceValidationCatalog.All, static definition =>
        {
            Assert.Equal(BuiltInReferenceValidationCatalog.ValidatedModelVersion, definition.ModelVersion);
            Assert.False(string.IsNullOrWhiteSpace(definition.ReferenceSource));
            Assert.True(definition.ReferenceSource.Contains("not an external historical measurement", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Theory]
    [InlineData(ReferenceSensitivityDirection.Increase, 10d, 12d, true)]
    [InlineData(ReferenceSensitivityDirection.Increase, 10d, 8d, false)]
    [InlineData(ReferenceSensitivityDirection.Decrease, 10d, 8d, true)]
    [InlineData(ReferenceSensitivityDirection.AnyNonZero, 10d, 10d, false)]
    public void SensitivityAnalyzer_EnforcesDeclaredResponseDirection(
        ReferenceSensitivityDirection direction,
        double baseline,
        double perturbed,
        bool expectedPass)
    {
        var definition = new ReferenceSensitivityProbeDefinition(
            "probe",
            "parameter-x",
            1d,
            1.1d,
            "metric-x",
            direction,
            minimumAbsoluteResponse: 0.1d);

        var report = ReferenceSensitivityAnalyzer.Evaluate(definition, "test-model-v1", baseline, perturbed);

        Assert.Equal(expectedPass, report.IsPassed);
        Assert.Equal(perturbed - baseline, report.MetricDelta, 8);
        Assert.Equal(0.1d, report.ParameterDelta, 8);
    }

    [Fact]
    public void Runner_FailsWhenObservedValueExceedsDeclaredTolerance()
    {
        var definition = new ReferenceValidationCaseDefinition(
            "out-of-budget",
            "Out of budget",
            "Exercises an explicit quantitative regression failure.",
            ReferenceValidationCaseKind.Transient,
            "test-model-v1",
            "test reference",
            new[]
            {
                new ReferenceValidationTarget(
                    "metric-a",
                    3,
                    50d,
                    new ReferenceValidationToleranceBudget(0.5d)),
            });
        var sample = new ReferenceValidationSample(3, new Dictionary<string, double?>
        {
            ["metric-a"] = 51d,
        });

        var result = Assert.Single(ReferenceValidationRunner.Evaluate(definition, new[] { sample }).Metrics);

        Assert.Equal(ReferenceValidationMetricStatus.Failed, result.Status);
        Assert.True(result.AbsoluteError.HasValue);
        Assert.Equal(1d, result.AbsoluteError.Value, 8);
    }

    [Fact]
    public void SuiteDefinition_RejectsCasesFromDifferentModelVersions()
    {
        var target = new ReferenceValidationTarget(
            "metric-a",
            0,
            1d,
            new ReferenceValidationToleranceBudget(0d));
        var caseDefinition = new ReferenceValidationCaseDefinition(
            "case-a",
            "Case A",
            "Version mismatch test.",
            ReferenceValidationCaseKind.SteadyState,
            "model-v1",
            "test reference",
            new[] { target });

        Assert.Throws<ArgumentException>(() => new ReferenceValidationSuiteDefinition(
            "suite",
            "model-v2",
            new[] { caseDefinition }));
    }

    [Fact]
    public void SnapshotMetricExtraction_PreservesUnavailableValuesAsMissingEvidence()
    {
        var sample = ControlRoomReferenceMetricExtractor.Extract(ControlRoomSnapshot.ShellOnly);

        Assert.True(sample.Metrics.ContainsKey(ReferenceValidationMetricIds.ReactorThermalPowerMw));
        Assert.Null(sample.Metrics[ReferenceValidationMetricIds.ReactorThermalPowerMw]);
        Assert.Null(sample.Metrics[ReferenceValidationMetricIds.PrimaryTotalMassKg]);
    }
    [Fact]
    public void SuiteRunner_FailsClosedWhenAnyCuratedCaseEvidenceIsMissing()
    {
        var samples = new Dictionary<string, IReadOnlyList<ReferenceValidationSample>>(StringComparer.Ordinal)
        {
            [BuiltInReferenceValidationCatalog.ColdShutdownSteadyState.CaseId] = new[]
            {
                new ReferenceValidationSample(0, new Dictionary<string, double?>
                {
                    [ReferenceValidationMetricIds.ReactorAverageRodWithdrawalPercent] = 0d,
                    [ReferenceValidationMetricIds.PrimaryRunningPumpCount] = 0d,
                    [ReferenceValidationMetricIds.SecondaryMaximumRotorSpeedRpm] = 0d,
                    [ReferenceValidationMetricIds.ElectricalClosedBreakerCount] = 0d,
                    [ReferenceValidationMetricIds.InstrumentationInvalidSignalCount] = 0d,
                    [ReferenceValidationMetricIds.ProtectionReactorScramActive] = 0d,
                }),
            },
        };

        var report = ReferenceValidationSuiteRunner.Evaluate(
            BuiltInReferenceValidationCatalog.ValidatedBaselineSuite,
            samples);

        Assert.False(report.IsPassed);
        Assert.Equal(ReferenceValidationSuiteReport.CurrentSchemaVersion, report.SchemaVersion);
        Assert.Contains(report.Cases, static caseReport => caseReport.MissingMetricCount > 0);
    }

    [Fact]
    public void SensitivityReport_TracksRealFissionPowerCalibrationPerturbation()
    {
        var baseline = CreateFissionPowerSolver(3_200d).Solve(NeutronPopulation.Reference).TotalFissionThermalPower.Megawatts;
        var perturbed = CreateFissionPowerSolver(3_520d).Solve(NeutronPopulation.Reference).TotalFissionThermalPower.Megawatts;
        var probe = new ReferenceSensitivityProbeDefinition(
            "fission-reference-power-plus-10pct-v1",
            "fission-power.reference-thermal-power-mw",
            3_200d,
            3_520d,
            ReferenceValidationMetricIds.ReactorThermalPowerMw,
            ReferenceSensitivityDirection.Increase,
            minimumAbsoluteResponse: 319d,
            maximumAbsoluteResponse: 321d);

        var report = ReferenceSensitivityAnalyzer.Evaluate(
            probe,
            BuiltInReferenceValidationCatalog.ValidatedModelVersion,
            baseline,
            perturbed);

        Assert.True(report.IsPassed);
        Assert.Equal(320d, report.MetricDelta, 8);
        Assert.Equal(1d, report.NormalizedSensitivity, 8);
    }

    private static FissionPowerSolver CreateFissionPowerSolver(double referenceMegawatts)
        => new(new FissionPowerDefinition(
            "m96-sensitivity-fission",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(referenceMegawatts)),
            new[]
            {
                new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.FromFraction(1d)),
            }));

}
