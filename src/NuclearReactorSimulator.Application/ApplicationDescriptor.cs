namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.7.5 Hotfix 1 — Mission/Performance Closure Audit Wrapper Repair",
        "M10.9.7.5 Hotfix 1 is stacked exclusively on the original M10.9.7.5 closure candidate, itself stacked on M10.9.7.4 Hotfix 1 VALIDATED. The original candidate compiled and passed the complete ordinary suite, but its focused Windows batch gate aborted while resolving run_app_class and was not validated. Hotfix 1 removes all CALL :label batch subroutines from scripts\\run-m1097-mission-performance-closure-audit.cmd, uses explicit dotnet test invocations, and adds source-level wrapper regression coverage; production XAML/runtime semantics, Simulation physics, challenge/scoring/protection ownership, archive schema and plant-command authority are unchanged. The closure matrix covers no active mission, active mission without external demand, bounded demand-following, completed and failed mission presentation, challenge-specific required generator-trip evidence versus unexpected-trip failure, terminal mission presentation while plant logical time continues, checkpoint/archive replay equivalence, assistance-mode changes and requested/effective control-authority divergence. F1-F8 remain preserved, F9 remains absent, MISSION remains presentation-only with no plant-command authority, GRID DEMAND / REQUESTED LOAD / ACTUAL OUTPUT remain semantically separate, score presentation remains copied from the M10.9.6 owner, and deterministic replay/checkpoint presentation remains qualified. The frozen sha256-control-room-snapshot-v1 golden fingerprint and session archive schema v1 are unchanged. The focused gate is scripts\\run-m1097-mission-performance-closure-audit.cmd; final M10.9.7 promotion requires docs\\M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md before M10.9.8 may begin."
    );
}
