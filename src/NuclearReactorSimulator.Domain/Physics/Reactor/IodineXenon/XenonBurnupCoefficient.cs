namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Non-negative first-order Xe-135 removal coefficient per unit normalized neutron population.
/// </summary>
public readonly record struct XenonBurnupCoefficient : IComparable<XenonBurnupCoefficient>
{
    private XenonBurnupCoefficient(double perSecondPerRelativeNeutronPopulation)
    {
        if (!double.IsFinite(perSecondPerRelativeNeutronPopulation) || perSecondPerRelativeNeutronPopulation < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(perSecondPerRelativeNeutronPopulation),
                perSecondPerRelativeNeutronPopulation,
                "Xenon burnup coefficient must be finite and non-negative.");
        }

        PerSecondPerRelativeNeutronPopulation = perSecondPerRelativeNeutronPopulation == 0d
            ? 0d
            : perSecondPerRelativeNeutronPopulation;
    }

    public double PerSecondPerRelativeNeutronPopulation { get; }

    public static XenonBurnupCoefficient Zero { get; } = FromPerSecondPerRelativeNeutronPopulation(0d);

    public static XenonBurnupCoefficient FromPerSecondPerRelativeNeutronPopulation(double value) => new(value);

    public int CompareTo(XenonBurnupCoefficient other)
        => PerSecondPerRelativeNeutronPopulation.CompareTo(other.PerSecondPerRelativeNeutronPopulation);
}
