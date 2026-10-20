using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class ExperienceValidator : AbstractValidator<Experience>
    {
        public ExperienceValidator()
        {
            RuleFor(e => e.CompanyEn)
                .NotEmpty().WithMessage("Company (en) is required.")
                .MaximumLength(100).WithMessage("Company (en) must not exceed 100 characters.");

            RuleFor(e => e.CompanyAr)
                .NotEmpty().WithMessage("Company (ar) is required.")
                .MaximumLength(100).WithMessage("Company (ar) must not exceed 100 characters.");

            RuleFor(e => e.PositionEn)
                .NotEmpty().WithMessage("Position (en) is required.")
                .MaximumLength(100).WithMessage("Position (en) must not exceed 100 characters.");

            RuleFor(e => e.PositionAr)
                .NotEmpty().WithMessage("Position (ar) is required.")
                .MaximumLength(100).WithMessage("Position (ar) must not exceed 100 characters.");

            RuleFor(e => e.DescriptionEn)
                .NotEmpty().WithMessage("Description (en) is required.")
                .MaximumLength(1000).WithMessage("Description (en) must not exceed 1000 characters.");

            RuleFor(e => e.DescriptionAr)
                .NotEmpty().WithMessage("Description (ar) is required.")
                .MaximumLength(1000).WithMessage("Description (ar) must not exceed 1000 characters.");

            RuleFor(e => e.StartDate)
                .Must(d => d != default(DateTime)).WithMessage("Start date is required.");

            RuleFor(e => e.LocationEn)
                .MaximumLength(100).WithMessage("Location (en) must not exceed 100 characters.");

            RuleFor(e => e.LocationAr)
                .MaximumLength(100).WithMessage("Location (ar) must not exceed 100 characters.");

            RuleFor(e => e.EndDate)
                .GreaterThanOrEqualTo(e => e.StartDate)
                .When(e => e.EndDate.HasValue)
                .WithMessage("End date must be after the start date.");

            // Current ↔ EndDate invariant: a role marked Current must not carry an end date,
            // and a past role must have one.
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
