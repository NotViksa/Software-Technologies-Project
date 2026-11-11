using System.Diagnostics;
using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameCafe.Web.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly ICatalogueService _catalogueService;

    public HomeController(ICatalogueService catalogueService)
    {
        _catalogueService = catalogueService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cafe = await _catalogueService.GetCafeAsync(ct);
        var summary = await _catalogueService.GetSummaryAsync(ct);
        var games = await _catalogueService.GetGamesAsync(null, null, ct);

        return View(new HomeViewModel
        {
            Cafe = cafe,
            Summary = summary,
            Games = games.Take(6).ToList()
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
