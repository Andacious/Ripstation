using System.Text.RegularExpressions;
using Ripstation.Models;

namespace Ripstation.Services;

public partial class MakeMkvService(IProcessRunner processRunner, IRipEngineSettings settings, IFileSystem? fileSystem = null) : IMakeMkvService
{
    private readonly IFileSystem _fs = fileSystem ?? new FileSystem();
    // CINFO:code,flags,"value"
    [GeneratedRegex(@"^CINFO:(?<Code>\d+),(?<Flags>\d+),""(?<Value>.+)""$")]
    private static partial Regex CInfoRegex();

    // TINFO:id,code,flags,"value"
    [GeneratedRegex(@"^TINFO:(?<Id>\d+),(?<Code>\d+),(?<Flags>\d+),""(?<Value>.+)""$")]
    private static partial Regex TInfoRegex();

    // PRGV:current,total,max
    [GeneratedRegex(@"^PRGV:(?<Current>\d+),(?<Total>\d+),(?<Max>\d+)$")]
    private static partial Regex ProgressValueRegex();

    // PRGCT or PRGT: code,id,"name"
    [GeneratedRegex(@"^PRG[CT]:(?<Code>\d+),(?<Id>\d+),""(?<Name>.+)""$")]
    private static partial Regex ProgressTextRegex();

    // MSG:code,flags,count,"message",...
    [GeneratedRegex(@"^MSG:(?<Code>\d+),(?<Flags>\d+),\d+,""(?<Message>[^""]+)""")]
    private static partial Regex MsgRegex();

    public async Task<(Disk Disk, List<Title> Titles)> ScanDiskAsync(
        string diskNumber,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct)
    {
        var diskInfo = new Dictionary<string, string>();
        var titleInfo = new Dictionary<string, Dictionary<string, string>>();
        string lastStatus = string.Empty;

        void HandleLine(string line)
        {
            var cinfo = CInfoRegex().Match(line);
            if (cinfo.Success)
            {
                diskInfo[cinfo.Groups["Code"].Value] = cinfo.Groups["Value"].Value;
                return;
            }

            var tinfo = TInfoRegex().Match(line);
            if (tinfo.Success)
            {
                var id = tinfo.Groups["Id"].Value;
                if (!titleInfo.TryGetValue(id, out var dict))
                    titleInfo[id] = dict = new Dictionary<string, string>();
                dict[tinfo.Groups["Code"].Value] = tinfo.Groups["Value"].Value;
                return;
            }

            var prgv = ProgressValueRegex().Match(line);
            if (prgv.Success && progress != null)
            {
                var total = int.Parse(prgv.Groups["Total"].Value);
                var max = int.Parse(prgv.Groups["Max"].Value);
                var pct = max > 0 ? (int)((double)total / max * 100) : 0;
                progress.Report((pct, lastStatus));
                return;
            }

            var prgt = ProgressTextRegex().Match(line);
            if (prgt.Success)
            {
                lastStatus = prgt.Groups["Name"].Value;
                return;
            }

            var msg = MsgRegex().Match(line);
            if (msg.Success)
            {
                var flags = int.Parse(msg.Groups["Flags"].Value);
                var prefix = (flags & 8) != 0 ? "[ERROR] " : (flags & 4) != 0 ? "[WARN] " : string.Empty;
                log($"{prefix}{msg.Groups["Message"].Value}");
            }
        }

        var args = $"--robot --cache=1024 --messages=-stdout --progress=-same --minlength=600 info disc:{diskNumber}";
        var result = await processRunner.RunAsync(settings.RipperExecutablePath, args, HandleLine, onStderr: null, ct);

        if (result.Cancelled) ct.ThrowIfCancellationRequested();
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"MakeMKV exited with code {result.ExitCode}");

        var disk = BuildDisk(diskNumber, diskInfo);
        var titles = titleInfo
            .Select(kvp => BuildTitle(kvp.Key, kvp.Value))
            .OrderBy(t => t.Id)
            .ToList();

        return (disk, titles);
    }

    public async Task<string> RipTitleAsync(
        string titleId,
        string diskNumber,
        string outputPath,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct)
    {
        string? writtenFile = null;
        string lastStatus = string.Empty;

        void HandleLine(string line)
        {
            var prgv = ProgressValueRegex().Match(line);
            if (prgv.Success && progress != null)
            {
                var total = int.Parse(prgv.Groups["Total"].Value);
                var max = int.Parse(prgv.Groups["Max"].Value);
                var pct = max > 0 ? (int)((double)total / max * 100) : 0;
                progress.Report((pct, lastStatus));
                return;
            }

            var prgt = ProgressTextRegex().Match(line);
            if (prgt.Success)
            {
                lastStatus = prgt.Groups["Name"].Value;
                return;
            }

            // Track any TINFO filename code 27 updates during rip
            var tinfo = TInfoRegex().Match(line);
            if (tinfo.Success && tinfo.Groups["Code"].Value == "27")
            {
                writtenFile = Path.Combine(outputPath, tinfo.Groups["Value"].Value);
                return;
            }

            var msg = MsgRegex().Match(line);
            if (msg.Success)
            {
                var flags = int.Parse(msg.Groups["Flags"].Value);
                var prefix = (flags & 8) != 0 ? "[ERROR] " : (flags & 4) != 0 ? "[WARN] " : string.Empty;
                log($"{prefix}{msg.Groups["Message"].Value}");
            }
        }

        var args = $"--robot --noscan --cache=1024 --messages=-stdout --progress=-same --minlength=600 mkv disc:{diskNumber} {titleId} \"{outputPath}\"";
        var result = await processRunner.RunAsync(settings.RipperExecutablePath, args, HandleLine, onStderr: null, ct);

        if (result.Cancelled) ct.ThrowIfCancellationRequested();
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"MakeMKV exited with code {result.ExitCode}");

        if (writtenFile is null)
            throw new InvalidOperationException("MakeMKV did not report an output filename");

        if (!_fs.FileExists(writtenFile))
            throw new FileNotFoundException($"Expected MKV not found: {writtenFile}", writtenFile);

        log($"MakeMKV wrote: {writtenFile}");
        return writtenFile;
    }

    private static Disk BuildDisk(string diskNumber, Dictionary<string, string> info) => new()
    {
        Id = diskNumber,
        Type = info.GetValueOrDefault("1", string.Empty),
        Name = info.GetValueOrDefault("2", string.Empty),
        AlternateName = info.GetValueOrDefault("30", string.Empty),
    };

    private static Title BuildTitle(string id, Dictionary<string, string> info)
    {
        TimeSpan duration = TimeSpan.Zero;
        if (info.TryGetValue("9", out var durStr) && !string.IsNullOrEmpty(durStr))
            TimeSpan.TryParse(durStr, out duration);

        long.TryParse(info.GetValueOrDefault("11", "0"), out var sizeBytes);
        int.TryParse(info.GetValueOrDefault("8", "0"), out var chapters);
        int.TryParse(id, out var titleId);

        return new Title
        {
            Id = titleId,
            Name = info.GetValueOrDefault("2", string.Empty),
            FileName = info.GetValueOrDefault("27", string.Empty),
            Chapters = chapters,
            Duration = duration,
            SizeInBytes = sizeBytes,
        };
    }
}
