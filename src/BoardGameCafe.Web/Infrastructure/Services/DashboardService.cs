using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var todayReservations = await _db.Reservations.CountAsync(x => x.StartAt >= today && x.StartAt < tomorrow, ct);
        var pendingReservations = await _db.Reservations.CountAsync(x => x.Status == ReservationStatus.Pending, ct);
        var activeSessions = await _db.Sessions.CountAsync(x => x.EndedAt == null, ct);
        var overdueLoans = await _db.Loans.CountAsync(x => x.ReturnedAt == null && x.DueAt < now, ct);
        var availableCopies = await _db.GameCopies.CountAsync(x => x.Status == GameCopyStatus.Available, ct);
        var damagedCopies = await _db.GameCopies.CountAsync(x => x.Status == GameCopyStatus.Damaged, ct);
        var games = await _db.Games.CountAsync(x => x.IsActive, ct);
        var tables = await _db.Tables.CountAsync(x => x.IsActive, ct);
        var zones = await _db.Zones.CountAsync(x => x.IsActive, ct);

        return new DashboardStatsDto(
            todayReservations,
            pendingReservations,
            activeSessions,
            overdueLoans,
            availableCopies,
            damagedCopies,
            games,
            tables,
            zones);
    }
}
