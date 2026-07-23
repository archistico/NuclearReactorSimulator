using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10.9.4 generation-ready desktop seed. Version 2 intentionally leaves the historical M9.7 v1 seed untouched for
/// replay/archive compatibility while providing a thermodynamically pressurized steam path, matched low-load steam/condensate/feedwater
/// capacity and bumpless controller biases suitable for sustained turbine-generator-grid operation.
/// </summary>
public sealed class DesktopSustainedGenerationInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation GenerationReadySeed = NeutronPopulation.FromRelative(0.30d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 2);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v2",
        "M10.9.4 generation-ready desktop seed preserving the v1 replay baseline while establishing a continuously pressure-graded staged steam path, matched admission/condenser/feedwater hydraulics and a generation-scale condenser steam-space inventory, bumpless control biases and finite heat rejection for sustained low-load electrical export.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            GenerationReadySeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 280d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: true,
            initialRequestedElectricalPowerMegawatts: 5d,
            initialCondenserCoolingPowerMegawatts: 24.5d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialControlValvePercentOpen: 46d,
            initialHeaderSteamTemperatureCelsius: 275d,
            initialStopOutletSteamTemperatureCelsius: 269.5d,
            initialControlOutletSteamTemperatureCelsius: 253d,
            initialTurbineInletSteamTemperatureCelsius: 246d,
            mainSteamLineResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            turbineAdmissionValveResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            speedControllerIntegralGainPerSecond: 0.02d,
            speedControllerDerivativeGainSeconds: 0.2d,
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
            steamDrumLiquidRecirculationMode: SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            deterministicSeedStepCount: 2);
}
