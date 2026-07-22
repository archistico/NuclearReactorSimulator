namespace NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;

public static class ElectricalLossFaultTypeIds
{
    public const string ExternalSupplyLoss = "electrical.external-supply-loss";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        ExternalSupplyLoss,
    };
}
