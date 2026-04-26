namespace Ripstation.Services;

public interface IDriveService
{
    /// <summary>
    /// Ejects the disc tray for the given drive.
    /// <paramref name="makeMkvDiscIndex"/> is the 0-based MakeMKV disc index,
    /// which typically matches the Windows CD-ROM index but may differ on
    /// some systems — use <paramref name="wmpCdRomIndex"/> to override for WMP.
    /// </summary>
    void EjectDrive(int wmpCdRomIndex = 0);

    /// <summary>
    /// Enumerates optical (CD/DVD/BD) drives using Windows DriveInfo.
    /// Returns (DiscIndex, DrivePath) pairs where DiscIndex is the 0-based
    /// enumeration order that typically matches the MakeMKV disc: index.
    /// </summary>
    IReadOnlyList<(int DiscIndex, string DrivePath)> GetOpticalDrives();
}
