using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class ScheduleRuleItemConfiguration : IEntityTypeConfiguration<ScheduleRuleItem>
{
  public void Configure(EntityTypeBuilder<ScheduleRuleItem> builder)
  {
    builder.ToTable("ScheduleRuleItems", DataSchemaConstants.DefaultSchema);

    builder.HasKey(ri => ri.Id);

    builder.Property(ri => ri.DayOfWeek).IsRequired();
    builder.Property(ri => ri.StartTime).IsRequired();
    builder.Property(ri => ri.EndTime).IsRequired();
    builder.Property(ri => ri.IsWorkingDay).IsRequired();

    // Relationship to Schedule (Parent)
    // This is already configured in ScheduleConfiguration via HasMany.
    // builder.HasOne(ri => ri.Schedule)
    //     .WithMany(s => s.RuleItems)
    //     .HasForeignKey(ri => ri.ScheduleId);

    // Relationship to BreakRules (Children)
    builder.HasMany(ri => ri.Breaks)
        .WithOne(b => b.ScheduleRuleItem)
        .HasForeignKey(b => b.ScheduleRuleItemId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(ri => ri.ScheduleId);
  }
}
