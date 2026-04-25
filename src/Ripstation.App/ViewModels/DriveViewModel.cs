using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ripstation.Services;

namespace Ripstation.ViewModels;

public partial class DriveViewModel : ObservableObject
{
    private readonly GlobalSettings _settings;
    private readonly IMakeMkvService _makeMkv;
    private readonly IHandBrakeService _handBrake;
    private readonly IMediaNamingService _naming;
    private readonly IDriveService _driveService;
    private readonly Action<string> _sharedLog;

    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _ripCts;

    // Active rip task — returned to RipAll so Task.WhenAll can track it
    private Task _activeRipTask = Task.CompletedTask;

    // ── Disc identity ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabHeader))]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private int _diskNumber;

    /// <summary>
    /// WMP CD-ROM index used for ejection (usually same as DiskNumber but
    /// may differ on some systems).
    /// </summary>
    [ObservableProperty]
    private int _wmpDriveIndex;

    /// <summary>Drive letter from OS detection (display only, e.g. "D:\").</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabHeader))]
    private string _driveLetter = string.Empty;

    // ── Media settings ─────────────────────────────────────────────────────────

    [ObservableProperty] private string _mediaName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyPropertyChangedFor(nameof(IsTvShow))]
    private bool _isTvMode;

    public bool IsTvShow => IsTvMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    private int _season = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyPropertyChangedFor(nameof(EpisodeCount))]
    private int _episodeStart = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyPropertyChangedFor(nameof(EpisodeCount))]
    private int _episodeEnd = 1;

    public int EpisodeCount => Math.Max(0, EpisodeEnd - EpisodeStart + 1);

    [ObservableProperty] private bool _ejectWhenDone;

    // ── Scan state ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isScanning;

    [ObservableProperty] private int _scanProgress;

    [ObservableProperty] private string _scanStatus = string.Empty;

    public bool IsScanStatusVisible => !string.IsNullOrEmpty(ScanStatus);
    partial void OnScanStatusChanged(string value) => OnPropertyChanged(nameof(IsScanStatusVisible));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabHeader))]
    [NotifyPropertyChangedFor(nameof(IsDiscNameVisible))]
    private string _discName = string.Empty;

    public bool IsDiscNameVisible => !string.IsNullOrEmpty(DiscName);

    // ── Rip state ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isRipping;

    [ObservableProperty] private int _ripProgress;
    [ObservableProperty] private string _ripStatus = string.Empty;

    // ── Titles ─────────────────────────────────────────────────────────────────

    public ObservableCollection<TitleViewModel> Titles { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    private int _selectedTitleCount;

    public bool HasSelectedTitles => SelectedTitleCount > 0;
    partial void OnSelectedTitleCountChanged(int value) => OnPropertyChanged(nameof(HasSelectedTitles));

    // ── Derived ────────────────────────────────────────────────────────────────

    public string TabHeader
    {
        get
        {
            var driveId = string.IsNullOrEmpty(DriveLetter)
                ? $"disc:{DiskNumber}"
                : $"disc:{DiskNumber} ({DriveLetter.TrimEnd('\\', '/')})";

            return string.IsNullOrEmpty(DiscName)
                ? $"Drive — {driveId}"
                : $"Drive — {DiscName}";
        }
    }

    private bool IsIdle => !IsScanning && !IsRipping;

    // ── Constructor ────────────────────────────────────────────────────────────

    public DriveViewModel(
        int diskNumber,
        GlobalSettings settings,
        IMakeMkvService makeMkv,
        IHandBrakeService handBrake,
        IMediaNamingService naming,
        IDriveService driveService,
        Action<string> sharedLog,
        string driveLetter = "")
    {
        _diskNumber = diskNumber;
        _wmpDriveIndex = diskNumber;
        _driveLetter = driveLetter;
        _settings = settings;
        _makeMkv = makeMkv;
        _handBrake = handBrake;
        _naming = naming;
        _driveService = driveService;
        _sharedLog = sharedLog;

        // Refresh CanExecute when global settings change (e.g. user changes HandBrake path)
        _settings.PropertyChanged += (_, _) =>
        {
            ScanCommand.NotifyCanExecuteChanged();
            RipStandaloneCommand.NotifyCanExecuteChanged();
        };
    }

    // ── Scan command ───────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsScanning = true;
        ScanProgress = 0;
        ScanStatus = "Scanning…";
        DiscName = string.Empty;
        Titles.Clear();
        SelectedTitleCount = 0;
        _scanCts = new CancellationTokenSource();

        var makeMkvExe = _settings.MakeMkvExePath; // snapshot before async

        var progress = new Progress<(int Percent, string Status)>(p =>
        {
            ScanProgress = p.Percent;
            ScanStatus = p.Status;
        });

        try
        {
            Log($"Scanning disc {DiskNumber}…");
            var (disk, titles) = await _makeMkv.ScanDiskAsync(
                DiskNumber.ToString(), makeMkvExe, progress, Log, _scanCts.Token);

            DiscName = disk.Name;
            foreach (var t in titles)
            {
                var vm = new TitleViewModel(t);
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(TitleViewModel.IsSelected))
                        RefreshSelectedCount();
                };
                Titles.Add(vm);
            }

            Log($"Found {titles.Count} title(s) on disc {DiskNumber} — {disk.Name}");
            ScanStatus = $"{titles.Count} title(s) found";
            ScanProgress = 100;
        }
        catch (OperationCanceledException)
        {
            Log($"Scan cancelled (disc {DiskNumber}).");
            ScanStatus = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"Scan failed (disc {DiskNumber}): {ex.Message}");
            ScanStatus = "Failed";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private bool CanScan() => IsIdle && !string.IsNullOrWhiteSpace(_settings.MakeMkvExePath);

    // ── Rip commands ───────────────────────────────────────────────────────────

    /// <summary>
    /// Per-drive Rip button — starts a standalone rip with no external cancellation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRip))]
    private Task RipStandaloneAsync() => RipAsync(CancellationToken.None);

    /// <summary>
    /// Called by MainViewModel.RipAllAsync for parallel execution.
    /// Returns the existing task if this drive is already ripping so that
    /// Task.WhenAll correctly waits for it.
    /// </summary>
    public Task RipAsync(CancellationToken externalCt)
    {
        if (!_activeRipTask.IsCompleted)
            return _activeRipTask; // already running — include in WhenAll

        if (!CanRip()) return Task.CompletedTask;

        _ripCts = new CancellationTokenSource();
        var linked = CancellationTokenSource.CreateLinkedTokenSource(externalCt, _ripCts.Token);

        _activeRipTask = ExecuteRipCoreAsync(linked.Token)
            .ContinueWith(_ =>
            {
                linked.Dispose();
                _ripCts?.Dispose();
                _ripCts = null;
            }, TaskScheduler.Default);

        return _activeRipTask;
    }

    private async Task ExecuteRipCoreAsync(CancellationToken ct)
    {
        // Snapshot mutable settings before any await
        var s = _settings.Snapshot();

        var selected = Titles
            .Where(t => t.IsSelected)
            .OrderBy(t => t.Id)
            .ToList();

        List<int> episodes;
        if (IsTvMode)
        {
            episodes = Enumerable.Range(EpisodeStart, EpisodeEnd - EpisodeStart + 1).ToList();
            if (episodes.Count != selected.Count)
            {
                Log($"Drive {DiskNumber}: episode/title count mismatch " +
                    $"({episodes.Count} episodes vs {selected.Count} selected).");
                return;
            }
        }
        else
        {
            episodes = selected.Select(_ => 0).ToList();
        }

        IsRipping = true;
        RipProgress = 0;
        RipStatus = string.Empty;

        // Per-drive subdirectory prevents MKV filename collisions across drives
        var intermediateDir = Path.Combine(s.IntermediatePath, $"disc{DiskNumber}");

        try
        {
            Directory.CreateDirectory(intermediateDir);

            var ripProgress = new Progress<(int Percent, string Status)>(p =>
            {
                RipProgress = p.Percent;
                RipStatus = p.Status;
            });

            for (int i = 0; i < selected.Count; i++)
            {
                var title = selected[i].Title;
                var episode = episodes[i];

                // ── 1. Rip ────────────────────────────────────────────────
                Log($"[Drive {DiskNumber}] [{i + 1}/{selected.Count}] Ripping title {title.Id} ({title.DurationDisplay})…");
                RipStatus = $"Ripping title {title.Id}…";
                RipProgress = 0;

                var actualMkv = await _makeMkv.RipTitleAsync(
                    title.Id.ToString(), DiskNumber.ToString(),
                    intermediateDir, s.MakeMkvExePath,
                    ripProgress, Log, ct);

                // ── 2. Compute output path ────────────────────────────────
                var mediaName = string.IsNullOrWhiteSpace(MediaName)
                    ? _naming.GetTitleFileName(
                        !string.IsNullOrEmpty(title.Name) ? title.Name : DiscName)
                    : MediaName;

                var m4vPath = _naming.GetMediaFilePath(
                    s.OutputPath, mediaName,
                    IsTvMode ? Season : 0,
                    episode);

                if (File.Exists(m4vPath))
                {
                    Log($"[Drive {DiskNumber}] Deleting existing: {m4vPath}");
                    File.Delete(m4vPath);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(m4vPath)!);

                // ── 3. Encode ─────────────────────────────────────────────
                Log($"[Drive {DiskNumber}] [{i + 1}/{selected.Count}] Encoding → {m4vPath}");
                RipStatus = $"Encoding title {title.Id}…";
                RipProgress = 0;

                await _handBrake.ConvertVideoAsync(
                    actualMkv, m4vPath,
                    s.PresetName, s.PresetFilePath,
                    s.HandBrakeExePath,
                    ripProgress, Log, ct);

                // ── 4. Clean up intermediate MKV ──────────────────────────
                if (File.Exists(actualMkv))
                {
                    File.Delete(actualMkv);
                    Log($"[Drive {DiskNumber}] Deleted: {actualMkv}");
                }

                Log($"[Drive {DiskNumber}] ✓ {m4vPath}");
            }

            RipStatus = "Done ✓";
            RipProgress = 100;

            if (EjectWhenDone)
            {
                Log($"[Drive {DiskNumber}] Ejecting…");
                _driveService.EjectDrive(WmpDriveIndex);
            }
        }
        catch (OperationCanceledException)
        {
            Log($"[Drive {DiskNumber}] Rip cancelled.");
            RipStatus = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"[Drive {DiskNumber}] Rip failed: {ex.Message}");
            RipStatus = "Failed";
        }
        finally
        {
            IsRipping = false;
            RipStandaloneCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRip()
    {
        if (!IsIdle) return false;
        if (SelectedTitleCount == 0) return false;
        if (string.IsNullOrWhiteSpace(_settings.HandBrakeExePath)) return false;
        if (string.IsNullOrWhiteSpace(_settings.IntermediatePath)) return false;
        if (string.IsNullOrWhiteSpace(_settings.OutputPath)) return false;
        if (string.IsNullOrWhiteSpace(_settings.PresetFilePath)) return false;

        if (IsTvMode)
        {
            if (EpisodeEnd < EpisodeStart) return false;
            return EpisodeEnd - EpisodeStart + 1 == SelectedTitleCount;
        }

        return SelectedTitleCount == 1;
    }

    // ── Other commands ─────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => CancelCurrentOperation();

    /// <summary>Cancels any active scan or rip on this drive.</summary>
    public void CancelCurrentOperation()
    {
        _scanCts?.Cancel();
        _ripCts?.Cancel();
    }

    private bool CanCancel() => IsScanning || IsRipping;

    [RelayCommand]
    private void Eject()
    {
        try
        {
            Log($"[Drive {DiskNumber}] Ejecting…");
            _driveService.EjectDrive(WmpDriveIndex);
        }
        catch (Exception ex)
        {
            Log($"[Drive {DiskNumber}] Eject failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var t in Titles) t.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var t in Titles) t.IsSelected = false;
    }

    // ── Output path helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Computes the set of planned output M4V paths for the currently selected
    /// titles. Used by MainViewModel to detect cross-drive collisions before
    /// starting a Rip All operation.
    /// </summary>
    public IEnumerable<string> GetPlannedOutputPaths()
    {
        var selected = Titles.Where(t => t.IsSelected).OrderBy(t => t.Id).ToList();
        if (selected.Count == 0) yield break;

        var episodes = IsTvMode
            ? Enumerable.Range(EpisodeStart, EpisodeEnd - EpisodeStart + 1).ToList()
            : selected.Select(_ => 0).ToList();

        if (episodes.Count != selected.Count) yield break;

        var outputPath = _settings.OutputPath;

        for (int i = 0; i < selected.Count; i++)
        {
            var title = selected[i].Title;
            var episode = episodes[i];

            var mediaName = string.IsNullOrWhiteSpace(MediaName)
                ? _naming.GetTitleFileName(
                    !string.IsNullOrEmpty(title.Name) ? title.Name : DiscName)
                : MediaName;

            yield return _naming.GetMediaFilePath(
                outputPath, mediaName,
                IsTvMode ? Season : 0,
                episode);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void RefreshSelectedCount()
    {
        SelectedTitleCount = Titles.Count(t => t.IsSelected);
        RipStandaloneCommand.NotifyCanExecuteChanged();
    }

    private void Log(string message) => _sharedLog(message);
}
