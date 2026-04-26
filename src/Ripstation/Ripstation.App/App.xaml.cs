using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Ripstation.Services;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyTheme(GetWindowsIsDark());
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        IFileSystem fs = new FileSystem();
        IProcessRunner processRunner = new ProcessRunner();
        IMakeMkvService makeMkv = new MakeMkvService(processRunner, fs);
        IHandBrakeService handBrake = new HandBrakeService(processRunner, fs);
        IMediaNamingService naming = new MediaNamingService();
        IDriveService drive = new DriveService();
        IUiDispatcher dispatcher = new WpfDispatcher(Dispatcher.CurrentDispatcher);

        var vm = new MainViewModel(makeMkv, handBrake, naming, drive, dispatcher);
        var window = new MainWindow { DataContext = vm };
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        base.OnExit(e);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
            Dispatcher.Invoke(() => ApplyTheme(GetWindowsIsDark()));
    }

    private static bool GetWindowsIsDark()
    {
        var val = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1);
        return val is int i && i == 0;
    }

    private void ApplyTheme(bool isDark)
    {
        var name = isDark ? "Dark.xaml" : "Light.xaml";
        var uri = new Uri($"pack://application:,,,/Resources/Themes/{name}");
        var dict = new ResourceDictionary { Source = uri };

        var existing = Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("/Themes/") == true);
        if (existing != null) Resources.MergedDictionaries.Remove(existing);
        Resources.MergedDictionaries.Add(dict);
    }
}
