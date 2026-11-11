namespace ScreenCut.Native;

/// <summary>
/// Brings a window to the foreground even when the foreground lock would normally prevent it
/// (our process is not the one that received the last user input — the hook thread was).
/// </summary>
internal static class ForegroundHelper
{
    public static void ForceForeground(IntPtr hwnd)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var currentThread = NativeMethods.GetCurrentThreadId();
        var attached = foregroundThread != 0
            && foregroundThread != currentThread
            && NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }
}
