using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;

/// <summary>
/// Automatic pressure-actuated steam-dump path from one canonical main-steam header to one condenser steam space.
/// The destination pressure is resolved from committed condenser state at runtime; F.3 intentionally adds no manual
/// authority, actuator travel, hysteresis, downstream receiver inventory or flow-work/enthalpy migration.
/// </summary>
public sealed class TurbineBypassDefinition
{
    public TurbineBypassDefinition(
        string id,
        string sourceHeaderNodeId,
        string condenserId,
        Pressure setPressure,
        Pressure fullOpenPressure,
        CompressibleSteamFlowDefinition flowDefinition)
    {
        Id = ValidateId(id, nameof(id), "Turbine-bypass");
        SourceHeaderNodeId = ValidateId(sourceHeaderNodeId, nameof(sourceHeaderNodeId), "Turbine-bypass source-header node");
        CondenserId = ValidateId(condenserId, nameof(condenserId), "Turbine-bypass condenser");
        FlowDefinition = flowDefinition ?? throw new ArgumentNullException(nameof(flowDefinition));

        if (fullOpenPressure <= setPressure)
        {
            throw new ArgumentException(
                "Turbine-bypass full-open pressure must be greater than its set pressure.",
                nameof(fullOpenPressure));
        }

        SetPressure = setPressure;
        FullOpenPressure = fullOpenPressure;
    }

    public string Id { get; }

    public string SourceHeaderNodeId { get; }

    public string CondenserId { get; }

    public Pressure SetPressure { get; }

    public Pressure FullOpenPressure { get; }

    public CompressibleSteamFlowDefinition FlowDefinition { get; }

    public double CalculateOpenFraction(Pressure sourcePressure)
    {
        if (sourcePressure <= SetPressure)
        {
            return 0d;
        }

        if (sourcePressure >= FullOpenPressure)
        {
            return 1d;
        }

        return (sourcePressure.Pascals - SetPressure.Pascals)
            / (FullOpenPressure.Pascals - SetPressure.Pascals);
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
