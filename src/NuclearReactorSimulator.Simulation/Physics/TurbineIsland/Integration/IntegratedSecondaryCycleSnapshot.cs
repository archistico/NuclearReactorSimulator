using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>Immutable M4.6 plant-level secondary-cycle and electrical heat-balance snapshot.</summary>
public sealed class IntegratedSecondaryCycleSnapshot
{
    public IntegratedSecondaryCycleSnapshot(
        IntegratedSecondaryCycleDefinition definition,
        GeneratorGridSnapshot generatorGrid,
        SecondaryCycleHeatBalanceAudit heatBalance)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        GeneratorGrid = generatorGrid ?? throw new ArgumentNullException(nameof(generatorGrid));
        HeatBalance = heatBalance ?? throw new ArgumentNullException(nameof(heatBalance));

        if (!ReferenceEquals(generatorGrid.Definition, definition.GeneratorGridSystem))
        {
            throw new ArgumentException(
                "Generator/grid snapshot does not use the integrated secondary cycle's canonical M4.5 definition.",
                nameof(generatorGrid));
        }
    }

    public IntegratedSecondaryCycleDefinition Definition { get; }

    public GeneratorGridSnapshot GeneratorGrid { get; }

    public CondensateFeedwaterSystemSnapshot CondensateFeedwater => GeneratorGrid.CondensateFeedwater;

    public CondenserSystemSnapshot Condenser => CondensateFeedwater.CondenserSnapshot;

    public TurbineExpansionSnapshot TurbineExpansion => GeneratorGrid.TurbineExpansion;

    public IntegratedPrimaryCircuitSnapshot PrimaryCircuit => TurbineExpansion.MainSteamNetwork.PrimaryCircuit;

    public IReadOnlyList<SynchronousGeneratorSnapshot> Generators => GeneratorGrid.Generators;

    public SecondaryCycleHeatBalanceAudit HeatBalance { get; }

    public PlantNetworkAudit ThermofluidAudit => CondensateFeedwater.ThermofluidAudit;

    public Power TotalNuclearHeatPower => PrimaryCircuit.TotalNuclearHeatPower;

    public Power TotalTurbineShaftPower => TurbineExpansion.TotalShaftPower;

    public Power TotalCondenserHeatRejectionPower => Condenser.TotalHeatRejectionPower;

    public Power TotalElectricalOutputPower => GeneratorGrid.TotalElectricalOutputPower;
}
