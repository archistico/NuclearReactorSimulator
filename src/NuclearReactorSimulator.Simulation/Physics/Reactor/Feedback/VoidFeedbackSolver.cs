using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// Pure, stateless linear void-reactivity feedback solver.
/// Void-fraction resolution from thermodynamic state is intentionally a separate concern.
/// </summary>
public sealed class VoidFeedbackSolver
{
    private readonly ReactivityModel _reactivityModel = new();

    public VoidFeedbackSnapshot Evaluate(VoidFeedbackInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var definition = input.Definition;
        var delta = input.VoidFraction - definition.ReferenceVoidFraction;
        var reactivity = definition.Coefficient * delta;

        return new VoidFeedbackSnapshot(
            definition.Id,
            definition.ReferenceVoidFraction,
            input.VoidFraction,
            delta,
            definition.Coefficient,
            reactivity);
    }

    public VoidFeedbackSetSnapshot Evaluate(IEnumerable<VoidFeedbackInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var canonical = inputs.ToArray();
        Validate(canonical);
        Array.Sort(canonical, VoidFeedbackInputComparer.Instance);

        var snapshots = new VoidFeedbackSnapshot[canonical.Length];
        var contributions = new ReactivityContribution[canonical.Length];

        for (var index = 0; index < canonical.Length; index++)
        {
            var snapshot = Evaluate(canonical[index]);
            snapshots[index] = snapshot;
            contributions[index] = snapshot.ToContribution();
        }

        return new VoidFeedbackSetSnapshot(
            snapshots,
            _reactivityModel.Evaluate(contributions));
    }

    private static void Validate(IReadOnlyList<VoidFeedbackInput> inputs)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index]
                ?? throw new ArgumentException(
                    $"Void-feedback input at index {index} is null.",
                    nameof(inputs));

            if (!ids.Add(input.Definition.Id))
            {
                throw new ArgumentException(
                    $"Duplicate void-feedback id '{input.Definition.Id}'.",
                    nameof(inputs));
            }
        }
    }

    private sealed class VoidFeedbackInputComparer : IComparer<VoidFeedbackInput>
    {
        public static VoidFeedbackInputComparer Instance { get; } = new();

        public int Compare(VoidFeedbackInput? left, VoidFeedbackInput? right)
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

            return StringComparer.Ordinal.Compare(left.Definition.Id, right.Definition.Id);
        }
    }
}
