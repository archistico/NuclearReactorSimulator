using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

public sealed class TurbineExpansionSnapshot
{
    public TurbineExpansionSnapshot(
        TurbineExpansionSystemDefinition definition,
        MainSteamNetworkSnapshot mainSteamNetwork,
        IEnumerable<TurbineStageGroupSnapshot> stageGroups,
        IEnumerable<TurbineRotorSnapshot> rotors,
        TurbineMechanicalAudit mechanicalAudit)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        MainSteamNetwork = mainSteamNetwork ?? throw new ArgumentNullException(nameof(mainSteamNetwork));
        ArgumentNullException.ThrowIfNull(stageGroups);
        ArgumentNullException.ThrowIfNull(rotors);
        MechanicalAudit = mechanicalAudit ?? throw new ArgumentNullException(nameof(mechanicalAudit));

        if (!ReferenceEquals(mainSteamNetwork.Definition, definition.MainSteamNetwork))
        {
            throw new ArgumentException("Main-steam snapshot does not use the turbine expansion system's canonical M4.1 definition.", nameof(mainSteamNetwork));
        }

        var canonicalStageGroups = stageGroups.OrderBy(static item => item.StageGroupId, StringComparer.Ordinal).ToArray();
        var canonicalRotors = rotors.OrderBy(static item => item.RotorId, StringComparer.Ordinal).ToArray();
        ValidateExactSet(definition.StageGroups.Select(static item => item.Id), canonicalStageGroups.Select(static item => item.StageGroupId), "stage group");
        ValidateExactSet(definition.Rotors.Select(static item => item.Id), canonicalRotors.Select(static item => item.RotorId), "rotor");

        StageGroups = new ReadOnlyCollection<TurbineStageGroupSnapshot>(canonicalStageGroups);
        Rotors = new ReadOnlyCollection<TurbineRotorSnapshot>(canonicalRotors);
        TotalSteamMassFlowRate = SumMassFlow(canonicalStageGroups.Select(static item => item.EffectiveMassFlowRate));
        TotalShaftPower = SumPower(canonicalStageGroups.Select(static item => item.ShaftPower));
        TotalExternalLoadPower = SumPower(canonicalRotors.Select(static item => item.ExternalLoadPower));
    }

    public TurbineExpansionSystemDefinition Definition { get; }

    public MainSteamNetworkSnapshot MainSteamNetwork { get; }

    public IReadOnlyList<TurbineStageGroupSnapshot> StageGroups { get; }

    public IReadOnlyList<TurbineRotorSnapshot> Rotors { get; }

    public TurbineMechanicalAudit MechanicalAudit { get; }

    public MassFlowRate TotalSteamMassFlowRate { get; }

    public Power TotalShaftPower { get; }

    public Power TotalExternalLoadPower { get; }

    public PlantNetworkAudit ThermofluidAudit => MainSteamNetwork.Audit;

    public double CoupledEnergyClosureResidualJoules
        => ThermofluidAudit.EnergyClosureResidualJoules + MechanicalAudit.MechanicalEnergyClosureResidualJoules;

    public TurbineStageGroupSnapshot GetStageGroup(string id)
        => StageGroups.FirstOrDefault(item => string.Equals(item.StageGroupId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine stage-group snapshot '{id}'.");

    public TurbineRotorSnapshot GetRotor(string id)
        => Rotors.FirstOrDefault(item => string.Equals(item.RotorId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine rotor snapshot '{id}'.");

    private static void ValidateExactSet(IEnumerable<string> expectedIds, IEnumerable<string> actualIds, string label)
    {
        var expected = expectedIds.OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        var actual = actualIds.OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Turbine expansion snapshot must contain exactly one snapshot per defined {label}. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }

    private static MassFlowRate SumMassFlow(IEnumerable<MassFlowRate> values)
        => MassFlowRate.FromKilogramsPerSecond(CompensatedSum(values.Select(static item => item.KilogramsPerSecond)));

    private static Power SumPower(IEnumerable<Power> values)
        => Power.FromWatts(CompensatedSum(values.Select(static item => item.Watts)));

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
