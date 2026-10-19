using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class FaturaConfiguration : IEntityTypeConfiguration<Fatura>
{
    public void Configure(EntityTypeBuilder<Fatura> builder)
    {
        builder.ToTable("Faturas");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.VendaId).IsRequired();
        builder.HasOne(f => f.Venda)
            .WithMany()
            .HasForeignKey(f => f.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(f => f.LojaId).IsRequired();
        builder.Property(f => f.Ano).IsRequired();
        builder.Property(f => f.NumeroSequencial).IsRequired();
        builder.Property(f => f.NumeroCompleto).IsRequired().HasMaxLength(64);

        // Sequencial unique per loja+ano (RN03 do UC01)
        builder.HasIndex(f => new { f.LojaId, f.Ano, f.NumeroSequencial }).IsUnique();
        builder.HasIndex(f => f.NumeroCompleto).IsUnique();
        builder.HasIndex(f => f.VendaId).IsUnique();

        builder.Property(f => f.DataEmissao).IsRequired();
        builder.Property(f => f.ClienteId);
        builder.HasOne(f => f.Cliente)
            .WithMany()
            .HasForeignKey(f => f.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.TotalSemIva).IsRequired().HasPrecision(10, 2);
        builder.Property(f => f.TotalIva).IsRequired().HasPrecision(10, 2);
        builder.Property(f => f.Total).IsRequired().HasPrecision(10, 2);
    }
}
