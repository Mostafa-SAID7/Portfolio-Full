using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class ProjectValidator : AbstractValidator<Project>
    {
        public ProjectValidator()
        {
            RuleFor(p => p.TitleEn)
                .NotEmpty().WithMessage("Title (en) is required.")
                .MaximumLength(100).WithMessage("Title (en) must not exceed 100 characters.");

            RuleFor(p => p.TitleAr)
                .NotEmpty().WithMessage("Title (ar) is required.")
                .MaximumLength(100).WithMessage("Title (ar) must not exceed 100 characters.");

            RuleFor(p => p.DescriptionEn)
                .NotEmpty().WithMessage("Description (en) is required.")
                .MaximumLength(500).WithMessage("Description (en) must not exceed 500 characters.");

            RuleFor(p => p.DescriptionAr)
                .NotEmpty().WithMessage("Description (ar) is required.")
                .MaximumLength(500).WithMessage("Description (ar) must not exceed 500 characters.");

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

            RuleFor(p => p.CategoryEn).MaximumLength(100).WithMessage("Category (en) must not exceed 100 characters.");
            RuleFor(p => p.CategoryAr).MaximumLength(100).WithMessage("Category (ar) must not exceed 100 characters.");
            RuleFor(p => p.ClientEn).MaximumLength(100).WithMessage("Client (en) must not exceed 100 characters.");
            RuleFor(p => p.ClientAr).MaximumLength(100).WithMessage("Client (ar) must not exceed 100 characters.");
            RuleFor(p => p.TypeEn).MaximumLength(50).WithMessage("Type (en) must not exceed 50 characters.");
            RuleFor(p => p.TypeAr).MaximumLength(50).WithMessage("Type (ar) must not exceed 50 characters.");
            RuleFor(p => p.DatabaseEn).MaximumLength(100).WithMessage("Database (en) must not exceed 100 characters.");
            RuleFor(p => p.DatabaseAr).MaximumLength(100).WithMessage("Database (ar) must not exceed 100 characters.");
            RuleFor(p => p.StatusEn).MaximumLength(50).WithMessage("Status (en) must not exceed 50 characters.");
            RuleFor(p => p.StatusAr).MaximumLength(50).WithMessage("Status (ar) must not exceed 50 characters.");
            RuleFor(p => p.Gradient).MaximumLength(200).WithMessage("Gradient must not exceed 200 characters.");
            RuleFor(p => p.BadgesEn)
                .Must(list => list == null || list.Count <= 5)
                .WithMessage("A project can have at most 5 badges (en).");
            RuleFor(p => p.BadgesAr)
                .Must(list => list == null || list.Count <= 5)
                .WithMessage("A project can have at most 5 badges (ar).");
            RuleFor(p => p.MetricsEn)
                .Must(list => list == null || list.Count <= 6)
                .WithMessage("A project can show at most 6 metrics (en).");
            RuleFor(p => p.MetricsAr)
                .Must(list => list == null || list.Count <= 6)
                .WithMessage("A project can show at most 6 metrics (ar).");
        }
    }
}
