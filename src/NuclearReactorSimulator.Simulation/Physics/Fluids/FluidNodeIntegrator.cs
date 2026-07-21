using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Deterministically integrates conserved mass and internal-energy balances for one lumped fluid node.
/// </summary>
public sealed class FluidNodeIntegrator
{
    private readonly IFluidThermodynamicModel _thermodynamicModel;

    public FluidNodeIntegrator(IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _thermodynamicModel = thermodynamicModel;
    }

    public FluidNodeState Step(
        FluidNodeState state,
        FluidNodeBalance balance,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Fluid-node integration time must be greater than zero.");
        }

        var seconds = deltaTime.TotalSeconds;
        var candidateMassKilograms = state.Mass.Kilograms + (balance.NetMassFlowRate.KilogramsPerSecond * seconds);

        if (!double.IsFinite(candidateMassKilograms))
        {
            throw new ArithmeticException($"Fluid node '{state.Id}' mass integration produced a non-finite result.");
        }

        if (candidateMassKilograms <= 0d)
        {
            throw new FluidNodeDepletionException(state.Id, candidateMassKilograms);
        }

        var candidateInventory = new FluidNodeInventory(
            Mass.FromKilograms(candidateMassKilograms),
            state.InternalEnergy + balance.NetEnergyRate.Over(deltaTime));

        var candidateThermodynamics = _thermodynamicModel.Resolve(
            state.Definition,
            candidateInventory,
            state.Thermodynamics)
            ?? throw new InvalidOperationException("The fluid thermodynamic model returned no state.");

        return new FluidNodeState(
            state.Definition,
            candidateInventory,
            candidateThermodynamics);
    }
}
