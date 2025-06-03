using FluentValidation;
using System; // For Guid, DateTime

namespace TNK.UseCases.AvailabilitySlots.ListByWorkerAndDate;

public class ListAvailabilitySlotsQueryValidator : AbstractValidator<ListAvailabilitySlotsQuery>
{
  public ListAvailabilitySlotsQueryValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.StartDate)
        .NotEmpty().WithMessage("Start date is required.");

    RuleFor(x => x.EndDate)
        .NotEmpty().WithMessage("End date is required.")
        .GreaterThanOrEqualTo(x => x.StartDate)
        .WithMessage("End date must be on or after start date.");
  }
}
