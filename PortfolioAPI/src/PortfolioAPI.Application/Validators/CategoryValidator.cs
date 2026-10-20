using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class CategoryValidator : AbstractValidator<Category>
    {
        public CategoryValidator()
        {
            RuleFor(c => c.NameEn)
                .NotEmpty().WithMessage("Category name (en) is required.")
                .MaximumLength(100).WithMessage("Category name (en) must not exceed 100 characters.");

            RuleFor(c => c.NameAr)
                .NotEmpty().WithMessage("Category name (ar) is required.")
                .MaximumLength(100).WithMessage("Category name (ar) must not exceed 100 characters.");
        }
    }
}
