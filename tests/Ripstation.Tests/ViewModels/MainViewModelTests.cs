using NSubstitute;
using Ripstation.Services;
using Ripstation.Tests.Helpers;
using Ripstation.ViewModels;

namespace Ripstation.Tests.ViewModels;

public class MainViewModelTests
{
    private static MainViewModel Build(
        FakeDriveService? driveService = null,
        IMakeMkvService? makeMkv = null,
        IHandBrakeService? handBrake = null,
        IMediaNamingService? naming = null)
    {
        return new MainViewModel(
            makeMkv ?? Substitute.For<IMakeMkvService>(),
            handBrake ?? Substitute.For<IHandBrakeService>(),
            naming ?? new MediaNamingService(),
            driveService ?? new FakeDriveService(),
            dispatcher: new SynchronousDispatcher());
    }

    private static TitleViewModel MakeSelectedTitle(int id = 0)
    {
        var vm = new TitleViewModel(new Ripstation.Models.Title { Id = id, Name = $"Title {id}" });
        vm.IsSelected = true;
        return vm;
    }

    // ── Drive detection at startup ────────────────────────────────────────────

    [Fact]
    public void Constructor_NoOpticalDrives_Creates2DefaultDrives()
    {
        var vm = Build(new FakeDriveService(/* no drives */));
        Assert.Equal(2, vm.Drives.Count);
        Assert.Equal(0, vm.Drives[0].DiskNumber);
        Assert.Equal(1, vm.Drives[1].DiskNumber);
    }

    [Fact]
    public void Constructor_TwoOpticalDrives_UsesThem()
    {
        var ds = new FakeDriveService((0, @"D:\"), (1, @"E:\"));
        var vm = Build(ds);
        Assert.Equal(2, vm.Drives.Count);
        Assert.Equal(0, vm.Drives[0].DiskNumber);
        Assert.Equal(1, vm.Drives[1].DiskNumber);
    }

    [Fact]
    public void Constructor_OpticalDrives_SetsDriveLetter()
    {
        var ds = new FakeDriveService((0, @"D:\"));
        var vm = Build(ds);
        Assert.Equal(@"D:\", vm.Drives[0].DriveLetter);
    }

    // ── AddDriveCommand ───────────────────────────────────────────────────────

    [Fact]
    public void AddDrive_IncreasesDriveCount()
    {
        var vm = Build();
        var before = vm.Drives.Count;
        vm.AddDriveCommand.Execute(null);
        Assert.Equal(before + 1, vm.Drives.Count);
    }

    [Fact]
    public void AddDrive_NewDriveHasDiskNumberMaxPlusOne()
    {
        var vm = Build();
        // Default 2 drives: 0, 1
        vm.AddDriveCommand.Execute(null);
        Assert.Equal(2, vm.Drives[^1].DiskNumber);
    }

    [Fact]
    public void AddDrive_Empty_StartsWith0()
    {
        var ds = new FakeDriveService((5, @"F:\"));
        var vm = Build(ds);
        vm.Drives.Clear();
        vm.AddDriveCommand.Execute(null);
        Assert.Equal(0, vm.Drives[0].DiskNumber);
    }

    // ── RemoveDriveCommand ────────────────────────────────────────────────────

    [Fact]
    public void RemoveDrive_RemovesDriveFromCollection()
    {
        var vm = Build();
        var driveToRemove = vm.Drives[0];
        vm.RemoveDriveCommand.Execute(driveToRemove);
        Assert.DoesNotContain(driveToRemove, vm.Drives);
    }

    [Fact]
    public void RemoveDrive_Null_DoesNothing()
    {
        var vm = Build();
        var before = vm.Drives.Count;
        vm.RemoveDriveCommand.Execute(null);
        Assert.Equal(before, vm.Drives.Count);
    }

    // ── DetectDrivesCommand ───────────────────────────────────────────────────

    [Fact]
    public void DetectDrives_NoDrivesFound_LogsMessage()
    {
        var vm = Build(new FakeDriveService());
        vm.Drives.Clear();
        vm.DetectDrivesCommand.Execute(null);
        Assert.Contains(vm.LogLines, l => l.Contains("No optical drives detected"));
    }

    [Fact]
    public void DetectDrives_NewDrives_AddsThemNonDestructively()
    {
        // Start with disc:0 already present
        var ds = new FakeDriveService((0, @"D:\"), (1, @"E:\"));
        var vm = Build(ds);
        Assert.Equal(2, vm.Drives.Count); // already has both from constructor

        // Remove disc:1 to simulate it not being present
        vm.Drives.RemoveAt(1);
        Assert.Single(vm.Drives);

        // Re-detect — should add disc:1 back without touching disc:0
        vm.DetectDrivesCommand.Execute(null);
        Assert.Equal(2, vm.Drives.Count);
        Assert.Contains(vm.Drives, d => d.DiskNumber == 0);
        Assert.Contains(vm.Drives, d => d.DiskNumber == 1);
    }

    [Fact]
    public void DetectDrives_AllAlreadyPresent_LogsAlreadyPresent()
    {
        var ds = new FakeDriveService((0, @"D:\"), (1, @"E:\"));
        var vm = Build(ds);
        // Both drives already added at startup
        vm.DetectDrivesCommand.Execute(null);
        Assert.Contains(vm.LogLines, l => l.Contains("all already present"));
    }

    // ── RipAllCommand CanExecute ──────────────────────────────────────────────

    [Fact]
    public void RipAllCommand_CannotExecute_WhenNoDrivesHaveSelection()
    {
        var vm = Build();
        Assert.False(vm.RipAllCommand.CanExecute(null));
    }

    [Fact]
    public void RipAllCommand_CanExecute_WhenAtLeastOneDriveHasTitlesSelected()
    {
        var vm = Build();
        vm.Drives[0].Titles.Add(MakeSelectedTitle());
        vm.Drives[0].SelectedTitleCount = 1;
        Assert.True(vm.RipAllCommand.CanExecute(null));
    }

    // ── RipAllAsync collision detection ───────────────────────────────────────

    [Fact]
    public async Task RipAllAsync_OutputCollision_LogsErrorAndAborts()
    {
        var makeMkv = Substitute.For<IMakeMkvService>();
        var handBrake = Substitute.For<IHandBrakeService>();
        var vm = Build(makeMkv: makeMkv, handBrake: handBrake);

        // Set both drives to produce the same output path
        vm.Drives[0].MediaName = "SharedName";
        vm.Drives[0].Titles.Add(MakeSelectedTitle(0));
        vm.Drives[0].SelectedTitleCount = 1;

        vm.Drives[1].MediaName = "SharedName";
        vm.Drives[1].Titles.Add(MakeSelectedTitle(0));
        vm.Drives[1].SelectedTitleCount = 1;

        await vm.RipAllCommand.ExecuteAsync(null);

        Assert.Contains(vm.LogLines, l => l.Contains("collision"));
        // Neither drive should have started ripping
        await makeMkv.DidNotReceive().RipTitleAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
            Arg.Any<Action<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RipAllAsync_NoEligibleDrives_LogsAndReturns()
    {
        var vm = Build();
        // No titles selected on any drive
        await vm.RipAllCommand.ExecuteAsync(null);
        Assert.Contains(vm.LogLines, l => l.Contains("No drives have selected titles"));
    }

    // ── CancelAllCommand ──────────────────────────────────────────────────────

    [Fact]
    public void CancelAllCommand_CannotExecute_WhenNotRippingAll()
    {
        var vm = Build();
        Assert.False(vm.CancelAllCommand.CanExecute(null));
    }

    // ── LogFromThread ─────────────────────────────────────────────────────────

    [Fact]
    public void LogFromThread_AppendsFormattedLine()
    {
        var vm = Build();
        vm.LogLines.Clear();
        vm.LogFromThread("hello world");
        Assert.Single(vm.LogLines);
        Assert.Contains("hello world", vm.LogLines[0]);
    }

    [Fact]
    public void LogFromThread_AtMaxCapacity_RemovesOldestLine()
    {
        var vm = Build();
        vm.LogLines.Clear();

        // Fill to max (500)
        for (int i = 0; i < 500; i++)
            vm.LogLines.Add($"line {i}");

        vm.LogFromThread("overflow line");

        Assert.Equal(500, vm.LogLines.Count);
        Assert.Contains(vm.LogLines, l => l.Contains("overflow line"));
        // The very first line should have been evicted
        Assert.DoesNotContain(vm.LogLines, l => l == "line 0");
    }

    [Fact]
    public void LogFromThread_BelowCapacity_JustAdds()
    {
        var vm = Build();
        vm.LogLines.Clear();
        vm.LogFromThread("msg1");
        vm.LogFromThread("msg2");
        Assert.Equal(2, vm.LogLines.Count);
    }

    // ── EjectAll ─────────────────────────────────────────────────────────────

    [Fact]
    public void EjectAllCommand_TriggersEjectOnEachDrive()
    {
        var ds = new FakeDriveService((0, @"D:\"), (1, @"E:\"));
        var vm = Build(ds);

        vm.EjectAllCommand.Execute(null);

        // Both drives should have been ejected
        Assert.Contains(0, ds.EjectedIndices);
        Assert.Contains(1, ds.EjectedIndices);
    }
}
