using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using TNK.Core.BusinessAggregate;
using TNK.Core.Identity; // For Customer (ApplicationUser)
using TNK.Core.ServiceManagementAggregate.Enums;

namespace TNK.Core.ServiceManagementAggregate.Entities;

public class Booking : EntityBase<Guid>, IAggregateRoot
{
  public int BusinessProfileId { get; private set; } // FK to BusinessProfile
  public virtual BusinessProfile? BusinessProfile { get; private set; }

  public string CustomerId { get; private set; } = string.Empty; // FK to ApplicationUser
  public virtual ApplicationUser? Customer { get; private set; }

  public Guid ServiceId { get; private set; }
  public virtual Service? Service { get; private set; }

  public Guid WorkerId { get; private set; }
  public virtual Worker? Worker { get; private set; }

  public Guid AvailabilitySlotId { get; private set; } // The specific slot being booked
  public virtual AvailabilitySlot? AvailabilitySlot { get; private set; }

  public DateTime BookingStartTime { get; private set; } // Denormalized from AvailabilitySlot for convenience and history
  public DateTime BookingEndTime { get; private set; }   // Denormalized from AvailabilitySlot for convenience and history

  public BookingStatus Status { get; private set; }
  public string? NotesByCustomer { get; private set; }
  public string? NotesByVendor { get; private set; }
  public decimal PriceAtBooking { get; private set; } // Denormalized from Service, in case service price changes
  public string? CancellationReason { get; private set; } // Reason if cancelled

  public DateTime CreatedAt { get; private set; }
  public DateTime? UpdatedAt { get; private set; }

  private Booking() { }

  public Booking(
      int businessProfileId,
      string customerId,
      Guid serviceId,
      Guid workerId,
      Guid availabilitySlotId,
      DateTime bookingStartTime,
      DateTime bookingEndTime,
      decimal priceAtBooking)
  {
    BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
    CustomerId = Guard.Against.NullOrWhiteSpace(customerId, nameof(customerId));
    ServiceId = Guard.Against.Default(serviceId, nameof(serviceId));
    WorkerId = Guard.Against.Default(workerId, nameof(workerId));
    AvailabilitySlotId = Guard.Against.Default(availabilitySlotId, nameof(availabilitySlotId));
    BookingStartTime = Guard.Against.Default(bookingStartTime, nameof(bookingStartTime));
    BookingEndTime = Guard.Against.Default(bookingEndTime, nameof(bookingEndTime));
    PriceAtBooking = Guard.Against.Negative(priceAtBooking, nameof(priceAtBooking));

    if (BookingEndTime <= BookingStartTime)
    {
      throw new ArgumentException("Booking end time must be after booking start time.");
    }

    Status = BookingStatus.PendingConfirmation; // Default initial status
    CreatedAt = DateTime.UtcNow;
  }

  public void ConfirmBooking()
  {
    if (Status != BookingStatus.PendingConfirmation)
    {
      throw new InvalidOperationException("Booking can only be confirmed if it's pending confirmation.");
    }
    Status = BookingStatus.Confirmed;
    UpdatedAt = DateTime.UtcNow;
    // Domain Event: BookingConfirmedEvent
  }

  public void CancelBooking(bool cancelledByVendor, string reason)
  {
    if (Status == BookingStatus.Completed || Status == BookingStatus.CancelledByCustomer || Status == BookingStatus.CancelledByVendor)
    {
      throw new InvalidOperationException($"Booking cannot be cancelled as it is already {Status}.");
    }
    Status = cancelledByVendor ? BookingStatus.CancelledByVendor : BookingStatus.CancelledByCustomer;
    CancellationReason = Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
    UpdatedAt = DateTime.UtcNow;
    // Domain Event: BookingCancelledEvent
  }
  public void UpdateTimes(DateTime newStartTime, DateTime newEndTime)
  {
    // Add validation if needed, e.g., newEndTime > newStartTime
    // Also consider if this should change the booking status, e.g., to 'Rescheduled'.
    BookingStartTime = newStartTime;
    BookingEndTime = newEndTime;
    UpdatedAt = DateTime.UtcNow;
    // Consider raising a domain event here too if appropriate
  }

  public void MarkAsCompleted()
  {
    if (Status != BookingStatus.Confirmed)
    {
      // Potentially allow marking as completed from other states if business logic allows
      throw new InvalidOperationException("Booking must be confirmed to be marked as completed.");
    }
    Status = BookingStatus.Completed;
    UpdatedAt = DateTime.UtcNow;
    // Domain Event: BookingCompletedEvent
  }

  public void MarkAsNoShow()
  {
    if (Status != BookingStatus.Confirmed)
    {
      throw new InvalidOperationException("Booking must be confirmed to be marked as no-show.");
    }
    Status = BookingStatus.NoShow;
    UpdatedAt = DateTime.UtcNow;
  }

  public void Reschedule(Guid newAvailabilitySlotId, DateTime newStartTime, DateTime newEndTime)
  {
    // More complex logic might be needed here, e.g., ensuring the new slot is valid and available.
    // This might involve creating a new booking and cancelling the old one, or directly updating this one.
    // For simplicity, let's assume direct update for now.
    Guard.Against.Default(newAvailabilitySlotId, nameof(newAvailabilitySlotId));
    Guard.Against.Default(newStartTime, nameof(newStartTime));
    Guard.Against.Default(newEndTime, nameof(newEndTime));
    if (newEndTime <= newStartTime)
    {
      throw new ArgumentException("New end time must be after new start time for reschedule.");
    }

    // Old slot should be released. New slot should be marked booked.
    // This method implies coordination with AvailabilitySlot updates.
    this.AvailabilitySlotId = newAvailabilitySlotId;
    this.BookingStartTime = newStartTime;
    this.BookingEndTime = newEndTime;
    this.Status = BookingStatus.Rescheduled; // Or back to Confirmed if reschedule implies re-confirmation
    UpdatedAt = DateTime.UtcNow;
    // Domain Event: BookingRescheduledEvent
  }

  public void UpdateNotes(string? notesByCustomer, string? notesByVendor)
  {
    NotesByCustomer = notesByCustomer; // Allow null
    NotesByVendor = notesByVendor;     // Allow null
    UpdatedAt = DateTime.UtcNow;
  }
}
