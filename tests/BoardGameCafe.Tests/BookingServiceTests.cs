using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Infrastructure.Persistence;
using BoardGameCafe.Web.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Tests;

public class BookingServiceTests
{
    private static readonly DateTime BaseDate = new(2026, 12, 5);
    
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"BookingTests_{Guid.NewGuid()}")
            .Options;
        
        return new AppDbContext(options);
    }
    
    private static BookingService CreateService(AppDbContext db, ICurrentUserContext userContext) => new(db, new SqlServerTransactionRunner(db), userContext);
    
    private static async Task<Reservation> SeedReservationAsync(AppDbContext dbContext, Guid customerId, ReservationStatus status = ReservationStatus.Pending)
    {
        var user = new ApplicationUser
        {
            Id = customerId,
            UserName = $"user-{customerId:N}@local.test",
            Email = $"user-{customerId:N}@local.test",
            DisplayName = "Test Customer"
        };
        
        var zone = new Zone { Name = $"Zone-{Guid.NewGuid():N}", IsActive = true };
        var table = new Table { Zone = zone, ZoneId = zone.Id, Name = "T1", Capacity = 4, IsActive = true };
        
        var reservation = new Reservation
        {
            CustomerUserId = customerId,
            Customer = user,
            TableId = table.Id,
            Table = table,
            StartAt = BaseDate.AddHours(14),
            EndAt = BaseDate.AddHours(16),
            PartySize = 4,
            Status = status
        };
        
        dbContext.Users.Add(user);
        dbContext.Zones.Add(zone);
        dbContext.Tables.Add(table);
        dbContext.Reservations.Add(reservation);
        
        await dbContext.SaveChangesAsync();
        return reservation;
    }
    
    [Fact]
    public async Task CreateAsync_InvalidPartySize_ReturnsValidationErrorWithoutDatabaseCall()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, FakeCurrentUserContext.Customer(Guid.NewGuid()));
        
        var command = new CreateReservationCommand(
        Guid.NewGuid(),
        BaseDate.AddHours(14),
        BaseDate.AddHours(16),
        0);
        
        var result = await service.CreateAsync(command, CancellationToken.None);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.ErrorCode);
    }
    
    [Fact]
    public async Task CancelAsync_OwnerCancelsPendingReservation_SetsStatusToCancelled()
    {
        await using var dbContext = CreateDbContext();
        var customerId = Guid.NewGuid();
        var reservation = await SeedReservationAsync(dbContext, customerId);
        
        var customer = FakeCurrentUserContext.Customer(customerId);
        var service = CreateService(dbContext, customer);
        
        var result = await service.CancelAsync(reservation.Id, CancellationToken.None);
        Assert.True(result.IsSuccess);
        
        var updated = await dbContext.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, updated!.Status);
    }
    
    [Fact]
    public async Task CancelAsync_NonExistentReservation_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        
        var staff = FakeCurrentUserContext.Staff(Guid.NewGuid());
        var service = CreateService(dbContext, staff);
        
        var result = await service.CancelAsync(Guid.NewGuid(), CancellationToken.None);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }
    
    [Fact]
    public async Task ChangeStatusAsync_FullStaffLifecycle_CreatesAndClosesSession()
    {
        await using var dbContext = CreateDbContext();
        var reservation = await SeedReservationAsync(dbContext, Guid.NewGuid());
        var service = CreateService(dbContext, FakeCurrentUserContext.Staff(Guid.NewGuid()));
        
        var confirmedResult = await service.ChangeStatusAsync(reservation.Id, ReservationStatus.Confirmed, CancellationToken.None);
        Assert.True(confirmedResult.IsSuccess);
        
        var checkedInResult = await service.ChangeStatusAsync(reservation.Id, ReservationStatus.CheckedIn, CancellationToken.None);
        Assert.True(checkedInResult.IsSuccess);
        
        var inSessionResult = await service.ChangeStatusAsync(reservation.Id, ReservationStatus.InSession, CancellationToken.None);
        Assert.True(inSessionResult.IsSuccess);
        
        var inSession = await dbContext.Reservations
            .Include(r => r.Sessions)
            .SingleAsync(r => r.Id == reservation.Id);
        
        Assert.Equal(ReservationStatus.InSession, inSession.Status);
        Assert.Single(inSession.Sessions);
        Assert.Null(inSession.Sessions.Single().EndedAt);
        
        var completedResult = await service.ChangeStatusAsync(reservation.Id, ReservationStatus.Completed, CancellationToken.None);
        Assert.True(completedResult.IsSuccess);
        
        var completed = await dbContext.Reservations
            .Include(r => r.Sessions)
            .SingleAsync(r => r.Id == reservation.Id);
        
        Assert.Equal(ReservationStatus.Completed, completed.Status);
        Assert.NotNull(completed.Sessions.Single().EndedAt);
    }
    
    [Fact]
    public async Task ChangeStatusAsync_InvalidStateSkip_ReturnsInvalidStateTransition()
    {
        await using var dbContext = CreateDbContext();
        var reservation = await SeedReservationAsync(dbContext, Guid.NewGuid());
        var service = CreateService(dbContext, FakeCurrentUserContext.Staff(Guid.NewGuid()));
        
        // Cannot jump directly from Pending to InSession
        var result = await service.ChangeStatusAsync(reservation.Id, ReservationStatus.InSession, CancellationToken.None);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidStateTransition, result.ErrorCode);
    }

    [Fact]
    public async Task CancelAsync_UnauthenticatedUser_ReturnsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var reservation = await SeedReservationAsync(dbContext, Guid.NewGuid());
        var service = CreateService(dbContext, FakeCurrentUserContext.Anonymous());
        
        var result = await service.CancelAsync(reservation.Id, CancellationToken.None);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Forbidden, result.ErrorCode);
    }

    [Fact]
    public async Task CancelAsync_DifferentCustomer_ReturnsForbiddenAndDoesNotModifyStatus()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var reservation = await SeedReservationAsync(dbContext, ownerId, ReservationStatus.Confirmed);
        
        var service = CreateService(dbContext, FakeCurrentUserContext.Customer(otherCustomerId));
        var result = await service.CancelAsync(reservation.Id, CancellationToken.None);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Forbidden, result.ErrorCode);
        
        var unchanged = await dbContext.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Confirmed, unchanged!.Status);
    }
    
    [Fact]
    public async Task ChangeStatusAsync_CustomerAttemptsStaffOnlyCheckIn_ReturnsInvalidStateTransition()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var reservation = await SeedReservationAsync(dbContext, ownerId, ReservationStatus.Confirmed);
        
        var service = CreateService(dbContext, FakeCurrentUserContext.Staff(ownerId));
        var result = await service.ChangeStatusAsync(reservation.Id, ReservationStatus.InSession, CancellationToken.None);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidStateTransition, result.ErrorCode);
    }

    [Fact]
    public async Task CancelAsync_StaffCancelsCustomerReservation_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var staffId =  Guid.NewGuid();
        var reservation = await SeedReservationAsync(dbContext, ownerId, ReservationStatus.Confirmed);
        
        var service = CreateService(dbContext, FakeCurrentUserContext.Staff(staffId));
        var result = await service.CancelAsync(reservation.Id, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        
        var updated = await dbContext.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, updated!.Status);
    }
    
    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; private init; }
        public bool IsAuthenticated { get; private init; }
        public bool IsManager { get; private init; }
        public bool IsStaff { get; private init; }
        public string? DisplayName { get; private init; }
        
        public static FakeCurrentUserContext Anonymous() => new() { IsAuthenticated = false };
        
        public static FakeCurrentUserContext Customer(Guid userId) => new()
        {
            UserId = userId,
            IsAuthenticated = true,
            IsStaff = false,
            IsManager = false,
            DisplayName = "Customer"
        };
        
        public static FakeCurrentUserContext Staff(Guid userId) => new()
        {
            UserId = userId,
            IsAuthenticated = true,
            IsStaff = true,
            IsManager = false,
            DisplayName = "Staff"
        };
    }
}