using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10FinalLongMissionProjectionHotfixTests
{
    [Fact]
    public void IncrementalLiveDemandEvidence_MatchesFullPrefixScoreAndTimelineSemantics()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var full = BuildTimeline(2_000);
        var lifecycle = Lifecycle(pack, full[^1].LogicalStep);
        var accumulator = new MissionPerformanceLiveDemandEvidenceAccumulator();
        foreach (var sample in full)
        {
            accumulator.Upsert(sample);
        }

        var expectedScore = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, full);
        var actualScore = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, accumulator.ScoreAggregate);
        Assert.Equal(expectedScore.ToArray(), actualScore.ToArray());

        var expectedTimeline = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, full);
        var actualTimeline = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, accumulator.RecentDemandChanges);
        Assert.Equal(expectedTimeline.LifecycleSpine, actualTimeline.LifecycleSpine);
        Assert.Equal(expectedTimeline.RecentOperationalEvidence, actualTimeline.RecentOperationalEvidence);
        Assert.Equal(expectedTimeline.Timeline, actualTimeline.Timeline);
        Assert.True(accumulator.RecentDemandChanges.Count <= MissionPerformanceTimelineProjector.MaximumRecentOperationalEvidenceEntries);
    }

    [Fact]
    public void IncrementalLiveDemandEvidence_SameStepReplacementPreservesLegacyUpsertMeaning()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var accumulator = new MissionPerformanceLiveDemandEvidenceAccumulator();
        var full = new List<ExternalEnergyDemandEvidenceSnapshot>();

        Upsert(full, accumulator, Sample(0, available: false, demand: null, actual: null));
        Upsert(full, accumulator, Sample(1, available: true, demand: 5d, actual: 4d));
        Upsert(full, accumulator, Sample(2, available: true, demand: 5d, actual: 4.5d));
        Upsert(full, accumulator, Sample(2, available: true, demand: 10d, actual: 8d));
        Upsert(full, accumulator, Sample(3, available: false, demand: null, actual: null));
        Upsert(full, accumulator, Sample(4, available: true, demand: 10d, actual: 9d));
        Upsert(full, accumulator, Sample(4, available: true, demand: 5d, actual: 5d));

        var lifecycle = Lifecycle(pack, full[^1].LogicalStep);
        var expectedScore = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, full);
        var actualScore = OperationalChallengeScoreEvidenceProjector.ProjectLive(pack, lifecycle, accumulator.ScoreAggregate);
        Assert.Equal(expectedScore.ToArray(), actualScore.ToArray());

        var expectedTimeline = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, full);
        var actualTimeline = MissionPerformanceTimelineProjector.Project(lifecycle, null, null, accumulator.RecentDemandChanges);
        Assert.Equal(expectedTimeline.RecentOperationalEvidence, actualTimeline.RecentOperationalEvidence);
        Assert.Equal(expectedTimeline.Timeline, actualTimeline.Timeline);
    }

    [Fact]
    public void IncrementalLiveDemandEvidence_KeepsBoundedPresentationInputForLongConstantPrefix()
    {
        var accumulator = new MissionPerformanceLiveDemandEvidenceAccumulator();
        for (var step = 0; step < 100_000; step++)
        {
            accumulator.Upsert(Sample(step, available: true, demand: 5d, actual: 5d));
        }

        Assert.Equal(99_999, accumulator.Current.LogicalStep);
        Assert.Single(accumulator.RecentDemandChanges);
        Assert.Equal(100_000, accumulator.ScoreAggregate.PairedSampleCount);
        Assert.Equal(0d, accumulator.ScoreAggregate.SumAbsoluteErrorMegawatts);
        Assert.Equal(500_000d, accumulator.ScoreAggregate.SumAbsoluteDemandMegawatts);
    }

    private static IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot> BuildTimeline(int count)
    {
        var result = new ExternalEnergyDemandEvidenceSnapshot[count];
        for (var index = 0; index < count; index++)
        {
            var demand = ((index / 5) % 2) == 0 ? 5d : 10d;
            var actual = demand - ((index % 7) * 0.05d);
            result[index] = Sample(index, available: true, demand, actual);
        }
        return result;
    }

    private static ExternalEnergyDemandEvidenceSnapshot Sample(
        long step,
        bool available,
        double? demand,
        double? actual)
        => available
            ? new ExternalEnergyDemandEvidenceSnapshot(
                true,
                "bounded-demand-following-5-10-5@1",
                step,
                step,
                demand,
                demand,
                actual,
                demand - actual,
                null,
                null)
            : ExternalEnergyDemandEvidenceSnapshot.Unavailable(step);

    private static ChallengeLifecycleSnapshot Lifecycle(OperationalChallengePackDefinition pack, long step)
        => new(
            pack.Challenge.ExactId,
            ChallengeLifecycleState.Active,
            step,
            0,
            null,
            null,
            null,
            null,
            Array.Empty<ChallengeConditionObservation>(),
            Array.Empty<ChallengeLifecycleTransition>());

    private static void Upsert(
        List<ExternalEnergyDemandEvidenceSnapshot> full,
        MissionPerformanceLiveDemandEvidenceAccumulator accumulator,
        ExternalEnergyDemandEvidenceSnapshot sample)
    {
        accumulator.Upsert(sample);
        if (full.Count != 0 && full[^1].LogicalStep == sample.LogicalStep)
        {
            full[^1] = sample;
        }
        else
        {
            full.Add(sample);
        }
    }
}
