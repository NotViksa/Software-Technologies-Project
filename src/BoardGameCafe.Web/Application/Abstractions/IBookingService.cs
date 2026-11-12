using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface IBookingService
{
     Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct); 
     Task<Result> CancelAsync(Guid reservationId, CancellationToken ct);
     Task<Result> ChangeStatusAsync(Guid reservationId, ReservationStatus newStatus, CancellationToken ct);
}