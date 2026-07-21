using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Complete M4.2 inputs. The temporary M4.1 terminal boundary sinks must be zero while turbine expansion owns the seam.
/// </summary>
public sealed class TurbineExpansionInputs
{
    public TurbineExpansionInputs(
        TurbineExpansionSystemDefinition definition,
        MainSteamNetworkInputs mainSteamInputs,
        IEnumerable<TurbineStageGroupInput> stageGroupInputs,
        IEnumerable<TurbineRotorInput> rotorInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        MainSteamInputs = mainSteamInputs ?? throw new ArgumentNullException(nameof(mainSteamInputs));
        ArgumentNullException.ThrowIfNull(stageGroupInputs);
        ArgumentNullException.ThrowIfNull(rotorInputs);

        if (!ReferenceEquals(mainSteamInputs.Definition, definition.MainSteamNetwork))
        {
            throw new ArgumentException("Main-steam inputs do not use the turbine expansion system's canonical M4.1 definition.", nameof(mainSteamInputs));
        }

        foreach (var boundaryInput in mainSteamInputs.TurbineAdmissionBoundaryInputs)
        {
            if (boundaryInput.MassFlowRate != MassFlowRate.Zero)
            {
                throw new ArgumentException(
                    $"M4.1 turbine-admission boundary '{boundaryInput.BoundaryId}' must be zero while M4.2 expansion owns the turbine inlet seam.",
                    nameof(mainSteamInputs));
            }
        }

        var canonicalStageInputs = stageGroupInputs
            .Select(item => item ?? throw new ArgumentException("Turbine stage-group input collections cannot contain null entries.", nameof(stageGroupInputs)))
            .OrderBy(static item => item.StageGroupId, StringComparer.Ordinal)
            .ToArray();
        var canonicalRotorInputs = rotorInputs
            .Select(item => item ?? throw new ArgumentException("Turbine rotor input collections cannot contain null entries.", nameof(rotorInputs)))
            .OrderBy(static item => item.RotorId, StringComparer.Ordinal)
            .ToArray();

        ValidateExactSet(
            definition.StageGroups.Select(static item => item.Id),
            canonicalStageInputs.Select(static item => item.StageGroupId),
            "stage-group input");
        ValidateExactSet(
            definition.Rotors.Select(static item => item.Id),
            canonicalRotorInputs.Select(static item => item.RotorId),
            "rotor input");

        StageGroupInputs = new ReadOnlyCollection<TurbineStageGroupInput>(canonicalStageInputs);
        RotorInputs = new ReadOnlyCollection<TurbineRotorInput>(canonicalRotorInputs);
    }

    public TurbineExpansionSystemDefinition Definition { get; }

    public MainSteamNetworkInputs MainSteamInputs { get; }

    public IReadOnlyList<TurbineStageGroupInput> StageGroupInputs { get; }

    public IReadOnlyList<TurbineRotorInput> RotorInputs { get; }

    public TurbineStageGroupInput GetStageGroupInput(string id)
        => StageGroupInputs.FirstOrDefault(item => string.Equals(item.StageGroupId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine stage-group input '{id}'.");

    public TurbineRotorInput GetRotorInput(string id)
        => RotorInputs.FirstOrDefault(item => string.Equals(item.RotorId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine rotor input '{id}'.");

    private static void ValidateExactSet(IEnumerable<string> expectedIds, IEnumerable<string> actualIds, string label)
    {
        var expected = expectedIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = actualIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length
            || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Turbine expansion inputs must contain exactly one {label} per definition. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }
}
