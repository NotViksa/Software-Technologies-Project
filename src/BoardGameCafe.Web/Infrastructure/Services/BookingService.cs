using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class BookingService : IBookingService
{
    private const int MaxNotesLength = 500;
    
    private readonly AppDbContext _dbContext;
    
    public BookingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct)
    {
        var validation = ValidateCreateCommand(command);
        if (validation.IsFailure)
        {
            return await Task.FromResult(Result<ReservationDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!));
        }
        
        var customerExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == command.CustomerUserId, ct);
        
        if (!customerExists)
        {
            return Result<ReservationDto>.Failure(ErrorCodes.NotFound, "Customer account was not found.");
        }
        
        var cafeQuery = _dbContext.Cafes
            .AsNoTracking()
            .Where(c => c.IsActive);
        
        if (command.CafeId.HasValue)
        {
            cafeQuery = cafeQuery.Where(c => c.Id == command.CafeId);
        }
        
        var cafe = await cafeQuery
            .OrderBy(c => c.Name)
            .FirstOrDefaultAsync(ct);
        
        var openingTime = cafe?.OpeningTime ?? new TimeSpan(10, 0, 0);
        var closingTime = cafe?.ClosingTime ?? new TimeSpan(23, 0, 0);
        
        Game? requestedGame = null;
        List<GameCopy> candidateCopies = [];
        
        if (command.GameId.HasValue)
        {
            requestedGame = await _dbContext.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == command.GameId.Value && g.IsActive, ct);
            
            if (requestedGame is null)
            {
                return Result<ReservationDto>.Failure(ErrorCodes.NotFound, "Requested game was not found or is inactive.");
            }
            
            candidateCopies = await _dbContext.GameCopies
                .AsNoTracking()
                .Where(c => c.GameId == command.GameId.Value)
                .ToListAsync(ct);
        }
        
        var tables = await _dbContext.Tables
            .AsNoTracking()
            .Where(t => t.IsActive && t.Zone.IsActive && t.Capacity >= command.PartySize)
            .ToListAsync(ct);
        
        var dayStart = command.StartAt.Date;
        var dayEnd = dayStart.AddDays(1);
        
        var sameDayReservations = await _dbContext.Reservations
            .AsNoTracking()
            .Include(r => r.Games)
            .Where(r => r.StartAt >= dayStart && r.StartAt < dayEnd)
            .ToListAsync(ct);
        
        var allocation = ReservationAllocator.Allocate(
        command.PartySize,
        command.StartAt,
        command.EndAt,
        openingTime,
        closingTime,
        tables,
        sameDayReservations,
        command.GameId,
        candidateCopies,
        command.PreferredTableId,
        requestedGame);
        
        if (allocation.IsFailure)
        {
            return Result<ReservationDto>.Failure(allocation.ErrorCode!, allocation.ErrorMessage!);
        }
        
        var allocatedTable = allocation.Value.Table;
        var allocatedCopy = allocation.Value.Copy;
        
        var reservation = new Reservation
        {
            CustomerUserId = command.CustomerUserId,
            TableId = allocatedTable.Id,
            StartAt = command.StartAt,
            EndAt = command.EndAt,
            PartySize = command.PartySize,
            Status = ReservationStatus.Pending,
            Notes = command.Notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        ReservationGame? reservationGame = null;
        if (command.GameId.HasValue && allocatedCopy is not null)
        {
            reservationGame = new ReservationGame
            {
                ReservationId = reservation.Id,
                GameId = command.GameId.Value,
                GameCopyId = allocatedCopy.Id,
                IsCopyGuaranteed = true
            };
            
            reservation.Games.Add(reservationGame);
        }
        
        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync(ct);
        
        return Result<ReservationDto>.Success(MapToDto(reservation, allocatedTable, reservationGame));
    }
    
    public Task<Result> CancelAsync(Guid reservationId, CancellationToken ct) => throw new NotImplementedException();
    public Task<Result> ChangeStatusAsync(Guid reservationId, ReservationStatus newStatus, CancellationToken ct) => throw new NotImplementedException();
    
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