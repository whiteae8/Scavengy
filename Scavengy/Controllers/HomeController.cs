using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Scavengy.Models;
using Scavengy.ServiceModel;
using Scavengy.Services;
using ServiceStack;
using ServiceStack.Mvc;

namespace Scavengy.Controllers;

public class HomeController : ServiceStackController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPlacesService _places;

    public HomeController(ILogger<HomeController> logger, IPlacesService places)
    {
        _logger = logger;
        _places = places;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var hunts = await Gateway.SendAsync(new QueryHunts());
            return View(hunts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    [HttpGet]
    public IActionResult CreateHuntForm() =>
        PartialView("_CreateHuntForm", new CreateHuntViewModel());

    [HttpGet]
    public async Task<IActionResult> LocationSuggestions(string? query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var suggestions = await _places.SearchCitiesAsync(query.Trim(), ct);
        return Json(suggestions.Select(s => new { value = s, text = s }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateHunt(CreateHunt request)
    {
        try
        {
            var hunt = await Gateway.SendAsync(request);
            if (hunt.Clues.Count == 0)
            {
                var trigger = JsonSerializer.Serialize(new { huntCreated = true, cluesFailed = true });
                Response.Headers.Append("HX-Trigger", trigger);
            }
            else
            {
                Response.Headers.Append("HX-Trigger", "huntCreated");
            }
            return PartialView("_HuntRow", hunt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            Response.Headers.Append("HX-Retarget", "#createHuntModalContent");
            Response.Headers.Append("HX-Reswap", "innerHTML");
            return PartialView("_CreateHuntForm", new CreateHuntViewModel
            {
                Form = request,
                Error = "Failed to create hunt. Please try again later."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> RenameHuntForm(int id)
    {
        try
        {
            var hunt = await Gateway.SendAsync(new GetHunt { Id = id });
            return PartialView("_RenameHuntForm", new RenameHuntViewModel { Id = id, Title = hunt!.Title });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rename form for hunt {Id}", id);
            return StatusCode(500);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteHunt(int id)
    {
        try
        {
            await Gateway.SendAsync(new DeleteHunt { Id = id });
            Response.Headers.Append("HX-Trigger-After-Swap", "huntDeleted");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete hunt {Id}", id);
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<IActionResult> HuntDetails(int id)
    {
        try
        {
            var hunt = await Gateway.SendAsync(new GetHunt { Id = id });
            if (hunt == null) return NotFound();
            return View(hunt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load details for hunt {Id}", id);
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateClues(int id)
    {
        try
        {
            var hunt = await Gateway.SendAsync(new GenerateClues { Id = id });
            return PartialView("_ClueTable", hunt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate clues for hunt {Id}", id);
            return StatusCode(500);
        }
    }

    [HttpPost]
    public async Task<IActionResult> RenameHunt(RenameHuntViewModel request)
    {
        try
        {
            var hunt = await Gateway.SendAsync(new UpdateHunt { Id = request.Id, Title = request.Title });
            Response.Headers.Append("HX-Trigger", "huntRenamed");
            return PartialView("_HuntRow", hunt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            Response.Headers.Append("HX-Retarget", "#renameHuntModalContent");
            Response.Headers.Append("HX-Reswap", "innerHTML");
            return PartialView("_RenameHuntForm", new RenameHuntViewModel
            {
                Id = request.Id,
                Title = request.Title,
                Error = "Failed to rename hunt. Please try again later."
            });
        }
    }
}