using Microsoft.EntityFrameworkCore;
using Scavengy.Data;
using Scavengy.ServiceModel;
using ServiceStack;

namespace Scavengy.ServiceInterface;

public class HuntService : Service
{
    private readonly ScavengyDbContext _db;
    public HuntService(ScavengyDbContext db) => _db = db;
    
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
}