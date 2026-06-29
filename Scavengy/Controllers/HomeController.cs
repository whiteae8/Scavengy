using Microsoft.AspNetCore.Mvc;
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
}