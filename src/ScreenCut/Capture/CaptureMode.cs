namespace ScreenCut.Capture;

/// <summary>What the overlay pre-selects when it opens.</summary>
internal enum CaptureMode
{
    /// <summary>PrtScn — free region or click-to-select a window.</summary>
    Region,

    /// <summary>Alt+PrtScn — the active window.</summary>
    ActiveWindow,

    /// <summary>Shift+PrtScn — the monitor under the mouse cursor.</summary>
    Screen,

    /// <summary>Ctrl+PrtScn — every monitor.</summary>
    AllScreens,
}
