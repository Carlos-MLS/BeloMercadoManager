using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class CaixaDiariaConfiguration : IEntityTypeConfiguration<CaixaDiaria>
{
    public void Configure(EntityTypeBuilder<CaixaDiaria> builder)
    {
        builder.ToTable("CaixasDiarias");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.LojaId).IsRequired();
        builder.HasOne(c => c.Loja)
            .WithMany()
            .HasForeignKey(c => c.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Data).IsRequired();

        builder.Property(c => c.OperadorAberturaId).IsRequired();
        builder.Property(c => c.OperadorFechoId);

        builder.Property(c => c.DataHoraAbertura).IsRequired();
        builder.Property(c => c.DataHoraFecho);

        builder.Property(c => c.FundoInicial).IsRequired().HasPrecision(10, 2);
        builder.Property(c => c.ValorEsperado).HasPrecision(10, 2);
        builder.Property(c => c.ValorContado).HasPrecision(10, 2);
        builder.Property(c => c.Diferenca).HasPrecision(10, 2);

        builder.Property(c => c.Justificacao).HasMaxLength(512);

        builder.Property(c => c.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(c => c.ConsolidacaoTriggered).IsRequired();

        // RN01: 1 caixa por loja por dia
        builder.HasIndex(c => new { c.LojaId, c.Data }).IsUnique();
    }
}
