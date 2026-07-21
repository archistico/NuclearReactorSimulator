using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

/// <summary>
/// Canonical M5.5 protection definition. Protection consumes measured channels only and owns no physical plant state.
/// </summary>
public sealed class ProtectionSystemDefinition
{
    public ProtectionSystemDefinition(
        string id,
        IntegratedSecondaryCycleDefinition plantDefinition,
        InstrumentationSystemDefinition instrumentation,
        IEnumerable<ProtectionFunctionDefinition> tripFunctions,
        IEnumerable<ProtectionInterlockDefinition>? interlocks = null,
        IEnumerable<ProtectionPermissiveDefinition>? resetPermissives = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Protection-system id cannot be empty or whitespace.", nameof(id));
        }
        PlantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
        Instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        ArgumentNullException.ThrowIfNull(tripFunctions);

        var functions = tripFunctions.Select(item => item ?? throw new ArgumentException("Protection functions cannot contain null entries.", nameof(tripFunctions)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal).ToArray();
        var canonicalInterlocks = (interlocks ?? Array.Empty<ProtectionInterlockDefinition>())
            .Select(item => item ?? throw new ArgumentException("Protection interlocks cannot contain null entries.", nameof(interlocks)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal).ToArray();
        var permissives = (resetPermissives ?? Array.Empty<ProtectionPermissiveDefinition>())
            .Select(item => item ?? throw new ArgumentException("Protection permissives cannot contain null entries.", nameof(resetPermissives)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal).ToArray();

        if (functions.Length == 0 && canonicalInterlocks.Length == 0)
        {
            throw new ArgumentException("A protection system must contain at least one trip function or interlock.", nameof(tripFunctions));
        }

        EnsureUnique(functions.Select(static item => item.Id), "protection function");
        EnsureUnique(canonicalInterlocks.Select(static item => item.Id), "protection interlock");
        EnsureUnique(permissives.Select(static item => item.Id), "reset permissive");
        EnsureUnique(functions.Select(static item => item.Id).Concat(canonicalInterlocks.Select(static item => item.Id)).Concat(permissives.Select(static item => item.Id)), "protection element");

        foreach (var channelId in functions.Select(static item => item.MeasurementChannelId)
                     .Concat(canonicalInterlocks.Select(static item => item.MeasurementChannelId))
                     .Concat(permissives.Select(static item => item.MeasurementChannelId)))
        {
            _ = instrumentation.GetChannel(channelId);
        }

        Id = id.Trim();
        TripFunctions = new ReadOnlyCollection<ProtectionFunctionDefinition>(functions);
        Interlocks = new ReadOnlyCollection<ProtectionInterlockDefinition>(canonicalInterlocks);
        ResetPermissives = new ReadOnlyCollection<ProtectionPermissiveDefinition>(permissives);
    }

    public string Id { get; }
    public IntegratedSecondaryCycleDefinition PlantDefinition { get; }
    public InstrumentationSystemDefinition Instrumentation { get; }
    public IReadOnlyList<ProtectionFunctionDefinition> TripFunctions { get; }
    public IReadOnlyList<ProtectionInterlockDefinition> Interlocks { get; }
    public IReadOnlyList<ProtectionPermissiveDefinition> ResetPermissives { get; }

    public ProtectionFunctionDefinition GetTripFunction(string id)
        => TripFunctions.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown protection function '{id}'.");

    private static void EnsureUnique(IEnumerable<string> ids, string label)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (!seen.Add(id))
            {
                throw new ArgumentException($"Duplicate {label} id '{id}'.");
            }
        }
    }
}
