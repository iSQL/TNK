using System;
using TNK.Core.ServiceManagementAggregate.Enums; // For BookingStatus
// We'll need DTOs for related entities if we want to show their names/details
using TNK.UseCases.Services; // Assuming ServiceDTO exists
using TNK.UseCases.Workers;  // Assuming WorkerDTO exists
// For customer details, you might have a simple CustomerInfoDTO or just IDs/names

namespace TNK.UseCases.Bookings;

// DTO for representing booking information, potentially for vendor or customer view
public record BookingDTO(
    Guid Id,
    int BusinessProfileId,
    string CustomerId, // Or a CustomerInfoDTO
    string? CustomerName, // Denormalized for convenience
    string? CustomerEmail, // Denormalized
    string? CustomerPhoneNumber, //Denormalized

    Guid ServiceId,
    string ServiceName, // Denormalized from ServiceDTO or Service entity

    Guid WorkerId,
    string WorkerName, // Denormalized from WorkerDTO or Worker entity

    Guid AvailabilitySlotId,
    DateTime BookingStartTime,
    DateTime BookingEndTime,
    BookingStatus Status,
    string? NotesByCustomer,
    string? NotesByVendor,
    decimal PriceAtBooking,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// You might also have simpler DTOs for customer info if needed
// public record CustomerInfoDTO(string Id, string FullName, string Email);
