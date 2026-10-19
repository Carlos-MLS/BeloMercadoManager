using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stocks");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.LojaId).IsRequired();
        builder.HasOne(s => s.Loja)
            .WithMany()
            .HasForeignKey(s => s.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.ProdutoId).IsRequired();
        builder.HasOne(s => s.Produto)
            .WithMany()
            .HasForeignKey(s => s.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Quantidade).IsRequired();
        builder.Property(s => s.QtdMinima).IsRequired();
        builder.Property(s => s.QtdMaxima).IsRequired();

        // RN: one Stock row per (Loja, Produto)
        builder.HasIndex(s => new { s.LojaId, s.ProdutoId }).IsUnique();

        // Common queries: alerts within a Loja
        builder.HasIndex(s => new { s.LojaId, s.Quantidade });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Stocks_Quantidade_NaoNegativa", "[Quantidade] >= 0");
            t.HasCheckConstraint("CK_Stocks_Bounds", "[QtdMaxima] >= [QtdMinima]");
        });
    }
}
