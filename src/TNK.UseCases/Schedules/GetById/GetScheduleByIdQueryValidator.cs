using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Schedules.GetById;

public class GetScheduleByIdQueryValidator : AbstractValidator<GetScheduleByIdQuery>
{
  public GetScheduleByIdQueryValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
