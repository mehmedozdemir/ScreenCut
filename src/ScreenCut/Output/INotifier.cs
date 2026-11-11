namespace ScreenCut.Output;

/// <summary>Shows short, non-blocking messages to the user (tray notifications).</summary>
internal interface INotifier
{
    void Info(string title, string message, Action? onClick = null);

    void Error(string title, string message);
}
