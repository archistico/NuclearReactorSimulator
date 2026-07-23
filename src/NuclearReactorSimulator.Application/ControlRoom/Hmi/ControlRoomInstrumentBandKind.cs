namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>Semantic operating band for presentation scales. Instrument range and protection limits remain separate concepts.</summary>
public enum ControlRoomInstrumentBandKind
{
    NormalOperating = 0,
    Warning = 1,
    Alarm = 2,
}
