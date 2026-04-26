using System.Text.RegularExpressions;

namespace Ripstation.Services;

public partial class HandBrakeService(IProcessRunner processRunner, IFileSystem? fileSystem = null) : IHandBrakeService
{
    private readonly IFileSystem _fs = fileSystem ?? new FileSystem();
    // HandBrake --json emits lines like:
    // {"State":"WORKING","Working":{"Progress":0.12345,...}}
    [GeneratedRegex(@"""Progress"":\s*([\d.]+)")]
    private static partial Regex ProgressRegex();

    [GeneratedRegex(@"""State""\s*:\s*""(?<State>[A-Z]+)""")]
    private static partial Regex StateRegex();

    public async Task ConvertVideoAsync(
        string inputFile,
        string outputFile,
        string presetName,
        string presetFile,
        string handBrakeExe,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct)
    {
        void HandleLine(string line)
        {
            if (progress == null || !line.StartsWith('{')) return;

            var stateMatch = StateRegex().Match(line);
            if (!stateMatch.Success) return;

            var state = stateMatch.Groups["State"].Value;
            var progressMatch = ProgressRegex().Match(line);

            if (state == "WORKING" && progressMatch.Success &&
                double.TryParse(progressMatch.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var pct))
            {
                progress.Report(((int)(pct * 100), "Encoding…"));
            }
            else if (state == "SCANNING")
            {
                progress.Report((0, "Scanning source…"));
            }
            else if (state == "MUXING")
            {
                progress.Report((99, "Muxing…"));
            }
        }

        var args = $"--preset-import-file \"{presetFile}\" -i \"{inputFile}\" -o \"{outputFile}\" --preset \"{presetName}\" --json";
        var result = await processRunner.RunAsync(handBrakeExe, args, HandleLine, log, ct);

        if (result.Cancelled) ct.ThrowIfCancellationRequested();
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"HandBrake exited with code {result.ExitCode}");
        if (!_fs.FileExists(outputFile))
            throw new FileNotFoundException($"HandBrake did not produce output: {outputFile}", outputFile);

        log($"HandBrake wrote: {outputFile}");
    }
}
