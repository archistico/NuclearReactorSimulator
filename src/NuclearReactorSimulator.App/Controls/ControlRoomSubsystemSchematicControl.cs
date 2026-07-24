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
    private readonly TextBlock _connectionLegendHeading = new()
    {
        Text = "LINES & SIGNALS · LIVE VALUES",
        FontSize = 10d,
        FontWeight = FontWeight.Bold,
        Foreground = ControlRoomPalette.InformationAccent,
        LetterSpacing = 1d,
        IsHitTestVisible = false,
    };

    public ControlRoomSubsystemSchematicControl()
    {
        ClipToBounds = true;
        MinHeight = 620d;
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
        var width = double.IsInfinity(availableSize.Width) ? 1120d : Math.Max(780d, availableSize.Width);
        var columns = Math.Max(2, (int)(width / 205d));
        var rows = Math.Max(1, (int)Math.Ceiling(_connectionLabels.Count / (double)columns));
        var legendHeight = 38d + (rows * 58d);
        var desired = new Size(width, Math.Max(620d, 450d + legendHeight));

        _connectionLayer.Measure(new Size(desired.Width, 450d));
        foreach (var (_, card) in _nodeCards)
        {
            card.Measure(desired);
        }

        foreach (var (_, label) in _connectionLabels)
        {
            label.Measure(new Size((desired.Width - ((columns - 1) * 8d)) / columns, 54d));
        }
        _connectionLegendHeading.Measure(new Size(desired.Width, 24d));

        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Math.Max(2, (int)(finalSize.Width / 205d));
        var rows = Math.Max(1, (int)Math.Ceiling(_connectionLabels.Count / (double)columns));
        var legendHeight = 38d + (rows * 58d);
        var diagramHeight = Math.Max(430d, finalSize.Height - legendHeight);
        _connectionLayer.Arrange(new Rect(0d, 0d, finalSize.Width, diagramHeight));

        foreach (var (snapshot, card) in _nodeCards)
        {
            card.Arrange(new Rect(
                snapshot.X * finalSize.Width,
                snapshot.Y * diagramHeight,
                Math.Max(102d, snapshot.Width * finalSize.Width),
                Math.Max(108d, snapshot.Height * diagramHeight)));
        }

        var legendTop = diagramHeight + 12d;
        _connectionLegendHeading.Arrange(new Rect(4d, legendTop, finalSize.Width - 8d, 22d));
        var gap = 8d;
        var itemWidth = (finalSize.Width - ((columns - 1) * gap)) / columns;
        for (var index = 0; index < _connectionLabels.Count; index++)
        {
            var (_, label) = _connectionLabels[index];
            var row = index / columns;
            var column = index % columns;
            label.Arrange(new Rect(
                column * (itemWidth + gap),
                legendTop + 28d + (row * 58d),
                itemWidth,
                52d));
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
        Children.Add(_connectionLegendHeading);

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
            Width = 34d,
            Height = 28d,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var heading = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("34,*"),
            ColumnSpacing = 7d,
        };
        heading.Children.Add(glyph);
        var headingText = new StackPanel { Spacing = 0d };
        headingText.Children.Add(new TextBlock
        {
            Text = node.DisplayName,
            FontSize = 11.5d,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        });
        headingText.Children.Add(new TextBlock
        {
            Text = node.StatusText,
            FontSize = 9.5d,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.Accent(node.State),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        });
        Grid.SetColumn(headingText, 1);
        heading.Children.Add(headingText);

        var stack = new StackPanel { Spacing = 3d };
        stack.Children.Add(heading);
        stack.Children.Add(Mono(node.PrimaryText, 10.5d, Brushes.White));
        stack.Children.Add(Mono(node.SecondaryText, 9.5d, ControlRoomPalette.TextMuted));

        var ports = new StackPanel { Spacing = 1d };
        ports.Children.Add(new TextBlock
        {
            Text = $"IN  ‹ {NormalizePortText(node.InputText, "IN")}",
            FontSize = 9.5d,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.InformationAccent,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        });
        var output = new TextBlock
        {
            Text = $"{NormalizePortText(node.OutputText, "OUT")} ›  OUT",
            FontSize = 9.5d,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.InformationAccent,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };
        ports.Children.Add(output);
        stack.Children.Add(ports);

        return new Border
        {
            Padding = new Thickness(8d, 7d),
            CornerRadius = new CornerRadius(4d),
            Background = Brush.Parse("#F20C1820"),
            BorderBrush = ControlRoomPalette.Accent(node.State),
            BorderThickness = new Thickness(1.4d),
            Child = stack,
            ClipToBounds = true,
            IsHitTestVisible = false,
        };
    }

    private static Border BuildConnectionLabel(ControlRoomSubsystemSchematicConnectionSnapshot connection)
    {
        var colorKey = new Border
        {
            Width = 5d,
            CornerRadius = new CornerRadius(2d),
            Background = ConnectionBrush(connection.Kind, connection.State),
        };
        var stack = new StackPanel { Spacing = 1d };
        stack.Children.Add(new TextBlock
        {
            Text = connection.Label,
            FontSize = 10d,
            FontWeight = FontWeight.Bold,
            Foreground = ConnectionBrush(connection.Kind, connection.State),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        });
        stack.Children.Add(Mono($"{connection.PrimaryText}  ·  {connection.SecondaryText}", 9.5d, Brushes.White));

        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("5,*"),
            ColumnSpacing = 7d,
        };
        layout.Children.Add(colorKey);
        Grid.SetColumn(stack, 1);
        layout.Children.Add(stack);

        return new Border
        {
            IsHitTestVisible = false,
            Padding = new Thickness(8d, 6d),
            CornerRadius = new CornerRadius(4d),
            Background = Brush.Parse("#E6101C24"),
            BorderBrush = ControlRoomPalette.Border,
            BorderThickness = new Thickness(1d),
            Child = layout,
        };
    }

    private static TextBlock Mono(string text, double size, IBrush brush) => new()
    {
        Text = text,
        FontSize = size,
        FontFamily = ControlRoomTypography.DataFont,
        Foreground = brush,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxLines = 1,
    };

    private static string NormalizePortText(string text, string prefix)
    {
        var value = text?.Trim() ?? string.Empty;
        foreach (var separator in new[] { " · ", ":", " " })
        {
            var marker = prefix + separator;
            if (value.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                return value[marker.Length..].Trim();
            }
        }

        return value;
    }

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

            var gridPen = new Pen(Brush.Parse("#162A3440"), 1d);
            for (var x = 0d; x < Bounds.Width; x += 80d)
            {
                context.DrawLine(gridPen, new Point(x, 0d), new Point(x, Bounds.Height));
            }
            for (var y = 0d; y < Bounds.Height; y += 64d)
            {
                context.DrawLine(gridPen, new Point(0d, y), new Point(Bounds.Width, y));
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
