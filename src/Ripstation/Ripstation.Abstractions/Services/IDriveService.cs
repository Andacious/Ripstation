namespace Ripstation.Services;

public interface IDriveService
{
    /// <summary>
    /// Ejects the disc tray for the given drive.
    /// <paramref name="driveIndex"/> is the 0-based drive index.
    /// </summary>
    void EjectDrive(int driveIndex = 0);

    /// <summary>
    /// Enumerates optical (CD/DVD/BD) drives.
    /// Returns (DiscIndex, DrivePath) pairs where DiscIndex is the 0-based
    /// enumeration order that typically matches the disc ripper index.
    /// </summary>
    IReadOnlyList<(int DiscIndex, string DrivePath)> GetOpticalDrives();

    /// <summary>
    /// Returns the volume label for the given drive path (e.g. "D:\"),
    /// or an empty string if the drive has no disc or the label is unavailable.
    /// </summary>
    string GetVolumeLabel(string drivePath);
}
