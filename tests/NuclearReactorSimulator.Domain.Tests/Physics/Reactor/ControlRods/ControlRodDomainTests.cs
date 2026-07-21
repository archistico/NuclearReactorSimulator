using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.ControlRods;

public sealed class ControlRodDomainTests
{
    [Fact]
    public void Position_UsesExplicitFullyInsertedAndFullyWithdrawnSemantics()
    {
        Assert.Equal(0d, ControlRodPosition.FullyInserted.FractionWithdrawn);
        Assert.Equal(1d, ControlRodPosition.FullyWithdrawn.FractionWithdrawn);
        Assert.Equal(25d, ControlRodPosition.FromFractionWithdrawn(0.25d).PercentWithdrawn, 12);
        Assert.Equal(0.75d, ControlRodPosition.FromPercentWithdrawn(25d).FractionInserted, 12);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Position_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ControlRodPosition.FromFractionWithdrawn(value));
    }

    [Fact]
    public void TravelRate_CanBeDerivedFromFullTravelTime()
    {
        var rate = ControlRodTravelRate.FromFullTravelTime(TimeSpan.FromSeconds(20d));

        Assert.Equal(0.05d, rate.FractionPerSecond, 12);
        Assert.Equal(TimeSpan.FromSeconds(20d), rate.FullTravelTime);
    }

    [Fact]
    public void Definition_PreservesSignedWorthEndpointsWithoutEmbeddingKinetics()
    {
        var definition = Rod("rod-a", "bank-a", -2_000d, 100d);

        Assert.Equal(-2_000d, definition.FullyInsertedReactivity.Pcm, 10);
        Assert.Equal(100d, definition.FullyWithdrawnReactivity.Pcm, 10);
    }

    [Fact]
    public void Group_CanonicalizesMembersAndRejectsDuplicates()
    {
        var group = new ControlRodGroupDefinition("bank-a", ["rod-b", "rod-a"]);

        Assert.Equal(new[] { "rod-a", "rod-b" }, group.RodIds);
        Assert.Throws<ArgumentException>(() => new ControlRodGroupDefinition("bad", ["rod-a", "rod-a"]));
    }

    [Fact]
    public void SystemDefinition_RequiresBidirectionallyConsistentMembership()
    {
        var rods = new[] { Rod("rod-a", "bank-a", -1_000d, 0d) };
        var wrongGroup = new ControlRodGroupDefinition("bank-a", ["rod-b"]);

        Assert.Throws<ArgumentException>(() => new ControlRodSystemDefinition(rods, [wrongGroup]));
    }

    [Fact]
    public void SystemState_IsCanonicalAndIndependentFromCallerArray()
    {
        var source = new[]
        {
            new ControlRodState("rod-b", ControlRodPosition.FullyWithdrawn),
            new ControlRodState("rod-a", ControlRodPosition.FullyInserted),
        };
        var state = new ControlRodSystemState(source);

        source[0] = new ControlRodState("replacement", ControlRodPosition.FullyInserted);

        Assert.Equal(new[] { "rod-a", "rod-b" }, state.Rods.Select(static rod => rod.RodId));
        Assert.Equal("rod-b", state.GetRod("rod-b").RodId);
    }

    [Fact]
    public void Command_ValidatesTargetAndMotionEnums()
    {
        Assert.Throws<ArgumentException>(() => new ControlRodCommand(" ", ControlRodCommandTargetKind.Rod, ControlRodMotion.Hold));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRodCommand("rod-a", (ControlRodCommandTargetKind)99, ControlRodMotion.Hold));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRodCommand("rod-a", ControlRodCommandTargetKind.Rod, (ControlRodMotion)99));
    }

    private static ControlRodDefinition Rod(
        string id,
        string groupId,
        double insertedPcm,
        double withdrawnPcm)
    {
        return new ControlRodDefinition(
            id,
            groupId,
            ControlRodTravelRate.FromFullTravelTime(TimeSpan.FromSeconds(20d)),
            Reactivity.FromPcm(insertedPcm),
            Reactivity.FromPcm(withdrawnPcm));
    }
}
