using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>M5.5 logical latch state only. It owns no physical plant inventory or actuator position.</summary>
public sealed class ProtectionSystemState
{
    public ProtectionSystemState(
        ProtectionSystemDefinition definition,
        IEnumerable<ProtectionFunctionLatchState> functionLatches,
        ProtectionAction manualLatchedActions = ProtectionAction.None)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(functionLatches);

        var canonical = functionLatches
            .Select(item => item ?? throw new ArgumentException("Protection latch state cannot contain null entries.", nameof(functionLatches)))
            .OrderBy(static item => item.FunctionId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.TripFunctions.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.FunctionId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Protection state must contain exactly one latch state per trip function.", nameof(functionLatches));
        }
        if ((manualLatchedActions & ~(ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip)) != ProtectionAction.None)
        {
            throw new ArgumentOutOfRangeException(nameof(manualLatchedActions), manualLatchedActions, "Unsupported manual protection action latch.");
        }

        FunctionLatches = new ReadOnlyCollection<ProtectionFunctionLatchState>(canonical);
        ManualLatchedActions = manualLatchedActions;
    }

    public ProtectionSystemDefinition Definition { get; }
    public IReadOnlyList<ProtectionFunctionLatchState> FunctionLatches { get; }
    public ProtectionAction ManualLatchedActions { get; }

    public bool IsFunctionLatched(string functionId)
        => FunctionLatches.FirstOrDefault(item => string.Equals(item.FunctionId, functionId, StringComparison.Ordinal))?.IsLatched
            ?? throw new KeyNotFoundException($"Unknown protection function latch '{functionId}'.");

    public static ProtectionSystemState CreateInitial(ProtectionSystemDefinition definition)
        => new(definition, definition.TripFunctions.Select(static item => new ProtectionFunctionLatchState(item.Id, false)));
}
