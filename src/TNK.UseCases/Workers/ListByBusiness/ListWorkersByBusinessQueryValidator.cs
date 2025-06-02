using FluentValidation;

namespace TNK.UseCases.Workers.ListByBusiness;

public class ListWorkersByBusinessQueryValidator : AbstractValidator<ListWorkersByBusinessQuery>
{
  public ListWorkersByBusinessQueryValidator()
  {
    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required.");

    // Add rules for pagination parameters if you implement them
    // RuleFor(x => x.PageNumber).GreaterThan(0);
    // RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
  }
}
