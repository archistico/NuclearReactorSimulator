namespace NuclearReactorSimulator.Application.Scenarios;

public sealed class ScenarioOperatorActionAcceptedEventArgs : EventArgs
{
    public ScenarioOperatorActionAcceptedEventArgs(ScenarioOperatorActionRecord action)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public ScenarioOperatorActionRecord Action { get; }
}
