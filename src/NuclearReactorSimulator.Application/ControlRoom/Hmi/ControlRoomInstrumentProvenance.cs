namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>Operator-facing provenance for one displayed instrument value.</summary>
public enum ControlRoomInstrumentProvenance
{
    Unspecified = 0,
    Measured = 1,
    Model = 2,
    Annunciator = 3,
}
