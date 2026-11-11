using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Complexity).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Title).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.ToTable("Games", t =>
        {
            t.HasCheckConstraint("CK_Games_Players", "[MinPlayers] > 0 AND [MaxPlayers] >= [MinPlayers]");
            t.HasCheckConstraint("CK_Games_Duration", "[DurationMinutes] > 0");
        });
    }
}
