using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Quantities;

public sealed class ReactivityTests
{
    [Fact]
    public void StoresCanonicalDeltaKOverKAndConvertsUnits()
    {
        var reactivity = Reactivity.FromPcm(100d);

        Assert.Equal(0.001d, reactivity.DeltaKOverK, 12);
        Assert.Equal(0.1d, reactivity.PercentDeltaKOverK, 12);
        Assert.Equal(100d, reactivity.Pcm, 12);
    }

    [Fact]
    public void PercentDeltaKOverKConversion_IsExplicit()
    {
        var reactivity = Reactivity.FromPercentDeltaKOverK(-0.35d);

        Assert.Equal(-0.0035d, reactivity.DeltaKOverK, 12);
        Assert.Equal(-350d, reactivity.Pcm, 12);
    }

    [Fact]
    public void Reactivity_IsSignedAndSupportsArithmetic()
    {
        var positive = Reactivity.FromPcm(500d);
        var negative = Reactivity.FromPcm(-125d);

        Assert.Equal(375d, (positive + negative).Pcm, 12);
        Assert.Equal(625d, (positive - negative).Pcm, 12);
        Assert.Equal(-500d, (-positive).Pcm, 12);
        Assert.Equal(250d, (positive * 0.5d).Pcm, 12);
        Assert.Equal(250d, (positive / 2d).Pcm, 12);
    }

    [Fact]
    public void NonFiniteReactivity_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Reactivity.FromDeltaKOverK(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Reactivity.FromPcm(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => Reactivity.FromPercentDeltaKOverK(double.NegativeInfinity));
    }

    [Fact]
    public void Zero_IsCanonicalAcrossSignedZero()
    {
        Assert.Equal(Reactivity.Zero, Reactivity.FromDeltaKOverK(-0d));
    }
}
