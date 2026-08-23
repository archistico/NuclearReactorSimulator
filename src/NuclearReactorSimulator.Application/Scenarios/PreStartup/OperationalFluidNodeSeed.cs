namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

/// <summary>
/// Internal authored-state seam for versioned operational initial conditions that need to seed a fluid node from
/// a thermodynamically explicit state instead of the historical common-temperature recipe. It is intentionally
/// internal so exact-version factories can add evidence-backed operating points without changing the public runtime API.
/// </summary>
internal abstract record OperationalFluidNodeSeed(string NodeId)
{
    internal sealed record SaturatedMixture(
        string NodeId,
        double PressureMegapascals,
        double VaporQualityFraction)
        : OperationalFluidNodeSeed(NodeId);

    internal sealed record SubcooledLiquid(
        string NodeId,
        double TemperatureCelsius,
        double CompressionFraction)
        : OperationalFluidNodeSeed(NodeId);
}
