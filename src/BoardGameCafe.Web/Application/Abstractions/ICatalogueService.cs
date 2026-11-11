using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface ICatalogueService
{
    Task<CafeDto?> GetCafeAsync(CancellationToken ct);
    Task<CatalogueSummaryDto> GetSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<GameListItemDto>> GetGamesAsync(string? search, string? category, CancellationToken ct);
    Task<GameDetailsDto?> GetGameAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct);
}
