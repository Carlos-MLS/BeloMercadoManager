using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class LojaConfiguration : IEntityTypeConfiguration<Loja>
{
    public void Configure(EntityTypeBuilder<Loja> builder)
    {
        builder.ToTable("Lojas");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Designacao)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(l => l.Designacao).IsUnique();

        builder.Property(l => l.Morada)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(l => l.CodigoPostal)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(l => l.Contacto)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(l => l.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(l => l.Horario)
            .HasMaxLength(255);

        builder.Property(l => l.StockMinPorDefeito).IsRequired();
        builder.Property(l => l.StockMaxPorDefeito).IsRequired();
        builder.Property(l => l.Ativo).IsRequired();
        builder.Property(l => l.DataCriacao).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Lojas_StockBounds",
            "[StockMaxPorDefeito] >= [StockMinPorDefeito]"));
    }
}
