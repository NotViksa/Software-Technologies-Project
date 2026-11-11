using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class ReservationGameConfiguration : IEntityTypeConfiguration<ReservationGame>
{
    public void Configure(EntityTypeBuilder<ReservationGame> builder)
    {
        builder.HasIndex(x => x.ReservationId);
        builder.HasIndex(x => x.GameId);
        builder.HasOne(x => x.Reservation)
            .WithMany(x => x.Games)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GameCopy)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.GameCopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
