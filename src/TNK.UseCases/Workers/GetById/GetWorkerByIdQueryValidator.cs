using FluentValidation;

namespace TNK.UseCases.Workers.GetById;

public class GetWorkerByIdQueryValidator : AbstractValidator<GetWorkerByIdQuery>
{
  public GetWorkerByIdQueryValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
