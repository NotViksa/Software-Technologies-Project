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
    
    private readonly AppDbContext _dbContext;
    private readonly SqlServerTransactionRunner _transactionRunner;
    private readonly ICurrentUserContext _currentUser;
    
    public BookingService(AppDbContext dbContext, SqlServerTransactionRunner transactionRunner, ICurrentUserContext currentUser)
    {
        _dbContext = dbContext;
        _transactionRunner = transactionRunner;
        _currentUser = currentUser;
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
        if (!await _dbContext.Users.AsNoTracking().AnyAsync(u => u.Id == command.CustomerUserId, ct))
        {
            return Result<ReservationDto>.Failure(ErrorCodes.NotFound, "Customer account was not found.");
        }
        
        var (game, copies, gameError) = await LoadRequestedGameAsync(command.GameId, ct);
        if (gameError is not null)
        {
            return Result<ReservationDto>.Failure(gameError.ErrorCode!, gameError.ErrorMessage!);
        }
        
        var tables = await _dbContext.Tables
            .AsNoTracking()
            .Where(t => t.IsActive && t.Zone.IsActive && t.Capacity >= command.PartySize)
            .ToListAsync(ct);
        
        var dayStart = command.StartAt.Date;
        var sameDayReservations = await _dbContext.Reservations
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
        
        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync(ct);
        
        return Result<ReservationDto>.Success(MapToDto(reservation, allocation.Value.Table, reservationGame));
    }
    
    public async Task<Result> CancelAsync(Guid reservationId, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return Result.Failure(ErrorCodes.Forbidden, "Authentication is required to cancel a reservation.");
        }
        
        var reservation = await _dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);
        
        if (reservation is null)
        {
            return Result.Failure(ErrorCodes.NotFound, "Reservation was not found.");
        }
        
        if (!_currentUser.IsStaff && reservation.CustomerUserId != _currentUser.UserId.Value)
        {
            return Result.Failure(ErrorCodes.Forbidden, "You do not have permission to cancel another customer's reservation.");
        }
        
        var transition = ReservationStateMachine.ValidateTransition(reservation.Status, ReservationStatus.Cancelled, _currentUser.IsStaff);
        if (transition.IsFailure)
        {
            return transition;
        }
        
        reservation.Status = ReservationStatus.Cancelled;
        
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ErrorCodes.ReservationConflict, "The reservation was modified by another request. Please refresh and try again.");
        }
    }
    
    public async Task<Result> ChangeStatusAsync(Guid reservationId, ReservationStatus newStatus, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return Result.Failure(ErrorCodes.Forbidden, "Authentication is required to modify a reservation.");
        }
        
        if (newStatus == ReservationStatus.Cancelled)
        {
            return await CancelAsync(reservationId, ct);
        }
        
        var reservation = await _dbContext.Reservations
            .Include(r => r.Sessions)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);
        
        if (reservation is null)
        {
            return Result.Failure(ErrorCodes.NotFound, "Reservation was not found.");
        }
        
        if (!_currentUser.IsStaff && reservation.CustomerUserId != _currentUser.UserId.Value)
        {
            return Result.Failure(ErrorCodes.Forbidden, "You do not have permission to cancel another customer's reservation.");
        }
        
        var transition = ReservationStateMachine.ValidateTransition(reservation.Status, newStatus, _currentUser.IsStaff);
        if (transition.IsFailure)
        {
            return transition;
        }
        
        reservation.Status = newStatus;
        SyncSessionLifecycle(reservationId, newStatus, reservation);
        
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ErrorCodes.ReservationConflict, "The reservation was modified by another request. Please refresh and try again.");
        }
    }
}