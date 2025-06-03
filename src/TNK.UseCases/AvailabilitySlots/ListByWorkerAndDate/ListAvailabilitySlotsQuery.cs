using Ardalis.Result;
using MediatR;
using System; // For Guid, DateTime (or DateOnly if preferred for request)
using System.Collections.Generic; // For List
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO

namespace TNK.UseCases.AvailabilitySlots.ListByWorkerAndDate;

public record ListAvailabilitySlotsQuery(
    Guid WorkerId,
    int BusinessProfileId, // For authorization
    DateTime StartDate,    // Could be DateOnly if UI sends just date
    DateTime EndDate      // Could be DateOnly
                          // Potentially add pagination parameters if lists can be very long
) : IRequest<Result<List<AvailabilitySlotDTO>>>;
