using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.UpdateNotesByVendor;

public class UpdateBookingNotesByVendorCommandValidator : AbstractValidator<UpdateBookingNotesByVendorCommand>
{
  public UpdateBookingNotesByVendorCommandValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.VendorNotes)
        // Notes can be empty or null if the vendor wants to clear them.
        // Set a max length if applicable, e.g., using your DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH
        .MaximumLength(255)
        .WithMessage($"Vendor notes cannot exceed 255 characters.");
  }
}
