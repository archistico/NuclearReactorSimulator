using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Domain.Physics.Control;

/// <summary>Command-side actuator binding. Physical actuator state remains owned by the existing plant/rod domains.</summary>
public sealed class ActuatorDefinition
{
    private ActuatorDefinition(
        string id,
        string controllerId,
        ActuatorTargetKind targetKind,
        string targetId,
        ControllerOutputRange inputRange,
        ControlRodCommandTargetKind? rodTargetKind,
        double rodNeutralDeadbandFraction,
        bool positiveRodOutputWithdraws)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(controllerId) || string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Actuator, controller and target ids must be non-empty.");
        }

        if (!Enum.IsDefined(typeof(ActuatorTargetKind), targetKind))
        {
            throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Unknown actuator target kind.");
        }

        if (!double.IsFinite(inputRange.Minimum) || !double.IsFinite(inputRange.Maximum) || inputRange.Maximum <= inputRange.Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(inputRange), inputRange, "Actuator input range must be valid.");
        }

        if (!double.IsFinite(rodNeutralDeadbandFraction) || rodNeutralDeadbandFraction < 0d || rodNeutralDeadbandFraction >= 0.5d)
        {
            throw new ArgumentOutOfRangeException(nameof(rodNeutralDeadbandFraction), rodNeutralDeadbandFraction, "Rod neutral deadband must be in [0, 0.5).");
        }

        if (targetKind == ActuatorTargetKind.ControlRod && rodTargetKind is null)
        {
            throw new ArgumentException("Control-rod actuators require a rod command target kind.", nameof(rodTargetKind));
        }

        Id = id.Trim();
        ControllerId = controllerId.Trim();
        TargetKind = targetKind;
        TargetId = targetId.Trim();
        InputRange = inputRange;
        RodTargetKind = rodTargetKind;
        RodNeutralDeadbandFraction = rodNeutralDeadbandFraction;
        PositiveRodOutputWithdraws = positiveRodOutputWithdraws;
    }

    public string Id { get; }
    public string ControllerId { get; }
    public ActuatorTargetKind TargetKind { get; }
    public string TargetId { get; }
    public ControllerOutputRange InputRange { get; }
    public ControlRodCommandTargetKind? RodTargetKind { get; }
    public double RodNeutralDeadbandFraction { get; }
    public bool PositiveRodOutputWithdraws { get; }

    public static ActuatorDefinition Valve(string id, string controllerId, string valveId, ControllerOutputRange inputRange)
        => new(id, controllerId, ActuatorTargetKind.Valve, valveId, inputRange, null, 0d, true);

    public static ActuatorDefinition Pump(string id, string controllerId, string pumpId, ControllerOutputRange inputRange)
        => new(id, controllerId, ActuatorTargetKind.Pump, pumpId, inputRange, null, 0d, true);

    public static ActuatorDefinition ControlRod(
        string id,
        string controllerId,
        string targetId,
        ControlRodCommandTargetKind targetKind,
        ControllerOutputRange inputRange,
        double neutralDeadbandFraction = 0.05d,
        bool positiveOutputWithdraws = true)
        => new(id, controllerId, ActuatorTargetKind.ControlRod, targetId, inputRange, targetKind, neutralDeadbandFraction, positiveOutputWithdraws);
}
