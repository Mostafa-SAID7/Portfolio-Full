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
            await _categoryService.EnsureCategoryAsync(project.CategoryEn);
            await _unitOfWork.SaveChangesAsync();

            if (dto.WebsiteAssignments != null && dto.WebsiteAssignments.Any())
            {
                var allWebsites = (await _unitOfWork.Websites.GetAllAsync()).ToDictionary(w => w.Id);
                created.ProjectWebsites ??= new List<ProjectWebsite>();
                foreach (var assign in dto.WebsiteAssignments)
                {
                    if (allWebsites.TryGetValue(assign.WebsiteId, out var website))
                    {
                        var pw = new ProjectWebsite
                        {
                            ProjectId = created.Id,
                            WebsiteId = assign.WebsiteId,
                            IsPublished = assign.IsPublished,
                            DisplayOrder = assign.DisplayOrder,
                            CreatedAt = DateTime.UtcNow,
                            Website = website
                        };
                        await _unitOfWork.ProjectWebsites.AddAsync(pw);
                        created.ProjectWebsites.Add(pw);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }


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
            // Storage identity: the stored values survive only while the cover
            // itself is unchanged (a payload that omits the fields must not
            // strip them). A REPLACE without metadata leaves the new asset
            // unmanaged instead of tagging it with the old asset's identity —
            // a stale public id would aim later deletes at the wrong asset.
            // Clearing the cover clears the identity.
            var previousImageUrl = project.ImageUrl;
            project.ImageUrl = dto.ImageUrl;
            project.StorageProvider =
                string.IsNullOrWhiteSpace(project.ImageUrl)
                    ? null
                    : string.Equals(project.ImageUrl, previousImageUrl, StringComparison.Ordinal)
                        ? dto.StorageProvider ?? project.StorageProvider
                        : dto.StorageProvider;
            project.StoragePublicId =
                string.IsNullOrWhiteSpace(project.ImageUrl)
                    ? null
                    : string.Equals(project.ImageUrl, previousImageUrl, StringComparison.Ordinal)
                        ? dto.StoragePublicId ?? project.StoragePublicId
                        : dto.StoragePublicId;
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
            if (!string.Equals(previousCategoryEn, project.CategoryEn, StringComparison.OrdinalIgnoreCase))
            {
                await _categoryService.EnsureCategoryAsync(project.CategoryEn);
            }

            if (dto.WebsiteAssignments != null)
            {
                var allWebsites = (await _unitOfWork.Websites.GetAllAsync()).ToDictionary(w => w.Id);

                // Use the tracked collection on the loaded project entity to avoid EF entity tracking conflicts
                project.ProjectWebsites ??= new List<ProjectWebsite>();
                var existingMap = project.ProjectWebsites.ToDictionary(pw => pw.WebsiteId);
                var validAssignments = dto.WebsiteAssignments.Where(a => allWebsites.ContainsKey(a.WebsiteId)).ToList();
                var validWebsiteIds = validAssignments.Select(a => a.WebsiteId).ToHashSet();

                var toRemove = project.ProjectWebsites.Where(pw => !validWebsiteIds.Contains(pw.WebsiteId)).ToList();
                foreach (var rem in toRemove)
                {
                    await _unitOfWork.ProjectWebsites.DeleteAsync(rem);
                    project.ProjectWebsites.Remove(rem);
                }

                foreach (var assign in validAssignments)
                {
                    if (existingMap.TryGetValue(assign.WebsiteId, out var existing))
                    {
                        existing.IsPublished = assign.IsPublished;
                        existing.DisplayOrder = assign.DisplayOrder;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.Website = allWebsites[assign.WebsiteId];
                    }
                    else
                    {
                        var newAssignment = new ProjectWebsite
                        {
                            ProjectId = id,
                            WebsiteId = assign.WebsiteId,
                            IsPublished = assign.IsPublished,
                            DisplayOrder = assign.DisplayOrder,
                            CreatedAt = DateTime.UtcNow,
                            Website = allWebsites[assign.WebsiteId]
                        };
                        await _unitOfWork.ProjectWebsites.AddAsync(newAssignment);
                        project.ProjectWebsites.Add(newAssignment);
                    }
                }
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

        public async Task<Result<IEnumerable<ProjectDto>>> GetProjectsByWebsiteAsync(string websiteSlug, string? category = null, string? search = null, bool? featured = null)
        {
            if (string.IsNullOrWhiteSpace(websiteSlug))
            {
                return Result<IEnumerable<ProjectDto>>.Fail("Website slug is required.");
            }

            var projects = await _unitOfWork.Projects.GetProjectsByWebsiteAsync(websiteSlug, category, search, featured);
            return Result<IEnumerable<ProjectDto>>.Ok(projects.Select(EntityMappings.ToDto).ToList());
        }

        public async Task<Result<ProjectDto>> GetProjectByWebsiteAndSlugAsync(string websiteSlug, string projectSlug)
        {
            if (string.IsNullOrWhiteSpace(websiteSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return Result<ProjectDto>.Fail("Website slug and project slug are required.");
            }

            var project = await _unitOfWork.Projects.GetProjectByWebsiteAndSlugAsync(websiteSlug, projectSlug);
            return project == null
                ? Result<ProjectDto>.NotFound($"Project with slug '{projectSlug}' was not found on website '{websiteSlug}'.")
                : Result<ProjectDto>.Ok(EntityMappings.ToDto(project));
        }

        public async Task<Result<IEnumerable<string>>> GetCategoriesByWebsiteAsync(string websiteSlug)
        {
            if (string.IsNullOrWhiteSpace(websiteSlug))
            {
                return Result<IEnumerable<string>>.Fail("Website slug is required.");
            }

            var categories = await _unitOfWork.Projects.GetCategoriesByWebsiteAsync(websiteSlug);
            return Result<IEnumerable<string>>.Ok(categories);
        }

        public async Task<Result<IEnumerable<ProjectWebsiteDto>>> GetWebsiteAssignmentsAsync(int projectId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
            {
                return Result<IEnumerable<ProjectWebsiteDto>>.NotFound($"Project with id {projectId} was not found.");
            }

            var assignments = await _unitOfWork.ProjectWebsites.FindAsync(pw => pw.ProjectId == projectId);
            var websites = (await _unitOfWork.Websites.GetAllAsync()).ToDictionary(w => w.Id);

            var result = assignments.Select(pw => new ProjectWebsiteDto
            {
                WebsiteId = pw.WebsiteId,
                WebsiteSlug = websites.TryGetValue(pw.WebsiteId, out var w) ? w.Slug : string.Empty,
                WebsiteName = websites.TryGetValue(pw.WebsiteId, out var w2) ? w2.Name : string.Empty,
                IsPublished = pw.IsPublished,
                DisplayOrder = pw.DisplayOrder
            }).ToList();

            return Result<IEnumerable<ProjectWebsiteDto>>.Ok(result);
        }

        public async Task<Result<IEnumerable<ProjectWebsiteDto>>> UpdateWebsiteAssignmentsAsync(int projectId, UpdateProjectWebsitesDto dto)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
            {
                return Result<IEnumerable<ProjectWebsiteDto>>.NotFound($"Project with id {projectId} was not found.");
            }

            var existingAssignments = (await _unitOfWork.ProjectWebsites.FindAsync(pw => pw.ProjectId == projectId)).ToList();
            var newIds = (dto.Websites ?? new()).Select(a => a.WebsiteId).ToHashSet();

            foreach (var existing in existingAssignments.Where(e => !newIds.Contains(e.WebsiteId)))
            {
                await _unitOfWork.ProjectWebsites.DeleteAsync(existing);
            }

            foreach (var assign in dto.Websites ?? new())
            {
                var existing = existingAssignments.FirstOrDefault(e => e.WebsiteId == assign.WebsiteId);
                if (existing != null)
                {
                    existing.IsPublished = assign.IsPublished;
                    existing.DisplayOrder = assign.DisplayOrder;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProjectWebsites.UpdateAsync(existing);
                }
                else
                {
                    await _unitOfWork.ProjectWebsites.AddAsync(new ProjectWebsite
                    {
                        ProjectId = projectId,
                        WebsiteId = assign.WebsiteId,
                        IsPublished = assign.IsPublished,
                        DisplayOrder = assign.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return await GetWebsiteAssignmentsAsync(projectId);
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
