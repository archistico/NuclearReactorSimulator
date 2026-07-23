using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

/// <summary>
/// M10.9.4 opt-in generation-ready synchronization seed used by long gameplay/system acceptance. The historical M7.5
/// v1 seed remains unchanged. This v2 profile retains zero initial generator load while using a bumpless spinning-reserve governor bias,
/// a staged pressurized steam inventory and matched steam/condensation/feedwater capacity for deliberate post-synchronization loading.
/// </summary>
public sealed class GridSynchronizationSustainedInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation GenerationReadySeed = NeutronPopulation.FromRelative(0.30d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public static InitialConditionReference Reference { get; } = new("pre-synchronization-grid-loading", 2);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Pre-Synchronization / Sustained Initial Loading v2",
        "M10.9.4 long-gameplay synchronization seed preserving M7.5 v1 while providing a continuously pressure-graded staged steam path and matched admission/condenser/feedwater hydraulics and a generation-scale condenser steam-space inventory for post-synchronization load acceptance.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            GenerationReadySeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 280d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: false,
            initialRequestedElectricalPowerMegawatts: 0d,
            initialCondenserCoolingPowerMegawatts: 24.5d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialControlValvePercentOpen: 46d,
            initialHeaderSteamTemperatureCelsius: 275d,
            initialStopOutletSteamTemperatureCelsius: 269.5d,
            initialControlOutletSteamTemperatureCelsius: 253d,
            initialTurbineInletSteamTemperatureCelsius: 246d,
            mainSteamLineResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            turbineAdmissionValveResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            speedControllerProportionalGain: 0.5d,
            speedControllerIntegralGainPerSecond: 0.02d,
            hotwellControllerProportionalGain: -0.01d,
            includeTurbineShaftPowerInstrumentation: true,
            maximumCondenserMassFlowRateKilogramsPerSecond: 15d,
            condenserOverallHeatTransferConductanceMegawattsPerKelvin: 1.225d,
            condenserCoolingWaterTemperatureCelsius: 20d,
            secondaryPumpResistancePascalSecondsSquaredPerKilogramSquared: 500d,
            initialCondensatePumpPercent: 42d,
            initialFeedwaterPumpPercent: 97d,
            levelControllerIntegralGainPerSecond: 0.001d,
            hotwellControllerIntegralGainPerSecond: -0.000001d,
            exhaustSteamSpaceVolumeCubicMetres: 1_000d,
            turbineExpansionResistancePascalSecondsSquaredPerKilogramSquared: 21_400d,
            generatorMaximumSynchronizingCorrectionPowerMegawatts: 10d,
            generatorFrequencyDampingPowerAtOneHertzSlipMegawatts: 10d,
            secondaryPumpsHaveDischargeCheckValves: true,
            includeEnhancedSecondaryProtections: true,
            steamDrumLiquidRecirculationMode: SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            deterministicSeedStepCount: 2);
}
