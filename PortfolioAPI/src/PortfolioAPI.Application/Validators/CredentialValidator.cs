using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public class CredentialValidator : AbstractValidator<Credential>
    {
        private static readonly string[] AllowedTabs = { "work", "projects", "education", "certifications" };

        public CredentialValidator()
        {
            RuleFor(c => c.Tab)
                .NotEmpty().WithMessage("Tab is required.")
                .Must(tab => AllowedTabs.Contains(tab.ToLower()))
                .WithMessage("Tab must be one of: work, projects, education, certifications.");

            RuleFor(c => c.Year).MaximumLength(10).WithMessage("Year must not exceed 10 characters.");

            RuleFor(c => c.PeriodEn).NotEmpty().WithMessage("Period (en) is required.").MaximumLength(100);
            RuleFor(c => c.PeriodAr).NotEmpty().WithMessage("Period (ar) is required.").MaximumLength(100);
            RuleFor(c => c.TitleEn).NotEmpty().WithMessage("Title (en) is required.").MaximumLength(200);
            RuleFor(c => c.TitleAr).NotEmpty().WithMessage("Title (ar) is required.").MaximumLength(200);
            RuleFor(c => c.OrgEn).NotEmpty().WithMessage("Organization (en) is required.").MaximumLength(150);
            RuleFor(c => c.OrgAr).NotEmpty().WithMessage("Organization (ar) is required.").MaximumLength(150);
            RuleFor(c => c.LocationEn).MaximumLength(150);
            RuleFor(c => c.LocationAr).MaximumLength(150);
            RuleFor(c => c.SummaryEn).MaximumLength(1000);
            RuleFor(c => c.SummaryAr).MaximumLength(1000);

            RuleFor(c => c.HighlightsEn)
                .Must(list => list == null || list.Count <= 10)
                .WithMessage("At most 10 highlights (en) are allowed.");
            RuleFor(c => c.HighlightsAr)
                .Must(list => list == null || list.Count <= 10)
                .WithMessage("At most 10 highlights (ar) are allowed.");
            RuleFor(c => c.Stack)
                .Must(list => list == null || list.Count <= 20)
                .WithMessage("At most 20 stack entries are allowed.");

            RuleFor(c => c.CredentialId).MaximumLength(100);
            RuleFor(c => c.StatusEn).MaximumLength(50);
            RuleFor(c => c.StatusAr).MaximumLength(50);
        }
    }
}
