namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;

/// <summary>
/// Immutable semantic mapping of one equivalent fuel-channel group to its passive return path.
/// The channel hydraulic path itself remains owned by the canonical fuel-channel-group definition.
/// </summary>
public sealed record MainCirculationBranchDefinition
{
    public MainCirculationBranchDefinition(string fuelChannelGroupId, string returnPipeId)
    {
        FuelChannelGroupId = ValidateId(fuelChannelGroupId, nameof(fuelChannelGroupId), "Fuel-channel group");
        ReturnPipeId = ValidateId(returnPipeId, nameof(returnPipeId), "Return pipe");
    }

    public string FuelChannelGroupId { get; }

    public string ReturnPipeId { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
