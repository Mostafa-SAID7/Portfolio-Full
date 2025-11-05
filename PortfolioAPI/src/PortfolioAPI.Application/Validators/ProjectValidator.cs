using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class ProjectValidator : AbstractValidator<Project>
    {
        public ProjectValidator()
        {
            RuleFor(p => p.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(p => p.ProjectUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(p => !string.IsNullOrEmpty(p.ProjectUrl))
                .WithMessage("Project URL must be a valid URL.");

            RuleFor(p => p.RepositoryUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(p => !string.IsNullOrEmpty(p.RepositoryUrl))
                .WithMessage("Repository URL must be a valid URL.");

            RuleFor(p => p.StartDate)
                .LessThanOrEqualTo(p => p.EndDate)
                .When(p => p.StartDate.HasValue && p.EndDate.HasValue)
                .WithMessage("Start date must be before or equal to end date.");
        }
    }
}