namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>Presentation quality of one instrument value. This does not replace canonical M5.1 signal validity/quality.</summary>
public enum ControlRoomInstrumentQuality
{
    Good = 0,
    Suspect = 1,
    Unavailable = 2,
}
