using ScreenCut.Core;
using ScreenCut.Output;
using ScreenCut.Overlay;

namespace ScreenCut.Capture;

/// <summary>Owns the capture lifecycle: grab the screen, show the overlay, handle recapture.</summary>
internal sealed class CaptureCoordinator(OutputActions actions, AppSettings settings, Action persistSettings, INotifier notifier)
{
    private static readonly TimeSpan RecaptureHideDelay = TimeSpan.FromMilliseconds(250);
    private OverlayWindow? _overlay;
    private bool _isCapturing;

    public void Capture(CaptureMode mode)
    {
        if (_overlay is not null)
        {
            _overlay.ActivateOverlay();
            return;
        }

        if (_isCapturing)
        {
            return;
        }

        _isCapturing = true;
        try
        {
            var snapshot = ScreenCapturer.Capture();
            _overlay = new OverlayWindow(snapshot, mode, actions, settings);
            _overlay.RecaptureRequested += OnRecaptureRequested;
            _overlay.Closed += OnOverlayClosed;
            _overlay.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ekran yakalanamadı.");
            notifier.Error("Ekran yakalanamadı", ex.Message);
            _overlay = null;
        }
        finally
        {
            _isCapturing = false;
        }
    }

    public async Task CaptureDelayedAsync(CaptureMode mode, TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(true);
        Capture(mode);
    }

    private async void OnRecaptureRequested(object? sender, EventArgs e)
    {
        if (_overlay is not { } overlay)
        {
            return;
        }

        try
        {
            overlay.Hide();
            await Task.Delay(RecaptureHideDelay).ConfigureAwait(true);
            overlay.LoadSnapshot(ScreenCapturer.Capture(), CaptureMode.Region);
            overlay.Show();
            overlay.ActivateOverlay();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Yeniden yakalama başarısız.");
            notifier.Error("Yeniden yakalama başarısız", ex.Message);
            overlay.Close();
        }
    }

    private void OnOverlayClosed(object? sender, EventArgs e)
    {
        _overlay = null;
        try
        {
            persistSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, "Ayarlar kaydedilemedi.");
        }

        // A full-desktop screenshot can be tens of MB; give it back promptly since we live in the tray.
        GC.Collect(2, GCCollectionMode.Optimized, blocking: false);
    }
}
