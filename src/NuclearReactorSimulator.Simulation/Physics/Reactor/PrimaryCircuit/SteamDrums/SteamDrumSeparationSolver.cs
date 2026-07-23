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
        return Solve(committedPlantState, _circulationSolver.Solve(committedPlantState));
    }

    public SteamDrumStepResult Solve(
        PlantState committedPlantState,
        MainCirculationSystemSnapshot circulation)
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
            var steamFlow = incoming * split.VaporMassFraction;
            var liquidFlow = ResolveLiquidRecirculationFlow(drum, loopSnapshot, incoming, steamFlow);
            var totalSeparatedOutflow = steamFlow + liquidFlow;
            var steamEnergyRate = split.VaporSpecificEnergy * steamFlow;
            var liquidEnergyRate = split.LiquidSpecificEnergy * liquidFlow;
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
                split.VaporSpecificEnergy,
                split.LiquidSpecificEnergy,
                steamEnergyRate,
                liquidEnergyRate,
                (-totalSeparatedOutflow + steamFlow + liquidFlow).KilogramsPerSecond,
                (-totalEnergyRate + steamEnergyRate + liquidEnergyRate).Watts));
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


    private static MassFlowRate ResolveLiquidRecirculationFlow(
        SteamDrumDefinition drum,
        MainCirculationLoopSnapshot loop,
        MassFlowRate incomingReturnFlow,
        MassFlowRate separatedSteamFlow)
        => drum.LiquidRecirculationMode switch
        {
            SteamDrumLiquidRecirculationMode.LegacyReturnSplit => incomingReturnFlow - separatedSteamFlow,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced => SumPositivePumpOutflows(loop),
            _ => throw new InvalidOperationException(
                $"Steam drum '{drum.Id}' uses unsupported liquid-recirculation mode '{drum.LiquidRecirculationMode}'."),
        };

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

    private sealed record PhaseSplit(
        double VaporMassFraction,
        SpecificEnergy LiquidSpecificEnergy,
        SpecificEnergy VaporSpecificEnergy,
        VoidFraction VoidFraction,
        SteamDrumLevelFraction LiquidLevelFraction);
}
