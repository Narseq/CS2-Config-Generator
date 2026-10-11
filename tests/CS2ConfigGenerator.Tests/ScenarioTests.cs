using CS2ConfigGenerator.Models;
using CS2ConfigGenerator.Services;
using Xunit;

namespace CS2ConfigGenerator.Tests;

public class ScenarioTests : IDisposable
{
    private readonly DataService _data = new();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "cs2cfg_test_" + Guid.NewGuid().ToString("N"));

    public ScenarioTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, true);

    private static KeyBind Bind(string key, BindCategory category, params string[] commands) =>
        new() { Key = key, Category = category, Commands = commands.ToList() };

    private static List<KeyBind> ScenarioBinds() => new()
    {
        Bind("x", BindCategory.Granaty, "slot7"),
        Bind("c", BindCategory.Granaty, "slot8"),
        Bind("z", BindCategory.Granaty, "slot6"),
        Bind("v", BindCategory.Granaty, "slot10"),
        Bind("kp_end", BindCategory.Zakupy, "buy ak47", "buy vesthelm", "buy hegrenade", "buy flashbang", "buy smokegrenade")
    };

    [Fact]
    public void Scenario_generates_expected_config_and_saves_with_backup()
    {
        var generator = new ConfigGeneratorService(_data);
        var files = new ConfigFileService(_data);

        var text = generator.Generate(ScenarioBinds(), Array.Empty<string>(), trainingMode: false, now: new DateTime(2026, 10, 11, 12, 0, 0));

        Assert.Contains("// Granaty", text);
        Assert.Contains("bind \"x\" \"slot7\"", text);
        Assert.Contains("bind \"c\" \"slot8\"", text);
        Assert.Contains("bind \"z\" \"slot6\"", text);
        Assert.Contains("bind \"v\" \"slot10\"", text);
        Assert.Contains("// Zakupy", text);
        Assert.Contains("bind \"kp_end\" \"buy ak47; buy vesthelm; buy hegrenade; buy flashbang; buy smokegrenade\"", text);

        var path = Path.Combine(_tempDir, "autoexec.cfg");
        var first = files.Save(path, text);
        Assert.Null(first.BackupPath);
        Assert.Equal(text, File.ReadAllText(path));
        Assert.False(File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }), "plik nie powinien mieć BOM");

        var second = files.Save(path, text + "// zmiana\n");
        Assert.NotNull(second.BackupPath);
        Assert.EndsWith($"autoexec_backup_{DateTime.Now:yyyy-MM-dd}.cfg", second.BackupPath);
        Assert.Equal(text, File.ReadAllText(second.BackupPath!));
    }

    [Fact]
    public void Generated_config_round_trips_through_import()
    {
        var generator = new ConfigGeneratorService(_data);
        var files = new ConfigFileService(_data);
        var text = generator.Generate(ScenarioBinds(), Array.Empty<string>(), false);

        var result = files.ParseConfig(text);

        Assert.Empty(result.Unrecognized);
        Assert.Equal(5, result.Binds.Count);
        var ak = result.Binds.Single(b => b.Key == "kp_end");
        Assert.Equal(BindCategory.Zakupy, ak.Category);
        Assert.Equal(5, ak.Commands.Count);
    }

    [Fact]
    public void Import_keeps_unknown_lines_and_last_bind_wins()
    {
        var files = new ConfigFileService(_data);
        var result = files.ParseConfig("""
            // komentarz
            sensitivity "1.2"
            bind "x" "slot7"
            bind x +jump
            bind "f99" "say hi"
            """);

        Assert.Single(result.Binds);
        Assert.Equal("+jump", result.Binds[0].CommandText);
        Assert.Equal(2, result.Unrecognized.Count);
        Assert.Contains("sensitivity \"1.2\"", result.Unrecognized);
    }

    [Fact]
    public void Training_binds_are_skipped_unless_training_mode()
    {
        var generator = new ConfigGeneratorService(_data);
        var binds = new[] { Bind("n", BindCategory.Trening, "noclip"), Bind("m", BindCategory.WlasnaKomenda, "give weapon_ak47") };

        var off = generator.Generate(binds, Array.Empty<string>(), false);
        Assert.DoesNotContain("\nbind \"n\"", off);
        Assert.DoesNotContain("\nbind \"m\"", off);
        Assert.Contains("// bind \"n\" \"noclip\"", off);

        var on = generator.Generate(binds, Array.Empty<string>(), true);
        Assert.Contains("bind \"n\" \"noclip\"", on);
        Assert.Contains("tylko trening / lokalny serwer", on);
        Assert.Contains("bind \"m\" \"give weapon_ak47\" // tylko trening / lokalny serwer", on);
    }

    [Fact]
    public void Presets_only_use_known_keys_and_actions()
    {
        var presets = new PresetService();
        foreach (var preset in presets.Presets)
            foreach (var bind in preset.Binds)
                Assert.True(_data.IsKnownKey(bind.Key), $"{preset.Name}: nieznany klawisz {bind.Key}");

        Assert.Contains(presets.Presets, p => p.Clear);
    }

    [Fact]
    public void Imported_import_rejects_binary_file()
    {
        var path = Path.Combine(_tempDir, "bad.cfg");
        File.WriteAllBytes(path, new byte[] { 1, 2, 0, 3 });
        Assert.Throws<InvalidDataException>(() => new ConfigFileService(_data).ImportFile(path));
    }

    [Fact]
    public void Save_to_missing_folder_throws()
    {
        var files = new ConfigFileService(_data);
        Assert.Throws<DirectoryNotFoundException>(() => files.Save(Path.Combine(_tempDir, "nope", "autoexec.cfg"), "x"));
    }
}
