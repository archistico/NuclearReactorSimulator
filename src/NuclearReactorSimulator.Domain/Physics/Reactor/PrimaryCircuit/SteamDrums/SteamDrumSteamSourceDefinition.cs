using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

/// <summary>
/// Optional current-version steam-source closure between a steam-drum inventory and its canonical steam-outlet node.
/// A missing definition preserves the historical return-phase-split behavior.
/// </summary>
public sealed record SteamDrumSteamSourceDefinition
{
    public SteamDrumSteamSourceDefinition(QuadraticHydraulicResistance hydraulicResistance)
    {
        if (hydraulicResistance.PascalSecondsSquaredPerKilogramSquared <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hydraulicResistance),
                hydraulicResistance,
                "Steam-drum steam-source hydraulic resistance must be greater than zero.");
        }

        HydraulicResistance = hydraulicResistance;
    }

    /// <summary>
    /// Forward-only lumped resistance used to convert drum-to-steam-outlet pressure head into a maximum source mass flow.
    /// </summary>
    public QuadraticHydraulicResistance HydraulicResistance { get; }
}
