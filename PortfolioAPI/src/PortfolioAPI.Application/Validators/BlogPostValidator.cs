using System.Text.RegularExpressions;
using FluentValidation;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Validators
{
    public partial class BlogPostValidator : AbstractValidator<BlogPost>
    {
        [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
        private static partial Regex SlugRegex();

        public BlogPostValidator()
        {
            RuleFor(b => b.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(b => b.Slug)
                .NotEmpty().WithMessage("Slug is required.")
                .MaximumLength(300).WithMessage("Slug must not exceed 300 characters.")
                .Must(slug => slug == null || SlugRegex().IsMatch(slug))
                .WithMessage("Slug may only contain lowercase letters, numbers and hyphens.");

            RuleFor(b => b.Excerpt)
                .NotEmpty().WithMessage("Excerpt is required.")
                .MaximumLength(500).WithMessage("Excerpt must not exceed 500 characters.");

            RuleFor(b => b.Content)
                .NotEmpty().WithMessage("Content is required.");

            RuleFor(b => b.Author)
                .NotEmpty().WithMessage("Author is required.")
                .MaximumLength(100).WithMessage("Author must not exceed 100 characters.");

            RuleFor(b => b.ReadingTime)
                .InclusiveBetween(0, 600).WithMessage("Reading time must be between 0 and 600 minutes.");

            RuleFor(b => b.Tags)
                .Must(list => list == null || list.Count <= 20)
                .WithMessage("A post can have at most 20 tags.");

            RuleFor(b => b.MetaTitle)
                .MaximumLength(200).WithMessage("Meta title must not exceed 200 characters.");

            RuleFor(b => b.MetaDescription)
                .MaximumLength(500).WithMessage("Meta description must not exceed 500 characters.");
        }
    }
}
