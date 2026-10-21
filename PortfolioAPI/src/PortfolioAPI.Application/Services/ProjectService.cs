using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    /// <summary>
    /// Project management. Implements the DTO/Result-based contract: "not
    /// found" comes back as a failed Result (→ 404), while validation failures
    /// throw ValidationException like every other service, so the exception
    /// middleware emits the one shared { message, errors } 400 shape.
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Project> _validator;
        private readonly ICategoryService _categoryService;

        public ProjectService(
            IUnitOfWork unitOfWork,
            IValidator<Project> validator,
            ICategoryService categoryService)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
            _categoryService = categoryService;
        }

        public async Task<Result<ProjectDto>> GetByIdAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            return project == null
                ? Result<ProjectDto>.NotFound($"Project with id {id} was not found.")
                : Result<ProjectDto>.Ok(EntityMappings.ToDto(project));
        }

        public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync()
        {
            var projects = await _unitOfWork.Projects.GetAllAsync();
            var ordered = projects.OrderBy(p => p.Order).ThenByDescending(p => p.CreatedAt);
            return Result<IEnumerable<ProjectDto>>.Ok(ordered.Select(EntityMappings.ToDto).ToList());
        }

        public async Task<Result<IEnumerable<ProjectDto>>> GetFeaturedAsync()
        {
            var projects = await _unitOfWork.Projects.FindAsync(p => p.Featured && p.IsActive && !p.IsDeleted);
            return Result<IEnumerable<ProjectDto>>.Ok(projects.OrderBy(p => p.Order).Select(EntityMappings.ToDto).ToList());
        }

        /// <summary>
        /// Public surface: active projects only, filtered and ordered in SQL
        /// (issue #33). The admin list keeps using <see cref="GetAllAsync"/>.
        /// </summary>
        public async Task<Result<IEnumerable<ProjectDto>>> GetPublicProjectsAsync()
        {
            var projects = await _unitOfWork.Projects.GetPublicProjectsAsync();
            return Result<IEnumerable<ProjectDto>>.Ok(projects.Select(EntityMappings.ToDto).ToList());
        }

        public async Task<Result<IEnumerable<ProjectDto>>> GetByTechnologyAsync(string technology)
        {
            // Runs entirely in SQL (LIKE on the comma-separated Technologies
            // column) — EF cannot translate collection operators over the
            // converted column (issue #17).
            var projects = await _unitOfWork.Projects.GetByTechnologyAsync(technology);
            return Result<IEnumerable<ProjectDto>>.Ok(projects.OrderBy(p => p.Order).Select(EntityMappings.ToDto).ToList());
        }

        public async Task<Result<IEnumerable<ProjectDto>>> SearchAsync(string searchTerm)
        {
            // Database-side LIKE search across title/description/technologies
            // (issue #17) — previously an untranslatable expression that 500'd.
            var projects = await _unitOfWork.Projects.SearchAsync(searchTerm);
            return Result<IEnumerable<ProjectDto>>.Ok(projects.OrderBy(p => p.Order).Select(EntityMappings.ToDto).ToList());
        }

        /// <summary>
        /// Builds a URL-safe slug from a title and makes it unique
        /// ("haxel footwear" -&gt; "haxel-footwear", "haxel-footwear-2").
        /// Uses targeted SQL EXISTS checks (fix for C1) instead of loading
        /// the full projects table. The UNIQUE index on Slug acts as the
        /// final race-condition guard (fix for C2).
        /// </summary>
        private async Task<string> GenerateUniqueSlugAsync(string? title, int? excludeId = null)
        {
            var baseSlug = Slugify(title);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "project";
            }

            if (!await _unitOfWork.Projects.SlugExistsAsync(baseSlug, excludeId))
            {
                return baseSlug;
            }

            for (var suffix = 2; suffix < 500; suffix++)
            {
                var candidate = $"{baseSlug}-{suffix}";
                if (!await _unitOfWork.Projects.SlugExistsAsync(candidate, excludeId))
                {
                    return candidate;
                }
            }

            // Guaranteed-unique fallback: append a short random hex segment.
            return $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        /// <summary>
        /// Lowercase, dash-separated, ASCII-only version of a title.
        /// Applies NFD Unicode normalization first so accented/diacritic
        /// characters (e.g. é → e) survive — non-ASCII characters that
        /// have no ASCII equivalent (e.g. Arabic script) are dropped,
        /// producing a safe empty-string that the caller turns into "project"
        /// rather than a garbage slug (fix for M2).
        /// </summary>
        private static string Slugify(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            // NFD breaks composite characters (e.g. é) into base + combining
            // mark; the loop then keeps only ASCII letters/digits, so accented
            // Latin characters survive while pure non-Latin scripts (Arabic,
            // CJK, etc.) are dropped gracefully.
            var normalized = title.Trim()
                .Normalize(System.Text.NormalizationForm.FormD)
                .ToLowerInvariant();

            var builder = new System.Text.StringBuilder(normalized.Length);
            var lastWasDash = false;

            foreach (var ch in normalized)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    builder.Append(ch);
                    lastWasDash = false;
                }
                else if (!lastWasDash && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasDash = true;
                }
            }

            return builder.ToString().Trim('-');
        }

        public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto)
        {
            var project = new Project
            {
                // Null-tolerant: an explicit JSON null must reach the validator
                // as an empty string (→ 400) instead of NRE-ing here (→ 500).
                TitleEn = dto.TitleEn?.Trim() ?? string.Empty,
                TitleAr = dto.TitleAr?.Trim() ?? string.Empty,
                DescriptionEn = dto.DescriptionEn?.Trim() ?? string.Empty,
                DescriptionAr = dto.DescriptionAr?.Trim() ?? string.Empty,
                ImageUrl = dto.ImageUrl,
                StorageProvider = dto.StorageProvider,
                StoragePublicId = dto.StoragePublicId,
                Technologies = Sanitize(dto.Technologies),
                GithubUrl = dto.GithubUrl,
                LiveUrl = dto.LiveUrl,
                Featured = dto.Featured,
                Order = dto.Order,
                IsActive = dto.IsActive,
                CategoryEn = NullIfBlank(dto.CategoryEn),
                CategoryAr = NullIfBlank(dto.CategoryAr),
                ClientEn = NullIfBlank(dto.ClientEn),
                ClientAr = NullIfBlank(dto.ClientAr),
                TypeEn = NullIfBlank(dto.TypeEn),
                TypeAr = NullIfBlank(dto.TypeAr),
                DatabaseEn = NullIfBlank(dto.DatabaseEn),
                DatabaseAr = NullIfBlank(dto.DatabaseAr),
                StatusEn = NullIfBlank(dto.StatusEn),
                StatusAr = NullIfBlank(dto.StatusAr),
                BadgesEn = Sanitize(dto.BadgesEn),
                BadgesAr = Sanitize(dto.BadgesAr),
                MetricsEn = dto.MetricsEn?.Select(m => new ProjectMetric
                {
                    LabelEn = m.LabelEn?.Trim() ?? string.Empty,
                    LabelAr = m.LabelAr?.Trim() ?? string.Empty,
                    Value = m.Value?.Trim() ?? string.Empty
                }).Where(m => m.LabelEn.Length > 0).ToList() ?? new List<ProjectMetric>(),
                MetricsAr = dto.MetricsAr?.Select(m => new ProjectMetric
                {
                    LabelEn = m.LabelEn?.Trim() ?? string.Empty,
                    LabelAr = m.LabelAr?.Trim() ?? string.Empty,
                    Value = m.Value?.Trim() ?? string.Empty
                }).Where(m => m.LabelEn.Length > 0).ToList() ?? new List<ProjectMetric>(),
                Gradient = NullIfBlank(dto.Gradient),
                CreatedAt = DateTime.UtcNow
            };

            var validation = await _validator.ValidateAsync(project);
            if (!validation.IsValid)
            {
                // Thrown (like every other service) so the exception middleware
                // maps it to 400 { message, errors: { field: [...] } } — the same
                // shape clients get from all other entities (issue #16).
                throw new ValidationException(validation.Errors);
            }

            // The public link is generated once and never changes again, so
            // renaming a project cannot break its indexed URL. It is set after
            // the validation, so an invalid project never touches the database.
            project.Slug = await GenerateUniqueSlugAsync(project.TitleEn);

            var created = await _unitOfWork.Projects.AddAsync(project);
            // A category typed in the dashboard becomes a managed category, so
            // it can be renamed/reordered/hidden later and shows up as a filter
            // chip on the public site.
            await _categoryService.EnsureCategoryAsync(project.CategoryEn);
            await _unitOfWork.SaveChangesAsync();
            return Result<ProjectDto>.Ok(EntityMappings.ToDto(created), "Project created.");
        }

        public async Task<Result<ProjectDto>> UpdateAsync(int id, UpdateProjectDto dto)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                return Result<ProjectDto>.NotFound($"Project with id {id} was not found.");
            }

            // Capture the original category BEFORE the update so we only call
            // EnsureCategoryAsync when the value actually changed (fix for M1).
            var previousCategoryEn = project.CategoryEn;
            project.TitleEn = dto.TitleEn?.Trim() ?? string.Empty;
            project.TitleAr = dto.TitleAr?.Trim() ?? string.Empty;
            // The slug is NOT touched by a rename - that is the whole point:
            // the public link of a project has to stay valid. Legacy rows
            // (created before slugs existed) get one on their first save.
            if (string.IsNullOrWhiteSpace(project.Slug))
            {
                project.Slug = await GenerateUniqueSlugAsync(project.TitleEn, project.Id);
            }
            project.DescriptionEn = dto.DescriptionEn?.Trim() ?? string.Empty;
            project.DescriptionAr = dto.DescriptionAr?.Trim() ?? string.Empty;
            project.ImageUrl = dto.ImageUrl;
            project.StorageProvider = dto.StorageProvider;
            project.StoragePublicId = dto.StoragePublicId;
            project.Technologies = Sanitize(dto.Technologies);
            project.GithubUrl = dto.GithubUrl;
            project.LiveUrl = dto.LiveUrl;
            project.Featured = dto.Featured;
            project.Order = dto.Order;
            project.IsActive = dto.IsActive;
            project.CategoryEn = NullIfBlank(dto.CategoryEn);
            project.CategoryAr = NullIfBlank(dto.CategoryAr);
            project.ClientEn = NullIfBlank(dto.ClientEn);
            project.ClientAr = NullIfBlank(dto.ClientAr);
            project.TypeEn = NullIfBlank(dto.TypeEn);
            project.TypeAr = NullIfBlank(dto.TypeAr);
            project.DatabaseEn = NullIfBlank(dto.DatabaseEn);
            project.DatabaseAr = NullIfBlank(dto.DatabaseAr);
            project.StatusEn = NullIfBlank(dto.StatusEn);
            project.StatusAr = NullIfBlank(dto.StatusAr);
            project.BadgesEn = Sanitize(dto.BadgesEn);
            project.BadgesAr = Sanitize(dto.BadgesAr);
            project.MetricsEn = dto.MetricsEn?.Select(m => new ProjectMetric
            {
                LabelEn = m.LabelEn?.Trim() ?? string.Empty,
                LabelAr = m.LabelAr?.Trim() ?? string.Empty,
                Value = m.Value?.Trim() ?? string.Empty
            }).Where(m => m.LabelEn.Length > 0).ToList() ?? new List<ProjectMetric>();
            project.MetricsAr = dto.MetricsAr?.Select(m => new ProjectMetric
            {
                LabelEn = m.LabelEn?.Trim() ?? string.Empty,
                LabelAr = m.LabelAr?.Trim() ?? string.Empty,
                Value = m.Value?.Trim() ?? string.Empty
            }).Where(m => m.LabelEn.Length > 0).ToList() ?? new List<ProjectMetric>();
            project.Gradient = NullIfBlank(dto.Gradient);
            project.UpdatedAt = DateTime.UtcNow;

            var validation = await _validator.ValidateAsync(project);
            if (!validation.IsValid)
            {
                // Same 400 { message, errors } path as create (issue #16).
                throw new ValidationException(validation.Errors);
            }

            await _unitOfWork.Projects.UpdateAsync(project);
            // Only sync the category when it actually changed (fix for M1).
            if (!string.Equals(previousCategoryEn, project.CategoryEn, StringComparison.OrdinalIgnoreCase))
            {
                await _categoryService.EnsureCategoryAsync(project.CategoryEn);
            }
            await _unitOfWork.SaveChangesAsync();
            return Result<ProjectDto>.Ok(EntityMappings.ToDto(project), "Project updated.");
        }

        public async Task<Result<bool>> DeleteAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                return Result<bool>.NotFound($"Project with id {id} was not found.");
            }

            await _unitOfWork.Projects.DeleteAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Ok(true, "Project deleted.");
        }

        public async Task<Result<bool>> ToggleFeaturedAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                return Result<bool>.NotFound($"Project with id {id} was not found.");
            }

            project.Featured = !project.Featured;
            project.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Projects.UpdateAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Ok(project.Featured);
        }

        private static List<string> Sanitize(List<string>? technologies) =>
            (technologies ?? new List<string>())
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => t.Length > 0)
            .ToList();

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
