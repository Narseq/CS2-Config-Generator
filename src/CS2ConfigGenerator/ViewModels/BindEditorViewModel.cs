using System.Collections.ObjectModel;
using CS2ConfigGenerator.Infrastructure;
using CS2ConfigGenerator.Models;
using CS2ConfigGenerator.Services;

namespace CS2ConfigGenerator.ViewModels;

public sealed class CategoryItem
{
    public CategoryItem(BindCategory category)
    {
        Category = category;
        Name = BindCategoryInfo.Name(category);
        Icon = BindCategoryInfo.Icon(category);
        Color = BindCategoryInfo.Color(category);
    }

    public BindCategory Category { get; }
    public string Name { get; }
    public string Icon { get; }
    public string Color { get; }
}

public sealed class CommandEntry
{
    public CommandEntry(string text) => Text = text;
    public string Text { get; }
}

public sealed class BindEditorViewModel : ObservableObject
{
    private readonly DataService _data;
    private readonly Func<string, KeyBind?> _getBind;
    private readonly Action<KeyBind> _onSave;
    private readonly Action<string> _onRemove;

    private KeyViewModel? _key;
    private CategoryItem _selectedCategory;
    private BindCategory _bindCategory = BindCategory.WlasnaKomenda;
    private string _customCommand = "";
    private bool _isKeyOccupied;
    private string? _occupiedCommand;

    public BindEditorViewModel(DataService data, Func<string, KeyBind?> getBind, Action<KeyBind> onSave,
        Action<string> onRemove, Action onClearAll)
    {
        _data = data;
        _getBind = getBind;
        _onSave = onSave;
        _onRemove = onRemove;

        Categories = Enum.GetValues<BindCategory>().Select(c => new CategoryItem(c)).ToList();
        _selectedCategory = Categories[0];
        RefreshActions();

        Commands.CollectionChanged += (_, _) => RefreshState();

        AddActionCommand = new RelayCommand(p => { if (p is BindAction a) AddAction(a); }, _ => CanEdit);
        AddCustomCommand = new RelayCommand(AddCustom, () => CanEdit && !string.IsNullOrWhiteSpace(CustomCommand));
        RemoveCommandCommand = new RelayCommand(p => { if (p is CommandEntry e) Commands.Remove(e); });
        SaveCommand = new RelayCommand(Save, () => CanSave);
        RemoveBindCommand = new RelayCommand(RemoveBind, () => HasExistingBind);
        OverwriteCommand = new RelayCommand(() => { IsKeyOccupied = false; Commands.Clear(); });
        EditExistingCommand = new RelayCommand(EditExisting);
        ClearAllCommand = new RelayCommand(onClearAll);
    }

    public IReadOnlyList<CategoryItem> Categories { get; }
    public ObservableCollection<BindAction> Actions { get; } = new();
    public ObservableCollection<CommandEntry> Commands { get; } = new();

    public RelayCommand AddActionCommand { get; }
    public RelayCommand AddCustomCommand { get; }
    public RelayCommand RemoveCommandCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand RemoveBindCommand { get; }
    public RelayCommand OverwriteCommand { get; }
    public RelayCommand EditExistingCommand { get; }
    public RelayCommand ClearAllCommand { get; }

    public bool HasKey => _key != null;
    public string KeyTitle => _key == null ? "" : $"Klawisz: {_key.Label}  ({_key.Id})";

    public CategoryItem SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (value == null || !SetProperty(ref _selectedCategory, value)) return;
            RefreshActions();
            OnPropertyChanged(nameof(IsCustomCategory));
            OnPropertyChanged(nameof(ShowActions));
        }
    }

    public bool IsCustomCategory => _selectedCategory.Category == BindCategory.WlasnaKomenda;
    public bool ShowActions => !IsCustomCategory;

    public string CustomCommand
    {
        get => _customCommand;
        set
        {
            if (!SetProperty(ref _customCommand, value)) return;
            RefreshState();
        }
    }

    /// <summary>Klawisz ma już bind — edytor czeka na decyzję: „Nadpisz” albo „Edytuj istniejący”.</summary>
    public bool IsKeyOccupied
    {
        get => _isKeyOccupied;
        private set
        {
            if (!SetProperty(ref _isKeyOccupied, value)) return;
            RefreshState();
        }
    }

    public string OccupiedWarning => $"Ten klawisz ma już bind: {_occupiedCommand}";

    public bool CanEdit => HasKey && !IsKeyOccupied;
    public bool HasExistingBind => _key != null && _getBind(_key.Id) != null;
    public bool CanSave => CanEdit && Commands.Count > 0 && ValidationError == null;

    public string PreviewText => _key == null
        ? ""
        : $"bind \"{_key.Id}\" \"{string.Join("; ", Commands.Select(c => c.Text))}\"";

    /// <summary>Komunikat walidacji lub null, gdy bind jest poprawny.</summary>
    public string? ValidationError
    {
        get
        {
            if (_key == null) return null;
            if (IsKeyOccupied) return null;
            if (Commands.Count == 0) return "Dodaj co najmniej jedną komendę.";
            if (Commands.Any(c => string.IsNullOrWhiteSpace(c.Text))) return "Komenda nie może być pusta.";
            if (Commands.Any(c => c.Text.Contains('"'))) return "Komenda nie może zawierać cudzysłowu (\").";
            return null;
        }
    }

    /// <summary>Ostrzeżenie o niepoprawnym tekście w polu własnej komendy.</summary>
    public string? CustomCommandError =>
        CustomCommand.Contains('"') ? "Komenda nie może zawierać cudzysłowu (\")." : null;

    public void Open(KeyViewModel? key)
    {
        _key = key;
        Commands.Clear();
        CustomCommand = "";

        var existing = key == null ? null : _getBind(key.Id);
        _occupiedCommand = existing?.CommandText;
        _bindCategory = existing?.Category ?? BindCategory.WlasnaKomenda;
        IsKeyOccupied = existing != null;
        if (existing == null) SelectedCategory = Categories[0];

        OnPropertyChanged(nameof(HasKey));
        OnPropertyChanged(nameof(KeyTitle));
        OnPropertyChanged(nameof(OccupiedWarning));
        RefreshState();
    }

    private void EditExisting()
    {
        if (_key == null) return;
        var existing = _getBind(_key.Id);
        if (existing == null) return;

        Commands.Clear();
        foreach (var command in existing.Commands) Commands.Add(new CommandEntry(command));
        _bindCategory = existing.Category;
        SelectedCategory = Categories.First(c => c.Category == existing.Category);
        IsKeyOccupied = false;
    }

    private void AddAction(BindAction action)
    {
        if (Commands.Count == 0) _bindCategory = action.Category;
        foreach (var command in action.Commands) Commands.Add(new CommandEntry(command));
    }

    private void AddCustom()
    {
        if (CustomCommandError != null) return;
        if (Commands.Count == 0) _bindCategory = BindCategory.WlasnaKomenda;
        // Średnik w polu własnej komendy rozdziela osobne komendy.
        foreach (var part in CustomCommand.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            Commands.Add(new CommandEntry(part));
        CustomCommand = "";
    }

    private void Save()
    {
        if (_key == null || !CanSave) return;
        _onSave(new KeyBind
        {
            Key = _key.Id,
            Category = _bindCategory,
            Commands = Commands.Select(c => c.Text).ToList()
        });
        RefreshState();
    }

    private void RemoveBind()
    {
        if (_key == null) return;
        _onRemove(_key.Id);
        Commands.Clear();
        IsKeyOccupied = false;
        RefreshState();
    }

    /// <summary>Wywoływane, gdy bindy zmieniły się poza edytorem (preset, import, czyszczenie).</summary>
    public void Reload() => Open(_key);

    private void RefreshActions()
    {
        Actions.Clear();
        foreach (var action in _data.ActionsFor(_selectedCategory.Category))
            Actions.Add(action);
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasExistingBind));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(ValidationError));
        OnPropertyChanged(nameof(CustomCommandError));
        AddActionCommand.RaiseCanExecuteChanged();
        AddCustomCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        RemoveBindCommand.RaiseCanExecuteChanged();
    }
}
