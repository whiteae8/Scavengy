using Scavengy.ServiceModel;

namespace Scavengy.Models;

public class HuntRowViewModel
{
    public required Hunt Hunt { get; set; }
    public string? Oob { get; set; }
}
