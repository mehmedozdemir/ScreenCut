using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenCut.Core;

namespace ScreenCut.Output;

/// <summary>Everything the user can do with a finished screenshot.</summary>
internal sealed class OutputActions(AppSettings settings, INotifier notifier)
{
    public INotifier Notifier => notifier;

    public void CopyImage(BitmapSource image) => ClipboardService.SetImage(image);

    /// <summary>Saves without a dialog and copies the file + full path to the clipboard.</summary>
    public void QuickSave(BitmapSource image)
    {
        var path = ImageFileWriter.SaveToConfiguredFolder(image, settings);
        ClipboardService.SetFileAndPath(path);
        notifier.Info("Kaydedildi — dosya yolu panoda", path, () => AppLauncher.ShowInExplorer(path));
    }

    /// <summary>Returns false when the user cancels the dialog.</summary>
    public bool SaveAs(BitmapSource image, Window owner)
    {
        Directory.CreateDirectory(settings.SaveFolder);
        var dialog = new SaveFileDialog
        {
            Title = "Ekran görüntüsünü kaydet",
            InitialDirectory = settings.SaveFolder,
            FileName = FileNameGenerator.Generate(settings.FileNamePattern, DateTime.Now),
            Filter = "PNG resmi (*.png)|*.png|JPEG resmi (*.jpg)|*.jpg",
            FilterIndex = settings.ImageFormat == ImageFormatKind.Jpeg ? 2 : 1,
            AddExtension = true,
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(owner) != true)
        {
            return false;
        }

        var format = dialog.FilterIndex == 2 ? ImageFormatKind.Jpeg : ImageFormatKind.Png;
        if (File.Exists(dialog.FileName))
        {
            File.Delete(dialog.FileName);
        }

        ImageFileWriter.Save(image, dialog.FileName, format, settings.JpegQuality);
        ClipboardService.SetFileAndPath(dialog.FileName);
        return true;
    }

    public void ShareWithWindows(BitmapSource image) =>
        ShareService.Share(ImageFileWriter.SaveTemporary(image), notifier);

    /// <summary>Copies the image and brings the app forward; the user picks the chat and presses Ctrl+V.</summary>
    public void SendTo(MessagingApp app, BitmapSource image)
    {
        ClipboardService.SetImage(image);
        AppLauncher.Open(app);
        notifier.Info($"{AppLauncher.DisplayName(app)} açılıyor", "Görüntü panoda. Sohbeti seçip Ctrl+V ile yapıştırın.");
    }

    public void Email(BitmapSource image)
    {
        var path = ImageFileWriter.SaveTemporary(image);
        var dispatcher = Application.Current.Dispatcher;
        MailService.Compose(path, ex =>
        {
            Log.Error(ex, "E-posta oluşturulamadı.");
            dispatcher.BeginInvoke(() => notifier.Error("E-posta oluşturulamadı", ex.Message));
        });
    }
}
