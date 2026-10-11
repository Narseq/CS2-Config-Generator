using CS2ConfigGenerator.Infrastructure;
using CS2ConfigGenerator.Models;

namespace CS2ConfigGenerator.ViewModels;

public sealed class KeyViewModel : ObservableObject
{
    public const double Unit = 48;
    private const double Margin = 2;

    private KeyBind? _bind;
    private bool _isSelected;
    private bool _isMatch;
    private bool _isDimmed;

    public KeyViewModel(KeyDefinition definition) => Definition = definition;

    public KeyDefinition Definition { get; }
    public string Id => Definition.Id;
    public string Label => Definition.Label;

    public double Left => Definition.X * Unit + Margin;
    public double Top => Definition.Y * Unit + Margin;
    public double Width => Definition.Width * Unit - 2 * Margin;
    public double Height => Definition.Height * Unit - 2 * Margin;

    public KeyBind? Bind
    {
        get => _bind;
        set
        {
            if (!SetProperty(ref _bind, value)) return;
            OnPropertyChanged(nameof(HasBind));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(Tooltip));
        }
    }

    public bool HasBind => _bind != null;
    public string BorderColor => _bind == null ? "#3A4352" : BindCategoryInfo.Color(_bind.Category);
    public string Icon => _bind == null ? "" : BindCategoryInfo.Icon(_bind.Category);
    public string Tooltip => _bind == null ? $"{Id} — brak przypisanej akcji" : _bind.ToConfigLine();

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public bool IsMatch { get => _isMatch; set => SetProperty(ref _isMatch, value); }
    public bool IsDimmed { get => _isDimmed; set => SetProperty(ref _isDimmed, value); }
}

public sealed class KeySectionViewModel
{
    public KeySectionViewModel(KeySectionDefinition definition)
    {
        Name = definition.Name;
        PixelWidth = definition.Width * KeyViewModel.Unit;
        PixelHeight = definition.Height * KeyViewModel.Unit;
        Keys = definition.Keys.Select(k => new KeyViewModel(k)).ToList();
    }

    public string Name { get; }
    public double PixelWidth { get; }
    public double PixelHeight { get; }
    public IReadOnlyList<KeyViewModel> Keys { get; }
}
