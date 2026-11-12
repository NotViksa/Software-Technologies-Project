using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface IAvailabilityService
{
    Task<AvailabilityResult> FindAsync(AvailabilityRequest request, CancellationToken ct);
}