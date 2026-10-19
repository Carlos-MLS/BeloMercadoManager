using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class EncomendaConfiguration : IEntityTypeConfiguration<Encomenda>
{
    public void Configure(EntityTypeBuilder<Encomenda> builder)
    {
        builder.ToTable("Encomendas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FornecedorId).IsRequired();
        builder.HasOne(e => e.Fornecedor)
            .WithMany()
            .HasForeignKey(e => e.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.LojaId).IsRequired();
        builder.HasOne(e => e.Loja)
            .WithMany()
            .HasForeignKey(e => e.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CriadoPorId).IsRequired();

        builder.Property(e => e.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.DataCriacao).IsRequired();
        builder.Property(e => e.DataSubmissao);
        builder.Property(e => e.DataRececao);
        builder.Property(e => e.Total).IsRequired().HasPrecision(12, 2);

        builder.HasMany(e => e.Linhas)
            .WithOne(l => l.Encomenda)
            .HasForeignKey(l => l.EncomendaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.LojaId, e.DataCriacao });
        builder.HasIndex(e => new { e.FornecedorId, e.Estado });
    }
}
