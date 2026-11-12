using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Application.Mappings;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Application.Interfaces;

namespace PortfolioAPI.Application.Services
{
    public class WebsiteService : IWebsiteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WebsiteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IEnumerable<WebsiteDto>>> GetAllAsync()
        {
            var websites = await _unitOfWork.Websites.GetAllAsync();
            var allAssignments = await _unitOfWork.ProjectWebsites.GetAllAsync();
            var projects = (await _unitOfWork.Projects.GetAllAsync()).ToDictionary(p => p.Id);

            var counts = allAssignments
                .Where(pw => projects.TryGetValue(pw.ProjectId, out var p) && !p.IsDeleted)
                .GroupBy(pw => pw.WebsiteId)
                .ToDictionary(g => g.Key, g => g.Count());

            var dtos = websites
                .Where(w => !w.IsDeleted)
                .OrderBy(w => w.Order)
                .ThenBy(w => w.Name)
                .Select(w =>
                {
                    var dto = EntityMappings.ToDto(w);
                    dto.AssignedProjectsCount = counts.GetValueOrDefault(w.Id, 0);
                    return dto;
                })
                .ToList();

            return Result<IEnumerable<WebsiteDto>>.Ok(dtos);
        }

        public async Task<Result<IEnumerable<WebsiteDto>>> GetActiveWebsitesAsync()
        {
            var websites = await _unitOfWork.Websites.FindAsync(w => w.IsActive && !w.IsDeleted);
            var dtos = websites
                .OrderBy(w => w.Order)
                .ThenBy(w => w.Name)
                .Select(EntityMappings.ToDto)
                .ToList();

            return Result<IEnumerable<WebsiteDto>>.Ok(dtos);
        }

        public async Task<Result<WebsiteDto>> GetByIdAsync(int id)
        {
            var website = await _unitOfWork.Websites.GetByIdAsync(id);
            if (website == null || website.IsDeleted)
            {
                return Result<WebsiteDto>.NotFound($"Website with id {id} was not found.");
            }

            var assignments = await _unitOfWork.ProjectWebsites.FindAsync(pw => pw.WebsiteId == id);
            var projects = (await _unitOfWork.Projects.GetAllAsync()).ToDictionary(p => p.Id);
            var count = assignments.Count(pw => projects.TryGetValue(pw.ProjectId, out var p) && !p.IsDeleted);

            var dto = EntityMappings.ToDto(website);
            dto.AssignedProjectsCount = count;
            return Result<WebsiteDto>.Ok(dto);
        }

        public async Task<Result<WebsiteDto>> GetBySlugAsync(string slug)
        {
            var normalized = slug.Trim().ToLowerInvariant();
            var websites = await _unitOfWork.Websites.FindAsync(w => w.Slug.ToLower() == normalized && !w.IsDeleted);
            var website = websites.FirstOrDefault();

            if (website == null)
            {
                return Result<WebsiteDto>.NotFound($"Website with slug '{slug}' was not found.");
            }

            return Result<WebsiteDto>.Ok(EntityMappings.ToDto(website));
        }

        public async Task<Result<WebsiteDto>> CreateAsync(CreateWebsiteDto dto)
        {
            var name = dto.Name?.Trim() ?? string.Empty;
            var slug = (dto.Slug?.Trim() ?? string.Empty).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(nameof(dto.Name), "Website name is required.") });
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = Slugify(name);
            }

            var existing = await _unitOfWork.Websites.FindAsync(w => w.Slug.ToLower() == slug && !w.IsDeleted);
            if (existing.Any())
            {
                throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(nameof(dto.Slug), $"A website with slug '{slug}' already exists.") });
            }

            var website = new Website
            {
                Name = name,
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim(),
                Order = dto.Order,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Websites.AddAsync(website);
            await _unitOfWork.SaveChangesAsync();

            return Result<WebsiteDto>.Ok(EntityMappings.ToDto(website), "Website created successfully.");
        }

        public async Task<Result<WebsiteDto>> UpdateAsync(int id, UpdateWebsiteDto dto)
        {
            var website = await _unitOfWork.Websites.GetByIdAsync(id);
            if (website == null || website.IsDeleted)
            {
                return Result<WebsiteDto>.NotFound($"Website with id {id} was not found.");
            }

            var name = dto.Name?.Trim() ?? string.Empty;
            var slug = (dto.Slug?.Trim() ?? string.Empty).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(nameof(dto.Name), "Website name is required.") });
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = Slugify(name);
            }

            var existing = await _unitOfWork.Websites.FindAsync(w => w.Slug.ToLower() == slug && w.Id != id && !w.IsDeleted);
            if (existing.Any())
            {
                throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(nameof(dto.Slug), $"A website with slug '{slug}' already exists.") });
            }

            website.Name = name;
            website.Slug = slug;
            website.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            website.BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim();
            website.Order = dto.Order;
            website.IsActive = dto.IsActive;
            website.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Websites.UpdateAsync(website);
            await _unitOfWork.SaveChangesAsync();

            return Result<WebsiteDto>.Ok(EntityMappings.ToDto(website), "Website updated successfully.");
        }

        public async Task<Result<bool>> DeleteAsync(int id)
        {
            var website = await _unitOfWork.Websites.GetByIdAsync(id);
            if (website == null || website.IsDeleted)
            {
                return Result<bool>.NotFound($"Website with id {id} was not found.");
            }

            await _unitOfWork.Websites.DeleteAsync(website);
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Ok(true, "Website deleted successfully.");
        }

        public async Task<Result<bool>> ToggleActiveAsync(int id)
        {
            var website = await _unitOfWork.Websites.GetByIdAsync(id);
            if (website == null || website.IsDeleted)
            {
                return Result<bool>.NotFound($"Website with id {id} was not found.");
            }

            website.IsActive = !website.IsActive;
            website.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Websites.UpdateAsync(website);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Ok(website.IsActive);
        }

        private static string Slugify(string name)
        {
            var normalized = name.Trim()
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

            var slug = builder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "website" : slug;
        }
    }
}

