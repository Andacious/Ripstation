using Microsoft.UI.Xaml;
using Ripstation.Services;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class App : Application
{
    public static Window? MainWindowInstance { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = new GlobalSettings();
        IFileSystem fs = new FileSystem();
        IProcessRunner processRunner = new ProcessRunner();
        IMakeMkvService makeMkv = new MakeMkvService(processRunner, settings, fs);
        IHandBrakeService handBrake = new HandBrakeService(processRunner, settings, fs);
        IMediaNamingService naming = new MediaNamingService();
        IDriveService drive = new DriveService();

        var window = new MainWindow(makeMkv, handBrake, naming, drive, settings);
        MainWindowInstance = window;
        window.Activate();
    }
}
