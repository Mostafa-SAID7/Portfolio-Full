using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    /// <summary>
    /// Rules for an admin reply. Mirrors the stored-message limits so the
    /// dashboard composer and the API can never disagree, and re-checks the
    /// recipient because a reply goes out to a real address.
    /// </summary>
    public class ContactReplyValidator : AbstractValidator<ContactReply>
    {
        public ContactReplyValidator()
        {
            RuleFor(r => r.Body)
                .NotEmpty().WithMessage("Write a reply before sending it.")
                .MaximumLength(ContactReply.BodyMaxLength)
                .WithMessage($"Reply must not exceed {ContactReply.BodyMaxLength} characters.");

            RuleFor(r => r.RecipientEmail)
                .NotEmpty().WithMessage("This message has no reply address.")
                .EmailAddress().WithMessage("This message has an invalid reply address.");

            RuleFor(r => r.Subject)
                .MaximumLength(255).WithMessage("Reply subject must not exceed 255 characters.");
        }
    }
}
