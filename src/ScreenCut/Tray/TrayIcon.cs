using ScreenCut.Core;
using ScreenCut.Output;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ScreenCut.Tray;

/// <summary>Commands exposed through the tray icon menu.</summary>
internal sealed record TrayActions(
    Action CaptureRegion,
    Action CaptureWindow,
    Action CaptureScreen,
    Action CaptureAllScreens,
    Action CaptureDelayed,
    Action OpenSaveFolder,
    Action OpenSettings,
    Action OpenReleases,
    Action Exit);

/// <summary>Notification-area icon: left click captures, right click opens the menu.</summary>
internal sealed class TrayIcon : INotifier, IDisposable
{
    private const int BalloonTimeoutMs = 4000;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Drawing.Icon _icon;
    private Action? _balloonAction;

    public TrayIcon(TrayActions actions)
    {
        _icon = LoadIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = $"{AppInfo.Name} — yakalamak için PrtScn",
            ContextMenuStrip = BuildMenu(actions),
            Visible = true,
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                actions.CaptureRegion();
            }
        };
        _notifyIcon.BalloonTipClicked += (_, _) => RunBalloonAction();
        _notifyIcon.BalloonTipClosed += (_, _) => _balloonAction = null;
    }

    public void Info(string title, string message, Action? onClick = null) => ShowBalloon(title, message, Forms.ToolTipIcon.Info, onClick);

    public void Error(string title, string message) => ShowBalloon(title, message, Forms.ToolTipIcon.Error, null);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private static Forms.ContextMenuStrip BuildMenu(TrayActions actions)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(CreateItem("Alan yakala", "PrtScn", actions.CaptureRegion, bold: true));
        menu.Items.Add(CreateItem("Etkin pencereyi yakala", "Alt+PrtScn", actions.CaptureWindow));
        menu.Items.Add(CreateItem("Bu ekranı yakala", "Shift+PrtScn", actions.CaptureScreen));
        menu.Items.Add(CreateItem("Tüm ekranları yakala", "Ctrl+PrtScn", actions.CaptureAllScreens));
        menu.Items.Add(CreateItem("3 saniye sonra yakala", null, actions.CaptureDelayed));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(CreateItem("Kayıt klasörünü aç", null, actions.OpenSaveFolder));
        menu.Items.Add(CreateItem("Ayarlar…", null, actions.OpenSettings));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(CreateItem($"Sürüm {AppInfo.Version} — sürüm notları", null, actions.OpenReleases));
        menu.Items.Add(CreateItem("Çıkış", null, actions.Exit));
        return menu;
    }

    private static Forms.ToolStripMenuItem CreateItem(string text, string? shortcut, Action action, bool bold = false)
    {
        var item = new Forms.ToolStripMenuItem(text) { ShortcutKeyDisplayString = shortcut };
        if (bold)
        {
            item.Font = new Drawing.Font(item.Font, Drawing.FontStyle.Bold);
        }

        item.Click += (_, _) => action();
        return item;
    }

    private static Drawing.Icon LoadIcon()
    {
        var exePath = Environment.ProcessPath;
        return (exePath is not null ? Drawing.Icon.ExtractAssociatedIcon(exePath) : null) ?? (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
    }

    private void ShowBalloon(string title, string message, Forms.ToolTipIcon icon, Action? onClick)
    {
        _balloonAction = onClick;
        _notifyIcon.ShowBalloonTip(BalloonTimeoutMs, title, message, icon);
    }

    private void RunBalloonAction()
    {
        var action = _balloonAction;
        _balloonAction = null;
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bildirim eylemi başarısız.");
        }
    }
}
