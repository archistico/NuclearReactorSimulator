namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Observational probe controls for M10.9.4.1-H.11. These values only localize already-observed
/// thermodynamic phase/envelope switching and never alter the production timestep, physics, trigger,
/// nonlinear-corrector tolerances or committed state.
/// </summary>
public sealed record ThermodynamicSwitchingLocalizationOptions
{
    public ThermodynamicSwitchingLocalizationOptions(
        double relativeInventoryProbe,
        double fineProbeFactor)
    {
        if (!double.IsFinite(relativeInventoryProbe) || relativeInventoryProbe <= 0d || relativeInventoryProbe > 0.01d)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeInventoryProbe), relativeInventoryProbe, "Relative inventory probe must be finite and in (0, 0.01].");
        }

        if (!double.IsFinite(fineProbeFactor) || fineProbeFactor <= 0d || fineProbeFactor >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fineProbeFactor), fineProbeFactor, "Fine-probe factor must be finite and in (0, 1).");
        }

        RelativeInventoryProbe = relativeInventoryProbe;
        FineProbeFactor = fineProbeFactor;
    }

    public static ThermodynamicSwitchingLocalizationOptions H11AuditDefault { get; } = new(
        relativeInventoryProbe: 1e-6d,
        fineProbeFactor: 0.25d);

    public double RelativeInventoryProbe { get; }

    public double FineProbeFactor { get; }
}
