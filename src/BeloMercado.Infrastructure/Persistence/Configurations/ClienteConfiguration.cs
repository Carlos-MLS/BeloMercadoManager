using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nif).IsRequired().HasMaxLength(9);
        builder.HasIndex(c => c.Nif).IsUnique();

        builder.Property(c => c.Nome).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(255);
        builder.Property(c => c.DataCriacao).IsRequired();
    }
}
