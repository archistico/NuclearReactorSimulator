using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>
/// Immutable plant-level M3 snapshot collecting the complete primary-circuit diagnostics produced from one committed state
/// and the candidate plant state produced by the single network integration boundary.
/// </summary>
public sealed class IntegratedPrimaryCircuitSnapshot
{
    public IntegratedPrimaryCircuitSnapshot(
        IntegratedPrimaryCircuitDefinition definition,
        AggregatedCoreSnapshot core,
        FuelChannelGroupSetSnapshot channelGroups,
        MainCirculationSystemSnapshot mainCirculation,
        SteamDrumSystemSnapshot steamDrums,
        PrimaryCircuitBoundarySystemSnapshot boundaries,
        PlantSnapshot candidatePlant,
        PlantNetworkAudit audit)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Core = core ?? throw new ArgumentNullException(nameof(core));
        ChannelGroups = channelGroups ?? throw new ArgumentNullException(nameof(channelGroups));
        MainCirculation = mainCirculation ?? throw new ArgumentNullException(nameof(mainCirculation));
        SteamDrums = steamDrums ?? throw new ArgumentNullException(nameof(steamDrums));
        Boundaries = boundaries ?? throw new ArgumentNullException(nameof(boundaries));
        CandidatePlant = candidatePlant ?? throw new ArgumentNullException(nameof(candidatePlant));
        Audit = audit ?? throw new ArgumentNullException(nameof(audit));

        if (!ReferenceEquals(core.Definition, definition.CoreDefinition)
            || !string.Equals(channelGroups.DefinitionId, definition.ChannelGroups.Id, StringComparison.Ordinal)
            || !ReferenceEquals(mainCirculation.Definition, definition.MainCirculationSystem)
            || !ReferenceEquals(steamDrums.Definition, definition.SteamDrumSystem)
            || !ReferenceEquals(boundaries.Definition, definition.BoundarySystem)
            || !ReferenceEquals(candidatePlant.Definition, definition.PlantDefinition))
        {
            throw new ArgumentException("Integrated primary-circuit snapshot components do not share the canonical integrated definition.");
        }

        TotalPlantMass = audit.FinalTotalMass;
        TotalStoredEnergy = audit.FinalTotalStoredEnergy;
        TotalFeedwaterMassFlowRate = MassFlowRate.FromKilogramsPerSecond(
            CompensatedSum(boundaries.FeedwaterBoundaries.Select(static item => item.MassFlowRate.KilogramsPerSecond)));
        TotalSteamExportMassFlowRate = MassFlowRate.FromKilogramsPerSecond(
            CompensatedSum(boundaries.SteamExportBoundaries.Select(static item => item.MassFlowRate.KilogramsPerSecond)));
    }

    public IntegratedPrimaryCircuitDefinition Definition { get; }

    public AggregatedCoreSnapshot Core { get; }

    public FuelChannelGroupSetSnapshot ChannelGroups { get; }

    public MainCirculationSystemSnapshot MainCirculation { get; }

    public SteamDrumSystemSnapshot SteamDrums { get; }

    public PrimaryCircuitBoundarySystemSnapshot Boundaries { get; }

    public PlantSnapshot CandidatePlant { get; }

    public PlantNetworkAudit Audit { get; }

    public Power TotalFissionThermalPower => Core.TotalFissionThermalPower;

    public Power TotalDecayHeatPower => ChannelGroups.TotalDecayHeatPower;

    public Power TotalNuclearHeatPower => ChannelGroups.TotalNuclearHeatPower;

    public MassFlowRate TotalFeedwaterMassFlowRate { get; }

    public MassFlowRate TotalSteamExportMassFlowRate { get; }

    public Mass TotalPlantMass { get; }

    public Energy TotalStoredEnergy { get; }

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;
        foreach (var value in values)
        {
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }
}
