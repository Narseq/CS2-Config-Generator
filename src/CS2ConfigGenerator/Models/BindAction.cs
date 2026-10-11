namespace CS2ConfigGenerator.Models;

public sealed class BindAction
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public BindCategory Category { get; set; }
    public List<string> Commands { get; set; } = new();
    public bool TrainingOnly { get; set; }
}
