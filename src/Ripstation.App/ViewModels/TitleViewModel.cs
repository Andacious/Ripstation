using CommunityToolkit.Mvvm.ComponentModel;
using Ripstation.Models;

namespace Ripstation.ViewModels;

public partial class TitleViewModel(Title title) : ObservableObject
{
    public Title Title { get; } = title;

    [ObservableProperty]
    private bool _isSelected;

    public int Id => Title.Id;
    public string Name => Title.Name;
    public string FileName => Title.FileName;
    public int Chapters => Title.Chapters;
    public string DurationDisplay => Title.DurationDisplay;
    public string SizeDisplay => Title.SizeDisplay;
}
