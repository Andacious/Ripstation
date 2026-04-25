namespace Ripstation.Services;

public record ProcessResult(int ExitCode, bool Cancelled);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string exe,
        string arguments,
        Action<string>? onStdout,
        Action<string>? onStderr,
        CancellationToken ct);
}
