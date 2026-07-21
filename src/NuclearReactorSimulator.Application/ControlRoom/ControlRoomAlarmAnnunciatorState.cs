namespace NuclearReactorSimulator.Application.ControlRoom;

public enum ControlRoomAlarmAnnunciatorState
{
    Normal = 0,
    ActiveUnacknowledged = 1,
    ActiveAcknowledged = 2,
    ReturnedUnacknowledged = 3,
    ReturnedAcknowledged = 4,
}
