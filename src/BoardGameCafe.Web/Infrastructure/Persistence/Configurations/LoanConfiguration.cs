using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.GameCopyId);
        builder.HasIndex(x => new { x.DueAt, x.ReturnedAt });
        builder.HasOne(x => x.Reservation)
            .WithMany()
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Session)
            .WithMany(x => x.Loans)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.GameCopy)
            .WithMany(x => x.Loans)
            .HasForeignKey(x => x.GameCopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
