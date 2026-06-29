using Scavengy.ServiceModel;

namespace Scavengy.Models;

public class CreateHuntViewModel
{
    public CreateHunt Form { get; set; } = new();
    public string? Error { get; set; }
}