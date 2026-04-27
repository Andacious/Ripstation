using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ripstation.Services;

namespace Ripstation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMakeMkvService _makeMkv;
    private readonly IHandBrakeService _handBrake;
    private readonly IMediaNamingService _naming;
    private readonly IDriveService _driveService;
    private readonly IUiDispatcher _dispatcher;

    private CancellationTokenSource? _ripAllCts;

    private const int MaxLogLines = 500;

    // ── Shared settings ───────────────────────────────────────────────────────

    public GlobalSettings Settings { get; } = new();

    // ── Drives ────────────────────────────────────────────────────────────────

    public ObservableCollection<DriveViewModel> Drives { get; } = [];

    public string StatusText =>
        $"{Drives.Count} drive(s)  ·  {Settings.OutputPath}";

    // ── Rip All state ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelAllCommand))]
    private bool _isRippingAll;

    // ── Log ───────────────────────────────────────────────────────────────────

    public ObservableCollection<string> LogLines { get; } = [];

    private readonly StringBuilder _logBuilder = new();

    [ObservableProperty]
    private string _logText = string.Empty;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainViewModel(
        IMakeMkvService makeMkv,
        IHandBrakeService handBrake,
        IMediaNamingService naming,
        IDriveService driveService,
        IUiDispatcher? dispatcher = null)
    {
        _makeMkv = makeMkv;
        _handBrake = handBrake;
        _naming = naming;
        _driveService = driveService;
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        Drives.CollectionChanged += (_, _) => OnPropertyChanged(nameof(StatusText));
        Settings.PropertyChanged += (_, _) => OnPropertyChanged(nameof(StatusText));

        // Populate drives from attached optical drives; fall back to two defaults
        var detected = _driveService.GetOpticalDrives();
        if (detected.Count > 0)
        {
            foreach (var (idx, path) in detected)
                Drives.Add(CreateDrive(idx, path));
        }
        else
        {
            Drives.Add(CreateDrive(0));
            Drives.Add(CreateDrive(1));
        }
    }

    // ── Drive management ──────────────────────────────────────────────────────

    private DriveViewModel CreateDrive(int diskNumber, string driveLetter = "")
    {
        return new DriveViewModel(
            diskNumber, Settings,
            _makeMkv, _handBrake, _naming, _driveService,
            LogFromThread, driveLetter);
    }

    [RelayCommand]
    private void AddDrive()
    {
        int next = Drives.Count > 0 ? Drives.Max(d => d.DiskNumber) + 1 : 0;
        Drives.Add(CreateDrive(next));
    }

    [RelayCommand]
    private void RemoveDrive(DriveViewModel drive)
    {
        if (drive is null) return;
        drive.CancelCurrentOperation();
        Drives.Remove(drive);
    }

    [RelayCommand]
    private void DetectDrives()
    {
        var detected = _driveService.GetOpticalDrives();
        if (detected.Count == 0)
        {
            LogFromThread("No optical drives detected.");
            return;
        }

        // Non-destructive: add drives not already represented
        var existingIndices = Drives.Select(d => d.DiskNumber).ToHashSet();
        int added = 0;
        foreach (var (idx, path) in detected)
        {
            if (existingIndices.Contains(idx)) continue;
            Drives.Add(CreateDrive(idx, path));
            added++;
        }

        LogFromThread(added > 0
            ? $"Detected {detected.Count} optical drive(s); added {added} new."
            : $"Detected {detected.Count} optical drive(s) — all already present.");
    }

    // ── Rip All ───────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRipAll))]
    private async Task RipAllAsync()
    {
        // Gather drives that have selected titles
        var eligible = Drives.Where(d => d.SelectedTitleCount > 0).ToList();
        if (eligible.Count == 0)
        {
            LogFromThread("No drives have selected titles.");
            return;
        }

        // Collision check: ensure no two drives would write to the same file
        var allPaths = eligible.SelectMany(d => d.GetPlannedOutputPaths()).ToList();
        var duplicates = allPaths
            .GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            LogFromThread("Output path collision detected — cannot Rip All:");
            foreach (var dup in duplicates)
                LogFromThread($"  • {dup}");
            return;
        }

        IsRippingAll = true;
        _ripAllCts = new CancellationTokenSource();

        try
        {
            LogFromThread($"Starting parallel rip on {eligible.Count} drive(s)…");
            var tasks = eligible.Select(d => d.RipAsync(_ripAllCts.Token)).ToList();
            await Task.WhenAll(tasks);
            LogFromThread("All drives finished.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFromThread($"Rip All encountered an error: {ex.Message}");
        }
        finally
        {
            IsRippingAll = false;
            _ripAllCts?.Dispose();
            _ripAllCts = null;
        }
    }

    private bool CanRipAll() => !IsRippingAll && Drives.Any(d => d.SelectedTitleCount > 0);

    [RelayCommand(CanExecute = nameof(CanCancelAll))]
    private void CancelAll()
    {
        _ripAllCts?.Cancel();
        foreach (var d in Drives)
            d.CancelCurrentOperation();
    }

    private bool CanCancelAll() => IsRippingAll;

    [RelayCommand]
    private void EjectAll()
    {
        foreach (var d in Drives)
            d.EjectCommand.Execute(null);
    }

    // ── Log ───────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ClearLog()
    {
        LogLines.Clear();
        _logBuilder.Clear();
        LogText = string.Empty;
    }

    /// <summary>
    /// Thread-safe log append, capped at MaxLogLines. Safe to call from
    /// background threads — dispatches to the UI thread without blocking.
    /// </summary>
    public void LogFromThread(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _dispatcher.Post(() =>
        {
            bool trimmed = false;
            while (LogLines.Count >= MaxLogLines)
            {
                LogLines.RemoveAt(0);
                trimmed = true;
            }
            LogLines.Add(line);
            if (trimmed)
            {
                _logBuilder.Clear();
                foreach (var l in LogLines) _logBuilder.AppendLine(l);
            }
            else
            {
                _logBuilder.AppendLine(line);
            }
            LogText = _logBuilder.ToString();
        });
    }
}
