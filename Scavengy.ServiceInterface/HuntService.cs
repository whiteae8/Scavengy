using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Scavengy.Data;
using Scavengy.ServiceModel;
using ServiceStack;

namespace Scavengy.ServiceInterface;

public class HuntService : Service
{
    private const double MaxLandmarkDistanceMiles = 10;

    private const string ClueListSchemaJson = """
        {
            "type": "object",
            "properties": {
                "clues": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "clueText": { "type": "string" },
                            "locationName": { "type": "string" }
                        },
                        "required": ["clueText", "locationName"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["clues"],
            "additionalProperties": false
        }
        """;
    
    private record ClueListDto(List<Clue> Clues);

    private readonly ScavengyDbContext _db;
    private readonly ILogger<HuntService> _logger;
    private readonly ChatClient _chatClient;
    private readonly IPlacesService _places;
    private readonly string _clueGenerationMode;
    private readonly int _clueCount;

    public HuntService(ScavengyDbContext db, IConfiguration config, ILogger<HuntService> logger, ChatClient chatClient, IPlacesService places)
    {
        _db = db;
        _logger = logger;
        _chatClient = chatClient;
        _places = places;
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
        var clues = await GenerateClues(hunt.HuntLocation, hunt.Id);
        if (clues.Count <= 0) return hunt;

        _db.Clues.AddRange(clues);
        await _db.SaveChangesAsync();

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

        if (hunt.Clues.Count != 0) return hunt;
        var clues = await GenerateClues(hunt.HuntLocation, hunt.Id);
        if (clues.Count == 0) throw new Exception("Clue generation failed to produce any clues");

        _db.Clues.AddRange(clues);
        await _db.SaveChangesAsync();

        return hunt;
    }

    private async Task<List<Clue>> GenerateClues(string huntLocation, int huntId)
    {
        if (_clueGenerationMode != "azure") return [];

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    """
                    You design scavenger hunts. Each clue points to ONE real, well-known,
                    currently-existing landmark that is free and open to the general public
                    (parks, monuments, public squares, notable bridges, historic buildings).

                    Rules:
                    - Every landmark must be a real, verifiable place in the requested location.
                      Never invent places, names, or addresses. If unsure a place exists, pick
                      a different, more famous one.
                    - Prefer iconic, widely-recognized landmarks, close to downtown or main tourist district.
                    - If the requested location is too small to have enough well-known standalone
                      landmarks, you may pull from the immediately surrounding area, but ONLY within
                      about a 15-minute drive (roughly 10 miles) of the requested location. Never
                      reach into a different city's downtown or anywhere requiring a long drive.
                    - clueText and locationName must contain ONLY actual clue and landmark content.
                      Never ask a question, request clarification, or comment on the task itself in
                      these fields — always produce your best clues using your own judgment instead.
                    - Each clueText is a short riddle (1-2 sentences) that hints at the landmark
                      through its history, appearance, or reputation. 
                    - The clueText must NOT contain any word from the landmark's name, nor the
                      singular, plural, or root form of those words. Example: for a landmark
                      named "Printers Alley", the words "printer", "printers", and "alley" are
                      all forbidden in the clue. Describe the place only through its history,
                      appearance, function, or reputation, using different vocabulary.
                    
                    Example of a BAD clue for "Printers Alley" (leaks the name):
                      "Follow the neon into a narrow alley that once housed printers by day."
                    Example of a GOOD clue for "Printers Alley":
                      "Follow the neon into a narrow downtown passage that once buzzed with
                       ink-stained trades by day and now hums with nightlife after dark."
                    - No two clues may point to the same landmark.
                    - locationName: the landmark's common name.
                    """),
                new UserChatMessage(
                    $"Create exactly {_clueCount} clues for a scavenger hunt in {huntLocation}.")
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "clue_list", BinaryData.FromString(ClueListSchemaJson), jsonSchemaIsStrict: true)
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options);
            var parsed = JsonSerializer.Deserialize<ClueListDto>(completion.Value.Content[0].Text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var huntPlace = await _places.FindPlaceAsync(huntLocation, huntLocation, CancellationToken.None);

            var clues = parsed!.Clues;
            var geocodedClues = new List<Clue>();
            foreach (var clue in clues)
            {
                var place = await _places.FindPlaceAsync($"{clue.LocationName}, {huntLocation}", huntLocation, CancellationToken.None);
                if (place is null)
                {
                    _logger.LogWarning("No Places match for {LocationName}; dropping clue", clue.LocationName);
                    continue;
                }

                if (huntPlace is not null &&
                    DistanceMiles(huntPlace.Latitude, huntPlace.Longitude, place.Latitude, place.Longitude) > MaxLandmarkDistanceMiles)
                {
                    _logger.LogWarning("{LocationName} is more than {MaxMiles} miles from {HuntLocation}; dropping clue",
                        clue.LocationName, MaxLandmarkDistanceMiles, huntLocation);
                    continue;
                }

                clue.HuntId = huntId;
                clue.LocationAddress = place.Address;
                clue.Latitude = place.Latitude;
                clue.Longitude = place.Longitude;
                clue.ClueIndex = geocodedClues.Count + 1;
                geocodedClues.Add(clue);
            }

            return geocodedClues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI clue generation failed for {HuntLocation}", huntLocation);
            return [];
        }
    }

    private static double DistanceMiles(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMiles = 3958.8;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMiles * c;
    }
}