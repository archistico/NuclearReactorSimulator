namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;

/// <summary>Interpolation applied from one authored external-demand control point to the next.</summary>
public enum ExternalEnergyDemandInterpolationMode
{
    Hold = 0,
    Linear = 1,
}
