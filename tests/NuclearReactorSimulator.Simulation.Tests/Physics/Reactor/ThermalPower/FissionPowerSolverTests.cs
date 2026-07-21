using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ThermalPower;

public sealed class FissionPowerSolverTests
{
    [Fact]
    public void ReferencePopulation_ProducesReferenceThermalPower()
    {
        var snapshot = Solver().Solve(NeutronPopulation.Reference);

        Assert.Equal(3_200d, snapshot.TotalFissionThermalPower.Megawatts, 10);
    }

    [Fact]
    public void ThermalPower_ScalesLinearlyWithNormalizedNeutronPopulation()
    {
        var solver = Solver();

        Assert.Equal(0d, solver.Solve(NeutronPopulation.Zero).TotalFissionThermalPower.Watts, 12);
        Assert.Equal(1_600d, solver.Solve(NeutronPopulation.FromRelative(0.5d)).TotalFissionThermalPower.Megawatts, 10);
        Assert.Equal(6_400d, solver.Solve(NeutronPopulation.FromRelative(2d)).TotalFissionThermalPower.Megawatts, 10);
    }

    [Fact]
    public void HeatDistribution_PreservesTotalPowerExactly()
    {
        var snapshot = Solver().Solve(NeutronPopulation.Reference);

        var sumWatts = snapshot.HeatDepositions.Sum(static deposition => deposition.ThermalPower.Watts);

        Assert.Equal(snapshot.TotalFissionThermalPower.Watts, sumWatts);
        Assert.Equal(640d, snapshot.GetDeposition("coolant").ThermalPower.Megawatts, 10);
        Assert.Equal(2_240d, snapshot.GetDeposition("fuel").ThermalPower.Megawatts, 10);
        Assert.Equal(320d, snapshot.GetDeposition("structures").ThermalPower.Megawatts, 10);
    }

    [Fact]
    public void HeatDistribution_IsCanonicalAndIndependentFromInputOrder()
    {
        var left = new FissionPowerSolver(Definition(
            Destination("structures", 0.1d),
            Destination("fuel", 0.7d),
            Destination("coolant", 0.2d)))
            .Solve(NeutronPopulation.Reference);

        var right = new FissionPowerSolver(Definition(
            Destination("coolant", 0.2d),
            Destination("structures", 0.1d),
            Destination("fuel", 0.7d)))
            .Solve(NeutronPopulation.Reference);

        Assert.Equal(left.HeatDepositions.Select(static item => item.TargetDomainId), right.HeatDepositions.Select(static item => item.TargetDomainId));
        Assert.Equal(left.HeatDepositions.Select(static item => item.ThermalPower.Watts), right.HeatDepositions.Select(static item => item.ThermalPower.Watts));
    }

    [Fact]
    public void Deposition_AdaptsToThermalAndFluidEnergyBoundariesWithoutMassCreation()
    {
        var deposition = Solver().Solve(NeutronPopulation.Reference).GetDeposition("coolant");

        var thermalBalance = deposition.ToThermalEnergyBalance();
        var fluidBalance = deposition.ToFluidNodeBalance();

        Assert.Equal(deposition.ThermalPower, thermalBalance.NetHeatRate);
        Assert.Equal(MassFlowRate.Zero, fluidBalance.NetMassFlowRate);
        Assert.Equal(deposition.ThermalPower, fluidBalance.NetEnergyRate);
    }

    [Fact]
    public void OverflowingPowerScaling_FailsFast()
    {
        var definition = new FissionPowerDefinition(
            "overflow",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromWatts(double.MaxValue)),
            [Destination("fuel", 1d)]);
        var solver = new FissionPowerSolver(definition);

        Assert.Throws<FissionPowerNumericalException>(() => solver.Solve(NeutronPopulation.FromRelative(2d)));
    }

    private static FissionPowerSolver Solver() => new(Definition(
        Destination("fuel", 0.7d),
        Destination("coolant", 0.2d),
        Destination("structures", 0.1d)));

    private static FissionPowerDefinition Definition(params FissionHeatDestinationDefinition[] destinations)
        => new(
            "core-fission",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(3_200d)),
            destinations);

    private static FissionHeatDestinationDefinition Destination(string id, double fraction)
        => new(id, HeatDepositionFraction.FromFraction(fraction));
}
