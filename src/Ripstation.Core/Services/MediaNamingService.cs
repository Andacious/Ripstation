using System.Globalization;
using System.Text.RegularExpressions;

namespace Ripstation.Services;

public partial class MediaNamingService : IMediaNamingService
{
    [GeneratedRegex(@"[^\w\s\-\.]")]
    private static partial Regex InvalidCharsRegex();

    public string GetTitleFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return fileName;

        // Remove characters that are invalid in file or path names
        var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var pattern = "[" + Regex.Escape(invalid) + "]";
        var safe = Regex.Replace(fileName, pattern, string.Empty);

        var spaced = safe.Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToLowerInvariant());
    }

    public string GetMediaFilePath(string outputPath, string mediaName, int season, int episode)
    {
        if (season > 0 && episode > 0)
        {
            var seasonNumber = season.ToString("D2");
            var episodeNumber = episode.ToString("D2");
            var dir = Path.Combine(outputPath, mediaName, $"Season {seasonNumber}");
            var file = $"{mediaName} - s{seasonNumber}e{episodeNumber}.m4v";
            return Path.Combine(dir, file);
        }

        return Path.Combine(outputPath, $"{mediaName}.m4v");
    }
}
