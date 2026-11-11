using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.ZoneId, x.Name }).IsUnique();
        builder.ToTable("Tables", t => t.HasCheckConstraint("CK_Tables_Capacity", "[Capacity] > 0"));
        builder.HasOne(x => x.Zone)
            .WithMany(x => x.Tables)
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
