using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>
/// Interactive whole-plant engineering schematic. Layout/topology/value semantics arrive from the Application HMI snapshot;
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
    private readonly TextBlock _connectionLegendHeading = new()
    {
        Text = "LINES & SIGNALS · LIVE VALUES",
        FontSize = 10d,
        FontWeight = FontWeight.Bold,
        Foreground = ControlRoomPalette.InformationAccent,
        LetterSpacing = 1d,
        IsHitTestVisible = false,
    };

    public ControlRoomPlantMimicControl()
    {
        ClipToBounds = true;
        MinHeight = 620d;
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
        var width = double.IsInfinity(availableSize.Width) ? 1120d : Math.Max(780d, availableSize.Width);
        var columns = Math.Max(2, (int)(width / 205d));
        var rows = Math.Max(1, (int)Math.Ceiling(_connectionLabels.Count / (double)columns));
        var legendHeight = 38d + (rows * 58d);
        var desired = new Size(width, Math.Max(620d, 450d + legendHeight));

        _connectionLayer.Measure(new Size(desired.Width, 450d));
        foreach (var (_, card) in _elementCards)
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

        foreach (var (snapshot, card) in _elementCards)
        {
            var width = Math.Max(102d, snapshot.Width * finalSize.Width);
            var height = Math.Max(116d, snapshot.Height * diagramHeight);
            card.Arrange(new Rect(
                Math.Min(snapshot.X * finalSize.Width, Math.Max(0d, finalSize.Width - width)),
                Math.Min(snapshot.Y * diagramHeight, Math.Max(0d, diagramHeight - height)),
                width,
                height));
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
        _elementCards.Clear();
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
            Text = element.DisplayName,
            FontSize = 11.5d,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        });
        headingText.Children.Add(new TextBlock
        {
            Text = element.StatusText,
            FontSize = 9.5d,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.Accent(element.State),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        });
        Grid.SetColumn(headingText, 1);
        heading.Children.Add(headingText);

        var ports = new StackPanel { Spacing = 1d };
        ports.Children.Add(Port(element.InputText));
        ports.Children.Add(Port(element.OutputText));

        var content = new StackPanel
        {
            Spacing = 3d,
            Children =
            {
                heading,
                Mono(element.PrimaryValueText, 10.5d, Brushes.White),
                Mono(element.SecondaryValueText, 9.5d, ControlRoomPalette.TextMuted),
                ports,
            },
        };

        var border = new Border
        {
            Padding = new Thickness(8d, 7d),
            CornerRadius = new CornerRadius(4d),
            Background = Brush.Parse("#F20C1820"),
            BorderBrush = ControlRoomPalette.Accent(element.State),
            BorderThickness = new Thickness(1.4d),
            Child = content,
            Focusable = true,
            ClipToBounds = true,
        };

        border.GotFocus += (_, _) => SetCurrentValue(SelectedElementIdProperty, element.ElementId);
        border.PointerPressed += (_, _) => SetCurrentValue(SelectedElementIdProperty, element.ElementId);
        return border;
    }

    private static Border BuildConnectionLabel(ControlRoomPlantMimicConnectionSnapshot connection)
    {
        var brush = connection.State == ControlRoomVisualState.Trip
            ? ControlRoomPalette.Accent(ControlRoomVisualState.Trip)
            : MediumBrush(connection.Medium);
        var colorKey = new Border
        {
            Width = 5d,
            CornerRadius = new CornerRadius(2d),
            Background = brush,
        };
        var stack = new StackPanel { Spacing = 1d };
        stack.Children.Add(new TextBlock
        {
            Text = connection.MediumText,
            FontSize = 10d,
            FontWeight = FontWeight.Bold,
            Foreground = brush,
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

    private void RefreshSelection()
    {
        foreach (var (snapshot, card) in _elementCards)
        {
            var selected = string.Equals(snapshot.ElementId, SelectedElementId, StringComparison.Ordinal);
            card.BorderBrush = selected ? ControlRoomPalette.InformationAccentStrong : ControlRoomPalette.Accent(snapshot.State);
            card.BorderThickness = selected ? new Thickness(2.4d) : new Thickness(1.4d);
            card.Opacity = selected || string.IsNullOrEmpty(SelectedElementId) ? 1d : 0.72d;
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
        FontFamily = ControlRoomTypography.DataFont,
        Foreground = brush,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxLines = 1,
    };

    private static TextBlock Port(string text) => new()
    {
        Text = text,
        FontSize = 9.5d,
        FontWeight = FontWeight.SemiBold,
        Foreground = ControlRoomPalette.InformationAccent,
        TextWrapping = TextWrapping.NoWrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxLines = 1,
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
