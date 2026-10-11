using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CS2ConfigGenerator.Services;

public sealed partial class Cs2PathService
{
    public const string DefaultCfgPath =
        @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg";

    private const string RelativeCfgPath =
        @"steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg";

    /// <summary>Szuka folderu cfg: ścieżka domyślna, potem biblioteki Steam. Zwraca null, gdy nie znaleziono.</summary>
    public string? Detect()
    {
        if (Directory.Exists(DefaultCfgPath)) return DefaultCfgPath;

        foreach (var library in SteamLibraries())
        {
            var candidate = Path.Combine(library, RelativeCfgPath);
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>
    /// Użytkownik może wskazać folder cfg, game\csgo, folder gry albo bibliotekę Steam —
    /// zwracamy docelowy folder cfg, jeśli da się go wywnioskować.
    /// </summary>
    public string NormalizeSelectedFolder(string selected)
    {
        var relatives = new[] { "", "cfg", @"csgo\cfg", @"game\csgo\cfg", RelativeCfgPath, @"common\Counter-Strike Global Offensive\game\csgo\cfg" };
        foreach (var relative in relatives)
        {
            var candidate = Path.Combine(selected, relative);
            if (relative.Length == 0
                ? Path.GetFileName(selected.TrimEnd('\\', '/')).Equals("cfg", StringComparison.OrdinalIgnoreCase) && Directory.Exists(candidate)
                : Directory.Exists(candidate))
                return Path.GetFullPath(candidate);
        }
        return selected;
    }

    public string GetAutoexecPath(string cfgFolder) => Path.Combine(cfgFolder, "autoexec.cfg");

    private static IEnumerable<string> SteamLibraries()
    {
        string? steamPath = null;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            steamPath = key?.GetValue("SteamPath") as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // brak dostępu do rejestru — pomijamy
        }
        if (string.IsNullOrWhiteSpace(steamPath)) yield break;

        steamPath = steamPath.Replace('/', '\\');
        yield return steamPath;

        var vdf = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
        if (!File.Exists(vdf)) yield break;

        string text;
        try { text = File.ReadAllText(vdf); }
        catch (IOException) { yield break; }
        catch (UnauthorizedAccessException) { yield break; }

        foreach (Match m in LibraryPathRegex().Matches(text))
            yield return m.Groups[1].Value.Replace(@"\\", @"\");
    }

    [GeneratedRegex("\"path\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryPathRegex();
}
