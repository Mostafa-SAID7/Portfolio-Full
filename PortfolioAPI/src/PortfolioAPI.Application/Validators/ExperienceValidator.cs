using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class ExperienceValidator : AbstractValidator<Experience>
    {
        public ExperienceValidator()
        {
            RuleFor(e => e.Company)
                .NotEmpty().WithMessage("Company is required.")
                .MaximumLength(100).WithMessage("Company must not exceed 100 characters.");

            RuleFor(e => e.Position)
                .NotEmpty().WithMessage("Position is required.")
                .MaximumLength(100).WithMessage("Position must not exceed 100 characters.");

            RuleFor(e => e.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(e => e.StartDate)
                .Must(d => d != default(DateTime)).WithMessage("Start date is required.");

            RuleFor(e => e.Location)
                .MaximumLength(100).WithMessage("Location must not exceed 100 characters.");

            RuleFor(e => e.EndDate)
                .GreaterThanOrEqualTo(e => e.StartDate)
                .When(e => e.EndDate.HasValue)
                .WithMessage("End date must be after the start date.");

            // Current ↔ EndDate invariant (missing business rule): a role
            // marked Current must not carry an end date, and a past role must
            // have one — otherwise the public timeline renders contradictory
            // entries (e.g. "Current since 2020, ended 2021").
            RuleFor(e => e.EndDate)
                .Null()
                .When(e => e.Current)
                .WithMessage("A current role must not have an end date.");

            RuleFor(e => e.EndDate)
                .NotNull()
                .Unless(e => e.Current)
                .WithMessage("A past role needs an end date.");

            RuleFor(e => e.Technologies)
                .Must(list => list == null || list.Count <= 20)
                .WithMessage("An experience can list at most 20 technologies.");
        }
    }
}
