using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ripstation.Services;
using Ripstation.ViewModels;
using Windows.Graphics;
using Windows.UI;

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
        RootGrid.ActualThemeChanged += (_, _) => SetTitleBarColors();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;
        SetTitleBarColors();
    }

    private void SetTitleBarColors()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported()) return;

        var titleBar = AppWindow.TitleBar;
        bool isDark = RootGrid.ActualTheme == ElementTheme.Dark;

        if (isDark)
        {
            titleBar.BackgroundColor        = Color.FromArgb(255, 0x1C, 0x1C, 0x1C);
            titleBar.ForegroundColor        = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
            titleBar.InactiveBackgroundColor  = Color.FromArgb(255, 0x1C, 0x1C, 0x1C);
            titleBar.InactiveForegroundColor  = Color.FromArgb(255, 0xAB, 0xAB, 0xAB);
            titleBar.ButtonBackgroundColor    = Color.FromArgb(255, 0x1C, 0x1C, 0x1C);
            titleBar.ButtonForegroundColor    = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
            titleBar.ButtonHoverBackgroundColor   = Color.FromArgb(255, 0x3A, 0x3A, 0x3A);
            titleBar.ButtonHoverForegroundColor   = Color.FromArgb(255, 0xFF, 0xFF, 0xFF);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 0x2D, 0x2D, 0x2D);
            titleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 0xFF, 0xFF, 0xFF);
            titleBar.ButtonInactiveBackgroundColor  = Color.FromArgb(255, 0x1C, 0x1C, 0x1C);
            titleBar.ButtonInactiveForegroundColor  = Color.FromArgb(255, 0xAB, 0xAB, 0xAB);
        }
        else
        {
            titleBar.BackgroundColor        = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
            titleBar.ForegroundColor        = Color.FromArgb(255, 0x1A, 0x1A, 0x1A);
            titleBar.InactiveBackgroundColor  = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
            titleBar.InactiveForegroundColor  = Color.FromArgb(255, 0x76, 0x76, 0x76);
            titleBar.ButtonBackgroundColor    = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
            titleBar.ButtonForegroundColor    = Color.FromArgb(255, 0x1A, 0x1A, 0x1A);
            titleBar.ButtonHoverBackgroundColor   = Color.FromArgb(255, 0xE0, 0xE0, 0xE0);
            titleBar.ButtonHoverForegroundColor   = Color.FromArgb(255, 0x1A, 0x1A, 0x1A);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 0xCC, 0xCC, 0xCC);
            titleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 0x1A, 0x1A, 0x1A);
            titleBar.ButtonInactiveBackgroundColor  = Color.FromArgb(255, 0xF3, 0xF3, 0xF3);
            titleBar.ButtonInactiveForegroundColor  = Color.FromArgb(255, 0x76, 0x76, 0x76);
        }
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
