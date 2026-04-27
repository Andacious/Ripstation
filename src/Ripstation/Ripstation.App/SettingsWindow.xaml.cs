using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ripstation.ViewModels;
using Windows.Graphics;

namespace Ripstation;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow(GlobalSettings settings)
    {
        InitializeComponent();
        Title = "Settings";
        AppWindow.Resize(new SizeInt32(660, 600));
        ContentGrid.DataContext = settings;
    }

    private GlobalSettings Settings => (GlobalSettings)ContentGrid.DataContext;

    private async void BrowseMakeMkv_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file != null) Settings.MakeMkvExePath = file.Path;
    }

    private async void BrowseHandBrake_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file != null) Settings.HandBrakeExePath = file.Path;
    }

    private async void BrowsePreset_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file != null) Settings.PresetFilePath = file.Path;
    }

    private async void BrowseIntermediate_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null) Settings.IntermediatePath = folder.Path;
    }

    private async void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null) Settings.OutputPath = folder.Path;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
