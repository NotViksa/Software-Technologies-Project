using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.TableId, x.StartAt, x.EndAt });
        builder.HasIndex(x => x.CustomerUserId);
        builder.HasIndex(x => x.StartAt);
        builder.ToTable("Reservations", t =>
        {
            t.HasCheckConstraint("CK_Reservations_PartySize", "[PartySize] > 0");
            t.HasCheckConstraint("CK_Reservations_Time", "[EndAt] > [StartAt]");
        });
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Table)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
