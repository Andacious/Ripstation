namespace Ripstation.Services;

public interface IHandBrakeService
{
    /// <summary>
    /// Transcodes a video file using the configured encoder and preset.
    /// </summary>
    Task ConvertVideoAsync(
        string inputFile,
        string outputFile,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct);
}
