using Microsoft.UI.Xaml;
using Ripstation.Services;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class App : Application
{
    public static Window? MainWindowInstance { get; private set; }

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        IFileSystem fs = new FileSystem();
        IProcessRunner processRunner = new ProcessRunner();
        IMakeMkvService makeMkv = new MakeMkvService(processRunner, fs);
        IHandBrakeService handBrake = new HandBrakeService(processRunner, fs);
        IMediaNamingService naming = new MediaNamingService();
        IDriveService drive = new DriveService();

        var window = new MainWindow(makeMkv, handBrake, naming, drive);
        MainWindowInstance = window;
        window.Activate();
    }
}
