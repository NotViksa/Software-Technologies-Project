using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Rules;

public class ReservationAllocator
{
    public const int GameCopyBufferMinutes = 15;
    
    private static readonly ReservationStatus[] BlockingStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed,
        ReservationStatus.CheckedIn,
        ReservationStatus.InSession,
        ReservationStatus.Overdue
    ];
    
    public static bool IsReservationBlocking(ReservationStatus status) => BlockingStatuses.Contains(status);
    
    public static bool IsWithinOpeningHours(DateTime startAt, DateTime endAt, TimeSpan openingTime, TimeSpan closingTime)
    {
        if (endAt <= startAt || startAt.Date != endAt.Date)
        {
            return false;
        }
        
        return startAt.TimeOfDay >= openingTime && endAt.TimeOfDay <= closingTime;
    }
    
    public static Result<(Table Table, GameCopy? Copy)> Allocate(
        int partySize,
        DateTime startAt,
        DateTime endAt,
        TimeSpan openingTime,
        TimeSpan closingTime,
        IEnumerable<Table> candidateTables,
        IEnumerable<Reservation> existingReservations,
        Guid? requestedGameId = null,
        IEnumerable<GameCopy>? candidateCopies = null,
        Guid? preferredTableId = null)
    {
        if (partySize <= 0)
        {
            return Result<(Table, GameCopy?)>.Failure(ErrorCodes.ValidationError, "Party size must be greater than zero.");
        }
        
        if (!IsWithinOpeningHours(startAt, endAt, openingTime, closingTime))
        {
            return Result<(Table, GameCopy?)>.Failure(ErrorCodes.ValidationError, "Requested interval is outside café working hours.");
        }
        
        var activeConflicts = existingReservations
            .Where(r => IsReservationBlocking(r.Status))
            .ToList();
        
        // 1. Order active tables with sufficient capacity by smallest capacity first (Best-Fit)
        var orderedTables = candidateTables
            .Where(t => t.IsActive && t.Capacity >= partySize)
            .Where(t => preferredTableId == null || t.Id == preferredTableId.Value)
            .OrderBy(t => t.Capacity)
            .ThenBy(t => t.Name)
            .ToList();
        
        var freeTable = orderedTables.FirstOrDefault(t =>
            !activeConflicts.Any(r => r.TableId == t.Id && startAt < r.EndAt && endAt > r.StartAt));
        
        if (freeTable is null)
        {
            return Result<(Table, GameCopy?)>.Failure(ErrorCodes.ReservationConflict, "No available table for the selected time and party size.");
        }
        
        // 2. If a specific game is requested, allocate a healthy copy with a 15-minute buffer
        if (!requestedGameId.HasValue)
        {
            return Result<(Table, GameCopy?)>.Success((freeTable, null));
        }
        
        var bufferedEnd = endAt.AddMinutes(GameCopyBufferMinutes);
        
        var viableCopies = (candidateCopies ?? [])
            .Where(c => c.GameId == requestedGameId.Value
                        && c.Condition != GameCondition.Damaged
                        && c.Status != GameCopyStatus.Damaged
                        && c.Status != GameCopyStatus.Retired)
            .OrderBy(c => c.InventoryCode)
            .ToList();
        
        var freeCopy = viableCopies.FirstOrDefault(copy =>
            !activeConflicts.Any(r =>
                r.Games.Any(g => g.GameCopyId == copy.Id)
                && startAt < r.EndAt.AddMinutes(GameCopyBufferMinutes)
                && bufferedEnd > r.StartAt));
        
        if (freeCopy is null)
        {
            return Result<(Table, GameCopy?)>.Failure(
            ErrorCodes.CopyNotAvailable,
            "No available copy of the selected game for this time slot (including 15-minute buffer).");
        }
        
        return Result<(Table, GameCopy?)>.Success((freeTable, freeCopy));
    }
}