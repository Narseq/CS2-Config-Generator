using System.Text;
using CS2ConfigGenerator.Models;

namespace CS2ConfigGenerator.Services;

public sealed class ConfigGeneratorService
{
    public const string TrainingNote = "// tylko trening / lokalny serwer";

    private static readonly string[] TrainingPrefixes = { "sv_", "mp_", "bot_", "ent_", "cl_showpos" };
    private static readonly HashSet<string> TrainingTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "noclip", "god", "notarget", "give", "impulse", "kill", "buddha"
    };

    private readonly DataService _data;

    public ConfigGeneratorService(DataService data) => _data = data;

    /// <summary>Czy bind zawiera komendy niedozwolone na oficjalnych serwerach.</summary>
    public bool IsTrainingBind(KeyBind bind) =>
        bind.Category == BindCategory.Trening || bind.Commands.Any(IsTrainingCommand);

    private bool IsTrainingCommand(string command)
    {
        var trimmed = command.Trim();
        if (_data.IsTrainingCommand(trimmed)) return true;
        var token = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        // "toggle sv_..." i "sv_..." wymagają sv_cheats / lokalnego serwera
        if (token.Equals("toggle", StringComparison.OrdinalIgnoreCase))
            token = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1) ?? "";
        return TrainingTokens.Contains(token) || TrainingPrefixes.Any(p => token.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    public string Generate(IEnumerable<KeyBind> binds, IEnumerable<string> unrecognizedLines, bool trainingMode, DateTime? now = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// autoexec.cfg — wygenerowano w CS2 Config Generator");
        sb.AppendLine($"// Data: {(now ?? DateTime.Now):yyyy-MM-dd HH:mm}");

        var all = binds.Where(b => b.Commands.Count > 0).ToList();
        var skipped = new List<KeyBind>();

        foreach (var category in Enum.GetValues<BindCategory>())
        {
            var group = new List<KeyBind>();
            foreach (var bind in all.Where(b => b.Category == category))
            {
                if (IsTrainingBind(bind) && !trainingMode) skipped.Add(bind);
                else group.Add(bind);
            }
            if (group.Count == 0) continue;

            sb.AppendLine();
            sb.AppendLine($"// {BindCategoryInfo.ConfigHeader(category)}");
            if (group.Any(IsTrainingBind))
            {
                sb.AppendLine(TrainingNote);
                sb.AppendLine($"sv_cheats 1 {TrainingNote}");
            }
            foreach (var bind in group)
            {
                var line = bind.ToConfigLine();
                sb.AppendLine(IsTrainingBind(bind) && category != BindCategory.Trening ? $"{line} {TrainingNote}" : line);
            }
        }

        if (skipped.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("// Pominięte — komendy tylko do treningu (włącz „Tryb treningowy”, aby je dodać)");
            foreach (var bind in skipped)
                sb.AppendLine($"// {bind.ToConfigLine()}");
        }

        var extra = unrecognizedLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (extra.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("// Zaimportowane (nie rozpoznane)");
            foreach (var line in extra) sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
