using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class LinhaEncomendaConfiguration : IEntityTypeConfiguration<LinhaEncomenda>
{
    public void Configure(EntityTypeBuilder<LinhaEncomenda> builder)
    {
        builder.ToTable("LinhasEncomenda");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.EncomendaId).IsRequired();
        builder.Property(l => l.ProdutoId).IsRequired();
        builder.HasOne(l => l.Produto)
            .WithMany()
            .HasForeignKey(l => l.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.ProdutoDescricao).IsRequired().HasMaxLength(255);
        builder.Property(l => l.ProdutoEan13).IsRequired().HasMaxLength(13);
        builder.Property(l => l.Quantidade).IsRequired();
        builder.Property(l => l.PrecoCustoUnitario).IsRequired().HasPrecision(10, 2);
        builder.Property(l => l.DescontoPercentagem).IsRequired().HasPrecision(5, 2);
        builder.Property(l => l.Subtotal).IsRequired().HasPrecision(12, 2);
        builder.Property(l => l.QuantidadeRecebida).IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_LinhasEncomenda_Quantidade_Pos", "[Quantidade] > 0");
            t.HasCheckConstraint("CK_LinhasEncomenda_QtdRecebida_NaoNeg", "[QuantidadeRecebida] >= 0");
        });
    }
}
