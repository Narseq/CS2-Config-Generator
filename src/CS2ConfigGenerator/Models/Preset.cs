namespace CS2ConfigGenerator.Models;

public sealed class Preset
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Czy wczytanie presetu włącza tryb treningowy.</summary>
    public bool TrainingMode { get; set; }
    /// <summary>Czy preset czyści wszystkie bindy przed wczytaniem (Reset).</summary>
    public bool Clear { get; set; }
    public List<PresetBind> Binds { get; set; } = new();
}

public sealed class PresetBind
{
    public string Key { get; set; } = "";
    public BindCategory Category { get; set; }
    public List<string> Commands { get; set; } = new();
}
