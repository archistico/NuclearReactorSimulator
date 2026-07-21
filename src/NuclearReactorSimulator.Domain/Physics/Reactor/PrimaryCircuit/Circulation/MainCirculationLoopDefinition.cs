using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;

/// <summary>
/// Immutable semantic definition of one main-circulation loop.
/// It composes canonical plant pumps, header nodes, channel groups and passive return pipes.
/// </summary>
public sealed class MainCirculationLoopDefinition
{
    public MainCirculationLoopDefinition(
        string id,
        string suctionHeaderNodeId,
        string pressureHeaderNodeId,
        IEnumerable<string> pumpIds,
        IEnumerable<MainCirculationBranchDefinition> branches)
        : this(id, suctionHeaderNodeId, pressureHeaderNodeId, suctionHeaderNodeId, pumpIds, branches)
    {
    }

    public MainCirculationLoopDefinition(
        string id,
        string suctionHeaderNodeId,
        string pressureHeaderNodeId,
        string returnCollectorNodeId,
        IEnumerable<string> pumpIds,
        IEnumerable<MainCirculationBranchDefinition> branches)
    {
        Id = ValidateId(id, nameof(id), "Main-circulation loop");
        SuctionHeaderNodeId = ValidateId(suctionHeaderNodeId, nameof(suctionHeaderNodeId), "Suction-header node");
        PressureHeaderNodeId = ValidateId(pressureHeaderNodeId, nameof(pressureHeaderNodeId), "Pressure-header node");
        ReturnCollectorNodeId = ValidateId(returnCollectorNodeId, nameof(returnCollectorNodeId), "Return-collector node");
        ArgumentNullException.ThrowIfNull(pumpIds);
        ArgumentNullException.ThrowIfNull(branches);

        if (string.Equals(SuctionHeaderNodeId, PressureHeaderNodeId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Suction-header and pressure-header nodes must be distinct.");
        }

        if (string.Equals(ReturnCollectorNodeId, PressureHeaderNodeId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Return-collector and pressure-header nodes must be distinct.");
        }

        var canonicalPumpIds = pumpIds
            .Select(idValue => ValidateId(idValue, nameof(pumpIds), "Pump"))
            .OrderBy(static idValue => idValue, StringComparer.Ordinal)
            .ToArray();
        if (canonicalPumpIds.Length == 0)
        {
            throw new ArgumentException("A main-circulation loop must contain at least one pump.", nameof(pumpIds));
        }

        if (canonicalPumpIds.Distinct(StringComparer.Ordinal).Count() != canonicalPumpIds.Length)
        {
            throw new ArgumentException("Pump ids inside a main-circulation loop must be unique.", nameof(pumpIds));
        }

        var canonicalBranches = branches
            .Select(branch => branch ?? throw new ArgumentException("Main-circulation branches cannot contain null entries.", nameof(branches)))
            .OrderBy(static branch => branch.FuelChannelGroupId, StringComparer.Ordinal)
            .ThenBy(static branch => branch.ReturnPipeId, StringComparer.Ordinal)
            .ToArray();
        if (canonicalBranches.Length == 0)
        {
            throw new ArgumentException("A main-circulation loop must contain at least one fuel-channel branch.", nameof(branches));
        }

        if (canonicalBranches.Select(static branch => branch.FuelChannelGroupId).Distinct(StringComparer.Ordinal).Count() != canonicalBranches.Length)
        {
            throw new ArgumentException("Fuel-channel groups inside one main-circulation loop must be unique.", nameof(branches));
        }

        if (canonicalBranches.Select(static branch => branch.ReturnPipeId).Distinct(StringComparer.Ordinal).Count() != canonicalBranches.Length)
        {
            throw new ArgumentException("Return pipes inside one main-circulation loop must be unique.", nameof(branches));
        }

        PumpIds = new ReadOnlyCollection<string>(canonicalPumpIds);
        Branches = new ReadOnlyCollection<MainCirculationBranchDefinition>(canonicalBranches);
    }

    public string Id { get; }

    public string SuctionHeaderNodeId { get; }

    public string PressureHeaderNodeId { get; }

    public string ReturnCollectorNodeId { get; }

    public IReadOnlyList<string> PumpIds { get; }

    public IReadOnlyList<MainCirculationBranchDefinition> Branches { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
