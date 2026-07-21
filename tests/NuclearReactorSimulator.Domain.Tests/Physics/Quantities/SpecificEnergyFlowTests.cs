using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Quantities;

public sealed class SpecificEnergyFlowTests
{
    [Fact]
    public void SpecificEnergyTimesMassFlow_ProducesSignedPower()
    {
        var specificEnergy = SpecificEnergy.FromKilojoulesPerKilogram(250d);

        var forward = specificEnergy * MassFlowRate.FromKilogramsPerSecond(4d);
        var reverse = MassFlowRate.FromKilogramsPerSecond(-2d) * specificEnergy;

        Assert.Equal(1d, forward.Megawatts, 12);
        Assert.Equal(-0.5d, reverse.Megawatts, 12);
    }
}
