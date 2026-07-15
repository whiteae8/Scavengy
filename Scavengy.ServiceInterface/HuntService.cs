using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Scavengy.Data;
using Scavengy.ServiceModel;
using ServiceStack;

namespace Scavengy.ServiceInterface;

public class HuntService : Service
{
    // Templated clues used while AiClueGeneration:Mode is "fake" — no network
    // call, no cost. "none" (the default) always returns no clues, so hunt
    // creation exercises the "clue generation failed" path with zero risk.
    // A future real (Claude-backed) mode plugs into this same switch.
    private static readonly (string ClueText, string LocationName)[] ClueTemplates =
    [
        ("Find the spot in {0} where people gather to watch the sunset.", "Sunset Viewpoint"),
        ("Look for the oldest building near the heart of {0}.", "Historic Landmark"),
        ("Somewhere in {0}, locals go here for the best view of downtown.", "Downtown Overlook"),
        ("Search for the landmark most photographed by visitors to {0}.", "Popular Landmark")
    ];

    private readonly ScavengyDbContext _db;
    private readonly string _clueGenerationMode;
    private readonly int _clueCount;

    public HuntService(ScavengyDbContext db, IConfiguration config)
    {
        _db = db;
        _clueGenerationMode = config["AiClueGeneration:Mode"] ?? "none";
        _clueCount = config.GetValue("AiClueGeneration:ClueCount", 4);
    }

    public async Task<Hunt> Post(CreateHunt request)
    {
        var hunt = new Hunt
        {
            Title = request.Title,
            HuntLocation = request.HuntLocation,
            CreatedDate = DateTime.UtcNow
        };
        _db.Hunts.Add(hunt);
        await _db.SaveChangesAsync();

        // Hunt creation always succeeds regardless of clue generation outcome.
        var clues = GenerateClues(hunt.HuntLocation);
        if (clues.Count > 0)
        {
            foreach (var clue in clues) clue.HuntId = hunt.Id;
            _db.Clues.AddRange(clues);
            await _db.SaveChangesAsync();
        }

        return hunt;
    }

    public async Task<List<Hunt>> Get(QueryHunts request)
    {
        return await _db.Hunts
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }

    public async Task<Hunt?> Get(GetHunt request)
    {
        return await _db.Hunts
            .FirstOrDefaultAsync(x => x.Id == request.Id);
    }

    public async Task<Hunt> Put(UpdateHunt request)
    {
        var hunt = await _db.Hunts.FindAsync(request.Id);
        if (hunt == null) throw new Exception("Hunt not found");

        hunt.Title = request.Title;

        await _db.SaveChangesAsync();
        return hunt;
    }

    public async Task Delete(DeleteHunt request)
    {
        var hunt = await _db.Hunts.FindAsync(request.Id);
        if (hunt != null)
        {
            _db.Hunts.Remove(hunt);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<Hunt> Post(GenerateClues request)
    {
        var hunt = await _db.Hunts.FindAsync(request.Id);
        if (hunt == null) throw HttpError.NotFound("Hunt not found");

        // Empty-guard: only generate when the hunt has no clues yet, so retrying
        // (or spamming) this endpoint against an already-populated hunt is a no-op.
        // Future: ask for a confirmation to clear and re-generate clues.
        if (hunt.Clues.Count == 0)
        {
            var clues = GenerateClues(hunt.HuntLocation);
            if (clues.Count > 0)
            {
                foreach (var clue in clues) clue.HuntId = hunt.Id;
                _db.Clues.AddRange(clues);
                await _db.SaveChangesAsync();
            }
        }

        return hunt;
    }

    private List<Clue> GenerateClues(string huntLocation)
    {
        if (_clueGenerationMode != "fake") return [];

        return ClueTemplates
            .Take(_clueCount)
            .Select((template, index) => new Clue
            {
                ClueIndex = index + 1,
                ClueText = string.Format(template.ClueText, huntLocation),
                LocationName = template.LocationName
            })
            .ToList();
    }
}
