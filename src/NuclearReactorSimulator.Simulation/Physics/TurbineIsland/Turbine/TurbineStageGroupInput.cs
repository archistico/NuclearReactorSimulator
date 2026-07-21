using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

public sealed record TurbineStageGroupInput
{
    public TurbineStageGroupInput(string stageGroupId, MassFlowRate massFlowRate)
    {
        if (string.IsNullOrWhiteSpace(stageGroupId))
        {
            throw new ArgumentException("Turbine stage-group input id cannot be empty or whitespace.", nameof(stageGroupId));
        }

        if (massFlowRate < MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(massFlowRate), massFlowRate, "Turbine stage-group mass flow cannot be negative.");
        }

        StageGroupId = stageGroupId.Trim();
        MassFlowRate = massFlowRate;
    }

    public string StageGroupId { get; }

    public MassFlowRate MassFlowRate { get; }
}
