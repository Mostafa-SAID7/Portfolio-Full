using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class TestimonialValidator : AbstractValidator<Testimonial>
    {
        public TestimonialValidator()
        {
            RuleFor(t => t.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(t => t.Position)
                .MaximumLength(100).WithMessage("Position must not exceed 100 characters.");

            RuleFor(t => t.Company)
                .MaximumLength(100).WithMessage("Company must not exceed 100 characters.");

            RuleFor(t => t.Content)
                .NotEmpty().WithMessage("Content is required.")
                .MaximumLength(1000).WithMessage("Content must not exceed 1000 characters.");

            RuleFor(t => t.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

            RuleFor(t => t.ImageUrl)
                .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.");

            RuleFor(t => t.ProjectUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(t => !string.IsNullOrEmpty(t.ProjectUrl))
                .WithMessage("Project URL must be a valid URL.");
        }
    }
}
