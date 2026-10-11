using System.Text.Json;
using System.Text.Json.Serialization;
using CS2ConfigGenerator.Models;

namespace CS2ConfigGenerator.Services;

/// <summary>Ładuje statyczne dane z folderu Data: klawisze i akcje.</summary>
public sealed class DataService
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    public IReadOnlyList<KeySectionDefinition> Sections { get; }
    public IReadOnlyList<BindAction> Actions { get; }

    private readonly HashSet<string> _keyIds;
    private readonly HashSet<string> _trainingCommands;
    private readonly Dictionary<string, BindCategory> _commandCategories;

    public DataService() : this(DataDirectory) { }

    public DataService(string dataDirectory)
    {
        var keys = Load<KeysFile>(Path.Combine(dataDirectory, "keys.json"));
        Sections = keys.Sections.Select(BuildSection).ToList();
        Actions = Load<List<BindAction>>(Path.Combine(dataDirectory, "actions.json"));

        _keyIds = new HashSet<string>(Sections.SelectMany(s => s.Keys).Select(k => k.Id), StringComparer.OrdinalIgnoreCase);
        _trainingCommands = new HashSet<string>(
            Actions.Where(a => a.TrainingOnly).SelectMany(a => a.Commands), StringComparer.OrdinalIgnoreCase);
        _commandCategories = new Dictionary<string, BindCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in Actions)
            foreach (var command in action.Commands)
                _commandCategories.TryAdd(command, action.Category);
    }

    public bool IsKnownKey(string key) => _keyIds.Contains(key);

    public IEnumerable<BindAction> ActionsFor(BindCategory category) => Actions.Where(a => a.Category == category);

    public bool IsTrainingCommand(string command) => _trainingCommands.Contains(command.Trim());

    /// <summary>Zgaduje kategorię binda na podstawie znanych akcji (używane przy imporcie).</summary>
    public BindCategory InferCategory(IEnumerable<string> commands)
    {
        foreach (var command in commands)
            if (_commandCategories.TryGetValue(command.Trim(), out var category))
                return category;
        return BindCategory.WlasnaKomenda;
    }

    public static T Load<T>(string path) where T : new()
    {
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, JsonOptions) ?? new T();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"Nie można wczytać pliku danych: {path}\n{ex.Message}", ex);
        }
    }

    private static KeySectionDefinition BuildSection(KeySectionJson json)
    {
        var keys = new List<KeyDefinition>();
        double width = 0, height = 0;
        foreach (var row in json.Rows)
        {
            double x = 0;
            foreach (var entry in row.Keys)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    x += entry.Gap;
                    continue;
                }
                keys.Add(new KeyDefinition
                {
                    Id = entry.Id,
                    Label = entry.Label ?? entry.Id.ToUpperInvariant(),
                    X = x,
                    Y = row.Y,
                    Width = entry.W,
                    Height = entry.H
                });
                x += entry.W;
                height = Math.Max(height, row.Y + entry.H);
            }
            width = Math.Max(width, x);
        }
        return new KeySectionDefinition { Name = json.Name, Width = width, Height = height, Keys = keys };
    }
}
