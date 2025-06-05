using Ardalis.Result;
using Ardalis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Required for List<ValidationError>
using System.Linq; // Required for .Any()
using System.Threading;
using System.Threading.Tasks;
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking, Service, Worker, AvailabilitySlot entities
using TNK.Core.ServiceManagementAggregate.Enums;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Core.Interfaces; // For IRepository and IReadRepository
using TNK.UseCases.Bookings; // For BookingDTO
using TNK.UseCases.AvailabilitySlots.Specifications; // For SlotByIdAndWorkerSpec

namespace TNK.UseCases.Bookings.Create;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<BookingDTO>>
{
  private readonly IBookingRepository _bookingRepository;
  private readonly IAvailabilitySlotRepository _slotRepository;
  private readonly IReadRepository<Service> _serviceRepository;
  private readonly IReadRepository<Worker> _workerRepository;
  private readonly ILogger<CreateBookingCommandHandler> _logger;
  private readonly ICurrentUserService _currentUserService;

  public CreateBookingCommandHandler(
      IBookingRepository bookingRepository,
      IAvailabilitySlotRepository slotRepository,
      IReadRepository<Service> serviceRepository,
      IReadRepository<Worker> workerRepository,
      ICurrentUserService currentUserService,
      ILogger<CreateBookingCommandHandler> logger)
  {
    _bookingRepository = bookingRepository;
    _slotRepository = slotRepository;
    _serviceRepository = serviceRepository;
    _workerRepository = workerRepository;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
  {
    try
    {
      // 1. Validate the requested Availability Slot
      // Ensure SlotByIdAndWorkerSpec is correctly located and namespaced in your project.
      var slotSpec = new SlotByIdAndWorkerSpec(request.AvailabilitySlotId, request.WorkerId, request.BusinessProfileId);
      var availabilitySlot = await _slotRepository.FirstOrDefaultAsync(slotSpec, cancellationToken);

      if (availabilitySlot == null)
      {
        return Result<BookingDTO>.NotFound($"Availability slot with ID {request.AvailabilitySlotId} for worker {request.WorkerId} not found or does not belong to business {request.BusinessProfileId}.");
      }

      if (availabilitySlot.Status != AvailabilitySlotStatus.Available)
      {
        return Result<BookingDTO>.Invalid(new List<ValidationError>
                {
                    new() { ErrorMessage = "The selected time slot is no longer available.", Identifier = nameof(request.AvailabilitySlotId) }
                });
      }

      // 2. Fetch Service details
      var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
      if (service == null || service.BusinessProfileId != request.BusinessProfileId)
      {
        return Result<BookingDTO>.NotFound($"Service with ID {request.ServiceId} not found or does not belong to business {request.BusinessProfileId}.");
      }
      if (!service.IsActive)
      {
        return Result<BookingDTO>.Invalid(new List<ValidationError>
                {
                    new() { ErrorMessage = "The selected service is currently not active.", Identifier = nameof(request.ServiceId) }
                });
      }

      // 3. Fetch Worker details
      // IMPORTANT: Ensure your _workerRepository.GetByIdAsync or the specification used
      // loads the worker.Services collection if it's not loaded by default.
      var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
      if (worker == null || worker.BusinessProfileId != request.BusinessProfileId)
      {
        return Result<BookingDTO>.NotFound($"Worker with ID {request.WorkerId} not found or does not belong to business {request.BusinessProfileId}.");
      }
      if (!worker.IsActive)
      {
        return Result<BookingDTO>.Invalid(new List<ValidationError>
                {
                    new() { ErrorMessage = "The selected worker is currently not active.", Identifier = nameof(request.WorkerId) }
                });
      }

      // Check if the worker's Services collection contains the requested ServiceId
      if (worker.Services == null || !worker.Services.Any(s => s.Id == request.ServiceId))
      {
        return Result<BookingDTO>.Invalid(new List<ValidationError>
                {
                    new() { ErrorMessage = $"Worker {worker.FirstName} {worker.LastName} does not offer the selected service '{service.Name}'. Ensure worker services are loaded when fetching the worker.", Identifier = nameof(request.ServiceId) }
                });
      }

      // 4. Validate slot duration against service duration
      var slotDuration = availabilitySlot.EndTime - availabilitySlot.StartTime;
      if (slotDuration.TotalMinutes != service.DurationInMinutes)
      {
        _logger.LogWarning("Slot duration mismatch: Slot is {SlotDuration} mins, Service requires {ServiceDuration} mins. SlotId: {SlotId}, ServiceId: {ServiceId}",
            slotDuration.TotalMinutes, service.DurationInMinutes, availabilitySlot.Id, service.Id);
        return Result<BookingDTO>.Invalid(new List<ValidationError>
                {
                    new() { ErrorMessage = $"The selected time slot duration ({slotDuration.TotalMinutes} min) does not match the service duration ({service.DurationInMinutes} min). Please select a valid slot for this service.", Identifier = nameof(request.AvailabilitySlotId) }
                });
      }

      // 5. Create the Booking entity - using the actual constructor parameters
      var newBooking = new Booking(
          businessProfileId: request.BusinessProfileId,
          customerId: request.CustomerId,
          serviceId: request.ServiceId,
          workerId: request.WorkerId,
          availabilitySlotId: request.AvailabilitySlotId,
          bookingStartTime: availabilitySlot.StartTime,
          bookingEndTime: availabilitySlot.EndTime,
          priceAtBooking: service.Price
      );
      // Set notes using the entity's method
      newBooking.UpdateNotes(request.NotesByCustomer, null);


      // 6. Update AvailabilitySlot status
      availabilitySlot.BookSlot(newBooking.Id);
      await _slotRepository.UpdateAsync(availabilitySlot, cancellationToken);

      // 7. Add and save the booking
      await _bookingRepository.AddAsync(newBooking, cancellationToken);
      // Assuming repositories handle SaveChanges or a Unit of Work pattern is used elsewhere.

      _logger.LogInformation("Booking created successfully: {BookingId} for customer {CustomerId} with service {ServiceId}",
          newBooking.Id, newBooking.CustomerId, newBooking.ServiceId);

      // 8. Map to DTO and return, sourcing denormalized fields correctly
      var bookingDto = new BookingDTO(
          newBooking.Id,
          newBooking.BusinessProfileId,
          newBooking.CustomerId,
          request.CustomerName, // Sourced from the command/request
          request.CustomerEmail, // Sourced from the command/request
          request.CustomerPhoneNumber, // Sourced from the command/request
          newBooking.ServiceId,
          service.Name, // Sourced from the fetched service entity
          newBooking.WorkerId,
          worker.FullName, // Sourced from the fetched worker entity's FullName property
          newBooking.AvailabilitySlotId,
          newBooking.BookingStartTime,
          newBooking.BookingEndTime,
          newBooking.Status,
          newBooking.NotesByCustomer, // From the entity, which was set from request via UpdateNotes
          newBooking.NotesByVendor, // Will be null initially
          newBooking.PriceAtBooking,
          newBooking.CancellationReason, // Will be null initially
          newBooking.CreatedAt,
          newBooking.UpdatedAt
      );

      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while creating booking for request: {@CreateBookingCommand}", request);
      return Result<BookingDTO>.Error("An unexpected error occurred while creating the booking. Please try again.");
    }
  }
}
