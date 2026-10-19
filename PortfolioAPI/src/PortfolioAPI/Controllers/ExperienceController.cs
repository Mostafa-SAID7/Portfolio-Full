using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            return Ok(experiences.OrderBy(e => e.Order).ThenByDescending(e => e.StartDate).Select(Map));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExperienceDto>> GetExperienceById(int id)
        {
            var experience = await _experienceService.GetExperienceByIdAsync(id);
            return Ok(Map(experience));
        }

        [HttpGet("current")]
        public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetCurrentExperiences()
        {
            var experiences = await _experienceService.GetCurrentExperiencesAsync();
            return Ok(experiences.Select(Map));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ExperienceDto>> CreateExperience([FromBody] CreateExperienceDto dto)
        {
            var experience = MapToEntity(dto);
            var createdExperience = await _experienceService.CreateExperienceAsync(experience);
            return CreatedAtAction(nameof(GetExperienceById), new { id = createdExperience.Id }, Map(createdExperience));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateExperience(int id, [FromBody] UpdateExperienceDto dto)
        {
            // DTO binding (not the raw EF entity): the route id wins and
            // server-managed fields (createdAt/updatedAt) can never be over-posted.
            var experience = MapToEntity(dto);
            experience.Id = id;

            await _experienceService.UpdateExperienceAsync(experience);
            return NoContent();
        }

        private static Experience MapToEntity(CreateExperienceDto dto) => new()
        {
            Company = dto.Company,
            Position = dto.Position,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Description = dto.Description,
            Technologies = dto.Technologies ?? new List<string>(),
            Location = dto.Location,
            Current = dto.Current,
            Order = dto.Order,
            IsActive = dto.IsActive
        };

        private static Experience MapToEntity(UpdateExperienceDto dto) => new()
        {
            Company = dto.Company,
            Position = dto.Position,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Description = dto.Description,
            Technologies = dto.Technologies ?? new List<string>(),
            Location = dto.Location,
            Current = dto.Current,
            Order = dto.Order,
            IsActive = dto.IsActive
        };

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            await _experienceService.DeleteExperienceAsync(id);
            return NoContent();
        }

        private static ExperienceDto Map(Experience e) => new()
        {
            Id = e.Id,
            Company = e.Company,
            Position = e.Position,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Description = e.Description,
            Technologies = e.Technologies?.ToList() ?? new List<string>(),
            Location = e.Location,
            Current = e.Current,
            Order = e.Order,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
