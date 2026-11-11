using Microsoft.Win32;

namespace ScreenCut.Core;

/// <summary>Per-user Windows integration: autostart and the Print Screen key mapping. No admin rights needed.</summary>
internal static class SystemIntegration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string KeyboardKeyPath = @"Control Panel\Keyboard";
    private const string SnippingValueName = "PrintScreenKeyForSnippingEnabled";
    public const string AutostartArgument = "--autostart";

    public static void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled && Environment.ProcessPath is { } exePath)
        {
            key.SetValue(AppInfo.Name, $"\"{exePath}\" {AutostartArgument}");
        }
        else
        {
            key.DeleteValue(AppInfo.Name, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// Turns off Windows 11's "Use the Print screen key to open screen capture" so the key is ours.
    /// Our keyboard hook already swallows the key; this keeps Windows' setting consistent with that.
    /// </summary>
    public static void DisableSnippingToolPrintScreen()
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyboardKeyPath);
        if (key.GetValue(SnippingValueName) is not int current || current != 0)
        {
            key.SetValue(SnippingValueName, 0, RegistryValueKind.DWord);
        }
    }

    /// <summary>Restores Windows defaults; used by <c>ScreenCut.exe --uninstall</c>.</summary>
    public static void Uninstall()
    {
        SetStartWithWindows(false);
        using var key = Registry.CurrentUser.CreateSubKey(KeyboardKeyPath);
        key.SetValue(SnippingValueName, 1, RegistryValueKind.DWord);
    }
}
