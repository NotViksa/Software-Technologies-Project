using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;

namespace BoardGameCafe.Tests;

public class ReservationAllocatorTests
{
    private static readonly TimeSpan OpenTime = new(10, 0, 0);
    private static readonly TimeSpan CloseTime = new(23, 0, 0);
    private static readonly DateTime BaseDate = new(2026, 11, 15);
    
    private static List<Table> CreateSampleTables() =>
    [
        new() { Id = Guid.NewGuid(), Name = "T1-Large", Capacity = 8, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "T2-Small", Capacity = 2, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "T3-Medium", Capacity = 4, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "T4-Inactive", Capacity = 4, IsActive = false }
    ];
    
    [Theory]
    [InlineData(1, "T2-Small")]
    [InlineData(2, "T2-Small")]
    [InlineData(3, "T3-Medium")]
    [InlineData(4, "T3-Medium")]
    [InlineData(5, "T1-Large")]
    [InlineData(8, "T1-Large")]
    public void Allocate_BestFitCapacity_SelectsSmallestSufficientActiveTable(int partySize, string expectedTableName)
    {
        var tables = CreateSampleTables();
        var start = BaseDate.AddHours(14);
        var end = BaseDate.AddHours(16);
        
        var result = ReservationAllocator.Allocate(partySize, start, end, OpenTime, CloseTime, tables, []);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedTableName, result.Value.Table.Name);
        Assert.Null(result.Value.Copy);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(9)]
    public void Allocate_InvalidOrExcessivePartySize_Fails(int partySize)
    {
        var tables = CreateSampleTables();
        var start = BaseDate.AddHours(14);
        var end = BaseDate.AddHours(16);
        
        var result = ReservationAllocator.Allocate(partySize, start, end, OpenTime, CloseTime, tables, []);
        
        Assert.True(result.IsFailure);
    }
    
    [Theory]
    [InlineData(9, 0, 11, 0)]   // Starts before 10:00 opening
    [InlineData(21, 30, 23, 30)]    // Ends after 23:00 closing
    [InlineData(16, 0, 15, 0)]  // End before start
    [InlineData(16, 0, 16, 0)]  // Zero duration
    public void Allocate_OutsideWorkingHoursOrInvalidInterval_ReturnsValidationError(
        int startHour,
        int startMin,
        int endHour,
        int endMin)
    {
        var tables = CreateSampleTables();
        var start = BaseDate.AddHours(startHour).AddMinutes(startMin);
        var end = BaseDate.AddHours(endHour).AddMinutes(endMin);
        
        var result = ReservationAllocator.Allocate(4, start, end, OpenTime, CloseTime, tables, []);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.ErrorCode);
    }
    
    [Fact]
    public void Allocate_WhenSmallestTableIsBooked_FallsBackToNextSmallestTable()
    {
        var tables = CreateSampleTables();
        var smallTable = tables.Single(t => t.Name == "T2-Small");
        
        var existingReservations = new List<Reservation>
        {
            new()
            {
                TableId = smallTable.Id,
                StartAt = BaseDate.AddHours(18),
                EndAt = BaseDate.AddHours(20),
                Status = ReservationStatus.Confirmed
            }
        };
        
        var result = ReservationAllocator.Allocate(
        2, BaseDate.AddHours(18).AddMinutes(30), BaseDate.AddHours(20),
        OpenTime, CloseTime, tables, existingReservations);
        
        Assert.True(result.IsSuccess);
        Assert.Equal("T3-Medium", result.Value.Table.Name);
    }
    
    [Theory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    [InlineData(ReservationStatus.NoShow)]
    public void Allocate_NonBlockingReservationStatuses_DoNotBlockTable(ReservationStatus nonBlockingStatus)
    {
        var tables = CreateSampleTables();
        var smallTable = tables.Single(t => t.Name == "T2-Small");
        
        var existingReservations = new List<Reservation>
        {
            new()
            {
                TableId = smallTable.Id,
                StartAt = BaseDate.AddHours(18),
                EndAt = BaseDate.AddHours(20),
                Status = nonBlockingStatus
            }
        };
        
        var result = ReservationAllocator.Allocate(
        2, BaseDate.AddHours(18), BaseDate.AddHours(20), 
        OpenTime, CloseTime, tables, existingReservations);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(smallTable.Id, result.Value.Table.Id);
    }
    
    [Fact]
    public void Allocate_WithGameCopyWithin15MinuteBuffer_ReturnsCopyNotAvailable()
    {
        var tables = CreateSampleTables();
        var gameId = Guid.NewGuid();
        var copy = new GameCopy
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            InventoryCode = "CATAN-01",
            Condition = GameCondition.Good,
            Status = GameCopyStatus.Available
        };

        var existingReservations = new List<Reservation>
        {
            new()
            {
                TableId = tables.First(t => t.Name == "T1-Large").Id,
                StartAt = BaseDate.AddHours(16),
                EndAt = BaseDate.AddHours(18),
                Status = ReservationStatus.Confirmed,
                Games = [new ReservationGame { GameId = gameId, GameCopyId = copy.Id, IsCopyGuaranteed = true }]
            }
        };
        
        // New request starts at 18:10 (violates the 15-minute buffer on CATAN-01)
        var conflictResult = ReservationAllocator.Allocate(2, BaseDate.AddHours(18).AddMinutes(10), 
        BaseDate.AddHours(20), OpenTime, CloseTime, tables, existingReservations, gameId,
        [copy]);
        
        Assert.True(conflictResult.IsFailure);
        Assert.Equal(ErrorCodes.CopyNotAvailable, conflictResult.ErrorCode);
        
        // Starting at 18:15 (exact end of buffer) succeeds
        var validResult = ReservationAllocator.Allocate(2, BaseDate.AddHours(18).AddMinutes(15), BaseDate.AddHours(20),
        OpenTime, CloseTime, tables, existingReservations, gameId, [copy]);
        
        Assert.True(validResult.IsSuccess);
        Assert.Equal(copy.Id, validResult.Value.Copy?.Id);
    }
    
    [Fact]
    public void Allocate_SkipsDamagedCopy_AndAssignsHealthyDuplicateCopy()
    {
        var tables = CreateSampleTables();
        var gameId = Guid.NewGuid();
        var damagedCopy = new GameCopy
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            InventoryCode = "DUNE-01",
            Condition = GameCondition.Damaged,
            Status = GameCopyStatus.Damaged
        };

        var healthyCopy = new GameCopy
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            InventoryCode = "DUNE-02",
            Condition = GameCondition.Good,
            Status = GameCopyStatus.Available
        };
        
        var result = ReservationAllocator.Allocate(4, BaseDate.AddHours(14), BaseDate.AddHours(17), 
        OpenTime, CloseTime, tables, [], gameId, [damagedCopy, healthyCopy]);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(healthyCopy.Id, result.Value.Copy?.Id);
    }
    
    [Fact]
    public void Allocate_AntiFragmentation_PrefersTableWithBackToBackBookingOverEmptyTable()
    {
        var tableA = new Table { Id = Guid.NewGuid(), Name = "T-A", Capacity = 4, IsActive = true };
        var tableB = new Table { Id = Guid.NewGuid(), Name = "T-B", Capacity = 4, IsActive = true };
        
        // Table B already has a reservation from 12:00 to 14:00.
        // A new request for 14:00 to 16:00 should pick Table B (0 min idle gap) to keep Table A completely free for long sessions.
        var existingReservations = new List<Reservation>
        {
            new()
            {
                TableId = tableB.Id,
                StartAt = BaseDate.AddHours(12),
                EndAt = BaseDate.AddHours(14),
                Status = ReservationStatus.Confirmed
            }
        };
        
        var result = ReservationAllocator.Allocate(
        4,
        BaseDate.AddHours(14),
        BaseDate.AddHours(16),
        OpenTime,
        CloseTime,
        [tableA, tableB],
        existingReservations);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(tableB.Id, result.Value.Table.Id);
    }
    
    [Fact]
    public void Allocate_CopyWearBalancing_PrefersBetterConditionAndLessUsedCopy()
    {
        var tables = CreateSampleTables();
        var gameId = Guid.NewGuid();
        
        var wornCopy = new GameCopy
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            InventoryCode = "CATAN-01",
            Condition = GameCondition.Worn,
            Status = GameCopyStatus.Available
        };
        
        var newCopy = new GameCopy
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            InventoryCode = "CATAN-02",
            Condition = GameCondition.New,
            Status = GameCopyStatus.Available
        };
        
        var result = ReservationAllocator.Allocate(
        4,
        BaseDate.AddHours(14),
        BaseDate.AddHours(16),
        OpenTime,
        CloseTime,
        tables,
        [],
        gameId,
        [wornCopy, newCopy]);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(newCopy.Id, result.Value.Copy?.Id);
    }
    
    [Fact]
    public void Allocate_WhenPartySizeExceedsGamePlayerLimits_ReturnsValidationError()
    {
        var tables = CreateSampleTables();
        var duelGame = new Game
        {
            Id = Guid.NewGuid(),
            Title = "7 Wonders Duel",
            MinPlayers = 2,
            MaxPlayers = 2,
            DurationMinutes = 30,
            IsActive = true
        };

        var result = ReservationAllocator.Allocate(
        4,
        BaseDate.AddHours(14),
        BaseDate.AddHours(16),
        OpenTime,
        CloseTime,
        tables,
        [],
        duelGame.Id,
        [],
        null,
        duelGame);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.ErrorCode);
    }
}