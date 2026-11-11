using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class CafeConfiguration : IEntityTypeConfiguration<Cafe>
{
    public void Configure(EntityTypeBuilder<Cafe> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
    }
}
