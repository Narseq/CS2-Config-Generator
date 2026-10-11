namespace CS2ConfigGenerator.Models;

public enum BindCategory
{
    Granaty,
    SlotyBroni,
    Zakupy,
    Ruch,
    Komunikacja,
    Trening,
    WlasnaKomenda
}

public static class BindCategoryInfo
{
    public static string Name(BindCategory c) => c switch
    {
        BindCategory.Granaty => "Granaty",
        BindCategory.SlotyBroni => "Sloty broni",
        BindCategory.Zakupy => "Zakupy",
        BindCategory.Ruch => "Ruch",
        BindCategory.Komunikacja => "Komunikacja",
        BindCategory.Trening => "Trening",
        _ => "Własna komenda"
    };

    public static string Icon(BindCategory c) => c switch
    {
        BindCategory.Granaty => "💥",
        BindCategory.SlotyBroni => "🔫",
        BindCategory.Zakupy => "🛒",
        BindCategory.Ruch => "🏃",
        BindCategory.Komunikacja => "💬",
        BindCategory.Trening => "🎯",
        _ => "⚙"
    };

    public static string Color(BindCategory c) => c switch
    {
        BindCategory.Granaty => "#F59E0B",
        BindCategory.SlotyBroni => "#3B82F6",
        BindCategory.Zakupy => "#22C55E",
        BindCategory.Ruch => "#06B6D4",
        BindCategory.Komunikacja => "#A855F7",
        BindCategory.Trening => "#EF4444",
        _ => "#E5E7EB"
    };

    /// <summary>Nagłówek sekcji w wygenerowanym pliku cfg.</summary>
    public static string ConfigHeader(BindCategory c) => c == BindCategory.WlasnaKomenda ? "Własne komendy" : Name(c);
}
