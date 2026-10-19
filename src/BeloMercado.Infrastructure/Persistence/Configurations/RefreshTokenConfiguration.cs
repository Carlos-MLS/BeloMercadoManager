using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UtilizadorId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        builder.Property(t => t.RevokedAt);
        builder.Property(t => t.ReplacedByTokenId);

        builder.HasOne(t => t.Utilizador)
            .WithMany()
            .HasForeignKey(t => t.UtilizadorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.UtilizadorId);
    }
}
