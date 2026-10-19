using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.ToTable("Vendas");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.LojaId).IsRequired();
        builder.HasOne(v => v.Loja)
            .WithMany()
            .HasForeignKey(v => v.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.CaixaDiariaId).IsRequired();
        builder.HasOne(v => v.CaixaDiaria)
            .WithMany()
            .HasForeignKey(v => v.CaixaDiariaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.OperadorId).IsRequired();
        builder.Property(v => v.ClienteId);
        builder.HasOne(v => v.Cliente)
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.DataHora).IsRequired();

        builder.Property(v => v.MetodoPagamento)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(v => v.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(v => v.ValorRecebido).HasPrecision(10, 2);
        builder.Property(v => v.Troco).HasPrecision(10, 2);
        builder.Property(v => v.TotalSemIva).IsRequired().HasPrecision(10, 2);
        builder.Property(v => v.TotalIva).IsRequired().HasPrecision(10, 2);
        builder.Property(v => v.Total).IsRequired().HasPrecision(10, 2);

        builder.HasMany(v => v.Linhas)
            .WithOne(l => l.Venda)
            .HasForeignKey(l => l.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.LojaId, v.DataHora });
        builder.HasIndex(v => v.CaixaDiariaId);
    }
}
