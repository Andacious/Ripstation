using System.Windows;
using Ripstation.Services;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IProcessRunner processRunner = new ProcessRunner();
        IMakeMkvService makeMkv = new MakeMkvService(processRunner);
        IHandBrakeService handBrake = new HandBrakeService(processRunner);
        IMediaNamingService naming = new MediaNamingService();
        IDriveService drive = new DriveService();

        var vm = new MainViewModel(makeMkv, handBrake, naming, drive);
        var window = new MainWindow { DataContext = vm };
        window.Show();
    }
}
