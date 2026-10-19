using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("Fornecedores");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Nome).IsRequired().HasMaxLength(100);

        builder.Property(f => f.Nif).IsRequired().HasMaxLength(9);
        builder.HasIndex(f => f.Nif).IsUnique();

        builder.Property(f => f.Morada).IsRequired().HasMaxLength(255);
        builder.Property(f => f.Contacto).IsRequired().HasMaxLength(32);
        builder.Property(f => f.Email).IsRequired().HasMaxLength(255);
        builder.Property(f => f.Ramo).HasMaxLength(64);
        builder.Property(f => f.CondicoesPagamento).HasMaxLength(255);
        builder.Property(f => f.Ativo).IsRequired();
        builder.Property(f => f.DataCriacao).IsRequired();

        builder.HasMany(f => f.ProdutosAssociados)
            .WithOne(pa => pa.Fornecedor)
            .HasForeignKey(pa => pa.FornecedorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
