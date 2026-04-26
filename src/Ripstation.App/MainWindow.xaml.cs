using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    // ── Mouse wheel forwarding (prevent inner ScrollViewer from swallowing events) ─

    private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var outerSv = (ScrollViewer)sender;
        var source = e.OriginalSource as DependencyObject;

        // Find the closest ScrollViewer ancestor of the event source that is NOT the outer one
        var innerSv = FindAncestorScrollViewer(source, outerSv);
        if (innerSv == null) return; // Mouse is over outer content — let outer SV handle normally

        // WPF ScrollViewer always marks MouseWheel as Handled even when it can't scroll,
        // so intercept here in the tunnel phase and route manually.
        e.Handled = true;

        double linePixels = SystemParameters.WheelScrollLines * 16.0;
        double amount = -e.Delta / 120.0 * linePixels;

        // Prefer inner SV if it actually has scrollable content; fall back to outer SV
        var target = innerSv.ScrollableHeight > 0 ? innerSv : outerSv;
        target.ScrollToVerticalOffset(
            Math.Clamp(target.VerticalOffset + amount, 0, target.ScrollableHeight));
    }

    private static ScrollViewer? FindAncestorScrollViewer(DependencyObject? obj, ScrollViewer exclude)
    {
        var current = obj;
        while (current != null && current != exclude)
        {
            if (current is ScrollViewer sv)
                return sv;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

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
