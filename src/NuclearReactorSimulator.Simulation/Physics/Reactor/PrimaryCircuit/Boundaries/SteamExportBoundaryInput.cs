using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Per-step controllable steam-export boundary input.
/// Exported specific energy is read from the committed canonical steam-outlet node.
/// </summary>
public sealed record SteamExportBoundaryInput
{
    public SteamExportBoundaryInput(string boundaryId, MassFlowRate massFlowRate)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
        {
            throw new ArgumentException("Steam-export boundary id cannot be empty or whitespace.", nameof(boundaryId));
        }

        if (massFlowRate < MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(massFlowRate), massFlowRate, "Steam-export mass flow cannot be negative.");
        }

        BoundaryId = boundaryId.Trim();
        MassFlowRate = massFlowRate;
    }

    public string BoundaryId { get; }

    public MassFlowRate MassFlowRate { get; }
}
