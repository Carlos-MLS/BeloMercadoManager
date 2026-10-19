using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class IdempotencyEntryConfiguration : IEntityTypeConfiguration<IdempotencyEntry>
{
    public void Configure(EntityTypeBuilder<IdempotencyEntry> builder)
    {
        builder.ToTable("IdempotencyKeys");
        builder.HasKey(e => e.Key);

        builder.Property(e => e.Key).HasMaxLength(128);
        builder.Property(e => e.VendaId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
