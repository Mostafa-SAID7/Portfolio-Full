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

            RuleFor(p => p.LiveUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(p => !string.IsNullOrEmpty(p.LiveUrl))
                .WithMessage("Live URL must be a valid URL.");

            RuleFor(p => p.GithubUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(p => !string.IsNullOrEmpty(p.GithubUrl))
                .WithMessage("GitHub URL must be a valid URL.");

            RuleFor(p => p.ImageUrl)
                .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.");

            RuleFor(p => p.Technologies)
                .Must(list => list == null || list.Count <= 20)
                .WithMessage("A project can list at most 20 technologies.");

            RuleFor(p => p.Category).MaximumLength(100).WithMessage("Category must not exceed 100 characters.");
            RuleFor(p => p.Client).MaximumLength(100).WithMessage("Client must not exceed 100 characters.");
            RuleFor(p => p.Type).MaximumLength(50).WithMessage("Type must not exceed 50 characters.");
            RuleFor(p => p.Database).MaximumLength(100).WithMessage("Database must not exceed 100 characters.");
            RuleFor(p => p.Status).MaximumLength(50).WithMessage("Status must not exceed 50 characters.");
            RuleFor(p => p.Gradient).MaximumLength(200).WithMessage("Gradient must not exceed 200 characters.");
            RuleFor(p => p.Badges)
                .Must(list => list == null || list.Count <= 5)
                .WithMessage("A project can have at most 5 badges.");
            RuleFor(p => p.Metrics)
                .Must(list => list == null || list.Count <= 6)
                .WithMessage("A project can show at most 6 metrics.");
        }
    }
}
