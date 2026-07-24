namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// Declares whether an infinite-bus coupling may only oppose the turbine in generator mode or may also
/// apply signed motoring torque from the grid. The legacy default preserves generation-only semantics.
/// </summary>
public enum SynchronousGridPowerFlowMode
{
    GenerationOnly = 0,
    Bidirectional = 1,
}
