using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class KPIDiarioConfiguration : IEntityTypeConfiguration<KPIDiario>
{
    public void Configure(EntityTypeBuilder<KPIDiario> builder)
    {
        builder.ToTable("KPIsDiarios");
        builder.HasKey(k => new { k.LojaId, k.Data });

        builder.HasOne(k => k.Loja)
            .WithMany()
            .HasForeignKey(k => k.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(k => k.TotalVendas).IsRequired().HasPrecision(12, 2);
        builder.Property(k => k.NumTransacoes).IsRequired();
        builder.Property(k => k.TicketMedio).IsRequired().HasPrecision(10, 2);
        builder.Property(k => k.TotalIva).IsRequired().HasPrecision(12, 2);
        builder.Property(k => k.TotalDescontos).IsRequired().HasPrecision(12, 2);
        builder.Property(k => k.VendasAnuladas).IsRequired();
        builder.Property(k => k.Devolucoes).IsRequired();
        builder.Property(k => k.UltimaAtualizacao).IsRequired();

        builder.HasIndex(k => new { k.LojaId, k.Data });
    }
}
