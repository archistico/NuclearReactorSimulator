namespace NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;

public static class LossOfCoolantFaultTypeIds
{
    public const string PressureDrivenBreak = "loca.pressure-driven-break";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        PressureDrivenBreak,
    };
}
