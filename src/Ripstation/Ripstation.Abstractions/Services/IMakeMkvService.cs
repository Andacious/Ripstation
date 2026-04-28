using Ripstation.Models;

namespace Ripstation.Services;

public interface IMakeMkvService
{
    /// <summary>
    /// Scans a disc and returns the disc info plus all title metadata.
    /// </summary>
    Task<(Disk Disk, List<Title> Titles)> ScanDiskAsync(
        string diskNumber,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct);

    /// <summary>
    /// Rips a single title from a disc to an intermediate MKV file.
    /// Returns the full path to the written MKV file.
    /// </summary>
    Task<string> RipTitleAsync(
        string titleId,
        string diskNumber,
        string outputPath,
        IProgress<(int Percent, string Status)>? progress,
        Action<string> log,
        CancellationToken ct);
}
