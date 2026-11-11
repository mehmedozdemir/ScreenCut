using System.ComponentModel;
using System.Runtime.InteropServices;
using ScreenCut.Capture;

namespace ScreenCut.Native;

/// <summary>
/// Global low-level keyboard hook that intercepts the Print Screen key.
/// Runs on a dedicated thread with its own message loop so that a busy UI thread
/// can never make Windows time out (and silently remove) the hook.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private readonly ManualResetEventSlim _ready = new();
    private NativeMethods.LowLevelKeyboardProc? _callback;
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hook;
    private Exception? _startError;

    /// <summary>Raised on the hook thread. Handlers must marshal to the UI thread and return quickly.</summary>
    public event EventHandler<CaptureMode>? PrintScreenPressed;

    public void Start()
    {
        _thread = new Thread(RunMessageLoop) { IsBackground = true, Name = "ScreenCut.KeyboardHook" };
        _thread.Start();
        _ready.Wait();
        if (_startError is not null)
        {
            throw _startError;
        }
    }

    public void Dispose()
    {
        if (_threadId != 0)
        {
            NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread?.Join(TimeSpan.FromSeconds(1));
        }

        _ready.Dispose();
    }

    private void RunMessageLoop()
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        _callback = OnKeyboardEvent;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _callback, NativeMethods.GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
        {
            _startError = new Win32Exception(Marshal.GetLastWin32Error(), "Klavye kancası kurulamadı.");
            _ready.Set();
            return;
        }

        _ready.Set();
        while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }

        NativeMethods.UnhookWindowsHookEx(_hook);
    }

    private IntPtr OnKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            if (info.vkCode == NativeMethods.VK_SNAPSHOT && !IsDown(NativeMethods.VK_LWIN) && !IsDown(NativeMethods.VK_RWIN))
            {
                var message = (int)wParam;
                if (message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
                {
                    PrintScreenPressed?.Invoke(this, ResolveMode());
                }

                // Swallow both key-down and key-up so Windows' own handler never runs.
                return 1;
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static CaptureMode ResolveMode()
    {
        if (IsDown(NativeMethods.VK_MENU))
        {
            return CaptureMode.ActiveWindow;
        }

        if (IsDown(NativeMethods.VK_CONTROL))
        {
            return CaptureMode.AllScreens;
        }

        return IsDown(NativeMethods.VK_SHIFT) ? CaptureMode.Screen : CaptureMode.Region;
    }

    private static bool IsDown(int virtualKey) => (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
}
