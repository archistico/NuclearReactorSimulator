using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Pressure-actuated external main-steam relief path. The source remains a canonical plant fluid node while the
/// receiver is an explicitly named boundary at fixed pressure. F.2 intentionally owns no downstream receiver
/// inventory, turbine bypass mixing, manual authority or two-phase critical-flow model.
/// </summary>
public sealed class MainSteamReliefBoundaryDefinition
{
    public MainSteamReliefBoundaryDefinition(
        string id,
        string sourceHeaderNodeId,
        string receiverBoundaryId,
        Pressure receiverPressure,
        Pressure setPressure,
        Pressure fullLiftPressure,
        CompressibleSteamFlowDefinition flowDefinition)
    {
        Id = ValidateId(id, nameof(id), "Main-steam relief boundary");
        SourceHeaderNodeId = ValidateId(sourceHeaderNodeId, nameof(sourceHeaderNodeId), "Relief source-header node");
        ReceiverBoundaryId = ValidateId(receiverBoundaryId, nameof(receiverBoundaryId), "Relief receiver boundary");
        FlowDefinition = flowDefinition ?? throw new ArgumentNullException(nameof(flowDefinition));

        if (receiverPressure >= setPressure)
        {
            throw new ArgumentException(
                "Main-steam relief receiver pressure must be below the relief set pressure.",
                nameof(receiverPressure));
        }

        if (fullLiftPressure <= setPressure)
        {
            throw new ArgumentException(
                "Main-steam relief full-lift pressure must be greater than the relief set pressure.",
                nameof(fullLiftPressure));
        }

        ReceiverPressure = receiverPressure;
        SetPressure = setPressure;
        FullLiftPressure = fullLiftPressure;
    }

    public string Id { get; }

    public string SourceHeaderNodeId { get; }

    public string ReceiverBoundaryId { get; }

    public Pressure ReceiverPressure { get; }

    public Pressure SetPressure { get; }

    public Pressure FullLiftPressure { get; }

    public CompressibleSteamFlowDefinition FlowDefinition { get; }

    public double CalculateLiftFraction(Pressure sourcePressure)
    {
        if (sourcePressure <= SetPressure)
        {
            return 0d;
        }

        if (sourcePressure >= FullLiftPressure)
        {
            return 1d;
        }

        return (sourcePressure.Pascals - SetPressure.Pascals)
            / (FullLiftPressure.Pascals - SetPressure.Pascals);
    }

    private static string ValidateId(string id, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return id.Trim();
    }
}
