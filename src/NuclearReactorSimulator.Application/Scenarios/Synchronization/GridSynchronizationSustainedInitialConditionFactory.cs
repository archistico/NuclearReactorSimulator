using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

/// <summary>
/// M10.9.4 opt-in generation-ready synchronization seed used by long gameplay/system acceptance. The historical M7.5
/// v1 seed remains unchanged. This v2 profile retains zero initial generator load while using matched primary circulation,
/// a half-full saturated steam drum and solid-to-coolant heat transfer, a bumpless spinning-reserve governor bias, a staged
/// pressurized steam inventory and matched steam/condensation/feedwater capacity with condenser headroom for deliberate
/// post-synchronization loading.
/// </summary>
public sealed class GridSynchronizationSustainedInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation GenerationReadySeed = NeutronPopulation.FromRelative(0.30d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public static InitialConditionReference Reference { get; } = new("pre-synchronization-grid-loading", 2);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Pre-Synchronization / Sustained Initial Loading v2",
        "M10.9.4 long-gameplay synchronization seed preserving M7.5 v1 while providing matched primary circulation, a half-full saturated steam drum with a coherent level-control setpoint and conservative solid-to-coolant heat transfer, a continuously pressure-graded staged steam path and matched admission/condenser/feedwater hydraulics, a generation-scale condenser steam-space inventory and 40 MW installed cooling-boundary headroom over the unchanged 1.225 MW/K surface law, pressure-resolved saturated-liquid condensate energy for post-synchronization load acceptance.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => CreateRuntimeEngine(includeEvidenceDerivedElectricalProtections: true);

    internal static IControlRoomRuntimeEngine CreateElectricalProtectionEvidenceRuntimeEngine()
        => CreateRuntimeEngine(includeEvidenceDerivedElectricalProtections: false);

    private static IControlRoomRuntimeEngine CreateRuntimeEngine(bool includeEvidenceDerivedElectricalProtections)
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            GenerationReadySeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 280d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: false,
            initialRequestedElectricalPowerMegawatts: 0d,
            initialCondenserCoolingPowerMegawatts: 40d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialControlValvePercentOpen: 28d,
            initialHeaderSteamTemperatureCelsius: 278.5d,
            initialStopOutletSteamTemperatureCelsius: 277d,
            initialControlOutletSteamTemperatureCelsius: 249.5d,
            initialTurbineInletSteamTemperatureCelsius: 246.5d,
            primaryCirculationPipeResistancePascalSecondsSquaredPerKilogramSquared: 25d,
            mainCirculationPumpResistancePascalSecondsSquaredPerKilogramSquared: 25d,
            mainSteamLineResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            turbineAdmissionValveResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            speedControllerProportionalGain: 0.5d,
            speedControllerIntegralGainPerSecond: 0.02d,
            hotwellControllerProportionalGain: -0.01d,
            includeTurbineShaftPowerInstrumentation: true,
            maximumCondenserMassFlowRateKilogramsPerSecond: 20d,
            condenserInstalledHeatRejectionCapacityMegawatts: 40d,
            condenserOverallHeatTransferConductanceMegawattsPerKelvin: 1.225d,
            condenserCoolingWaterTemperatureCelsius: 20d,
            usePressureResolvedCondenserCondensateEnergy: true,
            secondaryPumpResistancePascalSecondsSquaredPerKilogramSquared: 500d,
            initialCondensatePumpPercent: 42d,
            initialFeedwaterPumpPercent: 97d,
            levelControllerIntegralGainPerSecond: 0.001d,
            hotwellControllerIntegralGainPerSecond: -0.000001d,
            exhaustSteamSpaceVolumeCubicMetres: 1_000d,
            pressurizedSteamPathNodeVolumeCubicMetres: 100d,
            turbineExpansionResistancePascalSecondsSquaredPerKilogramSquared: 21_400d,
            useThermodynamicTurbineWork: true,
            turbineStageEfficiencyPercent: 86d,
            generatorMaximumSynchronizingCorrectionPowerMegawatts: 0.5d,
            generatorFrequencyDampingPowerAtOneHertzSlipMegawatts: 2d,
            secondaryPumpsHaveDischargeCheckValves: true,
            includeEnhancedSecondaryProtections: true,
            secondaryValveTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d),
            turbineStopValveTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d),
            secondaryPumpTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.25d),
            governorFullLoadSpeedReferenceRiseRpm: 1.5d,
            steamDrumLiquidRecirculationMode: SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            steamDrumSteamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100d,
            includeCoreThermalCoupling: true,
            primaryOperationalFlowDisplayLagSeconds: 0.5d,
            initialSteamDrumLiquidLevelFraction: 0.5d,
            useVaporFractionLimitedTurbineAdmission: true,
            turbineRotorRatedSpeedMechanicalLossMegawatts: 0.5d,
            deterministicSeedStepCount: 2,
            generatorMaximumElectricalPowerMegawatts: 10d,
            generatorGridPowerFlowMode: NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridPowerFlowMode.Bidirectional,
            includeEvidenceDerivedElectricalProtections: includeEvidenceDerivedElectricalProtections);
}
