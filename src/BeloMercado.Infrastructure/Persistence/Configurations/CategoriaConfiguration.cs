using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(c => c.Nome).IsUnique();

        builder.Property(c => c.ParentCategoriaId);
        builder.HasOne(c => c.Parent)
            .WithMany()
            .HasForeignKey(c => c.ParentCategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.ParentCategoriaId);

        builder.Property(c => c.Ativo).IsRequired();
        builder.Property(c => c.DataCriacao).IsRequired();
    }
}
