using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

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

    /// <summary>
    /// H.21 audit-only opt-in seam. It runs the exact current-v2 desktop configuration with the four-node
    /// branch-continuity sidecar wired through PlantNetworkOrchestrator while production candidate ownership remains explicit.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateFourNodeShadowIntegrationEvidenceRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Four-node shadow-integration evidence timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: true);
    }

    /// <summary>
    /// H.29 production-activation-candidate composition. It runs the unchanged current-v2 desktop plant with the
    /// H.22 corrected-candidate commit seam enabled from deterministic seed preconditioning onward. The numerical
    /// controls remain those already qualified through H.28/H.24 Requalification 1. Standard v2 factories remain explicit.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateFourNodeCorrectedCommitProductionCandidateRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Four-node corrected-commit production-candidate timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: true);
    }

    /// <summary>
    /// Historical H.22-H.28 evidence seam retained so previous audit code and fingerprints remain source-compatible.
    /// It delegates to the H.29 production-candidate composition without changing the numerical runtime definition.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(TimeSpan runtimeStep)
        => CreateFourNodeCorrectedCommitProductionCandidateRuntimeEngine(runtimeStep);

    /// <summary>
    /// I.5 REV1 repaired exact-version composition candidate. It preserves the validated v2 physical seed, 10 ms
    /// timestep and H.22 corrected-commit ownership while selecting the validated correlation-consistent water/steam
    /// inverse-domain closure. Historical exact v2/v3 factories remain unchanged.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateRepairedCorrectedCommitProductionCandidateRuntimeEngine(TimeSpan runtimeStep)
        => CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(runtimeStep, useFourNodeCorrectedCommit: true);

    /// <summary>
    /// I.5 REV1 thermodynamic-repair evidence seam. It preserves the exact v2 physical seed and 10 ms composition
    /// while opting only the water/steam inverse-domain closure into the correlation-consistent candidate. The
    /// caller selects explicit or H.29 corrected-commit hydraulics independently so the frozen operational journey
    /// can demonstrate that the thermodynamic repair, rather than a hydraulic-policy change, removes the gap.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(
        TimeSpan runtimeStep,
        bool useFourNodeCorrectedCommit)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Thermodynamic inverse-domain repair evidence timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: useFourNodeCorrectedCommit,
            thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
    }

    /// <summary>
    /// M10 final LR-H1 reference operating-point candidate. This is a distinct exact-version seed seam only:
    /// exact v4 remains immutable and the authoritative selector is not switched here.
    ///
    /// The Diagnostic 3 target 260 kg/s loop point was an intentionally incomplete pressure-grade probe.
    /// Diagnostic 5 plus source-level balance review later showed that the non-stationary seed relied on a
    /// large suction-to-drum pressure separation; exact-v5 already included pump path/internal resistance.
    /// Exact-v5 is retained unchanged as failed diagnostic evidence.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateReferenceOperatingPointCandidateRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Reference operating-point candidate timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: true,
            thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain,
            initialPrimarySuctionCompressionFraction: 0.002618181818181818d,
            initialPrimaryPressureCompressionFraction: 0.0015363636363636363d,
            initialPrimaryOutletSaturationPressureMegapascals: 8.106459281680372d,
            initialPrimaryOutletVaporQualityFraction: 0.035881742881444335d,
            initialFuelTemperatureCelsiusOverride: 316.93357730105606d,
            initialStructureTemperatureCelsiusOverride: 301.93357730105606d);
    }

    /// <summary>
    /// M10 final LR-H1 exact-v6 analytical whole-cycle equilibrium candidate. Unlike exact-v5, which was an
    /// intentionally diagnostic 260 kg/s probe, this seed closes the actual authored hydraulic resistances,
    /// passive enthalpy paths, turbine/generator mechanical demand, condenser UA balance and secondary-pump
    /// balances simultaneously. Exact-v4 remains the production selector and exact-v5 remains frozen failed evidence.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreateWholeCycleEquilibriumCandidateRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Whole-cycle equilibrium candidate timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        var fluidNodeSeeds = new OperationalFluidNodeSeed[]
        {
            // Primary loop: 1 MPa rated pump head closes against pump path+internal+channel+return
            // resistance = 100 Pa*s^2/kg^2, hence q = 100 kg/s. Drum/suction remain at 280 C saturation.
            new OperationalFluidNodeSeed.SaturatedMixture("suction", 6.416459281680372d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("pressure", 280.1582998275795d, 0.0002203018767873456d),
            new OperationalFluidNodeSeed.SaturatedMixture("outlet", 6.666459281680372d, 0.21186790020483265d),

            // Secondary steam path: 13.0280018984 kg/s supplies 5 MWe / 98% generator efficiency plus
            // the unchanged 0.5 MW rotor loss at 500 kJ/kg nominal work and 86% turbine efficiency.
            // Each passive node is seeded at the same steam-source specific enthalpy.
            new OperationalFluidNodeSeed.SaturatedMixture("steam", 6.399486398333812d, 0.9997985062240056d),
            new OperationalFluidNodeSeed.SaturatedMixture("header", 6.2552168898880565d, 0.9981120495093272d),
            new OperationalFluidNodeSeed.SaturatedMixture("stop-out", 6.085488056422462d, 0.9961878030705963d),
            new OperationalFluidNodeSeed.SaturatedMixture("control-out", 3.8101893137696408d, 0.9766316994558023d),
            new OperationalFluidNodeSeed.SaturatedMixture("turbine-inlet", 3.6404604803040463d, 0.975662912566689d),

            // Exhaust temperature is the root of q*(h_exhaust-h_condensate)=UA*(T_exhaust-T_cooling),
            // with the turbine extracting 430 kJ/kg. Hotwell is the matching saturated-liquid state.
            new OperationalFluidNodeSeed.SaturatedMixture("exhaust", 0.008263444140323916d, 0.857029927254011d),
            new OperationalFluidNodeSeed.SaturatedMixture("hotwell", 0.008263444140323916d, 0d),

            // The unchanged 42% condensate-pump bias lifts the 42.1258 C condensate only enough to balance
            // its 1000 Pa*s^2/kg^2 total path resistance at the same 13.028 kg/s throughput.
            new OperationalFluidNodeSeed.SubcooledLiquid("feedwater-inventory", 42.16659807598285d, 0.000003024302581887423d),
        };

        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: true,
            thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain,
            initialFuelTemperatureCelsiusOverride: 305.28430322032125d,
            initialStructureTemperatureCelsiusOverride: 289.0421762844392d,
            initialNeutronPopulationOverride: NeutronPopulation.FromRelative(0.3248425387176408d),
            initialControlValvePercentOpenOverride: 27.312320479840385d,
            initialCondensatePumpPercentOverride: 42d,
            initialFeedwaterPumpPercentOverride: 96.88913771103281d,
            initialFluidNodeSeeds: fluidNodeSeeds);
    }

    internal static IControlRoomRuntimeEngine CreateGridDroopIntegralReferenceCandidateRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Grid-droop integral-reference candidate timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        var fluidNodeSeeds = new OperationalFluidNodeSeed[]
        {
            // Primary loop: 1 MPa rated pump head closes against pump path+internal+channel+return
            // resistance = 100 Pa*s^2/kg^2, hence q = 100 kg/s. Drum/suction remain at 280 C saturation.
            new OperationalFluidNodeSeed.SaturatedMixture("suction", 6.416459281680372d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("pressure", 280.1582998275795d, 0.0002203018767873456d),
            new OperationalFluidNodeSeed.SaturatedMixture("outlet", 6.666459281680372d, 0.21186790020483265d),

            // Secondary steam path: 13.0280018984 kg/s supplies 5 MWe / 98% generator efficiency plus
            // the unchanged 0.5 MW rotor loss at 500 kJ/kg nominal work and 86% turbine efficiency.
            // Each passive node is seeded at the same steam-source specific enthalpy.
            new OperationalFluidNodeSeed.SaturatedMixture("steam", 6.399486398333812d, 0.9997985062240056d),
            new OperationalFluidNodeSeed.SaturatedMixture("header", 6.2552168898880565d, 0.9981120495093272d),
            new OperationalFluidNodeSeed.SaturatedMixture("stop-out", 6.085488056422462d, 0.9961878030705963d),
            new OperationalFluidNodeSeed.SaturatedMixture("control-out", 3.8101893137696408d, 0.9766316994558023d),
            new OperationalFluidNodeSeed.SaturatedMixture("turbine-inlet", 3.6404604803040463d, 0.975662912566689d),

            // Exhaust temperature is the root of q*(h_exhaust-h_condensate)=UA*(T_exhaust-T_cooling),
            // with the turbine extracting 430 kJ/kg. Hotwell is the matching saturated-liquid state.
            new OperationalFluidNodeSeed.SaturatedMixture("exhaust", 0.008263444140323916d, 0.857029927254011d),
            new OperationalFluidNodeSeed.SaturatedMixture("hotwell", 0.008263444140323916d, 0d),

            // The unchanged 42% condensate-pump bias lifts the 42.1258 C condensate only enough to balance
            // its 1000 Pa*s^2/kg^2 total path resistance at the same 13.028 kg/s throughput.
            new OperationalFluidNodeSeed.SubcooledLiquid("feedwater-inventory", 42.16659807598285d, 0.000003024302581887423d),
        };

        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: true,
            thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain,
            initialFuelTemperatureCelsiusOverride: 305.28430322032125d,
            initialStructureTemperatureCelsiusOverride: 289.0421762844392d,
            initialNeutronPopulationOverride: NeutronPopulation.FromRelative(0.3248425387176408d),
            initialControlValvePercentOpenOverride: 27.312320479840385d,
            initialCondensatePumpPercentOverride: 42d,
            initialFeedwaterPumpPercentOverride: 96.88913771103281d,
            initialFluidNodeSeeds: fluidNodeSeeds,
            governorIntegralReferenceMode: TurbineGovernorIntegralReferenceMode.SynchronousSpeedWhenParalleled);
    }

    internal static IControlRoomRuntimeEngine CreateMoistureDrainCandidateRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Moisture-drain candidate timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        var fluidNodeSeeds = new OperationalFluidNodeSeed[]
        {
            // Primary loop: 1 MPa rated pump head closes against pump path+internal+channel+return
            // resistance = 100 Pa*s^2/kg^2, hence q = 100 kg/s. Drum/suction remain at 280 C saturation.
            new OperationalFluidNodeSeed.SaturatedMixture("suction", 6.416459281680372d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("pressure", 280.1582998275795d, 0.0002203018767873456d),
            new OperationalFluidNodeSeed.SaturatedMixture("outlet", 6.666459281680372d, 0.21186790020483265d),

            // Secondary steam path: 13.0280018984 kg/s supplies 5 MWe / 98% generator efficiency plus
            // the unchanged 0.5 MW rotor loss at 500 kJ/kg nominal work and 86% turbine efficiency.
            // Each passive node is seeded at the same steam-source specific enthalpy.
            new OperationalFluidNodeSeed.SaturatedMixture("steam", 6.399486398333812d, 0.9997985062240056d),
            new OperationalFluidNodeSeed.SaturatedMixture("header", 6.2552168898880565d, 0.9981120495093272d),
            new OperationalFluidNodeSeed.SaturatedMixture("stop-out", 6.085488056422462d, 0.9961878030705963d),
            new OperationalFluidNodeSeed.SaturatedMixture("control-out", 3.8101893137696408d, 0.9766316994558023d),
            new OperationalFluidNodeSeed.SaturatedMixture("turbine-inlet", 3.6404604803040463d, 0.975662912566689d),

            // Exhaust temperature is the root of q*(h_exhaust-h_condensate)=UA*(T_exhaust-T_cooling),
            // with the turbine extracting 430 kJ/kg. Hotwell is the matching saturated-liquid state.
            new OperationalFluidNodeSeed.SaturatedMixture("exhaust", 0.008263444140323916d, 0.857029927254011d),
            new OperationalFluidNodeSeed.SaturatedMixture("hotwell", 0.008263444140323916d, 0d),

            // The unchanged 42% condensate-pump bias lifts the 42.1258 C condensate only enough to balance
            // its 1000 Pa*s^2/kg^2 total path resistance at the same 13.028 kg/s throughput.
            new OperationalFluidNodeSeed.SubcooledLiquid("feedwater-inventory", 42.16659807598285d, 0.000003024302581887423d),
        };

        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: true,
            thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain,
            initialFuelTemperatureCelsiusOverride: 305.28430322032125d,
            initialStructureTemperatureCelsiusOverride: 289.0421762844392d,
            initialNeutronPopulationOverride: NeutronPopulation.FromRelative(0.3248425387176408d),
            initialControlValvePercentOpenOverride: 27.312320479840385d,
            initialCondensatePumpPercentOverride: 42d,
            initialFeedwaterPumpPercentOverride: 96.88913771103281d,
            initialFluidNodeSeeds: fluidNodeSeeds,
            governorIntegralReferenceMode: TurbineGovernorIntegralReferenceMode.SynchronousSpeedWhenParalleled,
            turbineAdmissionPhasePolicyOverride: TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain,
            turbineMoistureDrainNodeId: "hotwell");
    }

    /// <summary>
    /// M10 final LR-H1 exact-v9 post-moisture whole-cycle equilibrium candidate. Diagnostic 10 Hotfix 1
    /// validated the explicit moisture-drain ownership but showed that exact-v8 still inherited the pre-drain
    /// secondary mass/energy root. This candidate keeps the exact-v8 governor/admission semantics unchanged and
    /// recomputes only the authored operating point with vapor flow, drain flow, condenser UA, secondary pumps and
    /// full external-energy closure solved together. Exact-v4 remains the production selector.
    /// </summary>
    internal static IControlRoomRuntimeEngine CreatePostMoistureEquilibriumCandidateRuntimeEngine(TimeSpan runtimeStep)
    {
        if (runtimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeStep));
        }

        var seedDuration = TimeSpan.FromMilliseconds(20d);
        if (seedDuration.Ticks % runtimeStep.Ticks != 0)
        {
            throw new ArgumentException(
                "Post-moisture equilibrium candidate timestep must divide the versioned 20 ms seed preconditioning duration exactly.",
                nameof(runtimeStep));
        }

        var seedStepCount = checked((int)(seedDuration.Ticks / runtimeStep.Ticks));
        var fluidNodeSeeds = new OperationalFluidNodeSeed[]
        {
            // Primary loop stays on the already-derived 100 kg/s hydraulic root. Only the outlet quality and solid
            // temperatures move with the revised full-cycle fission root.
            new OperationalFluidNodeSeed.SaturatedMixture("suction", 6.416459281680372d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("pressure", 280.1582998275795d, 0.0002203018767873456d),
            new OperationalFluidNodeSeed.SaturatedMixture("outlet", 6.666459281680372d, 0.21514191259171503d),

            // Post-moisture secondary root: 13.0280018984 kg/s vapor is required for 5 MWe while the phase-separated
            // inlet quality makes total admission 13.3392371354 kg/s and explicit moisture drain 0.3112352370 kg/s.
            // Passive steam-path nodes preserve the saturated-vapor drum-source enthalpy exactly.
            new OperationalFluidNodeSeed.SaturatedMixture("steam", 6.398665756944915d, 0.99978878048067332d),
            new OperationalFluidNodeSeed.SaturatedMixture("header", 6.2474207966935325d, 0.99802224982943177d),
            new OperationalFluidNodeSeed.SaturatedMixture("stop-out", 6.0694855493389648d, 0.99600970115709719d),
            new OperationalFluidNodeSeed.SaturatedMixture("control-out", 3.9941878857133641d, 0.97776527205630726d),
            new OperationalFluidNodeSeed.SaturatedMixture("turbine-inlet", 3.8162526383587956d, 0.97666768842836382d),

            // The condenser root is solved on vapor flow only. The hotwell temperature is then the mass-weighted
            // enthalpy root of saturated condensate plus the explicit saturated-liquid moisture drain.
            new OperationalFluidNodeSeed.SaturatedMixture("exhaust", 0.008438344971042927d, 0.87290510788436326d),
            new OperationalFluidNodeSeed.SaturatedMixture("hotwell", 0.010808002980612689d, 0d),
            new OperationalFluidNodeSeed.SubcooledLiquid("feedwater-inventory", 47.37848866583073d, 0.000003024302581887423d),
        };

        return CreateRuntimeEngine(
            includeEvidenceDerivedElectricalProtections: true,
            runtimeStep: runtimeStep,
            deterministicSeedStepCount: seedStepCount,
            useHybridSemiImplicitHydraulics: false,
            useFourNodeBranchContinuityShadowIntegration: false,
            useFourNodeBranchContinuityCorrectedCommitOptIn: true,
            thermodynamicClosureMode: WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain,
            initialFuelTemperatureCelsiusOverride: 305.62514906467646d,
            initialStructureTemperatureCelsiusOverride: 289.13956081139787d,
            initialNeutronPopulationOverride: NeutronPopulation.FromRelative(0.3297117650655722d),
            initialControlValvePercentOpenOverride: 29.281329697436618d,
            initialCondensatePumpPercentOverride: 42.966515369975916d,
            initialFeedwaterPumpPercentOverride: 96.930826801569154d,
            initialFluidNodeSeeds: fluidNodeSeeds,
            governorIntegralReferenceMode: TurbineGovernorIntegralReferenceMode.SynchronousSpeedWhenParalleled,
            turbineAdmissionPhasePolicyOverride: TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain,
            turbineMoistureDrainNodeId: "hotwell");
    }

    private static IControlRoomRuntimeEngine CreateRuntimeEngine(
        bool includeEvidenceDerivedElectricalProtections,
        TimeSpan? runtimeStep = null,
        int deterministicSeedStepCount = 2,
        bool useHybridSemiImplicitHydraulics = false,
        bool useFourNodeBranchContinuityShadowIntegration = false,
        bool useFourNodeBranchContinuityCorrectedCommitOptIn = false,
        WaterSteamThermodynamicClosureMode thermodynamicClosureMode = WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology,
        double? initialPrimarySuctionCompressionFraction = null,
        double? initialPrimaryPressureCompressionFraction = null,
        double? initialPrimaryOutletSaturationPressureMegapascals = null,
        double? initialPrimaryOutletVaporQualityFraction = null,
        double? initialFuelTemperatureCelsiusOverride = null,
        double? initialStructureTemperatureCelsiusOverride = null,
        NeutronPopulation? initialNeutronPopulationOverride = null,
        double? initialControlValvePercentOpenOverride = null,
        double? initialCondensatePumpPercentOverride = null,
        double? initialFeedwaterPumpPercentOverride = null,
        IReadOnlyCollection<OperationalFluidNodeSeed>? initialFluidNodeSeeds = null,
        TurbineGovernorIntegralReferenceMode governorIntegralReferenceMode = TurbineGovernorIntegralReferenceMode.EffectiveDroopSetpoint,
        TurbineAdmissionPhasePolicy? turbineAdmissionPhasePolicyOverride = null,
        string? turbineMoistureDrainNodeId = null)
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            initialNeutronPopulationOverride ?? GenerationReadySeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 280d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: true,
            initialRequestedElectricalPowerMegawatts: 5d,
            initialCondenserCoolingPowerMegawatts: 40d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialControlValvePercentOpen: initialControlValvePercentOpenOverride ?? LoadedControlValveBiasPercent,
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
            initialCondensatePumpPercent: initialCondensatePumpPercentOverride ?? 42d,
            initialFeedwaterPumpPercent: initialFeedwaterPumpPercentOverride ?? 97d,
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
            runtimeStep: runtimeStep,
            useFourNodeBranchContinuityShadowIntegration: useFourNodeBranchContinuityShadowIntegration,
            useFourNodeBranchContinuityCorrectedCommitOptIn: useFourNodeBranchContinuityCorrectedCommitOptIn,
            thermodynamicClosureMode: thermodynamicClosureMode,
            initialPrimarySuctionCompressionFraction: initialPrimarySuctionCompressionFraction,
            initialPrimaryPressureCompressionFraction: initialPrimaryPressureCompressionFraction,
            initialPrimaryOutletSaturationPressureMegapascals: initialPrimaryOutletSaturationPressureMegapascals,
            initialPrimaryOutletVaporQualityFraction: initialPrimaryOutletVaporQualityFraction,
            initialFuelTemperatureCelsiusOverride: initialFuelTemperatureCelsiusOverride,
            initialStructureTemperatureCelsiusOverride: initialStructureTemperatureCelsiusOverride,
            initialFluidNodeSeeds: initialFluidNodeSeeds,
            governorIntegralReferenceMode: governorIntegralReferenceMode,
            turbineAdmissionPhasePolicyOverride: turbineAdmissionPhasePolicyOverride,
            turbineMoistureDrainNodeId: turbineMoistureDrainNodeId);
}
