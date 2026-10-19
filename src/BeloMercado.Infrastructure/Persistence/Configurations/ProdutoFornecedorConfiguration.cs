using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class ProdutoFornecedorConfiguration : IEntityTypeConfiguration<ProdutoFornecedor>
{
    public void Configure(EntityTypeBuilder<ProdutoFornecedor> builder)
    {
        builder.ToTable("ProdutoFornecedor");
        builder.HasKey(pa => new { pa.FornecedorId, pa.ProdutoId });

        builder.Property(pa => pa.PrecoCusto).IsRequired().HasPrecision(10, 2);
        builder.Property(pa => pa.DescontoPercentagem).IsRequired().HasPrecision(5, 2);
        builder.Property(pa => pa.PrazoEntregaDias).IsRequired();

        builder.HasOne(pa => pa.Produto)
            .WithMany()
            .HasForeignKey(pa => pa.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ProdutoFornecedor_Preco_NaoNeg", "[PrecoCusto] >= 0");
            t.HasCheckConstraint("CK_ProdutoFornecedor_Desconto_0_100", "[DescontoPercentagem] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_ProdutoFornecedor_Prazo_NaoNeg", "[PrazoEntregaDias] >= 0");
        });
    }
}
