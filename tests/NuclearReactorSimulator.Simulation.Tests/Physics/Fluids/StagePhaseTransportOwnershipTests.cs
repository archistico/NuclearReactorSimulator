using System.Runtime.CompilerServices;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class StagePhaseTransportOwnershipTests
{
    [Theory]
    [InlineData(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology)]
    [InlineData(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain)]
    public void PressureLookup_PreservesEveryHistoricalPropertyBit(WaterSteamThermodynamicClosureMode mode)
    {
        var model = new SimplifiedWaterSteamThermodynamicModel(mode);
        foreach (var pascals in new[] { 1_000d, 8_438.34482487792d, 101_325d, 1_000_000d, 3_816_277.8025847385d, 6_416_459.281680372d, 15_000_000d })
        {
            var pressure = Pressure.FromPascals(pascals);
            var historical = model.GetSaturationProperties(pressure);
            var actual = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(pressure);
            SameBits(historical.SaturatedLiquidDensity.KilogramsPerCubicMetre, actual.SaturatedLiquidDensity.KilogramsPerCubicMetre);
            SameBits(historical.SaturatedVaporDensity.KilogramsPerCubicMetre, actual.SaturatedVaporDensity.KilogramsPerCubicMetre);
            SameBits(historical.SaturatedLiquidInternalEnergy.JoulesPerKilogram, actual.SaturatedLiquidInternalEnergy.JoulesPerKilogram);
            SameBits(historical.SaturatedVaporInternalEnergy.JoulesPerKilogram, actual.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        }
    }

    [Fact]
    public void C4PressureLookup_UsesExistingClosureRegardlessOfNodeTemperature()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        foreach (var pascals in new[] { 1_000d, 8_438.34482487792d, 101_325d, 3_816_277.8025847385d, 6_416_459.281680372d, 15_000_000d })
        {
            var pressure = Pressure.FromPascals(pascals);
            var expected = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(pressure, Temperature.FromKelvins(300d));
            Assert.Equal(expected, model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(pressure));
        }
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(30_000_000d)]
    public void C4PressureLookup_RejectsOutsidePayloadWithoutHistoricalFallback(double pascals)
    {
        var model = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        Assert.Throws<ArgumentOutOfRangeException>(() => model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(Pressure.FromPascals(pascals)));
    }

    [Fact]
    public void C4PressureLookup_HasZeroMarginalAllocationAndNoResourceIo()
    {
        var provider = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain).PhaseTransportPropertyProvider;
        _ = Measure(provider, 20_000);
        _ = Measure(provider, 1_000);
        var loads = ReferenceConsistentTabulatedInverseResolver.PayloadLoadCount;
        var small = Measure(provider, 1_000);
        var large = Measure(provider, 20_000);
        Assert.InRange(small, 0L, 256L);
        Assert.Equal(small, large);
        Assert.Equal(loads, ReferenceConsistentTabulatedInverseResolver.PayloadLoadCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimeResourceIoCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimePayloadDecodeCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(IWaterSteamPhaseTransportPropertyProvider provider, int count)
    {
        var pressure = Pressure.FromPascals(3_816_277.8025847385d);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < count; i++)
        {
            _ = provider.GetSaturatedPhaseTransportProperties(pressure);
        }
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void SameBits(double expected, double actual)
        => Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
}
