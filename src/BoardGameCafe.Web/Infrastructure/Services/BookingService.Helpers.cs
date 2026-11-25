using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public partial class BookingService
{
    private async Task<Cafe?> ResolveActiveCafeAsync(Guid? cafeId, CancellationToken ct)
    {
        var query = _db.Cafes.AsNoTracking().Where(c => c.IsActive);
        if (cafeId.HasValue)
        {
            query = query.Where(c => c.Id == cafeId.Value);
        }
        
        return await query.OrderBy(c => c.Name).FirstOrDefaultAsync(ct);
    }
    
    private async Task<(Game? Game, List<GameCopy> Copies, Result? Error)> LoadRequestedGameAsync(Guid? gameId, CancellationToken ct)
    {
        if (!gameId.HasValue)
        {
            return (null, [], null);
        }
        
        var game = await _db.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId.Value && g.IsActive, ct);
        
        if (game is null)
        {
            return (null, [], Result.Failure(ErrorCodes.NotFound, "Requested game was not found or is inactive."));
        }
        
        var copies = await _db.GameCopies
            .AsNoTracking()
            .Where(c => c.GameId == gameId.Value)
            .ToListAsync(ct);
        
        return (game, copies, null);
    }
    
    private static Reservation BuildReservationEntity(
        CreateReservationCommand command,
        Table table,
        GameCopy? copy,
        out ReservationGame? reservationGame)
    {
        var reservation = new Reservation
        {
            CustomerUserId = command.CustomerUserId,
            TableId = table.Id,
            StartAt = command.StartAt,
            EndAt = command.EndAt,
            PartySize = command.PartySize,
            Status = ReservationStatus.Pending,
            Notes = command.Notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        reservationGame = null;
        if (!command.GameId.HasValue || copy is null)
        {
            return reservation;
        }
        
        reservationGame = new ReservationGame
        {
            ReservationId = reservation.Id,
            GameId = command.GameId.Value,
            GameCopyId = copy.Id,
            IsCopyGuaranteed = true
        };
        
        reservation.Games.Add(reservationGame);
        return reservation;
    }
    
    private static Result ValidateCreateCommand(CreateReservationCommand command)
    {
        if (command.CustomerUserId == Guid.Empty)
        {
            return Result.Failure(ErrorCodes.ValidationError, "A valid customer user ID is required.");
        }
        
        if (command.PartySize <= 0)
        {
            return Result.Failure(ErrorCodes.ValidationError, "Party size must be greater than zero.");
        }
        
        var slot = new TimeRange(command.StartAt, command.EndAt);
        if (!slot.IsValid)
        {
            return Result.Failure(ErrorCodes.ValidationError, "Reservation end time must be after start time on the same day.");
        }
        
        if (!string.IsNullOrEmpty(command.Notes) && command.Notes.Trim().Length > MaxNotesLength)
        {
            return Result.Failure(ErrorCodes.ValidationError, $"Reservation notes cannot exceed {MaxNotesLength} characters.");
        }
        
        return Result.Success();
    }
    
    private static ReservationDto MapToDto(Reservation reservation, Table table, ReservationGame? reservationGame)
    {
        return new ReservationDto(
        reservation.Id,
        reservation.CustomerUserId,
        reservation.TableId,
        table.Name,
        reservation.StartAt,
        reservation.EndAt,
        reservation.PartySize,
        reservation.Status,
        reservationGame?.GameId,
        reservationGame?.GameCopyId,
        reservationGame?.IsCopyGuaranteed ?? false);
    }
}