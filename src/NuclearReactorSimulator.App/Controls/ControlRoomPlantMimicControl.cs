using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>
/// M10.9.3 interactive whole-plant mimic. Layout/topology/value semantics arrive from the Application HMI snapshot;
/// this control only renders them and publishes the selected presentation element id.
/// </summary>
public sealed class ControlRoomPlantMimicControl : Panel
{
    public static readonly StyledProperty<ControlRoomPlantMimicSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ControlRoomPlantMimicControl, ControlRoomPlantMimicSnapshot?>(nameof(Snapshot));

    public static readonly StyledProperty<string?> SelectedElementIdProperty =
        AvaloniaProperty.Register<ControlRoomPlantMimicControl, string?>(nameof(SelectedElementId));

    private readonly MimicConnectionLayer _connectionLayer = new() { IsHitTestVisible = false };
    private readonly List<(ControlRoomPlantMimicElementSnapshot Snapshot, Border Card)> _elementCards = new();
    private readonly List<(ControlRoomPlantMimicConnectionSnapshot Snapshot, Border Label)> _connectionLabels = new();

    public ControlRoomPlantMimicControl()
    {
        ClipToBounds = true;
        MinHeight = 520d;
        RebuildChildren();
    }

    public ControlRoomPlantMimicSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public string? SelectedElementId
    {
        get => GetValue(SelectedElementIdProperty);
        set => SetValue(SelectedElementIdProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SnapshotProperty)
        {
            RebuildChildren();
        }
        else if (change.Property == SelectedElementIdProperty)
        {
            RefreshSelection();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = new Size(
            double.IsInfinity(availableSize.Width) ? 1100d : Math.Max(760d, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? 600d : Math.Max(520d, availableSize.Height));

        _connectionLayer.Measure(desired);
        foreach (var (_, card) in _elementCards)
        {
            card.Measure(desired);
        }

        foreach (var (_, label) in _connectionLabels)
        {
            label.Measure(new Size(190d, 80d));
        }

        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _connectionLayer.Arrange(new Rect(0d, 0d, finalSize.Width, finalSize.Height));

        foreach (var (snapshot, card) in _elementCards)
        {
            card.Arrange(new Rect(
                snapshot.X * finalSize.Width,
                snapshot.Y * finalSize.Height,
                Math.Max(72d, snapshot.Width * finalSize.Width),
                Math.Max(82d, snapshot.Height * finalSize.Height)));
        }

        foreach (var (snapshot, label) in _connectionLabels)
        {
            var width = Math.Min(170d, Math.Max(108d, finalSize.Width * 0.14d));
            var height = Math.Min(70d, label.DesiredSize.Height + 8d);
            label.Arrange(new Rect(
                (snapshot.LabelX * finalSize.Width) - (width / 2d),
                (snapshot.LabelY * finalSize.Height) - (height / 2d),
                width,
                height));
        }

        return finalSize;
    }

    private void RebuildChildren()
    {
        Children.Clear();
        _elementCards.Clear();
        _connectionLabels.Clear();

        _connectionLayer.Snapshot = Snapshot;
        Children.Add(_connectionLayer);

        var snapshot = Snapshot;
        if (snapshot is null)
        {
            return;
        }

        foreach (var connection in snapshot.Connections)
        {
            var label = BuildConnectionLabel(connection);
            _connectionLabels.Add((connection, label));
            Children.Add(label);
        }

        foreach (var element in snapshot.Elements)
        {
            var card = BuildElementCard(element);
            _elementCards.Add((element, card));
            Children.Add(card);
        }

        RefreshSelection();
        InvalidateMeasure();
        InvalidateArrange();
    }

    private Border BuildElementCard(ControlRoomPlantMimicElementSnapshot element)
    {
        var glyph = new MimicEquipmentGlyph
        {
            Kind = element.Kind,
            State = element.State,
            Height = 43d,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var title = new TextBlock
        {
            Text = element.DisplayName,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        };
        var status = new TextBlock
        {
            Text = element.StatusText,
            FontSize = 9,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.Accent(element.State),
            TextWrapping = TextWrapping.Wrap,
        };
        var primary = Mono(element.PrimaryValueText, 10d, Brushes.White);
        var secondary = Mono(element.SecondaryValueText, 9d, ControlRoomPalette.TextMuted);
        var ports = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 6d };
        ports.Children.Add(new TextBlock
        {
            Text = element.InputText,
            FontSize = 7.5d,
            Foreground = ControlRoomPalette.InformationAccent,
            TextWrapping = TextWrapping.Wrap,
        });
        var output = new TextBlock
        {
            Text = element.OutputText,
            FontSize = 7.5d,
            Foreground = ControlRoomPalette.InformationAccent,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Right,
        };
        Grid.SetColumn(output, 1);
        ports.Children.Add(output);

        var content = new StackPanel
        {
            Spacing = 3d,
            Children = { glyph, title, status, primary, secondary, ports },
        };

        var border = new Border
        {
            Padding = new Thickness(8d, 7d),
            CornerRadius = new CornerRadius(7d),
            Background = Brush.Parse("#E811161D"),
            BorderBrush = ControlRoomPalette.Accent(element.State),
            BorderThickness = new Thickness(1.2d),
            Child = content,
            Focusable = true,
        };

        border.GotFocus += (_, _) => SetCurrentValue(SelectedElementIdProperty, element.ElementId);
        border.PointerPressed += (_, _) => SetCurrentValue(SelectedElementIdProperty, element.ElementId);
        return border;
    }

    private static Border BuildConnectionLabel(ControlRoomPlantMimicConnectionSnapshot connection)
    {
        var stack = new StackPanel { Spacing = 1d };
        stack.Children.Add(new TextBlock
        {
            Text = $"{connection.MediumText}  →",
            FontSize = 8d,
            FontWeight = FontWeight.Bold,
            Foreground = MediumBrush(connection.Medium),
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(Mono(connection.PrimaryText, 8d, Brushes.White));
        stack.Children.Add(Mono(connection.SecondaryText, 7.5d, ControlRoomPalette.TextMuted));

        return new Border
        {
            IsHitTestVisible = false,
            Padding = new Thickness(5d, 3d),
            CornerRadius = new CornerRadius(4d),
            Background = Brush.Parse("#D90B1016"),
            BorderBrush = connection.State == ControlRoomVisualState.Trip
                ? ControlRoomPalette.Accent(ControlRoomVisualState.Trip)
                : Brush.Parse("#3345505B"),
            BorderThickness = new Thickness(1d),
            Child = stack,
        };
    }

    private void RefreshSelection()
    {
        foreach (var (snapshot, card) in _elementCards)
        {
            var selected = string.Equals(snapshot.ElementId, SelectedElementId, StringComparison.Ordinal);
            card.BorderBrush = selected ? ControlRoomPalette.InformationAccentStrong : ControlRoomPalette.Accent(snapshot.State);
            card.BorderThickness = selected ? new Thickness(2.5d) : new Thickness(1.2d);
            card.Opacity = selected || string.IsNullOrEmpty(SelectedElementId) ? 1d : 0.84d;
        }

        foreach (var (snapshot, label) in _connectionLabels)
        {
            var related = string.IsNullOrEmpty(SelectedElementId)
                || string.Equals(snapshot.FromElementId, SelectedElementId, StringComparison.Ordinal)
                || string.Equals(snapshot.ToElementId, SelectedElementId, StringComparison.Ordinal);
            label.Opacity = related ? 1d : 0.34d;
        }

        _connectionLayer.SelectedElementId = SelectedElementId;
        _connectionLayer.InvalidateVisual();
    }

    private static TextBlock Mono(string text, double size, IBrush brush) => new()
    {
        Text = text,
        FontSize = size,
        FontFamily = new FontFamily("Consolas"),
        Foreground = brush,
        TextWrapping = TextWrapping.Wrap,
    };

    private static IBrush MediumBrush(ControlRoomPlantMimicMedium medium) => medium switch
    {
        ControlRoomPlantMimicMedium.PrimaryCoolant => Brush.Parse("#55C3D1"),
        ControlRoomPlantMimicMedium.Steam => Brush.Parse("#D8EEF2"),
        ControlRoomPlantMimicMedium.Condensate => Brush.Parse("#4D9DBB"),
        ControlRoomPlantMimicMedium.Feedwater => Brush.Parse("#5FC9A9"),
        ControlRoomPlantMimicMedium.Mechanical => Brush.Parse("#D7B765"),
        ControlRoomPlantMimicMedium.Electrical => Brush.Parse("#B89AE8"),
        _ => ControlRoomPalette.InformationAccent,
    };

    private sealed class MimicConnectionLayer : Control
    {
        public ControlRoomPlantMimicSnapshot? Snapshot { get; set; }
        public string? SelectedElementId { get; set; }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var snapshot = Snapshot;
            if (snapshot is null || Bounds.Width <= 0d || Bounds.Height <= 0d)
            {
                return;
            }

            foreach (var connection in snapshot.Connections)
            {
                if (connection.Route.Count < 2)
                {
                    continue;
                }

                var related = string.IsNullOrEmpty(SelectedElementId)
                    || string.Equals(connection.FromElementId, SelectedElementId, StringComparison.Ordinal)
                    || string.Equals(connection.ToElementId, SelectedElementId, StringComparison.Ordinal);
                var medium = MediumBrush(connection.Medium);
                var brush = connection.State == ControlRoomVisualState.Trip
                    ? ControlRoomPalette.Accent(ControlRoomVisualState.Trip)
                    : medium;
                var thickness = related ? 3d : 1.5d;

                for (var index = 1; index < connection.Route.Count; index++)
                {
                    var from = Map(connection.Route[index - 1]);
                    var to = Map(connection.Route[index]);
                    context.DrawLine(new Pen(brush, thickness), from, to);
                }

                var penultimate = Map(connection.Route[^2]);
                var last = Map(connection.Route[^1]);
                DrawArrow(context, brush, penultimate, last, related ? 8d : 6d);
            }
        }

        private Point Map(ControlRoomPlantMimicPointSnapshot point)
            => new(point.X * Bounds.Width, point.Y * Bounds.Height);

        private static void DrawArrow(DrawingContext context, IBrush brush, Point from, Point to, double size)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length < 1d)
            {
                return;
            }

            var ux = dx / length;
            var uy = dy / length;
            var px = -uy;
            var py = ux;
            var basePoint = new Point(to.X - (ux * size), to.Y - (uy * size));
            context.DrawLine(new Pen(brush, 2d), to, new Point(basePoint.X + (px * size * 0.55d), basePoint.Y + (py * size * 0.55d)));
            context.DrawLine(new Pen(brush, 2d), to, new Point(basePoint.X - (px * size * 0.55d), basePoint.Y - (py * size * 0.55d)));
        }
    }

    private sealed class MimicEquipmentGlyph : Control
    {
        public ControlRoomPlantMimicElementKind Kind { get; set; }
        public ControlRoomVisualState State { get; set; }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var accent = ControlRoomPalette.Accent(State);
            var line = new Pen(accent, 2d);
            var muted = new Pen(ControlRoomPalette.GaugeTick, 1d);
            var w = Bounds.Width;
            var h = Bounds.Height;
            if (w < 20d || h < 20d)
            {
                return;
            }

            switch (Kind)
            {
                case ControlRoomPlantMimicElementKind.Reactor:
                    context.DrawRectangle(null, line, new Rect(w * 0.32d, 3d, w * 0.36d, h - 6d), 10d, 10d);
                    for (var i = 0; i < 4; i++)
                    {
                        var x = w * (0.39d + (i * 0.075d));
                        context.DrawLine(muted, new Point(x, h * 0.24d), new Point(x, h * 0.78d));
                    }
                    context.DrawLine(line, new Point(w * 0.18d, h * 0.72d), new Point(w * 0.32d, h * 0.72d));
                    context.DrawLine(line, new Point(w * 0.68d, h * 0.28d), new Point(w * 0.84d, h * 0.28d));
                    break;

                case ControlRoomPlantMimicElementKind.MainCirculation:
                    DrawPump(context, new Point(w * 0.34d, h * 0.52d), h * 0.25d, line);
                    DrawPump(context, new Point(w * 0.66d, h * 0.52d), h * 0.25d, line);
                    context.DrawLine(line, new Point(w * 0.08d, h * 0.52d), new Point(w * 0.21d, h * 0.52d));
                    context.DrawLine(line, new Point(w * 0.79d, h * 0.52d), new Point(w * 0.94d, h * 0.52d));
                    break;

                case ControlRoomPlantMimicElementKind.SteamDrums:
                    context.DrawRectangle(null, line, new Rect(w * 0.12d, h * 0.22d, w * 0.76d, h * 0.56d), h * 0.24d, h * 0.24d);
                    context.DrawLine(muted, new Point(w * 0.25d, h * 0.58d), new Point(w * 0.75d, h * 0.58d));
                    context.DrawLine(line, new Point(w * 0.5d, h * 0.08d), new Point(w * 0.5d, h * 0.22d));
                    break;

                case ControlRoomPlantMimicElementKind.Turbine:
                    for (var i = 0; i < 4; i++)
                    {
                        var x = w * (0.1d + (i * 0.2d));
                        var top = h * (0.22d + (i * 0.04d));
                        context.DrawRectangle(null, line, new Rect(x, top, w * 0.16d, h - (top * 2d)), 2d, 2d);
                    }
                    context.DrawLine(line, new Point(w * 0.03d, h * 0.5d), new Point(w * 0.94d, h * 0.5d));
                    break;

                case ControlRoomPlantMimicElementKind.Generator:
                    context.DrawEllipse(null, line, new Point(w * 0.5d, h * 0.5d), h * 0.36d, h * 0.36d);
                    context.DrawEllipse(null, muted, new Point(w * 0.5d, h * 0.5d), h * 0.18d, h * 0.18d);
                    context.DrawLine(line, new Point(w * 0.08d, h * 0.5d), new Point(w * 0.14d, h * 0.5d));
                    context.DrawLine(line, new Point(w * 0.86d, h * 0.5d), new Point(w * 0.94d, h * 0.5d));
                    break;

                case ControlRoomPlantMimicElementKind.Grid:
                    context.DrawLine(line, new Point(w * 0.5d, h * 0.12d), new Point(w * 0.5d, h * 0.88d));
                    for (var i = 0; i < 3; i++)
                    {
                        var y = h * (0.28d + (i * 0.22d));
                        context.DrawLine(line, new Point(w * 0.18d, y), new Point(w * 0.82d, y));
                    }
                    break;

                case ControlRoomPlantMimicElementKind.Condenser:
                    context.DrawRectangle(null, line, new Rect(w * 0.12d, h * 0.16d, w * 0.76d, h * 0.68d), 4d, 4d);
                    for (var i = 0; i < 4; i++)
                    {
                        var y = h * (0.3d + (i * 0.12d));
                        context.DrawLine(muted, new Point(w * 0.22d, y), new Point(w * 0.78d, y));
                    }
                    break;

                case ControlRoomPlantMimicElementKind.Feedwater:
                    context.DrawRectangle(null, line, new Rect(w * 0.12d, h * 0.18d, w * 0.36d, h * 0.64d), 4d, 4d);
                    DrawPump(context, new Point(w * 0.68d, h * 0.5d), h * 0.25d, line);
                    context.DrawLine(line, new Point(w * 0.48d, h * 0.5d), new Point(w * 0.55d, h * 0.5d));
                    context.DrawLine(line, new Point(w * 0.81d, h * 0.5d), new Point(w * 0.94d, h * 0.5d));
                    break;
            }
        }

        private static void DrawPump(DrawingContext context, Point center, double radius, IPen pen)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            context.DrawLine(pen, new Point(center.X - radius * 0.45d, center.Y), new Point(center.X + radius * 0.45d, center.Y - radius * 0.45d));
            context.DrawLine(pen, new Point(center.X - radius * 0.45d, center.Y), new Point(center.X + radius * 0.45d, center.Y + radius * 0.45d));
        }
    }
}
