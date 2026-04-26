using System.Windows;
using Microsoft.Win32;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class SettingsWindow : Window
{
    public SettingsWindow(GlobalSettings settings)
    {
        InitializeComponent();
        DataContext = settings;
    }

    private GlobalSettings Settings => (GlobalSettings)DataContext;

    private void BrowseMakeMkv_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select MakeMKV CLI",
            Filter = "MakeMKV CLI|makemkvcon64.exe|Executable|*.exe",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == true)
            Settings.MakeMkvExePath = dlg.FileName;
    }

    private void BrowseHandBrake_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select HandBrakeCLI",
            Filter = "HandBrakeCLI|HandBrakeCLI.exe|Executable|*.exe",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == true)
            Settings.HandBrakeExePath = dlg.FileName;
    }

    private void BrowsePreset_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select HandBrake Preset File",
            Filter = "JSON Preset|*.json|All files|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == true)
            Settings.PresetFilePath = dlg.FileName;
    }

    private void BrowseIntermediate_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Intermediate (MKV) Folder" };
        if (dlg.ShowDialog(this) == true)
            Settings.IntermediatePath = dlg.FolderName;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Output (Plex) Folder" };
        if (dlg.ShowDialog(this) == true)
            Settings.OutputPath = dlg.FolderName;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
