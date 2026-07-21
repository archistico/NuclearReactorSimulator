using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Quantities;

public sealed class PumpDimensionalArithmeticTests
{
    [Fact]
    public void MassFlowDividedByDensity_ProducesVolumetricFlow()
    {
        var massFlow = MassFlowRate.FromKilogramsPerSecond(2d);
        var density = Density.FromKilogramsPerCubicMetre(1_000d);

        var volumetricFlow = massFlow / density;

        Assert.Equal(0.002d, volumetricFlow.CubicMetresPerSecond, 12);
    }

    [Fact]
    public void SignedMassFlow_PreservesSignWhenConvertedToVolumetricFlow()
    {
        var volumetricFlow = MassFlowRate.FromKilogramsPerSecond(-4d)
            / Density.FromKilogramsPerCubicMetre(800d);

        Assert.Equal(-0.005d, volumetricFlow.CubicMetresPerSecond, 12);
    }

    [Fact]
    public void PressureDifferenceTimesVolumetricFlow_ProducesSignedPower()
    {
        var pressure = PressureDifference.FromKilopascals(400d);
        var forward = VolumetricFlowRate.FromCubicMetresPerSecond(0.002d);
        var reverse = VolumetricFlowRate.FromCubicMetresPerSecond(-0.002d);

        Assert.Equal(800d, (pressure * forward).Watts, 12);
        Assert.Equal(-800d, (reverse * pressure).Watts, 12);
    }
}
