namespace CS2ConfigGenerator.Models;

/// <summary>Klawisz na wizualnej klawiaturze (pozycje w jednostkach siatki).</summary>
public sealed class KeyDefinition
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 1;
    public double Height { get; init; } = 1;
}

public sealed class KeySectionDefinition
{
    public string Name { get; init; } = "";
    public double Width { get; init; }
    public double Height { get; init; }
    public List<KeyDefinition> Keys { get; init; } = new();
}

// Format pliku Data/keys.json
public sealed class KeysFile
{
    public List<KeySectionJson> Sections { get; set; } = new();
}

public sealed class KeySectionJson
{
    public string Name { get; set; } = "";
    public List<KeyRowJson> Rows { get; set; } = new();
}

public sealed class KeyRowJson
{
    public double Y { get; set; }
    public List<KeyEntryJson> Keys { get; set; } = new();
}

public sealed class KeyEntryJson
{
    public string? Id { get; set; }
    public string? Label { get; set; }
    public double W { get; set; } = 1;
    public double H { get; set; } = 1;
    /// <summary>Puste miejsce o podanej szerokości (gdy brak Id).</summary>
    public double Gap { get; set; }
}
