namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// M10.9.7.3 live-shell activation contract. The M10.9.7.2 navigation decision remains the historical topology decision;
/// this contract records that the dedicated workspace is now registered in the main HMI shell without adding a COMPUTER F9
/// page or any plant-command authority.
/// </summary>
public sealed class MissionPerformanceWorkspaceActivation
{
    private MissionPerformanceWorkspaceActivation()
    {
    }

    public static MissionPerformanceWorkspaceActivation Current { get; } = new();

    public string ActivationMilestone => "M10.9.7.3";

    public string WorkspaceTitle => MissionPerformanceNavigationDecision.Current.WorkspaceTitle;

    public string WorkspaceLabel => MissionPerformanceNavigationDecision.Current.WorkspaceLabel;

    public bool UiRouteActivated => true;

    public bool OperatorComputerFunctionKeyContractChanged => false;

    public string? AddedOperatorComputerFunctionKey => null;

    public bool NavigationHasPlantCommandAuthority => false;
}
