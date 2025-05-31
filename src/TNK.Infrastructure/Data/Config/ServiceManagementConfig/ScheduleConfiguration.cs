using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
  public void Configure(EntityTypeBuilder<Schedule> builder)
  {
    builder.ToTable("Schedules", DataSchemaConstants.DefaultSchema);

    builder.HasKey(s => s.Id);

    builder.Property(s => s.Title)
        .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
        .IsRequired();

    builder.Property(s => s.TimeZoneId)
        .HasMaxLength(100) // Max length for TimeZoneInfo.Id
        .IsRequired();

    builder.Property(s => s.EffectiveStartDate).IsRequired();
    // EffectiveEndDate is nullable by default

    // Foreign Key to Worker
    builder.HasOne(s => s.Worker)
        .WithMany(w => w.Schedules)
        .HasForeignKey(s => s.WorkerId)
        .OnDelete(DeleteBehavior.Cascade); // If Worker is deleted, their schedules are deleted

    // Foreign Key to BusinessProfile (denormalized)
    builder.HasOne(s => s.BusinessProfile)
        .WithMany()
        .HasForeignKey(s => s.BusinessProfileId)
        .OnDelete(DeleteBehavior.Restrict); // Should not delete if BusinessProfile is deleted directly without handling workers

    // Relationships to child entities
    builder.HasMany(s => s.RuleItems)
        .WithOne(ri => ri.Schedule)
        .HasForeignKey(ri => ri.ScheduleId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(s => s.Overrides)
        .WithOne(o => o.Schedule)
        .HasForeignKey(o => o.ScheduleId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(s => s.WorkerId);
    builder.HasIndex(s => s.BusinessProfileId);
  }
}
