using System.Text.Json;
using CS2ConfigGenerator.Models;

namespace CS2ConfigGenerator.Services;

public sealed class SettingsService
{
    private readonly string _path;

    public SettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CS2ConfigGenerator", "settings.json")) { }

    public SettingsService(string path) => _path = path;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new AppSettings();
            using var stream = File.OpenRead(_path);
            return JsonSerializer.Deserialize<AppSettings>(stream, DataService.JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings(); // uszkodzone ustawienia nie mogą blokować startu aplikacji
        }
    }

    /// <summary>Zwraca false, jeśli nie udało się zapisać (np. brak uprawnień).</summary>
    public bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, DataService.JsonOptions));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
