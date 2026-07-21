using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class FluidNodeBalanceTests
{
    [Fact]
    public void DefaultBalance_IsZeroBalance()
    {
        Assert.Equal(FluidNodeBalance.Zero, default(FluidNodeBalance));
    }

    [Fact]
    public void Balances_ComposeAsSignedNetRates()
    {
        var inlet = new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(12d),
            Power.FromMegawatts(8d));
        var outlet = new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(-5d),
            Power.FromMegawatts(-3d));

        var net = inlet + outlet;

        Assert.Equal(7d, net.NetMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(5d, net.NetEnergyRate.Megawatts, 12);
    }
}
