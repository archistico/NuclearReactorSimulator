using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>
/// Immutable complete staged inputs for one integrated M3 primary-circuit step.
/// </summary>
public sealed class IntegratedPrimaryCircuitInputs
{
    public IntegratedPrimaryCircuitInputs(
        IntegratedPrimaryCircuitDefinition definition,
        AggregatedCoreState coreState,
        Power totalFissionThermalPower,
        Power totalDecayHeatPower,
        PrimaryCircuitBoundaryInputs boundaryInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CoreState = coreState ?? throw new ArgumentNullException(nameof(coreState));
        BoundaryInputs = boundaryInputs ?? throw new ArgumentNullException(nameof(boundaryInputs));

        if (!ReferenceEquals(coreState.Definition, definition.CoreDefinition))
        {
            throw new ArgumentException(
                "Aggregated-core state does not use the integrated primary circuit's canonical core definition.",
                nameof(coreState));
        }

        if (!ReferenceEquals(boundaryInputs.Definition, definition.BoundarySystem))
        {
            throw new ArgumentException(
                "Boundary inputs do not use the integrated primary circuit's canonical boundary definition.",
                nameof(boundaryInputs));
        }

        if (totalFissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalFissionThermalPower),
                totalFissionThermalPower,
                "Total fission thermal power cannot be negative.");
        }

        if (totalDecayHeatPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalDecayHeatPower),
                totalDecayHeatPower,
                "Total decay-heat power cannot be negative.");
        }

        TotalFissionThermalPower = totalFissionThermalPower;
        TotalDecayHeatPower = totalDecayHeatPower;
    }

    public IntegratedPrimaryCircuitDefinition Definition { get; }

    public AggregatedCoreState CoreState { get; }

    public Power TotalFissionThermalPower { get; }

    public Power TotalDecayHeatPower { get; }

    public PrimaryCircuitBoundaryInputs BoundaryInputs { get; }
}
