
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Presentation.Controllers;

[Authorize(Roles = Roles.Staff)]
public class StaffReservationsController : Controller
{
    private readonly AppDbContext _db;

    public StaffReservationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken ct)
    {
        var reservations = await _db.Reservations
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Table)
            .ThenInclude(x => x.Zone)
            .OrderByDescending(
                x => x.Status == ReservationStatus.Pending)
            .ThenBy(x => x.StartAt)
            .ToListAsync(ct);

        return View(reservations);
    }
}
