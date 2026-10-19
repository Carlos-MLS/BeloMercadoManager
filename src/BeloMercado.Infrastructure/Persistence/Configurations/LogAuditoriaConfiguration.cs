using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> builder)
    {
        builder.ToTable("LogsAuditoria");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UtilizadorId);

        builder.Property(l => l.Acao)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.Timestamp)
            .IsRequired();

        builder.Property(l => l.IpAddress)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.TargetEntidade)
            .HasMaxLength(64);

        builder.Property(l => l.TargetId);

        builder.Property(l => l.Resultado)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(l => l.Detalhes)
            .HasMaxLength(1024);

        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => new { l.UtilizadorId, l.Timestamp });
    }
}
