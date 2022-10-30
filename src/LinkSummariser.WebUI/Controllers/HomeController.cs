using LinkSummariser.WebUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LinkSummariser.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly IConfiguration _cfg;

    public HomeController(
        ILogger<HomeController> logger,
        IConfiguration cfg)
    {
        _logger = logger;
        _cfg = cfg;
    }

    public IActionResult Index()
    {
        var viewModel = new IndexViewModel();

        return View(viewModel);
    }
}
