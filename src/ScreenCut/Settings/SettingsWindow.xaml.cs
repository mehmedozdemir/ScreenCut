using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScreenCut.Core;

namespace ScreenCut.Settings;

/// <summary>Edits a copy of the settings; <see cref="Result"/> is set only when the user saves valid values.</summary>
internal partial class SettingsWindow : Window
{
    private readonly AppSettings _working;

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        _working = current.Clone();

        SaveFolderBox.Text = _working.SaveFolder;
        PatternBox.Text = _working.FileNamePattern;
        FormatCombo.SelectedIndex = _working.ImageFormat == ImageFormatKind.Jpeg ? 1 : 0;
        QualitySlider.Value = _working.JpegQuality;
        StartWithWindowsCheck.IsChecked = _working.StartWithWindows;
        VersionText.Text = $"{AppInfo.Name} {AppInfo.Version}";
        UpdatePatternPreview();
        UpdateQualityState();
    }

    public AppSettings? Result { get; private set; }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Kayıt klasörünü seçin",
            InitialDirectory = Directory.Exists(SaveFolderBox.Text) ? SaveFolderBox.Text : AppSettings.DefaultSaveFolder,
        };

        if (dialog.ShowDialog(this) == true)
        {
            SaveFolderBox.Text = dialog.FolderName;
        }
    }

    private void OnPatternChanged(object sender, TextChangedEventArgs e) => UpdatePatternPreview();

    private void OnFormatChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateQualityState();
        UpdatePatternPreview();
    }

    private void OnQualityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QualityLabel is not null)
        {
            QualityLabel.Text = $"JPEG kalitesi: {(int)e.NewValue}";
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        _working.SaveFolder = Path.GetFullPath(SaveFolderBox.Text.Trim());
        _working.FileNamePattern = PatternBox.Text.Trim();
        _working.ImageFormat = SelectedFormat;
        _working.JpegQuality = (int)QualitySlider.Value;
        _working.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        Result = _working.Normalize();
        DialogResult = true;
    }

    private ImageFormatKind SelectedFormat => FormatCombo.SelectedIndex == 1 ? ImageFormatKind.Jpeg : ImageFormatKind.Png;

    private string? Validate()
    {
        var folder = SaveFolderBox.Text.Trim();
        if (folder.Length == 0 || !Path.IsPathFullyQualified(folder) || folder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return "Kayıt klasörü tam bir yol olmalı (ör. C:\\Users\\…\\Pictures\\ScreenCut).";
        }

        return FileNameGenerator.Validate(PatternBox.Text);
    }

    private void UpdatePatternPreview()
    {
        if (PatternPreview is null || FormatCombo is null)
        {
            return;
        }

        var error = FileNameGenerator.Validate(PatternBox.Text);
        PatternPreview.Text = error ?? "Örnek: " + FileNameGenerator.Generate(PatternBox.Text, DateTime.Now) + FileNameGenerator.Extension(SelectedFormat);
        PatternPreview.Foreground = error is null ? System.Windows.Media.Brushes.DimGray : System.Windows.Media.Brushes.Firebrick;
    }

    private void UpdateQualityState()
    {
        if (QualityPanel is not null)
        {
            QualityPanel.IsEnabled = SelectedFormat == ImageFormatKind.Jpeg;
            QualityPanel.Opacity = QualityPanel.IsEnabled ? 1 : 0.5;
        }
    }
}
