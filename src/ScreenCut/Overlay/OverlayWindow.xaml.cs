using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenCut.Annotations;
using ScreenCut.Capture;
using ScreenCut.Core;
using ScreenCut.Native;
using ScreenCut.Output;
using CaptureMode = ScreenCut.Capture.CaptureMode;

namespace ScreenCut.Overlay;

/// <summary>
/// Full-desktop, topmost window showing the frozen screenshot. Handles region/window selection,
/// annotation tools and the quick-action toolbar.
/// </summary>
/// <remarks>
/// Coordinates inside <c>Surface</c> are physical pixels of the snapshot (the canvas is scaled by 1/DPI),
/// while <c>ChromeCanvas</c> uses normal DIPs so toolbar and labels have their usual size.
/// </remarks>
internal partial class OverlayWindow : Window
{
    private static readonly double[] ThicknessLevels = [2, 4, 7];
    private static readonly double[] FontSizeLevels = [14, 20, 28];
    private static readonly double[] NumberRadiusLevels = [10, 13, 17];
    private const double DragThreshold = 4;
    private const double HandleSize = 8;
    private const double HitTolerance = 6;
    private const double ToolbarMargin = 8;
    private const double SelectionStroke = 1.5;
    private const double MinimumSelectionSize = 3;
    private const double PixelateBlockSize = 10;

    private static readonly Dictionary<Key, ToolKind> ToolShortcuts = new()
    {
        [Key.V] = ToolKind.Select,
        [Key.R] = ToolKind.Rectangle,
        [Key.E] = ToolKind.Ellipse,
        [Key.L] = ToolKind.Line,
        [Key.A] = ToolKind.Arrow,
        [Key.P] = ToolKind.Pen,
        [Key.H] = ToolKind.Highlighter,
        [Key.T] = ToolKind.Text,
        [Key.N] = ToolKind.Number,
        [Key.B] = ToolKind.Pixelate,
    };

    private readonly OutputActions _actions;
    private readonly AppSettings _settings;
    private readonly AnnotationDocument _document = new();
    private readonly Rectangle[] _handles = new Rectangle[8];

    private CaptureSnapshot _snapshot;
    private CaptureMode _pendingMode;
    private IntPtr _hwnd;
    private double _scale = 1;

    private OverlayPhase _phase;
    private Rect _selection = Rect.Empty;
    private Rect _hover = Rect.Empty;

    private ToolKind _tool;
    private Color _color;
    private int _sizeLevel;
    private bool _textBackground;

    private DragMode _drag;
    private Point _dragStart;
    private Point _lastPoint;
    private bool _hasDragged;
    private Rect _dragOriginSelection;
    private SelectionHandle _activeHandle;
    private Annotation? _draft;
    private Annotation? _selected;
    private bool _moveRecorded;

    private bool _isTextEditing;
    private TextAnnotation? _textOriginal;
    private Point _textPosition;
    private Color _textColor;
    private double _textFontSize;
    private bool _textHasBackground;

    public OverlayWindow(CaptureSnapshot snapshot, CaptureMode mode, OutputActions actions, AppSettings settings)
    {
        InitializeComponent();
        _snapshot = snapshot;
        _pendingMode = mode;
        _actions = actions;
        _settings = settings;

        Annotations.Document = _document;
        CreateHandles();
        RestoreStylePreferences();

        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) =>
        {
            ApplyWindowBounds();
            Refresh();
            ActivateOverlay();
        };
        Closed += (_, _) => ScreenImage.Source = null;
    }

    public event EventHandler? RecaptureRequested;

    private Rect SurfaceBounds => _snapshot.ImageBounds;

    /// <summary>Replaces the screenshot (used by "recapture") and resets the editor.</summary>
    public void LoadSnapshot(CaptureSnapshot snapshot, CaptureMode mode)
    {
        _snapshot = snapshot;
        _pendingMode = mode;
        if (_hwnd != IntPtr.Zero)
        {
            ApplyWindowBounds();
            UpdateScale();
        }

        ApplySnapshot();
    }

    public void ActivateOverlay()
    {
        if (_hwnd != IntPtr.Zero)
        {
            ForegroundHelper.ForceForeground(_hwnd);
        }

        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    // ───────────────────────── Window setup ─────────────────────────

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
        ApplyWindowBounds();
        UpdateScale();
        ApplySnapshot();
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // The overlay spans every monitor. Ignore per-monitor DPI changes so WPF never resizes it;
        // all geometry is in physical pixels anyway.
        if (msg == NativeMethods.WM_DPICHANGED)
        {
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ApplyWindowBounds()
    {
        var bounds = _snapshot.VirtualBounds;
        NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, bounds.X, bounds.Y, bounds.Width, bounds.Height, NativeMethods.SWP_NOACTIVATE);
    }

    private void UpdateScale()
    {
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        _scale = dpi > 0 ? dpi : 1;
        SurfaceScale.ScaleX = 1 / _scale;
        SurfaceScale.ScaleY = 1 / _scale;
        Annotations.ChromeScale = _scale;
        SelectionBorder.StrokeThickness = SelectionStroke * _scale;
    }

    private void ApplySnapshot()
    {
        var image = _snapshot.Image;
        ScreenImage.Source = image;
        foreach (var element in new FrameworkElement[] { ScreenImage, Surface, Annotations })
        {
            element.Width = image.PixelWidth;
            element.Height = image.PixelHeight;
        }

        CancelTextEdit();
        _document.Clear();
        _draft = null;
        _selected = null;
        _drag = DragMode.None;
        _selection = Rect.Empty;

        Rect? initial = _pendingMode switch
        {
            CaptureMode.ActiveWindow => (_snapshot.ForegroundWindow ?? _snapshot.MonitorAt(_snapshot.Cursor)).ToRect(),
            CaptureMode.Screen => _snapshot.MonitorAt(_snapshot.Cursor).ToRect(),
            CaptureMode.AllScreens => SurfaceBounds,
            _ => null,
        };

        if (initial is { } rect)
        {
            _selection = SelectionGeometry.Clamp(rect, SurfaceBounds);
            EnterEditing();
        }
        else
        {
            EnterSelecting(_snapshot.Cursor);
        }
    }

    private void CreateHandles()
    {
        for (var i = 0; i < _handles.Length; i++)
        {
            _handles[i] = new Rectangle { Fill = Brushes.White, Stroke = (Brush)FindResource("Accent") };
            HandlesCanvas.Children.Add(_handles[i]);
        }
    }

    private void RestoreStylePreferences()
    {
        _tool = Enum.TryParse<ToolKind>(_settings.LastTool, out var tool) ? tool : ToolKind.Rectangle;
        _color = ParseColor(_settings.LastColor) ?? Colors.Red;
        _sizeLevel = Math.Clamp(_settings.LastSizeLevel, 0, AppSettings.MaxSizeLevel);
        _textBackground = _settings.TextBackground;

        CheckByTag(_tool.ToString(), "Tool");
        CheckByTag(_settings.LastColor, "Color");
        CheckByTag(_sizeLevel.ToString(System.Globalization.CultureInfo.InvariantCulture), "Size");
        TextBackgroundToggle.IsChecked = _textBackground;
    }

    private void CheckByTag(string tag, string groupName)
    {
        foreach (var radio in FindRadioButtons(Toolbar))
        {
            if (radio.GroupName == groupName && string.Equals(radio.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                radio.IsChecked = true;
                return;
            }
        }
    }

    private static IEnumerable<RadioButton> FindRadioButtons(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is RadioButton radio)
            {
                yield return radio;
            }

            foreach (var nested in FindRadioButtons(child))
            {
                yield return nested;
            }
        }
    }

    // ───────────────────────── Phases ─────────────────────────

    private void EnterSelecting(Point cursor)
    {
        _phase = OverlayPhase.Selecting;
        _selection = Rect.Empty;
        _selected = null;
        _hover = _snapshot.WindowAt(cursor).ToRect();
        Surface.Cursor = Cursors.Cross;
        Refresh();
    }

    private void EnterEditing()
    {
        _phase = OverlayPhase.Editing;
        _hover = Rect.Empty;
        _selection = SelectionGeometry.Snap(_selection);
        UpdateEditingCursor(Mouse.GetPosition(Surface));
        Refresh();
    }

    // ───────────────────────── Mouse ─────────────────────────

    private void OnSurfaceMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var point = SelectionGeometry.Clamp(e.GetPosition(Surface), SurfaceBounds);

        if (_isTextEditing)
        {
            var wasTextTool = _tool == ToolKind.Text;
            CommitTextEdit();
            if (wasTextTool)
            {
                return;
            }
        }

        _dragStart = point;
        _lastPoint = point;
        _hasDragged = false;

        if (_phase == OverlayPhase.Selecting)
        {
            _drag = DragMode.CreatingSelection;
        }
        else if (e.ClickCount == 2 && HitTestAnnotation(point) is TextAnnotation text)
        {
            BeginTextEdit(text.Position, text);
            return;
        }
        else
        {
            BeginEditingInteraction(point);
        }

        if (_drag != DragMode.None)
        {
            Surface.CaptureMouse();
        }

        Refresh();
    }

    private void BeginEditingInteraction(Point point)
    {
        var handle = SelectionGeometry.HitTestHandle(_selection, point, HandleSize * _scale);
        if (handle != SelectionHandle.None)
        {
            _drag = DragMode.ResizingSelection;
            _activeHandle = handle;
            _dragOriginSelection = _selection;
            return;
        }

        switch (_tool)
        {
            case ToolKind.Select:
                _selected = HitTestAnnotation(point);
                if (_selected is not null)
                {
                    _drag = DragMode.MovingAnnotation;
                    _moveRecorded = false;
                }
                else if (_selection.Contains(point))
                {
                    _drag = DragMode.MovingSelection;
                    _dragOriginSelection = _selection;
                }

                break;
            case ToolKind.Text:
                BeginTextEdit(point, HitTestAnnotation(point) as TextAnnotation);
                break;
            case ToolKind.Number:
                _document.Add(new NumberAnnotation(point, _document.NextNumber, _color, NumberRadiusLevels[_sizeLevel] * _scale));
                break;
            default:
                _selected = null;
                _draft = CreateDraft(point);
                _drag = DragMode.DrawingAnnotation;
                break;
        }
    }

    private Annotation CreateDraft(Point point)
    {
        var thickness = ThicknessLevels[_sizeLevel] * _scale;
        return _tool switch
        {
            ToolKind.Rectangle => new ShapeAnnotation(ShapeKind.Rectangle, point, _color, thickness),
            ToolKind.Ellipse => new ShapeAnnotation(ShapeKind.Ellipse, point, _color, thickness),
            ToolKind.Line => new ShapeAnnotation(ShapeKind.Line, point, _color, thickness),
            ToolKind.Arrow => new ShapeAnnotation(ShapeKind.Arrow, point, _color, thickness),
            ToolKind.Pen => new StrokeAnnotation(point, _color, thickness, isHighlighter: false),
            ToolKind.Highlighter => new StrokeAnnotation(point, _color, thickness, isHighlighter: true),
            ToolKind.Pixelate => new PixelateAnnotation(point, Pixelate),
            _ => throw new InvalidOperationException($"Çizim aracı değil: {_tool}"),
        };
    }

    private PixelatedImage? Pixelate(Rect area) =>
        SelectionRenderer.Pixelate(_snapshot.Image, area, (int)Math.Max(4, Math.Round(PixelateBlockSize * _scale)));

    private void OnSurfaceMouseMove(object sender, MouseEventArgs e)
    {
        var point = SelectionGeometry.Clamp(e.GetPosition(Surface), SurfaceBounds);
        if (_drag == DragMode.None)
        {
            UpdateHover(point);
            return;
        }

        if (!_hasDragged && (point - _dragStart).Length < DragThreshold)
        {
            return;
        }

        _hasDragged = true;
        var constrain = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        switch (_drag)
        {
            case DragMode.CreatingSelection:
                _selection = SelectionGeometry.Clamp(SelectionGeometry.FromPoints(_dragStart, point, constrain), SurfaceBounds);
                break;
            case DragMode.ResizingSelection:
                _selection = SelectionGeometry.Clamp(SelectionGeometry.Resize(_dragOriginSelection, _activeHandle, point - _dragStart), SurfaceBounds);
                break;
            case DragMode.MovingSelection:
                _selection = SelectionGeometry.Move(_dragOriginSelection, point - _dragStart, SurfaceBounds);
                break;
            case DragMode.DrawingAnnotation:
                _draft?.UpdateDrag(_dragStart, point, constrain);
                break;
            case DragMode.MovingAnnotation when _selected is not null:
                if (!_moveRecorded)
                {
                    _document.RecordChange();
                    _moveRecorded = true;
                }

                _selected.Offset(point - _lastPoint);
                break;
            case DragMode.None:
            default:
                break;
        }

        _lastPoint = point;
        Refresh();
    }

    private void OnSurfaceMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_drag == DragMode.None)
        {
            return;
        }

        var point = SelectionGeometry.Clamp(e.GetPosition(Surface), SurfaceBounds);
        var drag = _drag;
        _drag = DragMode.None;
        Surface.ReleaseMouseCapture();

        switch (drag)
        {
            case DragMode.CreatingSelection:
                if (!_hasDragged)
                {
                    _selection = _snapshot.WindowAt(point).ToRect();
                }

                if (_selection.Width >= MinimumSelectionSize && _selection.Height >= MinimumSelectionSize)
                {
                    EnterEditing();
                    return;
                }

                _selection = Rect.Empty;
                break;
            case DragMode.DrawingAnnotation:
                if (_draft is { IsMeaningful: true } draft)
                {
                    (draft as PixelateAnnotation)?.Complete();
                    _document.Add(draft);
                }

                _draft = null;
                break;
            case DragMode.MovingAnnotation:
                (_selected as PixelateAnnotation)?.Complete();
                break;
            case DragMode.ResizingSelection:
            case DragMode.MovingSelection:
                _selection = SelectionGeometry.Snap(_selection);
                break;
            case DragMode.None:
            default:
                break;
        }

        Refresh();
    }

    private void OnSurfaceMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_phase == OverlayPhase.Selecting && _drag == DragMode.None)
        {
            Close();
        }
    }

    private void OnSurfaceLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_drag == DragMode.None)
        {
            return;
        }

        // Capture lost unexpectedly (e.g. Alt+Tab): drop any half-finished interaction.
        _drag = DragMode.None;
        _draft = null;
        Refresh();
    }

    private void UpdateHover(Point point)
    {
        if (_phase == OverlayPhase.Selecting)
        {
            var hover = _snapshot.WindowAt(point).ToRect();
            if (hover != _hover)
            {
                _hover = hover;
                Refresh();
            }

            return;
        }

        UpdateEditingCursor(point);
    }

    private void UpdateEditingCursor(Point point)
    {
        Surface.Cursor = SelectionGeometry.HitTestHandle(_selection, point, HandleSize * _scale) switch
        {
            SelectionHandle.TopLeft or SelectionHandle.BottomRight => Cursors.SizeNWSE,
            SelectionHandle.TopRight or SelectionHandle.BottomLeft => Cursors.SizeNESW,
            SelectionHandle.Top or SelectionHandle.Bottom => Cursors.SizeNS,
            SelectionHandle.Left or SelectionHandle.Right => Cursors.SizeWE,
            _ => _tool switch
            {
                ToolKind.Select => HitTestAnnotation(point) is not null || _selection.Contains(point) ? Cursors.SizeAll : Cursors.Arrow,
                ToolKind.Text => Cursors.IBeam,
                _ => Cursors.Cross,
            },
        };
    }

    private Annotation? HitTestAnnotation(Point point)
    {
        var items = _document.Items;
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].HitTest(point, HitTolerance * _scale))
            {
                return items[i];
            }
        }

        return null;
    }

    // ───────────────────────── Text editing ─────────────────────────

    private void BeginTextEdit(Point position, TextAnnotation? existing)
    {
        if (existing is not null)
        {
            _document.RecordChange();
            _document.RemoveWithoutRecording(existing);
            _textPosition = existing.Position;
            _textColor = existing.Color;
            _textFontSize = existing.FontSize;
            _textHasBackground = existing.HasBackground;
            TextEditor.Text = existing.Text;
        }
        else
        {
            _textPosition = position;
            _textColor = _color;
            _textFontSize = FontSizeLevels[_sizeLevel] * _scale;
            _textHasBackground = _textBackground;
            TextEditor.Text = string.Empty;
        }

        _textOriginal = existing;
        _isTextEditing = true;
        _selected = null;
        StyleTextEditor();
        TextEditor.Visibility = Visibility.Visible;
        TextEditor.Focus();
        TextEditor.CaretIndex = TextEditor.Text.Length;
        Refresh();
    }

    private void StyleTextEditor()
    {
        var padding = _textHasBackground ? _textFontSize * TextAnnotation.PaddingFactor : 0;
        TextEditor.FontFamily = TextAnnotation.FontFamily;
        TextEditor.FontWeight = TextAnnotation.FontWeight;
        TextEditor.FontSize = _textFontSize;
        TextEditor.Foreground = new SolidColorBrush(_textColor);
        TextEditor.CaretBrush = TextEditor.Foreground;
        TextEditor.Background = _textHasBackground ? new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF)) : new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
        TextEditor.BorderThickness = new Thickness(1 * _scale);
        TextEditor.Padding = new Thickness(padding);
        TextEditor.MinWidth = _textFontSize * 2;
        Canvas.SetLeft(TextEditor, _textPosition.X);
        Canvas.SetTop(TextEditor, _textPosition.Y);
    }

    private void CommitTextEdit()
    {
        if (!_isTextEditing)
        {
            return;
        }

        var text = TextEditor.Text.TrimEnd();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var annotation = new TextAnnotation(_textPosition, text, _textColor, _textFontSize, _textHasBackground);
            if (_textOriginal is null)
            {
                _document.Add(annotation);
            }
            else
            {
                _document.AddWithoutRecording(annotation);
            }
        }

        EndTextEdit();
    }

    private void CancelTextEdit()
    {
        if (!_isTextEditing)
        {
            return;
        }

        if (_textOriginal is not null)
        {
            _document.CancelChange();
        }

        EndTextEdit();
    }

    private void EndTextEdit()
    {
        _isTextEditing = false;
        _textOriginal = null;
        TextEditor.Visibility = Visibility.Collapsed;
        Keyboard.Focus(this);
        Refresh();
    }

    // ───────────────────────── Keyboard ─────────────────────────

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isTextEditing)
        {
            HandleTextEditorKey(e);
            return;
        }

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        e.Handled = _phase == OverlayPhase.Selecting ? HandleSelectingKey(e.Key) : HandleEditingKey(e.Key, ctrl, shift);
    }

    private void HandleTextEditorKey(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelTextEdit();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            CommitTextEdit();
            e.Handled = true;
        }
    }

    private bool HandleSelectingKey(Key key)
    {
        switch (key)
        {
            case Key.Escape:
                Close();
                return true;
            case Key.F:
                _selection = _snapshot.MonitorAt(Mouse.GetPosition(Surface)).ToRect();
                EnterEditing();
                return true;
            case Key.Enter when !_hover.IsEmpty:
                _selection = _hover;
                EnterEditing();
                return true;
            default:
                return false;
        }
    }

    private bool HandleEditingKey(Key key, bool ctrl, bool shift)
    {
        switch (key)
        {
            case Key.Escape:
                Close();
                return true;
            case Key.Enter:
            case Key.C when ctrl:
                Copy();
                return true;
            case Key.S when ctrl && shift:
                SaveAs();
                return true;
            case Key.S when ctrl:
                QuickSave();
                return true;
            case Key.Z when ctrl && shift:
            case Key.Y when ctrl:
                Redo();
                return true;
            case Key.Z when ctrl:
                Undo();
                return true;
            case Key.R when ctrl:
                Reselect();
                return true;
            case Key.F5:
                RecaptureRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Key.Delete:
            case Key.Back:
                DeleteSelected();
                return true;
            default:
                if (!ctrl && ToolShortcuts.TryGetValue(key, out var tool))
                {
                    CheckByTag(tool.ToString(), "Tool");
                    return true;
                }

                return false;
        }
    }

    // ───────────────────────── Toolbar ─────────────────────────

    private void OnToolChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string tag } || !Enum.TryParse<ToolKind>(tag, out var tool))
        {
            return;
        }

        CommitTextEdit();
        _tool = tool;
        _settings.LastTool = tool.ToString();
        if (tool != ToolKind.Select)
        {
            _selected = null;
        }

        if (IsLoaded)
        {
            UpdateEditingCursor(Mouse.GetPosition(Surface));
            Refresh();
        }
    }

    private void OnColorChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string tag } || ParseColor(tag) is not { } color)
        {
            return;
        }

        _color = color;
        _settings.LastColor = tag;

        if (_isTextEditing)
        {
            _textColor = color;
            StyleTextEditor();
        }
        else if (_selected is { SupportsColor: true } selected)
        {
            _document.RecordChange();
            selected.SetColor(color);
            Refresh();
        }
    }

    private void OnSizeChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && int.TryParse(tag, System.Globalization.CultureInfo.InvariantCulture, out var level))
        {
            _sizeLevel = Math.Clamp(level, 0, AppSettings.MaxSizeLevel);
            _settings.LastSizeLevel = _sizeLevel;
        }
    }

    private void OnTextBackgroundChanged(object sender, RoutedEventArgs e)
    {
        _textBackground = TextBackgroundToggle.IsChecked == true;
        _settings.TextBackground = _textBackground;
        if (_isTextEditing)
        {
            _textHasBackground = _textBackground;
            StyleTextEditor();
        }
    }

    private void OnUndoClick(object sender, RoutedEventArgs e) => Undo();

    private void OnRedoClick(object sender, RoutedEventArgs e) => Redo();

    private void OnReselectClick(object sender, RoutedEventArgs e) => Reselect();

    private void OnRecaptureClick(object sender, RoutedEventArgs e) => RecaptureRequested?.Invoke(this, EventArgs.Empty);

    private void OnCopyClick(object sender, RoutedEventArgs e) => Copy();

    private void OnQuickSaveClick(object sender, RoutedEventArgs e) => QuickSave();

    private void OnSaveAsClick(object sender, RoutedEventArgs e) => SaveAs();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnShareClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = ShareButton, Placement = PlacementMode.Bottom };
        menu.Items.Add(CreateMenuItem("Windows ile paylaş…", () => RunAfterClose(_actions.ShareWithWindows)));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("WhatsApp'a gönder", () => RunAfterClose(image => _actions.SendTo(MessagingApp.WhatsApp, image))));
        menu.Items.Add(CreateMenuItem("Teams'e gönder", () => RunAfterClose(image => _actions.SendTo(MessagingApp.Teams, image))));
        menu.Items.Add(CreateMenuItem("E-posta ile gönder", () => RunAfterClose(_actions.Email)));
        menu.IsOpen = true;
    }

    private static MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    // ───────────────────────── Commands ─────────────────────────

    private void Undo()
    {
        CommitTextEdit();
        if (_document.Undo())
        {
            _selected = null;
            Refresh();
        }
    }

    private void Redo()
    {
        CommitTextEdit();
        if (_document.Redo())
        {
            _selected = null;
            Refresh();
        }
    }

    private void DeleteSelected()
    {
        if (_selected is null)
        {
            return;
        }

        _document.Remove(_selected);
        _selected = null;
        Refresh();
    }

    /// <summary>Go back to region selection on the same frozen screenshot. Annotations are kept.</summary>
    private void Reselect()
    {
        CommitTextEdit();
        EnterSelecting(Mouse.GetPosition(Surface));
    }

    private void Copy() => RunAndClose(image =>
    {
        _actions.CopyImage(image);
        return true;
    });

    private void QuickSave() => RunAndClose(image =>
    {
        _actions.QuickSave(image);
        return true;
    });

    private void SaveAs() => RunAndClose(image => _actions.SaveAs(image, this));

    private BitmapSource? RenderResult()
    {
        if (_phase != OverlayPhase.Editing)
        {
            return null;
        }

        CommitTextEdit();
        var area = PixelRect.FromRect(SelectionGeometry.Clamp(_selection, SurfaceBounds));
        return area.IsEmpty ? null : SelectionRenderer.Render(_snapshot.Image, area, _document.Items);
    }

    /// <summary>Runs an action on the result; closes the overlay if it returns true.</summary>
    private void RunAndClose(Func<BitmapSource, bool> action)
    {
        try
        {
            if (RenderResult() is { } image && action(image))
            {
                Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Çıktı işlemi başarısız.");
            _actions.Notifier.Error("İşlem başarısız", ex.Message);
        }
    }

    /// <summary>Closes the overlay first, then runs the action (for UI that must appear above everything).</summary>
    private void RunAfterClose(Action<BitmapSource> action)
    {
        BitmapSource? image;
        try
        {
            image = RenderResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Görüntü oluşturulamadı.");
            _actions.Notifier.Error("İşlem başarısız", ex.Message);
            return;
        }

        if (image is null)
        {
            return;
        }

        var dispatcher = Dispatcher;
        var notifier = _actions.Notifier;
        Close();
        dispatcher.BeginInvoke(() =>
        {
            try
            {
                action(image);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Paylaşım başarısız.");
                notifier.Error("Paylaşım başarısız", ex.Message);
            }
        });
    }

    // ───────────────────────── Rendering ─────────────────────────

    private void Refresh()
    {
        var display = _phase == OverlayPhase.Editing || (_drag == DragMode.CreatingSelection && _hasDragged) ? _selection : _hover;

        var dim = new GeometryGroup { FillRule = FillRule.EvenOdd };
        dim.Children.Add(new RectangleGeometry(SurfaceBounds));
        if (!display.IsEmpty)
        {
            dim.Children.Add(new RectangleGeometry(display));
        }

        DimPath.Data = dim;
        UpdateSelectionBorder(display);
        UpdateHandles(_phase == OverlayPhase.Editing ? display : Rect.Empty);
        UpdateSizeLabel(display);
        UpdateHintBanner();
        UpdateToolbar(display);

        Annotations.Draft = _draft;
        Annotations.Selected = _selected;
        Annotations.InvalidateVisual();
    }

    private void UpdateSelectionBorder(Rect display)
    {
        if (display.IsEmpty)
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            return;
        }

        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, display.X);
        Canvas.SetTop(SelectionBorder, display.Y);
        SelectionBorder.Width = display.Width;
        SelectionBorder.Height = display.Height;
    }

    private void UpdateHandles(Rect rect)
    {
        var size = HandleSize * _scale;
        var positions = rect.IsEmpty ? [] : SelectionGeometry.GetHandlePositions(rect);
        for (var i = 0; i < _handles.Length; i++)
        {
            var handle = _handles[i];
            if (i >= positions.Count)
            {
                handle.Visibility = Visibility.Collapsed;
                continue;
            }

            handle.Visibility = Visibility.Visible;
            handle.Width = size;
            handle.Height = size;
            handle.StrokeThickness = _scale;
            Canvas.SetLeft(handle, positions[i].Position.X - (size / 2));
            Canvas.SetTop(handle, positions[i].Position.Y - (size / 2));
        }
    }

    private void UpdateSizeLabel(Rect display)
    {
        if (display.IsEmpty)
        {
            SizeLabel.Visibility = Visibility.Collapsed;
            return;
        }

        SizeText.Text = $"{Math.Round(display.Width)} × {Math.Round(display.Height)}";
        SizeLabel.Visibility = Visibility.Visible;
        SizeLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var dip = ToDip(display);
        var top = dip.Top - SizeLabel.DesiredSize.Height - 4;
        Canvas.SetLeft(SizeLabel, dip.Left);
        Canvas.SetTop(SizeLabel, top < 0 ? dip.Top + 4 : top);
    }

    private void UpdateHintBanner()
    {
        if (_phase != OverlayPhase.Selecting || _drag == DragMode.CreatingSelection)
        {
            HintBanner.Visibility = Visibility.Collapsed;
            return;
        }

        HintBanner.Visibility = Visibility.Visible;
        HintBanner.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var monitor = ToDip(_snapshot.MonitorAt(_snapshot.Cursor).ToRect());
        Canvas.SetLeft(HintBanner, monitor.Left + ((monitor.Width - HintBanner.DesiredSize.Width) / 2));
        Canvas.SetTop(HintBanner, monitor.Top + 24);
    }

    private void UpdateToolbar(Rect display)
    {
        var interacting = _drag is DragMode.ResizingSelection or DragMode.MovingSelection;
        if (_phase != OverlayPhase.Editing || display.IsEmpty || interacting)
        {
            Toolbar.Visibility = Visibility.Collapsed;
            return;
        }

        UndoButton.IsEnabled = _document.CanUndo;
        RedoButton.IsEnabled = _document.CanRedo;
        Toolbar.Visibility = Visibility.Visible;
        Toolbar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var center = new Point(display.X + (display.Width / 2), display.Y + (display.Height / 2));
        var monitor = ToDip(_snapshot.MonitorAt(center).ToRect());
        var position = ToolbarPlacement.Compute(ToDip(display), Toolbar.DesiredSize, monitor, ToolbarMargin);
        Canvas.SetLeft(Toolbar, position.X);
        Canvas.SetTop(Toolbar, position.Y);
    }

    private Rect ToDip(Rect physical) =>
        new(physical.X / _scale, physical.Y / _scale, physical.Width / _scale, physical.Height / _scale);

    private static Color? ParseColor(string value)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(value);
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException)
        {
            return null;
        }
    }
}
