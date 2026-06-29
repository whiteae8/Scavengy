using Microsoft.EntityFrameworkCore;
using Scavengy.ServiceModel;

namespace Scavengy.Data;

public class ScavengyDbContext : DbContext
{
    public ScavengyDbContext(DbContextOptions<ScavengyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Hunt> Hunts { get; set; } = null!;
    public DbSet<Clue> Clues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Hunt>()
            .Navigation(x => x.Clues)
            .AutoInclude();
    }
}
