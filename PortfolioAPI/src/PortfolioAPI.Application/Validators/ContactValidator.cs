using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class ContactValidator : AbstractValidator<Contact>
    {
        public ContactValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(c => c.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(c => c.Phone)
                .MaximumLength(15).WithMessage("Phone number must not exceed 15 characters.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number must be a valid format.")
                .When(c => !string.IsNullOrEmpty(c.Phone));

            RuleFor(c => c.Subject)
                .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

            RuleFor(c => c.Message)
                .NotEmpty().WithMessage("Message is required.")
                .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters.");
        }
    }
}