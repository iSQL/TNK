using Ardalis.Result;
using MediatR;
using System;
using TNK.UseCases.Bookings; // For BookingDTO

namespace TNK.UseCases.Bookings.Create;

/// <summary>
/// Represents the command to create a new booking.
/// This is typically initiated by a customer or on behalf of a customer.
/// </summary>
public record CreateBookingCommand(
    // Information to identify the context and chosen slot/service
    int BusinessProfileId,    // The business where the booking is made
    Guid WorkerId,            // The specific worker selected for the service
    Guid ServiceId,           // The specific service being booked
    Guid AvailabilitySlotId,  // The specific time slot selected

    // Customer Information
    // Assuming CustomerId might be from your ApplicationUser table if they are registered,
    // or it could be a newly provided set of details for a guest booking.
    // For simplicity, we'll take explicit customer details here.
    // In a real system, you might have a separate Customer entity or use ApplicationUser.Id.
    string CustomerId, // Could be ApplicationUser.Id or a guest identifier
    string CustomerName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    string? NotesByCustomer // Optional notes from the customer
) : IRequest<Result<BookingDTO>>; // Returns the DTO of the newly created booking
