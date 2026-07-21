using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Complete immutable M4.1 inputs: validated M3 primary-circuit inputs plus one terminal turbine demand per admission train.
/// The temporary M3 steam-export sinks must be disabled while M4.1 owns the downstream steam path.
/// </summary>
public sealed class MainSteamNetworkInputs
{
    public MainSteamNetworkInputs(
        MainSteamNetworkDefinition definition,
        IntegratedPrimaryCircuitInputs primaryCircuitInputs,
        IEnumerable<TurbineAdmissionBoundaryInput> turbineAdmissionBoundaryInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        PrimaryCircuitInputs = primaryCircuitInputs ?? throw new ArgumentNullException(nameof(primaryCircuitInputs));
        ArgumentNullException.ThrowIfNull(turbineAdmissionBoundaryInputs);

        if (!ReferenceEquals(primaryCircuitInputs.Definition, definition.PrimaryCircuit))
        {
            throw new ArgumentException(
                "Primary-circuit inputs do not use the main-steam network's canonical integrated primary-circuit definition.",
                nameof(primaryCircuitInputs));
        }

        foreach (var input in primaryCircuitInputs.BoundaryInputs.SteamExportInputs)
        {
            if (input.MassFlowRate != MassFlowRate.Zero)
            {
                throw new ArgumentException(
                    $"M3 steam-export boundary '{input.BoundaryId}' must be commanded to zero while M4.1 main-steam transport is active.",
                    nameof(primaryCircuitInputs));
            }
        }

        var canonical = turbineAdmissionBoundaryInputs
            .Select(input => input ?? throw new ArgumentException(
                "Turbine-admission input collections cannot contain null entries.",
                nameof(turbineAdmissionBoundaryInputs)))
            .OrderBy(static input => input.BoundaryId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(static input => input.BoundaryId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Turbine-admission boundary input ids must be unique.", nameof(turbineAdmissionBoundaryInputs));
        }

        var expected = definition.TurbineAdmissionBoundaries
            .Select(static boundary => boundary.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var actual = canonical
            .Select(static input => input.BoundaryId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Main-steam inputs must contain exactly one input for every turbine-admission boundary. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(turbineAdmissionBoundaryInputs));
        }

        TurbineAdmissionBoundaryInputs = new ReadOnlyCollection<TurbineAdmissionBoundaryInput>(canonical);
    }

    public MainSteamNetworkDefinition Definition { get; }

    public IntegratedPrimaryCircuitInputs PrimaryCircuitInputs { get; }

    public IReadOnlyList<TurbineAdmissionBoundaryInput> TurbineAdmissionBoundaryInputs { get; }

    public TurbineAdmissionBoundaryInput GetTurbineAdmissionBoundaryInput(string boundaryId)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
        {
            throw new ArgumentException("Turbine-admission boundary id cannot be empty or whitespace.", nameof(boundaryId));
        }

        return TurbineAdmissionBoundaryInputs.FirstOrDefault(
                item => string.Equals(item.BoundaryId, boundaryId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine-admission boundary input '{boundaryId}'.");
    }
}
