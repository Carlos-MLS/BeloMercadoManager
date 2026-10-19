using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class LinhaVendaConfiguration : IEntityTypeConfiguration<LinhaVenda>
{
    public void Configure(EntityTypeBuilder<LinhaVenda> builder)
    {
        builder.ToTable("LinhasVenda");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.VendaId).IsRequired();
        builder.Property(l => l.ProdutoId).IsRequired();
        builder.HasOne(l => l.Produto)
            .WithMany()
            .HasForeignKey(l => l.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.ProdutoDescricao).IsRequired().HasMaxLength(255);
        builder.Property(l => l.ProdutoEan13).IsRequired().HasMaxLength(13);
        builder.Property(l => l.Quantidade).IsRequired();
        builder.Property(l => l.PrecoUnitario).IsRequired().HasPrecision(10, 2);
        builder.Property(l => l.TaxaIvaPercentagem).IsRequired();
        builder.Property(l => l.Subtotal).IsRequired().HasPrecision(10, 2);
        builder.Property(l => l.ValorIva).IsRequired().HasPrecision(10, 2);
        builder.Property(l => l.QuantidadeDevolvida).IsRequired().HasDefaultValue(0);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_LinhasVenda_Quantidade_Positiva", "[Quantidade] > 0");
            t.HasCheckConstraint("CK_LinhasVenda_QtdDevolvida_NaoNeg", "[QuantidadeDevolvida] >= 0");
            t.HasCheckConstraint("CK_LinhasVenda_QtdDevolvida_Limite", "[QuantidadeDevolvida] <= [Quantidade]");
        });
    }
}
