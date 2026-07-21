using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Canonical M4.1 semantic composition of drum steam-export seams, main-steam lines/headers,
/// stop/control/admission valve trains and replaceable turbine-admission boundaries.
/// </summary>
public sealed class MainSteamNetworkDefinition
{
    public MainSteamNetworkDefinition(
        string id,
        IntegratedPrimaryCircuitDefinition primaryCircuit,
        IEnumerable<MainSteamLineDefinition> steamLines,
        IEnumerable<TurbineAdmissionTrainDefinition> admissionTrains,
        IEnumerable<TurbineAdmissionBoundaryDefinition> turbineAdmissionBoundaries)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Main-steam network id cannot be empty or whitespace.", nameof(id));
        }

        PrimaryCircuit = primaryCircuit ?? throw new ArgumentNullException(nameof(primaryCircuit));
        ArgumentNullException.ThrowIfNull(steamLines);
        ArgumentNullException.ThrowIfNull(admissionTrains);
        ArgumentNullException.ThrowIfNull(turbineAdmissionBoundaries);

        var canonicalLines = Canonicalize(steamLines, static item => item.Id, nameof(steamLines));
        var canonicalTrains = Canonicalize(admissionTrains, static item => item.Id, nameof(admissionTrains));
        var canonicalBoundaries = Canonicalize(turbineAdmissionBoundaries, static item => item.Id, nameof(turbineAdmissionBoundaries));

        if (canonicalLines.Length == 0)
        {
            throw new ArgumentException("A main-steam network must contain at least one steam line.", nameof(steamLines));
        }

        if (canonicalTrains.Length == 0)
        {
            throw new ArgumentException("A main-steam network must contain at least one turbine-admission train.", nameof(admissionTrains));
        }

        if (canonicalBoundaries.Length == 0)
        {
            throw new ArgumentException("A main-steam network must contain at least one turbine-admission boundary.", nameof(turbineAdmissionBoundaries));
        }

        EnsureSemanticIdsAreGloballyUnique(canonicalLines, canonicalTrains, canonicalBoundaries);
        ValidateSteamLines(primaryCircuit, canonicalLines);
        ValidateAdmissionTrains(primaryCircuit.PlantDefinition, canonicalLines, canonicalTrains);
        ValidateAdmissionBoundaries(primaryCircuit.PlantDefinition, canonicalTrains, canonicalBoundaries);

        Id = id.Trim();
        SteamLines = new ReadOnlyCollection<MainSteamLineDefinition>(canonicalLines);
        AdmissionTrains = new ReadOnlyCollection<TurbineAdmissionTrainDefinition>(canonicalTrains);
        TurbineAdmissionBoundaries = new ReadOnlyCollection<TurbineAdmissionBoundaryDefinition>(canonicalBoundaries);
    }

    public string Id { get; }

    public IntegratedPrimaryCircuitDefinition PrimaryCircuit { get; }

    public PlantDefinition PlantDefinition => PrimaryCircuit.PlantDefinition;

    public IReadOnlyList<MainSteamLineDefinition> SteamLines { get; }

    public IReadOnlyList<TurbineAdmissionTrainDefinition> AdmissionTrains { get; }

    public IReadOnlyList<TurbineAdmissionBoundaryDefinition> TurbineAdmissionBoundaries { get; }

    public MainSteamLineDefinition GetSteamLine(string id)
        => GetById(SteamLines, id, static item => item.Id, "main-steam line");

    public TurbineAdmissionTrainDefinition GetAdmissionTrain(string id)
        => GetById(AdmissionTrains, id, static item => item.Id, "turbine-admission train");

    public TurbineAdmissionBoundaryDefinition GetTurbineAdmissionBoundary(string id)
        => GetById(TurbineAdmissionBoundaries, id, static item => item.Id, "turbine-admission boundary");

    private static void ValidateSteamLines(
        IntegratedPrimaryCircuitDefinition primaryCircuit,
        IReadOnlyList<MainSteamLineDefinition> steamLines)
    {
        var plant = primaryCircuit.PlantDefinition;
        var exports = primaryCircuit.BoundarySystem.SteamExportBoundaries;
        var assignedExports = new HashSet<string>(StringComparer.Ordinal);
        var assignedPipes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in steamLines)
        {
            var export = primaryCircuit.BoundarySystem.GetSteamExportBoundary(line.SteamExportBoundaryId);
            var pipe = plant.GetPipe(line.PipeId);
            _ = plant.GetFluidNode(line.HeaderNodeId);

            if (!assignedExports.Add(export.Id))
            {
                throw new ArgumentException(
                    $"Steam-export boundary '{export.Id}' is assigned to more than one main-steam line.",
                    nameof(steamLines));
            }

            if (!assignedPipes.Add(pipe.Id))
            {
                throw new ArgumentException(
                    $"Plant pipe '{pipe.Id}' is assigned to more than one main-steam line.",
                    nameof(steamLines));
            }

            if (!string.Equals(pipe.FromNodeId, export.SourceNodeId, StringComparison.Ordinal)
                || !string.Equals(pipe.ToNodeId, line.HeaderNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Main-steam line '{line.Id}' pipe '{pipe.Id}' must connect steam-export source '{export.SourceNodeId}' to header '{line.HeaderNodeId}'.",
                    nameof(steamLines));
            }
        }

        var expectedExports = exports.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        if (!expectedExports.SetEquals(assignedExports))
        {
            var missing = expectedExports.Except(assignedExports, StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every M3 steam-export seam must feed exactly one M4.1 main-steam line. Missing: {string.Join(", ", missing)}.",
                nameof(steamLines));
        }
    }

    private static void ValidateAdmissionTrains(
        PlantDefinition plant,
        IReadOnlyList<MainSteamLineDefinition> steamLines,
        IReadOnlyList<TurbineAdmissionTrainDefinition> admissionTrains)
    {
        var suppliedHeaders = steamLines.Select(static line => line.HeaderNodeId).ToHashSet(StringComparer.Ordinal);
        var usedValveIds = new HashSet<string>(StringComparer.Ordinal);

        var usedHeaders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var train in admissionTrains)
        {
            _ = plant.GetFluidNode(train.HeaderNodeId);
            _ = plant.GetFluidNode(train.TurbineInletNodeId);

            if (!suppliedHeaders.Contains(train.HeaderNodeId))
            {
                throw new ArgumentException(
                    $"Turbine-admission train '{train.Id}' header '{train.HeaderNodeId}' is not supplied by any defined main-steam line.",
                    nameof(admissionTrains));
            }

            usedHeaders.Add(train.HeaderNodeId);

            var stopValve = plant.GetValve(train.StopValveId);
            var controlValve = plant.GetValve(train.ControlValveId);
            var admissionValve = plant.GetValve(train.AdmissionValveId);

            foreach (var valveId in new[] { stopValve.Id, controlValve.Id, admissionValve.Id })
            {
                if (!usedValveIds.Add(valveId))
                {
                    throw new ArgumentException(
                        $"Valve '{valveId}' is assigned more than once across turbine-admission trains.",
                        nameof(admissionTrains));
                }
            }

            if (!string.Equals(stopValve.Pipe.FromNodeId, train.HeaderNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Admission train '{train.Id}' stop valve '{stopValve.Id}' must start at header '{train.HeaderNodeId}'.",
                    nameof(admissionTrains));
            }

            if (!string.Equals(controlValve.Pipe.FromNodeId, stopValve.Pipe.ToNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Admission train '{train.Id}' control valve '{controlValve.Id}' must start at stop-valve outlet '{stopValve.Pipe.ToNodeId}'.",
                    nameof(admissionTrains));
            }

            if (!string.Equals(admissionValve.Pipe.FromNodeId, controlValve.Pipe.ToNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Admission train '{train.Id}' admission valve '{admissionValve.Id}' must start at control-valve outlet '{controlValve.Pipe.ToNodeId}'.",
                    nameof(admissionTrains));
            }

            if (!string.Equals(admissionValve.Pipe.ToNodeId, train.TurbineInletNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Admission train '{train.Id}' admission valve '{admissionValve.Id}' must terminate at turbine-inlet node '{train.TurbineInletNodeId}'.",
                    nameof(admissionTrains));
            }
        }

        if (!suppliedHeaders.SetEquals(usedHeaders))
        {
            var unused = suppliedHeaders.Except(usedHeaders, StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every main-steam header must feed at least one turbine-admission train. Unused: {string.Join(", ", unused)}.",
                nameof(admissionTrains));
        }
    }

    private static void ValidateAdmissionBoundaries(
        PlantDefinition plant,
        IReadOnlyList<TurbineAdmissionTrainDefinition> admissionTrains,
        IReadOnlyList<TurbineAdmissionBoundaryDefinition> boundaries)
    {
        var assignedTrainIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var boundary in boundaries)
        {
            var train = admissionTrains.FirstOrDefault(item => string.Equals(item.Id, boundary.AdmissionTrainId, StringComparison.Ordinal))
                ?? throw new ArgumentException(
                    $"Turbine-admission boundary '{boundary.Id}' references unknown admission train '{boundary.AdmissionTrainId}'.",
                    nameof(boundaries));
            _ = plant.GetFluidNode(boundary.SourceNodeId);

            if (!assignedTrainIds.Add(train.Id))
            {
                throw new ArgumentException(
                    $"Turbine-admission train '{train.Id}' has more than one terminal boundary.",
                    nameof(boundaries));
            }

            if (!string.Equals(boundary.SourceNodeId, train.TurbineInletNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Turbine-admission boundary '{boundary.Id}' must source train '{train.Id}' inlet node '{train.TurbineInletNodeId}'.",
                    nameof(boundaries));
            }
        }

        var expectedTrainIds = admissionTrains.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        if (!expectedTrainIds.SetEquals(assignedTrainIds))
        {
            var missing = expectedTrainIds.Except(assignedTrainIds, StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every turbine-admission train must have exactly one terminal boundary. Missing: {string.Join(", ", missing)}.",
                nameof(boundaries));
        }
    }

    private static T[] Canonicalize<T>(IEnumerable<T> source, Func<T, string> idSelector, string parameterName)
        where T : class
    {
        var canonical = source
            .Select(item => item ?? throw new ArgumentException("Main-steam collections cannot contain null entries.", parameterName))
            .OrderBy(idSelector, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(idSelector).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException($"Ids in '{parameterName}' must be unique.", parameterName);
        }

        return canonical;
    }

    private static void EnsureSemanticIdsAreGloballyUnique(
        IEnumerable<MainSteamLineDefinition> lines,
        IEnumerable<TurbineAdmissionTrainDefinition> trains,
        IEnumerable<TurbineAdmissionBoundaryDefinition> boundaries)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in lines.Select(static item => item.Id)
                     .Concat(trains.Select(static item => item.Id))
                     .Concat(boundaries.Select(static item => item.Id)))
        {
            if (!ids.Add(id))
            {
                throw new ArgumentException($"Semantic main-steam id '{id}' is used more than once.");
            }
        }
    }

    private static T GetById<T>(IEnumerable<T> source, string id, Func<T, string> idSelector, string label)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"A {label} id cannot be empty or whitespace.", nameof(id));
        }

        return source.FirstOrDefault(item => string.Equals(idSelector(item), id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown {label} '{id}'.");
    }
}
