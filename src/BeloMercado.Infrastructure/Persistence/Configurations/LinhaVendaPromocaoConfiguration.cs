using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class LinhaVendaPromocaoConfiguration : IEntityTypeConfiguration<LinhaVendaPromocao>
{
    public void Configure(EntityTypeBuilder<LinhaVendaPromocao> builder)
    {
        builder.ToTable("LinhaVendaPromocao");
        builder.HasKey(lp => new { lp.LinhaVendaId, lp.PromocaoId });

        builder.HasOne(lp => lp.LinhaVenda)
            .WithMany()
            .HasForeignKey(lp => lp.LinhaVendaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lp => lp.Promocao)
            .WithMany()
            .HasForeignKey(lp => lp.PromocaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(lp => lp.ValorDescontoAplicado)
            .IsRequired()
            .HasPrecision(10, 2);
    }
}
