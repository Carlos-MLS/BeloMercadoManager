using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class PromocaoConfiguration : IEntityTypeConfiguration<Promocao>
{
    public void Configure(EntityTypeBuilder<Promocao> builder)
    {
        builder.ToTable("Promocoes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).IsRequired().HasMaxLength(100);

        builder.Property(p => p.ProdutoId).IsRequired();
        builder.HasOne(p => p.Produto)
            .WithMany()
            .HasForeignKey(p => p.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.LojaId);
        builder.HasOne(p => p.Loja)
            .WithMany()
            .HasForeignKey(p => p.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(p => p.Valor).IsRequired().HasPrecision(10, 2);
        builder.Property(p => p.DataInicio).IsRequired();
        builder.Property(p => p.DataFim).IsRequired();
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.DataCriacao).IsRequired();

        builder.HasIndex(p => new { p.ProdutoId, p.LojaId, p.Ativo });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Promocoes_Datas", "[DataFim] >= [DataInicio]");
            t.HasCheckConstraint("CK_Promocoes_Valor_Positivo", "[Valor] > 0");
        });
    }
}
