using System.Text;
using System.Text.RegularExpressions;
using CS2ConfigGenerator.Models;

namespace CS2ConfigGenerator.Services;

public sealed record SaveResult(string Path, string? BackupPath);

public sealed class ImportResult
{
    public List<KeyBind> Binds { get; } = new();
    public List<string> Unrecognized { get; } = new();
}

public sealed partial class ConfigFileService
{
    private const long MaxImportBytes = 2 * 1024 * 1024;

    private static readonly Regex QuotedBind = QuotedBindRegex();
    private static readonly Regex PlainBind = PlainBindRegex();

    private readonly DataService _data;

    public ConfigFileService(DataService data) => _data = data;

    /// <summary>Zapisuje plik; istniejący plik trafia najpierw do kopii zapasowej.</summary>
    public SaveResult Save(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Folder nie istnieje: {directory}");

        string? backup = null;
        if (File.Exists(path))
        {
            backup = BuildBackupPath(path, DateTime.Now);
            File.Copy(path, backup);
        }

        // UTF-8 bez BOM — BOM na początku autoexec.cfg psuje pierwszą linię.
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return new SaveResult(path, backup);
    }

    public static string BuildBackupPath(string path, DateTime now)
    {
        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var candidate = Path.Combine(directory, $"{name}_backup_{now:yyyy-MM-dd}{ext}");
        if (File.Exists(candidate))
            candidate = Path.Combine(directory, $"{name}_backup_{now:yyyy-MM-dd_HHmmss}{ext}");
        return candidate;
    }

    public ImportResult ImportFile(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Nie znaleziono pliku.", path);
        if (info.Length > MaxImportBytes)
            throw new InvalidDataException("Plik jest za duży jak na config CS2 (maks. 2 MB).");

        var text = File.ReadAllText(path);
        if (text.Contains('\0'))
            throw new InvalidDataException("To nie jest plik tekstowy — wybierz poprawny plik .cfg.");
        return ParseConfig(text);
    }

    public ImportResult ParseConfig(string text)
    {
        var result = new ImportResult();
        var byKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.TrimStart('\uFEFF').Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;

            if (!TryParseBind(line, out var key, out var commandText) || !_data.IsKnownKey(key))
            {
                result.Unrecognized.Add(line);
                continue;
            }

            var commands = commandText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (commands.Count == 0)
            {
                result.Unrecognized.Add(line);
                continue;
            }

            var bind = new KeyBind { Key = key.ToLowerInvariant(), Category = _data.InferCategory(commands), Commands = commands };
            // Ostatni bind na danym klawiszu wygrywa (tak samo jak w grze).
            if (byKey.TryGetValue(bind.Key, out var index)) result.Binds[index] = bind;
            else
            {
                byKey[bind.Key] = result.Binds.Count;
                result.Binds.Add(bind);
            }
        }
        return result;
    }

    private static bool TryParseBind(string line, out string key, out string commands)
    {
        var match = QuotedBind.Match(line);
        if (!match.Success) match = PlainBind.Match(line);
        key = match.Success ? match.Groups["key"].Value : "";
        commands = match.Success ? match.Groups["cmd"].Value.Trim() : "";
        return match.Success && key.Length > 0;
    }

    [GeneratedRegex("^bind\\s+\"(?<key>[^\"]+)\"\\s+\"(?<cmd>[^\"]*)\"\\s*(//.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex QuotedBindRegex();

    [GeneratedRegex("^bind\\s+\"?(?<key>[^\"\\s]+)\"?\\s+(?<cmd>[^\"]+?)\\s*(//.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex PlainBindRegex();
}
