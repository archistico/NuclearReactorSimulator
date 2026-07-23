using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NuclearReactorSimulator.App.Commands;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly string StartupToPowerCommandPath = BuildStartupToPowerCommandPath();

    private readonly IControlRoomSnapshotSource _snapshotSource;
    private readonly IControlRoomCommandDispatcher _commandDispatcher;
    private readonly IPlantControlAuthorityDispatcher? _plantControlAuthorityDispatcher;
    private readonly ControlRoomOperationalHistoryAccumulator _operationalHistory;
    private readonly ScenarioRecorder? _scenarioRecorder;
    private readonly PostIncidentAnalysisReport? _postIncidentAnalysis;
    private readonly OperatorComputerSessionWorkspaceController? _sessionWorkspace;
    private readonly PreStartupGuidancePlan? _preStartupGuidance;
    private readonly PreStartupChecklistEvaluator _preStartupChecklistEvaluator = new();
    private readonly FirstCriticalityGuidancePlan? _firstCriticalityGuidance;
    private readonly FirstCriticalityChecklistEvaluator _firstCriticalityChecklistEvaluator = new();
    private readonly HeatUpTurbineStartupGuidancePlan? _heatUpTurbineStartupGuidance;
    private readonly HeatUpTurbineStartupChecklistEvaluator _heatUpTurbineStartupChecklistEvaluator = new();
    private readonly GridSynchronizationGuidancePlan? _gridSynchronizationGuidance;
    private readonly GridSynchronizationChecklistEvaluator _gridSynchronizationChecklistEvaluator = new();
    private readonly PowerManoeuvringGuidancePlan? _powerManoeuvringGuidance;
    private readonly ScenarioTrainingTracker? _trainingTracker;
    private readonly PowerManoeuvringChecklistEvaluator _powerManoeuvringChecklistEvaluator = new();
    private ControlRoomWorkspaceDescriptor _selectedWorkspace;
    private ControlRoomSnapshot _snapshot;
    private ControlRoomInstrumentTrendSnapshot _reactorThermalPowerTrend = ControlRoomInstrumentTrendSnapshot.Unavailable;
    private ControlRoomInstrumentTrendSnapshot _grossElectricalOutputTrend = ControlRoomInstrumentTrendSnapshot.Unavailable;
    private ControlRoomPlantMimicSnapshot _plantMimic = ControlRoomPlantMimicSnapshot.Empty;
    private ControlRoomSubsystemSchematicsSnapshot _subsystemSchematics = ControlRoomSubsystemSchematicsSnapshot.Empty;
    private string? _selectedMimicElementId;
    private int _selectedRodIndex;
    private int _selectedPumpIndex;
    private int _selectedGeneratorIndex;
    private int _selectedAlarmIndex;
    private string _commandStatus;
    private string _lastControlActionText = "LAST CONTROL ACTION · none issued yet";
    private bool _runtimeSubscriptionsDetached;

    public MainWindowViewModel(
        ApplicationDescriptor descriptor,
        IControlRoomSnapshotSource snapshotSource,
        IControlRoomCommandDispatcher commandDispatcher,
        PreStartupGuidancePlan? preStartupGuidance = null,
        FirstCriticalityGuidancePlan? firstCriticalityGuidance = null,
        HeatUpTurbineStartupGuidancePlan? heatUpTurbineStartupGuidance = null,
        GridSynchronizationGuidancePlan? gridSynchronizationGuidance = null,
        PowerManoeuvringGuidancePlan? powerManoeuvringGuidance = null,
        ScenarioTrainingTracker? trainingTracker = null,
        ScenarioRecorder? scenarioRecorder = null,
        PostIncidentAnalysisReport? postIncidentAnalysis = null,
        IPlantControlAuthorityDispatcher? plantControlAuthorityDispatcher = null,
        OperatorComputerSessionWorkspaceController? sessionWorkspace = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(snapshotSource);
        _snapshotSource = snapshotSource;
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _preStartupGuidance = preStartupGuidance;
        _firstCriticalityGuidance = firstCriticalityGuidance;
        _heatUpTurbineStartupGuidance = heatUpTurbineStartupGuidance;
        _gridSynchronizationGuidance = gridSynchronizationGuidance;
        _powerManoeuvringGuidance = powerManoeuvringGuidance;
        _trainingTracker = trainingTracker;
        _plantControlAuthorityDispatcher = plantControlAuthorityDispatcher;
        _scenarioRecorder = scenarioRecorder;
        _postIncidentAnalysis = postIncidentAnalysis;
        _sessionWorkspace = sessionWorkspace;
        _commandStatus = trainingTracker is not null
            ? "Training session ready. No operator command has been issued yet. Fault-enabled scenarios use the same canonical instrumentation, control and protection paths as normal operation."
            : powerManoeuvringGuidance is not null
            ? "Power-manoeuvring / normal-shutdown scenario loaded in PAUSED state. Manoeuvre load deliberately, then unload, disconnect, insert rods and preserve post-shutdown circulation."
            : gridSynchronizationGuidance is not null
            ? "Grid-synchronization / initial-loading scenario loaded in PAUSED state. Close only when synchronization permissives are satisfied, then take load in deliberate increments."
            : heatUpTurbineStartupGuidance is not null
            ? "Heat-up / steam-raising / turbine-startup scenario loaded in PAUSED state. Preserve generator isolation while rolling the turbine with the canonical speed controls."
            : firstCriticalityGuidance is not null
                ? "First-criticality / low-power scenario loaded in PAUSED state. Use controlled rod motion, observe reactivity/period and preserve steam/grid isolation."
                : "Cold-shutdown / pre-start scenario loaded in PAUSED state. Follow preparation guidance; unavailable or blocked actions fail closed.";

        Title = descriptor.ProductName;
        Milestone = descriptor.Milestone;
        Status = descriptor.Status;
        Workspaces = ControlRoomWorkspaceCatalog.Default;
        _selectedWorkspace = Workspaces[0];
        _snapshot = snapshotSource.Current;
        _plantMimic = ControlRoomPlantMimicProjector.Project(_snapshot);
        _subsystemSchematics = ControlRoomSubsystemSchematicProjector.Project(_snapshot);
        _selectedMimicElementId = _plantMimic.Elements.FirstOrDefault()?.ElementId;
        PerformanceBudget = ControlRoomPerformanceBudget.DesktopDefault;
        _operationalHistory = new ControlRoomOperationalHistoryAccumulator(
            maximumVisibleTrendSeries: PerformanceBudget.MaximumVisibleTrendSeries);
        _operationalHistory.Observe(_snapshot);
        OperatorComputer = new OperatorComputerViewModel(
            ProjectOperatorComputerSnapshot(),
            _commandDispatcher,
            _trainingTracker,
            _plantControlAuthorityDispatcher,
            _sessionWorkspace);

        RunCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.Run));
        PauseCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.Pause));
        SingleStepCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.SingleStep));
        ReactorScramCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.ReactorScram));
        ProtectionResetCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.ProtectionReset));
        RodInsertCommand = new DelegateCommand(() => DispatchRod(ControlRoomCommandKind.ControlRodInsert));
        RodHoldCommand = new DelegateCommand(() => DispatchRod(ControlRoomCommandKind.ControlRodHold));
        RodWithdrawCommand = new DelegateCommand(() => DispatchRod(ControlRoomCommandKind.ControlRodWithdraw));
        PumpStartCommand = new DelegateCommand(() => DispatchPump(ControlRoomCommandKind.MainCirculationPumpStart));
        PumpStopCommand = new DelegateCommand(() => DispatchPump(ControlRoomCommandKind.MainCirculationPumpStop));
        TurbineTripCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.TurbineTrip));
        GeneratorTripCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.GeneratorTrip));
        GeneratorBreakerCloseCommand = new DelegateCommand(() => DispatchBreaker(ControlRoomCommandKind.GeneratorBreakerClose));
        GeneratorBreakerOpenCommand = new DelegateCommand(() => DispatchBreaker(ControlRoomCommandKind.GeneratorBreakerOpen));
        TurbineSpeedRaiseCommand = new DelegateCommand(() => DispatchSelectedGeneratorRotor(ControlRoomCommandKind.TurbineSpeedRaise));
        TurbineSpeedLowerCommand = new DelegateCommand(() => DispatchSelectedGeneratorRotor(ControlRoomCommandKind.TurbineSpeedLower));
        GeneratorLoadRaiseCommand = new DelegateCommand(() => DispatchSelectedGenerator(ControlRoomCommandKind.GeneratorLoadRaise));
        GeneratorLoadLowerCommand = new DelegateCommand(() => DispatchSelectedGenerator(ControlRoomCommandKind.GeneratorLoadLower));
        AlarmAcknowledgeCommand = new DelegateCommand(() => DispatchAlarm(ControlRoomCommandKind.AlarmAcknowledge));
        AlarmResetCommand = new DelegateCommand(() => DispatchAlarm(ControlRoomCommandKind.AlarmReset));
        AlarmAcknowledgeAllCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.AlarmAcknowledgeAll));
        AlarmResetAllCommand = new DelegateCommand(() => Dispatch(ControlRoomCommandKind.AlarmResetAll));
        OpenOperatorComputerGuidancePageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Guidance));
        OpenOperatorComputerInfoPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Info));
        OpenOperatorComputerAlarmsPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Alarms));
        OpenOperatorComputerCommandsPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Commands));
        OpenOperatorComputerModesPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Modes));
        OpenOperatorComputerDiagnosticsPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Diagnostics));
        OpenOperatorComputerLogPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Log));
        OpenOperatorComputerSessionPageCommand = new DelegateCommand(() => OpenOperatorComputerPage(OperatorComputerPageId.Session));
        OpenSelectedMimicSubsystemCommand = new DelegateCommand(OpenSelectedMimicSubsystem);

        snapshotSource.SnapshotChanged += OnSnapshotChanged;
        if (_trainingTracker is not null)
        {
            _trainingTracker.AssessmentChanged += OnTrainingAssessmentChanged;
            _trainingTracker.GuidanceModeChanged += OnTrainingGuidanceModeChanged;
        }
        if (_plantControlAuthorityDispatcher is not null)
        {
            _plantControlAuthorityDispatcher.AuthorityChanged += OnPlantControlAuthorityChanged;
        }
        if (_sessionWorkspace is not null)
        {
            _sessionWorkspace.Changed += OnSessionWorkspaceChanged;
        }
    }

    public void DetachRuntimeSubscriptions()
    {
        if (_runtimeSubscriptionsDetached)
        {
            return;
        }

        _snapshotSource.SnapshotChanged -= OnSnapshotChanged;
        if (_trainingTracker is not null)
        {
            _trainingTracker.AssessmentChanged -= OnTrainingAssessmentChanged;
            _trainingTracker.GuidanceModeChanged -= OnTrainingGuidanceModeChanged;
        }
        if (_plantControlAuthorityDispatcher is not null)
        {
            _plantControlAuthorityDispatcher.AuthorityChanged -= OnPlantControlAuthorityChanged;
        }
        if (_sessionWorkspace is not null)
        {
            _sessionWorkspace.Changed -= OnSessionWorkspaceChanged;
        }
        _scenarioRecorder?.Dispose();
        _runtimeSubscriptionsDetached = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }
    public string Milestone { get; }
    public string Status { get; }
    public IReadOnlyList<ControlRoomWorkspaceDescriptor> Workspaces { get; }

    public ControlRoomWorkspaceDescriptor SelectedWorkspace
    {
        get => _selectedWorkspace;
        set
        {
            if (value is null || _selectedWorkspace == value)
            {
                return;
            }

            _selectedWorkspace = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedWorkspaceTitle));
            OnPropertyChanged(nameof(SelectedWorkspaceDescription));
            OnPropertyChanged(nameof(SelectedWorkspaceContextText));
            OnPropertyChanged(nameof(IsReactorWorkspaceSelected));
            OnPropertyChanged(nameof(IsPrimaryWorkspaceSelected));
            OnPropertyChanged(nameof(IsTurbineWorkspaceSelected));
            OnPropertyChanged(nameof(IsElectricalWorkspaceSelected));
            OnPropertyChanged(nameof(IsAlarmsWorkspaceSelected));
            OnPropertyChanged(nameof(IsOperatorComputerWorkspaceSelected));
            OnPropertyChanged(nameof(IsMainWorkspaceScrollVisible));
            OnPropertyChanged(nameof(IsShellHostWorkspaceSelected));
            OnPropertyChanged(nameof(IsOverviewWorkspaceSelected));
        }
    }

    public string SelectedWorkspaceTitle => SelectedWorkspace.Title;
    public string SelectedWorkspaceDescription => SelectedWorkspace.Description;
    public string SelectedWorkspaceContextText => SelectedWorkspace.Id switch
    {
        ControlRoomWorkspaceId.Reactor => ReactorCore.ProtectionText,
        ControlRoomWorkspaceId.PrimaryCircuit => PrimaryContextText,
        ControlRoomWorkspaceId.TurbineSecondary => TurbineContextText,
        ControlRoomWorkspaceId.Electrical => ElectricalContextText,
        ControlRoomWorkspaceId.AlarmsEvents => AlarmContextText,
        ControlRoomWorkspaceId.OperatorComputer => "Utility workstation for guidance, diagnostics, canonical commands, authority modes, log and replay-backed session tools.",
        _ => SelectedMimicElement is null ? OperatorCurrentConditionText : $"{SelectedMimicTitle} · {SelectedMimicStatusText} · {SelectedMimicValuesText}",
    };
    public bool IsReactorWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.Reactor;
    public bool IsPrimaryWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.PrimaryCircuit;
    public bool IsTurbineWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.TurbineSecondary;
    public bool IsElectricalWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.Electrical;
    public bool IsAlarmsWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.AlarmsEvents;
    public bool IsOperatorComputerWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.OperatorComputer;
    public bool IsMainWorkspaceScrollVisible => !IsOperatorComputerWorkspaceSelected;
    public bool IsOverviewWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.Overview;
    public bool IsShellHostWorkspaceSelected => IsOverviewWorkspaceSelected;

    public OperatorComputerViewModel OperatorComputer { get; }

    public ControlRoomPerformanceBudget PerformanceBudget { get; }

    public string PerformanceBudgetText =>
        $"Presentation budget: ≤ {PerformanceBudget.MaximumPresentationRefreshHertz} Hz · " +
        $"≤ {PerformanceBudget.MaximumVisibleWorkspaceRows} visible rows · " +
        $"≤ {PerformanceBudget.MaximumVisibleTrendSeries} live trend series";


    public string PreStartupGuidanceText => _preStartupGuidance is null
        ? "No guided preparation plan loaded."
        : string.Join(Environment.NewLine, _preStartupGuidance.Steps.Select(step =>
            $"{step.Sequence}. {step.Title} — {step.Instruction}"));

    public string PreStartupChecklistText
    {
        get
        {
            if (_preStartupGuidance is null)
            {
                return "No pre-start checklist loaded.";
            }

            var results = _preStartupChecklistEvaluator.Evaluate(_snapshot, _preStartupGuidance.Checks);
            return string.Join(Environment.NewLine, results.Select(static result =>
                $"{(result.IsSatisfied ? "✓" : "○")} {result.Definition.Title}: {result.Observation}"));
        }
    }

    public string ScenarioGuidanceText
    {
        get
        {
            if (_trainingTracker?.GuidanceMode == TrainingGuidanceMode.Hidden)
            {
                return "Procedure guidance is hidden. Evaluation continues deterministically from plant checkpoints and accepted operator actions.";
            }
            if (_trainingTracker?.GuidanceMode == TrainingGuidanceMode.ChecklistOnly)
            {
                return "Checklist-only mode: step-by-step procedure guidance is suppressed. Evaluation semantics are unchanged.";
            }

            if (_powerManoeuvringGuidance is not null)
            {
                return string.Join(Environment.NewLine, _powerManoeuvringGuidance.Steps.Select(step =>
                    $"{step.Sequence}. {step.Title} — {step.Instruction}"));
            }

            if (_gridSynchronizationGuidance is not null)
            {
                return string.Join(Environment.NewLine, _gridSynchronizationGuidance.Steps.Select(step =>
                    $"{step.Sequence}. {step.Title} — {step.Instruction}"));
            }

            if (_heatUpTurbineStartupGuidance is not null)
            {
                return string.Join(Environment.NewLine, _heatUpTurbineStartupGuidance.Steps.Select(step =>
                    $"{step.Sequence}. {step.Title} — {step.Instruction}"));
            }

            return _firstCriticalityGuidance is null
                ? PreStartupGuidanceText
                : string.Join(Environment.NewLine, _firstCriticalityGuidance.Steps.Select(step =>
                    $"{step.Sequence}. {step.Title} — {step.Instruction}"));
        }
    }

    public string ScenarioChecklistText
    {
        get
        {
            if (_powerManoeuvringGuidance is not null)
            {
                var manoeuvringResults = _powerManoeuvringChecklistEvaluator.Evaluate(_snapshot, _powerManoeuvringGuidance.Checks);
                return string.Join(Environment.NewLine, manoeuvringResults.Select(static result =>
                    $"{(result.IsSatisfied ? "✓" : "○")} {result.Definition.Title}: {result.Observation}"));
            }

            if (_gridSynchronizationGuidance is not null)
            {
                var synchronizationResults = _gridSynchronizationChecklistEvaluator.Evaluate(_snapshot, _gridSynchronizationGuidance.Checks);
                return string.Join(Environment.NewLine, synchronizationResults.Select(static result =>
                    $"{(result.IsSatisfied ? "✓" : "○")} {result.Definition.Title}: {result.Observation}"));
            }

            if (_heatUpTurbineStartupGuidance is not null)
            {
                var startupResults = _heatUpTurbineStartupChecklistEvaluator.Evaluate(_snapshot, _heatUpTurbineStartupGuidance.Checks);
                return string.Join(Environment.NewLine, startupResults.Select(static result =>
                    $"{(result.IsSatisfied ? "✓" : "○")} {result.Definition.Title}: {result.Observation}"));
            }

            if (_firstCriticalityGuidance is null)
            {
                return PreStartupChecklistText;
            }

            var results = _firstCriticalityChecklistEvaluator.Evaluate(_snapshot, _firstCriticalityGuidance.Checks);
            return string.Join(Environment.NewLine, results.Select(static result =>
                $"{(result.IsSatisfied ? "✓" : "○")} {result.Definition.Title}: {result.Observation}"));
        }
    }

    public string TrainingGuidanceModeText => _trainingTracker is null
        ? "No training evaluation loaded"
        : _trainingTracker.GuidanceMode switch
        {
            TrainingGuidanceMode.Hidden => "HIDDEN — procedure assistance suppressed; scoring unchanged",
            TrainingGuidanceMode.ChecklistOnly => "CHECKLIST ONLY — checkpoints visible; step-by-step guidance suppressed",
            TrainingGuidanceMode.Guided => "GUIDED — procedure steps and checkpoints visible; scoring unchanged",
            _ => "UNKNOWN",
        };

    public string TrainingAssessmentText
    {
        get
        {
            if (_trainingTracker is null)
            {
                return "No deterministic training assessment loaded.";
            }

            var assessment = _trainingTracker.Assessment;
            var objectiveLines = assessment.Objectives.Select(objective =>
                $"{(objective.IsAchieved ? "✓" : "○")} {objective.Objective.Title}: {objective.Score}/{objective.MaximumScore}");
            var penaltyLines = assessment.Penalties.Where(static penalty => penalty.IsTriggered).Select(penalty =>
                $"−{penalty.Definition.Points} {penalty.Definition.Title}");
            var details = objectiveLines.Concat(penaltyLines);
            return $"SCORE {assessment.TotalScore}/{assessment.MaximumScore} · objective {assessment.ObjectiveScore} · penalties {assessment.PenaltyPoints}"
                + Environment.NewLine + string.Join(Environment.NewLine, details);
        }
    }

    public long LogicalStep => _snapshot.LogicalStep;

    public string RuntimeState => _snapshot.RunState switch
    {
        ControlRoomRunState.Running => "RUNNING",
        ControlRoomRunState.Paused => "PAUSED",
        _ => "SHELL ONLY",
    };

    public bool IsRuntimeRunning => _snapshot.RunState == ControlRoomRunState.Running;

    public string RuntimeProgressText => $"STEP {LogicalStep.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public string TrainingScoreText => _trainingTracker is null
        ? "—"
        : $"{_trainingTracker.Assessment.TotalScore}/{_trainingTracker.Assessment.MaximumScore}";

    public string GuidanceModeShortText => _trainingTracker?.GuidanceMode switch
    {
        TrainingGuidanceMode.Hidden => "HIDDEN",
        TrainingGuidanceMode.ChecklistOnly => "CHECKLIST",
        TrainingGuidanceMode.Guided => "GUIDED",
        _ => "—",
    };

    public string ControlAuthorityText
    {
        get
        {
            if (_plantControlAuthorityDispatcher is null)
            {
                return "—";
            }

            var automation = _plantControlAuthorityDispatcher.CurrentAutomation;
            if (!automation.IsAvailable)
            {
                return "UNAVAILABLE";
            }

            return automation.EffectiveAuthority switch
            {
                NuclearReactorSimulator.Domain.Physics.Control.Supervisory.PlantControlAuthorityMode.Manual => "MANUAL",
                NuclearReactorSimulator.Domain.Physics.Control.Supervisory.PlantControlAuthorityMode.Assisted => "ASSISTED",
                NuclearReactorSimulator.Domain.Physics.Control.Supervisory.PlantControlAuthorityMode.SupervisoryAutomatic => "SUPERVISORY",
                _ => "—",
            };
        }
    }

    public string ElectricalOutputText => Electrical.GrossElectricalOutput.State == ControlRoomVisualState.Unavailable
        ? "—"
        : $"{Electrical.GrossElectricalOutput.ValueText} {Electrical.GrossElectricalOutput.Unit}".TrimEnd();

    public string ProtectionStateText => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? "UNAVAILABLE"
        : _snapshot.AnyTripActive ? "TRIP ACTIVE" : "NORMAL";

    public string FirstOutStripText => AlarmEvents.FirstOutCount == 0
        ? "NO FIRST-OUT"
        : $"{AlarmEvents.FirstOutCount} FIRST-OUT ACTIVE";

    public string LatestEventText
    {
        get
        {
            var events = _operationalHistory.Current.Events;
            if (events.Count == 0)
            {
                return "No alarm/event transitions recorded in this session.";
            }

            var latest = events[0];
            return $"{latest.LogicalStepText} · {latest.KindText} · {latest.AlarmTitle}";
        }
    }

    public string SignalHealthText => _snapshot.TotalMeasuredSignalCount == 0
        ? "No runtime measured frame published yet"
        : $"{_snapshot.ValidMeasuredSignalCount}/{_snapshot.TotalMeasuredSignalCount} measured signals valid";

    public int AnnunciatedAlarmCount => _snapshot.AnnunciatedAlarmCount;
    public int UnacknowledgedAlarmCount => _snapshot.UnacknowledgedAlarmCount;
    public string AnnunciatedAlarmCountText => AnnunciatedAlarmCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string UnacknowledgedAlarmCountText => UnacknowledgedAlarmCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string LogicalStepText => LogicalStep.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public ControlRoomVisualState UnacknowledgedAlarmVisualState => UnacknowledgedAlarmCount > 0
        ? ControlRoomVisualState.Warning
        : ControlRoomVisualState.Normal;

    public string ProtectionSummary => _snapshot.AnyTripActive
        ? BuildProtectionSummary(_snapshot)
        : "No latched reactor/turbine/generator trip in presentation snapshot";

    public string FaultLifecycleText => _snapshot.Faults.Faults.Count == 0
        ? "No declared faults in the loaded training scenario"
        : $"{_snapshot.Faults.ActiveCount} active · {_snapshot.Faults.PendingCount} pending · {_snapshot.Faults.ClearedCount} cleared";

    public string PrimaryContextText => PrimaryCircuit.Loops.Count == 0
        ? "No primary-circuit presentation snapshot published yet"
        : $"{PrimaryCircuit.Loops.Count} loops · {PrimaryCircuit.Pumps.Count} MCPs · {PrimaryCircuit.SteamDrums.Count} steam drums · {PrimaryCircuit.Valves.Count} primary-connected valves";

    public ReactorCorePanelSnapshot ReactorCore => _snapshot.ReactorCore;

    public PrimaryCircuitPanelSnapshot PrimaryCircuit => _snapshot.PrimaryCircuit;

    public TurbineSecondaryPanelSnapshot TurbineSecondary => _snapshot.TurbineSecondary;

    public ElectricalPanelSnapshot Electrical => _snapshot.Electrical;

    public ControlRoomInstrumentTrendSnapshot ReactorThermalPowerTrend => _reactorThermalPowerTrend;


    public ControlRoomInstrumentTrendSnapshot GrossElectricalOutputTrend => _grossElectricalOutputTrend;

    public ControlRoomPlantMimicSnapshot PlantMimic => _plantMimic;

    public ControlRoomSubsystemSchematicsSnapshot SubsystemSchematics => _subsystemSchematics;

    public ControlRoomSubsystemSchematicSnapshot ReactorCoreSchematic => _subsystemSchematics.ReactorCore;

    public ControlRoomSubsystemSchematicSnapshot PrimarySteamDrumSchematic => _subsystemSchematics.PrimarySteamDrum;

    public ControlRoomSubsystemSchematicSnapshot TurbineSecondarySchematic => _subsystemSchematics.TurbineSecondary;

    public ControlRoomSubsystemSchematicSnapshot GeneratorGridSchematic => _subsystemSchematics.GeneratorGrid;

    public ControlRoomSubsystemSchematicSnapshot InstrumentationProtectionSchematic => _subsystemSchematics.InstrumentationProtection;

    public string GeneratorPowerPathDiagnosticText => ControlRoomSubsystemSchematicProjector.BuildGeneratorPowerPathDiagnostic(_snapshot);

    public string? SelectedMimicElementId
    {
        get => _selectedMimicElementId;
        set
        {
            if (string.Equals(_selectedMimicElementId, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedMimicElementId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedMimicElement));
            OnPropertyChanged(nameof(SelectedMimicTitle));
            OnPropertyChanged(nameof(SelectedMimicStatusText));
            OnPropertyChanged(nameof(SelectedMimicValuesText));
            OnPropertyChanged(nameof(SelectedMimicInputText));
            OnPropertyChanged(nameof(SelectedMimicOutputText));
            OnPropertyChanged(nameof(SelectedMimicDetailText));
            OnPropertyChanged(nameof(SelectedWorkspaceContextText));
        }
    }

    public ControlRoomPlantMimicElementSnapshot? SelectedMimicElement =>
        PlantMimic.Elements.FirstOrDefault(element => string.Equals(element.ElementId, SelectedMimicElementId, StringComparison.Ordinal))
        ?? PlantMimic.Elements.FirstOrDefault();

    public string SelectedMimicTitle => SelectedMimicElement?.DisplayName ?? "NO EQUIPMENT SELECTED";

    public string SelectedMimicStatusText => SelectedMimicElement?.StatusText ?? "MIMIC DATA UNAVAILABLE";

    public string SelectedMimicValuesText => SelectedMimicElement is { } element
        ? $"{element.PrimaryValueText} · {element.SecondaryValueText}"
        : "VALUES —";

    public string SelectedMimicInputText => SelectedMimicElement?.InputText ?? "IN —";

    public string SelectedMimicOutputText => SelectedMimicElement?.OutputText ?? "OUT —";

    public string SelectedMimicDetailText => SelectedMimicElement?.DetailText ?? "Select an equipment item in the plant mimic for canonical context.";

    public AlarmEventsPanelSnapshot AlarmEvents => _snapshot.AlarmEvents;

    public ControlRoomOperationalHistorySnapshot OperationalHistory => _operationalHistory.Current;

    public string TurbineContextText => TurbineSecondary.Rotors.Count == 0
        ? "No turbine/secondary presentation snapshot published yet"
        : $"{TurbineSecondary.SteamLines.Count} steam lines · {TurbineSecondary.Rotors.Count} rotors · {TurbineSecondary.Condensers.Count} condensers · {TurbineSecondary.FeedwaterTrains.Count} feedwater trains";

    public string ElectricalContextText => Electrical.Generators.Count == 0
        ? "No generator/electrical presentation snapshot published yet"
        : $"Grid {Electrical.Grid.GridId} · {Electrical.Generators.Count} generators · gross {Electrical.GrossElectricalOutput.ValueText} {Electrical.GrossElectricalOutput.Unit}".TrimEnd();

    public string AlarmOptionsText => AlarmEvents.Alarms.Count == 0
        ? "NO ALARM TARGETS"
        : string.Join("|", AlarmEvents.Alarms.Select(static alarm => alarm.AlarmId));

    public int SelectedAlarmIndex
    {
        get => _selectedAlarmIndex;
        set
        {
            var maximum = Math.Max(0, AlarmEvents.Alarms.Count - 1);
            var next = Math.Clamp(value, 0, maximum);
            if (_selectedAlarmIndex == next)
            {
                return;
            }

            _selectedAlarmIndex = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedAlarmId));
            OnPropertyChanged(nameof(SelectedAlarmStatusText));
            OnPropertyChanged(nameof(AlarmAcknowledgeCommandState));
            OnPropertyChanged(nameof(AlarmResetCommandState));
        }
    }

    public string SelectedAlarmId => AlarmEvents.Alarms.Count == 0
        ? "—"
        : AlarmEvents.Alarms[Math.Clamp(_selectedAlarmIndex, 0, AlarmEvents.Alarms.Count - 1)].AlarmId;

    public string SelectedAlarmStatusText
    {
        get
        {
            if (AlarmEvents.Alarms.Count == 0)
            {
                return "No alarm selected";
            }

            var alarm = AlarmEvents.Alarms[Math.Clamp(_selectedAlarmIndex, 0, AlarmEvents.Alarms.Count - 1)];
            return $"{alarm.Title} · {alarm.AnnunciatorText}";
        }
    }

    public ControlRoomVisualState AlarmAcknowledgeCommandState
    {
        get
        {
            if (_snapshot.RunState == ControlRoomRunState.ShellOnly || AlarmEvents.Alarms.Count == 0)
            {
                return ControlRoomVisualState.Unavailable;
            }

            var alarm = AlarmEvents.Alarms[Math.Clamp(_selectedAlarmIndex, 0, AlarmEvents.Alarms.Count - 1)];
            return alarm.CanAcknowledge ? alarm.VisualState : ControlRoomVisualState.Unavailable;
        }
    }

    public ControlRoomVisualState AlarmResetCommandState
    {
        get
        {
            if (_snapshot.RunState == ControlRoomRunState.ShellOnly || AlarmEvents.Alarms.Count == 0)
            {
                return ControlRoomVisualState.Unavailable;
            }

            var alarm = AlarmEvents.Alarms[Math.Clamp(_selectedAlarmIndex, 0, AlarmEvents.Alarms.Count - 1)];
            return alarm.CanReset ? ControlRoomVisualState.Normal : ControlRoomVisualState.Unavailable;
        }
    }

    public ControlRoomVisualState AlarmSelectionState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly || AlarmEvents.Alarms.Count == 0
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public ControlRoomVisualState AlarmAcknowledgeAllCommandState =>
        AlarmSelectionState == ControlRoomVisualState.Unavailable || !AlarmEvents.Alarms.Any(static alarm => alarm.CanAcknowledge)
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Warning;

    public ControlRoomVisualState AlarmResetAllCommandState =>
        AlarmSelectionState == ControlRoomVisualState.Unavailable || !AlarmEvents.Alarms.Any(static alarm => alarm.CanReset)
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public string AlarmContextText => AlarmEvents.Alarms.Count == 0
        ? "No M5.6 alarm presentation snapshot published yet"
        : $"{AlarmEvents.AnnunciatedCount} annunciated · {AlarmEvents.UnacknowledgedCount} unacknowledged · {AlarmEvents.FirstOutCount} first-out groups active";


    private IReadOnlyList<PrimaryCircuitPumpPresentationSnapshot> CommandablePumps =>
        PrimaryCircuit.Pumps.Where(static pump => pump.IsOperatorCommandable).ToArray();

    public string PumpOptionsText => CommandablePumps.Count == 0
        ? "NO MCP TARGETS"
        : string.Join("|", CommandablePumps.Select(static pump => pump.PumpId));

    public int SelectedPumpIndex
    {
        get => _selectedPumpIndex;
        set
        {
            var maximum = Math.Max(0, CommandablePumps.Count - 1);
            var next = Math.Clamp(value, 0, maximum);
            if (_selectedPumpIndex == next)
            {
                return;
            }

            _selectedPumpIndex = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedPumpId));
            OnPropertyChanged(nameof(SelectedPumpText));
            OnPropertyChanged(nameof(PumpStartCommandActive));
            OnPropertyChanged(nameof(PumpStopCommandActive));
            OnPropertyChanged(nameof(PumpStartCommandEnabled));
            OnPropertyChanged(nameof(PumpStopCommandEnabled));
        }
    }

    private PrimaryCircuitPumpPresentationSnapshot? SelectedPump => CommandablePumps.Count == 0
        ? null
        : CommandablePumps[Math.Clamp(_selectedPumpIndex, 0, CommandablePumps.Count - 1)];

    public string SelectedPumpId => SelectedPump?.PumpId ?? "—";

    public string SelectedPumpText => SelectedPump is null
        ? "Selected canonical pump: —"
        : $"Selected canonical pump: {SelectedPump.PumpId} · ACTUAL STATE: {SelectedPump.OperatingText}";

    public ControlRoomVisualState PumpCommandState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly || CommandablePumps.Count == 0
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public bool PumpStartCommandActive => SelectedPump?.IsRunning == true;

    public bool PumpStopCommandActive => SelectedPump is { IsRunning: false };

    public bool PumpStartCommandEnabled => PumpCommandState != ControlRoomVisualState.Unavailable && !PumpStartCommandActive;

    public bool PumpStopCommandEnabled => PumpCommandState != ControlRoomVisualState.Unavailable && !PumpStopCommandActive;

    public string GeneratorOptionsText => Electrical.Generators.Count == 0
        ? "NO GENERATOR TARGETS"
        : string.Join("|", Electrical.Generators.Select(static generator => generator.GeneratorId));

    public int SelectedGeneratorIndex
    {
        get => _selectedGeneratorIndex;
        set
        {
            var maximum = Math.Max(0, Electrical.Generators.Count - 1);
            var next = Math.Clamp(value, 0, maximum);
            if (_selectedGeneratorIndex == next)
            {
                return;
            }

            _selectedGeneratorIndex = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedGeneratorId));
            OnPropertyChanged(nameof(SelectedBreakerId));
            OnPropertyChanged(nameof(SelectedGeneratorSynchronizationText));
            OnPropertyChanged(nameof(SelectedGeneratorSynchronizationDetailText));
            OnPropertyChanged(nameof(SelectedGeneratorRotorId));
            OnPropertyChanged(nameof(BreakerCloseCommandState));
            OnPropertyChanged(nameof(TurbineSpeedCommandState));
            OnPropertyChanged(nameof(GeneratorLoadCommandState));
            OnPropertyChanged(nameof(BreakerOpenCommandState));
            OnPropertyChanged(nameof(BreakerCloseCommandActive));
            OnPropertyChanged(nameof(BreakerCloseCommandEnabled));
            OnPropertyChanged(nameof(BreakerOpenCommandActive));
            OnPropertyChanged(nameof(BreakerOpenCommandEnabled));
        }
    }

    private GeneratorPresentationSnapshot? SelectedGenerator => Electrical.Generators.Count == 0
        ? null
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)];

    public string SelectedGeneratorId => SelectedGenerator?.GeneratorId ?? "—";

    public string SelectedBreakerId => SelectedGenerator?.BreakerId ?? "—";

    public string SelectedGeneratorSynchronizationText => Electrical.Generators.Count == 0
        ? "No generator selected"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].DisplaySynchronizationText;

    public string SelectedGeneratorSynchronizationDetailText => Electrical.Generators.Count == 0
        ? "No generator selected"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].SynchronizationDetailText;

    public string SelectedGeneratorRotorId => Electrical.Generators.Count == 0
        ? "—"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].RotorId;

    public ControlRoomVisualState TurbineTripCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : TurbineSecondary.TurbineTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;

    public bool TurbineTripCommandEnabled => _snapshot.RunState != ControlRoomRunState.ShellOnly && !TurbineSecondary.TurbineTripActive;

    public string TurbineTripCommandLabel => TurbineSecondary.TurbineTripActive ? "TURBINE TRIP — ACTIVE" : "TURBINE TRIP";

    public ControlRoomVisualState GeneratorSelectionState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly || Electrical.Generators.Count == 0
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public ControlRoomVisualState GeneratorTripCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : Electrical.GeneratorTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;

    public bool GeneratorTripCommandEnabled => _snapshot.RunState != ControlRoomRunState.ShellOnly && !Electrical.GeneratorTripActive;

    public string GeneratorTripCommandLabel => Electrical.GeneratorTripActive ? "GENERATOR TRIP — ACTIVE" : "GENERATOR TRIP";

    public ControlRoomVisualState TurbineSpeedCommandState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly
        || Electrical.Generators.Count == 0
        || TurbineSecondary.TurbineTripActive
        || Electrical.GeneratorTripActive
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public ControlRoomVisualState GeneratorLoadCommandState
    {
        get
        {
            if (TurbineSpeedCommandState == ControlRoomVisualState.Unavailable)
            {
                return ControlRoomVisualState.Unavailable;
            }

            var generator = Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)];
            return generator.BreakerClosed ? ControlRoomVisualState.Normal : ControlRoomVisualState.Unavailable;
        }
    }

    public ControlRoomVisualState BreakerCloseCommandState
    {
        get
        {
            if (_snapshot.RunState == ControlRoomRunState.ShellOnly || SelectedGenerator is null)
            {
                return ControlRoomVisualState.Unavailable;
            }

            if (SelectedGenerator.BreakerClosed)
            {
                return ControlRoomVisualState.Normal;
            }

            if (Electrical.GeneratorTripActive)
            {
                return ControlRoomVisualState.Unavailable;
            }

            return SelectedGenerator.SynchronizationConditionsSatisfied
                ? ControlRoomVisualState.Normal
                : ControlRoomVisualState.Warning;
        }
    }

    public bool BreakerCloseCommandActive => SelectedGenerator?.BreakerClosed == true;

    public bool BreakerCloseCommandEnabled =>
        BreakerCloseCommandState == ControlRoomVisualState.Normal
        && !BreakerCloseCommandActive
        && !Electrical.GeneratorTripActive;

    public ControlRoomVisualState BreakerOpenCommandState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly || SelectedGenerator is null
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public bool BreakerOpenCommandActive => SelectedGenerator is { BreakerClosed: false };

    public bool BreakerOpenCommandEnabled => BreakerOpenCommandState != ControlRoomVisualState.Unavailable && !BreakerOpenCommandActive;

    public string RodOptionsText => ReactorCore.RodTargets.Count == 0
        ? "NO ROD TARGETS"
        : string.Join("|", ReactorCore.RodTargets.Select(static target => target.Label));

    public int SelectedRodIndex
    {
        get => _selectedRodIndex;
        set
        {
            var maximum = Math.Max(0, ReactorCore.RodTargets.Count - 1);
            var next = Math.Clamp(value, 0, maximum);
            if (_selectedRodIndex == next)
            {
                return;
            }

            _selectedRodIndex = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRodId));
            OnPropertyChanged(nameof(SelectedRodTargetKind));
            OnPropertyChanged(nameof(SelectedRodMotionText));
            OnPropertyChanged(nameof(RodInsertCommandActive));
            OnPropertyChanged(nameof(RodHoldCommandActive));
            OnPropertyChanged(nameof(RodWithdrawCommandActive));
            OnPropertyChanged(nameof(RodInsertCommandEnabled));
            OnPropertyChanged(nameof(RodHoldCommandEnabled));
            OnPropertyChanged(nameof(RodWithdrawCommandEnabled));
        }
    }

    private ReactorRodTargetPresentationSnapshot? SelectedRodTarget => ReactorCore.RodTargets.Count == 0
        ? null
        : ReactorCore.RodTargets[Math.Clamp(_selectedRodIndex, 0, ReactorCore.RodTargets.Count - 1)];

    public string SelectedRodId => SelectedRodTarget?.TargetId ?? "—";

    public ControlRoomCommandTargetKind? SelectedRodTargetKind => SelectedRodTarget?.TargetKind;

    public string SelectedRodMotionText => SelectedRodTarget is null
        ? "ACTUAL MOTION · unavailable"
        : $"ACTUAL MOTION · {SelectedRodTarget.EffectiveMotion}";

    public bool RodInsertCommandActive => string.Equals(SelectedRodTarget?.EffectiveMotion, "INSERT", StringComparison.Ordinal);

    public bool RodHoldCommandActive => string.Equals(SelectedRodTarget?.EffectiveMotion, "HOLD", StringComparison.Ordinal);

    public bool RodWithdrawCommandActive => string.Equals(SelectedRodTarget?.EffectiveMotion, "WITHDRAW", StringComparison.Ordinal);

    public ControlRoomVisualState ReactorCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : ControlRoomVisualState.Normal;

    public ControlRoomVisualState ScramCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : ReactorCore.ReactorScramActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;

    public bool ScramCommandEnabled => _snapshot.RunState != ControlRoomRunState.ShellOnly && !ReactorCore.ReactorScramActive;

    public string ScramCommandLabel => ReactorCore.ReactorScramActive ? "SCRAM — ACTIVE" : "SCRAM";

    public ControlRoomVisualState ProtectionResetCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : _snapshot.ProtectionReset.State;

    public bool ProtectionResetCommandEnabled => _snapshot.RunState != ControlRoomRunState.ShellOnly && _snapshot.ProtectionReset.CanResetNow;

    public string ProtectionResetStatusText => _snapshot.ProtectionReset.StatusText;

    public ControlRoomVisualState RodCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly || ReactorCore.RodTargets.Count == 0
        ? ControlRoomVisualState.Unavailable
        : ControlRoomVisualState.Normal;

    public ControlRoomVisualState RodWithdrawCommandState =>
        RodCommandState == ControlRoomVisualState.Unavailable
            ? ControlRoomVisualState.Unavailable
            : ReactorCore.RodWithdrawalInhibited
                ? ControlRoomVisualState.Warning
                : ControlRoomVisualState.Normal;

    public bool RodInsertCommandEnabled => RodCommandState != ControlRoomVisualState.Unavailable && !RodInsertCommandActive;

    public bool RodHoldCommandEnabled => RodCommandState != ControlRoomVisualState.Unavailable && !RodHoldCommandActive;

    public bool RodWithdrawCommandEnabled => RodWithdrawCommandState == ControlRoomVisualState.Normal && !RodWithdrawCommandActive;

    public string XenonAvailabilityText => ReactorCore.XenonReactivity.State == ControlRoomVisualState.Unavailable
        ? "Canonical iodine/xenon state is unavailable for this runtime configuration; no value is fabricated."
        : "Canonical M2.8 xenon reactivity is promoted through the operational snapshot; negative values indicate modeled poisoning worth.";

    public string OperatorCurrentConditionText => BuildOperatorCurrentConditionText();

    public string OperatorNextActionText => BuildOperatorNextActionText();

    public string StartupToPowerCommandPathText => _trainingTracker?.GuidanceMode switch
    {
        TrainingGuidanceMode.Hidden => "Procedure guidance is hidden. The startup-to-power command map is intentionally suppressed.",
        TrainingGuidanceMode.ChecklistOnly => "Checklist-only assistance is active. The step-by-step startup-to-power command map is intentionally suppressed.",
        _ => StartupToPowerCommandPath,
    };

    public string CommandStatus
    {
        get => _commandStatus;
        private set
        {
            if (string.Equals(_commandStatus, value, StringComparison.Ordinal))
            {
                return;
            }

            _commandStatus = value;
            OnPropertyChanged();
        }
    }

    public string LastControlActionText
    {
        get => _lastControlActionText;
        private set
        {
            if (string.Equals(_lastControlActionText, value, StringComparison.Ordinal))
            {
                return;
            }

            _lastControlActionText = value;
            OnPropertyChanged();
        }
    }

    public ICommand RunCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand SingleStepCommand { get; }
    public ICommand ReactorScramCommand { get; }
    public ICommand ProtectionResetCommand { get; }
    public ICommand RodInsertCommand { get; }
    public ICommand RodHoldCommand { get; }
    public ICommand RodWithdrawCommand { get; }
    public ICommand PumpStartCommand { get; }
    public ICommand PumpStopCommand { get; }
    public ICommand TurbineTripCommand { get; }
    public ICommand GeneratorTripCommand { get; }
    public ICommand GeneratorBreakerCloseCommand { get; }
    public ICommand GeneratorBreakerOpenCommand { get; }
    public ICommand TurbineSpeedRaiseCommand { get; }
    public ICommand TurbineSpeedLowerCommand { get; }
    public ICommand GeneratorLoadRaiseCommand { get; }
    public ICommand GeneratorLoadLowerCommand { get; }
    public ICommand AlarmAcknowledgeCommand { get; }
    public ICommand AlarmResetCommand { get; }
    public ICommand AlarmAcknowledgeAllCommand { get; }
    public ICommand AlarmResetAllCommand { get; }
    public ICommand OpenOperatorComputerGuidancePageCommand { get; }
    public ICommand OpenOperatorComputerInfoPageCommand { get; }
    public ICommand OpenOperatorComputerAlarmsPageCommand { get; }
    public ICommand OpenOperatorComputerCommandsPageCommand { get; }
    public ICommand OpenOperatorComputerModesPageCommand { get; }
    public ICommand OpenOperatorComputerDiagnosticsPageCommand { get; }
    public ICommand OpenOperatorComputerLogPageCommand { get; }
    public ICommand OpenOperatorComputerSessionPageCommand { get; }
    public ICommand OpenSelectedMimicSubsystemCommand { get; }

    private void OpenSelectedMimicSubsystem()
    {
        var element = SelectedMimicElement;
        if (element is null)
        {
            return;
        }

        var workspace = Workspaces.FirstOrDefault(candidate => candidate.Id == element.DrillDownWorkspaceId);
        if (workspace is not null)
        {
            SelectedWorkspace = workspace;
        }
    }

    private string BuildOperatorCurrentConditionText()
    {
        if (_snapshot.RunState == ControlRoomRunState.ShellOnly)
        {
            return "NO INTEGRATED PLANT SESSION";
        }

        var generator = Electrical.Generators.FirstOrDefault();
        var generatorState = generator is null
            ? "GENERATOR UNAVAILABLE"
            : generator.BreakerClosed
                ? "GENERATOR PARALLELED"
                : "GENERATOR ISOLATED";
        var protection = _snapshot.AnyTripActive ? $"PROTECTION ACTIVE: {BuildProtectionSummary(_snapshot)}" : "PROTECTION CLEAR";
        return $"{generatorState} · {protection} · GROSS OUTPUT {Electrical.GrossElectricalOutput.ValueText} {Electrical.GrossElectricalOutput.Unit}";
    }

    private string BuildOperatorNextActionText()
    {
        if (_trainingTracker?.GuidanceMode == TrainingGuidanceMode.Hidden)
        {
            return "Procedure guidance is hidden. Use plant indications and protection status only; no next-action recommendation is shown.";
        }

        if (_trainingTracker?.GuidanceMode == TrainingGuidanceMode.ChecklistOnly)
        {
            return "Checklist-only assistance is active. Use OPERATIONAL CHECKS / F6 DIAGNOSTICS; step-by-step next-action recommendations are suppressed.";
        }

        if (_snapshot.AnyTripActive)
        {
            return $"1. Stabilize the plant and verify why {BuildProtectionSummary(_snapshot)} is latched.{Environment.NewLine}" +
                   $"2. When safe, use PROTECTION RESET. M5.5 will accept it only when canonical reset conditions and permissives are satisfied.{Environment.NewLine}" +
                   "3. Do not re-issue an already latched TRIP/SCRAM command.";
        }

        if (_powerManoeuvringGuidance is not null)
        {
            var results = _powerManoeuvringChecklistEvaluator.Evaluate(_snapshot, _powerManoeuvringGuidance.Checks)
                .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);
            var step = SelectCurrentPowerManoeuvringStep(results);
            if (step is not null)
            {
                var blockers = step.RequiredCheckIds
                    .Where(id => results.TryGetValue(id, out var result) && !result.IsSatisfied)
                    .Select(id => results[id].Observation)
                    .Take(3)
                    .ToArray();
                var suggested = step.SuggestedOperatorAction.HasValue
                    ? CommandDisplayName(step.SuggestedOperatorAction.Value)
                    : "OBSERVE / HOLD";
                return $"CURRENT OBJECTIVE: {step.Sequence}. {step.Title}{Environment.NewLine}" +
                       $"NEXT COMMAND/ACTION: {suggested}{Environment.NewLine}" +
                       $"{step.Instruction}" +
                       (blockers.Length == 0 ? string.Empty : $"{Environment.NewLine}WAIT / VERIFY: {string.Join(" · ", blockers)}");
            }
        }

        var generator = Electrical.Generators.FirstOrDefault();
        if (generator is not null && generator.BreakerClosed)
        {
            return "Generator is already PARALLELED. Raise electrical load in one bounded GENERATOR LOAD RAISE increment, observe turbine speed/reactor power, stabilize, then coordinate further load with deliberate rod WITHDRAW/HOLD as required.";
        }

        if (generator is not null)
        {
            return generator.SynchronizationConditionsSatisfied
                ? "SYNC READY. Issue CLOSE BREAKER once, then verify PARALLELED before applying electrical load."
                : $"SYNC NOT READY. {generator.SynchronizationDetailText} Use small TURBINE SPEED RAISE/LOWER corrections only as needed and wait for all three close-check dimensions to be OK.";
        }

        return "Use F1 GUIDANCE and F6 DIAGNOSTICS to follow the loaded canonical procedure. No generator target is currently published.";
    }

    private PowerManoeuvringStepDefinition? SelectCurrentPowerManoeuvringStep(
        IReadOnlyDictionary<string, PowerManoeuvringCheckResult> results)
    {
        if (_powerManoeuvringGuidance is null)
        {
            return null;
        }

        if (_trainingTracker?.Assessment is { } assessment)
        {
            var checkpoints = assessment.Checkpoints.ToDictionary(
                static checkpoint => checkpoint.Definition.CheckpointId,
                static checkpoint => checkpoint.IsSatisfied,
                StringComparer.Ordinal);

            bool Satisfied(string checkpointId)
                => checkpoints.TryGetValue(checkpointId, out var satisfied) && satisfied;

            var stepId = !Satisfied("stable-low-load") ? "verify-handoff"
                : !Satisfied("increased-load") ? "raise-load"
                : !Satisfied("temperature-feedback") || !Satisfied("void-feedback") || !Satisfied("xenon-boundary") ? "observe-feedback"
                : !Satisfied("reduced-load") ? "reduce-load"
                : !Satisfied("generator-unloaded") ? "unload"
                : !Satisfied("generator-disconnected") ? "disconnect"
                : !Satisfied("reactor-shutdown") ? "shutdown-reactor"
                : !Satisfied("post-shutdown-cooling") ? "post-shutdown"
                : null;

            return stepId is null
                ? null
                : _powerManoeuvringGuidance.Steps.Single(step => string.Equals(step.StepId, stepId, StringComparison.Ordinal));
        }

        return _powerManoeuvringGuidance.Steps.FirstOrDefault(candidate =>
            candidate.RequiredCheckIds.Any(id => !results.TryGetValue(id, out var result) || !result.IsSatisfied));
    }

    private static string CommandDisplayName(ControlRoomCommandKind kind) => kind switch
    {
        ControlRoomCommandKind.MainCirculationPumpStart => "START MAIN CIRCULATION PUMP",
        ControlRoomCommandKind.MainCirculationPumpStop => "STOP MAIN CIRCULATION PUMP",
        ControlRoomCommandKind.ReactorScram => "REACTOR SCRAM",
        ControlRoomCommandKind.TurbineTrip => "TURBINE TRIP",
        ControlRoomCommandKind.GeneratorTrip => "GENERATOR TRIP",
        ControlRoomCommandKind.ControlRodWithdraw => "CONTROL ROD WITHDRAW",
        ControlRoomCommandKind.ControlRodHold => "CONTROL ROD HOLD",
        ControlRoomCommandKind.ControlRodInsert => "CONTROL ROD INSERT",
        ControlRoomCommandKind.TurbineSpeedRaise => "TURBINE SPEED RAISE",
        ControlRoomCommandKind.TurbineSpeedLower => "TURBINE SPEED LOWER",
        ControlRoomCommandKind.GeneratorBreakerClose => "CLOSE GENERATOR BREAKER",
        ControlRoomCommandKind.GeneratorBreakerOpen => "OPEN GENERATOR BREAKER",
        ControlRoomCommandKind.GeneratorLoadRaise => "GENERATOR LOAD RAISE",
        ControlRoomCommandKind.GeneratorLoadLower => "GENERATOR LOAD LOWER",
        ControlRoomCommandKind.ProtectionReset => "PROTECTION RESET",
        _ => kind.ToString().ToUpperInvariant(),
    };

    private static string BuildStartupToPowerCommandPath()
    {
        static string Actions<TStep>(IEnumerable<TStep> steps, Func<TStep, int> sequence, Func<TStep, ControlRoomCommandKind?> action)
            => string.Join(
                " → ",
                steps.OrderBy(sequence)
                    .Select(step => action(step))
                    .Where(static value => value.HasValue)
                    .Select(static value => CommandDisplayName(value.GetValueOrDefault()))
                    .Distinct(StringComparer.Ordinal));

        var preStartup = Actions(
            ColdShutdownPreStartupProgram.Guidance.Steps,
            static step => step.Sequence,
            static step => step.SuggestedOperatorAction);
        var criticality = Actions(
            FirstCriticalityLowPowerProgram.Guidance.Steps,
            static step => step.Sequence,
            static step => step.SuggestedOperatorAction);
        var heatUp = Actions(
            HeatUpTurbineStartupProgram.Guidance.Steps,
            static step => step.Sequence,
            static step => step.SuggestedOperatorAction);
        var synchronization = Actions(
            GridSynchronizationLoadProgram.Guidance.Steps,
            static step => step.Sequence,
            static step => step.SuggestedOperatorAction);

        return $"1 PREPARE / CIRCULATION  {preStartup}{Environment.NewLine}" +
               $"2 FIRST CRITICALITY      {criticality}{Environment.NewLine}" +
               $"3 STEAM / TURBINE ROLL   {heatUp}{Environment.NewLine}" +
               $"4 SYNCHRONIZE / LOAD     {synchronization}{Environment.NewLine}" +
               "5 ON GRID                 GENERATOR LOAD RAISE → CONTROL ROD WITHDRAW/HOLD → STABILIZE → REPEAT IN SMALL INCREMENTS";
    }

    private static string BuildProtectionSummary(ControlRoomSnapshot snapshot)
    {
        var active = new List<string>(3);
        if (snapshot.ReactorScramActive)
        {
            active.Add("REACTOR SCRAM");
        }

        if (snapshot.TurbineTripActive)
        {
            active.Add("TURBINE TRIP");
        }

        if (snapshot.GeneratorTripActive)
        {
            active.Add("GENERATOR TRIP");
        }

        return string.Join(" · ", active);
    }

    internal void ReportRuntimeHostFailure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        CommandStatus = $"Runtime paused after deterministic step failure: {message}";
    }

    private void OpenOperatorComputerPage(OperatorComputerPageId pageId)
    {
        SelectedWorkspace = Workspaces.Single(static workspace => workspace.Id == ControlRoomWorkspaceId.OperatorComputer);
        OperatorComputer.SelectPage(pageId);
    }

    private void Dispatch(ControlRoomCommandKind kind)
    {
        TryDispatch(
            new ControlRoomCommand(kind),
            $"{kind} command dispatched through the application boundary. No simulation physics executed by the UI.");
    }

    private void DispatchRod(ControlRoomCommandKind kind)
    {
        if (ReactorCore.RodTargets.Count == 0 || !SelectedRodTargetKind.HasValue)
        {
            CommandStatus = $"{kind} not dispatched: no canonical rod/group target is available in the presentation snapshot.";
            return;
        }

        var target = SelectedRodId;
        var targetKind = SelectedRodTargetKind.Value;
        TryDispatch(
            new ControlRoomCommand(kind, target, targetKind),
            $"{kind} command for {targetKind} '{target}' dispatched through the application boundary.");
    }

    private void DispatchPump(ControlRoomCommandKind kind)
    {
        if (CommandablePumps.Count == 0)
        {
            CommandStatus = $"{kind} not dispatched: no commandable canonical main-circulation pump target is available.";
            return;
        }

        var target = SelectedPumpId;
        TryDispatch(
            new ControlRoomCommand(kind, target, ControlRoomCommandTargetKind.Pump),
            $"{kind} command for Pump '{target}' dispatched through the application boundary.");
    }

    private void DispatchBreaker(ControlRoomCommandKind kind)
    {
        if (Electrical.Generators.Count == 0)
        {
            CommandStatus = $"{kind} not dispatched: no canonical generator/breaker target is available.";
            return;
        }

        var target = SelectedBreakerId;
        TryDispatch(
            new ControlRoomCommand(kind, target, ControlRoomCommandTargetKind.Breaker),
            $"{kind} command for Breaker '{target}' dispatched through the application boundary.");
    }

    private void DispatchSelectedGeneratorRotor(ControlRoomCommandKind kind)
    {
        if (Electrical.Generators.Count == 0)
        {
            CommandStatus = $"{kind} not dispatched: no canonical turbine rotor target is available.";
            return;
        }

        var target = SelectedGeneratorRotorId;
        TryDispatch(
            new ControlRoomCommand(kind, target, ControlRoomCommandTargetKind.TurbineRotor),
            $"{kind} command for TurbineRotor '{target}' dispatched through the application boundary.");
    }

    private void DispatchSelectedGenerator(ControlRoomCommandKind kind)
    {
        if (Electrical.Generators.Count == 0)
        {
            CommandStatus = $"{kind} not dispatched: no canonical generator target is available.";
            return;
        }

        var target = SelectedGeneratorId;
        TryDispatch(
            new ControlRoomCommand(kind, target, ControlRoomCommandTargetKind.Generator),
            $"{kind} command for Generator '{target}' dispatched through the application boundary.");
    }

    private void DispatchAlarm(ControlRoomCommandKind kind)
    {
        if (AlarmEvents.Alarms.Count == 0)
        {
            CommandStatus = $"{kind} not dispatched: no canonical alarm target is available.";
            return;
        }

        var target = SelectedAlarmId;
        TryDispatch(
            new ControlRoomCommand(kind, target, ControlRoomCommandTargetKind.Alarm),
            $"{kind} command for Alarm '{target}' dispatched through the application boundary. Protection state is unaffected by annunciator commands.");
    }

    private void TryDispatch(ControlRoomCommand command, string successMessage)
    {
        try
        {
            _commandDispatcher.Dispatch(command);
            CommandStatus = successMessage;
            LastControlActionText = DescribeLastControlAction(command, accepted: true, reason: null);
        }
        catch (InvalidOperationException exception)
        {
            CommandStatus = $"Command blocked by the loaded scenario: {exception.Message}";
            LastControlActionText = DescribeLastControlAction(command, accepted: false, reason: exception.Message);
        }
    }

    private static string DescribeLastControlAction(ControlRoomCommand command, bool accepted, string? reason)
    {
        var target = string.IsNullOrWhiteSpace(command.TargetId) ? string.Empty : $" · {command.TargetId}";
        var prefix = accepted ? "LAST CONTROL ACTION · ACCEPTED" : "LAST CONTROL ACTION · BLOCKED";
        var description = $"{prefix} · {CommandDisplayName(command.Kind)}{target}";
        return accepted || string.IsNullOrWhiteSpace(reason) ? description : $"{description} · {reason}";
    }


    private OperatorComputerSnapshot ProjectOperatorComputerSnapshot()
    {
        var guidanceMode = _trainingTracker?.GuidanceMode ?? TrainingGuidanceMode.Guided;
        OperatorComputerScenarioContentSnapshot? scenarioContent = null;

        if (_powerManoeuvringGuidance is not null)
        {
            scenarioContent = OperatorComputerScenarioContentProjector.Project(
                _snapshot,
                _powerManoeuvringGuidance,
                _powerManoeuvringChecklistEvaluator,
                guidanceMode,
                _trainingTracker?.Assessment);
        }
        else if (_gridSynchronizationGuidance is not null)
        {
            scenarioContent = OperatorComputerScenarioContentProjector.Project(
                _snapshot,
                _gridSynchronizationGuidance,
                _gridSynchronizationChecklistEvaluator,
                guidanceMode,
                _trainingTracker?.Assessment);
        }
        else if (_heatUpTurbineStartupGuidance is not null)
        {
            scenarioContent = OperatorComputerScenarioContentProjector.Project(
                _snapshot,
                _heatUpTurbineStartupGuidance,
                _heatUpTurbineStartupChecklistEvaluator,
                guidanceMode,
                _trainingTracker?.Assessment);
        }
        else if (_firstCriticalityGuidance is not null)
        {
            scenarioContent = OperatorComputerScenarioContentProjector.Project(
                _snapshot,
                _firstCriticalityGuidance,
                _firstCriticalityChecklistEvaluator,
                guidanceMode,
                _trainingTracker?.Assessment);
        }
        else if (_preStartupGuidance is not null)
        {
            scenarioContent = OperatorComputerScenarioContentProjector.Project(
                _snapshot,
                _preStartupGuidance,
                _preStartupChecklistEvaluator,
                guidanceMode,
                _trainingTracker?.Assessment);
        }

        var modes = _plantControlAuthorityDispatcher is null
            ? null
            : new OperatorComputerModesSnapshot(
                guidanceMode,
                _plantControlAuthorityDispatcher.CurrentAutomation);

        return OperatorComputerSnapshotProjector.Project(
            _snapshot,
            scenarioContent,
            _operationalHistory.Current,
            _scenarioRecorder?.Events,
            _postIncidentAnalysis,
            modes,
            _sessionWorkspace?.Current);
    }

    private void OnSessionWorkspaceChanged(object? sender, EventArgs e)
    {
        OperatorComputer.UpdateSnapshot(ProjectOperatorComputerSnapshot());
    }

    private void OnTrainingGuidanceModeChanged(object? sender, EventArgs e)
    {
        OperatorComputer.UpdateSnapshot(ProjectOperatorComputerSnapshot());
        OnPropertyChanged(nameof(TrainingGuidanceModeText));
        OnPropertyChanged(nameof(GuidanceModeShortText));
        OnPropertyChanged(nameof(SelectedWorkspaceContextText));
        OnPropertyChanged(nameof(OperatorNextActionText));
        OnPropertyChanged(nameof(StartupToPowerCommandPathText));
    }

    private void OnPlantControlAuthorityChanged(object? sender, PlantControlAuthorityChangedEventArgs e)
    {
        OperatorComputer.UpdateSnapshot(ProjectOperatorComputerSnapshot());
        OnPropertyChanged(nameof(ControlAuthorityText));
    }

    private void OnTrainingAssessmentChanged(object? sender, ScenarioTrainingAssessmentChangedEventArgs e)
    {
        OperatorComputer.UpdateSnapshot(ProjectOperatorComputerSnapshot());
        OnPropertyChanged(nameof(TrainingAssessmentText));
        OnPropertyChanged(nameof(TrainingScoreText));
        OnPropertyChanged(nameof(TrainingGuidanceModeText));
        OnPropertyChanged(nameof(GuidanceModeShortText));
        OnPropertyChanged(nameof(OperatorNextActionText));
    }

    private void OnSnapshotChanged(object? sender, ControlRoomSnapshotChangedEventArgs e)
    {
        UpdateInstrumentTrends(_snapshot, e.Snapshot);
        _snapshot = e.Snapshot;
        _plantMimic = ControlRoomPlantMimicProjector.Project(_snapshot);
        _subsystemSchematics = ControlRoomSubsystemSchematicProjector.Project(_snapshot);
        if (_plantMimic.Elements.All(element => !string.Equals(element.ElementId, _selectedMimicElementId, StringComparison.Ordinal)))
        {
            _selectedMimicElementId = _plantMimic.Elements.FirstOrDefault()?.ElementId;
        }
        _operationalHistory.Observe(_snapshot);
        OperatorComputer.UpdateSnapshot(ProjectOperatorComputerSnapshot());
        if (_snapshot.ReactorCore.RodTargets.Count == 0)
        {
            _selectedRodIndex = 0;
        }
        else if (_selectedRodIndex >= _snapshot.ReactorCore.RodTargets.Count)
        {
            _selectedRodIndex = _snapshot.ReactorCore.RodTargets.Count - 1;
        }

        if (CommandablePumps.Count == 0)
        {
            _selectedPumpIndex = 0;
        }
        else if (_selectedPumpIndex >= CommandablePumps.Count)
        {
            _selectedPumpIndex = CommandablePumps.Count - 1;
        }

        if (_snapshot.Electrical.Generators.Count == 0)
        {
            _selectedGeneratorIndex = 0;
        }
        else if (_selectedGeneratorIndex >= _snapshot.Electrical.Generators.Count)
        {
            _selectedGeneratorIndex = _snapshot.Electrical.Generators.Count - 1;
        }

        if (_snapshot.AlarmEvents.Alarms.Count == 0)
        {
            _selectedAlarmIndex = 0;
        }
        else if (_selectedAlarmIndex >= _snapshot.AlarmEvents.Alarms.Count)
        {
            _selectedAlarmIndex = _snapshot.AlarmEvents.Alarms.Count - 1;
        }

        OnPropertyChanged(nameof(LogicalStep));
        OnPropertyChanged(nameof(RuntimeState));
        OnPropertyChanged(nameof(IsRuntimeRunning));
        OnPropertyChanged(nameof(RuntimeProgressText));
        OnPropertyChanged(nameof(ElectricalOutputText));
        OnPropertyChanged(nameof(ProtectionStateText));
        OnPropertyChanged(nameof(FirstOutStripText));
        OnPropertyChanged(nameof(LatestEventText));
        OnPropertyChanged(nameof(SelectedWorkspaceContextText));
        OnPropertyChanged(nameof(SignalHealthText));
        OnPropertyChanged(nameof(AnnunciatedAlarmCount));
        OnPropertyChanged(nameof(UnacknowledgedAlarmCount));
        OnPropertyChanged(nameof(AnnunciatedAlarmCountText));
        OnPropertyChanged(nameof(UnacknowledgedAlarmCountText));
        OnPropertyChanged(nameof(LogicalStepText));
        OnPropertyChanged(nameof(UnacknowledgedAlarmVisualState));
        OnPropertyChanged(nameof(ProtectionSummary));
        OnPropertyChanged(nameof(FaultLifecycleText));
        OnPropertyChanged(nameof(PreStartupChecklistText));
        OnPropertyChanged(nameof(ScenarioChecklistText));
        OnPropertyChanged(nameof(PrimaryContextText));
        OnPropertyChanged(nameof(TurbineContextText));
        OnPropertyChanged(nameof(ElectricalContextText));
        OnPropertyChanged(nameof(ReactorCore));
        OnPropertyChanged(nameof(PrimaryCircuit));
        OnPropertyChanged(nameof(TurbineSecondary));
        OnPropertyChanged(nameof(Electrical));
        OnPropertyChanged(nameof(PlantMimic));
        OnPropertyChanged(nameof(SubsystemSchematics));
        OnPropertyChanged(nameof(ReactorCoreSchematic));
        OnPropertyChanged(nameof(PrimarySteamDrumSchematic));
        OnPropertyChanged(nameof(TurbineSecondarySchematic));
        OnPropertyChanged(nameof(GeneratorGridSchematic));
        OnPropertyChanged(nameof(InstrumentationProtectionSchematic));
        OnPropertyChanged(nameof(GeneratorPowerPathDiagnosticText));
        OnPropertyChanged(nameof(SelectedMimicElementId));
        OnPropertyChanged(nameof(SelectedMimicElement));
        OnPropertyChanged(nameof(SelectedMimicTitle));
        OnPropertyChanged(nameof(SelectedMimicStatusText));
        OnPropertyChanged(nameof(SelectedMimicValuesText));
        OnPropertyChanged(nameof(SelectedMimicInputText));
        OnPropertyChanged(nameof(SelectedMimicOutputText));
        OnPropertyChanged(nameof(SelectedMimicDetailText));
        OnPropertyChanged(nameof(ReactorThermalPowerTrend));
        OnPropertyChanged(nameof(GrossElectricalOutputTrend));
        OnPropertyChanged(nameof(AlarmEvents));
        OnPropertyChanged(nameof(OperationalHistory));
        OnPropertyChanged(nameof(AlarmOptionsText));
        OnPropertyChanged(nameof(SelectedAlarmIndex));
        OnPropertyChanged(nameof(SelectedAlarmId));
        OnPropertyChanged(nameof(SelectedAlarmStatusText));
        OnPropertyChanged(nameof(AlarmAcknowledgeCommandState));
        OnPropertyChanged(nameof(AlarmResetCommandState));
        OnPropertyChanged(nameof(AlarmSelectionState));
        OnPropertyChanged(nameof(AlarmAcknowledgeAllCommandState));
        OnPropertyChanged(nameof(AlarmResetAllCommandState));
        OnPropertyChanged(nameof(AlarmContextText));
        OnPropertyChanged(nameof(PumpOptionsText));
        OnPropertyChanged(nameof(SelectedPumpIndex));
        OnPropertyChanged(nameof(SelectedPumpId));
        OnPropertyChanged(nameof(SelectedPumpText));
        OnPropertyChanged(nameof(PumpCommandState));
        OnPropertyChanged(nameof(PumpStartCommandActive));
        OnPropertyChanged(nameof(PumpStopCommandActive));
        OnPropertyChanged(nameof(PumpStartCommandEnabled));
        OnPropertyChanged(nameof(PumpStopCommandEnabled));
        OnPropertyChanged(nameof(GeneratorOptionsText));
        OnPropertyChanged(nameof(SelectedGeneratorIndex));
        OnPropertyChanged(nameof(SelectedGeneratorId));
        OnPropertyChanged(nameof(SelectedBreakerId));
        OnPropertyChanged(nameof(SelectedGeneratorSynchronizationText));
        OnPropertyChanged(nameof(SelectedGeneratorSynchronizationDetailText));
        OnPropertyChanged(nameof(SelectedGeneratorRotorId));
        OnPropertyChanged(nameof(TurbineSpeedCommandState));
        OnPropertyChanged(nameof(GeneratorLoadCommandState));
        OnPropertyChanged(nameof(TurbineTripCommandState));
        OnPropertyChanged(nameof(TurbineTripCommandEnabled));
        OnPropertyChanged(nameof(TurbineTripCommandLabel));
        OnPropertyChanged(nameof(GeneratorSelectionState));
        OnPropertyChanged(nameof(GeneratorTripCommandState));
        OnPropertyChanged(nameof(GeneratorTripCommandEnabled));
        OnPropertyChanged(nameof(GeneratorTripCommandLabel));
        OnPropertyChanged(nameof(BreakerCloseCommandState));
        OnPropertyChanged(nameof(BreakerOpenCommandState));
        OnPropertyChanged(nameof(BreakerCloseCommandActive));
        OnPropertyChanged(nameof(BreakerCloseCommandEnabled));
        OnPropertyChanged(nameof(BreakerOpenCommandActive));
        OnPropertyChanged(nameof(BreakerOpenCommandEnabled));
        OnPropertyChanged(nameof(RodOptionsText));
        OnPropertyChanged(nameof(SelectedRodIndex));
        OnPropertyChanged(nameof(SelectedRodId));
        OnPropertyChanged(nameof(SelectedRodTargetKind));
        OnPropertyChanged(nameof(SelectedRodMotionText));
        OnPropertyChanged(nameof(RodInsertCommandActive));
        OnPropertyChanged(nameof(RodHoldCommandActive));
        OnPropertyChanged(nameof(RodWithdrawCommandActive));
        OnPropertyChanged(nameof(RodInsertCommandEnabled));
        OnPropertyChanged(nameof(RodHoldCommandEnabled));
        OnPropertyChanged(nameof(RodWithdrawCommandEnabled));
        OnPropertyChanged(nameof(ReactorCommandState));
        OnPropertyChanged(nameof(ScramCommandState));
        OnPropertyChanged(nameof(ScramCommandEnabled));
        OnPropertyChanged(nameof(ScramCommandLabel));
        OnPropertyChanged(nameof(ProtectionResetCommandState));
        OnPropertyChanged(nameof(ProtectionResetCommandEnabled));
        OnPropertyChanged(nameof(ProtectionResetStatusText));
        OnPropertyChanged(nameof(OperatorCurrentConditionText));
        OnPropertyChanged(nameof(OperatorNextActionText));
        OnPropertyChanged(nameof(RodCommandState));
        OnPropertyChanged(nameof(RodWithdrawCommandState));
        OnPropertyChanged(nameof(XenonAvailabilityText));
    }

    private void UpdateInstrumentTrends(ControlRoomSnapshot previous, ControlRoomSnapshot current)
    {
        _reactorThermalPowerTrend = BuildInstrumentTrend(
            previous.LogicalStep,
            previous.ReactorCore.ReactorThermalPower,
            current.LogicalStep,
            current.ReactorCore.ReactorThermalPower);
        _grossElectricalOutputTrend = BuildInstrumentTrend(
            previous.LogicalStep,
            previous.Electrical.GrossElectricalOutput,
            current.LogicalStep,
            current.Electrical.GrossElectricalOutput);
    }

    private static ControlRoomInstrumentTrendSnapshot BuildInstrumentTrend(
        long previousLogicalStep,
        ControlRoomValueSnapshot previous,
        long currentLogicalStep,
        ControlRoomValueSnapshot current)
    {
        var scale = current.InstrumentScale;
        var steadyTolerance = scale is null ? 1e-9d : (scale.Maximum - scale.Minimum) * 1e-6d;
        return ControlRoomInstrumentTrendSnapshot.Between(
            previousLogicalStep,
            previous.NumericValue,
            currentLogicalStep,
            current.NumericValue,
            steadyTolerance,
            current.Unit);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
