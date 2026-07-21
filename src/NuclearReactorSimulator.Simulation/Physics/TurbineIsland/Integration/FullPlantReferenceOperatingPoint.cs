using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Immutable M4.7 fixed-input reactor-to-grid reference condition. It contains no hidden controller or state correction.
/// </summary>
public sealed class FullPlantReferenceOperatingPoint
{
    public FullPlantReferenceOperatingPoint(
        string id,
        IntegratedSecondaryCycleDefinition definition,
        FullPlantState initialState,
        IntegratedSecondaryCycleInputs inputs,
        TimeSpan stepSize,
        FullPlantSteadyStateCriteria criteria)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Reference operating-point id cannot be empty or whitespace.", nameof(id));
        }

        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));

        if (!ReferenceEquals(initialState.Definition, definition))
        {
            throw new ArgumentException("Initial full-plant state does not use the operating point's canonical definition.", nameof(initialState));
        }

        if (!ReferenceEquals(inputs.Definition, definition))
        {
            throw new ArgumentException("Integrated inputs do not use the operating point's canonical definition.", nameof(inputs));
        }

        if (stepSize <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, "Operating-point step size must be greater than zero.");
        }

        Id = id.Trim();
        StepSize = stepSize;
    }

    public string Id { get; }

    public IntegratedSecondaryCycleDefinition Definition { get; }

    public FullPlantState InitialState { get; }

    public IntegratedSecondaryCycleInputs Inputs { get; }

    public TimeSpan StepSize { get; }

    public FullPlantSteadyStateCriteria Criteria { get; }
}
