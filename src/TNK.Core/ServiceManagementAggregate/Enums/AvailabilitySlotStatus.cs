namespace TNK.Core.ServiceManagementAggregate.Enums;

public enum AvailabilitySlotStatus
{
  Available = 1,  // Slot is open for booking
  Booked = 2,     // Slot has a confirmed booking
  Pending = 3,    // Slot has a booking request but not yet confirmed (optional, if bookings don't immediately make slot booked)
  Unavailable = 4,// Slot is blocked (e.g., holiday, manual override, break)
  Break = 5       // Slot is designated as a break time for the worker
}
