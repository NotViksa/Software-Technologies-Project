using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public partial class BookingService : IBookingService
{
    private const int MaxNotesLength = 500;
    
    private readonly AppDbContext _db;
    private readonly SqlServerTransactionRunner _transactionRunner;
    
    public BookingService(AppDbContext db, SqlServerTransactionRunner transactionRunner)
    {
        _db = db;
        _transactionRunner = transactionRunner;
    }
    
    public async Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct)
    {
        var validation = ValidateCreateCommand(command);
        if (validation.IsFailure)
        {
            return Result<ReservationDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!);
        }
        
        var cafe = await ResolveActiveCafeAsync(command.CafeId, ct);
        var cafeScopeId = command.CafeId ?? cafe?.Id ?? Guid.Empty;
        
        return await _transactionRunner.ExecuteAsync(
            cafeScopeId,
            command.StartAt.Date,
            token => AllocateAndPersistAsync(command, cafe, token),
            ct);
    }
    
    private async Task<Result<ReservationDto>> AllocateAndPersistAsync(
        CreateReservationCommand command,
        Cafe? cafe,
        CancellationToken ct)
    {
        if (!await _db.Users.AsNoTracking().AnyAsync(u => u.Id == command.CustomerUserId, ct))
        {
            return Result<ReservationDto>.Failure(ErrorCodes.NotFound, "Customer account was not found.");
        }
        
        var (game, copies, gameError) = await LoadRequestedGameAsync(command.GameId, ct);
        if (gameError is not null)
        {
            return Result<ReservationDto>.Failure(gameError.ErrorCode!, gameError.ErrorMessage!);
        }
        
        var tables = await _db.Tables
            .AsNoTracking()
            .Where(t => t.IsActive && t.Zone.IsActive && t.Capacity >= command.PartySize)
            .ToListAsync(ct);
        
        var dayStart = command.StartAt.Date;
        var sameDayReservations = await _db.Reservations
            .AsNoTracking()
            .Include(r => r.Games)
            .Where(r => r.StartAt >= dayStart && r.StartAt < dayStart.AddDays(1))
            .ToListAsync(ct);
        
        var allocation = ReservationAllocator.Allocate(
        command.PartySize,
        command.StartAt,
        command.EndAt,
        cafe?.OpeningTime ?? new TimeSpan(10, 0, 0),
        cafe?.ClosingTime ?? new TimeSpan(23, 0, 0),
        tables,
        sameDayReservations,
        command.GameId,
        copies,
        command.PreferredTableId,
        game);
        
        if (allocation.IsFailure)
        {
            return Result<ReservationDto>.Failure(allocation.ErrorCode!, allocation.ErrorMessage!);
        }
        
        var reservation = BuildReservationEntity(command, allocation.Value.Table, allocation.Value.Copy, out var reservationGame);
        
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);
        
        return Result<ReservationDto>.Success(MapToDto(reservation, allocation.Value.Table, reservationGame));
    }
    
    public Task<Result> CancelAsync(Guid reservationId, CancellationToken ct) => throw new NotImplementedException();
    public Task<Result> ChangeStatusAsync(Guid reservationId, ReservationStatus newStatus, CancellationToken ct) => throw new NotImplementedException();
}