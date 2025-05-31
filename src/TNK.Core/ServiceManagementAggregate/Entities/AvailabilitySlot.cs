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

  public void UpdateTime(DateTime newStartTime, DateTime newEndTime)
  {
    // Add validation, ensure it doesn't conflict with existing bookings if status is Booked, etc.
    // This is a complex operation if the slot is already booked.
    if (Status == AvailabilitySlotStatus.Booked)
    {
      throw new InvalidOperationException("Cannot directly update time for a booked slot. Consider rescheduling the booking.");
    }

    Guard.Against.Default(newStartTime, nameof(newStartTime));
    Guard.Against.Default(newEndTime, nameof(newEndTime));
    if (newEndTime <= newStartTime)
    {
      throw new ArgumentException("New end time must be after new start time.");
    }
    StartTime = newStartTime;
    EndTime = newEndTime;
  }
}
