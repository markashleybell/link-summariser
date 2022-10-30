using LinkSummariser.WebUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LinkSummariser.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger) =>
        _logger = logger;

    public IActionResult Index() =>
        View(new IndexViewModel());
}
