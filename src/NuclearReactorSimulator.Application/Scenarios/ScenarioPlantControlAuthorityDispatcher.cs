using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Scenario-session M10.5/M10.6 authority seam. It forwards typed intents to the canonical runtime and journals only intents
/// that were accepted by that boundary, preserving deterministic replay without recasting them as physical commands.
/// </summary>
public sealed class ScenarioPlantControlAuthorityDispatcher : IPlantControlAuthorityDispatcher
{
    private readonly IPlantControlAuthorityDispatcher _inner;
    private readonly ScenarioAutomationIntentJournal _journal;
    private readonly Func<long> _logicalStepSource;

    public ScenarioPlantControlAuthorityDispatcher(
        IPlantControlAuthorityDispatcher inner,
        ScenarioAutomationIntentJournal journal,
        Func<long> logicalStepSource)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _logicalStepSource = logicalStepSource ?? throw new ArgumentNullException(nameof(logicalStepSource));
    }

    public event EventHandler<PlantControlAuthorityChangedEventArgs>? AuthorityChanged
    {
        add => _inner.AuthorityChanged += value;
        remove => _inner.AuthorityChanged -= value;
    }

    public PlantControlAuthorityPresentationSnapshot CurrentAutomation => _inner.CurrentAutomation;

    public void RequestAuthority(PlantControlAuthorityMode mode)
    {
        _inner.RequestAuthority(mode);
        _journal.RecordAuthority(_logicalStepSource(), mode);
    }

    public void RequestSupervisoryObjective(SupervisoryObjectiveRequest objective)
    {
        ArgumentNullException.ThrowIfNull(objective);
        _inner.RequestSupervisoryObjective(objective);
        _journal.RecordObjective(_logicalStepSource(), objective);
    }
}
