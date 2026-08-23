using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Follow-up evidence after Diagnostic 1 classified LR-M1 as a live prefix-scan scalability defect and showed a real
/// exact-v4 outlet inventory drift. This gate verifies the incremental MISSION read-side semantics/cost shape and correlates
/// outlet dm/dt with the canonical main-circulation channel-to-return residual without modifying the physical runtime.
/// </summary>
public sealed class M10FinalLongFailureDiagnostic2Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC2";
    private const int StepsPerSecond = 100;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic2")]
    public void LR_H1_ExactV4_PrimaryBranchContinuityAndControllerCensus()
    {
        RequireOptIn();
        const int totalSteps = 30_000;
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());

        var rows = new List<PrimarySample>();
        CapturePrimary(engine, 0, rows);
        for (var step = 1; step <= totalSteps; step++)
        {
            _ = engine.Step(ControlRoomRunState.Running);
            if (step % StepsPerSecond == 0)
            {
                CapturePrimary(engine, step, rows);
            }
            if (step % 3000 == 0)
            {
                AppendProgress($"LR-H1 diagnostic2 simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }

        var formatted = new List<string>
        {
            "logical_step,simulated_seconds,outlet_mass_kg,pressure_header_pa,outlet_pressure_pa,drum_pressure_pa,pump_flow_kg_s,channel_flow_kg_s,return_flow_kg_s,channel_return_residual_kg_s,drum_incoming_return_kg_s,drum_recirculated_liquid_kg_s,drum_separated_steam_kg_s,flow_controller_error,flow_controller_integral,flow_controller_output,level_controller_error,level_controller_integral,level_controller_output"
        };
        formatted.AddRange(rows.Select(FormatPrimary));
        WriteAllLines("20-lr-h1-primary-branch-controller-trajectory.csv", formatted);

        var finalWindow = rows.Where(static item => item.SimulatedSeconds >= 240d).ToArray();
        var outletMassSlope = Slope(finalWindow, static item => item.OutletMassKilograms);
        var continuityResidualMean = finalWindow.Average(static item => item.ChannelReturnResidualKilogramsPerSecond);
        var continuityResidualSlope = Slope(finalWindow, static item => item.ChannelReturnResidualKilogramsPerSecond);
        var pressureMidpointOffsetMean = finalWindow.Average(static item =>
            item.OutletPressurePascals - ((item.PressureHeaderPascals + item.DrumPressurePascals) / 2d));
        var pressureMidpointOffsetSlope = Slope(finalWindow, static item =>
            item.OutletPressurePascals - ((item.PressureHeaderPascals + item.DrumPressurePascals) / 2d));
        var flowIntegralSlope = Slope(finalWindow, static item => item.FlowControllerIntegral);
        var levelIntegralSlope = Slope(finalWindow, static item => item.LevelControllerIntegral);

        WriteAllLines("21-lr-h1-primary-branch-final60-summary.txt", new[]
        {
            "scope=exact-v4 300 s canonical primary-circulation/controller correlation; production physics and acceptance criteria unchanged;",
            FormattableString.Invariant($"outlet-mass-slope-kg-s={outletMassSlope:G17};"),
            FormattableString.Invariant($"channel-return-residual-mean-kg-s={continuityResidualMean:G17};"),
            FormattableString.Invariant($"channel-return-residual-slope-kg-s2={continuityResidualSlope:G17};"),
            FormattableString.Invariant($"outlet-pressure-midpoint-offset-mean-pa={pressureMidpointOffsetMean:G17};"),
            FormattableString.Invariant($"outlet-pressure-midpoint-offset-slope-pa-s={pressureMidpointOffsetSlope:G17};"),
            FormattableString.Invariant($"flow-controller-integral-slope-per-s={flowIntegralSlope:G17};"),
            FormattableString.Invariant($"level-controller-integral-slope-per-s={levelIntegralSlope:G17};"),
            "classification-rule=if outlet mass slope tracks channel-return residual while total mass closure remains green, classify immediate owner as primary-circulation operating-point redistribution rather than global mass loss; controller slopes decide whether closed-loop bias is a material upstream contributor;",
        });

        Assert.Equal(totalSteps, engine.LogicalStep);
        Assert.All(rows, static item => Assert.True(new[]
        {
            item.OutletMassKilograms,
            item.PressureHeaderPascals,
            item.OutletPressurePascals,
            item.DrumPressurePascals,
            item.PumpFlowKilogramsPerSecond,
            item.ChannelFlowKilogramsPerSecond,
            item.ReturnFlowKilogramsPerSecond,
            item.ChannelReturnResidualKilogramsPerSecond,
            item.FlowControllerIntegral,
            item.LevelControllerIntegral,
        }.All(double.IsFinite)));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic2")]
    public void LR_M1_IncrementalMissionProjectionScalingAndSemanticEquivalence()
    {
        RequireOptIn();
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var sizes = new[] { 1_000, 5_000, 10_000, 25_000, 50_000, 100_000 };
        var rows = new List<string>
        {
            "sample_count,incremental_score_avg_us,incremental_score_alloc_bytes,incremental_timeline_avg_us,incremental_timeline_alloc_bytes,recent_demand_change_count,semantic_equivalence"
        };

        foreach (var size in sizes)
        {
            var full = BuildDemandTimeline(size);
            var accumulator = new MissionPerformanceLiveDemandEvidenceAccumulator();
            foreach (var sample in full)
            {
                accumulator.Upsert(sample);
            }
            var lifecycle = new ChallengeLifecycleSnapshot(
                pack.Challenge.ExactId,
                ChallengeLifecycleState.Active,
                size - 1L,
                0,
                null,
                null,
                null,
                null,
                Array.Empty<ChallengeConditionObservation>(),
                Array.Empty<ChallengeLifecycleTransition>());

            var expectedScore = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, full);
            var expectedTimeline = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, full);
            var actualScore = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, accumulator.ScoreAggregate);
            var actualTimeline = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, accumulator.RecentDemandChanges);
            var equivalent = expectedScore.SequenceEqual(actualScore)
                && expectedTimeline.RecentOperationalEvidence.SequenceEqual(actualTimeline.RecentOperationalEvidence)
                && expectedTimeline.Timeline.SequenceEqual(actualTimeline.Timeline);
            Assert.True(equivalent);

            var scoreMeasurement = Measure(
                () => OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, accumulator.ScoreAggregate),
                20);
            var timelineMeasurement = Measure(
                () => MissionPerformanceTimelineProjector.Project(lifecycle, null, null, accumulator.RecentDemandChanges),
                20);
            rows.Add(FormattableString.Invariant(
                $"{size},{scoreMeasurement.AverageMicroseconds:G17},{scoreMeasurement.AllocatedBytes},{timelineMeasurement.AverageMicroseconds:G17},{timelineMeasurement.AllocatedBytes},{accumulator.RecentDemandChanges.Count},{equivalent}"));
        }

        WriteAllLines("30-lr-m1-incremental-projector-scaling.csv", rows);
        WriteAllLines("31-lr-m1-incremental-summary.txt", new[]
        {
            "scope=MISSION live projection hotfix verification; replay/offline full-prefix projectors retained unchanged;",
            "live-demand-score-state=paired sample count + sum(abs error) + sum(abs demand);",
            $"live-demand-timeline-retention-max={MissionPerformanceTimelineProjector.MaximumRecentOperationalEvidenceEntries};",
            "strict-logical-order-validation=moved to incremental Upsert boundary;",
            "semantic-equivalence=full-prefix score and bounded timeline compared at every synthetic prefix size;",
            "expected-cost-shape=projection cost independent of historical sample_count for constant-demand prefix; aggregate session complexity O(n) rather than O(n^2);",
        });
        AppendProgress("LR-M1 incremental projector scaling/equivalence census completed");
    }

    private static void CapturePrimary(
        IntegratedAutomaticOperationRuntimeEngine engine,
        int logicalStep,
        List<PrimarySample> destination)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var plant = fullPlant.CandidatePlant;
        var primary = fullPlant.IntegratedCycle.PrimaryCircuit;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var flowController = protectedControl.ReactorPrimary.ControlAndActuator.Controllers.GetDiagnostic("flow-control");
        var levelController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("level-control");
        destination.Add(new PrimarySample(
            logicalStep,
            logicalStep / (double)StepsPerSecond,
            plant.GetFluidNode("outlet").Mass.Kilograms,
            plant.GetFluidNode("pressure").Pressure.Pascals,
            plant.GetFluidNode("outlet").Pressure.Pascals,
            plant.GetFluidNode("drum").Pressure.Pascals,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond - primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            drum.IncomingReturnMassFlowRate.KilogramsPerSecond,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            drum.SeparatedSteamMassFlowRate.KilogramsPerSecond,
            flowController.Error,
            flowController.IntegralTerm,
            flowController.Output,
            levelController.Error,
            levelController.IntegralTerm,
            levelController.Output));
    }

    private static IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot> BuildDemandTimeline(int count)
    {
        var result = new ExternalEnergyDemandEvidenceSnapshot[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = new ExternalEnergyDemandEvidenceSnapshot(
                true,
                "bounded-demand-following-5-10-5@1",
                index,
                index,
                5d,
                5d,
                5d,
                0d,
                null,
                null);
        }
        return result;
    }

    private static Measurement<T> Measure<T>(Func<T> action, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        var before = GC.GetAllocatedBytesForCurrentThread();
        T result = default!;
        for (var index = 0; index < iterations; index++)
        {
            result = action();
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        stopwatch.Stop();
        return new Measurement<T>(stopwatch.Elapsed.TotalMicroseconds / iterations, allocated / iterations, result);
    }

    private static double Slope(IReadOnlyList<PrimarySample> samples, Func<PrimarySample, double> selector)
    {
        var meanX = samples.Average(static item => item.SimulatedSeconds);
        var meanY = samples.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var sample in samples)
        {
            var dx = sample.SimulatedSeconds - meanX;
            numerator += dx * (selector(sample) - meanY);
            denominator += dx * dx;
        }
        return denominator > 0d ? numerator / denominator : double.NaN;
    }

    private static string FormatPrimary(PrimarySample item)
        => FormattableString.Invariant(
            $"{item.LogicalStep},{item.SimulatedSeconds:G17},{item.OutletMassKilograms:G17},{item.PressureHeaderPascals:G17},{item.OutletPressurePascals:G17},{item.DrumPressurePascals:G17},{item.PumpFlowKilogramsPerSecond:G17},{item.ChannelFlowKilogramsPerSecond:G17},{item.ReturnFlowKilogramsPerSecond:G17},{item.ChannelReturnResidualKilogramsPerSecond:G17},{item.DrumIncomingReturnKilogramsPerSecond:G17},{item.DrumRecirculatedLiquidKilogramsPerSecond:G17},{item.DrumSeparatedSteamKilogramsPerSecond:G17},{item.FlowControllerError:G17},{item.FlowControllerIntegral:G17},{item.FlowControllerOutput:G17},{item.LevelControllerError:G17},{item.LevelControllerIntegral:G17},{item.LevelControllerOutput:G17}");

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{OptInEnvironmentVariable}=1 is required for M10 final long failure diagnostic 2.");
        }
    }

    private static void AppendProgress(string text)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {text}{Environment.NewLine}", Utf8WithoutBom);
    }

    private static void WriteAllLines(string fileName, IEnumerable<string> lines)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.WriteAllLines(Path.Combine(ReportDirectory(), fileName), lines, Utf8WithoutBom);
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic2");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root could not be resolved for M10 final long diagnostic 2.");
    }

    private sealed record PrimarySample(
        int LogicalStep,
        double SimulatedSeconds,
        double OutletMassKilograms,
        double PressureHeaderPascals,
        double OutletPressurePascals,
        double DrumPressurePascals,
        double PumpFlowKilogramsPerSecond,
        double ChannelFlowKilogramsPerSecond,
        double ReturnFlowKilogramsPerSecond,
        double ChannelReturnResidualKilogramsPerSecond,
        double DrumIncomingReturnKilogramsPerSecond,
        double DrumRecirculatedLiquidKilogramsPerSecond,
        double DrumSeparatedSteamKilogramsPerSecond,
        double FlowControllerError,
        double FlowControllerIntegral,
        double FlowControllerOutput,
        double LevelControllerError,
        double LevelControllerIntegral,
        double LevelControllerOutput);

    private sealed record Measurement<T>(double AverageMicroseconds, long AllocatedBytes, T LastResult);
}
