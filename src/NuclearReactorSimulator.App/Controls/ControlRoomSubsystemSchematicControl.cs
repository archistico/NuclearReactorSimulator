using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>
/// M10.9.4 renderer for Application-owned subsystem schematic presentation topology.
/// It renders equipment/process/signal semantics only; it owns no physics, topology inference, control or protection rules.
/// </summary>
public sealed class ControlRoomSubsystemSchematicControl : Panel
{
    public static readonly StyledProperty<ControlRoomSubsystemSchematicSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ControlRoomSubsystemSchematicControl, ControlRoomSubsystemSchematicSnapshot?>(nameof(Snapshot));

    private readonly SchematicConnectionLayer _connectionLayer = new() { IsHitTestVisible = false };
    private readonly List<(ControlRoomSubsystemSchematicNodeSnapshot Snapshot, Border Card)> _nodeCards = new();
    private readonly List<(ControlRoomSubsystemSchematicConnectionSnapshot Snapshot, Border Label)> _connectionLabels = new();

    public ControlRoomSubsystemSchematicControl()
    {
        ClipToBounds = true;
        MinHeight = 500d;
        RebuildChildren();
    }

    public ControlRoomSubsystemSchematicSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SnapshotProperty)
        {
            RebuildChildren();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = new Size(
            double.IsInfinity(availableSize.Width) ? 1120d : Math.Max(780d, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? 560d : Math.Max(500d, availableSize.Height));

        _connectionLayer.Measure(desired);
        foreach (var (_, card) in _nodeCards)
        {
            card.Measure(desired);
        }

        foreach (var (_, label) in _connectionLabels)
        {
            label.Measure(new Size(200d, 78d));
        }

        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _connectionLayer.Arrange(new Rect(0d, 0d, finalSize.Width, finalSize.Height));

        foreach (var (snapshot, card) in _nodeCards)
        {
            card.Arrange(new Rect(
                snapshot.X * finalSize.Width,
                snapshot.Y * finalSize.Height,
                Math.Max(86d, snapshot.Width * finalSize.Width),
                Math.Max(72d, snapshot.Height * finalSize.Height)));
        }

        foreach (var (snapshot, label) in _connectionLabels)
        {
            var width = Math.Min(190d, Math.Max(105d, finalSize.Width * 0.145d));
            var height = Math.Min(74d, label.DesiredSize.Height + 7d);
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
        _nodeCards.Clear();
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

        foreach (var node in snapshot.Nodes)
        {
            var card = BuildNodeCard(node);
            _nodeCards.Add((node, card));
            Children.Add(card);
        }

        InvalidateMeasure();
        InvalidateArrange();
    }

    private static Border BuildNodeCard(ControlRoomSubsystemSchematicNodeSnapshot node)
    {
        var glyph = new SchematicNodeGlyph
        {
            Kind = node.Kind,
            State = node.State,
            Height = 34d,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var stack = new StackPanel { Spacing = 2.5d };
        stack.Children.Add(glyph);
        stack.Children.Add(new TextBlock
        {
            Text = node.DisplayName,
            FontSize = 10.5d,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(new TextBlock
        {
            Text = node.StatusText,
            FontSize = 8d,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.Accent(node.State),
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(Mono(node.PrimaryText, 8.5d, Brushes.White));
        stack.Children.Add(Mono(node.SecondaryText, 7.6d, ControlRoomPalette.TextMuted));

        var ports = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 5d };
        ports.Children.Add(new TextBlock
        {
            Text = node.InputText,
            FontSize = 6.7d,
            Foreground = ControlRoomPalette.InformationAccent,
            TextWrapping = TextWrapping.Wrap,
        });
        var output = new TextBlock
        {
            Text = node.OutputText,
            FontSize = 6.7d,
            Foreground = ControlRoomPalette.InformationAccent,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Right,
        };
        Grid.SetColumn(output, 1);
        ports.Children.Add(output);
        stack.Children.Add(ports);

        return new Border
        {
            Padding = new Thickness(7d, 6d),
            CornerRadius = new CornerRadius(6d),
            Background = Brush.Parse("#E911161D"),
            BorderBrush = ControlRoomPalette.Accent(node.State),
            BorderThickness = new Thickness(1.15d),
            Child = stack,
            IsHitTestVisible = false,
        };
    }

    private static Border BuildConnectionLabel(ControlRoomSubsystemSchematicConnectionSnapshot connection)
    {
        var stack = new StackPanel { Spacing = 0.5d };
        stack.Children.Add(new TextBlock
        {
            Text = connection.Label,
            FontSize = 7.4d,
            FontWeight = FontWeight.Bold,
            Foreground = ConnectionBrush(connection.Kind, connection.State),
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(Mono(connection.PrimaryText, 7.3d, Brushes.White));
        stack.Children.Add(Mono(connection.SecondaryText, 6.8d, ControlRoomPalette.TextMuted));

        return new Border
        {
            IsHitTestVisible = false,
            Padding = new Thickness(4d, 2.5d),
            CornerRadius = new CornerRadius(3d),
            Background = Brush.Parse("#E30B1016"),
            BorderBrush = Brush.Parse("#3345505B"),
            BorderThickness = new Thickness(1d),
            Child = stack,
        };
    }

    private static TextBlock Mono(string text, double size, IBrush brush) => new()
    {
        Text = text,
        FontSize = size,
        FontFamily = new FontFamily("Consolas"),
        Foreground = brush,
        TextWrapping = TextWrapping.Wrap,
    };

    private static IBrush ConnectionBrush(ControlRoomSubsystemSchematicConnectionKind kind, ControlRoomVisualState state)
    {
        if (state == ControlRoomVisualState.Trip && kind == ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride)
        {
            return ControlRoomPalette.Accent(ControlRoomVisualState.Trip);
        }

        return kind switch
        {
            ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant => Brush.Parse("#55C3D1"),
            ControlRoomSubsystemSchematicConnectionKind.Steam => Brush.Parse("#D8EEF2"),
            ControlRoomSubsystemSchematicConnectionKind.Condensate => Brush.Parse("#4D9DBB"),
            ControlRoomSubsystemSchematicConnectionKind.Feedwater => Brush.Parse("#5FC9A9"),
            ControlRoomSubsystemSchematicConnectionKind.Mechanical => Brush.Parse("#D7B765"),
            ControlRoomSubsystemSchematicConnectionKind.Electrical => Brush.Parse("#B89AE8"),
            ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal => Brush.Parse("#6FCFE0"),
            ControlRoomSubsystemSchematicConnectionKind.ControlSignal => Brush.Parse("#AFA0E8"),
            ControlRoomSubsystemSchematicConnectionKind.FeedbackSignal => Brush.Parse("#67D7B0"),
            ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride => Brush.Parse("#F2A65A"),
            ControlRoomSubsystemSchematicConnectionKind.AlarmSignal => Brush.Parse("#E8C46B"),
            ControlRoomSubsystemSchematicConnectionKind.ThermalInfluence => Brush.Parse("#E49D73"),
            _ => ControlRoomPalette.InformationAccent,
        };
    }

    private sealed class SchematicConnectionLayer : Control
    {
        public ControlRoomSubsystemSchematicSnapshot? Snapshot { get; set; }

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

                var brush = ConnectionBrush(connection.Kind, connection.State);
                var thickness = connection.Kind switch
                {
                    ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride => 4d,
                    ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant or
                    ControlRoomSubsystemSchematicConnectionKind.Steam or
                    ControlRoomSubsystemSchematicConnectionKind.Condensate or
                    ControlRoomSubsystemSchematicConnectionKind.Feedwater or
                    ControlRoomSubsystemSchematicConnectionKind.Mechanical or
                    ControlRoomSubsystemSchematicConnectionKind.Electrical => 2.8d,
                    _ => 1.45d,
                };

                for (var index = 1; index < connection.Route.Count; index++)
                {
                    var from = Map(connection.Route[index - 1]);
                    var to = Map(connection.Route[index]);
                    context.DrawLine(new Pen(brush, thickness), from, to);
                    if (IsSignal(connection.Kind))
                    {
                        var midpoint = new Point((from.X + to.X) / 2d, (from.Y + to.Y) / 2d);
                        context.DrawEllipse(brush, null, midpoint, 2.2d, 2.2d);
                    }
                }

                var penultimate = Map(connection.Route[^2]);
                var last = Map(connection.Route[^1]);
                DrawArrow(context, brush, penultimate, last, connection.Kind == ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride ? 9d : 7d);
            }
        }

        private Point Map(ControlRoomSubsystemSchematicPointSnapshot point)
            => new(point.X * Bounds.Width, point.Y * Bounds.Height);

        private static bool IsSignal(ControlRoomSubsystemSchematicConnectionKind kind) => kind is
            ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal or
            ControlRoomSubsystemSchematicConnectionKind.ControlSignal or
            ControlRoomSubsystemSchematicConnectionKind.FeedbackSignal or
            ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride or
            ControlRoomSubsystemSchematicConnectionKind.AlarmSignal;

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
            context.DrawLine(new Pen(brush, 2d), to, new Point(basePoint.X + (px * size * .55d), basePoint.Y + (py * size * .55d)));
            context.DrawLine(new Pen(brush, 2d), to, new Point(basePoint.X - (px * size * .55d), basePoint.Y - (py * size * .55d)));
        }
    }

    private sealed class SchematicNodeGlyph : Control
    {
        public ControlRoomSubsystemSchematicNodeKind Kind { get; set; }
        public ControlRoomVisualState State { get; set; }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var pen = new Pen(ControlRoomPalette.Accent(State), 1.8d);
            var muted = new Pen(ControlRoomPalette.GaugeTick, 1d);
            var w = Bounds.Width;
            var h = Bounds.Height;
            if (w < 20d || h < 18d)
            {
                return;
            }

            switch (Kind)
            {
                case ControlRoomSubsystemSchematicNodeKind.Pump:
                    DrawPump(context, new Point(w * .5d, h * .5d), h * .32d, pen);
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Valve:
                    context.DrawLine(pen, new Point(w * .12d, h * .5d), new Point(w * .88d, h * .5d));
                    context.DrawLine(pen, new Point(w * .34d, h * .24d), new Point(w * .5d, h * .5d));
                    context.DrawLine(pen, new Point(w * .34d, h * .76d), new Point(w * .5d, h * .5d));
                    context.DrawLine(pen, new Point(w * .66d, h * .24d), new Point(w * .5d, h * .5d));
                    context.DrawLine(pen, new Point(w * .66d, h * .76d), new Point(w * .5d, h * .5d));
                    break;
                case ControlRoomSubsystemSchematicNodeKind.SteamDrum:
                    context.DrawRectangle(null, pen, new Rect(w * .14d, h * .18d, w * .72d, h * .64d), h * .28d, h * .28d);
                    context.DrawLine(muted, new Point(w * .27d, h * .58d), new Point(w * .73d, h * .58d));
                    break;
                case ControlRoomSubsystemSchematicNodeKind.TurbineStage:
                    for (var i = 0; i < 4; i++)
                    {
                        var x = w * (.12d + (i * .19d));
                        context.DrawRectangle(null, pen, new Rect(x, h * (.2d + i * .04d), w * .14d, h * (.6d - i * .08d)), 2d, 2d);
                    }
                    context.DrawLine(pen, new Point(w * .05d, h * .5d), new Point(w * .93d, h * .5d));
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Rotor:
                case ControlRoomSubsystemSchematicNodeKind.Generator:
                    context.DrawEllipse(null, pen, new Point(w * .5d, h * .5d), h * .34d, h * .34d);
                    context.DrawEllipse(null, muted, new Point(w * .5d, h * .5d), h * .15d, h * .15d);
                    context.DrawLine(pen, new Point(w * .08d, h * .5d), new Point(w * .16d, h * .5d));
                    context.DrawLine(pen, new Point(w * .84d, h * .5d), new Point(w * .92d, h * .5d));
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Breaker:
                    context.DrawLine(pen, new Point(w * .12d, h * .56d), new Point(w * .43d, h * .56d));
                    context.DrawLine(pen, new Point(w * .57d, h * .56d), new Point(w * .88d, h * .56d));
                    context.DrawLine(pen, new Point(w * .43d, h * .56d), new Point(w * .60d, h * .30d));
                    context.DrawEllipse(ControlRoomPalette.Accent(State), null, new Point(w * .43d, h * .56d), 2.6d, 2.6d);
                    context.DrawEllipse(ControlRoomPalette.Accent(State), null, new Point(w * .57d, h * .56d), 2.6d, 2.6d);
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Grid:
                    context.DrawLine(pen, new Point(w * .5d, h * .10d), new Point(w * .5d, h * .90d));
                    for (var i = 0; i < 3; i++)
                    {
                        var y = h * (.28d + i * .22d);
                        context.DrawLine(pen, new Point(w * .2d, y), new Point(w * .8d, y));
                    }
                    break;
                case ControlRoomSubsystemSchematicNodeKind.ReactorCore:
                    context.DrawRectangle(null, pen, new Rect(w * .32d, 2d, w * .36d, h - 4d), 8d, 8d);
                    for (var i = 0; i < 4; i++)
                    {
                        var x = w * (.39d + i * .075d);
                        context.DrawLine(muted, new Point(x, h * .22d), new Point(x, h * .78d));
                    }
                    break;
                case ControlRoomSubsystemSchematicNodeKind.ControlRods:
                    for (var i = 0; i < 5; i++)
                    {
                        var x = w * (.25d + i * .12d);
                        context.DrawLine(pen, new Point(x, h * .12d), new Point(x, h * .84d));
                    }
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Condenser:
                    context.DrawRectangle(null, pen, new Rect(w * .12d, h * .14d, w * .76d, h * .72d), 3d, 3d);
                    for (var i = 0; i < 4; i++)
                    {
                        var y = h * (.30d + i * .12d);
                        context.DrawLine(muted, new Point(w * .23d, y), new Point(w * .77d, y));
                    }
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Protection:
                    context.DrawRectangle(null, pen, new Rect(w * .22d, h * .15d, w * .56d, h * .70d), 4d, 4d);
                    context.DrawLine(pen, new Point(w * .36d, h * .35d), new Point(w * .64d, h * .65d));
                    context.DrawLine(pen, new Point(w * .64d, h * .35d), new Point(w * .36d, h * .65d));
                    break;
                case ControlRoomSubsystemSchematicNodeKind.Instrumentation:
                    context.DrawEllipse(null, pen, new Point(w * .5d, h * .5d), h * .34d, h * .34d);
                    context.DrawLine(pen, new Point(w * .5d, h * .5d), new Point(w * .68d, h * .32d));
                    break;
                default:
                    context.DrawRectangle(null, pen, new Rect(w * .14d, h * .18d, w * .72d, h * .64d), 4d, 4d);
                    context.DrawLine(muted, new Point(w * .24d, h * .50d), new Point(w * .76d, h * .50d));
                    break;
            }
        }

        private static void DrawPump(DrawingContext context, Point center, double radius, IPen pen)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            context.DrawLine(pen, new Point(center.X - radius * .45d, center.Y), new Point(center.X + radius * .45d, center.Y - radius * .45d));
            context.DrawLine(pen, new Point(center.X - radius * .45d, center.Y), new Point(center.X + radius * .45d, center.Y + radius * .45d));
        }
    }
}
