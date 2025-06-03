using FluentValidation;
using System;
using TNK.Core.ServiceManagementAggregate.Enums;

namespace TNK.UseCases.Bookings.ListByBusiness;

public class ListBookingsByBusinessQueryValidator : AbstractValidator<ListBookingsByBusinessQuery>
{
  public ListBookingsByBusinessQueryValidator()
  {
    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required.");

    RuleFor(x => x.PageNumber)
        .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

    RuleFor(x => x.PageSize)
        .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
        .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100."); // Max page size limit

    RuleFor(x => x.DateTo)
        .GreaterThanOrEqualTo(x => x.DateFrom)
        .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
        .WithMessage("DateTo must be on or after DateFrom.");

    RuleFor(x => x.Status)
        .IsInEnum().When(x => x.Status.HasValue)
        .WithMessage("A valid booking status must be provided if filtering by status.");
  }
}
