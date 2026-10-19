using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class ConsolidacaoDiariaConfiguration : IEntityTypeConfiguration<ConsolidacaoDiaria>
{
    public void Configure(EntityTypeBuilder<ConsolidacaoDiaria> builder)
    {
        builder.ToTable("ConsolidacoesDiarias");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.LojaId).IsRequired();
        builder.HasOne(c => c.Loja)
            .WithMany()
            .HasForeignKey(c => c.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Data).IsRequired();

        builder.Property(c => c.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(c => c.DataInicio);
        builder.Property(c => c.DataFim);
        builder.Property(c => c.Tentativas).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.UltimaMensagemErro).HasMaxLength(1024);
        builder.Property(c => c.Origem).IsRequired().HasMaxLength(16);

        builder.HasIndex(c => new { c.LojaId, c.Data }).IsUnique();
        builder.HasIndex(c => c.Estado);
    }
}
