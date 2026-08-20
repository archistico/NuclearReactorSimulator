namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

public sealed class ChallengeLifecycleChangedEventArgs : EventArgs
{
    public ChallengeLifecycleChangedEventArgs(ChallengeLifecycleSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public ChallengeLifecycleSnapshot Snapshot { get; }
}
