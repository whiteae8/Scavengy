namespace Scavengy.Models;

public class RenameHuntViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Error { get; set; }
}