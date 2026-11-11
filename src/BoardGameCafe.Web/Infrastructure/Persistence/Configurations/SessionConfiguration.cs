using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasIndex(x => x.ReservationId);
        builder.HasOne(x => x.Reservation)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
