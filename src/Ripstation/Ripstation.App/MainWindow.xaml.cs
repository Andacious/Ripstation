using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ripstation.Services;
using Ripstation.ViewModels;
using Windows.Graphics;

namespace Ripstation;

public sealed partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow(
        IMakeMkvService makeMkv,
        IHandBrakeService handBrake,
        IMediaNamingService naming,
        IDriveService drive)
    {
        InitializeComponent();
        Title = "Ripstation";

        var dispatcher = new WinUIDispatcher(DispatcherQueue.GetForCurrentThread());
        _vm = new MainViewModel(makeMkv, handBrake, naming, drive, dispatcher);
        RootGrid.DataContext = _vm;

        AppWindow.Resize(new SizeInt32(1100, 860));
        RootGrid.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.LogText))
        {
            LogTextBox.SelectionStart = LogTextBox.Text.Length;
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var win = new SettingsWindow(_vm.Settings);
        win.Activate();
    }
}
