namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public enum ControlRoomSubsystemSchematicConnectionKind
{
    PrimaryCoolant = 0,
    Steam = 1,
    Condensate = 2,
    Feedwater = 3,
    Mechanical = 4,
    Electrical = 5,
    MeasurementSignal = 6,
    ControlSignal = 7,
    FeedbackSignal = 8,
    ProtectionOverride = 9,
    AlarmSignal = 10,
    ThermalInfluence = 11,
}
