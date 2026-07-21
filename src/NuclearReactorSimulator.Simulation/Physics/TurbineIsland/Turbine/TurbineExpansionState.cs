using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Canonical committed mechanical state for the M4.2 turbine expansion system.
/// </summary>
public sealed class TurbineExpansionState
{
    public TurbineExpansionState(
        TurbineExpansionSystemDefinition definition,
        IEnumerable<TurbineRotorState> rotors)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(rotors);

        var canonical = rotors
            .Select(item => item ?? throw new ArgumentException("Turbine rotor-state collections cannot contain null entries.", nameof(rotors)))
            .OrderBy(static item => item.RotorId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(static item => item.RotorId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Turbine rotor-state ids must be unique.", nameof(rotors));
        }

        var expected = definition.Rotors.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.RotorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Turbine expansion state must contain exactly one state per rotor. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(rotors));
        }

        Rotors = new ReadOnlyCollection<TurbineRotorState>(canonical);
    }

    public TurbineExpansionSystemDefinition Definition { get; }

    public IReadOnlyList<TurbineRotorState> Rotors { get; }

    public TurbineRotorState GetRotor(string rotorId)
    {
        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Rotor id cannot be empty or whitespace.", nameof(rotorId));
        }

        return Rotors.FirstOrDefault(item => string.Equals(item.RotorId, rotorId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine rotor state '{rotorId}'.");
    }

    public static TurbineExpansionState CreateStopped(TurbineExpansionSystemDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new TurbineExpansionState(
            definition,
            definition.Rotors.Select(static rotor => new TurbineRotorState(rotor.Id, AngularSpeed.Zero)));
    }
}
