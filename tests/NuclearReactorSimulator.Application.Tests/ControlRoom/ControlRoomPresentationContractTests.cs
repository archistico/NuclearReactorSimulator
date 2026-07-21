using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomPresentationContractTests
{
    [Fact]
    public void ControlRoomSnapshot_ContainsPresentationDataOnly()
    {
        var propertyTypes = typeof(ControlRoomSnapshot)
            .GetProperties()
            .Select(static property => property.PropertyType)
            .ToArray();

        Assert.All(
            propertyTypes,
            static type => Assert.False(
                type.Namespace?.StartsWith("NuclearReactorSimulator.Simulation", StringComparison.Ordinal) == true));
        Assert.All(
            propertyTypes,
            static type => Assert.False(
                type.Namespace?.StartsWith("NuclearReactorSimulator.Domain.Physics", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void InMemorySnapshotSource_PublishesImmutablePresentationSnapshot()
    {
        var source = new InMemoryControlRoomSnapshotSource(ControlRoomSnapshot.ShellOnly);
        ControlRoomSnapshot? observed = null;
        source.SnapshotChanged += (_, args) => observed = args.Snapshot;
        var published = new ControlRoomSnapshot(
            42,
            ControlRoomRunState.Paused,
            120,
            2,
            3,
            1,
            false,
            true,
            false);

        source.Publish(published);

        Assert.Same(published, source.Current);
        Assert.Same(published, observed);
        Assert.Equal(118, source.Current.ValidMeasuredSignalCount);
        Assert.True(source.Current.AnyTripActive);
    }

    [Fact]
    public void DesktopPerformanceBudget_IsFiniteAndPresentationOnly()
    {
        var budget = ControlRoomPerformanceBudget.DesktopDefault;

        Assert.InRange(budget.MaximumPresentationRefreshHertz, 1, 60);
        Assert.True(budget.MaximumVisibleWorkspaceRows > 0);
        Assert.True(budget.MaximumVisibleTrendSeries > 0);
    }
}
