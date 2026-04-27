using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ripstation.Services;

#pragma warning disable MVVMTK0045 // Use partial property instead of [ObservableProperty] field (AOT compat warning only — reflection bindings work fine)

namespace Ripstation.ViewModels;

public partial class DriveViewModel : ObservableObject
{
    private readonly GlobalSettings _settings;
    private readonly IMakeMkvService _makeMkv;
    private readonly IHandBrakeService _handBrake;
    private readonly IMediaNamingService _naming;
    private readonly IDriveService _driveService;
    private readonly IFileSystem _fs;
    private readonly Action<string> _sharedLog;

    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _ripCts;

    // Active rip task — returned to RipAll so Task.WhenAll can track it
    private Task _activeRipTask = Task.CompletedTask;

    // ── Disc identity ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabHeader))]
    [NotifyPropertyChangedFor(nameof(RadioGroupName))]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private int _diskNumber;

    public string RadioGroupName => $"MediaType_{DiskNumber}";

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
    [NotifyPropertyChangedFor(nameof(EpisodeCountText))]
    private int _episodeStart = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyPropertyChangedFor(nameof(EpisodeCount))]
    [NotifyPropertyChangedFor(nameof(EpisodeCountText))]
    private int _episodeEnd = 1;

    public int EpisodeCount => Math.Max(0, EpisodeEnd - EpisodeStart + 1);
    public string EpisodeCountText => $"{EpisodeCount} episodes";

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
    [NotifyPropertyChangedFor(nameof(IsRipStatusIdleVisible))]
    private bool _isRipping;

    [ObservableProperty] private int _ripProgress;
    [ObservableProperty] private string _ripStatus = string.Empty;

    public bool IsRipStatusIdleVisible => !IsRipping && !string.IsNullOrEmpty(RipStatus);
    partial void OnRipStatusChanged(string value) => OnPropertyChanged(nameof(IsRipStatusIdleVisible));

    // ── Titles ─────────────────────────────────────────────────────────────────

    public ObservableCollection<TitleViewModel> Titles { get; } = [];

    public bool HasTitles => Titles.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RipStandaloneCommand))]
    [NotifyPropertyChangedFor(nameof(HasSelectedTitles))]
    [NotifyPropertyChangedFor(nameof(SelectedTitleCountText))]
    private int _selectedTitleCount;

    public bool HasSelectedTitles => SelectedTitleCount > 0;
    public string SelectedTitleCountText => $"{SelectedTitleCount} title(s) selected";
    partial void OnSelectedTitleCountChanged(int value) => OnPropertyChanged(nameof(HasSelectedTitles));

    // ── Derived ────────────────────────────────────────────────────────────────

    public string TabHeader
    {
        get
        {
            var driveId = string.IsNullOrEmpty(DriveLetter)
                ? $"Drive {DiskNumber}"
                : DriveLetter.TrimEnd('\\', '/');

            return string.IsNullOrEmpty(DiscName)
                ? driveId
                : $"{DiscName} ({driveId})";
        }
    }

    private bool IsIdle => !IsScanning && !IsRipping;

    // ── Constructor ────────────────────────────────────────────────────────────

    private Action? _onRemoveSelf;

    [RelayCommand]
    private void RemoveSelf() => _onRemoveSelf?.Invoke();

    public DriveViewModel(
        int diskNumber,
        GlobalSettings settings,
        IMakeMkvService makeMkv,
        IHandBrakeService handBrake,
        IMediaNamingService naming,
        IDriveService driveService,
        Action<string> sharedLog,
        string driveLetter = "",
        IFileSystem? fileSystem = null,
        Action? onRemoveSelf = null)
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
        _fs = fileSystem ?? new FileSystem();
        _onRemoveSelf = onRemoveSelf;

        // Show the OS volume label immediately so the tab has a name before scanning
        if (!string.IsNullOrEmpty(driveLetter))
        {
            var label = driveService.GetVolumeLabel(driveLetter);
            if (!string.IsNullOrEmpty(label))
                _discName = label;
        }

        Titles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTitles));

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

        // Pre-flight: ensure the drive/root of both paths is accessible
        var intermediateRoot = Path.GetPathRoot(s.IntermediatePath);
        var outputRoot       = Path.GetPathRoot(s.OutputPath);

        if (!string.IsNullOrEmpty(intermediateRoot) && !_fs.DirectoryExists(intermediateRoot))
        {
            Log($"[Drive {DiskNumber}] Intermediate path drive '{intermediateRoot}' not found. " +
                 "Update the 'Intermediate (MKV)' path in settings.");
            RipStatus = "Failed — path not found";
            IsRipping = false;
            return;
        }
        if (!string.IsNullOrEmpty(outputRoot) && !_fs.DirectoryExists(outputRoot))
        {
            Log($"[Drive {DiskNumber}] Output path drive '{outputRoot}' not found. " +
                 "Update the 'Output (M4V)' path in settings.");
            RipStatus = "Failed — path not found";
            IsRipping = false;
            return;
        }

        try
        {
            _fs.DirectoryCreate(intermediateDir);

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

                if (_fs.FileExists(m4vPath))
                {
                    Log($"[Drive {DiskNumber}] Deleting existing: {m4vPath}");
                    _fs.FileDelete(m4vPath);
                }

                _fs.DirectoryCreate(Path.GetDirectoryName(m4vPath)!);

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
                if (_fs.FileExists(actualMkv))
                {
                    _fs.FileDelete(actualMkv);
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
            ? (EpisodeEnd >= EpisodeStart
                ? Enumerable.Range(EpisodeStart, EpisodeEnd - EpisodeStart + 1).ToList()
                : [])
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
