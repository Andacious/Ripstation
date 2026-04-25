namespace Ripstation.Models;

public class Title
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Chapters { get; set; }
    public TimeSpan Duration { get; set; }
    public long SizeInBytes { get; set; }

    public string DurationDisplay =>
        Duration == TimeSpan.Zero ? "—" : Duration.ToString(@"h\:mm\:ss");

    public string SizeDisplay =>
        SizeInBytes == 0 ? "—" : $"{SizeInBytes / 1_048_576.0:F0} MB";
}
