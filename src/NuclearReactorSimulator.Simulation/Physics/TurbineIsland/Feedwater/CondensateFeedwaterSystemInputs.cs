using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Complete immutable M4.4 inputs. The legacy M3 external feedwater sources must be zero while the closed condensate/feedwater path owns drum makeup.
/// </summary>
public sealed class CondensateFeedwaterSystemInputs
{
    public CondensateFeedwaterSystemInputs(
        CondensateFeedwaterSystemDefinition definition,
        CondenserSystemInputs condenserInputs,
        IEnumerable<CondensateFeedwaterTrainInput> trainInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CondenserInputs = condenserInputs ?? throw new ArgumentNullException(nameof(condenserInputs));
        ArgumentNullException.ThrowIfNull(trainInputs);

        if (!ReferenceEquals(condenserInputs.Definition, definition.CondenserSystem))
        {
            throw new ArgumentException("Condenser inputs do not use the condensate/feedwater system's canonical M4.3 definition.", nameof(condenserInputs));
        }

        var legacyFeedwaterInputs = condenserInputs
            .TurbineExpansionInputs
            .MainSteamInputs
            .PrimaryCircuitInputs
            .BoundaryInputs
            .FeedwaterInputs;
        foreach (var input in legacyFeedwaterInputs)
        {
            if (input.MassFlowRate != MassFlowRate.Zero)
            {
                throw new ArgumentException(
                    $"M3 feedwater boundary '{input.BoundaryId}' must be commanded to zero while M4.4 closes the condensate/feedwater path.",
                    nameof(condenserInputs));
            }
        }

        var canonical = trainInputs
            .Select(item => item ?? throw new ArgumentException("Condensate/feedwater input collections cannot contain null entries.", nameof(trainInputs)))
            .OrderBy(static item => item.TrainId, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Select(static item => item.TrainId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Condensate/feedwater train input ids must be unique.", nameof(trainInputs));
        }

        var expected = definition.Trains.Select(static item => item.Id).OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.TrainId).OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"M4.4 inputs must contain exactly one input per condensate/feedwater train. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(trainInputs));
        }

        foreach (var input in canonical)
        {
            var train = definition.GetTrain(input.TrainId);
            if (input.ThermalConditioningPower > train.MaximumThermalConditioningPower)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(trainInputs),
                    input.ThermalConditioningPower,
                    $"Train '{train.Id}' thermal-conditioning power exceeds its configured maximum of {train.MaximumThermalConditioningPower.Watts} W.");
            }
        }

        TrainInputs = new ReadOnlyCollection<CondensateFeedwaterTrainInput>(canonical);
    }

    public CondensateFeedwaterSystemDefinition Definition { get; }

    public CondenserSystemInputs CondenserInputs { get; }

    public IReadOnlyList<CondensateFeedwaterTrainInput> TrainInputs { get; }

    public CondensateFeedwaterTrainInput GetTrainInput(string id)
        => TrainInputs.FirstOrDefault(item => string.Equals(item.TrainId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown condensate/feedwater train input '{id}'.");
}
