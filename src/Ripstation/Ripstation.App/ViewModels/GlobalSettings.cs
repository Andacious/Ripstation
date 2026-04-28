using CommunityToolkit.Mvvm.ComponentModel;
using Ripstation.Services;

#pragma warning disable MVVMTK0045 // Use partial property — AOT compat only; reflection bindings work fine

namespace Ripstation.ViewModels;

/// <summary>
/// Settings shared across all drive rip sessions. Captured as a snapshot at
/// the start of each operation so mid-run edits never affect an active rip.
/// </summary>
public partial class GlobalSettings : ObservableObject, IRipEngineSettings
{
    [ObservableProperty]
    private string _makeMkvExePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "MakeMKV", "makemkvcon64.exe");

    [ObservableProperty]
    private string _handBrakeExePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "HandBrakeCLI", "HandBrakeCLI.exe");

    [ObservableProperty]
    private string _presetFilePath =
        Path.Combine(AppContext.BaseDirectory, "presets", "Plex.json");

    [ObservableProperty]
    private string _presetName = "Plex";

    [ObservableProperty]
    private string _intermediatePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ripstation", "Intermediate");

    [ObservableProperty]
    private string _outputPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "Ripstation");

    // IRipEngineSettings — maps observable properties to the interface
    string IRipEngineSettings.RipperExecutablePath  => MakeMkvExePath;
    string IRipEngineSettings.EncoderExecutablePath => HandBrakeExePath;
    string IRipEngineSettings.EncoderPresetName     => PresetName;
    string IRipEngineSettings.EncoderPresetFilePath => PresetFilePath;

    /// <summary>Immutable snapshot of path settings for use during an operation.</summary>
    public SettingsSnapshot Snapshot() => new(IntermediatePath, OutputPath);
}

public sealed record SettingsSnapshot(
    string IntermediatePath,
    string OutputPath);
