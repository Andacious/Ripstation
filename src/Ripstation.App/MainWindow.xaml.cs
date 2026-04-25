using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    // ── Log auto-scroll ───────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm.LogLines is INotifyCollectionChanged notifier)
            notifier.CollectionChanged += LogLines_CollectionChanged;
    }

    private void LogLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogListBox.Items.Count > 0)
            LogListBox.ScrollIntoView(LogListBox.Items[^1]);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (Vm.LogLines is INotifyCollectionChanged notifier)
            notifier.CollectionChanged -= LogLines_CollectionChanged;
        base.OnClosed(e);
    }

    // ── Browse helpers ────────────────────────────────────────────────────────

    private void BrowseMakeMkv_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select MakeMKV CLI",
            Filter = "MakeMKV CLI|makemkvcon64.exe|Executable|*.exe",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog() == true)
            Vm.Settings.MakeMkvExePath = dlg.FileName;
    }

    private void BrowseHandBrake_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select HandBrakeCLI",
            Filter = "HandBrakeCLI|HandBrakeCLI.exe|Executable|*.exe",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog() == true)
            Vm.Settings.HandBrakeExePath = dlg.FileName;
    }

    private void BrowsePreset_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select HandBrake Preset File",
            Filter = "JSON Preset|*.json|All files|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog() == true)
            Vm.Settings.PresetFilePath = dlg.FileName;
    }

    private void BrowseIntermediate_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Intermediate (MKV) Folder" };
        if (dlg.ShowDialog() == true)
            Vm.Settings.IntermediatePath = dlg.FolderName;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Output (Plex) Folder" };
        if (dlg.ShowDialog() == true)
            Vm.Settings.OutputPath = dlg.FolderName;
    }

    // ── Clear log ─────────────────────────────────────────────────────────────

    private void ClearLog_Click(object sender, RoutedEventArgs e) =>
        Vm.LogLines.Clear();
}
