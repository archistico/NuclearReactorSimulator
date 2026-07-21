namespace NuclearReactorSimulator.Simulation.Physics.Thermal;

public sealed class ThermalBodyEnergyDepletionException : InvalidOperationException
{
    public ThermalBodyEnergyDepletionException(string thermalBodyId, double candidateEnergyJoules)
        : base($"Thermal body '{thermalBodyId}' would cross below absolute-zero energy: {candidateEnergyJoules:R} J.")
    {
        ThermalBodyId = thermalBodyId;
        CandidateEnergyJoules = candidateEnergyJoules;
    }

    public string ThermalBodyId { get; }

    public double CandidateEnergyJoules { get; }
}
