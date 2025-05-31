using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums; // For enum conversions
using TNK.Infrastructure.Data.Config;

namespace TNK.Infrastructure.Data.Config.ServiceManagementConfig;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
  public void Configure(EntityTypeBuilder<Booking> builder)
  {
    builder.ToTable("Bookings", DataSchemaConstants.DefaultSchema);

    builder.HasKey(b => b.Id);

    builder.Property(b => b.BookingStartTime).IsRequired();
    builder.Property(b => b.BookingEndTime).IsRequired();

    builder.Property(b => b.Status)
        .IsRequired()
        .HasConversion<string>() // Store enum as string
        .HasMaxLength(50);

    builder.Property(b => b.NotesByCustomer)
        .HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH);

    builder.Property(b => b.NotesByVendor)
        .HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH);

    builder.Property(b => b.CancellationReason)
        .HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH);

    builder.Property(b => b.PriceAtBooking)
        .HasColumnType("decimal(18,2)")
        .IsRequired();

    builder.Property(b => b.CreatedAt).IsRequired();

    builder.Property(b => b.UpdatedAt).IsRequired(false); // Nullable DateTime

    // Foreign Key to BusinessProfile
    builder.HasOne(b => b.BusinessProfile)
        .WithMany() // BusinessProfile doesn't have a direct collection of Bookings
        .HasForeignKey(b => b.BusinessProfileId)
        .OnDelete(DeleteBehavior.Restrict); // Or Cascade if appropriate

    // Foreign Key to Customer (ApplicationUser)
    builder.HasOne(b => b.Customer)
        .WithMany() // ApplicationUser doesn't have a direct collection of Bookings
        .HasForeignKey(b => b.CustomerId)
        .IsRequired() // A booking must have a customer
        .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a user if they have bookings; consider anonymization or soft delete for users.

    // Foreign Key to Service
    builder.HasOne(b => b.Service)
        .WithMany() // Service doesn't have a direct collection of Bookings
        .HasForeignKey(b => b.ServiceId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a service if it has bookings.

    // Foreign Key to Worker
    builder.HasOne(b => b.Worker)
        .WithMany(w => w.Bookings) // Worker has a collection of their bookings
        .HasForeignKey(b => b.WorkerId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a worker if they have bookings.

    // Foreign Key to AvailabilitySlot (One-to-One with AvailabilitySlot)
    // This is the dependent side of the 1-to-0..1 relationship with AvailabilitySlot
    // The FK AvailabilitySlotId is defined here.
    // The configuration in AvailabilitySlotConfiguration establishes the other side.
    builder.HasOne(b => b.AvailabilitySlot)
        .WithOne(a => a.Booking) // AvailabilitySlot.Booking navigation property
        .HasForeignKey<Booking>(b => b.AvailabilitySlotId) // This booking entity has the FK
        .IsRequired() // A booking must be for a specific slot
        .OnDelete(DeleteBehavior.Restrict); // If slot is deleted, what happens to booking? Usually restrict.

    builder.HasIndex(b => b.BusinessProfileId);
    builder.HasIndex(b => b.CustomerId);
    builder.HasIndex(b => b.ServiceId);
    builder.HasIndex(b => b.WorkerId);
    builder.HasIndex(b => b.AvailabilitySlotId).IsUnique(); // Each slot can only be booked once
    builder.HasIndex(b => new { b.WorkerId, b.BookingStartTime, b.BookingEndTime }); // Common query pattern
    builder.HasIndex(b => new { b.CustomerId, b.BookingStartTime }); // For customer's booking history
  }
}
