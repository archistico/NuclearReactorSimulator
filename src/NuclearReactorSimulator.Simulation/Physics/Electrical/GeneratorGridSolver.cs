using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Deterministic M4.5 synchronous generator, breaker and infinite-bus grid coupling over the full validated M4.4 secondary-cycle stack.
/// Breaker synchronization is evaluated from committed state; accepted electrical loading is fed back through the existing M4.2 rotor integrator.
/// </summary>
public sealed class GeneratorGridSolver
{
    private readonly GeneratorGridSystemDefinition _definition;
    private readonly CondensateFeedwaterSystemSolver _condensateFeedwaterSolver;

    public GeneratorGridSolver(GeneratorGridSystemDefinition definition, IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _condensateFeedwaterSolver = new CondensateFeedwaterSystemSolver(definition.CondensateFeedwaterSystem, thermodynamicModel);
    }

    public GeneratorGridSystemDefinition Definition => _definition;

    public GeneratorGridStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        GeneratorGridState committedElectricalState,
        GeneratorGridInputs inputs,
        TimeSpan deltaTime)
        => Step(
            committedPlantState,
            committedTurbineState,
            committedElectricalState,
            inputs,
            deltaTime,
            PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// Higher-phase composition seam preserving the single thermofluid integration boundary.
    /// </summary>
    public GeneratorGridStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        GeneratorGridState committedElectricalState,
        GeneratorGridInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(committedTurbineState);
        ArgumentNullException.ThrowIfNull(committedElectricalState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Generator/grid step time must be greater than zero.");
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.CondensateFeedwaterSystem.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the generator/grid system's canonical plant definition.", nameof(committedPlantState));
        }

        if (!ReferenceEquals(committedElectricalState.Definition, _definition))
        {
            throw new ArgumentException("Committed electrical state does not use this solver's canonical generator/grid definition.", nameof(committedElectricalState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Generator/grid inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        if (!ReferenceEquals(committedTurbineState.Definition, _definition.TurbineExpansionSystem))
        {
            throw new ArgumentException("Committed turbine state does not use the generator/grid system's canonical turbine definition.", nameof(committedTurbineState));
        }

        var electricalWorking = BuildElectricalWorking(committedTurbineState, committedElectricalState, inputs);
        var effectiveSecondaryInputs = BuildCondensateFeedwaterInputs(inputs.CondensateFeedwaterInputs, electricalWorking);
        var secondaryStep = _condensateFeedwaterSolver.Step(
            committedPlantState,
            committedTurbineState,
            effectiveSecondaryInputs,
            deltaTime,
            supplementalSourceTerms);

        var finalGridPhase = committedElectricalState.GridPhaseAngle.Advance(
            2d * Math.PI * _definition.Grid.NominalFrequency.Hertz * deltaTime.TotalSeconds);
        var turbineSnapshot = secondaryStep.Snapshot.CondenserSnapshot.TurbineExpansion;
        var generatorSnapshots = BuildGeneratorSnapshots(
            electricalWorking,
            turbineSnapshot,
            finalGridPhase,
            deltaTime);
        var candidateState = new GeneratorGridState(
            _definition,
            finalGridPhase,
            generatorSnapshots.Select(snapshot => new SynchronousGeneratorState(
                snapshot.GeneratorId,
                snapshot.FinalElectricalPhaseAngle,
                snapshot.BreakerFinallyClosed)));
        var audit = BuildAudit(generatorSnapshots);
        var gridSnapshot = new ElectricalGridSnapshot(
            _definition.Grid.Id,
            _definition.Grid.NominalFrequency,
            _definition.Grid.NominalLineVoltage,
            committedElectricalState.GridPhaseAngle,
            finalGridPhase);
        var snapshot = new GeneratorGridSnapshot(
            _definition,
            secondaryStep.Snapshot,
            gridSnapshot,
            generatorSnapshots,
            audit);

        return new GeneratorGridStepResult(secondaryStep, candidateState, snapshot);
    }

    private IReadOnlyDictionary<string, ElectricalWorking> BuildElectricalWorking(
        TurbineExpansionState committedTurbineState,
        GeneratorGridState committedElectricalState,
        GeneratorGridInputs inputs)
    {
        var result = new Dictionary<string, ElectricalWorking>(StringComparer.Ordinal);
        foreach (var generator in _definition.Generators)
        {
            var input = inputs.GetGeneratorInput(generator.Id);
            var state = committedElectricalState.GetGenerator(generator.Id);
            var rotorState = committedTurbineState.GetRotor(generator.RotorId);
            var initialFrequency = generator.ElectricalFrequencyAt(rotorState.AngularSpeed);
            var frequencyDifference = Frequency.FromHertz(Math.Abs(initialFrequency.Hertz - _definition.Grid.NominalFrequency.Hertz));
            var phaseDifference = state.ElectricalPhaseAngle.ShortestDifference(committedElectricalState.GridPhaseAngle);
            var voltageDifference = ElectricPotential.FromVolts(Math.Abs(input.TerminalLineVoltage.Volts - _definition.Grid.NominalLineVoltage.Volts));
            var synchronizationSatisfied = frequencyDifference <= generator.MaximumSynchronizationFrequencyDifference
                && phaseDifference <= generator.MaximumSynchronizationPhaseDifference
                && voltageDifference <= generator.MaximumSynchronizationVoltageDifference;

            var closeAccepted = !state.BreakerClosed && input.CloseBreakerCommand && synchronizationSatisfied;
            var closeRejected = !state.BreakerClosed && input.CloseBreakerCommand && !synchronizationSatisfied;
            var breakerFinallyClosed = input.OpenBreakerCommand
                ? false
                : state.BreakerClosed || closeAccepted;

            var requestedElectricalPower = breakerFinallyClosed ? input.RequestedElectricalPower : Power.Zero;
            var requestedMechanicalPower = requestedElectricalPower / generator.Efficiency.Fraction;
            var rotorDefinition = _definition.TurbineExpansionSystem.GetRotor(generator.RotorId);
            var commandedTorque = breakerFinallyClosed
                ? Torque.FromNewtonMetres(requestedMechanicalPower.Watts / rotorDefinition.RatedAngularSpeed.RadiansPerSecond)
                : Torque.Zero;

            result.Add(
                generator.Id,
                new ElectricalWorking(
                    generator,
                    input,
                    state,
                    initialFrequency,
                    frequencyDifference,
                    phaseDifference,
                    voltageDifference,
                    synchronizationSatisfied,
                    closeAccepted,
                    closeRejected,
                    breakerFinallyClosed,
                    commandedTorque));
        }

        return result;
    }

    private CondensateFeedwaterSystemInputs BuildCondensateFeedwaterInputs(
        CondensateFeedwaterSystemInputs original,
        IReadOnlyDictionary<string, ElectricalWorking> electricalWorking)
    {
        var originalCondenserInputs = original.CondenserInputs;
        var originalTurbineInputs = originalCondenserInputs.TurbineExpansionInputs;
        var rotorInputs = originalTurbineInputs.RotorInputs
            .Select(rotorInput =>
            {
                var generator = _definition.GetGeneratorForRotor(rotorInput.RotorId);
                return new TurbineRotorInput(
                    rotorInput.RotorId,
                    electricalWorking[generator.Id].CommandedElectromagneticTorque,
                    rotorInput.TripCommand);
            })
            .ToArray();
        var effectiveTurbineInputs = new TurbineExpansionInputs(
            originalTurbineInputs.Definition,
            originalTurbineInputs.MainSteamInputs,
            originalTurbineInputs.StageGroupInputs,
            rotorInputs);
        var effectiveCondenserInputs = new CondenserSystemInputs(
            originalCondenserInputs.Definition,
            effectiveTurbineInputs,
            originalCondenserInputs.CoolingBoundaryInputs);

        return new CondensateFeedwaterSystemInputs(
            original.Definition,
            effectiveCondenserInputs,
            original.TrainInputs);
    }

    private IReadOnlyList<SynchronousGeneratorSnapshot> BuildGeneratorSnapshots(
        IReadOnlyDictionary<string, ElectricalWorking> electricalWorking,
        TurbineExpansionSnapshot turbineSnapshot,
        PhaseAngle finalGridPhase,
        TimeSpan deltaTime)
    {
        var result = new List<SynchronousGeneratorSnapshot>(_definition.Generators.Count);
        foreach (var generator in _definition.Generators)
        {
            var working = electricalWorking[generator.Id];
            var rotor = turbineSnapshot.Rotors.Single(item => string.Equals(item.RotorId, generator.RotorId, StringComparison.Ordinal));
            var finalGeneratorPhase = working.State.ElectricalPhaseAngle.Advance(
                generator.PolePairs * rotor.AverageAngularSpeed.RadiansPerSecond * deltaTime.TotalSeconds);
            var finalFrequency = generator.ElectricalFrequencyAt(rotor.FinalAngularSpeed);
            var mechanicalInputPower = working.BreakerFinallyClosed ? rotor.ExternalLoadPower : Power.Zero;
            var electricalOutputPower = mechanicalInputPower * generator.Efficiency.Fraction;
            var conversionLossPower = mechanicalInputPower - electricalOutputPower;

            result.Add(new SynchronousGeneratorSnapshot(
                generator.Id,
                generator.RotorId,
                generator.BreakerId,
                working.InitialFrequency,
                finalFrequency,
                working.State.ElectricalPhaseAngle,
                finalGeneratorPhase,
                working.InitialPhaseDifference,
                finalGeneratorPhase.ShortestDifference(finalGridPhase),
                working.Input.TerminalLineVoltage,
                _definition.Grid.NominalLineVoltage,
                working.FrequencyDifference,
                working.VoltageDifference,
                working.SynchronizationSatisfied,
                working.State.BreakerClosed,
                working.BreakerFinallyClosed,
                working.Input.CloseBreakerCommand,
                working.Input.OpenBreakerCommand,
                working.CloseAccepted,
                working.CloseRejected,
                working.Input.RequestedElectricalPower,
                working.CommandedElectromagneticTorque,
                rotor.EffectiveExternalLoadTorque,
                mechanicalInputPower,
                electricalOutputPower,
                conversionLossPower));
        }

        return result.OrderBy(static item => item.GeneratorId, StringComparer.Ordinal).ToArray();
    }

    private static GeneratorElectricalAudit BuildAudit(IEnumerable<SynchronousGeneratorSnapshot> snapshots)
    {
        var canonical = snapshots.OrderBy(static item => item.GeneratorId, StringComparer.Ordinal).ToArray();
        var mechanicalInput = Power.FromWatts(canonical.Sum(static item => item.MechanicalInputPower.Watts));
        var electricalExport = Power.FromWatts(canonical.Sum(static item => item.ElectricalOutputPower.Watts));
        var conversionLoss = Power.FromWatts(canonical.Sum(static item => item.ConversionLossPower.Watts));
        var residual = mechanicalInput.Watts - electricalExport.Watts - conversionLoss.Watts;
        return new GeneratorElectricalAudit(mechanicalInput, electricalExport, conversionLoss, residual);
    }

    private sealed record ElectricalWorking(
        SynchronousGeneratorDefinition Definition,
        SynchronousGeneratorInput Input,
        SynchronousGeneratorState State,
        Frequency InitialFrequency,
        Frequency FrequencyDifference,
        PhaseAngleDifference InitialPhaseDifference,
        ElectricPotential VoltageDifference,
        bool SynchronizationSatisfied,
        bool CloseAccepted,
        bool CloseRejected,
        bool BreakerFinallyClosed,
        Torque CommandedElectromagneticTorque);
}
