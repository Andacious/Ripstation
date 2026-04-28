using Ripstation.ViewModels;

namespace RipstationApp.Tests.ViewModels;

public class GlobalSettingsTests
{
    [Fact]
    public void Snapshot_CapturesAllFields()
    {
        var s = new GlobalSettings
        {
            IntermediatePath = @"D:\mkv",
            OutputPath       = @"E:\movies",
        };

        var snap = s.Snapshot();

        Assert.Equal(@"D:\mkv",     snap.IntermediatePath);
        Assert.Equal(@"E:\movies",  snap.OutputPath);
    }

    [Fact]
    public void Snapshot_IsImmutable_ChangingSettingsDoesNotAffectSnapshot()
    {
        var s = new GlobalSettings { OutputPath = @"D:\original" };
        var snap = s.Snapshot();

        s.OutputPath = @"D:\changed";

        // The snapshot must still hold the original value
        Assert.Equal(@"D:\original", snap.OutputPath);
        // And the live setting shows the new value
        Assert.Equal(@"D:\changed", s.OutputPath);
    }

    [Fact]
    public void Snapshot_TwiceFromSameSettings_AreEqual()
    {
        var s = new GlobalSettings { PresetName = "Plex" };
        var snap1 = s.Snapshot();
        var snap2 = s.Snapshot();
        Assert.Equal(snap1, snap2);
    }
}
