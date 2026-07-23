using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;

/// <summary>
/// Optional current-model governor policy that keeps the normal speed controller for run-up while deriving a
/// post-synchronization droop speed reference from requested electrical load.
/// </summary>
public sealed class TurbineGovernorDroopDefinition
{
    public TurbineGovernorDroopDefinition(
        string speedControllerId,
        string generatorId,
        AngularSpeed fullLoadSpeedReferenceRise)
    {
        if (string.IsNullOrWhiteSpace(speedControllerId))
        {
            throw new ArgumentException("Governor speed-controller id cannot be empty or whitespace.", nameof(speedControllerId));
        }

        if (string.IsNullOrWhiteSpace(generatorId))
        {
            throw new ArgumentException("Governor generator id cannot be empty or whitespace.", nameof(generatorId));
        }

        if (fullLoadSpeedReferenceRise <= AngularSpeed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullLoadSpeedReferenceRise),
                fullLoadSpeedReferenceRise,
                "Governor full-load droop speed-reference rise must be greater than zero.");
        }

        SpeedControllerId = speedControllerId.Trim();
        GeneratorId = generatorId.Trim();
        FullLoadSpeedReferenceRise = fullLoadSpeedReferenceRise;
    }

    public string SpeedControllerId { get; }

    public string GeneratorId { get; }

    /// <summary>
    /// Mechanical speed-reference rise between zero and full requested electrical load while paralleled.
    /// A 150 rpm rise on a 3000 rpm machine corresponds to 5% droop.
    /// </summary>
    public AngularSpeed FullLoadSpeedReferenceRise { get; }
}
