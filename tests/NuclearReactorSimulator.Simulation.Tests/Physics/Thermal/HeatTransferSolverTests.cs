using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Thermal;

public sealed class HeatTransferSolverTests
{
    [Fact]
    public void HotFromDomain_TransfersPositiveHeatTowardColderToDomain()
    {
        var result = Solve(300d, 250d, 10d);

        Assert.Equal(50d, result.TemperatureDifference.Kelvins, 12);
        Assert.Equal(500d, result.HeatFlowRate.Kilowatts, 12);
        Assert.Equal(-500d, result.FromDomainBalance.NetHeatRate.Kilowatts, 12);
        Assert.Equal(500d, result.ToDomainBalance.NetHeatRate.Kilowatts, 12);
    }

    [Fact]
    public void ReversedTemperatureGradient_ReversesHeatFlowNaturally()
    {
        var result = Solve(250d, 300d, 10d);

        Assert.Equal(-50d, result.TemperatureDifference.Kelvins, 12);
        Assert.Equal(-500d, result.HeatFlowRate.Kilowatts, 12);
        Assert.True(result.FromDomainBalance.NetHeatRate.Watts > 0d);
        Assert.True(result.ToDomainBalance.NetHeatRate.Watts < 0d);
    }

    [Fact]
    public void EqualTemperatures_ProduceExactlyZeroHeatFlow()
    {
        var result = Solve(275d, 275d, 10d);

        Assert.Equal(Power.Zero, result.HeatFlowRate);
        Assert.Equal(ThermalEnergyBalance.Zero, result.FromDomainBalance);
        Assert.Equal(ThermalEnergyBalance.Zero, result.ToDomainBalance);
    }

    [Fact]
    public void EndpointBalances_ConserveEnergyExactly()
    {
        var result = Solve(400d, 100d, 25d);
        var total = result.FromDomainBalance + result.ToDomainBalance;

        Assert.Equal(Power.Zero, total.NetHeatRate);
    }

    [Fact]
    public void HigherConductance_IncreasesHeatFlowLinearly()
    {
        var low = Solve(300d, 250d, 5d);
        var high = Solve(300d, 250d, 20d);

        Assert.Equal(low.HeatFlowRate.Watts * 4d, high.HeatFlowRate.Watts, 12);
    }

    [Fact]
    public void SameInputs_ProduceSameResult()
    {
        var solver = new HeatTransferSolver();
        var definition = CreateDefinition(10d);
        var from = Temperature.FromDegreesCelsius(300d);
        var to = Temperature.FromDegreesCelsius(250d);

        Assert.Equal(
            solver.Solve(definition, from, to),
            solver.Solve(definition, from, to));
    }

    private static HeatTransferResult Solve(
        double fromCelsius,
        double toCelsius,
        double kilowattsPerKelvin)
    {
        return new HeatTransferSolver().Solve(
            CreateDefinition(kilowattsPerKelvin),
            Temperature.FromDegreesCelsius(fromCelsius),
            Temperature.FromDegreesCelsius(toCelsius));
    }

    private static HeatTransferDefinition CreateDefinition(double kilowattsPerKelvin)
    {
        return new HeatTransferDefinition(
            "heat-link",
            "from",
            "to",
            ThermalConductance.FromKilowattsPerKelvin(kilowattsPerKelvin));
    }
}
