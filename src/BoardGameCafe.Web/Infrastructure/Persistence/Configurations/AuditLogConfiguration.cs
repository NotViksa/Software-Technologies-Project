
using BoardGameCafe.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAt);

        builder.HasIndex(x => x.UserId);
    }
}
