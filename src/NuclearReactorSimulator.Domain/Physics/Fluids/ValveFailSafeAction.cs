namespace NuclearReactorSimulator.Domain.Physics.Fluids;

public enum ValveFailSafeAction
{
    FailClosed = 0,
    FailOpen = 1,
    HoldLastPosition = 2,
}
