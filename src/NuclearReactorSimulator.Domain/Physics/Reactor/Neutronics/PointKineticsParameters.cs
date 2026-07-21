using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;

/// <summary>
/// Immutable parameter set for generic point-reactor kinetics.
/// No plant-specific constants are hardcoded by the simulation engine.
/// </summary>
public sealed class PointKineticsParameters
{
    public PointKineticsParameters(
        TimeSpan promptNeutronGenerationTime,
        IEnumerable<DelayedNeutronGroupDefinition> delayedNeutronGroups)
    {
        if (promptNeutronGenerationTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(promptNeutronGenerationTime),
                promptNeutronGenerationTime,
                "Prompt-neutron generation time must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(delayedNeutronGroups);

        var canonical = delayedNeutronGroups
            .Select(static group => group ?? throw new ArgumentException("Delayed-neutron groups cannot contain null entries.", "delayedNeutronGroups"))
            .OrderBy(static group => group.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("Point kinetics requires at least one delayed-neutron group.", nameof(delayedNeutronGroups));
        }

        if (canonical.Select(static group => group.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Delayed-neutron group ids must be unique.", nameof(delayedNeutronGroups));
        }

        var beta = SumFractions(canonical);
        if (beta <= 0d || beta >= 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayedNeutronGroups),
                beta,
                "Effective delayed-neutron fraction must be greater than zero and less than one.");
        }

        PromptNeutronGenerationTime = promptNeutronGenerationTime;
        DelayedNeutronGroups = new ReadOnlyCollection<DelayedNeutronGroupDefinition>(canonical);
        EffectiveDelayedNeutronFraction = DelayedNeutronFraction.FromFraction(beta);
    }

    public TimeSpan PromptNeutronGenerationTime { get; }

    public double PromptNeutronGenerationTimeSeconds => PromptNeutronGenerationTime.TotalSeconds;

    public IReadOnlyList<DelayedNeutronGroupDefinition> DelayedNeutronGroups { get; }

    public DelayedNeutronFraction EffectiveDelayedNeutronFraction { get; }

    public DelayedNeutronGroupDefinition GetGroup(string id)
        => DelayedNeutronGroups.FirstOrDefault(group => string.Equals(group.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown delayed-neutron group '{id}'.");

    private static double SumFractions(IEnumerable<DelayedNeutronGroupDefinition> groups)
    {
        var sum = 0d;
        var compensation = 0d;

        foreach (var group in groups)
        {
            var corrected = group.Fraction.Fraction - compensation;
            var next = sum + corrected;
            compensation = (next - sum) - corrected;
            sum = next;
        }

        return sum;
    }
}
