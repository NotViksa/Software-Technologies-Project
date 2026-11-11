using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameCafe.Web.Presentation.Controllers;

public class GamesController : Controller
{
    private readonly ICatalogueService _catalogueService;

    public GamesController(ICatalogueService catalogueService)
    {
        _catalogueService = catalogueService;
    }

    public async Task<IActionResult> Index(string? search, string? category, CancellationToken ct)
    {
        var games = await _catalogueService.GetGamesAsync(search, category, ct);
        var categories = await _catalogueService.GetCategoriesAsync(ct);

        return View(new GamesIndexViewModel
        {
            Games = games,
            Categories = categories,
            Search = search,
            Category = category
        });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var game = await _catalogueService.GetGameAsync(id, ct);
        if (game is null)
        {
            return NotFound();
        }

        return View(game);
    }
}
