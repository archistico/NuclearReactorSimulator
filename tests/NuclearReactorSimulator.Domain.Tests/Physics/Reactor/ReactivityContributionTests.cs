using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor;

public sealed class ReactivityContributionTests
{
    [Fact]
    public void Contribution_PreservesIdentityKindAndSignedValue()
    {
        var contribution = new ReactivityContribution(
            "void-zone-a",
            ReactivityContributionKind.Void,
            Reactivity.FromPcm(275d));

        Assert.Equal("void-zone-a", contribution.Id);
        Assert.Equal(ReactivityContributionKind.Void, contribution.Kind);
        Assert.Equal(275d, contribution.Value.Pcm, 12);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyId_IsRejected(string id)
    {
        Assert.Throws<ArgumentException>(() => new ReactivityContribution(
            id,
            ReactivityContributionKind.Other,
            Reactivity.Zero));
    }

    [Fact]
    public void UnknownKind_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReactivityContribution(
            "invalid-kind",
            (ReactivityContributionKind)999,
            Reactivity.Zero));
    }
}
