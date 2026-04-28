using Ripstation.Core.Tests.Helpers;
using Ripstation.Services;

namespace Ripstation.Core.Tests.Services;

public class HandBrakeServiceTests
{
    private static readonly string OutputFile = @"C:\output\movie.m4v";

    private HandBrakeService Build(IEnumerable<string> lines, int exitCode = 0,
        bool cancelled = false, bool outputExists = true)
    {
        var runner = new FakeProcessRunner(lines, exitCode, cancelled);
        var fs = new FakeFileSystem(outputExists ? [OutputFile] : []);
        return new HandBrakeService(runner, new FakeRipEngineSettings(), fs);
    }

    // ── Progress reporting ───────────────────────────────────────────────────

    [Fact]
    public async Task Convert_WorkingState_ReportsPercentProgress()
    {
        var line = "{\"State\":\"WORKING\",\"Working\":{\"Progress\":0.5}}";
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build([line], outputExists: true);

        await svc.ConvertVideoAsync("in.mkv", OutputFile, progress, _ => { }, default);
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(50, reports[0].Percent);
        Assert.Equal("Encoding…", reports[0].Status);
    }

    [Fact]
    public async Task Convert_ScanningState_ReportsZeroProgress()
    {
        var line = "{\"State\":\"SCANNING\"}";
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build([line], outputExists: true);

        await svc.ConvertVideoAsync("in.mkv", OutputFile, progress, _ => { }, default);
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(0, reports[0].Percent);
        Assert.Equal("Scanning source…", reports[0].Status);
    }

    [Fact]
    public async Task Convert_MuxingState_Reports99Percent()
    {
        var line = "{\"State\":\"MUXING\"}";
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build([line], outputExists: true);

        await svc.ConvertVideoAsync("in.mkv", OutputFile, progress, _ => { }, default);
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(99, reports[0].Percent);
    }

    [Fact]
    public async Task Convert_NonJsonLine_NotReported()
    {
        var line = "HandBrake: some plain text line";
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build([line], outputExists: true);

        await svc.ConvertVideoAsync("in.mkv", OutputFile, progress, _ => { }, default);
        await Task.Yield();

        Assert.Empty(reports);
    }

    [Fact]
    public async Task Convert_WorkingAtZero_ReportsZeroPercent()
    {
        var line = "{\"State\":\"WORKING\",\"Working\":{\"Progress\":0.0}}";
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build([line], outputExists: true);

        await svc.ConvertVideoAsync("in.mkv", OutputFile, progress, _ => { }, default);
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(0, reports[0].Percent);
    }

    [Fact]
    public async Task Convert_WorkingAt100_Reports100Percent()
    {
        var line = "{\"State\":\"WORKING\",\"Working\":{\"Progress\":1.0}}";
        var reports = new List<(int Percent, string Status)>();
        var progress = new Progress<(int Percent, string Status)>(r => reports.Add(r));
        var svc = Build([line], outputExists: true);

        await svc.ConvertVideoAsync("in.mkv", OutputFile, progress, _ => { }, default);
        await Task.Yield();

        Assert.Single(reports);
        Assert.Equal(100, reports[0].Percent);
    }

    // ── Error cases ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Convert_NonZeroExitCode_Throws()
    {
        var svc = Build([], exitCode: 1);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ConvertVideoAsync("in.mkv", OutputFile, null, _ => { }, default));
    }

    [Fact]
    public async Task Convert_OutputFileMissing_Throws()
    {
        var svc = Build([], outputExists: false);
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => svc.ConvertVideoAsync("in.mkv", OutputFile, null, _ => { }, default));
    }

    [Fact]
    public async Task Convert_Cancelled_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var runner = new FakeProcessRunner([], 0, cancelled: true);
        var svc = new HandBrakeService(runner, new FakeRipEngineSettings(), new FakeFileSystem());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.ConvertVideoAsync("in.mkv", OutputFile, null, _ => { }, cts.Token));
    }
}
