namespace Ripstation.Services;

public interface IHandBrakeService
{
    /// <summary>
    /// Transcodes a video file to M4V using HandBrakeCLI with the given preset.
    /// </summary>
    Task ConvertVideoAsync(
        string inputFile,
        string outputFile,
        string presetName,
        string presetFile,
        string handBrakeExe,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct);
}
