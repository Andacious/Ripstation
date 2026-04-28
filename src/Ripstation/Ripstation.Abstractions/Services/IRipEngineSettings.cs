namespace Ripstation.Services;

/// <summary>
/// Tool-path and encoding configuration consumed by the rip and encode services.
/// Implementations supply values from user settings or test stubs.
/// </summary>
public interface IRipEngineSettings
{
    /// <summary>Full path to the disc-ripping executable.</summary>
    string RipperExecutablePath { get; }

    /// <summary>Full path to the video-encoding executable.</summary>
    string EncoderExecutablePath { get; }

    /// <summary>Name of the encoding preset to use.</summary>
    string EncoderPresetName { get; }

    /// <summary>Full path to the encoding preset file.</summary>
    string EncoderPresetFilePath { get; }
}
