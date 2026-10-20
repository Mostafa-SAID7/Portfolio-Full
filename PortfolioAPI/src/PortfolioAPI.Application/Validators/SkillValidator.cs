using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class SkillValidator : AbstractValidator<Skill>
    {
        public SkillValidator()
        {
            RuleFor(s => s.NameEn)
                .NotEmpty().WithMessage("Skill name (en) is required.")
                .MaximumLength(50).WithMessage("Skill name (en) must not exceed 50 characters.");

            RuleFor(s => s.NameAr)
                .NotEmpty().WithMessage("Skill name (ar) is required.")
                .MaximumLength(50).WithMessage("Skill name (ar) must not exceed 50 characters.");

            RuleFor(s => s.CategoryEn)
                .NotEmpty().WithMessage("Category (en) is required.")
                .MaximumLength(100).WithMessage("Category (en) must not exceed 100 characters.");

            RuleFor(s => s.CategoryAr)
                .NotEmpty().WithMessage("Category (ar) is required.")
                .MaximumLength(100).WithMessage("Category (ar) must not exceed 100 characters.");

            RuleFor(s => s.Proficiency)
                .InclusiveBetween(0, 100).WithMessage("Proficiency must be between 0 and 100.");

            RuleFor(s => s.IconUrl)
                .MaximumLength(500).WithMessage("Icon URL must not exceed 500 characters.");

            RuleFor(s => s.DescriptionEn)
                .MaximumLength(500).WithMessage("Description (en) must not exceed 500 characters.");

            RuleFor(s => s.DescriptionAr)
                .MaximumLength(500).WithMessage("Description (ar) must not exceed 500 characters.");
        }
    }
}
