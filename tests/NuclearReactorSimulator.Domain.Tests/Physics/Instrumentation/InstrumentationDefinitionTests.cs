using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Instrumentation;

public sealed class InstrumentationDefinitionTests
{
    [Fact]
    public void SignalRange_RequiresFiniteOrderedBoundsAndClampsDeterministically()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignalRange(1d, 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignalRange(double.NaN, 1d));

        var range = new SignalRange(-10d, 30d);

        Assert.Equal(-10d, range.Clamp(-100d));
        Assert.Equal(12d, range.Clamp(12d));
        Assert.Equal(30d, range.Clamp(100d));
    }

    [Fact]
    public void LinearScale_MapsEngineeringRangeWithoutHiddenClamping()
    {
        var range = new SignalRange(0d, 200d);
        var scale = new LinearSignalScale(4d, 20d);

        Assert.Equal(4d, scale.Map(0d, range));
        Assert.Equal(12d, scale.Map(100d, range));
        Assert.Equal(20d, scale.Map(200d, range));
        Assert.Equal(28d, scale.Map(300d, range));
    }

    [Fact]
    public void InstrumentationSystem_RejectsDuplicateChannelIdsButAllowsRedundantSensorsOnOneSource()
    {
        var range = new SignalRange(0d, 100d);
        var scale = LinearSignalScale.NormalizedZeroToOne;
        var first = new InstrumentChannelDefinition("A", "source", "%", range, scale, TimeSpan.Zero);
        var redundant = new InstrumentChannelDefinition("B", "source", "%", range, scale, TimeSpan.Zero);

        var system = new InstrumentationSystemDefinition("instrumentation", new[] { first, redundant });

        Assert.Throws<ArgumentOutOfRangeException>(() => new InstrumentChannelDefinition("default-range", "source", "%", default, scale, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InstrumentChannelDefinition("default-scale", "source", "%", range, default, TimeSpan.Zero));
        Assert.Equal(2, system.Channels.Count);
        Assert.Throws<ArgumentException>(() => new InstrumentationSystemDefinition("invalid", new[] { first, first }));
    }
}
