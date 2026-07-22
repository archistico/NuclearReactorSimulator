using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

namespace NuclearReactorSimulator.Application.Scenarios.Xenon;

/// <summary>
/// Version-pinned reduced M2.8 poison configuration used by the built-in M9.3 v1 initial conditions. These are
/// educational configuration-relative coefficients, not plant-specific isotope constants or historical calibration data.
/// </summary>
internal static class AdvancedXenonModelConfiguration
{
    public static IodineXenonDefinition Definition { get; } = new(
        "core-poison-m93-v1",
        Power.FromMegawatts(100d),
        PoisonProductionRate.FromRelativePerSecond(0.02d),
        PoisonProductionRate.FromRelativePerSecond(0.005d),
        DecayConstant.FromPerSecond(0.02d),
        DecayConstant.FromPerSecond(0.01d),
        XenonBurnupCoefficient.FromPerSecondPerRelativeNeutronPopulation(0.02d),
        XenonReactivityCoefficient.FromPcmPerRelativeInventory(-40d));
}
