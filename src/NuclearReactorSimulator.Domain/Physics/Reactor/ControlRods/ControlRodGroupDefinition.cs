using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Immutable named group of rods that may receive a common motion command.
/// </summary>
public sealed record ControlRodGroupDefinition
{
    public ControlRodGroupDefinition(string id, IEnumerable<string> rodIds)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Control-rod group id cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(rodIds);

        var canonicalRodIds = rodIds
            .Select(static rodId => string.IsNullOrWhiteSpace(rodId)
                ? throw new ArgumentException("Control-rod group members cannot contain empty ids.", "rodIds")
                : rodId.Trim())
            .OrderBy(static rodId => rodId, StringComparer.Ordinal)
            .ToArray();

        if (canonicalRodIds.Length == 0)
        {
            throw new ArgumentException("A control-rod group must contain at least one rod.", "rodIds");
        }

        if (canonicalRodIds.Distinct(StringComparer.Ordinal).Count() != canonicalRodIds.Length)
        {
            throw new ArgumentException("A control-rod group cannot contain duplicate rod ids.", "rodIds");
        }

        Id = id.Trim();
        RodIds = new ReadOnlyCollection<string>(canonicalRodIds);
    }

    public string Id { get; }

    public IReadOnlyList<string> RodIds { get; }
}
