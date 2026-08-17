using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10.9.4 generation-ready desktop seed. Version 2 intentionally leaves the historical M9.7 v1 seed untouched for
/// replay/archive compatibility while providing matched primary circulation, a half-full saturated steam drum and solid-to-coolant heat transfer, a
/// thermodynamically pressurized steam path, matched low-load steam/condensate/feedwater capacity, explicit condenser
/// headroom above the 24.5 MW surface-transfer design point and bumpless controller biases suitable for sustained turbine-generator-grid operation.
/// </summary>
public sealed class DesktopSustainedGenerationInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation GenerationReadySeed = NeutronPopulation.FromRelative(0.30d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);
    private const double LoadedControlValveBiasPercent = 28d;

    // D.3.2 Hotfix 3: retain the Hotfix 2 pressure-grade correction, then remove the actual upstream
    // bottleneck identified by local evidence. The loaded desktop main-steam line is sized at 850
    // Pa·s²/kg² so its initial capacity no longer masks control-valve authority.
    // The 28% seed bias then remains an admission-authority measurement point instead of being
    // retuned around the upstream bottleneck.
    private const double LoadedStopOutletSteamTemperatureCelsius = 276.755d;
    private const double LoadedMainSteamLineResistancePascalSecondsSquaredPerKilogramSquared = 850d;

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 2);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v2",
        "M10.9.4 generation-ready desktop seed preserving the v1 replay baseline while establishing matched primary circulation, a half-full saturated steam drum with a coherent level-control setpoint, conservative solid-to-coolant heat transfer, a continuously pressure-graded staged steam path, matched admission/condenser/feedwater hydraulics, a generation-scale condenser steam-space inventory, 40 MW installed cooling-boundary headroom over the unchanged 1.225 MW/K surface law, pressure-resolved saturated-liquid condensate energy, bumpless control biases and finite heat rejection for sustained low-load electrical export.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => CreateRuntimeEngine(includeEvidenceDerivedElectricalProtections: true);

    internal static IControlRoomRuntimeEngine CreateElectricalProtectionEvidenceRuntimeEngine()
        => CreateRuntimeEngine(includeEvidenceDerivedElectricalProtections: false);

    /// <summary>
    /// Phase H audit-only factory seam. It preserves the production 10 ms default and the same 20 ms deterministic seed
    /// preconditioning duration while allowing explicitly requested divisor timesteps for convergence evidence.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateNumericalStiffnessEvidenceRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Numerical-stiffness evidence timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false);
    }

    private static IControlRoomRuntimeEngine CreateRuntimeEngine(
        bool includeEvidenceDerivedElectricalProtections,
        TimeSpan? runtimeStep = null,
        int deterministicSeedStepCount = 2,
        bool useHybridSemiImplicitHydraulics = false)
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            GenerationReadySeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 280d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: true,
            initialRequestedElectricalPowerMegawatts: 5d,
            initialCondenserCoolingPowerMegawatts: 40d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialControlValvePercentOpen: LoadedControlValveBiasPercent,
            initialHeaderSteamTemperatureCelsius: 278.5d,
            initialStopOutletSteamTemperatureCelsius: LoadedStopOutletSteamTemperatureCelsius,
            initialControlOutletSteamTemperatureCelsius: 249.5d,
            initialTurbineInletSteamTemperatureCelsius: 246.5d,
            primaryCirculationPipeResistancePascalSecondsSquaredPerKilogramSquared: 25d,
            mainCirculationPumpResistancePascalSecondsSquaredPerKilogramSquared: 25d,
            mainSteamLineResistancePascalSecondsSquaredPerKilogramSquared: LoadedMainSteamLineResistancePascalSecondsSquaredPerKilogramSquared,
            turbineAdmissionValveResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            speedControllerIntegralGainPerSecond: 0.02d,
            speedControllerDerivativeGainSeconds: 0.2d,
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
            generatorMaximumElectricalPowerMegawatts: 10d,
            generatorGridPowerFlowMode: NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridPowerFlowMode.Bidirectional,
            includeEvidenceDerivedElectricalProtections: includeEvidenceDerivedElectricalProtections,
            includeMainSteamHeaderRelief: true,
            includeTurbineBypass: true,
            useEnthalpyTransportForPassivePipesAndValves: true,
            useEnthalpyTransportForRemainingNonTurbinePaths: true,
            useEnthalpyTransportForTurbineExpansion: true,
            useHybridSemiImplicitHydraulics: useHybridSemiImplicitHydraulics,
            deterministicSeedStepCount: deterministicSeedStepCount,
            runtimeStep: runtimeStep);
}
