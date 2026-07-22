using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerContentTests
{
    [Fact]
    public void InformationProjector_PreservesUnavailableValuesAndProvenanceInsteadOfFabricatingNumbers()
    {
        var information = OperatorComputerInformationProjector.Project(ControlRoomSnapshot.ShellOnly);

        Assert.Equal(new[] { "REACTOR", "PRIMARY", "TURBINE", "ELECTRICAL", "PROTECTION" },
            information.Sections.Select(static section => section.SectionId));
        var xenon = information.Sections.Single(static section => section.SectionId == "REACTOR")
            .Items.Single(static item => item.Label == "XENON REACTIVITY");
        Assert.Equal("—", xenon.ValueText);
        Assert.Equal(OperatorComputerInformationProvenance.Unavailable, xenon.Provenance);
        Assert.DoesNotContain(information.Sections.SelectMany(static section => section.Items), static item =>
            item.Provenance == OperatorComputerInformationProvenance.Unavailable && item.ValueText == "0");
    }

    [Fact]
    public void PowerManoeuvringProjection_ReusesCanonicalChecklistAndProducesGenericGuidanceAndDiagnostics()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var content = OperatorComputerScenarioContentProjector.Project(
            snapshot,
            PowerManoeuvringNormalShutdownProgram.Guidance,
            new PowerManoeuvringChecklistEvaluator(),
            TrainingGuidanceMode.Guided);

        Assert.Equal("POWER MANOEUVRING / NORMAL SHUTDOWN", content.Guidance.ProcedureTitle);
        Assert.NotEmpty(content.Guidance.Steps);
        Assert.Contains(content.Guidance.Steps, static step => step.State == OperatorComputerGuidanceStepState.Completed);
        Assert.Contains(content.Guidance.Steps, static step => step.State == OperatorComputerGuidanceStepState.Current);

        var lowLoad = content.Diagnostics.Items.Single(static item => item.CheckId == "low-load");
        var unloaded = content.Diagnostics.Items.Single(static item => item.CheckId == "generator-unloaded");
        Assert.True(lowLoad.IsSatisfied);
        Assert.False(unloaded.IsSatisfied);
        Assert.False(content.Diagnostics.AllChecksSatisfied);
        Assert.True(content.Diagnostics.UnsatisfiedCount > 0);
    }

    [Theory]
    [InlineData(TrainingGuidanceMode.Hidden)]
    [InlineData(TrainingGuidanceMode.ChecklistOnly)]
    public void NonGuidedAssistance_SuppressesStepTextWithoutSuppressingCanonicalDiagnostics(TrainingGuidanceMode mode)
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var content = OperatorComputerScenarioContentProjector.Project(
            snapshot,
            PowerManoeuvringNormalShutdownProgram.Guidance,
            new PowerManoeuvringChecklistEvaluator(),
            mode);

        Assert.Empty(content.Guidance.Steps);
        Assert.NotEmpty(content.Diagnostics.Items);
        Assert.Equal(mode, content.Guidance.GuidanceMode);
    }

    [Fact]
    public void InformationAndDiagnosticsContracts_FreezeCollectionsAndValidateIdentity()
    {
        var item = new OperatorComputerInformationItemSnapshot(
            "VALUE",
            "1.0",
            "MW",
            OperatorComputerInformationProvenance.Measured);
        var section = new OperatorComputerInformationSectionSnapshot("A", "SECTION A", new[] { item });
        var information = new OperatorComputerInformationSnapshot(new[] { section });
        var sections = Assert.IsAssignableFrom<IList<OperatorComputerInformationSectionSnapshot>>(information.Sections);

        Assert.Throws<NotSupportedException>(() => sections.Clear());
        Assert.Throws<ArgumentException>(() => new OperatorComputerInformationSnapshot(new[] { section, section }));

        var diagnostics = new OperatorComputerDiagnosticsSnapshot("TEST", new[]
        {
            new OperatorComputerDiagnosticItemSnapshot("a", "A", true, "ok"),
            new OperatorComputerDiagnosticItemSnapshot("b", "B", false, "blocked"),
        });
        Assert.False(diagnostics.AllChecksSatisfied);
        Assert.Equal(1, diagnostics.SatisfiedCount);
        Assert.Equal(1, diagnostics.UnsatisfiedCount);
    }

    [Fact]
    public void SnapshotProjector_WithScenarioContent_ActivatesM102AndReadOnlyM103AlarmPageWhileHistoryRemainsExplicitlyUnavailable()
    {
        var guidance = new OperatorComputerGuidanceSnapshot(
            "TEST PROCEDURE",
            TrainingGuidanceMode.Guided,
            new[] { new OperatorComputerGuidanceStepSnapshot("s1", 1, "Step", "Instruction", OperatorComputerGuidanceStepState.Current) },
            "Current procedure step: 1. Step");
        var diagnostics = new OperatorComputerDiagnosticsSnapshot(
            "TEST PROCEDURE",
            new[] { new OperatorComputerDiagnosticItemSnapshot("c1", "Check", true, "Satisfied") });
        var snapshot = OperatorComputerSnapshotProjector.Project(
            ControlRoomSnapshot.ShellOnly,
            new OperatorComputerScenarioContentSnapshot(guidance, diagnostics));

        Assert.Same(guidance, snapshot.Guidance);
        Assert.Same(diagnostics, snapshot.Diagnostics);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Guidance).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Info).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Diagnostics).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Alarms).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Unavailable, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Log).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Commands).ContentState);
        Assert.All(snapshot.Pages.Where(static page => page.Id is OperatorComputerPageId.Modes or OperatorComputerPageId.Session),
            static page => Assert.Equal(OperatorComputerPageContentState.ShellOnly, page.ContentState));
    }
}
