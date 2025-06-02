using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Schedules.ListByWorker;

public class ListSchedulesByWorkerQueryValidator : AbstractValidator<ListSchedulesByWorkerQuery>
{
  public ListSchedulesByWorkerQueryValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    // Add rules for pagination parameters if you implement them
    // RuleFor(x => x.PageNumber).GreaterThan(0);
    // RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
  }
}
