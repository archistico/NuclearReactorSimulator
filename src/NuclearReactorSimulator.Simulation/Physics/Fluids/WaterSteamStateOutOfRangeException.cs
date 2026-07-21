namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

public sealed class WaterSteamStateOutOfRangeException : InvalidOperationException
{
    public WaterSteamStateOutOfRangeException(
        string nodeId,
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram)
        : base($"Fluid node '{nodeId}' is outside the supported simplified water/steam state envelope " +
               $"(v={specificVolumeCubicMetresPerKilogram:G17} m^3/kg, u={specificInternalEnergyJoulesPerKilogram:G17} J/kg).")
    {
        NodeId = nodeId;
        SpecificVolumeCubicMetresPerKilogram = specificVolumeCubicMetresPerKilogram;
        SpecificInternalEnergyJoulesPerKilogram = specificInternalEnergyJoulesPerKilogram;
    }

    public string NodeId { get; }

    public double SpecificVolumeCubicMetresPerKilogram { get; }

    public double SpecificInternalEnergyJoulesPerKilogram { get; }
}
