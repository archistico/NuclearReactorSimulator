namespace NuclearReactorSimulator.Application.ControlRoom;

public enum ControlRoomCommandKind
{
    Run = 0,
    Pause = 1,
    SingleStep = 2,
    ReactorScram = 3,
    ProtectionReset = 4,
    ControlRodInsert = 5,
    ControlRodHold = 6,
    ControlRodWithdraw = 7,
    MainCirculationPumpStart = 8,
    MainCirculationPumpStop = 9,
    TurbineTrip = 10,
    GeneratorTrip = 11,
    GeneratorBreakerClose = 12,
    GeneratorBreakerOpen = 13,
    TurbineSpeedRaise = 14,
    TurbineSpeedLower = 15,
    GeneratorLoadRaise = 16,
    GeneratorLoadLower = 17,
    AlarmAcknowledge = 18,
    AlarmReset = 19,
    AlarmAcknowledgeAll = 20,
    AlarmResetAll = 21,
}
