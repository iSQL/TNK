using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class BreakRuleConfiguration : IEntityTypeConfiguration<BreakRule>
{
  public void Configure(EntityTypeBuilder<BreakRule> builder)
  {
    builder.ToTable("BreakRules", DataSchemaConstants.DefaultSchema);

    builder.HasKey(b => b.Id);

    builder.Property(b => b.Name)
        .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
        .IsRequired();
    builder.Property(b => b.StartTime).IsRequired();
    builder.Property(b => b.EndTime).IsRequired();

    // Relationship to ScheduleRuleItem (Parent)
    // This is already configured in ScheduleRuleItemConfiguration via HasMany.
    // builder.HasOne(b => b.ScheduleRuleItem)
    //     .WithMany(ri => ri.Breaks)
    //     .HasForeignKey(b => b.ScheduleRuleItemId);
    //
    // Note: If BreakRule can also be parented by ScheduleOverride, the FK setup needs adjustment.
    // For now, assuming it's only parented by ScheduleRuleItem as per current entity design.
    // If ScheduleOverride.Breaks is implemented with this same BreakRule entity,
    // then BreakRule would need a nullable ScheduleRuleItemId and a nullable ScheduleOverrideId,
    // and configurations here and in ScheduleOverrideConfiguration would need to reflect that.
    // For simplicity, the current design has ScheduleOverride.Breaks commented out.

    builder.HasIndex(b => b.ScheduleRuleItemId);
  }
}
