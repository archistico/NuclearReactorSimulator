using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor;

/// <summary>
/// Pure algebraic composition of named reactivity contributions.
/// This model deliberately does not convert reactivity into neutron flux, period or power.
/// </summary>
public sealed class ReactivityModel
{
    public ReactivityBreakdownSnapshot Evaluate(IEnumerable<ReactivityContribution> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        var canonical = contributions.ToArray();
        ValidateContributions(canonical);

        Array.Sort(canonical, ReactivityContributionComparer.Instance);

        return new ReactivityBreakdownSnapshot(
            SumCanonical(canonical),
            canonical);
    }

    internal static Reactivity SumCanonical(IEnumerable<ReactivityContribution> contributions)
    {
        var sum = 0d;
        var compensation = 0d;

        foreach (var contribution in contributions)
        {
            var correctedValue = contribution.Value.DeltaKOverK - compensation;
            var next = sum + correctedValue;
            compensation = (next - sum) - correctedValue;
            sum = next;
        }

        return Reactivity.FromDeltaKOverK(sum);
    }

    private static void ValidateContributions(IReadOnlyList<ReactivityContribution> contributions)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < contributions.Count; index++)
        {
            var contribution = contributions[index]
                ?? throw new ArgumentException(
                    $"Reactivity contribution at index {index} is null.",
                    nameof(contributions));

            if (!ids.Add(contribution.Id))
            {
                throw new ArgumentException(
                    $"Duplicate reactivity contribution id '{contribution.Id}'.",
                    nameof(contributions));
            }
        }
    }

    private sealed class ReactivityContributionComparer : IComparer<ReactivityContribution>
    {
        public static ReactivityContributionComparer Instance { get; } = new();

        public int Compare(ReactivityContribution? left, ReactivityContribution? right)
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

            var kindComparison = left.Kind.CompareTo(right.Kind);
            return kindComparison != 0
                ? kindComparison
                : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}
