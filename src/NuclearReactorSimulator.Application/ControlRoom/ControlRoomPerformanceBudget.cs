namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// M6.1 presentation budgets. These are UI-side targets only and never alter simulation timestep or results.
/// </summary>
public sealed record ControlRoomPerformanceBudget
{
    public ControlRoomPerformanceBudget(
        int maximumPresentationRefreshHertz,
        int maximumVisibleWorkspaceRows,
        int maximumVisibleTrendSeries)
    {
        if (maximumPresentationRefreshHertz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPresentationRefreshHertz));
        }

        if (maximumVisibleWorkspaceRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisibleWorkspaceRows));
        }

        if (maximumVisibleTrendSeries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisibleTrendSeries));
        }

        MaximumPresentationRefreshHertz = maximumPresentationRefreshHertz;
        MaximumVisibleWorkspaceRows = maximumVisibleWorkspaceRows;
        MaximumVisibleTrendSeries = maximumVisibleTrendSeries;
    }

    public static ControlRoomPerformanceBudget DesktopDefault { get; } = new(20, 250, 12);

    public int MaximumPresentationRefreshHertz { get; }

    public int MaximumVisibleWorkspaceRows { get; }

    public int MaximumVisibleTrendSeries { get; }
}
