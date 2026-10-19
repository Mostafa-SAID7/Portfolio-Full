using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class SkillValidator : AbstractValidator<Skill>
    {
        public SkillValidator()
        {
            RuleFor(s => s.Name)
                .NotEmpty().WithMessage("Skill name is required.")
                .MaximumLength(50).WithMessage("Skill name must not exceed 50 characters.");

            RuleFor(s => s.Category)
                .NotEmpty().WithMessage("Category is required.")
                .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

            RuleFor(s => s.Proficiency)
                .InclusiveBetween(0, 100).WithMessage("Proficiency must be between 0 and 100.");

            RuleFor(s => s.IconUrl)
                .MaximumLength(500).WithMessage("Icon URL must not exceed 500 characters.");

            RuleFor(s => s.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        }
    }
}
