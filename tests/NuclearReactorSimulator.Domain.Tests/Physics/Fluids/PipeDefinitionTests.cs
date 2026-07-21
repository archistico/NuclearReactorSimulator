using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Fluids;

public sealed class PipeDefinitionTests
{
    private static readonly QuadraticHydraulicResistance Resistance =
        QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d);

    [Fact]
    public void Constructor_NormalizesIdentifiersAndPreservesResistance()
    {
        var pipe = new PipeDefinition(" primary-loop ", " node-a ", " node-b ", Resistance);

        Assert.Equal("primary-loop", pipe.Id);
        Assert.Equal("node-a", pipe.FromNodeId);
        Assert.Equal("node-b", pipe.ToNodeId);
        Assert.Equal(Resistance, pipe.Resistance);
    }

    [Fact]
    public void SameEndpoint_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new PipeDefinition("pipe", "node", "node", Resistance));
    }


    [Fact]
    public void DefaultResistance_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PipeDefinition("pipe", "from", "to", default));
    }

    [Theory]
    [InlineData("", "a", "b")]
    [InlineData("pipe", "", "b")]
    [InlineData("pipe", "a", "")]
    [InlineData("   ", "a", "b")]
    [InlineData("pipe", "   ", "b")]
    [InlineData("pipe", "a", "   ")]
    public void EmptyIdentifiers_AreRejected(string id, string fromNodeId, string toNodeId)
    {
        Assert.Throws<ArgumentException>(() =>
            new PipeDefinition(id, fromNodeId, toNodeId, Resistance));
    }
}
