using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using TNK.Core.BusinessAggregate;
using TNK.Core.ServiceManagementAggregate.Enums;

namespace TNK.Core.ServiceManagementAggregate.Entities;

// Represents a concrete, bookable time slot for a worker.
// These can be generated from a Schedule or created ad-hoc.
public class AvailabilitySlot : EntityBase<Guid>, IAggregateRoot
{
  public Guid WorkerId { get; private set; }
  public virtual Worker? Worker { get; private set; }

  public int BusinessProfileId { get; private set; } // Denormalized for easier vendor-specific queries
  public virtual BusinessProfile? BusinessProfile { get; private set; }

  public DateTime StartTime { get; private set; } // Specific start date and time of the slot (UTC or with TimeZone info)
  public DateTime EndTime { get; private set; }   // Specific end date and time of the slot (UTC or with TimeZone info)

  public AvailabilitySlotStatus Status { get; private set; }

  public Guid? GeneratingScheduleId { get; private set; } // Optional: FK to the Schedule that generated this slot
  public virtual Schedule? GeneratingSchedule { get; private set; }

  public Guid? BookingId { get; private set; } // If booked, link to the booking
  public virtual Booking? Booking { get; private set; }


  private AvailabilitySlot() { }

  public AvailabilitySlot(Guid workerId, int businessProfileId, DateTime startTime, DateTime endTime, AvailabilitySlotStatus status = AvailabilitySlotStatus.Available, Guid? generatingScheduleId = null)
  {
    WorkerId = Guard.Against.Default(workerId, nameof(workerId));
    BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
    StartTime = Guard.Against.Default(startTime, nameof(startTime));
    EndTime = Guard.Against.Default(endTime, nameof(endTime));
    Status = status;
    GeneratingScheduleId = generatingScheduleId;

    if (EndTime <= StartTime)
    {
      throw new ArgumentException("End time must be after start time.");
    }
  }

  public void BookSlot(Guid bookingId)
  {
    if (Status != AvailabilitySlotStatus.Available)
    {
      // Consider specific exceptions for different non-available states
      throw new InvalidOperationException("Slot is not available for booking.");
    }
    Status = AvailabilitySlotStatus.Booked;
    BookingId = Guard.Against.Default(bookingId, nameof(bookingId));
  }

  public void ReleaseSlot() // e.g. if a booking is cancelled
  {
    if (Status != AvailabilitySlotStatus.Booked && Status != AvailabilitySlotStatus.Pending)
    {
      // Or log a warning if trying to release a slot that wasn't booked/pending
      throw new InvalidOperationException("Slot is not in a state that can be released to available.");
    }
    Status = AvailabilitySlotStatus.Available;
    BookingId = null;
  }

  public void MarkAsUnavailable(string reason = "Blocked") // Reason could be logged elsewhere or added as a property
  {
    if (Status == AvailabilitySlotStatus.Booked)
    {
      throw new InvalidOperationException("Cannot mark a booked slot as unavailable directly. Cancel booking first.");
    }
    Status = AvailabilitySlotStatus.Unavailable;
    BookingId = null;
  }

  public void MarkAsBreak()
  {
    if (Status == AvailabilitySlotStatus.Booked)
    {
      throw new InvalidOperationException("Cannot mark a booked slot as break. Cancel booking first.");
    }
    Status = AvailabilitySlotStatus.Break;
    BookingId = null;
  }

  /// <summary>
  /// Updates the time of the availability slot.
  /// </summary>
  /// <param name="newStartTime">The new start time.</param>
  /// <param name="newEndTime">The new end time.</param>
  /// <param name="isRescheduleOfBookedSlot">
  /// Set to true if this time update is part of a reschedule for an already booked slot.
  /// This flag helps differentiate between updating an open slot vs. adjusting a booked one.
  /// The application service/handler is responsible for updating the associated Booking entity's times if this is true.
  /// </param>
  public void UpdateTime(DateTime newStartTime, DateTime newEndTime, bool isRescheduleOfBookedSlot = false)
  {
    Guard.Against.Default(newStartTime, nameof(newStartTime));
    Guard.Against.Default(newEndTime, nameof(newEndTime));
    if (newEndTime <= newStartTime)
    {
      throw new ArgumentException("New end time must be after new start time.");
    }

    if (Status == AvailabilitySlotStatus.Booked && !isRescheduleOfBookedSlot)
    {
      // Prevent accidental direct time changes on booked slots without going through a "reschedule" flow
      // that also handles the booking entity.
      throw new InvalidOperationException("Cannot directly update time for a booked slot without explicit reschedule intent. Associated booking must also be updated.");
    }

    StartTime = newStartTime;
    EndTime = newEndTime;
    // Note: Changing time might make it conflict with other slots.
    // Collision detection should be handled by the Application Service before calling this method.
  }

  public void ClearBookingLink()
  {
    this.BookingId = null;
    // Optionally, if clearing the booking link should always make the slot 'Available'
    // (and it's not already being set to something else like 'Unavailable'), you could add:
    // if (this.Status == AvailabilitySlotStatus.Booked) // Or any other relevant check
    // {
    //     this.Status = AvailabilitySlotStatus.Available;
    // }
    // However, the UpdateAvailabilitySlotHandler currently manages setting the new status explicitly
    // before potentially calling this, which is a cleaner separation of concerns.
    // So, just clearing BookingId here is sufficient if the handler sets the status.
  }

  /// <summary>
  /// Updates the status of the availability slot.
  /// Handles unlinking booking if status changes from Booked.
  /// </summary>
  public void UpdateStatus(AvailabilitySlotStatus newStatus)
  {
    if (Status == newStatus) return;

    // If the slot was booked and is now becoming something else, the BookingId should be cleared by the handler/service.
    // If the slot is becoming Booked, the BookingId should be set by the handler/service (via BookSlot).
    // This method just updates the status. The handler coordinates BookingId.
    Status = newStatus;
  }
}
