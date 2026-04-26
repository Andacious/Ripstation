using Ripstation.Services;

namespace RipstationApp.Tests.Helpers;

/// <summary>
/// Fake IFileSystem that tracks which paths "exist" and which operations were called.
/// </summary>
public class FakeFileSystem : IFileSystem
{
    private readonly HashSet<string> _existing;

    public List<string> Deleted { get; } = [];
    public List<string> Created { get; } = [];

    public FakeFileSystem(params string[] existingPaths)
    {
        _existing = new HashSet<string>(existingPaths, StringComparer.OrdinalIgnoreCase);
    }

    public bool FileExists(string path) => _existing.Contains(path);
    public void FileDelete(string path) { _existing.Remove(path); Deleted.Add(path); }
    public void DirectoryCreate(string path) => Created.Add(path);
    // Directories always "exist" in tests — the OS path checks are an integration concern
    public bool DirectoryExists(string path) => true;
}

/// <summary>
/// Fake IUiDispatcher that runs actions synchronously (suitable for tests).
/// </summary>
public class SynchronousDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}

/// <summary>
/// Fake IDriveService with configurable optical drives.
/// </summary>
public class FakeDriveService : IDriveService
{
    private readonly IReadOnlyList<(int DiscIndex, string DrivePath)> _drives;
    public List<int> EjectedIndices { get; } = [];

    public FakeDriveService(params (int DiscIndex, string DrivePath)[] drives)
    {
        _drives = drives;
    }

    public void EjectDrive(int wmpCdRomIndex) => EjectedIndices.Add(wmpCdRomIndex);
    public IReadOnlyList<(int DiscIndex, string DrivePath)> GetOpticalDrives() => _drives;
    public string GetVolumeLabel(string drivePath) => string.Empty;
}
