using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;
using BoardGameCafe.Web.Infrastructure.Persistence;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class BookingService : IBookingService
{
    private const int MaxNotesLength = 500;
    
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    
    public BookingService(AppDbContext dbContext, ICurrentUserContext currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }
    
    public Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct)
    {
        var validation = ValidateCreateCommand(command);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result<ReservationDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!));
        }
        
        throw new NotImplementedException();
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
}