using Microsoft.AspNetCore.Mvc;
using Scavengy.ServiceModel;
using ServiceStack;

namespace Scavengy.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IServiceGateway _gateway;

    public HomeController(ILogger<HomeController> logger, IServiceGateway gateway)
    {
        _logger = logger;
        _gateway = gateway;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var hunts = await _gateway.SendAsync(new QueryHunts());
            return View(hunts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}