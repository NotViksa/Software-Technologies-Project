using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class CatalogueService : ICatalogueService
{
    private readonly AppDbContext _db;

    public CatalogueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CafeDto?> GetCafeAsync(CancellationToken ct)
    {
        return await _db.Cafes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CafeDto(x.Name, x.Address, x.Description, x.TimeZoneId, x.OpeningTime, x.ClosingTime))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CatalogueSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var games = await _db.Games.CountAsync(x => x.IsActive, ct);
        var tables = await _db.Tables.CountAsync(x => x.IsActive, ct);
        var availableCopies = await _db.GameCopies.CountAsync(x => x.Status == GameCopyStatus.Available, ct);
        return new CatalogueSummaryDto(games, tables, availableCopies);
    }

    public async Task<IReadOnlyList<GameListItemDto>> GetGamesAsync(string? search, string? category, CancellationToken ct)
    {
        var query = _db.Games.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Category.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query
            .OrderBy(x => x.Title)
            .Select(x => new GameListItemDto(
                x.Id,
                x.Title,
                x.Category,
                x.MinPlayers,
                x.MaxPlayers,
                x.DurationMinutes,
                x.Complexity,
                x.Copies.Count(c => c.Status == GameCopyStatus.Available)))
            .ToListAsync(ct);
    }

    public async Task<GameDetailsDto?> GetGameAsync(Guid id, CancellationToken ct)
    {
        return await _db.Games
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new GameDetailsDto(
                x.Id,
                x.Title,
                x.Description,
                x.Category,
                x.MinPlayers,
                x.MaxPlayers,
                x.DurationMinutes,
                x.Complexity,
                x.Copies.Count,
                x.Copies.Count(c => c.Status == GameCopyStatus.Available)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct)
    {
        return await _db.Games
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
    }
}
