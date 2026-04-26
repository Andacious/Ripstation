namespace Ripstation.Services;

public interface IMediaNamingService
{
    /// <summary>
    /// Removes invalid path characters, replaces underscores with spaces,
    /// and title-cases the result — matching Get-TitleFileName.
    /// </summary>
    string GetTitleFileName(string fileName);

    /// <summary>
    /// Builds the full output file path.
    /// Movie:  OutputPath\Name.m4v
    /// TV:     OutputPath\Name\Season 01\Name - s01e05.m4v
    /// </summary>
    string GetMediaFilePath(string outputPath, string mediaName, int season, int episode);
}
