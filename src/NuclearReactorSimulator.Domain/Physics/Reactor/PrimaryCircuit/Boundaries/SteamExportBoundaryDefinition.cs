using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Semantic external steam-export sink boundary for one steam drum.
/// The source is an existing canonical steam-outlet fluid node; no duplicate inventory is introduced.
/// </summary>
public sealed record SteamExportBoundaryDefinition
{
    public SteamExportBoundaryDefinition(
        string id,
        string steamDrumId,
        string sourceNodeId,
        FluidEnergyTransportMode energyTransportMode = FluidEnergyTransportMode.SpecificInternalEnergy)
    {
        Id = ValidateId(id, nameof(id), "Steam-export boundary");
        SteamDrumId = ValidateId(steamDrumId, nameof(steamDrumId), "Steam drum");
        SourceNodeId = ValidateId(sourceNodeId, nameof(sourceNodeId), "Steam-export source node");
        if (!Enum.IsDefined(energyTransportMode))
        {
            throw new ArgumentOutOfRangeException(nameof(energyTransportMode), energyTransportMode, "Unsupported steam-export energy-transport mode.");
        }

        EnergyTransportMode = energyTransportMode;
    }

    public string Id { get; }

    public string SteamDrumId { get; }

    public string SourceNodeId { get; }

    public FluidEnergyTransportMode EnergyTransportMode { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
