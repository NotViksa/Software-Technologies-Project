using BoardGameCafe.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cafe> Cafes => Set<Cafe>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameCopy> GameCopies => Set<GameCopy>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationGame> ReservationGames => Set<ReservationGame>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
