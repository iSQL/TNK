using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class ScheduleOverrideConfiguration : IEntityTypeConfiguration<ScheduleOverride>
{
  public void Configure(EntityTypeBuilder<ScheduleOverride> builder)
  {
    builder.ToTable("ScheduleOverrides", DataSchemaConstants.DefaultSchema);

    builder.HasKey(so => so.Id);

    builder.Property(so => so.OverrideDate).IsRequired();
    builder.Property(so => so.Reason)
        .HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH)
        .IsRequired();
    builder.Property(so => so.IsWorkingDay).IsRequired();
    // StartTime and EndTime are nullable by default

    // Relationship to Schedule (Parent)
    // This is already configured in ScheduleConfiguration via HasMany.
    // builder.HasOne(so => so.Schedule)
    //     .WithMany(s => s.Overrides)
    //     .HasForeignKey(so => so.ScheduleId);

    // If ScheduleOverride has its own BreakRules (currently commented out in entity):
    // builder.HasMany(so => so.Breaks)
    //     .WithOne() // Assuming a separate OverrideBreakRule or complex FK setup for BreakRule
    //     .HasForeignKey("ScheduleOverrideId") // Example FK name
    //     .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(so => so.ScheduleId);
    builder.HasIndex(so => so.OverrideDate);
  }
}
