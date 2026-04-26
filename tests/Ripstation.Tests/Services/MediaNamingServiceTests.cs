using Ripstation.Services;

namespace Ripstation.Tests.Services;

public class MediaNamingServiceTests
{
    private readonly MediaNamingService _sut = new();

    // ── GetTitleFileName ────────────────────────────────────────────────────

    [Fact]
    public void GetTitleFileName_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.GetTitleFileName(string.Empty));
    }

    [Fact]
    public void GetTitleFileName_BasicName_TitleCases()
    {
        Assert.Equal("The Dark Knight", _sut.GetTitleFileName("the dark knight"));
    }

    [Fact]
    public void GetTitleFileName_UnderscoresReplacedWithSpaces()
    {
        Assert.Equal("The Dark Knight", _sut.GetTitleFileName("the_dark_knight"));
    }

    [Fact]
    public void GetTitleFileName_AlreadyTitleCased_Unchanged()
    {
        var result = _sut.GetTitleFileName("Interstellar");
        Assert.Equal("Interstellar", result);
    }

    [Fact]
    public void GetTitleFileName_RemovesInvalidFileNameChars()
    {
        // colons, angle brackets, pipes are invalid
        var result = _sut.GetTitleFileName("movie:title<version>|cut");
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain("|", result);
    }

    [Fact]
    public void GetTitleFileName_MixedCase_NormalizedToTitleCase()
    {
        Assert.Equal("Blade Runner 2049", _sut.GetTitleFileName("BLADE RUNNER 2049"));
    }

    // ── GetMediaFilePath ────────────────────────────────────────────────────

    [Fact]
    public void GetMediaFilePath_Movie_ReturnsDirectM4vInOutputPath()
    {
        var path = _sut.GetMediaFilePath(@"C:\Output", "Inception", season: 0, episode: 0);
        Assert.Equal(@"C:\Output\Inception.m4v", path);
    }

    [Fact]
    public void GetMediaFilePath_MovieWithSeasonZero_EpisodeIgnored()
    {
        var path = _sut.GetMediaFilePath(@"C:\Output", "Inception", season: 0, episode: 5);
        Assert.Equal(@"C:\Output\Inception.m4v", path);
    }

    [Fact]
    public void GetMediaFilePath_TvShow_ReturnsSeasonEpisodeFormat()
    {
        var path = _sut.GetMediaFilePath(@"C:\Output", "Breaking Bad", season: 1, episode: 1);
        Assert.Equal(@"C:\Output\Breaking Bad\Season 01\Breaking Bad - s01e01.m4v", path);
    }

    [Fact]
    public void GetMediaFilePath_TvShow_PadsSeasonAndEpisode()
    {
        var path = _sut.GetMediaFilePath(@"C:\Output", "The Wire", season: 3, episode: 12);
        Assert.Equal(@"C:\Output\The Wire\Season 03\The Wire - s03e12.m4v", path);
    }

    [Fact]
    public void GetMediaFilePath_TvShow_Season12Episode9_Padded()
    {
        var path = _sut.GetMediaFilePath(@"C:\Output", "Show", season: 12, episode: 9);
        Assert.Equal(@"C:\Output\Show\Season 12\Show - s12e09.m4v", path);
    }

    [Fact]
    public void GetMediaFilePath_TvShow_LargeEpisodeNumber()
    {
        var path = _sut.GetMediaFilePath(@"C:\Output", "Show", season: 1, episode: 24);
        Assert.Equal(@"C:\Output\Show\Season 01\Show - s01e24.m4v", path);
    }
}
