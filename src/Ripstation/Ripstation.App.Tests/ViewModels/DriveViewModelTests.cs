using NSubstitute;
using Ripstation.Models;
using Ripstation.Services;
using Ripstation.ViewModels;
using RipstationApp.Tests.Helpers;

namespace RipstationApp.Tests.ViewModels;

public class DriveViewModelTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static GlobalSettings DefaultSettings() => new()
    {
        MakeMkvExePath   = @"C:\tools\makemkvcon64.exe",
        HandBrakeExePath = @"C:\tools\HandBrakeCLI.exe",
        PresetFilePath   = @"C:\presets\Plex.json",
        PresetName       = "Plex",
        IntermediatePath = @"D:\mkv",
        OutputPath       = @"E:\movies",
    };

    private static DriveViewModel BuildDrive(
        int diskNumber = 0,
        GlobalSettings? settings = null,
        IMakeMkvService? makeMkv = null,
        IHandBrakeService? handBrake = null,
        IMediaNamingService? naming = null,
        IDriveService? driveService = null,
        string driveLetter = "",
        IFileSystem? fs = null,
        List<string>? log = null)
    {
        return new DriveViewModel(
            diskNumber,
            settings ?? DefaultSettings(),
            makeMkv ?? Substitute.For<IMakeMkvService>(),
            handBrake ?? Substitute.For<IHandBrakeService>(),
            naming ?? new MediaNamingService(),
            driveService ?? new FakeDriveService(),
            msg => log?.Add(msg),
            driveLetter,
            fs ?? new FakeFileSystem());
    }

    private static TitleViewModel MakeTitle(int id = 0, string name = "Title", string fileName = "title_t00.mkv")
        => new(new Title { Id = id, Name = name, FileName = fileName, Duration = TimeSpan.FromHours(2) });

    // ── TabHeader ─────────────────────────────────────────────────────────────

    [Fact]
    public void TabHeader_NoDriveLetterNoDiscName_ShowsDiskNumber()
    {
        var vm = BuildDrive(diskNumber: 0);
        Assert.Equal("Drive 0", vm.TabHeader);
    }

    [Fact]
    public void TabHeader_WithDriveLetter_ShowsDriveLetterOnly()
    {
        var vm = BuildDrive(diskNumber: 1, driveLetter: @"D:\");
        Assert.Equal("D:", vm.TabHeader);
    }

    [Fact]
    public void TabHeader_WithDiscName_ShowsDiscNameWithDriveId()
    {
        var vm = BuildDrive(diskNumber: 0);
        vm.DiscName = "INTERSTELLAR";
        Assert.Equal("INTERSTELLAR (Drive 0)", vm.TabHeader);
    }

    [Fact]
    public void TabHeader_DiscNameAndDriveLetter_ShowsBoth()
    {
        var vm = BuildDrive(diskNumber: 0, driveLetter: @"D:\");
        vm.DiscName = "MY_DISC";
        Assert.Equal("MY_DISC (D:)", vm.TabHeader);
        Assert.Contains("D:", vm.TabHeader);
    }

    // ── IsRipStatusIdleVisible ────────────────────────────────────────────────

    [Fact]
    public void IsRipStatusIdleVisible_DefaultState_False()
    {
        var vm = BuildDrive();
        Assert.False(vm.IsRipStatusIdleVisible);
    }

    [Fact]
    public void IsRipStatusIdleVisible_StatusSetWhenIdle_True()
    {
        var vm = BuildDrive();
        vm.RipStatus = "Done!";
        Assert.True(vm.IsRipStatusIdleVisible);
    }

    [Fact]
    public void IsRipStatusIdleVisible_EmptyStatus_False()
    {
        var vm = BuildDrive();
        vm.RipStatus = "";
        Assert.False(vm.IsRipStatusIdleVisible);
    }

    // ── HasTitles ─────────────────────────────────────────────────────────────

    [Fact]
    public void HasTitles_Default_False()
    {
        var vm = BuildDrive();
        Assert.False(vm.HasTitles);
    }

    [Fact]
    public void HasTitles_AfterAddingTitle_True()
    {
        var vm = BuildDrive();
        vm.Titles.Add(MakeTitle());
        Assert.True(vm.HasTitles);
    }

    [Fact]
    public void HasTitles_AfterClearingTitles_False()
    {
        var vm = BuildDrive();
        vm.Titles.Add(MakeTitle());
        vm.Titles.Clear();
        Assert.False(vm.HasTitles);
    }

    // ── EpisodeCount ──────────────────────────────────────────────────────────

    [Fact]
    public void EpisodeCount_StartEqualsEnd_ReturnsOne()
    {
        var vm = BuildDrive();
        vm.EpisodeStart = 3;
        vm.EpisodeEnd = 3;
        Assert.Equal(1, vm.EpisodeCount);
    }

    [Fact]
    public void EpisodeCount_RangeOf4_Returns4()
    {
        var vm = BuildDrive();
        vm.EpisodeStart = 2;
        vm.EpisodeEnd = 5;
        Assert.Equal(4, vm.EpisodeCount);
    }

    [Fact]
    public void EpisodeCount_InvalidRange_ReturnsZero()
    {
        var vm = BuildDrive();
        vm.EpisodeStart = 5;
        vm.EpisodeEnd = 3;
        Assert.Equal(0, vm.EpisodeCount);
    }

    // ── CanScan (via ScanCommand.CanExecute) ──────────────────────────────────

    [Fact]
    public void ScanCommand_CanExecute_WhenIdleAndPathSet()
    {
        var vm = BuildDrive();
        Assert.True(vm.ScanCommand.CanExecute(null));
    }

    [Fact]
    public void ScanCommand_CannotExecute_WhenMakeMkvPathEmpty()
    {
        var settings = DefaultSettings();
        settings.MakeMkvExePath = string.Empty;
        var vm = BuildDrive(settings: settings);
        Assert.False(vm.ScanCommand.CanExecute(null));
    }

    [Fact]
    public void ScanCommand_CannotExecute_WhenIsScanning()
    {
        var vm = BuildDrive();
        vm.IsScanning = true;
        Assert.False(vm.ScanCommand.CanExecute(null));
    }

    [Fact]
    public void ScanCommand_CannotExecute_WhenIsRipping()
    {
        var vm = BuildDrive();
        vm.IsRipping = true;
        Assert.False(vm.ScanCommand.CanExecute(null));
    }

    // ── CanRip (via RipStandaloneCommand.CanExecute) ──────────────────────────

    [Fact]
    public void RipCommand_CannotExecute_WhenNoTitlesSelected()
    {
        var vm = BuildDrive();
        Assert.False(vm.RipStandaloneCommand.CanExecute(null));
    }

    [Fact]
    public void RipCommand_Movie_CanExecute_WithExactlyOneSelected()
    {
        var vm = BuildDrive();
        var t = MakeTitle();
        t.IsSelected = true;
        vm.Titles.Add(t);
        vm.SelectedTitleCount = 1;
        Assert.True(vm.RipStandaloneCommand.CanExecute(null));
    }

    [Fact]
    public void RipCommand_Movie_CannotExecute_WithTwoSelected()
    {
        var vm = BuildDrive();
        for (int i = 0; i < 2; i++) { var t = MakeTitle(i); t.IsSelected = true; vm.Titles.Add(t); }
        vm.SelectedTitleCount = 2;
        Assert.False(vm.RipStandaloneCommand.CanExecute(null));
    }

    [Fact]
    public void RipCommand_TvMode_CanExecute_WhenCountMatchesEpisodes()
    {
        var vm = BuildDrive();
        vm.IsTvMode = true;
        vm.EpisodeStart = 1;
        vm.EpisodeEnd = 3;
        for (int i = 0; i < 3; i++) { var t = MakeTitle(i); t.IsSelected = true; vm.Titles.Add(t); }
        vm.SelectedTitleCount = 3;
        Assert.True(vm.RipStandaloneCommand.CanExecute(null));
    }

    [Fact]
    public void RipCommand_TvMode_CannotExecute_WhenCountMismatch()
    {
        var vm = BuildDrive();
        vm.IsTvMode = true;
        vm.EpisodeStart = 1;
        vm.EpisodeEnd = 5; // 5 episodes but only 3 titles
        for (int i = 0; i < 3; i++) { var t = MakeTitle(i); t.IsSelected = true; vm.Titles.Add(t); }
        vm.SelectedTitleCount = 3;
        Assert.False(vm.RipStandaloneCommand.CanExecute(null));
    }

    [Fact]
    public void RipCommand_TvMode_CannotExecute_WhenEpisodeEndLessThanStart()
    {
        var vm = BuildDrive();
        vm.IsTvMode = true;
        vm.EpisodeStart = 5;
        vm.EpisodeEnd = 3;
        var t = MakeTitle();
        t.IsSelected = true;
        vm.Titles.Add(t);
        vm.SelectedTitleCount = 1;
        Assert.False(vm.RipStandaloneCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(nameof(GlobalSettings.HandBrakeExePath))]
    [InlineData(nameof(GlobalSettings.IntermediatePath))]
    [InlineData(nameof(GlobalSettings.OutputPath))]
    [InlineData(nameof(GlobalSettings.PresetFilePath))]
    public void RipCommand_CannotExecute_WhenRequiredSettingEmpty(string settingName)
    {
        var settings = DefaultSettings();
        typeof(GlobalSettings).GetProperty(settingName)!.SetValue(settings, string.Empty);
        var vm = BuildDrive(settings: settings);
        var t = MakeTitle(); t.IsSelected = true; vm.Titles.Add(t);
        vm.SelectedTitleCount = 1;
        Assert.False(vm.RipStandaloneCommand.CanExecute(null));
    }

    // ── SelectAll / DeselectAll ───────────────────────────────────────────────

    [Fact]
    public void SelectAll_SetsAllTitlesSelected()
    {
        var vm = BuildDrive();
        vm.Titles.Add(MakeTitle(0));
        vm.Titles.Add(MakeTitle(1));
        vm.SelectAllCommand.Execute(null);
        Assert.All(vm.Titles, t => Assert.True(t.IsSelected));
    }

    [Fact]
    public void DeselectAll_ClearsAllTitlesSelected()
    {
        var vm = BuildDrive();
        var t1 = MakeTitle(0); t1.IsSelected = true; vm.Titles.Add(t1);
        var t2 = MakeTitle(1); t2.IsSelected = true; vm.Titles.Add(t2);
        vm.DeselectAllCommand.Execute(null);
        Assert.All(vm.Titles, t => Assert.False(t.IsSelected));
    }

    // ── GetPlannedOutputPaths ─────────────────────────────────────────────────

    [Fact]
    public void GetPlannedOutputPaths_Movie_ReturnsCorrectM4vPath()
    {
        var settings = DefaultSettings();
        settings.OutputPath = @"E:\movies";
        var vm = BuildDrive(settings: settings);
        vm.MediaName = "Inception";
        var t = MakeTitle(); t.IsSelected = true; vm.Titles.Add(t);

        var paths = vm.GetPlannedOutputPaths().ToList();

        Assert.Single(paths);
        Assert.Equal(@"E:\movies\Inception.m4v", paths[0]);
    }

    [Fact]
    public void GetPlannedOutputPaths_TvShow_ReturnsEpisodePaths()
    {
        var settings = DefaultSettings();
        settings.OutputPath = @"E:\tv";
        var vm = BuildDrive(settings: settings);
        vm.MediaName = "Breaking Bad";
        vm.IsTvMode = true;
        vm.Season = 1;
        vm.EpisodeStart = 1;
        vm.EpisodeEnd = 2;
        vm.Titles.Add(MakeTitle(0)); vm.Titles[^1].IsSelected = true;
        vm.Titles.Add(MakeTitle(1)); vm.Titles[^1].IsSelected = true;

        var paths = vm.GetPlannedOutputPaths().ToList();

        Assert.Equal(2, paths.Count);
        Assert.Equal(@"E:\tv\Breaking Bad\Season 01\Breaking Bad - s01e01.m4v", paths[0]);
        Assert.Equal(@"E:\tv\Breaking Bad\Season 01\Breaking Bad - s01e02.m4v", paths[1]);
    }

    [Fact]
    public void GetPlannedOutputPaths_NoSelection_ReturnsEmpty()
    {
        var vm = BuildDrive();
        vm.Titles.Add(MakeTitle()); // not selected
        Assert.Empty(vm.GetPlannedOutputPaths());
    }

    [Fact]
    public void GetPlannedOutputPaths_TvMode_InvalidRange_DoesNotThrow()
    {
        var vm = BuildDrive();
        vm.IsTvMode = true;
        vm.EpisodeStart = 5;
        vm.EpisodeEnd = 3; // invalid — end < start
        vm.Titles.Add(MakeTitle()); vm.Titles[0].IsSelected = true;

        // Should return empty (not throw)
        var paths = vm.GetPlannedOutputPaths().ToList();
        Assert.Empty(paths);
    }

    // ── ScanAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScanAsync_HappyPath_PopulatesTitlesAndDiscName()
    {
        var lines = new[]
        {
            "CINFO:2,0,\"BLADE_RUNNER_2049\"",
            "TINFO:0,2,0,\"Title 1\"",
            "TINFO:0,9,0,\"2:43:00\"",
            "TINFO:0,27,0,\"title_t00.mkv\"",
        };
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.ScanDiskAsync(Arg.Any<string>(),
                Arg.Any<IProgress<(int, string)>?>(), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((
                new Disk { Name = "BLADE_RUNNER_2049" },
                new List<Title> { new() { Id = 0, Name = "Title 1" } }
            )));

        var vm = BuildDrive(makeMkv: makeMkv);
        vm.ScanCommand.Execute(null);
        await Task.Delay(100); // give async command time to finish

        Assert.Equal("BLADE_RUNNER_2049", vm.DiscName);
        Assert.Single(vm.Titles);
        Assert.False(vm.IsScanning);
    }

    [Fact]
    public async Task ScanAsync_OnCancel_SetsStatusCancelled()
    {
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.ScanDiskAsync(Arg.Any<string>(),
                Arg.Any<IProgress<(int, string)>?>(), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ct = callInfo.ArgAt<CancellationToken>(3);
                var tcs = new TaskCompletionSource<(Disk, List<Title>)>();
                ct.Register(() => tcs.TrySetException(new OperationCanceledException(ct)));
                return tcs.Task;
            });

        var vm = BuildDrive(makeMkv: makeMkv);
        vm.ScanCommand.Execute(null);
        await Task.Delay(20);
        vm.CancelCommand.Execute(null); // cancels _scanCts token, triggers the ct.Register callback
        await Task.Delay(100);

        Assert.Equal("Cancelled", vm.ScanStatus);
        Assert.False(vm.IsScanning);
    }

    [Fact]
    public async Task ScanAsync_OnFailure_SetsStatusFailed()
    {
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.ScanDiskAsync(Arg.Any<string>(),
                Arg.Any<IProgress<(int, string)>?>(), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task<(Disk, List<Title>)>>(
                _ => Task.FromException<(Disk, List<Title>)>(
                    new InvalidOperationException("Drive not found")));

        var vm = BuildDrive(makeMkv: makeMkv);
        vm.ScanCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal("Failed", vm.ScanStatus);
        Assert.False(vm.IsScanning);
    }

    // ── RipAsync reentrancy ───────────────────────────────────────────────────

    [Fact]
    public async Task RipAsync_WhenAlreadyRipping_ReturnsSameTask()
    {
        var tcs = new TaskCompletionSource();
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(async _ => { await tcs.Task; return @"C:\mkv\t.mkv"; });

        var fs = new FakeFileSystem(@"C:\mkv\t.mkv");
        var vm = BuildDrive(makeMkv: makeMkv, fs: fs);
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        var task1 = vm.RipAsync(CancellationToken.None);
        var task2 = vm.RipAsync(CancellationToken.None);

        Assert.Same(task1, task2); // same underlying task object

        tcs.SetResult();
        await Task.WhenAll(task1, task2);
    }

    // ── Settings snapshot in rip ──────────────────────────────────────────────

    [Fact]
    public async Task RipAsync_SnapshotsSettingsBeforeAwait_ChangingPathMidRipHasNoEffect()
    {
        var settings = DefaultSettings();
        settings.IntermediatePath = @"D:\original";

        string? capturedIntermediatePath = null;
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedIntermediatePath = (string)callInfo[2]; // outputPath arg (3rd param)
                return Task.FromResult(@"D:\original\disc0\t.mkv");
            });

        var handBrake = Substitute.For<IHandBrakeService>();
        var fs = new FakeFileSystem(@"D:\original\disc0\t.mkv");
        var vm = BuildDrive(settings: settings, makeMkv: makeMkv, handBrake: handBrake, fs: fs);
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        var ripTask = vm.RipAsync(CancellationToken.None);
        settings.IntermediatePath = @"D:\changed"; // change AFTER rip starts
        await ripTask;

        // The rip should have used the original path, not the changed one
        Assert.Equal(@"D:\original\disc0", capturedIntermediatePath);
    }

    // ── File system side effects ──────────────────────────────────────────────

    [Fact]
    public async Task RipAsync_CreatesIntermediateDirectory()
    {
        var settings = DefaultSettings();
        settings.IntermediatePath = @"D:\mkv";
        var fs = new FakeFileSystem(@"D:\mkv\disc0\title_t00.mkv");

        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"D:\mkv\disc0\title_t00.mkv"));

        var handBrake = Substitute.For<IHandBrakeService>();
        var vm = BuildDrive(settings: settings, makeMkv: makeMkv, handBrake: handBrake, fs: fs);
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        await vm.RipAsync(CancellationToken.None);

        Assert.Contains(@"D:\mkv\disc0", fs.Created);
    }

    [Fact]
    public async Task RipAsync_ExistingOutputFile_DeletedBeforeEncode()
    {
        var settings = DefaultSettings();
        settings.OutputPath = @"E:\movies";
        var existingM4v = @"E:\movies\My Movie.m4v";
        var mkvPath = @"D:\mkv\disc0\title_t00.mkv";
        var fs = new FakeFileSystem(existingM4v, mkvPath);

        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mkvPath));

        var handBrake = Substitute.For<IHandBrakeService>();
        var vm = BuildDrive(settings: settings, makeMkv: makeMkv, handBrake: handBrake, fs: fs);
        vm.MediaName = "My Movie";
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        await vm.RipAsync(CancellationToken.None);

        Assert.Contains(existingM4v, fs.Deleted);
    }

    [Fact]
    public async Task RipAsync_AfterSuccessfulEncode_DeletesIntermediateMkv()
    {
        var settings = DefaultSettings();
        settings.IntermediatePath = @"D:\mkv";
        var mkvPath = @"D:\mkv\disc0\title_t00.mkv";
        var fs = new FakeFileSystem(mkvPath);

        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mkvPath));

        var handBrake = Substitute.For<IHandBrakeService>();
        var vm = BuildDrive(settings: settings, makeMkv: makeMkv, handBrake: handBrake, fs: fs);
        vm.MediaName = "Some Film";
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        await vm.RipAsync(CancellationToken.None);

        Assert.Contains(mkvPath, fs.Deleted);
    }

    // ── EjectWhenDone ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RipAsync_EjectWhenDone_CallsEjectAfterSuccess()
    {
        var settings = DefaultSettings();
        var fakeDs = new FakeDriveService();
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"D:\mkv\disc0\t.mkv"));
        var handBrake = Substitute.For<IHandBrakeService>();
        var fs = new FakeFileSystem(@"D:\mkv\disc0\t.mkv");

        var vm = BuildDrive(diskNumber: 2, settings: settings, makeMkv: makeMkv,
            handBrake: handBrake, driveService: fakeDs, fs: fs);
        vm.MediaName = "Film";
        vm.EjectWhenDone = true;
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        await vm.RipAsync(CancellationToken.None);

        Assert.Contains(2, fakeDs.EjectedIndices);
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RipAsync_OnCancellation_SetsRipStatusAndResetsIsRipping()
    {
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource();
        var makeMkv = Substitute.For<IMakeMkvService>();
        makeMkv.RipTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<(int, string)>?>(),
                Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(async _ => { await tcs.Task; return ""; });

        var fs = new FakeFileSystem();
        var vm = BuildDrive(makeMkv: makeMkv, fs: fs);
        var title = MakeTitle(0); title.IsSelected = true; vm.Titles.Add(title);
        vm.SelectedTitleCount = 1;

        var ripTask = vm.RipAsync(cts.Token);
        await Task.Delay(20);
        cts.Cancel();
        tcs.TrySetCanceled();
        await ripTask;

        Assert.Equal("Cancelled", vm.RipStatus);
        Assert.False(vm.IsRipping);
    }
}
