using System.Windows;
using System.Windows.Threading;
using ScreenCut.Capture;
using ScreenCut.Core;
using ScreenCut.Native;
using ScreenCut.Output;
using ScreenCut.Settings;
using ScreenCut.Tray;

namespace ScreenCut;

/// <summary>Composition root: wires settings, tray icon, keyboard hook and capture coordinator.</summary>
public partial class App : Application
{
    private const string UninstallArgument = "--uninstall";
    private static readonly TimeSpan CaptureDelay = TimeSpan.FromSeconds(3);

    private SingleInstance? _instance;
    private SettingsStore? _store;
    private AppSettings _settings = new();
    private TrayIcon? _tray;
    private KeyboardHook? _hook;
    private CaptureCoordinator? _coordinator;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Initialize(AppInfo.LogFile);
        RegisterExceptionHandlers();

        if (e.Args.Contains(UninstallArgument, StringComparer.OrdinalIgnoreCase))
        {
            SystemIntegration.Uninstall();
            MessageBox.Show("ScreenCut otomatik başlatması kaldırıldı ve PrtScn tuşu Windows varsayılanına döndürüldü.", AppInfo.Name);
            Shutdown();
            return;
        }

        _instance = SingleInstance.TryAcquire();
        if (_instance is null)
        {
            SingleInstance.SignalExisting();
            Shutdown();
            return;
        }

        _store = new SettingsStore(AppInfo.SettingsFile);
        var isFirstRun = !_store.Exists;
        _settings = _store.Load();
        PersistSettings();
        ApplySystemIntegration();

        _tray = new TrayIcon(CreateTrayActions());
        _coordinator = new CaptureCoordinator(new OutputActions(_settings, _tray), _settings, PersistSettings, _tray);
        _instance.ActivationRequested += (_, _) => Dispatcher.BeginInvoke(() => _tray.Info(AppInfo.Name + " zaten çalışıyor", "Ekran alıntısı için PrtScn tuşuna basın."));
        StartKeyboardHook();
        ImageFileWriter.CleanupTemporaryFiles();

        Log.Info($"{AppInfo.Name} {AppInfo.Version} başlatıldı.");
        if (isFirstRun || !e.Args.Contains(SystemIntegration.AutostartArgument, StringComparer.OrdinalIgnoreCase))
        {
            _tray.Info(AppInfo.Name + " hazır", "Arka planda çalışıyor. Ekran alıntısı için PrtScn tuşuna basın.");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        _tray?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }

    private void StartKeyboardHook()
    {
        _hook = new KeyboardHook();
        _hook.PrintScreenPressed += (_, mode) => Dispatcher.BeginInvoke(() => _coordinator?.Capture(mode));
        try
        {
            _hook.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Klavye kancası kurulamadı.");
            _tray?.Error("PrtScn tuşu dinlenemiyor", "Yakalamak için tepsi simgesine tıklayın.");
        }
    }

    private TrayActions CreateTrayActions() => new(
        CaptureRegion: () => _coordinator?.Capture(CaptureMode.Region),
        CaptureWindow: () => _coordinator?.Capture(CaptureMode.ActiveWindow),
        CaptureScreen: () => _coordinator?.Capture(CaptureMode.Screen),
        CaptureAllScreens: () => _coordinator?.Capture(CaptureMode.AllScreens),
        CaptureDelayed: () => _ = _coordinator?.CaptureDelayedAsync(CaptureMode.Region, CaptureDelay),
        OpenSaveFolder: OpenSaveFolder,
        OpenSettings: OpenSettings,
        OpenReleases: () => AppLauncher.OpenShell(AppInfo.ReleasesUrl),
        Exit: Shutdown);

    private void OpenSaveFolder()
    {
        Directory.CreateDirectory(_settings.SaveFolder);
        AppLauncher.OpenShell(_settings.SaveFolder);
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        try
        {
            if (_settingsWindow.ShowDialog() == true && _settingsWindow.Result is { } updated)
            {
                _settings.SaveFolder = updated.SaveFolder;
                _settings.FileNamePattern = updated.FileNamePattern;
                _settings.ImageFormat = updated.ImageFormat;
                _settings.JpegQuality = updated.JpegQuality;
                _settings.StartWithWindows = updated.StartWithWindows;
                PersistSettings();
                ApplySystemIntegration();
            }
        }
        finally
        {
            _settingsWindow = null;
        }
    }

    private void PersistSettings()
    {
        try
        {
            _store?.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, "Ayarlar kaydedilemedi.");
            _tray?.Error("Ayarlar kaydedilemedi", ex.Message);
        }
    }

    private void ApplySystemIntegration()
    {
        try
        {
#if DEBUG
            // Never register a bin\Debug build for autostart.
            Log.Info("DEBUG derlemesi: otomatik başlatma kaydı atlandı.");
#else
            SystemIntegration.SetStartWithWindows(_settings.StartWithWindows);
#endif
            SystemIntegration.DisableSnippingToolPrintScreen();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Log.Error(ex, "Windows entegrasyonu uygulanamadı.");
        }
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Error((Exception)args.ExceptionObject, "İşlenmemiş hata.");
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Gözlemlenmemiş görev hatası.");
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // A tray app must survive individual failures; log, inform, keep running.
        Log.Error(e.Exception, "UI iş parçacığında işlenmemiş hata.");
        _tray?.Error("Beklenmeyen bir hata oluştu", e.Exception.Message);
        e.Handled = true;
    }
}
