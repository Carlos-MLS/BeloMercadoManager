using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class RankingProdutoDiarioConfiguration : IEntityTypeConfiguration<RankingProdutoDiario>
{
    public void Configure(EntityTypeBuilder<RankingProdutoDiario> builder)
    {
        builder.ToTable("RankingProdutosDiarios");
        builder.HasKey(r => new { r.LojaId, r.Data, r.ProdutoId });

        builder.HasOne(r => r.Loja)
            .WithMany()
            .HasForeignKey(r => r.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Produto)
            .WithMany()
            .HasForeignKey(r => r.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Posicao).IsRequired();
        builder.Property(r => r.QuantidadeVendida).IsRequired();
        builder.Property(r => r.Receita).IsRequired().HasPrecision(12, 2);

        builder.HasIndex(r => new { r.LojaId, r.Data, r.Posicao });
    }
}
