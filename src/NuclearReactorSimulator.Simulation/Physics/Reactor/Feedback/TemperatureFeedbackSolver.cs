using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// Pure, stateless linear temperature-reactivity feedback solver.
/// The current milestone intentionally evaluates committed temperatures without algebraic iteration.
/// </summary>
public sealed class TemperatureFeedbackSolver
{
    private readonly ReactivityModel _reactivityModel = new();

    public TemperatureFeedbackSnapshot Evaluate(TemperatureFeedbackInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var definition = input.Definition;
        var delta = input.Temperature - definition.ReferenceTemperature;
        var reactivity = definition.Coefficient * delta;

        return new TemperatureFeedbackSnapshot(
            definition.Id,
            definition.Kind,
            definition.ReferenceTemperature,
            input.Temperature,
            delta,
            definition.Coefficient,
            reactivity);
    }

    public TemperatureFeedbackSetSnapshot Evaluate(IEnumerable<TemperatureFeedbackInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var canonical = inputs.ToArray();
        Validate(canonical);
        Array.Sort(canonical, TemperatureFeedbackInputComparer.Instance);

        var snapshots = new TemperatureFeedbackSnapshot[canonical.Length];
        var contributions = new ReactivityContribution[canonical.Length];

        for (var index = 0; index < canonical.Length; index++)
        {
            var snapshot = Evaluate(canonical[index]);
            snapshots[index] = snapshot;
            contributions[index] = snapshot.ToContribution();
        }

        return new TemperatureFeedbackSetSnapshot(
            snapshots,
            _reactivityModel.Evaluate(contributions));
    }

    private static void Validate(IReadOnlyList<TemperatureFeedbackInput> inputs)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index]
                ?? throw new ArgumentException(
                    $"Temperature-feedback input at index {index} is null.",
                    nameof(inputs));

            if (!ids.Add(input.Definition.Id))
            {
                throw new ArgumentException(
                    $"Duplicate temperature-feedback id '{input.Definition.Id}'.",
                    nameof(inputs));
            }
        }
    }

    private sealed class TemperatureFeedbackInputComparer : IComparer<TemperatureFeedbackInput>
    {
        public static TemperatureFeedbackInputComparer Instance { get; } = new();

        public int Compare(TemperatureFeedbackInput? left, TemperatureFeedbackInput? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            var kindComparison = left.Definition.Kind.CompareTo(right.Definition.Kind);
            return kindComparison != 0
                ? kindComparison
                : StringComparer.Ordinal.Compare(left.Definition.Id, right.Definition.Id);
        }
    }
}
