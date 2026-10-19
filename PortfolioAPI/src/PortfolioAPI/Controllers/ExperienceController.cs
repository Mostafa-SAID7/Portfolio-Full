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
        public async Task<ActionResult<ExperienceDto>> CreateExperience([FromBody] Experience experience)
        {
            var createdExperience = await _experienceService.CreateExperienceAsync(experience);
            return CreatedAtAction(nameof(GetExperienceById), new { id = createdExperience.Id }, Map(createdExperience));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExperience(int id, [FromBody] Experience experience)
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
