namespace NuclearReactorSimulator.Application.ControlRoom.Automation;

public sealed class PlantControlAuthorityChangedEventArgs : EventArgs
{
    public PlantControlAuthorityChangedEventArgs(PlantControlAuthorityPresentationSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public PlantControlAuthorityPresentationSnapshot Snapshot { get; }
}
