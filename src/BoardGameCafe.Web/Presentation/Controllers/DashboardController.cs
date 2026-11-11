using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameCafe.Web.Presentation.Controllers;

[Authorize(Roles = Roles.Staff)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(ct);
        return View(stats);
    }
}
