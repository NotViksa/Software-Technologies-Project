using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Rules;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class AvailabilityService(AppDbContext dbContext) : IAvailabilityService
{
    public async Task<AvailabilityResult> FindAsync(AvailabilityRequest request, CancellationToken ct)
    {
        var cafeQuery = dbContext.Cafes.AsNoTracking().Where(c => c.IsActive);
        if (request.CafeId.HasValue)
        {
            cafeQuery = cafeQuery.Where(c => c.Id == request.CafeId.Value);
        }
        
        var cafe = await cafeQuery.OrderBy(c => c.Name).FirstOrDefaultAsync(ct);
        var openingTime = cafe?.OpeningTime ?? new TimeSpan(10, 0, 0);
        var closingTime = cafe?.ClosingTime ?? new TimeSpan(23, 0, 0);
        
        var slot = new TimeRange(request.StartAt, request.EndAt);
        if (request.PartySize <= 0 || !slot.IsWithinWorkingHours(openingTime, closingTime))
        {
            return new AvailabilityResult(
            IsAvailable: false,
            RecommendedTableId: null,
            AvailableGameCopyId: null,
            AvailableTables: [],
            FailureCode: ErrorCodes.ValidationError,
            FailureReason: "Invalid party size or time interval outside café working hours.");
        }
        
        Game? requestedGame = null!;
        List<GameCopy> candidateCopies = [];
        
        if (request.GameId.HasValue)
        {
            requestedGame = await dbContext.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.GameId.Value && g.IsActive, ct);
            
            if (requestedGame is null)
            {
                return new AvailabilityResult(
                IsAvailable: false,
                RecommendedTableId: null,
                AvailableGameCopyId: null,
                AvailableTables: [],
                FailureCode: ErrorCodes.NotFound,
                FailureReason: "Requested game was not found or is inactive.");
            }
            
            candidateCopies = await dbContext.GameCopies
                .AsNoTracking()
                .Where(c => c.GameId == request.GameId.Value)
                .ToListAsync(ct);
        }
        
        var tables = await dbContext.Tables
            .AsNoTracking()
            .Include(t => t.Zone)
            .Where(t => t.IsActive && t.Zone.IsActive && t.Capacity >= request.PartySize)
            .ToListAsync(ct);
        
        var dayStart = request.StartAt.Date;
        var dayEnd = dayStart.AddDays(1);
        
        var sameDayReservations = await dbContext.Reservations
            .AsNoTracking()
            .Include(r => r.Games)
            .Where(r => r.StartAt >= dayStart && r.StartAt < dayEnd)
            .ToListAsync(ct);
        
        var allocation = ReservationAllocator.Allocate(
        request.PartySize,
        request.StartAt,
        request.EndAt,
        openingTime,
        closingTime,
        tables,
        sameDayReservations,
        request.GameId,
        candidateCopies,
        preferredTableId: null,
        requestedGame: requestedGame);
        
        var activeConflicts = sameDayReservations
            .Where(r => ReservationAllocator.IsReservationBlocking(r.Status))
            .ToList();
        
        var availableTableDtos = tables
            .Where(t => !activeConflicts.Any(r => r.TableId == t.Id && request.StartAt < r.EndAt && request.EndAt > r.StartAt))
            .OrderBy(t => t.Capacity)
            .ThenBy(t => t.Name)
            .Select(t => new AvailableTableDto(t.Id, t.Name, t.Zone.Name, t.Capacity))
            .ToList();
        
        if (allocation.IsFailure)
        {
            return new AvailabilityResult(
            IsAvailable: false,
            RecommendedTableId: null,
            AvailableGameCopyId: null,
            AvailableTables: availableTableDtos,
            FailureCode: allocation.ErrorCode,
            FailureReason: allocation.ErrorMessage);
        }
        
        return new AvailabilityResult(
        IsAvailable: true,
        RecommendedTableId: allocation.Value.Table.Id,
        AvailableGameCopyId: allocation.Value.Copy?.Id,
        AvailableTables: availableTableDtos);
    }
}