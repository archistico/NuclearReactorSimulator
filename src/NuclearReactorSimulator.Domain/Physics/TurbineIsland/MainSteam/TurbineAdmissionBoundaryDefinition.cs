using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Replaceable M4.1 terminal steam sink at one turbine-admission inlet.
/// M4.2 replaces this temporary external boundary with the turbine expansion/rotor model without changing upstream topology.
/// </summary>
public sealed record TurbineAdmissionBoundaryDefinition
{
    public TurbineAdmissionBoundaryDefinition(
        string id,
        string admissionTrainId,
        string sourceNodeId,
        FluidEnergyTransportMode energyTransportMode = FluidEnergyTransportMode.SpecificInternalEnergy)
    {
        Id = ValidateId(id, nameof(id), "Turbine-admission boundary");
        AdmissionTrainId = ValidateId(admissionTrainId, nameof(admissionTrainId), "Turbine-admission train");
        SourceNodeId = ValidateId(sourceNodeId, nameof(sourceNodeId), "Turbine-admission source node");
        if (!Enum.IsDefined(energyTransportMode))
        {
            throw new ArgumentOutOfRangeException(nameof(energyTransportMode), energyTransportMode, "Unsupported turbine-admission energy-transport mode.");
        }

        EnergyTransportMode = energyTransportMode;
    }

    public string Id { get; }

    public string AdmissionTrainId { get; }

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
