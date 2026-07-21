using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed class MainSteamNetworkSnapshot
{
    public MainSteamNetworkSnapshot(
        MainSteamNetworkDefinition definition,
        IntegratedPrimaryCircuitSnapshot primaryCircuit,
        IEnumerable<MainSteamLineSnapshot> steamLines,
        IEnumerable<TurbineAdmissionTrainSnapshot> admissionTrains,
        IEnumerable<TurbineAdmissionBoundarySnapshot> turbineAdmissionBoundaries)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        PrimaryCircuit = primaryCircuit ?? throw new ArgumentNullException(nameof(primaryCircuit));
        ArgumentNullException.ThrowIfNull(steamLines);
        ArgumentNullException.ThrowIfNull(admissionTrains);
        ArgumentNullException.ThrowIfNull(turbineAdmissionBoundaries);

        if (!ReferenceEquals(primaryCircuit.Definition, definition.PrimaryCircuit))
        {
            throw new ArgumentException(
                "Primary-circuit snapshot does not use the main-steam network's canonical primary-circuit definition.",
                nameof(primaryCircuit));
        }

        var canonicalLines = steamLines.OrderBy(static item => item.LineId, StringComparer.Ordinal).ToArray();
        var canonicalTrains = admissionTrains.OrderBy(static item => item.TrainId, StringComparer.Ordinal).ToArray();
        var canonicalBoundaries = turbineAdmissionBoundaries.OrderBy(static item => item.BoundaryId, StringComparer.Ordinal).ToArray();

        ValidateExactSet(
            definition.SteamLines.Select(static item => item.Id),
            canonicalLines.Select(static item => item.LineId),
            "main-steam line");
        ValidateExactSet(
            definition.AdmissionTrains.Select(static item => item.Id),
            canonicalTrains.Select(static item => item.TrainId),
            "turbine-admission train");
        ValidateExactSet(
            definition.TurbineAdmissionBoundaries.Select(static item => item.Id),
            canonicalBoundaries.Select(static item => item.BoundaryId),
            "turbine-admission boundary");

        SteamLines = new ReadOnlyCollection<MainSteamLineSnapshot>(canonicalLines);
        AdmissionTrains = new ReadOnlyCollection<TurbineAdmissionTrainSnapshot>(canonicalTrains);
        TurbineAdmissionBoundaries = new ReadOnlyCollection<TurbineAdmissionBoundarySnapshot>(canonicalBoundaries);
        TotalSteamLineMassFlowRate = SumMassFlow(canonicalLines.Select(static item => item.MassFlowRate));
        TotalTurbineAdmissionMassFlowRate = SumMassFlow(canonicalBoundaries.Select(static item => item.MassFlowRate));
        TotalTurbineAdmissionEnergyExportRate = SumPower(canonicalBoundaries.Select(static item => item.EnergyExportRate));
    }

    public MainSteamNetworkDefinition Definition { get; }

    public IntegratedPrimaryCircuitSnapshot PrimaryCircuit { get; }

    public IReadOnlyList<MainSteamLineSnapshot> SteamLines { get; }

    public IReadOnlyList<TurbineAdmissionTrainSnapshot> AdmissionTrains { get; }

    public IReadOnlyList<TurbineAdmissionBoundarySnapshot> TurbineAdmissionBoundaries { get; }

    public MassFlowRate TotalSteamLineMassFlowRate { get; }

    public MassFlowRate TotalTurbineAdmissionMassFlowRate { get; }

    public Power TotalTurbineAdmissionEnergyExportRate { get; }

    public PlantNetworkAudit Audit => PrimaryCircuit.Audit;

    public MainSteamLineSnapshot GetSteamLine(string id)
        => SteamLines.FirstOrDefault(item => string.Equals(item.LineId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown main-steam line '{id}'.");

    public TurbineAdmissionTrainSnapshot GetAdmissionTrain(string id)
        => AdmissionTrains.FirstOrDefault(item => string.Equals(item.TrainId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine-admission train '{id}'.");

    public TurbineAdmissionBoundarySnapshot GetTurbineAdmissionBoundary(string id)
        => TurbineAdmissionBoundaries.FirstOrDefault(item => string.Equals(item.BoundaryId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine-admission boundary '{id}'.");


    private static void ValidateExactSet(IEnumerable<string> expectedIds, IEnumerable<string> actualIds, string label)
    {
        var expected = expectedIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = actualIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Main-steam snapshot must contain exactly one snapshot per defined {label}. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }

    private static MassFlowRate SumMassFlow(IEnumerable<MassFlowRate> values)
        => MassFlowRate.FromKilogramsPerSecond(CompensatedSum(values.Select(static value => value.KilogramsPerSecond)));

    private static Power SumPower(IEnumerable<Power> values)
        => Power.FromWatts(CompensatedSum(values.Select(static value => value.Watts)));

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
