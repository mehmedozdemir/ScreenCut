using System.Runtime.InteropServices;
using ScreenCut.Core;

namespace ScreenCut.Native;

/// <summary>Lists visible top-level windows (top of z-order first) for click-to-select.</summary>
internal static class WindowEnumerator
{
    private const int MinWindowSize = 8;
    private static readonly string[] IgnoredClasses = ["Progman", "WorkerW"];

    public static List<PixelRect> GetVisibleWindows()
    {
        var result = new List<PixelRect>();
        var ownProcessId = (uint)Environment.ProcessId;
        NativeMethods.EnumWindowsProc callback = (hwnd, _) =>
        {
            if (IsCandidate(hwnd, ownProcessId) && TryGetBounds(hwnd, out var bounds))
            {
                result.Add(bounds);
            }

            return true;
        };

        NativeMethods.EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return result;
    }

    public static PixelRect? GetForegroundWindowBounds()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || IsIgnoredClass(hwnd))
        {
            return null;
        }

        return TryGetBounds(hwnd, out var bounds) ? bounds : null;
    }

    private static bool IsCandidate(IntPtr hwnd, uint ownProcessId)
    {
        if (!NativeMethods.IsWindowVisible(hwnd) || NativeMethods.IsIconic(hwnd))
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == ownProcessId || IsCloaked(hwnd) || IsIgnoredClass(hwnd))
        {
            return false;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        return (exStyle & NativeMethods.WS_EX_TRANSPARENT) == 0;
    }

    private static bool IsCloaked(IntPtr hwnd)
    {
        var hr = NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int));
        return hr == 0 && cloaked != 0;
    }

    private static bool IsIgnoredClass(IntPtr hwnd)
    {
        var buffer = new char[256];
        var length = NativeMethods.GetClassName(hwnd, buffer, buffer.Length);
        var className = new string(buffer, 0, Math.Max(0, length));
        return IgnoredClasses.Contains(className, StringComparer.Ordinal);
    }

    private static bool TryGetBounds(IntPtr hwnd, out PixelRect bounds)
    {
        // Extended frame bounds exclude the invisible resize borders / drop shadow.
        var hr = NativeMethods.DwmGetWindowAttribute(
            hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out NativeMethods.RECT rect, Marshal.SizeOf<NativeMethods.RECT>());
        if (hr != 0 && !NativeMethods.GetWindowRect(hwnd, out rect))
        {
            bounds = default;
            return false;
        }

        bounds = new PixelRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        return bounds.Width >= MinWindowSize && bounds.Height >= MinWindowSize;
    }
}
