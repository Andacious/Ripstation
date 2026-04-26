using System.Windows;
using System.Windows.Threading;
using Ripstation.Services;
using Ripstation.ViewModels;

namespace Ripstation;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
}
