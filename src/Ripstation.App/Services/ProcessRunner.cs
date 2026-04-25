using System.Diagnostics;

namespace Ripstation.Services;

public class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string exe,
        string arguments,
        Action<string>? onStdout,
        Action<string>? onStderr,
        CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) onStdout?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) onStderr?.Invoke(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            await process.WaitForExitAsync(CancellationToken.None);
            return new ProcessResult(process.ExitCode, Cancelled: true);
        }

        return new ProcessResult(process.ExitCode, Cancelled: false);
    }
}
