namespace CS2ConfigGenerator.Models;

public sealed class KeyBind
{
    public string Key { get; init; } = "";
    public BindCategory Category { get; init; } = BindCategory.WlasnaKomenda;
    public List<string> Commands { get; init; } = new();

    public string CommandText => string.Join("; ", Commands);

    public string ToConfigLine() => $"bind \"{Key}\" \"{CommandText}\"";
}
