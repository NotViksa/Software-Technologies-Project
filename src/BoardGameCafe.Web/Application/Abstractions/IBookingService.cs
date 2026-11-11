using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface IBookingService
{
    Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct);
    Task<Result> CancelAsync(Guid reservationId, CancellationToken ct);
}
