using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scavengy.ServiceModel;

public class Hunt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string HuntLocation { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public List<Clue> Clues { get; set; } = [];
}

public class Clue
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("Hunt")]
    public int HuntId { get; set; }
    
    [Required]
    public int ClueIndex { get; set; }
    
    [Required]
    public string ClueText { get; set; } = string.Empty;
    
    [Required]
    public string LocationName { get; set; } = string.Empty;
    
    public string? LocationAddress { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
}