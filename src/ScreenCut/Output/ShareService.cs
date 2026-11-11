using System.Windows;
using System.Windows.Interop;
using ScreenCut.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ScreenCut.Output;

/// <summary>Opens the Windows share sheet (WhatsApp, Teams, Outlook, Nearby Sharing… whatever is installed).</summary>
internal static class ShareService
{
    private static readonly TimeSpan HostLifetime = TimeSpan.FromMinutes(2);

    public static void Share(string filePath, INotifier notifier)
    {
        // The share sheet needs a visible owner window; the overlay is already closed at this point.
        var host = CreateHostWindow();
        host.Show();
        var hwnd = new WindowInteropHelper(host).Handle;

        var manager = DataTransferManagerInterop.GetForWindow(hwnd);
        manager.DataRequested += async (_, args) =>
        {
            var deferral = args.Request.GetDeferral();
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                args.Request.Data.Properties.Title = "ScreenCut ekran görüntüsü";
                args.Request.Data.SetStorageItems([file]);
                args.Request.Data.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Paylaşım verisi hazırlanamadı.");
                args.Request.FailWithDisplayText("Ekran görüntüsü paylaşılamadı.");
            }
            finally
            {
                deferral.Complete();
            }
        };
        manager.TargetApplicationChosen += (_, _) => host.Dispatcher.BeginInvoke(host.Close);

        try
        {
            DataTransferManagerInterop.ShowShareUIForWindow(hwnd);
        }
        catch (Exception ex)
        {
            host.Close();
            Log.Error(ex, "Windows paylaşım penceresi açılamadı.");
            notifier.Error("Paylaşım açılamadı", "Windows paylaşım penceresi kullanılamıyor.");
            return;
        }

        // If the user dismisses the sheet there is no event; clean up the invisible host later.
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = HostLifetime };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            host.Close();
        };
        timer.Start();
    }

    private static Window CreateHostWindow()
    {
        var workArea = SystemParameters.WorkArea;
        return new Window
        {
            Width = 1,
            Height = 1,
            Left = workArea.Left + (workArea.Width / 2),
            Top = workArea.Top + (workArea.Height / 2),
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            Topmost = true,
            Title = AppInfo.Name,
        };
    }
}
