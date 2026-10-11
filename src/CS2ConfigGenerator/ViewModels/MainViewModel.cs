using System.Collections.ObjectModel;
using CS2ConfigGenerator.Infrastructure;
using CS2ConfigGenerator.Models;
using CS2ConfigGenerator.Services;

namespace CS2ConfigGenerator.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;
    private readonly ConfigGeneratorService _generator;
    private readonly ConfigFileService _files;
    private readonly Cs2PathService _paths;
    private readonly PresetService _presets;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, KeyViewModel> _keysById = new(StringComparer.OrdinalIgnoreCase);

    private KeyViewModel? _selectedKey;
    private string _configText = "";
    private string _searchText = "";
    private string _searchSummary = "";
    private string _statusMessage = "";
    private bool _showWelcome = true;
    private bool _trainingMode;

    public MainViewModel(DataService data, PresetService presets, ConfigGeneratorService generator,
        ConfigFileService files, Cs2PathService paths, SettingsService settingsService, IDialogService dialogs)
    {
        _presets = presets;
        _generator = generator;
        _files = files;
        _paths = paths;
        _settingsService = settingsService;
        _dialogs = dialogs;
        _settings = settingsService.Load();
        _trainingMode = _settings.TrainingMode;

        foreach (var section in data.Sections)
        {
            var vm = new KeySectionViewModel(section);
            Sections.Add(vm);
            foreach (var key in vm.Keys) _keysById[key.Id] = key;
        }

        Editor = new BindEditorViewModel(data, GetBind, SetBind, RemoveBind, ClearAll);
        Presets = new ObservableCollection<Preset>(presets.Presets);

        SelectKeyCommand = new RelayCommand(p => SelectKey(p as KeyViewModel));
        StartCommand = new RelayCommand(() => ShowWelcome = false);
        CopyConfigCommand = new RelayCommand(CopyConfig);
        SaveAsCommand = new RelayCommand(SaveAs);
        SaveToCs2Command = new RelayCommand(SaveToCs2);
        ImportCommand = new RelayCommand(Import);
        LoadPresetCommand = new RelayCommand(p => { if (p is Preset preset) LoadPreset(preset); });
        ChooseCs2FolderCommand = new RelayCommand(() => ChooseCs2Folder());

        RefreshAll();
    }

    public ObservableCollection<KeySectionViewModel> Sections { get; } = new();
    public ObservableCollection<KeyBind> Binds { get; } = new();
    public ObservableCollection<string> UnrecognizedLines { get; } = new();
    public ObservableCollection<Preset> Presets { get; }
    public BindEditorViewModel Editor { get; }

    public RelayCommand SelectKeyCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand CopyConfigCommand { get; }
    public RelayCommand SaveAsCommand { get; }
    public RelayCommand SaveToCs2Command { get; }
    public RelayCommand ImportCommand { get; }
    public RelayCommand LoadPresetCommand { get; }
    public RelayCommand ChooseCs2FolderCommand { get; }

    public string ConfigText { get => _configText; private set => SetProperty(ref _configText, value); }
    public string SearchSummary { get => _searchSummary; private set => SetProperty(ref _searchSummary, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public bool ShowWelcome { get => _showWelcome; set => SetProperty(ref _showWelcome, value); }
    public bool HasUnrecognized => UnrecognizedLines.Count > 0;
    public string UnrecognizedHeader => $"Nie rozpoznane komendy ({UnrecognizedLines.Count})";
    public string Cs2PathDisplay => string.IsNullOrEmpty(_settings.Cs2CfgPath)
        ? "Folder CS2: nie wybrano"
        : $"Folder CS2: {_settings.Cs2CfgPath}";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value)) ApplySearch();
        }
    }

    public bool TrainingMode
    {
        get => _trainingMode;
        set
        {
            if (!SetProperty(ref _trainingMode, value)) return;
            _settings.TrainingMode = value;
            _settingsService.Save(_settings);
            RefreshConfig();
        }
    }

    // ---- Operacje na bindach -------------------------------------------------

    public KeyBind? GetBind(string key) => Binds.FirstOrDefault(b => b.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public void SetBind(KeyBind bind)
    {
        var index = IndexOf(bind.Key);
        if (index >= 0) Binds[index] = bind;
        else Binds.Add(bind);
        RefreshAll();
    }

    public void RemoveBind(string key)
    {
        var index = IndexOf(key);
        if (index < 0) return;
        Binds.RemoveAt(index);
        RefreshAll();
    }

    private void ClearAll()
    {
        if (Binds.Count == 0 && UnrecognizedLines.Count == 0) return;
        if (!_dialogs.Confirm("Wyczyść wszystkie bindy", "Czy na pewno usunąć wszystkie bindy?")) return;
        ResetState();
        RefreshAll();
        Editor.Reload();
    }

    private void ResetState()
    {
        Binds.Clear();
        UnrecognizedLines.Clear();
    }

    private int IndexOf(string key)
    {
        for (var i = 0; i < Binds.Count; i++)
            if (Binds[i].Key.Equals(key, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private void SelectKey(KeyViewModel? key)
    {
        if (_selectedKey != null) _selectedKey.IsSelected = false;
        _selectedKey = key;
        if (key != null) key.IsSelected = true;
        Editor.Open(key);
    }

    public void LoadPreset(Preset preset)
    {
        if (preset.Clear) ResetState();
        foreach (var bind in _presets.ToBinds(preset))
        {
            var index = IndexOf(bind.Key);
            if (index >= 0) Binds[index] = bind;
            else Binds.Add(bind);
        }
        if (preset.TrainingMode) TrainingMode = true;
        RefreshAll();
        Editor.Reload();
        StatusMessage = $"Wczytano preset „{preset.Name}”. Możesz teraz edytować bindy.";
    }

    // ---- Zapis, kopiowanie, import ------------------------------------------

    private void CopyConfig()
    {
        StatusMessage = _dialogs.CopyToClipboard(ConfigText)
            ? "Skopiowano config do schowka."
            : "Nie udało się skopiować — schowek jest zajęty przez inny program.";
    }

    private void SaveAs()
    {
        var path = _dialogs.PickSaveFile("autoexec.cfg", _settings.LastDirectory);
        if (path == null) return;
        _settings.LastDirectory = Path.GetDirectoryName(path);
        _settingsService.Save(_settings);
        WriteConfig(path);
    }

    private void SaveToCs2()
    {
        var folder = ResolveCs2Folder();
        if (folder == null) return;
        WriteConfig(_paths.GetAutoexecPath(folder));
    }

    /// <summary>Zwraca zapisany/wykryty folder cfg albo prosi użytkownika o wskazanie. Null = anulowano.</summary>
    private string? ResolveCs2Folder()
    {
        if (!string.IsNullOrEmpty(_settings.Cs2CfgPath) && Directory.Exists(_settings.Cs2CfgPath))
            return _settings.Cs2CfgPath;

        var detected = _paths.Detect();
        if (detected != null)
        {
            SaveCs2Path(detected);
            return detected;
        }

        _dialogs.ShowInfo("Nie znaleziono CS2",
            "Nie udało się automatycznie znaleźć folderu CS2.\nWskaż folder game\\csgo\\cfg (lub główny folder gry).");
        return ChooseCs2Folder();
    }

    private string? ChooseCs2Folder()
    {
        var selected = _dialogs.PickFolder("Wskaż folder cfg gry CS2", _settings.Cs2CfgPath);
        if (selected == null) return null;
        var folder = _paths.NormalizeSelectedFolder(selected);
        SaveCs2Path(folder);
        return folder;
    }

    private void SaveCs2Path(string folder)
    {
        _settings.Cs2CfgPath = folder;
        _settingsService.Save(_settings);
        OnPropertyChanged(nameof(Cs2PathDisplay));
    }

    private void WriteConfig(string path)
    {
        try
        {
            var result = _files.Save(path, ConfigText);
            var message = $"Zapisano config:\n{result.Path}";
            if (result.BackupPath != null) message += $"\n\nKopia zapasowa poprzedniego pliku:\n{result.BackupPath}";
            StatusMessage = $"Zapisano: {result.Path}";
            _dialogs.ShowInfo("Zapisano", message);
        }
        catch (UnauthorizedAccessException)
        {
            _dialogs.ShowError("Brak uprawnień", $"Nie masz uprawnień do zapisu w:\n{path}\n\nWybierz inny folder lub uruchom aplikację jako administrator.");
        }
        catch (DirectoryNotFoundException)
        {
            _dialogs.ShowError("Brak folderu", $"Folder nie istnieje:\n{Path.GetDirectoryName(path)}\n\nWskaż poprawny folder CS2.");
        }
        catch (IOException ex)
        {
            _dialogs.ShowError("Błąd zapisu", $"Nie udało się zapisać pliku:\n{path}\n\n{ex.Message}");
        }
    }

    private void Import()
    {
        var path = _dialogs.PickOpenFile(_settings.LastDirectory ?? _settings.Cs2CfgPath);
        if (path == null) return;

        if (Binds.Count > 0 && !_dialogs.Confirm("Importuj config", "Import zastąpi obecne bindy. Kontynuować?")) return;

        ImportResult result;
        try
        {
            result = _files.ImportFile(path);
        }
        catch (InvalidDataException ex)
        {
            _dialogs.ShowError("Zły plik", ex.Message);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Nie można odczytać pliku", $"{path}\n\n{ex.Message}");
            return;
        }

        ResetState();
        foreach (var bind in result.Binds) Binds.Add(bind);
        foreach (var line in result.Unrecognized) UnrecognizedLines.Add(line);
        RefreshAll();
        Editor.Reload();
        StatusMessage = $"Zaimportowano {result.Binds.Count} bindów, nie rozpoznano {result.Unrecognized.Count} linii.";
    }

    // ---- Odświeżanie widoku -------------------------------------------------

    private void RefreshAll()
    {
        foreach (var key in _keysById.Values) key.Bind = null;
        foreach (var bind in Binds)
            if (_keysById.TryGetValue(bind.Key, out var key)) key.Bind = bind;

        OnPropertyChanged(nameof(HasUnrecognized));
        OnPropertyChanged(nameof(UnrecognizedHeader));
        RefreshConfig();
        ApplySearch();
    }

    private void RefreshConfig() => ConfigText = _generator.Generate(Binds, UnrecognizedLines, TrainingMode);

    private void ApplySearch()
    {
        var query = SearchText.Trim();
        var active = query.Length > 0;
        var matches = 0;
        foreach (var key in _keysById.Values)
        {
            var match = active && key.Bind != null
                && (key.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || key.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || key.Bind.CommandText.Contains(query, StringComparison.OrdinalIgnoreCase));
            key.IsMatch = match;
            key.IsDimmed = active && !match;
            if (match) matches++;
        }
        SearchSummary = active ? $"Znaleziono: {matches}" : $"Bindów: {Binds.Count}";
    }
}
