using Microsoft.AspNetCore.Mvc;
using Scavengy.Data;
using Microsoft.Extensions.Logging;

namespace Scavengy.Web.Controllers;

public class HomeController : Controller
{
    private readonly ScavengyDbContext _db;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ScavengyDbContext db, ILogger<HomeController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
}