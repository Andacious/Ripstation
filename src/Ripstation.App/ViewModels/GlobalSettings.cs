using CommunityToolkit.Mvvm.ComponentModel;

namespace Ripstation.ViewModels;

/// <summary>
/// Settings shared across all drive rip sessions. Captured as a snapshot at
/// the start of each operation so mid-run edits never affect an active rip.
/// </summary>
public partial class GlobalSettings : ObservableObject
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
    private string _intermediatePath = @"S:\MKV";

    [ObservableProperty]
    private string _outputPath = @"S:\Plex\Media\Movies";

    /// <summary>Immutable snapshot of current values for use during an operation.</summary>
    public SettingsSnapshot Snapshot() => new(
        MakeMkvExePath, HandBrakeExePath, PresetFilePath, PresetName,
        IntermediatePath, OutputPath);
}

public sealed record SettingsSnapshot(
    string MakeMkvExePath,
    string HandBrakeExePath,
    string PresetFilePath,
    string PresetName,
    string IntermediatePath,
    string OutputPath);
