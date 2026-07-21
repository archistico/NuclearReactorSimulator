using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

/// <summary>
/// Deterministic M4.3 committed-state condenser solver.
/// Condensation is an internal steam-space-to-hotwell mass/energy transfer while rejected heat is an explicit external power sink.
/// Plant fluid/thermal inventories remain integrated exactly once through the inherited M3/M4 network orchestration boundary.
/// </summary>
public sealed class CondenserSystemSolver
{
    private const double MinimumResidualSteamSpaceMassKilograms = 1e-9d;

    private readonly CondenserSystemDefinition _definition;
    private readonly TurbineExpansionSolver _turbineExpansionSolver;

    public CondenserSystemSolver(
        CondenserSystemDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _turbineExpansionSolver = new TurbineExpansionSolver(definition.TurbineExpansionSystem, thermodynamicModel);
    }

    public CondenserSystemDefinition Definition => _definition;

    public CondenserSystemStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        CondenserSystemInputs inputs,
        TimeSpan deltaTime)
        => Step(
            committedPlantState,
            committedTurbineState,
            inputs,
            deltaTime,
            PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// M4.4-compatible overload that composes downstream supplemental source terms before the inherited single plant-network integration.
    /// </summary>
    public CondenserSystemStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        CondenserSystemInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(committedTurbineState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Condenser-system step time must be greater than zero.");
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the condenser system's canonical plant definition.", nameof(committedPlantState));
        }

        if (!ReferenceEquals(committedTurbineState.Definition, _definition.TurbineExpansionSystem))
        {
            throw new ArgumentException("Committed turbine state does not use the condenser system's canonical M4.2 definition.", nameof(committedTurbineState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Condenser-system inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var solutions = _definition.Condensers
            .Select(condenser => SolveCondenser(committedPlantState, condenser, inputs.GetCoolingBoundaryInput(condenser.CoolingBoundaryId), deltaTime))
            .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
            .ToArray();
        var sourceTerms = PlantNetworkSourceTerms.Combine(
            BuildSourceTerms(solutions),
            supplementalSourceTerms);
        var turbineStep = _turbineExpansionSolver.Step(
            committedPlantState,
            committedTurbineState,
            inputs.TurbineExpansionInputs,
            deltaTime,
            sourceTerms);

        var condenserSnapshots = solutions
            .Select(solution => BuildCondenserSnapshot(solution, turbineStep.CandidatePlantState))
            .ToArray();
        var coolingSnapshots = solutions
            .Select(solution => new CondenserCoolingBoundarySnapshot(
                solution.CoolingBoundaryInput.BoundaryId,
                solution.Definition.Id,
                solution.CoolingBoundaryInput.AvailableHeatRejectionPower,
                solution.HeatRejectionPower))
            .ToArray();
        var snapshot = new CondenserSystemSnapshot(
            _definition,
            turbineStep.Snapshot,
            condenserSnapshots,
            coolingSnapshots);

        return new CondenserSystemStepResult(turbineStep, snapshot);
    }

    private static CondenserSolution SolveCondenser(
        PlantState committedPlantState,
        CondenserDefinition definition,
        CondenserCoolingBoundaryInput coolingBoundaryInput,
        TimeSpan deltaTime)
    {
        var steamSpace = committedPlantState.GetFluidNode(definition.SteamSpaceNodeId);
        var hotwell = committedPlantState.GetFluidNode(definition.HotwellNodeId);
        var condensableVaporMassFraction = ResolveCondensableVaporMassFraction(steamSpace);
        var phaseLimitedMassKilograms = steamSpace.Mass.Kilograms * condensableVaporMassFraction;
        var residualLimitedMassKilograms = Math.Max(
            0d,
            steamSpace.Mass.Kilograms - MinimumResidualSteamSpaceMassKilograms);
        var availableCondensableMass = Mass.FromKilograms(Math.Min(phaseLimitedMassKilograms, residualLimitedMassKilograms));
        var inventoryLimitedFlow = availableCondensableMass.Per(deltaTime);

        var specificEnergyDropJoulesPerKilogram = steamSpace.SpecificInternalEnergy.JoulesPerKilogram
            - hotwell.SpecificInternalEnergy.JoulesPerKilogram;
        var thermalLimitedFlow = specificEnergyDropJoulesPerKilogram <= 0d
            ? MassFlowRate.Zero
            : MassFlowRate.FromKilogramsPerSecond(
                coolingBoundaryInput.AvailableHeatRejectionPower.Watts / specificEnergyDropJoulesPerKilogram);
        var actualFlow = MassFlowRate.FromKilogramsPerSecond(Math.Max(
            0d,
            Math.Min(
                definition.MaximumCondensationMassFlowRate.KilogramsPerSecond,
                Math.Min(inventoryLimitedFlow.KilogramsPerSecond, thermalLimitedFlow.KilogramsPerSecond))));

        var steamEnergyRemovalRate = steamSpace.SpecificInternalEnergy * actualFlow;
        var hotwellEnergyAdditionRate = hotwell.SpecificInternalEnergy * actualFlow;
        var heatRejectionPower = steamEnergyRemovalRate - hotwellEnergyAdditionRate;
        if (heatRejectionPower < Power.Zero)
        {
            throw new InvalidOperationException(
                $"Condenser '{definition.Id}' produced negative heat rejection from the committed steam-space/hotwell energy gradient.");
        }

        return new CondenserSolution(
            definition,
            coolingBoundaryInput,
            steamSpace,
            hotwell,
            condensableVaporMassFraction,
            availableCondensableMass,
            inventoryLimitedFlow,
            thermalLimitedFlow,
            actualFlow,
            steamEnergyRemovalRate,
            hotwellEnergyAdditionRate,
            heatRejectionPower);
    }

    private static PlantNetworkSourceTerms BuildSourceTerms(IEnumerable<CondenserSolution> solutions)
    {
        var fluidBalances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var totalHeatRejectionPower = Power.Zero;

        foreach (var solution in solutions.OrderBy(static item => item.Definition.Id, StringComparer.Ordinal))
        {
            AddBalance(
                fluidBalances,
                solution.Definition.SteamSpaceNodeId,
                new FluidNodeBalance(-solution.ActualCondensationMassFlowRate, -solution.SteamEnergyRemovalRate));
            AddBalance(
                fluidBalances,
                solution.Definition.HotwellNodeId,
                new FluidNodeBalance(solution.ActualCondensationMassFlowRate, solution.HotwellEnergyAdditionRate));
            totalHeatRejectionPower += solution.HeatRejectionPower;
        }

        return new PlantNetworkSourceTerms(
            fluidBalances,
            new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
            MassFlowRate.Zero,
            -totalHeatRejectionPower);
    }

    private static CondenserSnapshot BuildCondenserSnapshot(
        CondenserSolution solution,
        PlantState candidatePlantState)
    {
        var finalSteamSpace = candidatePlantState.GetFluidNode(solution.Definition.SteamSpaceNodeId);
        var finalHotwell = candidatePlantState.GetFluidNode(solution.Definition.HotwellNodeId);

        return new CondenserSnapshot(
            solution.Definition.Id,
            solution.Definition.TurbineStageGroupId,
            solution.Definition.SteamSpaceNodeId,
            solution.Definition.HotwellNodeId,
            solution.Definition.CoolingBoundaryId,
            solution.InitialSteamSpace.Pressure,
            finalSteamSpace.Pressure,
            VacuumBelowAtmosphere(solution.InitialSteamSpace.Pressure),
            VacuumBelowAtmosphere(finalSteamSpace.Pressure),
            solution.InitialSteamSpace.Temperature,
            finalSteamSpace.Temperature,
            solution.InitialSteamSpace.Phase,
            finalSteamSpace.Phase,
            solution.InitialSteamSpace.VaporQuality,
            finalSteamSpace.VaporQuality,
            solution.CondensableVaporMassFraction,
            solution.AvailableCondensableMass,
            solution.Definition.MaximumCondensationMassFlowRate,
            solution.InventoryLimitedCondensationMassFlowRate,
            solution.ThermalLimitedCondensationMassFlowRate,
            solution.ActualCondensationMassFlowRate,
            solution.InitialSteamSpace.SpecificInternalEnergy,
            solution.InitialHotwell.SpecificInternalEnergy,
            solution.SteamEnergyRemovalRate,
            solution.HotwellEnergyAdditionRate,
            solution.HeatRejectionPower,
            solution.InitialHotwell.Mass,
            finalHotwell.Mass,
            solution.InitialHotwell.Temperature,
            finalHotwell.Temperature,
            solution.InitialHotwell.Phase,
            finalHotwell.Phase);
    }

    private static double ResolveCondensableVaporMassFraction(FluidNodeState steamSpace)
        => steamSpace.Phase switch
        {
            FluidPhase.SuperheatedVapor => 1d,
            FluidPhase.SaturatedMixture => steamSpace.VaporQuality?.Fraction
                ?? throw new InvalidOperationException(
                    $"Saturated condenser steam-space node '{steamSpace.Id}' does not expose vapor quality."),
            FluidPhase.SubcooledLiquid => 0d,
            _ => 0d,
        };

    private static PressureDifference VacuumBelowAtmosphere(Pressure absolutePressure)
        => PressureDifference.FromPascals(Math.Max(0d, Pressure.StandardAtmosphere.Pascals - absolutePressure.Pascals));

    private static void AddBalance(
        IDictionary<string, FluidNodeBalance> balances,
        string nodeId,
        FluidNodeBalance balance)
    {
        balances[nodeId] = balances.TryGetValue(nodeId, out var existing)
            ? existing + balance
            : balance;
    }

    private sealed record CondenserSolution(
        CondenserDefinition Definition,
        CondenserCoolingBoundaryInput CoolingBoundaryInput,
        FluidNodeState InitialSteamSpace,
        FluidNodeState InitialHotwell,
        double CondensableVaporMassFraction,
        Mass AvailableCondensableMass,
        MassFlowRate InventoryLimitedCondensationMassFlowRate,
        MassFlowRate ThermalLimitedCondensationMassFlowRate,
        MassFlowRate ActualCondensationMassFlowRate,
        Power SteamEnergyRemovalRate,
        Power HotwellEnergyAdditionRate,
        Power HeatRejectionPower);
}
