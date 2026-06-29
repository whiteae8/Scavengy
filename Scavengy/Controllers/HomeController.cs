using Microsoft.AspNetCore.Mvc;
using Scavengy.Models;
using Scavengy.ServiceModel;
using ServiceStack;
using ServiceStack.Mvc;

namespace Scavengy.Controllers;

public class HomeController : ServiceStackController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
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
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpGet]
    public IActionResult CreateHuntForm() =>
        PartialView("_CreateHuntForm", new CreateHuntViewModel());

    [HttpPost]
    public async Task<IActionResult> CreateHunt(CreateHunt request)
    {
        try
        {
            var hunt = await Gateway.SendAsync(request);
            Response.Headers.Append("HX-Trigger", "huntCreated");
            return PartialView("_HuntRow", hunt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return PartialView("_CreateHuntForm", new CreateHuntViewModel
            {
                Form = request,
                Error = "Failed to create hunt. Please try again later."
            });
        }
    }
}