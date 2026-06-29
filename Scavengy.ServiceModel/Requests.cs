using System.ComponentModel.DataAnnotations;
using ServiceStack;

namespace Scavengy.ServiceModel;

[Route("/hunts", "POST")]
public class CreateHunt : IPost, IReturn<Hunt>
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    [Display(Name = "Hunt Location")]
    public string HuntLocation { get; set; } = string.Empty;
}

[Route("/hunts", "GET")]
public class QueryHunts : IGet, IReturn<List<Hunt>>
{
}

[Route("/hunts/{Id}", "GET")]
public class GetHunt : IGet, IReturn<Hunt>
{
    public int Id { get; set; }
}

[Route("/hunts/{Id}", "PUT")]
public class UpdateHunt : IPut, IReturn<Hunt>
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

[Route("/hunts/{Id}", "DELETE")]
public class DeleteHunt : IDelete, IReturnVoid
{
    public int Id { get; set; }
}