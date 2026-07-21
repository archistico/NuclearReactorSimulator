using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>
/// Immutable configurable initial condition and fixed command/input set for deterministic primary-circuit baseline runs.
/// It does not contain hidden controllers or corrective bookkeeping.
/// </summary>
public sealed class PrimaryCircuitReferenceOperatingPoint
{
    public PrimaryCircuitReferenceOperatingPoint(
        string id,
        IntegratedPrimaryCircuitDefinition definition,
        PlantState initialPlantState,
        IntegratedPrimaryCircuitInputs inputs,
        TimeSpan stepSize)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Reference operating-point id cannot be empty or whitespace.", nameof(id));
        }

        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        InitialPlantState = initialPlantState ?? throw new ArgumentNullException(nameof(initialPlantState));
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));

        if (!ReferenceEquals(initialPlantState.Definition, definition.PlantDefinition))
        {
            throw new ArgumentException(
                "Initial plant state does not use the operating point's canonical plant definition.",
                nameof(initialPlantState));
        }

        if (!ReferenceEquals(inputs.Definition, definition))
        {
            throw new ArgumentException(
                "Integrated inputs do not use the operating point's canonical integrated definition.",
                nameof(inputs));
        }

        if (stepSize <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, "Operating-point step size must be greater than zero.");
        }

        Id = id.Trim();
        StepSize = stepSize;
    }

    public string Id { get; }

    public IntegratedPrimaryCircuitDefinition Definition { get; }

    public PlantState InitialPlantState { get; }

    public IntegratedPrimaryCircuitInputs Inputs { get; }

    public TimeSpan StepSize { get; }
}
