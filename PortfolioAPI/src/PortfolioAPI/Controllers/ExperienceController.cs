using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExperienceController : ControllerBase
    {
        private readonly IExperienceService _experienceService;

        public ExperienceController(IExperienceService experienceService)
        {
            _experienceService = experienceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetAllExperiences()
        {
            var experiences = await _experienceService.GetAllExperiencesAsync();
            var experienceDtos = experiences.Select(e => new ExperienceDto
            {
                Id = e.Id,
                JobTitle = e.JobTitle,
                CompanyName = e.CompanyName,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsCurrentPosition = e.IsCurrentPosition,
                Description = e.Description,
                Technologies = e.Technologies,
                CompanyWebsite = e.CompanyWebsite,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            });
            return Ok(experienceDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExperienceDto>> GetExperienceById(int id)
        {
            var experience = await _experienceService.GetExperienceByIdAsync(id);
            var experienceDto = new ExperienceDto
            {
                Id = experience.Id,
                JobTitle = experience.JobTitle,
                CompanyName = experience.CompanyName,
                Location = experience.Location,
                StartDate = experience.StartDate,
                EndDate = experience.EndDate,
                IsCurrentPosition = experience.IsCurrentPosition,
                Description = experience.Description,
                Technologies = experience.Technologies,
                CompanyWebsite = experience.CompanyWebsite,
                CreatedAt = experience.CreatedAt,
                UpdatedAt = experience.UpdatedAt
            };
            return Ok(experienceDto);
        }

        [HttpGet("current")]
        public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetCurrentExperiences()
        {
            var experiences = await _experienceService.GetCurrentExperiencesAsync();
            var experienceDtos = experiences.Select(e => new ExperienceDto
            {
                Id = e.Id,
                JobTitle = e.JobTitle,
                CompanyName = e.CompanyName,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsCurrentPosition = e.IsCurrentPosition,
                Description = e.Description,
                Technologies = e.Technologies,
                CompanyWebsite = e.CompanyWebsite,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            });
            return Ok(experienceDtos);
        }

        [HttpPost]
        public async Task<ActionResult<ExperienceDto>> CreateExperience(Experience experience)
        {
            var createdExperience = await _experienceService.CreateExperienceAsync(experience);
            var experienceDto = new ExperienceDto
            {
                Id = createdExperience.Id,
                JobTitle = createdExperience.JobTitle,
                CompanyName = createdExperience.CompanyName,
                Location = createdExperience.Location,
                StartDate = createdExperience.StartDate,
                EndDate = createdExperience.EndDate,
                IsCurrentPosition = createdExperience.IsCurrentPosition,
                Description = createdExperience.Description,
                Technologies = createdExperience.Technologies,
                CompanyWebsite = createdExperience.CompanyWebsite,
                CreatedAt = createdExperience.CreatedAt,
                UpdatedAt = createdExperience.UpdatedAt
            };
            return CreatedAtAction(nameof(GetExperienceById), new { id = experienceDto.Id }, experienceDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExperience(int id, Experience experience)
        {
            if (id != experience.Id)
            {
                return BadRequest();
            }

            await _experienceService.UpdateExperienceAsync(experience);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            await _experienceService.DeleteExperienceAsync(id);
            return NoContent();
        }
    }
}