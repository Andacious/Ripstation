using System.Runtime.InteropServices;

namespace Ripstation.Services;

public class DriveService : IDriveService
{
    public void EjectDrive(int wmpCdRomIndex = 0)
    {
        var wmpType = Type.GetTypeFromProgID("WMPlayer.OCX.7")
            ?? throw new InvalidOperationException("Windows Media Player COM object not available");

        var wmp = Activator.CreateInstance(wmpType)
            ?? throw new InvalidOperationException("Could not create WMPlayer instance");

        try
        {
            dynamic wmpDynamic = wmp;
            dynamic drive = wmpDynamic.cdromCollection.item(wmpCdRomIndex);
            drive.eject();
        }
        finally
        {
            Marshal.ReleaseComObject(wmp);
        }
    }

    public string GetVolumeLabel(string drivePath)
    {
        try
        {
            var info = new DriveInfo(drivePath);
            return info.IsReady ? (info.VolumeLabel ?? string.Empty) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public IReadOnlyList<(int DiscIndex, string DrivePath)> GetOpticalDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.CDRom)
            .Select((d, i) => (DiscIndex: i, DrivePath: d.Name))
            .ToList()
            .AsReadOnly();
    }
}
