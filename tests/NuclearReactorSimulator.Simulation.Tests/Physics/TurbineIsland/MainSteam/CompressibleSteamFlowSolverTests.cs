using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.MainSteam;

public sealed class CompressibleSteamFlowSolverTests
{
    private static readonly Pressure UpstreamPressure = Pressure.FromMegapascals(6.2725d);
    private static readonly Temperature UpstreamTemperature = Temperature.FromDegreesCelsius(278.5d);

    [Fact]
    public void Solve_ChokesBelowCriticalPressureRatioAndCapsFurtherDownstreamPressureReduction()
    {
        var solver = new CompressibleSteamFlowSolver();
        var definition = CreateDefinition();

        var first = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.FromMegapascals(3d));
        var second = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.FromMegapascals(0.1d));

        Assert.True(first.IsChoked);
        Assert.True(second.IsChoked);
        Assert.Equal(definition.CriticalDownstreamToUpstreamPressureRatio, first.CriticalPressureRatio, 12);
        Assert.Equal(0.7880086767028818d, first.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(first.MassFlowRate.KilogramsPerSecond, second.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void Solve_SubcriticalFlowFallsContinuouslyTowardZeroAsBackpressureApproachesUpstreamPressure()
    {
        var solver = new CompressibleSteamFlowSolver();
        var definition = CreateDefinition();

        var lowerBackpressure = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.FromMegapascals(4d));
        var higherBackpressure = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.FromMegapascals(6d));
        var equalPressure = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            UpstreamPressure);

        Assert.False(lowerBackpressure.IsChoked);
        Assert.False(higherBackpressure.IsChoked);
        Assert.True(lowerBackpressure.MassFlowRate > higherBackpressure.MassFlowRate);
        Assert.True(higherBackpressure.MassFlowRate > MassFlowRate.Zero);
        Assert.Equal(MassFlowRate.Zero, equalPressure.MassFlowRate);
        Assert.Equal(1d, equalPressure.DownstreamToUpstreamPressureRatio, 12);
    }

    [Fact]
    public void Solve_IsContinuousAtTheCriticalPressureRatio()
    {
        var solver = new CompressibleSteamFlowSolver();
        var definition = CreateDefinition();
        var criticalDownstreamPressure = Pressure.FromPascals(
            UpstreamPressure.Pascals * definition.CriticalDownstreamToUpstreamPressureRatio);
        var justAboveCritical = Pressure.FromPascals(
            criticalDownstreamPressure.Pascals * (1d + 1e-9d));

        var choked = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            criticalDownstreamPressure);
        var subcritical = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            justAboveCritical);

        Assert.True(choked.IsChoked);
        Assert.False(subcritical.IsChoked);
        Assert.InRange(
            Math.Abs(choked.MassFlowRate.KilogramsPerSecond - subcritical.MassFlowRate.KilogramsPerSecond),
            0d,
            1e-8d);
    }

    [Fact]
    public void Solve_ScalesLinearlyWithDischargeAreaAndEffectiveOpening()
    {
        var solver = new CompressibleSteamFlowSolver();
        var baseDefinition = CreateDefinition();
        var halfAreaDefinition = new CompressibleSteamFlowDefinition(
            Area.FromSquareMillimetres(50d),
            baseDefinition.DischargeCoefficient,
            baseDefinition.SpecificGasConstant,
            baseDefinition.HeatCapacityRatio);
        var downstream = Pressure.FromMegapascals(1d);

        var full = solver.Solve(baseDefinition, UpstreamPressure, UpstreamTemperature, downstream);
        var halfArea = solver.Solve(halfAreaDefinition, UpstreamPressure, UpstreamTemperature, downstream);
        var halfOpening = solver.Solve(baseDefinition, UpstreamPressure, UpstreamTemperature, downstream, 0.5d);

        Assert.Equal(full.MassFlowRate.KilogramsPerSecond * 0.5d, halfArea.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(full.MassFlowRate.KilogramsPerSecond * 0.5d, halfOpening.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(50d, halfOpening.EffectiveThroatArea.SquareMillimetres, 12);
    }

    [Fact]
    public void Solve_OneWayContractReturnsZeroForClosedAreaOrNonPositiveDrivingPressure()
    {
        var solver = new CompressibleSteamFlowSolver();
        var definition = CreateDefinition();

        var closed = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.FromMegapascals(1d),
            effectiveAreaFraction: 0d);
        var reverseHead = solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.FromMegapascals(7d));

        Assert.Equal(MassFlowRate.Zero, closed.MassFlowRate);
        Assert.Equal(MassFlowRate.Zero, reverseHead.MassFlowRate);
        Assert.False(closed.IsChoked);
        Assert.False(reverseHead.IsChoked);
    }

    [Fact]
    public void Solve_RejectsInvalidReservoirStateAndEffectiveAreaFraction()
    {
        var solver = new CompressibleSteamFlowSolver();
        var definition = CreateDefinition();

        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Solve(
            definition,
            Pressure.Vacuum,
            UpstreamTemperature,
            Pressure.Vacuum));
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Solve(
            definition,
            UpstreamPressure,
            Temperature.AbsoluteZero,
            Pressure.Vacuum));
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.Vacuum,
            -0.01d));
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Solve(
            definition,
            UpstreamPressure,
            UpstreamTemperature,
            Pressure.Vacuum,
            1.01d));
    }

    private static CompressibleSteamFlowDefinition CreateDefinition()
        => new(
            Area.FromSquareMillimetres(100d),
            dischargeCoefficient: 0.95d,
            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
            heatCapacityRatio: 1.3d);
}
