using System.Diagnostics;
using ScreenCut.Native;

namespace ScreenCut.Output;

internal enum MessagingApp
{
    WhatsApp,
    Teams,
}

/// <summary>Opens desktop messaging apps via their URI protocol, falling back to the web version.</summary>
internal static class AppLauncher
{
    public static string DisplayName(MessagingApp app) => app == MessagingApp.WhatsApp ? "WhatsApp" : "Teams";

    public static void Open(MessagingApp app)
    {
        var (protocol, webUrl) = app switch
        {
            MessagingApp.WhatsApp => ("whatsapp", "https://web.whatsapp.com"),
            MessagingApp.Teams => ("msteams", "https://teams.microsoft.com"),
            _ => throw new ArgumentOutOfRangeException(nameof(app), app, null),
        };

        OpenShell(IsProtocolRegistered(protocol) ? protocol + ":" : webUrl);
    }

    public static void OpenShell(string target) =>
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();

    public static void ShowInExplorer(string filePath) =>
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true })?.Dispose();

    private static bool IsProtocolRegistered(string protocol)
    {
        uint length = 0;
        NativeMethods.AssocQueryString(NativeMethods.ASSOCF_IS_PROTOCOL, NativeMethods.ASSOCSTR_FRIENDLYAPPNAME, protocol, null, null, ref length);
        return length > 1;
    }
}
