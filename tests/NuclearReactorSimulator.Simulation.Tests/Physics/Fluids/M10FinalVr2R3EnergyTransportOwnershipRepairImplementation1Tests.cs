using System.Runtime.CompilerServices;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class M10FinalVr2R3EnergyTransportOwnershipRepairImplementation1Tests
{
    private static readonly Pressure FrozenDrumPressure = Pressure.FromPascals(6_416_459.281680372d);
    private static readonly Temperature FrozenDrumTemperature = Temperature.FromKelvins(553.15d);
    private const double FrozenIf97LiquidTransportJoulesPerKilogram = 1_236_671.0007034552d;
    private const double FrozenRawMode2SuctionTransportJoulesPerKilogram = 1_236_671.000850998d;

    [Fact]
    public void HistoricalModes_TransportPropertiesRemainBitIdenticalToHistoricalForwardSaturation()
    {
        AssertHistoricalMode(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology);
        AssertHistoricalMode(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
    }

    [Fact]
    public void ReferenceConsistentMode2_TransportPropertiesMatchFrozenReferenceAndAllocateZeroAfterWarmup()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var provider = model.PhaseTransportPropertyProvider;

        var properties = ResolveTransportProperties(provider);
        for (var iteration = 0; iteration < 20_000; iteration++)
        {
            properties = ResolveTransportProperties(provider);
        }

        // Warm the allocation-measurement helper itself before taking authoritative samples.
        // GC.GetAllocatedBytesForCurrentThread() measures all managed allocations on the thread,
        // including fixed one-shot/runtime measurement overhead that is not proportional to lookup count.
        // The contract is zero steady-state allocation PER LOOKUP, so qualify the marginal allocation slope:
        // increasing the lookup count from 1k -> 10k -> 20k must add exactly zero managed bytes.
        _ = MeasureLookupAllocations(provider, 1_000, out properties);

        var payloadLoadsAfterWarmup = ReferenceConsistentTabulatedInverseResolver.PayloadLoadCount;
        var allocated1k = MeasureLookupAllocations(provider, 1_000, out properties);
        var allocated10k = MeasureLookupAllocations(provider, 10_000, out properties);
        var allocated20k = MeasureLookupAllocations(provider, 20_000, out properties);

        var liquidTransport = FluidEnergyTransport.ResolveSpecificEnthalpy(
            properties.SaturatedLiquidInternalEnergy,
            FrozenDrumPressure,
            properties.SaturatedLiquidDensity);

        Assert.True(
            Math.Abs(liquidTransport.JoulesPerKilogram - FrozenIf97LiquidTransportJoulesPerKilogram) <= 0.001d,
            $"Mode-2 transport differs from frozen IF97 by {liquidTransport.JoulesPerKilogram - FrozenIf97LiquidTransportJoulesPerKilogram:R} J/kg.");
        Assert.True(
            Math.Abs(liquidTransport.JoulesPerKilogram - FrozenRawMode2SuctionTransportJoulesPerKilogram) <= 0.001d,
            $"Mode-2 transport differs from frozen raw suction transport by {liquidTransport.JoulesPerKilogram - FrozenRawMode2SuctionTransportJoulesPerKilogram:R} J/kg.");
        Assert.InRange(allocated1k, 0L, 256L);
        Assert.Equal(0L, allocated10k - allocated1k);
        Assert.Equal(0L, allocated20k - allocated10k);
        Assert.Equal(payloadLoadsAfterWarmup, ReferenceConsistentTabulatedInverseResolver.PayloadLoadCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimeResourceIoCount);
        Assert.Equal(0, ReferenceConsistentTabulatedInverseResolver.ResolveTimePayloadDecodeCount);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureLookupAllocations(
        IWaterSteamPhaseTransportPropertyProvider provider,
        int iterations,
        out WaterSteamPhaseTransportProperties properties)
    {
        properties = default;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            properties = ResolveTransportProperties(provider);
        }
        return GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WaterSteamPhaseTransportProperties ResolveTransportProperties(
        IWaterSteamPhaseTransportPropertyProvider provider)
        => provider.GetSaturatedPhaseTransportProperties(FrozenDrumPressure, FrozenDrumTemperature);

    private static void AssertHistoricalMode(WaterSteamThermodynamicClosureMode mode)
    {
        var model = new SimplifiedWaterSteamThermodynamicModel(mode);
        var forward = model.GetSaturationProperties(FrozenDrumTemperature);
        var transport = model.PhaseTransportPropertyProvider.GetSaturatedPhaseTransportProperties(
            FrozenDrumPressure,
            FrozenDrumTemperature);

        AssertSameBits(
            forward.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            transport.SaturatedLiquidDensity.KilogramsPerCubicMetre);
        AssertSameBits(
            forward.SaturatedVaporDensity.KilogramsPerCubicMetre,
            transport.SaturatedVaporDensity.KilogramsPerCubicMetre);
        AssertSameBits(
            forward.SaturatedLiquidInternalEnergy.JoulesPerKilogram,
            transport.SaturatedLiquidInternalEnergy.JoulesPerKilogram);
        AssertSameBits(
            forward.SaturatedVaporInternalEnergy.JoulesPerKilogram,
            transport.SaturatedVaporInternalEnergy.JoulesPerKilogram);
    }

    private static void AssertSameBits(double expected, double actual)
        => Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
}
