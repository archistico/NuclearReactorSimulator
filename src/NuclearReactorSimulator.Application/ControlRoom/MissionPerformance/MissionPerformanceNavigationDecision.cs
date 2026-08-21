namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// M10.9.7.2 placement decision for the future Mission &amp; Performance workstation.
/// This contract chooses navigation topology only; UI activation remains deferred to M10.9.7.3.
/// </summary>
public sealed class MissionPerformanceNavigationDecision
{
    private MissionPerformanceNavigationDecision()
    {
    }

    /// <summary>
    /// Canonical M10.9.7.2 placement/navigation decision.
    /// </summary>
    public static MissionPerformanceNavigationDecision Current { get; } = new();

    /// <summary>
    /// Mission/Performance is a dedicated peer workspace in the main control-room HMI shell.
    /// It is not a ninth fixed Operator Computer page.
    /// </summary>
    public MissionPerformanceWorkspacePlacement WorkspacePlacement =>
        MissionPerformanceWorkspacePlacement.DedicatedMainHmiWorkspace;

    /// <summary>
    /// COMPUTER may expose an explicit contextual navigation action to the dedicated workspace.
    /// Navigation remains selection-only and never dispatches a plant command.
    /// </summary>
    public MissionPerformanceComputerEntryMode ComputerEntryMode =>
        MissionPerformanceComputerEntryMode.ContextualNavigationAction;

    public string WorkspaceTitle => "Mission & Performance";

    public string WorkspaceLabel => "MISSION";

    public string ComputerSourceLabel => "COMPUTER";

    public bool ChangesOperatorComputerFunctionKeyContract => false;

    public string? AddedOperatorComputerFunctionKey => null;

    public bool NavigationHasPlantCommandAuthority => false;

    /// <summary>
    /// M10.9.7.2 freezes the decision but deliberately does not add the workspace to the live shell.
    /// </summary>
    public bool UiRouteActivated => false;

    public string UiActivationMilestone => "M10.9.7.3";
}

public enum MissionPerformanceWorkspacePlacement
{
    DedicatedMainHmiWorkspace = 0,
}

public enum MissionPerformanceComputerEntryMode
{
    ContextualNavigationAction = 0,
}
