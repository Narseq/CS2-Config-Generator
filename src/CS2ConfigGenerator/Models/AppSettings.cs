namespace CS2ConfigGenerator.Models;

public sealed class AppSettings
{
    /// <summary>Folder cfg gry CS2 (game\csgo\cfg).</summary>
    public string? Cs2CfgPath { get; set; }
    public bool TrainingMode { get; set; }
    public string? LastDirectory { get; set; }
}
