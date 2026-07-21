using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NuclearReactorSimulator.App.Commands;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IControlRoomCommandDispatcher _commandDispatcher;
    private readonly ControlRoomOperationalHistoryAccumulator _operationalHistory;
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
    private int _selectedRodIndex;
    private int _selectedPumpIndex;
    private int _selectedGeneratorIndex;
    private int _selectedAlarmIndex;
    private string _commandStatus;

    public MainWindowViewModel(
        ApplicationDescriptor descriptor,
        IControlRoomSnapshotSource snapshotSource,
        IControlRoomCommandDispatcher commandDispatcher,
        PreStartupGuidancePlan? preStartupGuidance = null,
        FirstCriticalityGuidancePlan? firstCriticalityGuidance = null,
        HeatUpTurbineStartupGuidancePlan? heatUpTurbineStartupGuidance = null,
        GridSynchronizationGuidancePlan? gridSynchronizationGuidance = null,
        PowerManoeuvringGuidancePlan? powerManoeuvringGuidance = null,
        ScenarioTrainingTracker? trainingTracker = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(snapshotSource);
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _preStartupGuidance = preStartupGuidance;
        _firstCriticalityGuidance = firstCriticalityGuidance;
        _heatUpTurbineStartupGuidance = heatUpTurbineStartupGuidance;
        _gridSynchronizationGuidance = gridSynchronizationGuidance;
        _powerManoeuvringGuidance = powerManoeuvringGuidance;
        _trainingTracker = trainingTracker;
        _commandStatus = trainingTracker is not null
            ? "M8.2 hydraulic fault effects are available over the validated M8.1 scheduler. The default desktop session remains the validated M7.7 normal-operations training scenario and declares no faults; load the M8.2 hydraulic demonstration scenario to exercise the fault pack."
            : powerManoeuvringGuidance is not null
            ? "M7.6 power-manoeuvring/normal-shutdown scenario loaded in PAUSED state. Manoeuvre load through canonical requests, then unload, disconnect, insert rods and preserve post-shutdown circulation."
            : gridSynchronizationGuidance is not null
            ? "M7.5 grid-synchronization/initial-loading scenario loaded in PAUSED state. Close only on the canonical synchronization permissive, then take load in deliberate increments."
            : heatUpTurbineStartupGuidance is not null
            ? "M7.4 heat-up/steam-raising/turbine-startup scenario loaded in PAUSED state. Preserve generator isolation while using the validated speed-control seam for turbine roll-off."
            : firstCriticalityGuidance is not null
                ? "M7.3 first-criticality/low-power scenario loaded in PAUSED state. Use controlled rod motion, observe reactivity/period and preserve steam/grid isolation."
                : "M7.2 cold-shutdown/pre-start scenario loaded in PAUSED state. Follow the preparation guidance; scenario permissions fail closed before runtime commands.";

        Title = descriptor.ProductName;
        Milestone = descriptor.Milestone;
        Status = descriptor.Status;
        Workspaces = ControlRoomWorkspaceCatalog.Default;
        _selectedWorkspace = Workspaces[0];
        _snapshot = snapshotSource.Current;
        PerformanceBudget = ControlRoomPerformanceBudget.DesktopDefault;
        _operationalHistory = new ControlRoomOperationalHistoryAccumulator(
            maximumVisibleTrendSeries: PerformanceBudget.MaximumVisibleTrendSeries);
        _operationalHistory.Observe(_snapshot);

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

        snapshotSource.SnapshotChanged += OnSnapshotChanged;
        if (_trainingTracker is not null)
        {
            _trainingTracker.AssessmentChanged += OnTrainingAssessmentChanged;
        }
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
            OnPropertyChanged(nameof(IsReactorWorkspaceSelected));
            OnPropertyChanged(nameof(IsPrimaryWorkspaceSelected));
            OnPropertyChanged(nameof(IsTurbineWorkspaceSelected));
            OnPropertyChanged(nameof(IsElectricalWorkspaceSelected));
            OnPropertyChanged(nameof(IsAlarmsWorkspaceSelected));
            OnPropertyChanged(nameof(IsShellHostWorkspaceSelected));
            OnPropertyChanged(nameof(IsOverviewWorkspaceSelected));
        }
    }

    public string SelectedWorkspaceTitle => SelectedWorkspace.Title;
    public string SelectedWorkspaceDescription => SelectedWorkspace.Description;
    public bool IsReactorWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.Reactor;
    public bool IsPrimaryWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.PrimaryCircuit;
    public bool IsTurbineWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.TurbineSecondary;
    public bool IsElectricalWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.Electrical;
    public bool IsAlarmsWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.AlarmsEvents;
    public bool IsOverviewWorkspaceSelected => SelectedWorkspace.Id == ControlRoomWorkspaceId.Overview;
    public bool IsShellHostWorkspaceSelected => IsOverviewWorkspaceSelected;

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
        ? "No M7.7/M8.1 training evaluation loaded"
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
        ? "No M8 fault declarations in the loaded training scenario"
        : $"{_snapshot.Faults.ActiveCount} active · {_snapshot.Faults.PendingCount} pending · {_snapshot.Faults.ClearedCount} cleared";

    public string PrimaryContextText => PrimaryCircuit.Loops.Count == 0
        ? "No primary-circuit presentation snapshot published yet"
        : $"{PrimaryCircuit.Loops.Count} loops · {PrimaryCircuit.Pumps.Count} MCPs · {PrimaryCircuit.SteamDrums.Count} steam drums · {PrimaryCircuit.Valves.Count} primary-connected valves";

    public ReactorCorePanelSnapshot ReactorCore => _snapshot.ReactorCore;

    public PrimaryCircuitPanelSnapshot PrimaryCircuit => _snapshot.PrimaryCircuit;

    public TurbineSecondaryPanelSnapshot TurbineSecondary => _snapshot.TurbineSecondary;

    public ElectricalPanelSnapshot Electrical => _snapshot.Electrical;

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
        }
    }

    public string SelectedPumpId => CommandablePumps.Count == 0
        ? "—"
        : CommandablePumps[Math.Clamp(_selectedPumpIndex, 0, CommandablePumps.Count - 1)].PumpId;

    public string SelectedPumpText => $"Selected canonical pump: {SelectedPumpId}";

    public ControlRoomVisualState PumpCommandState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly || CommandablePumps.Count == 0
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

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
            OnPropertyChanged(nameof(SelectedGeneratorRotorId));
            OnPropertyChanged(nameof(BreakerCloseCommandState));
            OnPropertyChanged(nameof(TurbineSpeedCommandState));
            OnPropertyChanged(nameof(GeneratorLoadCommandState));
            OnPropertyChanged(nameof(BreakerOpenCommandState));
        }
    }

    public string SelectedGeneratorId => Electrical.Generators.Count == 0
        ? "—"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].GeneratorId;

    public string SelectedBreakerId => Electrical.Generators.Count == 0
        ? "—"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].BreakerId;

    public string SelectedGeneratorSynchronizationText => Electrical.Generators.Count == 0
        ? "No generator selected"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].SynchronizationText;

    public string SelectedGeneratorRotorId => Electrical.Generators.Count == 0
        ? "—"
        : Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)].RotorId;

    public ControlRoomVisualState TurbineTripCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : TurbineSecondary.TurbineTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;

    public ControlRoomVisualState GeneratorTripCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : Electrical.GeneratorTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;

    public ControlRoomVisualState TurbineSpeedCommandState =>
        _snapshot.RunState == ControlRoomRunState.ShellOnly || Electrical.Generators.Count == 0 || Electrical.GeneratorTripActive
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
            if (_snapshot.RunState == ControlRoomRunState.ShellOnly || Electrical.Generators.Count == 0 || Electrical.GeneratorTripActive)
            {
                return ControlRoomVisualState.Unavailable;
            }

            var generator = Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)];
            return generator.BreakerClosed || !generator.SynchronizationConditionsSatisfied
                ? ControlRoomVisualState.Unavailable
                : ControlRoomVisualState.Normal;
        }
    }

    public ControlRoomVisualState BreakerOpenCommandState
    {
        get
        {
            if (_snapshot.RunState == ControlRoomRunState.ShellOnly || Electrical.Generators.Count == 0)
            {
                return ControlRoomVisualState.Unavailable;
            }

            var generator = Electrical.Generators[Math.Clamp(_selectedGeneratorIndex, 0, Electrical.Generators.Count - 1)];
            return generator.BreakerClosed ? ControlRoomVisualState.Normal : ControlRoomVisualState.Unavailable;
        }
    }

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
        }
    }

    public string SelectedRodId => ReactorCore.RodTargets.Count == 0
        ? "—"
        : ReactorCore.RodTargets[Math.Clamp(_selectedRodIndex, 0, ReactorCore.RodTargets.Count - 1)].TargetId;

    public ControlRoomCommandTargetKind? SelectedRodTargetKind => ReactorCore.RodTargets.Count == 0
        ? null
        : ReactorCore.RodTargets[Math.Clamp(_selectedRodIndex, 0, ReactorCore.RodTargets.Count - 1)].TargetKind;

    public ControlRoomVisualState ReactorCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : ControlRoomVisualState.Normal;

    public ControlRoomVisualState ScramCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly
        ? ControlRoomVisualState.Unavailable
        : ReactorCore.ReactorScramActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;

    public ControlRoomVisualState RodCommandState => _snapshot.RunState == ControlRoomRunState.ShellOnly || ReactorCore.RodTargets.Count == 0
        ? ControlRoomVisualState.Unavailable
        : ControlRoomVisualState.Normal;

    public ControlRoomVisualState RodWithdrawCommandState =>
        RodCommandState == ControlRoomVisualState.Unavailable || ReactorCore.RodWithdrawalInhibited
            ? ControlRoomVisualState.Unavailable
            : ControlRoomVisualState.Normal;

    public string XenonAvailabilityText => ReactorCore.XenonReactivity.State == ControlRoomVisualState.Unavailable
        ? "M2.8 xenon physics is validated, but xenon state is not yet promoted into the M5.7 operational snapshot. M6.3 does not fabricate a value."
        : "Xenon reactivity available from the operational presentation snapshot.";

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
        }
        catch (InvalidOperationException exception)
        {
            CommandStatus = $"Command blocked by the loaded scenario: {exception.Message}";
        }
    }

    private void OnTrainingAssessmentChanged(object? sender, ScenarioTrainingAssessmentChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TrainingAssessmentText));
        OnPropertyChanged(nameof(TrainingGuidanceModeText));
    }

    private void OnSnapshotChanged(object? sender, ControlRoomSnapshotChangedEventArgs e)
    {
        _snapshot = e.Snapshot;
        _operationalHistory.Observe(_snapshot);
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
        OnPropertyChanged(nameof(GeneratorOptionsText));
        OnPropertyChanged(nameof(SelectedGeneratorIndex));
        OnPropertyChanged(nameof(SelectedGeneratorId));
        OnPropertyChanged(nameof(SelectedBreakerId));
        OnPropertyChanged(nameof(SelectedGeneratorSynchronizationText));
        OnPropertyChanged(nameof(SelectedGeneratorRotorId));
        OnPropertyChanged(nameof(TurbineSpeedCommandState));
        OnPropertyChanged(nameof(GeneratorLoadCommandState));
        OnPropertyChanged(nameof(TurbineTripCommandState));
        OnPropertyChanged(nameof(GeneratorTripCommandState));
        OnPropertyChanged(nameof(BreakerCloseCommandState));
        OnPropertyChanged(nameof(BreakerOpenCommandState));
        OnPropertyChanged(nameof(RodOptionsText));
        OnPropertyChanged(nameof(SelectedRodIndex));
        OnPropertyChanged(nameof(SelectedRodId));
        OnPropertyChanged(nameof(SelectedRodTargetKind));
        OnPropertyChanged(nameof(ReactorCommandState));
        OnPropertyChanged(nameof(ScramCommandState));
        OnPropertyChanged(nameof(RodCommandState));
        OnPropertyChanged(nameof(RodWithdrawCommandState));
        OnPropertyChanged(nameof(XenonAvailabilityText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
