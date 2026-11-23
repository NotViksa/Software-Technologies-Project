using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Rules;

public static class ReservationAllocator
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
    
    public static bool IsWithinOpeningHours(DateTime startAt, DateTime endAt, TimeSpan openingTime, TimeSpan closingTime) =>
        new TimeRange(startAt, endAt).IsWithinWorkingHours(openingTime, closingTime);
    
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
        Guid? preferredTableId = null,
        Game? requestedGame = null)
    {
        if (partySize <= 0)
        {
            return Result<(Table, GameCopy?)>.Failure(
            ErrorCodes.ValidationError,
            "Party size must be greater than zero.");
        }
        
        var requestedSlot = new TimeRange(startAt, endAt);
        if (!requestedSlot.IsWithinWorkingHours(openingTime, closingTime))
        {
            return Result<(Table, GameCopy?)>.Failure(
                ErrorCodes.ValidationError,
                "Requested interval is outside café working hours.");
        }
        
        if (requestedGame is not null && !requestedGame.SupportsPlayerCount(partySize))
        {
            return Result<(Table, GameCopy?)>.Failure(
                ErrorCodes.ValidationError,
                $"Game '{requestedGame.Title}' requires between {requestedGame.MinPlayers} and {requestedGame.MaxPlayers} players.");
        }
        
        var activeConflicts = existingReservations
            .Where(r => IsReservationBlocking(r.Status))
            .ToList();
        
        // 1. Filter available tables and rank by:
        // a) Best-Fit capacity (smallest wasted seats)
        // b) Anti-fragmentation (smallest idle gap to adjacent reservation on the same table)
        // c) Deterministic name tie-breaker
        var availableTables = candidateTables
            .Where(t => t.IsActive && t.Capacity >= partySize)
            .Where(t => preferredTableId == null || t.Id == preferredTableId.Value)
            .Where(t => !IsTableOccupied(t.Id, requestedSlot, activeConflicts))
            .OrderBy(t => t.Capacity - partySize)
            .ThenBy(t => CalculateAdjacentGapMinutes(t.Id, requestedSlot, openingTime, closingTime, activeConflicts))
            .ThenBy(t => t.Name)
            .ToList();
        
        var freeTable = availableTables.FirstOrDefault();
        if (freeTable is null)
        {
            return Result<(Table, GameCopy?)>.Failure(
                ErrorCodes.ReservationConflict,
                "No available table for the selected time and party size.");
        }
        
        // 2. If a specific game is requested, allocate a healthy copy with a 15-minute turnaround buffer
        if (!requestedGameId.HasValue)
        {
            return Result<(Table, GameCopy?)>.Success((freeTable, null));
        }
        
        var bufferedSlot = requestedSlot.WithPostBuffer(GameCopyBufferMinutes);
        
        var freeCopy = (candidateCopies ?? [])
            .Where(c => c.GameId == requestedGameId.Value
                        && c.Condition != GameCondition.Damaged
                        && c.Status != GameCopyStatus.Damaged
                        && c.Status != GameCopyStatus.Retired)
            .Where(copy => !IsCopyOccupied(copy.Id, bufferedSlot, activeConflicts))
            // Prioritize best physical condition first, then least booked copy today (wear balancing)
            .OrderBy(c => c.Condition)
            .ThenBy(c => CountCopyReservations(c.Id, activeConflicts))
            .ThenBy(c => c.InventoryCode)
            .FirstOrDefault();
        
        if (freeCopy is null)
        {
            return Result<(Table, GameCopy?)>.Failure(
            ErrorCodes.CopyNotAvailable,
            "No available copy of the selected game for this time slot (including 15-minute buffer).");
        }
        
        return Result<(Table, GameCopy?)>.Success((freeTable, freeCopy));
    }
    
    private static bool IsTableOccupied(Guid tableId, TimeRange slot, IReadOnlyCollection<Reservation> activeReservations) =>
        activeReservations.Any(r =>
            r.TableId == tableId &&
            slot.Overlaps(new TimeRange(r.StartAt, r.EndAt)));
    
    private static bool IsCopyOccupied(Guid copyId, TimeRange bufferedSlot, IReadOnlyCollection<Reservation> activeReservations) =>
        activeReservations.Any(r =>
            r.Games.Any(g => g.GameCopyId == copyId) &&
            bufferedSlot.Overlaps(new TimeRange(r.StartAt, r.EndAt.AddMinutes(GameCopyBufferMinutes))));
    
    private static int CountCopyReservations(Guid copyId, IReadOnlyCollection<Reservation> activeReservations) =>
        activeReservations.Count(r => r.Games.Any(g => g.GameCopyId == copyId));
    
    private static double CalculateAdjacentGapMinutes(
        Guid tableId,
        TimeRange slot,
        TimeSpan openingTime,
        TimeSpan closingTime,
        IReadOnlyCollection<Reservation> activeReservations)
    {
        var tableBookings = activeReservations
            .Where(r => r.TableId == tableId && r.StartAt.Date == slot.Start.Date)
            .ToList();
        
        if (tableBookings.Count == 0)
        {
            return double.MaxValue;
        }
        
        var dayOpen = slot.Start.Date.Add(openingTime);
        var dayClose = slot.Start.Date.Add(closingTime);
        
        var previousEnd = tableBookings
            .Where(r => r.EndAt <= slot.Start)
            .Select(r => r.EndAt)
            .DefaultIfEmpty(dayOpen)
            .Max();
        
        var nextStart = tableBookings
            .Where(r => r.StartAt >= slot.End)
            .Select(r => r.StartAt)
            .DefaultIfEmpty(dayClose)
            .Min();
        
        return Math.Min((slot.Start - previousEnd).TotalMinutes, (nextStart - slot.End).TotalMinutes);
    }
}