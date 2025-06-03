using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking
using TNK.UseCases.Bookings.Specifications; // For BookingByIdWithDetailsSpec
using TNK.UseCases.Bookings; // For BookingDTO
using TNK.Core.Identity; // For ApplicationUser (Customer)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.Bookings.UpdateNotesByVendor;

public class UpdateBookingNotesByVendorHandler : IRequestHandler<UpdateBookingNotesByVendorCommand, Result<BookingDTO>>
{
  private readonly IRepository<Booking> _repository;
  private readonly IValidator<UpdateBookingNotesByVendorCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateBookingNotesByVendorHandler> _logger;

  public UpdateBookingNotesByVendorHandler(
      IRepository<Booking> repository,
      IValidator<UpdateBookingNotesByVendorCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateBookingNotesByVendorHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(UpdateBookingNotesByVendorCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateBookingNotesByVendorCommand for BookingId: {BookingId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateBookingNotesByVendorCommand: {Errors}", validationResult.Errors);
      return Result<BookingDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<BookingDTO>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized for BusinessProfileId ({CmdBusinessId}) to update booking notes.",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new BookingByIdWithDetailsSpec(request.BookingId); // Fetches booking with related details
    var bookingToUpdate = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (bookingToUpdate == null)
    {
      _logger.LogWarning("Booking with Id {BookingId} not found.", request.BookingId);
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }

    if (bookingToUpdate.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Booking (Id: {BookingId}) belongs to BusinessProfileId {ActualBusinessId}, but action was attempted for BusinessProfileId {CmdBusinessId}.",
          request.BookingId, bookingToUpdate.BusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("Access to this booking is not allowed for the specified business profile.");
    }

    try
    {
      // Preserve existing customer notes, only update vendor notes
      bookingToUpdate.UpdateNotes(bookingToUpdate.NotesByCustomer, request.VendorNotes);

      await _repository.UpdateAsync(bookingToUpdate, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      // Map to DTO
      var customer = bookingToUpdate.Customer;
      var service = bookingToUpdate.Service;
      var worker = bookingToUpdate.Worker;

      var bookingDto = new BookingDTO(
          bookingToUpdate.Id,
          bookingToUpdate.BusinessProfileId,
          bookingToUpdate.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
          customer?.Email,
          customer?.PhoneNumber,
          bookingToUpdate.ServiceId,
          service?.Name ?? "N/A",
          bookingToUpdate.WorkerId,
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
          bookingToUpdate.AvailabilitySlotId,
          bookingToUpdate.BookingStartTime,
          bookingToUpdate.BookingEndTime,
          bookingToUpdate.Status,
          bookingToUpdate.NotesByCustomer, // Preserved
          bookingToUpdate.NotesByVendor,   // Updated
          bookingToUpdate.PriceAtBooking,
          bookingToUpdate.CancellationReason,
          bookingToUpdate.CreatedAt,
          bookingToUpdate.UpdatedAt
      );

      _logger.LogInformation("Successfully updated vendor notes for Booking with Id: {BookingId}", bookingToUpdate.Id);
      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating vendor notes for booking {BookingId}: {ErrorMessage}", request.BookingId, ex.Message);
      return Result<BookingDTO>.Error($"An error occurred while updating vendor notes: {ex.Message}");
    }
  }
}
