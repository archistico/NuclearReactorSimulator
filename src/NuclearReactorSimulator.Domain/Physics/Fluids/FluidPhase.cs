namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Coarse thermodynamic phase classification exposed by a fluid closure model.
/// </summary>
public enum FluidPhase
{
    Unspecified = 0,
    SubcooledLiquid = 1,
    SaturatedMixture = 2,
    SuperheatedVapor = 3,
}
