using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class UtilizadorConfiguration : IEntityTypeConfiguration<Utilizador>
{
    // Stable seed Guid so re-running migrations does not regenerate the admin row.
    public static readonly Guid SeedAdminId = new("11111111-1111-1111-1111-111111111111");

    // BCrypt cost-11 hash of "BeloMercado2026!" (academic-prototype default, change in prod).
    private const string SeedAdminPasswordHash =
        "$2a$11$NTAjq03pqc8lDzIqdr0nDe42S040X37qaOce9t4ds9obe5vlEF3mC";

    private static readonly DateTime SeedAdminCreatedAt =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Utilizador> builder)
    {
        builder.ToTable("Utilizadores");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordTemporaria)
            .IsRequired();

        builder.Property(u => u.Perfil)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(u => u.LojaId);
        builder.HasOne<Loja>()
            .WithMany()
            .HasForeignKey(u => u.LojaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(u => u.LojaId);

        builder.Property(u => u.Ativo)
            .IsRequired();

        builder.Property(u => u.DataCriacao)
            .IsRequired();

        builder.Property(u => u.UltimoLogin);

        builder.Property(u => u.TentativasFalhadas)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.BloqueadoAte);

        builder.HasData(new Utilizador
        {
            Id = SeedAdminId,
            Nome = "Administrador BeloMercado",
            Email = "admin@belomercado.local",
            PasswordHash = SeedAdminPasswordHash,
            PasswordTemporaria = false,
            Perfil = Perfil.AdminCadeia,
            LojaId = null,
            Ativo = true,
            DataCriacao = SeedAdminCreatedAt,
            TentativasFalhadas = 0
        });
    }
}
