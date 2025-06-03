using Ardalis.Result;
using MediatR;
using TNK.Core.ServiceManagementAggregate.Enums; // For BookingStatus
using TNK.UseCases.Common.Models; // For PagedResult
using System;
using System.Collections.Generic;

namespace TNK.UseCases.Bookings.ListByBusiness;

public record ListBookingsByBusinessQuery(
    int BusinessProfileId, // For authorization and primary scope

    // Optional Filters
    Guid? WorkerId = null,
    Guid? ServiceId = null,
    string? CustomerId = null, // Customer's ApplicationUserId
    BookingStatus? Status = null,
    DateTime? DateFrom = null, // Represents the start of the day
    DateTime? DateTo = null,   // Represents the start of the day (query will include up to end of this day)

    // Pagination
    int PageNumber = 1,
    int PageSize = 10

) : IRequest<Result<Common.Models.PagedResult<BookingDTO>>>; // Returns a paged list of BookingDTOs
