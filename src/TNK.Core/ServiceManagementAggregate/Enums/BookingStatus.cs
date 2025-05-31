namespace TNK.Core.ServiceManagementAggregate.Enums;

public enum BookingStatus
{
  PendingConfirmation = 1, // Initial status when a customer requests a booking
  Confirmed = 2,           // Vendor has confirmed the booking
  CancelledByCustomer = 3, // Customer cancelled the booking
  CancelledByVendor = 4,   // Vendor cancelled the booking
  Completed = 5,           // Service rendered, booking fulfilled
  NoShow = 6,              // Customer did not show up
  Rescheduled = 7          // Booking was changed to a different time/slot
}
