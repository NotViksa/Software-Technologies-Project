using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class GameCopyConfiguration : IEntityTypeConfiguration<GameCopy>
{
    public void Configure(EntityTypeBuilder<GameCopy> builder)
    {
        builder.Property(x => x.InventoryCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Condition).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.InventoryCode).IsUnique();
        builder.HasIndex(x => new { x.GameId, x.Status });
        builder.HasOne(x => x.Game)
            .WithMany(x => x.Copies)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
