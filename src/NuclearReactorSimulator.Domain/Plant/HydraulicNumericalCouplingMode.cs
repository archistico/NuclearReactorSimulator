namespace NuclearReactorSimulator.Domain.Plant;

/// <summary>
/// Numerical pressure/flow coupling used by the composed plant-network integration boundary.
/// This is a versioned numerical-method choice, not a physical plant coefficient.
/// </summary>
public enum HydraulicNumericalCouplingMode
{
    ExplicitCommittedState = 0,
    DeterministicHybridSemiImplicit = 1,
}
