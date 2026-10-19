using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class HistoricoPrecoConfiguration : IEntityTypeConfiguration<HistoricoPreco>
{
    public void Configure(EntityTypeBuilder<HistoricoPreco> builder)
    {
        builder.ToTable("HistoricoPrecos");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ProdutoId).IsRequired();
        builder.HasOne(h => h.Produto)
            .WithMany()
            .HasForeignKey(h => h.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(h => h.Preco)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(h => h.DataMudanca).IsRequired();
        builder.Property(h => h.AlteradoPorUtilizadorId);

        builder.HasIndex(h => new { h.ProdutoId, h.DataMudanca });
    }
}
