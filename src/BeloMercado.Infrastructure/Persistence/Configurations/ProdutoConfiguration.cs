using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Ean13)
            .IsRequired()
            .HasMaxLength(13)
            .IsFixedLength();
        builder.HasIndex(p => p.Ean13).IsUnique();

        builder.Property(p => p.Descricao)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.CategoriaId).IsRequired();
        builder.HasOne(p => p.Categoria)
            .WithMany()
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.CategoriaId);

        builder.Property(p => p.Preco)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.TaxaIva)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.UnidadeMedida)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(p => p.ImagemUrl)
            .HasMaxLength(512);

        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.DataCriacao).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Produtos_Preco_NaoNegativo",
            "[Preco] >= 0"));
    }
}
