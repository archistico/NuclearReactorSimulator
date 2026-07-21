namespace NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;

public static class SecondaryTransientFaultTypeIds
{
    public const string TurbineTrip = "transient.turbine-trip";
    public const string GeneratorTrip = "transient.generator-trip";
    public const string CondenserCoolingDegradation = "transient.condenser-cooling-degradation";
    public const string CondenserCoolingLoss = "transient.condenser-cooling-loss";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        TurbineTrip,
        GeneratorTrip,
        CondenserCoolingDegradation,
        CondenserCoolingLoss,
    };
}
