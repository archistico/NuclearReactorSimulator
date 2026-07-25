using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;

/// <summary>
/// Deterministic ideal phase-separation model for aggregated steam drums.
/// It reads the same committed plant state used by the circulation solver and emits conservative internal source terms.
/// It never integrates plant state directly.
/// </summary>
public sealed class SteamDrumSeparationSolver
{
    private readonly SteamDrumSystemDefinition _definition;
    private readonly MainCirculationSystemSolver _circulationSolver;
    private readonly SimplifiedWaterSteamThermodynamicModel _thermodynamicModel = new();
    private readonly WaterSteamVoidFractionSolver _voidFractionSolver;

    public SteamDrumSeparationSolver(SteamDrumSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _circulationSolver = new MainCirculationSystemSolver(definition.MainCirculationSystem);
        _voidFractionSolver = new WaterSteamVoidFractionSolver(_thermodynamicModel);
    }

    public SteamDrumSystemDefinition Definition => _definition;

    public SteamDrumStepResult Solve(PlantState committedPlantState)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        return SolveCore(committedPlantState, _circulationSolver.Solve(committedPlantState), integrationInterval: null);
    }

    public SteamDrumStepResult Solve(PlantState committedPlantState, TimeSpan integrationInterval)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ValidateIntegrationInterval(integrationInterval);
        return SolveCore(committedPlantState, _circulationSolver.Solve(committedPlantState), integrationInterval);
    }

    public SteamDrumStepResult Solve(
        PlantState committedPlantState,
        MainCirculationSystemSnapshot circulation)
        => SolveCore(committedPlantState, circulation, integrationInterval: null);

    public SteamDrumStepResult Solve(
        PlantState committedPlantState,
        MainCirculationSystemSnapshot circulation,
        TimeSpan integrationInterval)
    {
        ValidateIntegrationInterval(integrationInterval);
        return SolveCore(committedPlantState, circulation, integrationInterval);
    }

    private SteamDrumStepResult SolveCore(
        PlantState committedPlantState,
        MainCirculationSystemSnapshot circulation,
        TimeSpan? integrationInterval)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(circulation);

        var canonicalPlant = _definition.MainCirculationSystem.ChannelGroups.CoreDefinition.PlantDefinition;
        if (!ReferenceEquals(committedPlantState.Definition, canonicalPlant))
        {
            throw new ArgumentException(
                "Committed plant state does not use the steam-drum system's canonical plant definition.",
                nameof(committedPlantState));
        }

        if (!ReferenceEquals(circulation.Definition, _definition.MainCirculationSystem))
        {
            throw new ArgumentException(
                "Main-circulation snapshot does not use the steam-drum system's canonical circulation definition.",
                nameof(circulation));
        }

        var fluidBalances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var snapshots = new List<SteamDrumSnapshot>(_definition.Drums.Count);

        foreach (var drum in _definition.Drums)
        {
            var loopDefinition = _definition.MainCirculationSystem.GetLoop(drum.MainCirculationLoopId);
            var loopSnapshot = circulation.GetLoop(drum.MainCirculationLoopId);
            var drumState = committedPlantState.GetFluidNode(drum.InventoryNodeId);
            var split = ResolvePhaseSplit(drumState);
            var incoming = SumPositiveReturnInflows(loopSnapshot);
            var steamSource = ResolveSteamSource(
                drum,
                loopSnapshot,
                committedPlantState,
                drumState,
                split,
                incoming,
                integrationInterval);
            var steamFlow = steamSource.ActualFlow;
            var separableLiquidInventory = ResolveSeparableLiquidInventoryMass(drumState);
            var liquidRecirculation = ResolveLiquidRecirculation(
                drum,
                loopSnapshot,
                incoming,
                steamFlow,
                separableLiquidInventory,
                integrationInterval);
            var liquidFlow = liquidRecirculation.ActualFlow;
            var totalSeparatedOutflow = steamFlow + liquidFlow;
            var steamEnergyRate = steamSource.SteamSpecificEnergy * steamFlow;
            var liquidEnergyRate = steamSource.LiquidSpecificEnergy * liquidFlow;
            var totalEnergyRate = steamEnergyRate + liquidEnergyRate;

            AddBalance(
                fluidBalances,
                drum.InventoryNodeId,
                new FluidNodeBalance(-totalSeparatedOutflow, -totalEnergyRate));
            AddBalance(
                fluidBalances,
                drum.SteamOutletNodeId,
                new FluidNodeBalance(steamFlow, steamEnergyRate));
            AddBalance(
                fluidBalances,
                loopDefinition.SuctionHeaderNodeId,
                new FluidNodeBalance(liquidFlow, liquidEnergyRate));

            snapshots.Add(new SteamDrumSnapshot(
                drum.Id,
                drum.MainCirculationLoopId,
                drum.InventoryNodeId,
                drum.SteamOutletNodeId,
                loopDefinition.SuctionHeaderNodeId,
                drumState.Mass,
                drumState.InternalEnergy,
                drumState.Pressure,
                drumState.Temperature,
                drumState.Phase,
                drumState.VaporQuality,
                split.VoidFraction,
                split.LiquidLevelFraction,
                incoming,
                steamFlow,
                liquidFlow,
                steamSource.SteamSpecificEnergy,
                steamSource.LiquidSpecificEnergy,
                steamEnergyRate,
                liquidEnergyRate,
                (-totalSeparatedOutflow + steamFlow + liquidFlow).KilogramsPerSecond,
                (-totalEnergyRate + steamEnergyRate + liquidEnergyRate).Watts)
            {
                SeparableLiquidInventoryMass = separableLiquidInventory,
                RequestedLiquidRecirculationMassFlowRate = liquidRecirculation.RequestedFlow,
                MaximumInventorySupportedLiquidRecirculationMassFlowRate = liquidRecirculation.MaximumInventorySupportedFlow,
                LiquidRecirculationInventoryLimited = liquidRecirculation.IsInventoryLimited,
                UsesPressureEnergyInventorySteamSource = steamSource.UsesCurrentSourceClosure,
                SteamSourcePressureDrivenCapacityMassFlowRate = steamSource.PressureDrivenCapacity,
                SteamSourceAvailableMassFlowRate = steamSource.AvailableFlow,
                SteamSourceIncomingEnergySupportedMassFlowRate = steamSource.IncomingEnergySupportedFlow,
                SteamSourceStoredVaporInventoryMass = steamSource.StoredVaporInventoryMass,
                SteamSourcePressureLimited = steamSource.IsPressureLimited,
                SteamSourceAvailabilityLimited = steamSource.IsAvailabilityLimited,
            });
        }

        var sourceTerms = new PlantNetworkSourceTerms(
            fluidBalances,
            new Dictionary<string, NuclearReactorSimulator.Simulation.Physics.Thermal.ThermalEnergyBalance>(StringComparer.Ordinal),
            Power.Zero);

        return new SteamDrumStepResult(
            new SteamDrumSystemSnapshot(_definition, snapshots),
            sourceTerms);
    }

    private PhaseSplit ResolvePhaseSplit(FluidNodeState state)
    {
        return state.Phase switch
        {
            FluidPhase.SubcooledLiquid => new PhaseSplit(
                0d,
                state.SpecificInternalEnergy,
                state.SpecificInternalEnergy,
                VoidFraction.NoVoid,
                SteamDrumLevelFraction.Full),
            FluidPhase.SuperheatedVapor => new PhaseSplit(
                1d,
                state.SpecificInternalEnergy,
                state.SpecificInternalEnergy,
                VoidFraction.AllVapor,
                SteamDrumLevelFraction.Empty),
            FluidPhase.SaturatedMixture => ResolveSaturatedMixture(state),
            _ => throw new InvalidOperationException(
                $"Steam-drum inventory node '{state.Id}' must have an explicit water/steam phase before separation can be solved."),
        };
    }

    private PhaseSplit ResolveSaturatedMixture(FluidNodeState state)
    {
        var quality = state.VaporQuality
            ?? throw new InvalidOperationException($"Steam-drum saturated mixture '{state.Id}' is missing vapor quality.");
        var saturation = _thermodynamicModel.GetSaturationProperties(state.Temperature);
        var liquidMassKilograms = state.Mass.Kilograms * (1d - quality.Fraction);
        var liquidVolumeCubicMetres = liquidMassKilograms / saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre;
        var levelFraction = Math.Clamp(liquidVolumeCubicMetres / state.Volume.CubicMetres, 0d, 1d);

        return new PhaseSplit(
            quality.Fraction,
            saturation.SaturatedLiquidInternalEnergy,
            saturation.SaturatedVaporInternalEnergy,
            _voidFractionSolver.Resolve(state.Thermodynamics),
            SteamDrumLevelFraction.FromFraction(levelFraction));
    }


    private SteamSourceResolution ResolveSteamSource(
        SteamDrumDefinition drum,
        MainCirculationLoopSnapshot loopSnapshot,
        PlantState committedPlantState,
        FluidNodeState drumState,
        PhaseSplit split,
        MassFlowRate incomingReturnFlow,
        TimeSpan? integrationInterval)
    {
        if (drum.SteamSource is null)
        {
            var legacyFlow = incomingReturnFlow * split.VaporMassFraction;
            return new SteamSourceResolution(
                legacyFlow,
                legacyFlow,
                legacyFlow,
                legacyFlow,
                Mass.Zero,
                split.VaporSpecificEnergy,
                split.LiquidSpecificEnergy,
                false,
                false,
                false);
        }

        if (!integrationInterval.HasValue)
        {
            throw new InvalidOperationException(
                $"Steam drum '{drum.Id}' uses the current pressure/energy/inventory steam-source closure and therefore requires an integration interval.");
        }

        var saturation = _thermodynamicModel.GetSaturationProperties(drumState.Temperature);
        var liquidSpecificEnergy = drumState.Phase == FluidPhase.SubcooledLiquid
            ? drumState.SpecificInternalEnergy
            : saturation.SaturatedLiquidInternalEnergy;
        var steamSpecificEnergy = drumState.Phase == FluidPhase.SuperheatedVapor
            ? drumState.SpecificInternalEnergy
            : saturation.SaturatedVaporInternalEnergy;
        var vaporizationEnergy = steamSpecificEnergy.JoulesPerKilogram - liquidSpecificEnergy.JoulesPerKilogram;
        if (!double.IsFinite(vaporizationEnergy) || vaporizationEnergy <= 0d)
        {
            throw new InvalidOperationException(
                $"Steam drum '{drum.Id}' cannot resolve a positive vaporization-energy interval for the current steam-source closure.");
        }

        var incomingEnergyRateWatts = SumPositiveReturnEnergyRateWatts(loopSnapshot, committedPlantState);
        var incomingLiquidReferencePowerWatts = liquidSpecificEnergy.JoulesPerKilogram * incomingReturnFlow.KilogramsPerSecond;
        var incomingExcessPowerWatts = Math.Max(0d, incomingEnergyRateWatts - incomingLiquidReferencePowerWatts);
        var incomingEnergySupportedFlow = MassFlowRate.FromKilogramsPerSecond(Math.Min(
            incomingReturnFlow.KilogramsPerSecond,
            incomingExcessPowerWatts / vaporizationEnergy));

        var storedVaporInventoryMass = ResolveSeparableVaporInventoryMass(drumState);
        var storedVaporAvailableFlow = storedVaporInventoryMass.Per(integrationInterval.Value);
        var availableFlow = incomingEnergySupportedFlow + storedVaporAvailableFlow;

        var steamOutletState = committedPlantState.GetFluidNode(drum.SteamOutletNodeId);
        var drivingPressurePascals = Math.Max(0d, drumState.Pressure.Pascals - steamOutletState.Pressure.Pascals);
        var pressureDrivenCapacity = MassFlowRate.FromKilogramsPerSecond(Math.Sqrt(
            drivingPressurePascals / drum.SteamSource.HydraulicResistance.PascalSecondsSquaredPerKilogramSquared));
        var actualFlow = MassFlowRate.FromKilogramsPerSecond(Math.Min(
            pressureDrivenCapacity.KilogramsPerSecond,
            availableFlow.KilogramsPerSecond));

        return new SteamSourceResolution(
            actualFlow,
            pressureDrivenCapacity,
            availableFlow,
            incomingEnergySupportedFlow,
            storedVaporInventoryMass,
            steamSpecificEnergy,
            liquidSpecificEnergy,
            true,
            pressureDrivenCapacity < availableFlow,
            availableFlow <= pressureDrivenCapacity);
    }

    private static double SumPositiveReturnEnergyRateWatts(
        MainCirculationLoopSnapshot loopSnapshot,
        PlantState committedPlantState)
    {
        var totalWatts = 0d;
        var compensation = 0d;

        foreach (var branchSnapshot in loopSnapshot.Branches)
        {
            var positiveFlow = Math.Max(0d, branchSnapshot.ReturnMassFlowRate.KilogramsPerSecond);
            if (positiveFlow == 0d)
            {
                continue;
            }

            var returnPipe = committedPlantState.Definition.GetPipe(branchSnapshot.ReturnPipeId);
            var upstream = committedPlantState.GetFluidNode(returnPipe.FromNodeId);
            var value = upstream.SpecificInternalEnergy.JoulesPerKilogram * positiveFlow;
            var adjusted = value - compensation;
            var next = totalWatts + adjusted;
            compensation = (next - totalWatts) - adjusted;
            totalWatts = next;
        }

        return totalWatts;
    }

    private static Mass ResolveSeparableVaporInventoryMass(FluidNodeState state)
        => state.Phase switch
        {
            FluidPhase.SubcooledLiquid => Mass.Zero,
            FluidPhase.SuperheatedVapor => state.Mass,
            FluidPhase.SaturatedMixture => state.Mass * (state.VaporQuality?.Fraction
                ?? throw new InvalidOperationException($"Steam-drum saturated mixture '{state.Id}' is missing vapor quality.")),
            _ => Mass.Zero,
        };

    private static LiquidRecirculationResolution ResolveLiquidRecirculation(
        SteamDrumDefinition drum,
        MainCirculationLoopSnapshot loop,
        MassFlowRate incomingReturnFlow,
        MassFlowRate separatedSteamFlow,
        Mass separableLiquidInventory,
        TimeSpan? integrationInterval)
    {
        if (drum.LiquidRecirculationMode == SteamDrumLiquidRecirculationMode.LegacyReturnSplit)
        {
            var legacyFlow = incomingReturnFlow - separatedSteamFlow;
            return new LiquidRecirculationResolution(legacyFlow, legacyFlow, legacyFlow, false);
        }

        if (drum.LiquidRecirculationMode != SteamDrumLiquidRecirculationMode.CirculationDemandBalanced)
        {
            throw new InvalidOperationException(
                $"Steam drum '{drum.Id}' uses unsupported liquid-recirculation mode '{drum.LiquidRecirculationMode}'.");
        }

        var pumpDemandFlow = SumPositivePumpOutflows(loop);
        var incomingLiquidFlow = MassFlowRate.FromKilogramsPerSecond(Math.Max(
            0d,
            incomingReturnFlow.KilogramsPerSecond - separatedSteamFlow.KilogramsPerSecond));
        var requestedFlow = MassFlowRate.FromKilogramsPerSecond(Math.Max(
            pumpDemandFlow.KilogramsPerSecond,
            incomingLiquidFlow.KilogramsPerSecond));

        if (separableLiquidInventory == Mass.Zero)
        {
            return new LiquidRecirculationResolution(
                requestedFlow,
                MassFlowRate.Zero,
                MassFlowRate.Zero,
                requestedFlow > MassFlowRate.Zero);
        }

        if (!integrationInterval.HasValue)
        {
            // Compatibility/diagnostic overloads without an integration horizon retain their historical instantaneous
            // demand result. Production current-v2 integration always supplies deltaTime and therefore uses the cap below.
            return new LiquidRecirculationResolution(requestedFlow, requestedFlow, requestedFlow, false);
        }

        var maximumInventorySupportedFlow = incomingLiquidFlow + separableLiquidInventory.Per(integrationInterval.Value);
        var actualFlow = MassFlowRate.FromKilogramsPerSecond(Math.Min(
            requestedFlow.KilogramsPerSecond,
            maximumInventorySupportedFlow.KilogramsPerSecond));
        return new LiquidRecirculationResolution(
            requestedFlow,
            actualFlow,
            maximumInventorySupportedFlow,
            actualFlow < requestedFlow);
    }

    private static Mass ResolveSeparableLiquidInventoryMass(FluidNodeState state)
        => state.Phase switch
        {
            FluidPhase.SubcooledLiquid => state.Mass,
            FluidPhase.SuperheatedVapor => Mass.Zero,
            FluidPhase.SaturatedMixture => state.Mass * (1d - (state.VaporQuality?.Fraction
                ?? throw new InvalidOperationException($"Steam-drum saturated mixture '{state.Id}' is missing vapor quality."))),
            _ => Mass.Zero,
        };

    private static void ValidateIntegrationInterval(TimeSpan integrationInterval)
    {
        if (integrationInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(integrationInterval), "Integration interval must be positive.");
        }
    }

    private static MassFlowRate SumPositivePumpOutflows(MainCirculationLoopSnapshot loop)
    {
        var totalKilogramsPerSecond = 0d;
        var compensation = 0d;
        foreach (var pump in loop.Pumps)
        {
            var value = Math.Max(0d, pump.MassFlowRate.KilogramsPerSecond);
            var adjusted = value - compensation;
            var next = totalKilogramsPerSecond + adjusted;
            compensation = (next - totalKilogramsPerSecond) - adjusted;
            totalKilogramsPerSecond = next;
        }

        return MassFlowRate.FromKilogramsPerSecond(totalKilogramsPerSecond);
    }

    private static MassFlowRate SumPositiveReturnInflows(MainCirculationLoopSnapshot loop)
    {
        var totalKilogramsPerSecond = 0d;
        var compensation = 0d;
        foreach (var branch in loop.Branches)
        {
            var value = Math.Max(0d, branch.ReturnMassFlowRate.KilogramsPerSecond);
            var adjusted = value - compensation;
            var next = totalKilogramsPerSecond + adjusted;
            compensation = (next - totalKilogramsPerSecond) - adjusted;
            totalKilogramsPerSecond = next;
        }

        return MassFlowRate.FromKilogramsPerSecond(totalKilogramsPerSecond);
    }

    private static void AddBalance(
        IDictionary<string, FluidNodeBalance> balances,
        string nodeId,
        FluidNodeBalance balance)
    {
        balances[nodeId] = balances.TryGetValue(nodeId, out var existing)
            ? existing + balance
            : balance;
    }

    private sealed record LiquidRecirculationResolution(
        MassFlowRate RequestedFlow,
        MassFlowRate ActualFlow,
        MassFlowRate MaximumInventorySupportedFlow,
        bool IsInventoryLimited);

    private sealed record SteamSourceResolution(
        MassFlowRate ActualFlow,
        MassFlowRate PressureDrivenCapacity,
        MassFlowRate AvailableFlow,
        MassFlowRate IncomingEnergySupportedFlow,
        Mass StoredVaporInventoryMass,
        SpecificEnergy SteamSpecificEnergy,
        SpecificEnergy LiquidSpecificEnergy,
        bool UsesCurrentSourceClosure,
        bool IsPressureLimited,
        bool IsAvailabilityLimited);

    private sealed record PhaseSplit(
        double VaporMassFraction,
        SpecificEnergy LiquidSpecificEnergy,
        SpecificEnergy VaporSpecificEnergy,
        VoidFraction VoidFraction,
        SteamDrumLevelFraction LiquidLevelFraction);
}
