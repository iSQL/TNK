using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.AvailabilitySlots.Delete;

public class DeleteAvailabilitySlotCommandValidator : AbstractValidator<DeleteAvailabilitySlotCommand>
{
  public DeleteAvailabilitySlotCommandValidator()
  {
    RuleFor(x => x.AvailabilitySlotId)
        .NotEmpty().WithMessage("AvailabilitySlotId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for authorization context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
