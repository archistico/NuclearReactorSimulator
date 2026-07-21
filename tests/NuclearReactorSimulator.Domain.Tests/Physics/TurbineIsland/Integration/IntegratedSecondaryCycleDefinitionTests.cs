using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Integration;

public sealed class IntegratedSecondaryCycleDefinitionTests
{
    [Fact]
    public void Constructor_RejectsBlankIdBeforeAcceptingAComposition()
    {
        Assert.Throws<ArgumentException>(() => new IntegratedSecondaryCycleDefinition(" ", null!));
    }

    [Fact]
    public void Constructor_RejectsMissingGeneratorGridSystem()
    {
        Assert.Throws<ArgumentNullException>(() => new IntegratedSecondaryCycleDefinition("secondary", null!));
    }
}
