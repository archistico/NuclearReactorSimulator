using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Deterministic M4.2 committed-state turbine expansion and rotor solver.
/// Steam expansion is staged as an internal inlet-to-exhaust mass transfer plus explicit thermofluid-to-shaft energy transfer.
/// Plant inventories are still integrated exactly once by the inherited M3/M4 network boundary; rotor kinetic energy is integrated once here.
/// </summary>
public sealed class TurbineExpansionSolver
{
    private readonly TurbineExpansionSystemDefinition _definition;
    private readonly MainSteamNetworkSolver _mainSteamSolver;

    public TurbineExpansionSolver(
        TurbineExpansionSystemDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _mainSteamSolver = new MainSteamNetworkSolver(definition.MainSteamNetwork, thermodynamicModel);
    }

    public TurbineExpansionSystemDefinition Definition => _definition;

    public TurbineExpansionStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        TurbineExpansionInputs inputs,
        TimeSpan deltaTime)
        => Step(committedPlantState, committedTurbineState, inputs, deltaTime, PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// Higher M4 composition seam. Condenser/feedwater/generator phases may stage additional thermofluid source terms
    /// before the same single plant-network integration boundary while this solver remains the sole rotor-state integrator.
    /// </summary>
    public TurbineExpansionStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        TurbineExpansionInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(committedTurbineState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Turbine expansion step time must be greater than zero.");
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the turbine expansion system's canonical plant definition.", nameof(committedPlantState));
        }

        if (!ReferenceEquals(committedTurbineState.Definition, _definition))
        {
            throw new ArgumentException("Committed turbine state does not use this solver's canonical turbine expansion definition.", nameof(committedTurbineState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Turbine expansion inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var seconds = deltaTime.TotalSeconds;
        var stageWorking = BuildStageWorkingSet(committedTurbineState, inputs);
        var rotorWorking = SolveRotors(committedTurbineState, inputs, stageWorking, seconds);
        var stageSolution = SolveStages(committedPlantState, stageWorking, rotorWorking);
        var sourceTerms = PlantNetworkSourceTerms.Combine(
            BuildSourceTerms(stageSolution),
            supplementalSourceTerms);
        var mainSteamStep = _mainSteamSolver.Step(
            committedPlantState,
            inputs.MainSteamInputs,
            deltaTime,
            sourceTerms);

        var candidateTurbineState = new TurbineExpansionState(
            _definition,
            rotorWorking.Values
                .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
                .Select(static item => new TurbineRotorState(item.Definition.Id, item.FinalAngularSpeed)));
        var rotorSnapshots = rotorWorking.Values
            .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
            .Select(BuildRotorSnapshot)
            .ToArray();
        var mechanicalAudit = BuildMechanicalAudit(rotorSnapshots, deltaTime);
        var snapshot = new TurbineExpansionSnapshot(
            _definition,
            mainSteamStep.Snapshot,
            stageSolution.Select(static item => item.Snapshot),
            rotorSnapshots,
            mechanicalAudit);

        return new TurbineExpansionStepResult(mainSteamStep, candidateTurbineState, snapshot);
    }

    private IReadOnlyDictionary<string, StageWorking> BuildStageWorkingSet(
        TurbineExpansionState committedState,
        TurbineExpansionInputs inputs)
    {
        var result = new Dictionary<string, StageWorking>(StringComparer.Ordinal);
        foreach (var stage in _definition.StageGroups)
        {
            var rotor = _definition.GetRotor(stage.RotorId);
            var rotorState = committedState.GetRotor(rotor.Id);
            var rotorInput = inputs.GetRotorInput(rotor.Id);
            var stageInput = inputs.GetStageGroupInput(stage.Id);
            var effectiveFlow = rotorInput.TripCommand ? MassFlowRate.Zero : stageInput.MassFlowRate;
            var ratedPower = stage.NominalSpecificWork * effectiveFlow * stage.Efficiency.Fraction;
            var torque = Torque.FromNewtonMetres(ratedPower.Watts / rotor.RatedAngularSpeed.RadiansPerSecond);

            result.Add(
                stage.Id,
                new StageWorking(
                    stage,
                    rotorState,
                    rotorInput,
                    stageInput.MassFlowRate,
                    effectiveFlow,
                    torque));
        }

        return result;
    }

    private IReadOnlyDictionary<string, RotorWorking> SolveRotors(
        TurbineExpansionState committedState,
        TurbineExpansionInputs inputs,
        IReadOnlyDictionary<string, StageWorking> stageWorking,
        double seconds)
    {
        var result = new Dictionary<string, RotorWorking>(StringComparer.Ordinal);
        foreach (var rotor in _definition.Rotors)
        {
            var rotorState = committedState.GetRotor(rotor.Id);
            var rotorInput = inputs.GetRotorInput(rotor.Id);
            var turbineTorque = Torque.FromNewtonMetres(
                stageWorking.Values
                    .Where(item => string.Equals(item.Definition.RotorId, rotor.Id, StringComparison.Ordinal))
                    .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
                    .Sum(static item => item.ShaftTorque.NewtonMetres));

            var maximumLoadWithoutReverse = turbineTorque.NewtonMetres
                + (rotor.MomentOfInertia.KilogramSquareMetres * rotorState.AngularSpeed.RadiansPerSecond / seconds);
            var loadLimitedAtZeroSpeed = rotorInput.ExternalLoadTorque.NewtonMetres > maximumLoadWithoutReverse;
            var effectiveLoadNewtonMetres = Math.Min(rotorInput.ExternalLoadTorque.NewtonMetres, maximumLoadWithoutReverse);
            var effectiveLoadTorque = Torque.FromNewtonMetres(effectiveLoadNewtonMetres);
            var netTorque = turbineTorque - effectiveLoadTorque;
            var finalRadiansPerSecond = rotorState.AngularSpeed.RadiansPerSecond
                + (netTorque.NewtonMetres / rotor.MomentOfInertia.KilogramSquareMetres * seconds);

            // When the anti-reverse limiter is active, its analytical solution is exactly zero speed.
            // Canonicalize only round-off around that constrained stop; do not erase genuine low-speed motion.
            if (loadLimitedAtZeroSpeed && Math.Abs(finalRadiansPerSecond) <= 1e-12d)
            {
                finalRadiansPerSecond = 0d;
            }

            if (!double.IsFinite(finalRadiansPerSecond) || finalRadiansPerSecond < 0d)
            {
                throw new InvalidOperationException($"Turbine rotor '{rotor.Id}' integration produced an invalid angular speed.");
            }

            var finalAngularSpeed = AngularSpeed.FromRadiansPerSecond(finalRadiansPerSecond);
            var averageAngularSpeed = AngularSpeed.FromRadiansPerSecond(
                0.5d * (rotorState.AngularSpeed.RadiansPerSecond + finalRadiansPerSecond));
            var shaftPower = turbineTorque.At(averageAngularSpeed);
            var loadPower = effectiveLoadTorque.At(averageAngularSpeed);

            result.Add(
                rotor.Id,
                new RotorWorking(
                    rotor,
                    rotorState,
                    rotorInput,
                    turbineTorque,
                    effectiveLoadTorque,
                    netTorque,
                    finalAngularSpeed,
                    averageAngularSpeed,
                    shaftPower,
                    loadPower,
                    loadLimitedAtZeroSpeed));
        }

        return result;
    }

    private IReadOnlyList<StageSolution> SolveStages(
        PlantState committedPlantState,
        IReadOnlyDictionary<string, StageWorking> stageWorking,
        IReadOnlyDictionary<string, RotorWorking> rotorWorking)
    {
        var result = new List<StageSolution>(_definition.StageGroups.Count);
        foreach (var stage in _definition.StageGroups)
        {
            var working = stageWorking[stage.Id];
            var rotor = rotorWorking[stage.RotorId];
            var boundary = _definition.MainSteamNetwork.GetTurbineAdmissionBoundary(stage.AdmissionBoundaryId);
            var inlet = committedPlantState.GetFluidNode(boundary.SourceNodeId);
            var inletEnergyFlow = inlet.SpecificInternalEnergy * working.EffectiveMassFlowRate;
            var shaftPower = working.ShaftTorque.At(rotor.AverageAngularSpeed);
            var extractedSpecificWork = working.EffectiveMassFlowRate == MassFlowRate.Zero
                ? SpecificEnergy.Zero
                : SpecificEnergy.FromJoulesPerKilogram(
                    shaftPower.Watts / working.EffectiveMassFlowRate.KilogramsPerSecond);

            if (extractedSpecificWork > inlet.SpecificInternalEnergy)
            {
                throw new InvalidOperationException(
                    $"Turbine stage group '{stage.Id}' would extract {extractedSpecificWork.KilojoulesPerKilogram:F3} kJ/kg from an inlet containing only {inlet.SpecificInternalEnergy.KilojoulesPerKilogram:F3} kJ/kg internal energy.");
            }

            var exhaustSpecificInternalEnergy = SpecificEnergy.FromJoulesPerKilogram(
                inlet.SpecificInternalEnergy.JoulesPerKilogram - extractedSpecificWork.JoulesPerKilogram);
            var exhaustEnergyFlow = inletEnergyFlow - shaftPower;
            if (exhaustEnergyFlow < Power.Zero)
            {
                throw new InvalidOperationException($"Turbine stage group '{stage.Id}' produced a negative exhaust energy flow.");
            }

            var snapshot = new TurbineStageGroupSnapshot(
                stage.Id,
                stage.AdmissionBoundaryId,
                boundary.SourceNodeId,
                stage.ExhaustNodeId,
                stage.RotorId,
                working.RotorInput.TripCommand,
                working.CommandedMassFlowRate,
                working.EffectiveMassFlowRate,
                inlet.Pressure,
                inlet.Temperature,
                inlet.Phase,
                inlet.VaporQuality,
                inlet.SpecificInternalEnergy,
                stage.NominalSpecificWork,
                extractedSpecificWork,
                exhaustSpecificInternalEnergy,
                inletEnergyFlow,
                exhaustEnergyFlow,
                shaftPower,
                working.ShaftTorque);
            result.Add(new StageSolution(snapshot));
        }

        return result;
    }

    private static PlantNetworkSourceTerms BuildSourceTerms(IEnumerable<StageSolution> stageSolutions)
    {
        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var totalShaftPower = Power.Zero;

        foreach (var solution in stageSolutions.OrderBy(static item => item.Snapshot.StageGroupId, StringComparer.Ordinal))
        {
            var stage = solution.Snapshot;
            AddBalance(
                balances,
                stage.InletNodeId,
                new FluidNodeBalance(-stage.EffectiveMassFlowRate, -stage.InletEnergyFlowRate));
            AddBalance(
                balances,
                stage.ExhaustNodeId,
                new FluidNodeBalance(stage.EffectiveMassFlowRate, stage.ExhaustEnergyFlowRate));
            totalShaftPower += stage.ShaftPower;
        }

        return new PlantNetworkSourceTerms(
            balances,
            new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
            MassFlowRate.Zero,
            -totalShaftPower);
    }

    private static TurbineRotorSnapshot BuildRotorSnapshot(RotorWorking rotor)
    {
        return new TurbineRotorSnapshot(
            rotor.Definition.Id,
            rotor.Definition.MomentOfInertia,
            rotor.CommittedState.AngularSpeed,
            rotor.FinalAngularSpeed,
            rotor.AverageAngularSpeed,
            rotor.Definition.RatedAngularSpeed,
            rotor.Definition.OverspeedThreshold,
            rotor.TurbineTorque,
            rotor.Input.ExternalLoadTorque,
            rotor.EffectiveExternalLoadTorque,
            rotor.NetTorque,
            rotor.ShaftPower,
            rotor.ExternalLoadPower,
            rotor.Definition.MomentOfInertia.KineticEnergyAt(rotor.CommittedState.AngularSpeed),
            rotor.Definition.MomentOfInertia.KineticEnergyAt(rotor.FinalAngularSpeed),
            rotor.Input.TripCommand,
            rotor.CommittedState.AngularSpeed >= rotor.Definition.OverspeedThreshold,
            rotor.FinalAngularSpeed >= rotor.Definition.OverspeedThreshold,
            rotor.ExternalLoadTorqueLimitedAtZeroSpeed);
    }

    private static TurbineMechanicalAudit BuildMechanicalAudit(
        IEnumerable<TurbineRotorSnapshot> rotorSnapshots,
        TimeSpan deltaTime)
    {
        var canonical = rotorSnapshots.OrderBy(static item => item.RotorId, StringComparer.Ordinal).ToArray();
        var initialEnergy = Energy.FromJoules(canonical.Sum(static item => item.InitialKineticEnergy.Joules));
        var finalEnergy = Energy.FromJoules(canonical.Sum(static item => item.FinalKineticEnergy.Joules));
        var shaftPower = Power.FromWatts(canonical.Sum(static item => item.ShaftPower.Watts));
        var loadPower = Power.FromWatts(canonical.Sum(static item => item.ExternalLoadPower.Watts));
        var expectedDeltaJoules = (shaftPower - loadPower).Over(deltaTime).Joules;
        var actualDeltaJoules = finalEnergy.Joules - initialEnergy.Joules;

        return new TurbineMechanicalAudit(
            initialEnergy,
            finalEnergy,
            shaftPower,
            loadPower,
            actualDeltaJoules - expectedDeltaJoules);
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

    private sealed record StageWorking(
        TurbineStageGroupDefinition Definition,
        TurbineRotorState RotorState,
        TurbineRotorInput RotorInput,
        MassFlowRate CommandedMassFlowRate,
        MassFlowRate EffectiveMassFlowRate,
        Torque ShaftTorque);

    private sealed record RotorWorking(
        TurbineRotorDefinition Definition,
        TurbineRotorState CommittedState,
        TurbineRotorInput Input,
        Torque TurbineTorque,
        Torque EffectiveExternalLoadTorque,
        Torque NetTorque,
        AngularSpeed FinalAngularSpeed,
        AngularSpeed AverageAngularSpeed,
        Power ShaftPower,
        Power ExternalLoadPower,
        bool ExternalLoadTorqueLimitedAtZeroSpeed);

    private sealed record StageSolution(TurbineStageGroupSnapshot Snapshot);
}
