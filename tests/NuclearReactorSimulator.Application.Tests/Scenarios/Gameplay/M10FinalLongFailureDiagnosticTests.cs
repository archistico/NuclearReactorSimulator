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
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Diagnostic-only follow-up to the first M10 final long campaign. It does not change production behavior or acceptance
/// limits. The first probe characterizes exact-v4 through the already-validated 300 s domain immediately before the
/// observed LR-H1 failure interval. The second isolates the MISSION live projection prefix-scan cost without a long plant run.
/// </summary>
public sealed class M10FinalLongFailureDiagnosticTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC";
    private const int StepsPerSecond = 100;
    private const double ObservedFailureSpecificVolume = 0.0026153411609661885d;
    private const double ObservedFailureSpecificInternalEnergy = 1_615_124.4119888516d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic")]
    public void LR_H1_ExactV4_ThreeHundredSecondEquilibriumResidualCensus()
    {
        RequireOptIn();
        const int totalSteps = 30_000;
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());

        var nodeSamples = new List<NodeSample>();
        var performanceRows = new List<string>
        {
            "window_end_seconds,window_wall_seconds,cumulative_wall_seconds,logical_step"
        };
        var stopwatch = Stopwatch.StartNew();
        var windowStart = stopwatch.Elapsed;

        CaptureNodes(engine, 0, nodeSamples);
        for (var step = 1; step <= totalSteps; step++)
        {
            _ = engine.Step(ControlRoomRunState.Running);

            if (step % StepsPerSecond == 0)
            {
                CaptureNodes(engine, step, nodeSamples);
            }
            if (step % 1000 == 0)
            {
                var now = stopwatch.Elapsed;
                performanceRows.Add(FormattableString.Invariant(
                    $"{step / StepsPerSecond},{(now - windowStart).TotalSeconds:G17},{now.TotalSeconds:G17},{step}"));
                windowStart = now;
            }
            if (step % 3000 == 0)
            {
                AppendProgress($"LR-H1 diagnostic simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }
        stopwatch.Stop();

        var finalWindowStart = 240d;
        var slopes = nodeSamples
            .Where(sample => sample.SimulatedSeconds >= finalWindowStart)
            .GroupBy(static sample => sample.NodeId, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(group => BuildSlopeRow(group.Key, group.ToArray()))
            .ToArray();

        WriteAllLines("01-lr-h1-node-state-trajectory.csv", new[]
        {
            "logical_step,simulated_seconds,node_id,mass_kg,internal_energy_j,specific_internal_energy_j_kg,specific_volume_m3_kg,pressure_pa,temperature_k,phase,vapor_quality,delta_to_observed_failure_u_j_kg,delta_to_observed_failure_v_m3_kg"
        }.Concat(nodeSamples.Select(FormatNodeSample)));
        WriteAllLines("02-lr-h1-final-60s-node-slopes.csv", new[]
        {
            "node_id,mass_slope_kg_s,internal_energy_slope_w,specific_u_slope_j_kg_s,specific_volume_slope_m3_kg_s,pressure_slope_pa_s,temperature_slope_k_s"
        }.Concat(slopes));
        WriteAllLines("03-lr-h1-window-performance.csv", performanceRows);

        var outlet = nodeSamples.Where(static sample => sample.NodeId == "outlet").ToArray();
        Assert.NotEmpty(outlet);
        var selected = outlet.Where(static sample => sample.SimulatedSeconds is 0d or 60d or 120d or 180d or 240d or 270d or 300d).ToArray();
        WriteAllLines("04-lr-h1-outlet-pre-failure-comparison.csv", new[]
        {
            "simulated_seconds,specific_volume_m3_kg,specific_internal_energy_j_kg,pressure_pa,temperature_k,phase,delta_to_observed_failure_u_j_kg,delta_to_observed_failure_v_m3_kg"
        }.Concat(selected.Select(static sample => FormattableString.Invariant(
            $"{sample.SimulatedSeconds:G17},{sample.SpecificVolume:G17},{sample.SpecificInternalEnergy:G17},{sample.Pressure:G17},{sample.Temperature:G17},{sample.Phase},{sample.DeltaToFailureU:G17},{sample.DeltaToFailureV:G17}"))));

        var finalOutlet = outlet[^1];
        WriteAllLines("00-lr-h1-diagnostic-summary.txt", new[]
        {
            "scope=M10 LR-H1 pre-failure exact-v4 diagnostic only; production runtime and acceptance criteria unchanged;",
            "validated-domain-replayed=300 simulated seconds / 30000 deterministic 10 ms steps;",
            FormattableString.Invariant($"wall-seconds={stopwatch.Elapsed.TotalSeconds:G17};"),
            FormattableString.Invariant($"outlet-at-300s-v={finalOutlet.SpecificVolume:G17}; outlet-at-300s-u={finalOutlet.SpecificInternalEnergy:G17};"),
            FormattableString.Invariant($"observed-lr-h1-failure-v={ObservedFailureSpecificVolume:G17}; observed-lr-h1-failure-u={ObservedFailureSpecificInternalEnergy:G17};"),
            FormattableString.Invariant($"outlet-300s-to-failure-delta-v={finalOutlet.DeltaToFailureV:G17}; outlet-300s-to-failure-delta-u={finalOutlet.DeltaToFailureU:G17};"),
            "classification=DIAGNOSTIC-ONLY; use final-60s node slopes and outlet trajectory to decide whether the next step is equilibrium/seed, controller, thermodynamic-domain or hydraulic/coupling diagnosis;",
        });

        Assert.Equal(totalSteps, engine.LogicalStep);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic")]
    public void LR_M1_MissionProjectionPrefixScalingCensus()
    {
        RequireOptIn();
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var sizes = new[] { 1_000, 5_000, 10_000, 25_000, 50_000, 100_000 };
        var rows = new List<string>
        {
            "sample_count,score_project_avg_us,score_project_alloc_bytes,timeline_project_avg_us,timeline_project_alloc_bytes,recent_operational_count,timeline_count"
        };

        foreach (var size in sizes)
        {
            var demand = BuildDemandTimeline(size);
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

            _ = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, demand);
            _ = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, demand);

            var scoreMeasurement = Measure(() => OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, demand), 3);
            var timelineProjection = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, demand);
            var timelineMeasurement = Measure(() => MissionPerformanceTimelineProjector.Project(lifecycle, null, null, demand), 3);

            Assert.Equal(pack.ScoreEvidenceBindings.Count, scoreMeasurement.LastResult.Count);
            Assert.True(timelineProjection.RecentOperationalEvidence.Count <= MissionPerformanceTimelineProjector.MaximumRecentOperationalEvidenceEntries);

            rows.Add(FormattableString.Invariant(
                $"{size},{scoreMeasurement.AverageMicroseconds:G17},{scoreMeasurement.AllocatedBytes},{timelineMeasurement.AverageMicroseconds:G17},{timelineMeasurement.AllocatedBytes},{timelineProjection.RecentOperationalEvidence.Count},{timelineProjection.Timeline.Count}"));
        }

        WriteAllLines("10-lr-m1-projector-scaling.csv", rows);
        WriteAllLines("11-lr-m1-static-call-chain.txt", new[]
        {
            "SingleStep -> ControlRoomRuntimeCoordinator publishes DeterministicStepCompleted and SnapshotChanged for every step.",
            "MissionPerformanceLiveSnapshotSource.OnDeterministicStepCompleted appends one demand sample per logical step.",
            "MissionPerformanceLiveSnapshotSource.OnPresentationSnapshotChanged -> RefreshLocked -> BuildCurrent on every SingleStep presentation.",
            "BuildCurrent -> OperationalChallengeScoreEvidenceProjector.ProjectLive(..., _demandTimeline).",
            "ProjectLive validates strict ordering by scanning the full timeline; Demand() filters/materializes the full timeline and computes aggregate averages.",
            "BuildCurrent -> MissionPerformanceSnapshotProjector -> MissionPerformanceTimelineProjector.Project(..., _demandTimeline).",
            "MissionPerformanceTimelineProjector.AddDemandChanges scans the full demand timeline even though output retains only demand change points.",
            "Therefore the live SingleStep path performs O(n) prefix work at logical step n and O(n^2) aggregate work over a long session unless replaced by incremental live evidence state.",
            "This diagnostic does not authorize a production fix; it records the cost shape needed to design an exact-semantics incremental replacement.",
        });
        AppendProgress("LR-M1 projector prefix scaling census completed");
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

    private static void CaptureNodes(IntegratedAutomaticOperationRuntimeEngine engine, int logicalStep, List<NodeSample> destination)
    {
        var plant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant;
        foreach (var node in plant.FluidNodes.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var specificVolume = node.Volume.CubicMetres / node.Mass.Kilograms;
            var specificEnergy = node.SpecificInternalEnergy.JoulesPerKilogram;
            destination.Add(new NodeSample(
                logicalStep,
                logicalStep / (double)StepsPerSecond,
                node.Id,
                node.Mass.Kilograms,
                node.InternalEnergy.Joules,
                specificEnergy,
                specificVolume,
                node.Pressure.Pascals,
                node.Temperature.Kelvins,
                node.Phase.ToString(),
                node.VaporQuality?.Fraction,
                ObservedFailureSpecificInternalEnergy - specificEnergy,
                ObservedFailureSpecificVolume - specificVolume));
        }
    }

    private static string BuildSlopeRow(string nodeId, IReadOnlyList<NodeSample> samples)
        => FormattableString.Invariant(
            $"{nodeId},{Slope(samples, static item => item.Mass):G17},{Slope(samples, static item => item.InternalEnergy):G17},{Slope(samples, static item => item.SpecificInternalEnergy):G17},{Slope(samples, static item => item.SpecificVolume):G17},{Slope(samples, static item => item.Pressure):G17},{Slope(samples, static item => item.Temperature):G17}");

    private static double Slope(IReadOnlyList<NodeSample> samples, Func<NodeSample, double> selector)
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

    private static string FormatNodeSample(NodeSample sample)
        => FormattableString.Invariant(
            $"{sample.LogicalStep},{sample.SimulatedSeconds:G17},{sample.NodeId},{sample.Mass:G17},{sample.InternalEnergy:G17},{sample.SpecificInternalEnergy:G17},{sample.SpecificVolume:G17},{sample.Pressure:G17},{sample.Temperature:G17},{sample.Phase},{(sample.VaporQuality?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty)},{sample.DeltaToFailureU:G17},{sample.DeltaToFailureV:G17}");

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{OptInEnvironmentVariable}=1 is required for M10 final long failure diagnostics.");
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic1");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root could not be resolved for M10 final long diagnostic 1.");
    }

    private sealed record NodeSample(
        int LogicalStep,
        double SimulatedSeconds,
        string NodeId,
        double Mass,
        double InternalEnergy,
        double SpecificInternalEnergy,
        double SpecificVolume,
        double Pressure,
        double Temperature,
        string Phase,
        double? VaporQuality,
        double DeltaToFailureU,
        double DeltaToFailureV);

    private sealed record Measurement<T>(double AverageMicroseconds, long AllocatedBytes, T LastResult);
}
