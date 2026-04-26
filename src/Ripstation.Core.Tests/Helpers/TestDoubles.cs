using Ripstation.Services;

namespace Ripstation.Core.Tests.Helpers;

/// <summary>
/// Fake IProcessRunner that lets tests specify lines to deliver on stdout and
/// a controlled exit code. Calls the stdout handler synchronously for each line.
/// </summary>
public class FakeProcessRunner : IProcessRunner
{
    private readonly IReadOnlyList<string> _stdoutLines;
    private readonly int _exitCode;
    private readonly bool _cancelled;

    public FakeProcessRunner(IEnumerable<string> stdoutLines, int exitCode = 0, bool cancelled = false)
    {
        _stdoutLines = stdoutLines.ToList();
        _exitCode = exitCode;
        _cancelled = cancelled;
    }

    public Task<ProcessResult> RunAsync(
        string exe,
        string arguments,
        Action<string>? onStdout,
        Action<string>? onStderr,
        CancellationToken ct)
    {
        foreach (var line in _stdoutLines)
            onStdout?.Invoke(line);

        return Task.FromResult(new ProcessResult(_exitCode, _cancelled));
    }
}

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
