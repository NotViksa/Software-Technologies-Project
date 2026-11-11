using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct);
}
