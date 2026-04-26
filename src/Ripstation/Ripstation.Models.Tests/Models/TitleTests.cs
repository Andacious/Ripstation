using Ripstation.Models;

namespace Ripstation.Models.Tests;

public class TitleTests
{
    // ── DurationDisplay ───────────────────────────────────────────────────────

    [Fact]
    public void DurationDisplay_ZeroDuration_ReturnsDash()
    {
        var t = new Title { Duration = TimeSpan.Zero };
        Assert.Equal("—", t.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_FullHours_FormattedCorrectly()
    {
        var t = new Title { Duration = new TimeSpan(2, 15, 30) };
        Assert.Equal("2:15:30", t.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_SingleHour_NoLeadingZero()
    {
        var t = new Title { Duration = new TimeSpan(1, 0, 0) };
        Assert.Equal("1:00:00", t.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_UnderOneHour_StartsWithZero()
    {
        var t = new Title { Duration = new TimeSpan(0, 45, 12) };
        Assert.Equal("0:45:12", t.DurationDisplay);
    }

    // ── SizeDisplay ───────────────────────────────────────────────────────────

    [Fact]
    public void SizeDisplay_ZeroBytes_ReturnsDash()
    {
        var t = new Title { SizeInBytes = 0 };
        Assert.Equal("—", t.SizeDisplay);
    }

    [Fact]
    public void SizeDisplay_OneMegabyte_Returns1MB()
    {
        var t = new Title { SizeInBytes = 1_048_576 };
        Assert.Equal("1 MB", t.SizeDisplay);
    }

    [Fact]
    public void SizeDisplay_OnGigabyte_Returns1024MB()
    {
        var t = new Title { SizeInBytes = 1_073_741_824 };
        Assert.Equal("1024 MB", t.SizeDisplay);
    }

    [Fact]
    public void SizeDisplay_FractionalMegabytes_RoundedToNearest()
    {
        // 1.5 MB = 1_572_864 bytes → rounds to 2 MB
        var t = new Title { SizeInBytes = 1_572_864 };
        Assert.Equal("2 MB", t.SizeDisplay);
    }
}
