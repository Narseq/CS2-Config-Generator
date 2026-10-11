using CS2ConfigGenerator.Models;

namespace CS2ConfigGenerator.Services;

public sealed class PresetService
{
    public IReadOnlyList<Preset> Presets { get; }

    public PresetService() : this(DataService.DataDirectory) { }

    public PresetService(string dataDirectory)
    {
        Presets = DataService.Load<List<Preset>>(Path.Combine(dataDirectory, "presets.json"));
    }

    public IEnumerable<KeyBind> ToBinds(Preset preset) =>
        preset.Binds.Select(b => new KeyBind
        {
            Key = b.Key.ToLowerInvariant(),
            Category = b.Category,
            Commands = new List<string>(b.Commands)
        });
}
