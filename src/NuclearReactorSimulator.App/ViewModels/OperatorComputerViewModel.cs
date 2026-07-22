using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NuclearReactorSimulator.App.Commands;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.App.ViewModels;

public sealed class OperatorComputerViewModel : INotifyPropertyChanged
{
    private readonly IControlRoomCommandDispatcher? _commandDispatcher;
    private readonly ITrainingAssistanceDispatcher? _trainingAssistanceDispatcher;
    private readonly IPlantControlAuthorityDispatcher? _plantControlAuthorityDispatcher;
    private readonly OperatorComputerSessionWorkspaceController? _sessionWorkspace;
    private OperatorComputerSnapshot _snapshot;
    private OperatorComputerPageSnapshot _selectedPage;
    private OperatorComputerCommandSnapshot? _selectedCommand;
    private string _commandConsoleStatus = "Select a contextual command. Availability is advisory; runtime/scenario validation remains authoritative.";
    private string _modesStatus = "Training assistance and physical plant-control authority are independent axes.";
    private string _sessionStatus = "M10.7 session lifecycle is replay-backed. Pause before checkpoint/save/replay operations.";
    private OperatorComputerSessionCheckpointSnapshot? _selectedSessionCheckpoint;

    public OperatorComputerViewModel(OperatorComputerSnapshot snapshot)
        : this(snapshot, null, null, null, null)
    {
    }

    public OperatorComputerViewModel(OperatorComputerSnapshot snapshot, IControlRoomCommandDispatcher? commandDispatcher)
        : this(snapshot, commandDispatcher, null, null, null)
    {
    }

    public OperatorComputerViewModel(
        OperatorComputerSnapshot snapshot,
        IControlRoomCommandDispatcher? commandDispatcher,
        ITrainingAssistanceDispatcher? trainingAssistanceDispatcher,
        IPlantControlAuthorityDispatcher? plantControlAuthorityDispatcher,
        OperatorComputerSessionWorkspaceController? sessionWorkspace = null)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _commandDispatcher = commandDispatcher;
        _trainingAssistanceDispatcher = trainingAssistanceDispatcher;
        _plantControlAuthorityDispatcher = plantControlAuthorityDispatcher;
        _sessionWorkspace = sessionWorkspace;
        _selectedPage = snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Guidance);
        _selectedCommand = snapshot.Commands?.Commands.FirstOrDefault(static command => command.CanDispatch)
            ?? snapshot.Commands?.Commands.FirstOrDefault();

        SelectGuidancePageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Guidance));
        SelectInfoPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Info));
        SelectAlarmsPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Alarms));
        SelectCommandsPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Commands));
        SelectModesPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Modes));
        SelectDiagnosticsPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Diagnostics));
        SelectLogPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Log));
        SelectSessionPageCommand = new DelegateCommand(() => SelectPage(OperatorComputerPageId.Session));
        ExecuteSelectedCommandCommand = new DelegateCommand(ExecuteSelectedCommand);
        SetTrainingNoneCommand = new DelegateCommand(() => SetTrainingAssistance(TrainingGuidanceMode.Hidden));
        SetTrainingChecklistCommand = new DelegateCommand(() => SetTrainingAssistance(TrainingGuidanceMode.ChecklistOnly));
        SetTrainingGuidedCommand = new DelegateCommand(() => SetTrainingAssistance(TrainingGuidanceMode.Guided));
        SetPlantManualCommand = new DelegateCommand(() => SetPlantAuthority(PlantControlAuthorityMode.Manual));
        SetPlantAssistedCommand = new DelegateCommand(() => SetPlantAuthority(PlantControlAuthorityMode.Assisted));
        SetPlantSupervisoryCommand = new DelegateCommand(() => SetPlantAuthority(PlantControlAuthorityMode.SupervisoryAutomatic));
        HoldCurrentOperatingPointCommand = new DelegateCommand(HoldCurrentOperatingPoint);
        CreateSessionCheckpointCommand = new DelegateCommand(CreateSessionCheckpoint);
        VerifySessionReplayCommand = new DelegateCommand(VerifySessionReplay);
        _selectedSessionCheckpoint = snapshot.Session?.Checkpoints.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<OperatorComputerPageSnapshot> Pages => _snapshot.Pages;

    public OperatorComputerPageSnapshot SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (value is null || _selectedPage == value)
            {
                return;
            }

            _selectedPage = value;
            RaisePagePropertiesChanged();
        }
    }

    public string SelectedPageLabel => SelectedPage.MenuLabel;

    public string SelectedPageTitle => SelectedPage.Title;

    public string SelectedPageDescription => SelectedPage.Description;

    public bool IsCommandsPageSelected => SelectedPage.Id == OperatorComputerPageId.Commands;

    public bool IsModesPageSelected => SelectedPage.Id == OperatorComputerPageId.Modes;

    public bool IsSessionPageSelected => SelectedPage.Id == OperatorComputerPageId.Session;

    public bool IsStandardContentPageSelected => !IsCommandsPageSelected && !IsModesPageSelected && !IsSessionPageSelected;

    public IReadOnlyList<OperatorComputerCommandSnapshot> CommandEntries =>
        _snapshot.Commands?.Commands ?? Array.Empty<OperatorComputerCommandSnapshot>();

    public OperatorComputerCommandSnapshot? SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            if (_selectedCommand == value)
            {
                return;
            }

            _selectedCommand = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCommandDetailText));
        }
    }

    public string SelectedCommandDetailText => SelectedCommand is null
        ? "No contextual command is available for selection."
        : $"{SelectedCommand.GroupText} // {SelectedCommand.DisplayName}{Environment.NewLine}" +
          $"TARGET       {SelectedCommand.TargetText}{Environment.NewLine}" +
          $"STATE        {SelectedCommand.CurrentState}{Environment.NewLine}" +
          $"AVAILABILITY {SelectedCommand.AvailabilityText}" +
          (SelectedCommand.BlockReason is null ? string.Empty : $"{Environment.NewLine}BLOCKED BY   {SelectedCommand.BlockReason}");

    public string CommandConsoleStatus
    {
        get => _commandConsoleStatus;
        private set
        {
            if (string.Equals(_commandConsoleStatus, value, StringComparison.Ordinal))
            {
                return;
            }

            _commandConsoleStatus = value;
            OnPropertyChanged();
        }
    }

    public string ModesStatus
    {
        get => _modesStatus;
        private set
        {
            if (string.Equals(_modesStatus, value, StringComparison.Ordinal))
            {
                return;
            }

            _modesStatus = value;
            OnPropertyChanged();
        }
    }

    public string SelectedPageStateText => SelectedPage.ContentState switch
    {
        OperatorComputerPageContentState.ShellOnly => "TERMINAL PAGE RESERVED — canonical content is staged for a later M10 milestone.",
        OperatorComputerPageContentState.Available => "CANONICAL CONTENT AVAILABLE",
        OperatorComputerPageContentState.Unavailable => "CONTENT UNAVAILABLE FOR THE LOADED SCENARIO / PRESENTATION CONTRACT",
        _ => "UNKNOWN PAGE STATE",
    };

    public string SelectedPageContentText => SelectedPage.Id switch
    {
        OperatorComputerPageId.Guidance => BuildGuidanceText(),
        OperatorComputerPageId.Info => BuildInformationText(),
        OperatorComputerPageId.Alarms => BuildAlarmsText(),
        OperatorComputerPageId.Commands => BuildCommandsText(),
        OperatorComputerPageId.Modes => BuildModesText(),
        OperatorComputerPageId.Diagnostics => BuildDiagnosticsText(),
        OperatorComputerPageId.Log => BuildLogText(),
        OperatorComputerPageId.Session => BuildSessionText(),
        _ => SelectedPageStateText,
    };

    public string RuntimeStateText => _snapshot.RuntimeStatus.RunState switch
    {
        ControlRoomRunState.Running => "RUNNING",
        ControlRoomRunState.Paused => "PAUSED",
        _ => "SHELL ONLY",
    };

    public string LogicalStepText => _snapshot.RuntimeStatus.LogicalStep.ToString("D8", CultureInfo.InvariantCulture);

    public string AlarmStatusText => $"ALARMS {_snapshot.RuntimeStatus.AnnunciatedAlarmCount.ToString(CultureInfo.InvariantCulture)}/{_snapshot.RuntimeStatus.UnacknowledgedAlarmCount.ToString(CultureInfo.InvariantCulture)} UNACK";

    public string SignalStatusText => _snapshot.RuntimeStatus.InvalidMeasuredSignalCount == 0
        ? "SIGNALS VALID"
        : $"SIGNALS {_snapshot.RuntimeStatus.InvalidMeasuredSignalCount.ToString(CultureInfo.InvariantCulture)} INVALID";

    public string ProtectionStatusText => _snapshot.RuntimeStatus.AnyTripActive ? "PROTECTION ACTIVE" : "PROTECTION CLEAR";

    public string StatusLineText =>
        $"PAGE {SelectedPageLabel}  |  {RuntimeStateText}  |  STEP {LogicalStepText}  |  {AlarmStatusText}  |  {SignalStatusText}  |  {ProtectionStatusText}";

    public ICommand SelectGuidancePageCommand { get; }
    public ICommand SelectInfoPageCommand { get; }
    public ICommand SelectAlarmsPageCommand { get; }
    public ICommand SelectCommandsPageCommand { get; }
    public ICommand SelectModesPageCommand { get; }
    public ICommand SelectDiagnosticsPageCommand { get; }
    public ICommand SelectLogPageCommand { get; }
    public ICommand SelectSessionPageCommand { get; }
    public ICommand ExecuteSelectedCommandCommand { get; }
    public ICommand SetTrainingNoneCommand { get; }
    public ICommand SetTrainingChecklistCommand { get; }
    public ICommand SetTrainingGuidedCommand { get; }
    public ICommand SetPlantManualCommand { get; }
    public ICommand SetPlantAssistedCommand { get; }
    public ICommand SetPlantSupervisoryCommand { get; }
    public ICommand HoldCurrentOperatingPointCommand { get; }
    public ICommand CreateSessionCheckpointCommand { get; }
    public ICommand VerifySessionReplayCommand { get; }

    public IReadOnlyList<OperatorComputerSessionCheckpointSnapshot> SessionCheckpoints =>
        _snapshot.Session?.Checkpoints ?? Array.Empty<OperatorComputerSessionCheckpointSnapshot>();

    public OperatorComputerSessionCheckpointSnapshot? SelectedSessionCheckpoint
    {
        get => _selectedSessionCheckpoint;
        set
        {
            if (_selectedSessionCheckpoint == value)
            {
                return;
            }
            _selectedSessionCheckpoint = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSessionCheckpointDetailText));
        }
    }

    public string SelectedSessionCheckpointDetailText => SelectedSessionCheckpoint is null
        ? "No replay-backed checkpoint selected."
        : $"{SelectedSessionCheckpoint.CheckpointId}{Environment.NewLine}STEP {SelectedSessionCheckpoint.LogicalStep:D8}{Environment.NewLine}FINGERPRINT {SelectedSessionCheckpoint.SnapshotFingerprint}";

    public string SessionStatus
    {
        get => _sessionStatus;
        private set
        {
            if (string.Equals(_sessionStatus, value, StringComparison.Ordinal))
            {
                return;
            }
            _sessionStatus = value;
            OnPropertyChanged();
        }
    }

    public string ExportSessionArchive()
    {
        if (_sessionWorkspace is null)
        {
            throw new InvalidOperationException("No M10.7 session workspace is attached.");
        }
        var content = _sessionWorkspace.ExportArchive();
        SessionStatus = "ARCHIVE READY — compact replay evidence serialized. Restoration remains M9.1 replay/fingerprint verified.";
        return content;
    }

    public string? SelectedSessionCheckpointId => SelectedSessionCheckpoint?.CheckpointId;

    public void ReportSessionWorkspaceStatus(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        SessionStatus = message;
    }

    public void SelectPage(OperatorComputerPageId pageId)
    {
        SelectedPage = _snapshot.Pages.Single(page => page.Id == pageId);
    }

    public void UpdateSnapshot(OperatorComputerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var selectedPageId = SelectedPage.Id;
        _snapshot = snapshot;
        _selectedPage = snapshot.Pages.Single(page => page.Id == selectedPageId);

        var selectedCommandId = _selectedCommand?.EntryId;
        _selectedCommand = selectedCommandId is null
            ? snapshot.Commands?.Commands.FirstOrDefault(static command => command.CanDispatch) ?? snapshot.Commands?.Commands.FirstOrDefault()
            : snapshot.Commands?.Commands.FirstOrDefault(command => string.Equals(command.EntryId, selectedCommandId, StringComparison.Ordinal))
                ?? snapshot.Commands?.Commands.FirstOrDefault(static command => command.CanDispatch)
                ?? snapshot.Commands?.Commands.FirstOrDefault();

        var selectedCheckpointId = _selectedSessionCheckpoint?.CheckpointId;
        _selectedSessionCheckpoint = selectedCheckpointId is null
            ? snapshot.Session?.Checkpoints.FirstOrDefault()
            : snapshot.Session?.Checkpoints.FirstOrDefault(checkpoint => string.Equals(checkpoint.CheckpointId, selectedCheckpointId, StringComparison.Ordinal))
                ?? snapshot.Session?.Checkpoints.FirstOrDefault();

        OnPropertyChanged(nameof(Pages));
        OnPropertyChanged(nameof(CommandEntries));
        OnPropertyChanged(nameof(SelectedCommand));
        OnPropertyChanged(nameof(SelectedCommandDetailText));
        OnPropertyChanged(nameof(SessionCheckpoints));
        OnPropertyChanged(nameof(SelectedSessionCheckpoint));
        OnPropertyChanged(nameof(SelectedSessionCheckpointDetailText));
        RaisePagePropertiesChanged();
        RaiseRuntimePropertiesChanged();
    }

    private void RaisePagePropertiesChanged()
    {
        OnPropertyChanged(nameof(SelectedPage));
        OnPropertyChanged(nameof(SelectedPageLabel));
        OnPropertyChanged(nameof(SelectedPageTitle));
        OnPropertyChanged(nameof(SelectedPageDescription));
        OnPropertyChanged(nameof(SelectedPageStateText));
        OnPropertyChanged(nameof(IsCommandsPageSelected));
        OnPropertyChanged(nameof(IsModesPageSelected));
        OnPropertyChanged(nameof(IsSessionPageSelected));
        OnPropertyChanged(nameof(IsStandardContentPageSelected));
        OnPropertyChanged(nameof(SelectedPageContentText));
        OnPropertyChanged(nameof(StatusLineText));
    }

    private void RaiseRuntimePropertiesChanged()
    {
        OnPropertyChanged(nameof(RuntimeStateText));
        OnPropertyChanged(nameof(LogicalStepText));
        OnPropertyChanged(nameof(AlarmStatusText));
        OnPropertyChanged(nameof(SignalStatusText));
        OnPropertyChanged(nameof(ProtectionStatusText));
        OnPropertyChanged(nameof(SelectedPageContentText));
        OnPropertyChanged(nameof(StatusLineText));
    }

    private string BuildSessionText()
    {
        var session = _snapshot.Session;
        if (session is null)
        {
            return "No M10.7 session workspace is attached.";
        }

        var lines = new List<string>
        {
            $"SCENARIO      {session.ScenarioTitle}",
            $"SCENARIO ID   {session.ScenarioId}",
            $"INITIAL       {session.InitialConditionText}",
            $"LOGICAL STEP  {session.LogicalStep:D8}",
            $"RECORDER      {session.RecorderStateText}",
            $"FRAMES        {session.RecordedFrameCount}",
            $"CHECKPOINTS   {session.Checkpoints.Count}",
            string.Empty,
        };

        if (!session.RecorderActive)
        {
            lines.Add("Recorder is intentionally inactive in the normal desktop path to avoid hidden every-fixed-step fingerprint overhead.");
            lines.Add("Use START RECORDED SESSION to restart from the exact versioned initial condition with M9.1 recording enabled.");
        }
        else
        {
            lines.Add("M9.1 RECORDER ACTIVE — checkpoints and save archives are replay-backed evidence, never opaque solver-state dumps.");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private void CreateSessionCheckpoint()
    {
        try
        {
            if (_sessionWorkspace is null)
            {
                throw new InvalidOperationException("No M10.7 session workspace is attached.");
            }
            var checkpoint = _sessionWorkspace.CreateCheckpoint();
            SessionStatus = $"CHECKPOINT CREATED — {checkpoint.CheckpointId} at STEP {checkpoint.LogicalStep:D8}.";
        }
        catch (InvalidOperationException exception)
        {
            SessionStatus = $"SESSION ACTION BLOCKED — {exception.Message}";
        }
    }

    private void VerifySessionReplay()
    {
        try
        {
            if (_sessionWorkspace is null)
            {
                throw new InvalidOperationException("No M10.7 session workspace is attached.");
            }
            SessionStatus = _sessionWorkspace.VerifyCurrentReplay();
        }
        catch (InvalidOperationException exception)
        {
            SessionStatus = $"REPLAY VERIFICATION FAILED/BLOCKED — {exception.Message}";
        }
    }

    private string BuildGuidanceText()
    {
        var guidance = _snapshot.Guidance;
        if (guidance is null)
        {
            return "No canonical guidance plan is available for the loaded scenario.";
        }

        var mode = guidance.GuidanceMode switch
        {
            NuclearReactorSimulator.Application.Scenarios.Training.TrainingGuidanceMode.Hidden => "HIDDEN",
            NuclearReactorSimulator.Application.Scenarios.Training.TrainingGuidanceMode.ChecklistOnly => "CHECKLIST ONLY",
            NuclearReactorSimulator.Application.Scenarios.Training.TrainingGuidanceMode.Guided => "GUIDED",
            _ => "UNKNOWN",
        };
        var lines = new List<string>
        {
            $"PROCEDURE : {guidance.ProcedureTitle}",
            $"ASSISTANCE: {mode}",
            string.Empty,
            guidance.Summary,
        };

        if (guidance.IsStepByStepVisible)
        {
            lines.Add(string.Empty);
            lines.Add("PROCEDURE STEPS");
            lines.AddRange(guidance.Steps.Select(static step =>
                $"{StepMarker(step.State)} {step.Sequence:D2}. {step.Title}{Environment.NewLine}     {step.Instruction}"));
        }

        if (guidance.IsChecklistVisible && guidance.TrainingCheckpoints.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("TRAINING CHECKPOINTS");
            if (!string.IsNullOrWhiteSpace(guidance.TrainingScoreText))
            {
                lines.Add(guidance.TrainingScoreText);
            }
            lines.AddRange(guidance.TrainingCheckpoints.Select(static checkpoint =>
                $"{(checkpoint.IsSatisfied ? "[OK]" : "[--]")} {checkpoint.Title}" +
                (checkpoint.FirstSatisfiedLogicalStep.HasValue
                    ? $" · STEP {checkpoint.FirstSatisfiedLogicalStep.Value:D8}"
                    : string.Empty)));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildInformationText()
    {
        var information = _snapshot.Information;
        if (information is null)
        {
            return "No canonical plant-information projection is available.";
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            information.Sections.Select(section =>
                $"[{section.Title}]{Environment.NewLine}" +
                string.Join(Environment.NewLine, section.Items.Select(static item =>
                    $"{ProvenanceMarker(item.Provenance),-13} {item.Label,-28} {item.ValueText} {item.Unit}".TrimEnd()))));
    }


    private string BuildAlarmsText()
    {
        var alarms = _snapshot.Alarms;
        if (alarms is null)
        {
            return "No canonical annunciator projection is available.";
        }

        var lines = new List<string>
        {
            $"ANNUNCIATED     {alarms.AnnunciatedCount}",
            $"UNACKNOWLEDGED {alarms.UnacknowledgedCount}",
            $"FIRST OUT      {alarms.FirstOutCount}",
            string.Empty,
            "CURRENT ANNUNCIATOR",
        };

        if (alarms.Alarms.Count == 0)
        {
            lines.Add("[OK] NO ANNUNCIATED ALARMS");
        }
        else
        {
            lines.AddRange(alarms.Alarms.Select(static alarm =>
                $"{(alarm.IsFirstOut ? "[FIRST]" : "[ALARM]")} {alarm.Severity,-7} {alarm.AlarmId} · {alarm.Title}{Environment.NewLine}        {alarm.State}"));
        }

        lines.Add(string.Empty);
        lines.Add("RECENT ALARM EVENTS");
        if (alarms.RecentEvents.Count == 0)
        {
            lines.Add("No alarm events recorded in the bounded live presentation history.");
        }
        else
        {
            lines.AddRange(alarms.RecentEvents.Take(30).Select(static item =>
                $"#{item.Sequence:D5} STEP {item.LogicalStep:D8} {item.Kind,-12} {item.AlarmId} · {item.AlarmTitle}"));
        }

        lines.Add(string.Empty);
        lines.Add("READ-ONLY WORKSTATION — ACK/RESET remains routed through canonical typed command seams and is staged for M10.4.");
        return string.Join(Environment.NewLine, lines);
    }

    private string BuildLogText()
    {
        var log = _snapshot.Log;
        if (log is null)
        {
            return "No operational-history projection is available.";
        }

        var lines = new List<string>
        {
            "LIVE // M6.6 BOUNDED PRESENTATION HISTORY",
        };
        if (log.LiveTrends.Count == 0)
        {
            lines.Add("No live trend series are configured.");
        }
        else
        {
            lines.AddRange(log.LiveTrends.Select(static trend =>
                $"{trend.Title,-28} {trend.Current,-14} {trend.Sparkline}  [{trend.Minimum} .. {trend.Maximum}] · {trend.SampleCount} samples"));
        }

        lines.Add(string.Empty);
        lines.Add("LIVE ALARM EVENT FEED");
        lines.AddRange(log.LiveEvents.Count == 0
            ? new[] { "No bounded live alarm events." }
            : log.LiveEvents.Take(30).Select(static item =>
                $"#{item.Sequence:D5} STEP {item.LogicalStep:D8} {item.Kind,-12} {item.AlarmId} · {item.AlarmTitle}"));

        lines.Add(string.Empty);
        lines.Add("SESSION // M9.1 DETERMINISTIC RECORDER EVIDENCE");
        if (!log.SessionEvidenceAvailable)
        {
            lines.Add("M9.1 recorder evidence is not attached to this live desktop session. No second session log is synthesized.");
        }
        else
        {
            lines.AddRange(log.SessionEvents.Count == 0
                ? new[] { "Recorder attached; no session events observed yet." }
                : log.SessionEvents.Take(40).Select(static item =>
                    $"#{item.Sequence:D5} STEP {item.LogicalStep:D8} {item.Kind,-22} {item.SourceId} · {item.Detail}"));
        }

        lines.Add(string.Empty);
        lines.Add("INCIDENT // M9.2 POST-INCIDENT ANALYSIS");
        if (log.Incident is null)
        {
            lines.Add("No finalized immutable post-incident report is loaded. M10.3 does not infer incident causality from the live stream.");
        }
        else
        {
            lines.Add($"ANCHOR {log.Incident.AnchorText}");
            if (log.Incident.PrecedingCheckpointId is not null)
            {
                lines.Add($"PRECEDING CHECKPOINT {log.Incident.PrecedingCheckpointId}");
            }
            lines.AddRange(log.Incident.MetricLines);
            lines.Add("TIMELINE");
            lines.AddRange(log.Incident.Timeline.Take(40).Select(static item =>
                $"#{item.Sequence:D5} STEP {item.LogicalStep:D8} T{item.RelativeLogicalStep:+0;-0;0} {item.Relation,-6} {item.Kind,-22} {item.SourceId} · {item.Detail}"));
        }

        return string.Join(Environment.NewLine, lines);
    }


    private string BuildCommandsText()
    {
        var commandConsole = _snapshot.Commands;
        if (commandConsole is null)
        {
            return "No contextual typed-command catalog is available.";
        }

        var lines = new List<string>
        {
            $"COMMANDS {commandConsole.Commands.Count} · AVAILABLE {commandConsole.AvailableCount} · BLOCKED {commandConsole.BlockedCount} · UNAVAILABLE {commandConsole.UnavailableCount}",
            "Availability is presentation context only. Every dispatch is revalidated fail-closed by the canonical application/runtime boundary.",
            string.Empty,
        };
        lines.AddRange(commandConsole.Commands.Select(static command => command.ConsoleLine));
        return string.Join(Environment.NewLine, lines);
    }

    private void ExecuteSelectedCommand()
    {
        if (!IsCommandsPageSelected)
        {
            return;
        }

        if (SelectedCommand is null)
        {
            CommandConsoleStatus = "No command selected.";
            return;
        }

        if (!SelectedCommand.CanDispatch)
        {
            CommandConsoleStatus = $"NOT DISPATCHED — {SelectedCommand.DisplayName}: {SelectedCommand.BlockReason}";
            return;
        }

        if (_commandDispatcher is null)
        {
            CommandConsoleStatus = "NOT DISPATCHED — no IControlRoomCommandDispatcher is attached to this presentation instance.";
            return;
        }

        try
        {
            _commandDispatcher.Dispatch(SelectedCommand.Command);
            CommandConsoleStatus = $"DISPATCHED — {SelectedCommand.DisplayName} · {SelectedCommand.TargetText}. Runtime/scenario validation accepted the typed intent.";
        }
        catch (InvalidOperationException exception)
        {
            CommandConsoleStatus = $"BLOCKED BY RUNTIME/SCENARIO — {exception.Message}";
        }
    }

    private string BuildModesText()
    {
        var modes = _snapshot.Modes;
        if (modes is null)
        {
            return "No M10.5/M10.6 assistance/control-authority projection is attached.";
        }

        var automation = modes.PlantControlAuthority;
        var lines = new List<string>
        {
            "TRAINING ASSISTANCE",
            $"CURRENT      {modes.TrainingAssistance.ToString().ToUpperInvariant()}",
            string.Empty,
            "PLANT CONTROL AUTHORITY",
            $"REQUESTED    {automation.RequestedAuthority.ToString().ToUpperInvariant()}",
            $"EFFECTIVE    {automation.EffectiveAuthority.ToString().ToUpperInvariant()}",
            $"HEALTH       {automation.Health.ToString().ToUpperInvariant()}",
            $"MIXED MODE   {(automation.IsMixedMode ? "YES" : "NO")}",
            $"OBJECTIVE    {automation.ObjectiveText}",
            $"TRANSITION   {automation.TransitionSequence}",
        };

        if (!string.IsNullOrWhiteSpace(automation.DegradationReason))
        {
            lines.Add($"DEGRADED BY  {automation.DegradationReason}");
        }

        lines.Add(string.Empty);
        lines.Add("LOCAL CONTROLLERS");
        lines.AddRange(automation.ControllerModes.Select(static controller =>
            $"{controller.Area,-22} {controller.ControllerId,-20} {controller.Mode,-9} SETPOINT {controller.Setpoint:0.###} {controller.SetpointUnit}"));
        return string.Join(Environment.NewLine, lines);
    }

    private void SetTrainingAssistance(TrainingGuidanceMode mode)
    {
        if (_trainingAssistanceDispatcher is null)
        {
            ModesStatus = "TRAINING ASSISTANCE NOT CHANGED — no typed training-assistance dispatcher is attached.";
            return;
        }

        _trainingAssistanceDispatcher.SetGuidanceMode(mode);
        ModesStatus = $"TRAINING ASSISTANCE REQUESTED — {mode}. Physics and scoring are unchanged.";
    }

    private void SetPlantAuthority(PlantControlAuthorityMode mode)
    {
        if (_plantControlAuthorityDispatcher is null)
        {
            ModesStatus = "PLANT CONTROL MODE NOT CHANGED — no M10.5/M10.6 authority dispatcher is attached.";
            return;
        }

        try
        {
            _plantControlAuthorityDispatcher.RequestAuthority(mode);
            ModesStatus = $"PLANT CONTROL AUTHORITY REQUESTED — {mode}. Effective authority is resolved deterministically at the next fixed step.";
        }
        catch (InvalidOperationException exception)
        {
            ModesStatus = $"PLANT CONTROL AUTHORITY BLOCKED — {exception.Message}";
        }
    }

    private void HoldCurrentOperatingPoint()
    {
        if (_plantControlAuthorityDispatcher is null)
        {
            ModesStatus = "SUPERVISORY OBJECTIVE NOT CHANGED — no M10.6 authority dispatcher is attached.";
            return;
        }

        try
        {
            _plantControlAuthorityDispatcher.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
            _plantControlAuthorityDispatcher.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
            ModesStatus = "SUPERVISORY OBJECTIVE REQUESTED — hold the current measured reactor-power / turbine-speed operating point through canonical M5 local controllers.";
        }
        catch (InvalidOperationException exception)
        {
            ModesStatus = $"SUPERVISORY OBJECTIVE BLOCKED — {exception.Message}";
        }
    }

    private string BuildDiagnosticsText()
    {
        var diagnostics = _snapshot.Diagnostics;
        if (diagnostics is null)
        {
            return "No procedure-specific readiness evaluator is available for the loaded scenario. No diagnostic criteria are invented.";
        }

        var header = diagnostics.AllChecksSatisfied
            ? $"STATUS: ALL DECLARED CHECKS SATISFIED · {diagnostics.SatisfiedCount}/{diagnostics.Items.Count}"
            : $"STATUS: {diagnostics.SatisfiedCount}/{diagnostics.Items.Count} CHECKS SATISFIED · {diagnostics.UnsatisfiedCount} NOT SATISFIED";
        var lines = diagnostics.Items.Select(static item =>
            $"{(item.IsSatisfied ? "[OK]" : "[!!]")} {item.Title}{Environment.NewLine}     {item.Observation}");
        return $"DIAGNOSTIC: {diagnostics.Title}{Environment.NewLine}{header}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }

    private static string StepMarker(OperatorComputerGuidanceStepState state) => state switch
    {
        OperatorComputerGuidanceStepState.Completed => "[OK]",
        OperatorComputerGuidanceStepState.Current => "[>>]",
        OperatorComputerGuidanceStepState.Pending => "[--]",
        _ => "[??]",
    };

    private static string ProvenanceMarker(OperatorComputerInformationProvenance provenance) => provenance switch
    {
        OperatorComputerInformationProvenance.Measured => "[MEASURED]",
        OperatorComputerInformationProvenance.ModelDiagnostic => "[MODEL]",
        OperatorComputerInformationProvenance.CanonicalState => "[STATE]",
        OperatorComputerInformationProvenance.Unavailable => "[UNAVAILABLE]",
        _ => "[UNKNOWN]",
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
