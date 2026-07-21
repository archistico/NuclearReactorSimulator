using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

public sealed class AutomaticOperationVerificationPlan
{
    public AutomaticOperationVerificationPlan(
        string id,
        IntegratedAutomaticOperationState initialState,
        TimeSpan stepSize,
        IEnumerable<AutomaticOperationVerificationPhase> phases,
        AutomaticOperationAcceptanceCriteria criteria)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Verification plan id cannot be empty or whitespace.", nameof(id));
        }
        InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
        if (stepSize <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, "Verification timestep must be positive.");
        }
        ArgumentNullException.ThrowIfNull(phases);

        var canonical = phases
            .Select(phase => phase ?? throw new ArgumentException("Verification phases cannot contain null entries.", nameof(phases)))
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("A verification plan must contain at least one phase.", nameof(phases));
        }
        if (canonical.Select(static phase => phase.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Verification phase ids must be unique.", nameof(phases));
        }

        foreach (var phase in canonical)
        {
            if (!ReferenceEquals(phase.Inputs.PlantInputs.Definition, initialState.PlantDefinition)
                || !ReferenceEquals(phase.Inputs.ReactorPrimaryInputs.Definition, initialState.ReactorPrimaryControlState.Definition)
                || !ReferenceEquals(phase.Inputs.TurbineSecondaryInputs.Definition, initialState.TurbineSecondaryControlState.Definition)
                || !ReferenceEquals(phase.Inputs.ProtectionInputs.Definition, initialState.ProtectionState.Definition)
                || !ReferenceEquals(phase.Inputs.AlarmInputs.Definition, initialState.AlarmState.Definition)
                || !ReferenceEquals(phase.Inputs.InstrumentationInputs.Definition, initialState.InstrumentationDefinition))
            {
                throw new ArgumentException($"Verification phase '{phase.Id}' does not use the plan's canonical M5.7 definitions.", nameof(phases));
            }
        }

        Id = id.Trim();
        StepSize = stepSize;
        Phases = new ReadOnlyCollection<AutomaticOperationVerificationPhase>(canonical);
    }

    public string Id { get; }
    public IntegratedAutomaticOperationState InitialState { get; }
    public TimeSpan StepSize { get; }
    public IReadOnlyList<AutomaticOperationVerificationPhase> Phases { get; }
    public AutomaticOperationAcceptanceCriteria Criteria { get; }
}
