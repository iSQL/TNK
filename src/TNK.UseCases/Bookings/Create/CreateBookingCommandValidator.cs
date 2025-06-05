using FluentValidation;
using System;

namespace TNK.UseCases.Bookings.Create;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
  public CreateBookingCommandValidator()
  {
    RuleFor(cmd => cmd.BusinessProfileId)
        .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(cmd => cmd.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(cmd => cmd.ServiceId)
        .NotEmpty().WithMessage("ServiceId is required.");

    RuleFor(cmd => cmd.AvailabilitySlotId)
        .NotEmpty().WithMessage("AvailabilitySlotId is required.");

    RuleFor(cmd => cmd.CustomerId)
        .NotEmpty().WithMessage("CustomerId is required."); // Or more complex validation if it's a specific format

    RuleFor(cmd => cmd.CustomerName)
        .NotEmpty().WithMessage("Customer name is required.")
        .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

    RuleFor(cmd => cmd.CustomerEmail)
        .NotEmpty().WithMessage("Customer email is required.")
        .EmailAddress().WithMessage("A valid email address is required.")
        .MaximumLength(255).WithMessage("Customer email cannot exceed 255 characters.");

    RuleFor(cmd => cmd.CustomerPhoneNumber)
        .NotEmpty().WithMessage("Customer phone number is required.")
        .MaximumLength(20).WithMessage("Customer phone number cannot exceed 20 characters.");
    // Consider more specific phone number validation if needed

    RuleFor(cmd => cmd.NotesByCustomer)
        .MaximumLength(500).WithMessage("Customer notes cannot exceed 500 characters.")
        .When(cmd => !string.IsNullOrEmpty(cmd.NotesByCustomer));
  }
}
