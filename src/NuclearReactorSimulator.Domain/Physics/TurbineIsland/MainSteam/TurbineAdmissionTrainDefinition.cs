namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Semantic series path from a main-steam header through stop, control and admission valves to one turbine-inlet node.
/// Hydraulic behavior remains owned by the existing canonical valve definitions in <c>PlantDefinition</c>.
/// </summary>
public sealed record TurbineAdmissionTrainDefinition
{
    public TurbineAdmissionTrainDefinition(
        string id,
        string headerNodeId,
        string stopValveId,
        string controlValveId,
        string admissionValveId,
        string turbineInletNodeId)
    {
        Id = ValidateId(id, nameof(id), "Turbine-admission train");
        HeaderNodeId = ValidateId(headerNodeId, nameof(headerNodeId), "Main-steam header node");
        StopValveId = ValidateId(stopValveId, nameof(stopValveId), "Stop valve");
        ControlValveId = ValidateId(controlValveId, nameof(controlValveId), "Control valve");
        AdmissionValveId = ValidateId(admissionValveId, nameof(admissionValveId), "Admission valve");
        TurbineInletNodeId = ValidateId(turbineInletNodeId, nameof(turbineInletNodeId), "Turbine-inlet node");
    }

    public string Id { get; }

    public string HeaderNodeId { get; }

    public string StopValveId { get; }

    public string ControlValveId { get; }

    public string AdmissionValveId { get; }

    public string TurbineInletNodeId { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
