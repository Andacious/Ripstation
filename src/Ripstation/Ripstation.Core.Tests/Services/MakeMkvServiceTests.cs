using Ripstation.Core.Tests.Helpers;
using Ripstation.Models;
using Ripstation.Services;

namespace Ripstation.Core.Tests.Services;

public class MakeMkvServiceTests
{
    private static readonly string FakeMkv = @"C:\mkv\title_t00.mkv";

    private MakeMkvService Build(IEnumerable<string> lines, int exitCode = 0, bool cancelled = false,
        string[]? existingFiles = null)
    {
        var runner = new FakeProcessRunner(lines, exitCode, cancelled);
        var fs = new FakeFileSystem(existingFiles ?? [FakeMkv]);
        return new MakeMkvService(runner, new FakeRipEngineSettings(), fs);
    }

    // ── Disk parsing ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ScanDisk_ParsesCInfoType()
    {
        var svc = Build(["CINFO:1,0,\"BD\""]);
        var (disk, _) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal("BD", disk.Type);
    }

    [Fact]
    public async Task ScanDisk_ParsesCInfoName()
    {
        var svc = Build(["CINFO:2,0,\"INTERSTELLAR\""]);
        var (disk, _) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal("INTERSTELLAR", disk.Name);
    }

    [Fact]
    public async Task ScanDisk_ParsesCInfoAlternateName()
    {
        var svc = Build(["CINFO:30,0,\"Interstellar\""]);
        var (disk, _) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal("Interstellar", disk.AlternateName);
    }

    [Fact]
    public async Task ScanDisk_DiskIdIsPassedDiskNumber()
    {
        var svc = Build([]);
        var (disk, _) = await svc.ScanDiskAsync("1", null, _ => { }, default);
        Assert.Equal("1", disk.Id);
    }

    // ── Title parsing ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ScanDisk_ParsesTitleName()
    {
        var svc = Build(["TINFO:0,2,0,\"Title 1\""]);
        var (_, titles) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal("Title 1", titles[0].Name);
    }

    [Fact]
    public async Task ScanDisk_ParsesTitleChapters()
    {
        var svc = Build(["TINFO:0,8,0,\"24\""]);
        var (_, titles) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal(24, titles[0].Chapters);
    }

    [Fact]
    public async Task ScanDisk_ParsesTitleDuration()
    {
        var svc = Build(["TINFO:0,9,0,\"2:15:30\""]);
        var (_, titles) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal(new TimeSpan(2, 15, 30), titles[0].Duration);
    }

    [Fact]
    public async Task ScanDisk_ParsesTitleSizeInBytes()
    {
        var svc = Build(["TINFO:0,11,0,\"4294967296\""]);
        var (_, titles) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal(4294967296L, titles[0].SizeInBytes);
    }

    [Fact]
    public async Task ScanDisk_ParsesTitleFileName()
    {
        var svc = Build(["TINFO:0,27,0,\"title_t00.mkv\""]);
        var (_, titles) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal("title_t00.mkv", titles[0].FileName);
    }

    [Fact]
    public async Task ScanDisk_MultipleTitles_OrderedById()
    {
        var lines = new[]
        {
            "TINFO:2,2,0,\"Title C\"",
            "TINFO:0,2,0,\"Title A\"",
            "TINFO:1,2,0,\"Title B\"",
        };
        var svc = Build(lines);
        var (_, titles) = await svc.ScanDiskAsync("0", null, _ => { }, default);
        Assert.Equal(new[] { 0, 1, 2 }, titles.Select(t => t.Id).ToArray());
        Assert.Equal("Title A", titles[0].Name);
        Assert.Equal("Title C", titles[2].Name);
    }

    // ── Progress reporting ───────────────────────────────────────────────────

    [Fact]
    public async Task ScanDisk_ReportsPrgvProgressAsPercentage()
    {
        var lines = new[] { "PRGV:500,750,1000" };
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build(lines);

        await svc.ScanDiskAsync("0", progress, _ => { }, default);
        // Give Progress<T> callbacks time to fire (they post to synchronisation context)
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(75, reports[0].Percent); // 750/1000 * 100
    }

    [Fact]
    public async Task ScanDisk_ZeroMaxProgress_ReportsZero()
    {
        var lines = new[] { "PRGV:0,0,0" };
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build(lines);

        await svc.ScanDiskAsync("0", progress, _ => { }, default);
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(0, reports[0].Percent);
    }

    // ── MSG lines ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScanDisk_MsgWithErrorFlag_PrefixesLog()
    {
        var lines = new[] { "MSG:1234,8,1,\"Something went wrong\"" };
        var logged = new List<string>();
        var svc = Build(lines);

        await svc.ScanDiskAsync("0", null, logged.Add, default);

        Assert.Contains(logged, l => l.StartsWith("[ERROR]"));
    }

    [Fact]
    public async Task ScanDisk_MsgWithWarnFlag_PrefixesLog()
    {
        var lines = new[] { "MSG:1234,4,1,\"Watch out\"" };
        var logged = new List<string>();
        var svc = Build(lines);

        await svc.ScanDiskAsync("0", null, logged.Add, default);

        Assert.Contains(logged, l => l.StartsWith("[WARN]"));
    }

    [Fact]
    public async Task ScanDisk_MsgNoSpecialFlag_NoPrefix()
    {
        var lines = new[] { "MSG:1234,0,1,\"Just info\"" };
        var logged = new List<string>();
        var svc = Build(lines);

        await svc.ScanDiskAsync("0", null, logged.Add, default);

        Assert.Contains(logged, l => l == "Just info");
    }

    // ── Error / cancel cases ─────────────────────────────────────────────────

    [Fact]
    public async Task ScanDisk_NonZeroExitCode_Throws()
    {
        var svc = Build([], exitCode: 1);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ScanDiskAsync("0", null, _ => { }, default));
    }

    [Fact]
    public async Task ScanDisk_CancelledResult_ThrowsOperationCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var runner = new FakeProcessRunner([], 0, cancelled: true);
        var svc = new MakeMkvService(runner, new FakeRipEngineSettings(), new FakeFileSystem());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.ScanDiskAsync("0", null, _ => { }, cts.Token));
    }

    // ── RipTitleAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RipTitle_ReturnsMkvPathFromTinfo27()
    {
        var lines = new[] { "TINFO:0,27,0,\"title_t00.mkv\"" };
        var svc = Build(lines, existingFiles: [$@"C:\tmp\title_t00.mkv"]);
        var result = await svc.RipTitleAsync("0", "0", @"C:\tmp", null, _ => { }, default);
        Assert.Equal(@"C:\tmp\title_t00.mkv", result);
    }

    [Fact]
    public async Task RipTitle_NoTinfo27_ThrowsInvalidOperation()
    {
        var svc = Build([]);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RipTitleAsync("0", "0", @"C:\tmp", null, _ => { }, default));
    }

    [Fact]
    public async Task RipTitle_FileNotFound_ThrowsFileNotFoundException()
    {
        var lines = new[] { "TINFO:0,27,0,\"title_t00.mkv\"" };
        // FakeFileSystem has no existing files — FileExists returns false
        var svc = Build(lines, existingFiles: []);
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => svc.RipTitleAsync("0", "0", @"C:\tmp", null, _ => { }, default));
    }

    [Fact]
    public async Task RipTitle_CancelledResult_ThrowsOperationCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var runner = new FakeProcessRunner([], 0, cancelled: true);
        var svc = new MakeMkvService(runner, new FakeRipEngineSettings(), new FakeFileSystem());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.RipTitleAsync("0", "0", @"C:\tmp", null, _ => { }, cts.Token));
    }
}
