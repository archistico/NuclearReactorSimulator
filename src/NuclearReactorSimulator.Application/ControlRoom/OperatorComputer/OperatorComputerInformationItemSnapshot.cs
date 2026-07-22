namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerInformationItemSnapshot
{
    public OperatorComputerInformationItemSnapshot(
        string label,
        string valueText,
        string unit,
        OperatorComputerInformationProvenance provenance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueText);
        if (!Enum.IsDefined(provenance))
        {
            throw new ArgumentOutOfRangeException(nameof(provenance));
        }

        Label = label;
        ValueText = valueText;
        Unit = unit ?? string.Empty;
        Provenance = provenance;
    }

    public string Label { get; }
    public string ValueText { get; }
    public string Unit { get; }
    public OperatorComputerInformationProvenance Provenance { get; }
}
