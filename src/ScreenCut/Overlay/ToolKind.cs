namespace ScreenCut.Overlay;

internal enum ToolKind
{
    Select,
    Rectangle,
    Ellipse,
    Line,
    Arrow,
    Pen,
    Highlighter,
    Text,
    Number,
    Pixelate,
}

internal enum OverlayPhase
{
    /// <summary>The user is choosing a region or window.</summary>
    Selecting,

    /// <summary>A region is fixed; annotation tools and the toolbar are active.</summary>
    Editing,
}

internal enum DragMode
{
    None,
    CreatingSelection,
    ResizingSelection,
    MovingSelection,
    DrawingAnnotation,
    MovingAnnotation,
}
