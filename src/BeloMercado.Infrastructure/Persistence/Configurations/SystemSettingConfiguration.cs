using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeloMercado.Infrastructure.Persistence.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public const string TimeOffsetDaysKey = "TimeOffsetDays";

    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(256).IsRequired();

        builder.HasData(new SystemSetting { Key = TimeOffsetDaysKey, Value = "0" });
    }
}
