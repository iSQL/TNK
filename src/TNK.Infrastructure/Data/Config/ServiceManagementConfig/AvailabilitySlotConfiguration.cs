using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums; // For enum conversions
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
  public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
  {
    builder.ToTable("AvailabilitySlots", DataSchemaConstants.DefaultSchema);

    builder.HasKey(a => a.Id);

    builder.Property(a => a.StartTime).IsRequired();
    builder.Property(a => a.EndTime).IsRequired();

    builder.Property(a => a.Status)
        .IsRequired()
        .HasConversion<string>() // Store enum as string for readability
        .HasMaxLength(50);

    // Foreign Key to Worker
    builder.HasOne(a => a.Worker)
        .WithMany(w => w.AvailabilitySlots)
        .HasForeignKey(a => a.WorkerId)
        .OnDelete(DeleteBehavior.Cascade); // If worker is deleted, their slots are deleted

    // Foreign Key to BusinessProfile (denormalized)
    builder.HasOne(a => a.BusinessProfile)
        .WithMany()
        .HasForeignKey(a => a.BusinessProfileId)
        .OnDelete(DeleteBehavior.Restrict);

    // Optional Foreign Key to Schedule (that generated this slot)
    builder.HasOne(a => a.GeneratingSchedule)
        .WithMany() // Schedule doesn't have a direct collection of generated slots
        .HasForeignKey(a => a.GeneratingScheduleId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.SetNull); // If schedule is deleted, just nullify the link

    // Relationship with Booking (AvailabilitySlot is the principal for this 1-to-0..1)
    // Booking entity will configure the other side with HasOne(b => b.AvailabilitySlot).WithOne(a => a.Booking)
    // However, we can define the FK on the Booking side here if preferred, or ensure BookingId in AvailabilitySlot is indexed.
    builder.HasOne(a => a.Booking)
        .WithOne(b => b.AvailabilitySlot) // This establishes the 1-to-1 with Booking being dependent
        .HasForeignKey<Booking>(b => b.AvailabilitySlotId) // FK is on Booking table
        .IsRequired(false) // A slot may not have a booking
        .OnDelete(DeleteBehavior.Restrict); // Don't delete slot if booking exists, handle cancellation logic first. Or SetNull if booking can exist without a slot (unlikely)

    builder.HasIndex(a => new { a.WorkerId, a.StartTime, a.EndTime }).IsUnique(false); // Common query
    builder.HasIndex(a => a.BusinessProfileId);
    builder.HasIndex(a => a.BookingId).IsUnique(); // A booking ID can only be linked to one slot
  }
}
