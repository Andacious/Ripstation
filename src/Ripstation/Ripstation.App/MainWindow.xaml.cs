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

        // Extend XAML content into the title bar area so the header
        // naturally follows the XAML theme (dark/light).
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        RootGrid.Loaded += OnLoaded;
        AppTitleBar.SizeChanged += (_, _) => UpdateCaptionInset();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;

        UpdateCaptionInset();
        SetCaptionButtonColors();
    }

    /// <summary>
    /// Keeps a spacer column in the header wide enough that the Settings button
    /// never hides behind the system caption buttons (min/max/close).
    /// </summary>
    private void UpdateCaptionInset()
    {
        double scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;
        double insetPx = AppWindow.TitleBar.RightInset;
        CaptionButtonColumn.Width = new GridLength(insetPx / scale);
    }

    /// <summary>
    /// Caption buttons (min/max/close) are rendered over the blue header, so
    /// style them with white icons and semi-transparent hover backgrounds.
    /// </summary>
    private void SetCaptionButtonColors()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported()) return;

        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonForegroundColor         = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonBackgroundColor         = Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonHoverForegroundColor    = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonHoverBackgroundColor    = Color.FromArgb(40, 255, 255, 255);
        titleBar.ButtonPressedForegroundColor  = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonPressedBackgroundColor  = Color.FromArgb(80, 255, 255, 255);
        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(150, 255, 255, 255);
        titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
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
